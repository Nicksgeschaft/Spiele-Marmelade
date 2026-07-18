using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Owns the brick grid/graph for one player: registers the Main-Brick at (0,0), attaches new
    // bricks at cardinal offsets, and keeps total mass / local center of mass (and the Rigidbody
    // that reads them) in sync. Attach() is the single entry point - WorldBrick calls it on
    // collision, the Debug context menu below calls it directly for manual testing.
    // Up/Down = world Y (jump axis), Left/Right = world X (horizontal axis).
    // See Docs/BrickMovementController_Anforderungen_v0.2.md sections 2.1 and 5.
    public class PlayerAssembly : MonoBehaviour
    {
        private static readonly CardinalDirection[] AllDirections =
        {
            CardinalDirection.Up, CardinalDirection.Down, CardinalDirection.Left, CardinalDirection.Right,
        };

        [Header("Grid")]
        [Tooltip("World-space size of one grid step along the jump axis (world Y).")]
        [SerializeField] private float gridStepVertical = 1.14f;
        [Tooltip("World-space size of one grid step along the horizontal axis (world X).")]
        [SerializeField] private float gridStepHorizontal = 0.795f;

        [Header("Main Brick")]
        [SerializeField] private BrickNode mainBrick;

        [Header("Detach")]
        [Tooltip("Impulse applied to a fragment as it falls off, pointing away from the assembly's pivot.")]
        [SerializeField] private float outwardImpulseStrength = 1f;

        [Header("Debug")]
        [SerializeField] private BrickNode testBrickPrefab;

        private readonly Dictionary<Vector2Int, BrickNode> _grid = new();
        private readonly List<BrickNode> _connectedBricks = new();
        private Rigidbody _rigidbody;
        private int _nextBrickId;

        public BrickNode MainBrick => mainBrick;
        public IReadOnlyList<BrickNode> ConnectedBricks => _connectedBricks;
        public float TotalMass { get; private set; }
        public Vector3 LocalCenterOfMass { get; private set; }

        // Docs section 7: only ever built from the bricks currently connected to the Main-Brick.
        // Recomputed on each access rather than cached - fine at our brick-count scale (P-02: <=20).
        public IReadOnlyDictionary<BrickColor, int> ColorCounts
        {
            get
            {
                var counts = new Dictionary<BrickColor, int>();
                foreach (BrickNode brick in _connectedBricks)
                {
                    counts.TryGetValue(brick.Color, out int count);
                    counts[brick.Color] = count + 1;
                }
                return counts;
            }
        }

        public event Action<BrickNode> OnBrickAttached;
        public event Action<IReadOnlyList<BrickNode>> OnBricksDetached;
        public event Action OnAssemblyChanged;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            if (mainBrick == null)
            {
                Debug.LogError($"[PlayerAssembly] No Main-Brick assigned on '{name}'.", this);
                return;
            }

            RegisterBrick(mainBrick, Vector2Int.zero);
            RecomputeMassAndCenter();
        }

        public static Vector2Int Offset(CardinalDirection direction) => direction switch
        {
            CardinalDirection.Up => new Vector2Int(1, 0),
            CardinalDirection.Down => new Vector2Int(-1, 0),
            CardinalDirection.Right => new Vector2Int(0, 1),
            CardinalDirection.Left => new Vector2Int(0, -1),
            _ => Vector2Int.zero,
        };

        private static CardinalDirection Opposite(CardinalDirection direction) => direction switch
        {
            CardinalDirection.Up => CardinalDirection.Down,
            CardinalDirection.Down => CardinalDirection.Up,
            CardinalDirection.Right => CardinalDirection.Left,
            CardinalDirection.Left => CardinalDirection.Right,
            _ => direction,
        };

        // Grid (0,0) is anchored to wherever the Main-Brick actually sits in PlayerRoot-local
        // space, NOT assumed to be the PlayerRoot pivot (0,0,0). The Main-Brick can be offset from
        // the pivot in the scene (e.g. raised to center it on a taller visual), and anchoring here
        // keeps attached cells adjacent to the Main-Brick instead of relative to the pivot - which
        // otherwise dropped every target that many units below the brick, into the floor.
        public Vector3 GridToLocalPosition(Vector2Int gridPosition)
        {
            Vector3 anchor = mainBrick != null ? mainBrick.transform.localPosition : Vector3.zero;
            return anchor + new Vector3(gridPosition.y * gridStepHorizontal, gridPosition.x * gridStepVertical, 0f);
        }

        // World-space vector for one grid cell step in a cardinal direction, honoring the
        // assembly's current rotation/scale. Used by WorldBrick to sweep the target cell at the
        // receiver's own height instead of from the ground the free brick is resting on.
        public Vector3 CellStepWorld(CardinalDirection direction)
        {
            Vector2Int o = Offset(direction);
            return transform.TransformVector(new Vector3(o.y * gridStepHorizontal, o.x * gridStepVertical, 0f));
        }

        // Attaches an already-instantiated, still-unparented brick to the cardinal cell next to
        // receiver. Returns false (and leaves brick untouched) if that cell is already occupied.
        public bool Attach(BrickNode brick, BrickNode receiver, CardinalDirection direction)
        {
            if (brick == null || receiver == null)
            {
                return false;
            }

            Vector2Int targetPosition = receiver.GridPosition + Offset(direction);
            if (_grid.ContainsKey(targetPosition))
            {
                return false;
            }

            // MVP decision (Docs section 1): the whole assembly shares exactly one Rigidbody.
            // The incoming brick's own Rigidbody must be REMOVED, not just made kinematic - a child
            // that keeps a Rigidbody stays its own physics body and keeps colliding with the player's
            // body, which feels like the character being jammed/restricted by the brick it just
            // picked up. Without one, its collider merges into PlayerRoot's compound collider and is
            // carried along passively. Its prior velocity is dropped, not added to the assembly
            // (Docs section 5.4).
            Rigidbody incomingRigidbody = brick.GetComponent<Rigidbody>();
            if (incomingRigidbody != null)
            {
                incomingRigidbody.linearVelocity = Vector3.zero;
                incomingRigidbody.angularVelocity = Vector3.zero;
                Destroy(incomingRigidbody);
            }

            Transform brickTransform = brick.transform;
            brickTransform.SetParent(transform, worldPositionStays: false);
            brickTransform.localRotation = Quaternion.identity;
            PlaceBrickAtCell(brickTransform, brick, receiver, direction);
            SetLayerRecursively(brick.gameObject, mainBrick.gameObject.layer);

            RegisterBrick(brick, targetPosition);
            LinkNeighborsAround(brick);
            RecomputeMassAndCenter();

            OnBrickAttached?.Invoke(brick);
            OnAssemblyChanged?.Invoke();
            return true;
        }

        // Edge-to-edge placement: put the new brick's collider centre exactly one brick-size away
        // from the receiver's collider centre, in the world direction of the touched side. Stepping
        // by the actual collider size (not a fixed grid constant) lands the faces flush for any brick
        // dimensions, as long as all bricks share the same size. Works whether the collider sits on
        // the brick root or a child (e.g. inside the visual). AutoSyncTransforms is off here, so sync
        // before reading bounds.
        private void PlaceBrickAtCell(Transform brickTransform, BrickNode brick, BrickNode receiver, CardinalDirection direction)
        {
            Collider receiverCollider = receiver.GetComponentInChildren<Collider>();
            Collider brickCollider = brick.GetComponentInChildren<Collider>();
            if (receiverCollider == null || brickCollider == null)
            {
                // No collider to align against - fall back to a plain grid transform placement.
                brickTransform.localPosition = GridToLocalPosition(receiver.GridPosition + Offset(direction));
                return;
            }

            Physics.SyncTransforms();

            Vector3 worldDir = (transform.rotation * LocalDirection(direction)).normalized;
            float stepDistance = BrickExtentAlong(receiverCollider, direction);
            Vector3 targetCenter = receiverCollider.bounds.center + worldDir * stepDistance;
            brickTransform.position += targetCenter - brickCollider.bounds.center;
        }

        public static Vector3 LocalDirection(CardinalDirection direction) => direction switch
        {
            CardinalDirection.Up => Vector3.up,
            CardinalDirection.Down => Vector3.down,
            CardinalDirection.Right => Vector3.right,
            CardinalDirection.Left => Vector3.left,
            _ => Vector3.zero,
        };

        // Actual world-space size of the collider box along the given cardinal axis. Uses the box's
        // own local size * scale (aligned with the brick, which shares the assembly's rotation), so
        // it stays correct even when the assembly is rotated - unlike the AABB bounds.size.
        private static float BrickExtentAlong(Collider collider, CardinalDirection direction)
        {
            bool vertical = direction == CardinalDirection.Up || direction == CardinalDirection.Down;
            if (collider is BoxCollider box)
            {
                Vector3 worldSize = Vector3.Scale(box.size, collider.transform.lossyScale);
                return vertical ? worldSize.y : worldSize.x;
            }
            Vector3 aabb = collider.bounds.size;
            return vertical ? aabb.y : aabb.x;
        }

        // Removes brick and anything left disconnected from the Main-Brick as a result (Docs
        // section 6): flood-fills from the Main-Brick over the remaining graph, treats every
        // brick that comes out unreachable as an additional fragment, and detaches the whole
        // batch together before recomputing mass/center once.
        public void Detach(BrickNode brick, DetachReason reason)
        {
            if (brick == null || brick == mainBrick)
            {
                // Docs section 6.3: the Main-Brick can't be detached this way - a hit against it
                // should trigger PlayerDeath (not implemented yet) or be ignored, as here.
                return;
            }

            if (!_connectedBricks.Contains(brick))
            {
                return;
            }

            UnlinkFromNeighbors(brick);
            _grid.Remove(brick.GridPosition);

            HashSet<BrickNode> connected = FloodFillFromMainBrick();

            var detachSet = new List<BrickNode> { brick };
            for (int i = _connectedBricks.Count - 1; i >= 0; i--)
            {
                BrickNode candidate = _connectedBricks[i];
                if (candidate != brick && !connected.Contains(candidate))
                {
                    detachSet.Add(candidate);
                }
            }

            foreach (BrickNode fragment in detachSet)
            {
                _connectedBricks.Remove(fragment);
                _grid.Remove(fragment.GridPosition);
                ReleaseFragment(fragment);
            }

            RecomputeMassAndCenter();

            OnBricksDetached?.Invoke(detachSet);
            OnAssemblyChanged?.Invoke();
        }

        private HashSet<BrickNode> FloodFillFromMainBrick()
        {
            var visited = new HashSet<BrickNode> { mainBrick };
            var queue = new Queue<BrickNode>();
            queue.Enqueue(mainBrick);

            while (queue.Count > 0)
            {
                BrickNode current = queue.Dequeue();
                foreach (BrickNode neighbor in current.Neighbors.Values)
                {
                    if (neighbor != null && visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited;
        }

        private static void UnlinkFromNeighbors(BrickNode brick)
        {
            foreach (KeyValuePair<CardinalDirection, BrickNode> neighborEntry in brick.Neighbors)
            {
                neighborEntry.Value.ClearNeighbor(Opposite(neighborEntry.Key));
            }
            brick.ClearAllNeighbors();
        }

        // Docs section 6.3: restore the fragment's free-floating state, hand it a believable
        // starting velocity from the point it broke off at, and nudge it outward so the break is
        // readable. WorldBrick.OnDetached() (if present) handles the re-attach grace period.
        private void ReleaseFragment(BrickNode fragment)
        {
            Transform fragmentTransform = fragment.transform;
            Vector3 worldCenter = fragmentTransform.position;
            Vector3 pointVelocity = _rigidbody != null ? _rigidbody.GetPointVelocity(worldCenter) : Vector3.zero;

            fragmentTransform.SetParent(null, worldPositionStays: true);

            int worldBrickLayer = LayerMask.NameToLayer("WorldBrick");
            SetLayerRecursively(fragment.gameObject, worldBrickLayer >= 0 ? worldBrickLayer : fragment.gameObject.layer);

            // Attach() removed the brick's Rigidbody so it could merge into the assembly's compound
            // collider, so give it a fresh one to fall free again.
            Rigidbody fragmentRigidbody = fragment.GetComponent<Rigidbody>();
            if (fragmentRigidbody == null)
            {
                fragmentRigidbody = fragment.gameObject.AddComponent<Rigidbody>();
            }

            fragmentRigidbody.isKinematic = false;
            fragmentRigidbody.useGravity = true;
            fragmentRigidbody.linearVelocity = pointVelocity;
            fragmentRigidbody.angularVelocity = Vector3.zero;

            Vector3 outward = worldCenter - transform.position;
            outward = outward.sqrMagnitude > 0.0001f ? outward.normalized : Vector3.up;
            fragmentRigidbody.AddForce(outward * outwardImpulseStrength, ForceMode.Impulse);

            fragment.GetComponent<WorldBrick>()?.OnDetached();
        }

        private void RegisterBrick(BrickNode brick, Vector2Int gridPosition)
        {
            brick.AssignId(_nextBrickId++);
            brick.GridPosition = gridPosition;
            _grid[gridPosition] = brick;
            _connectedBricks.Add(brick);
        }

        private void LinkNeighborsAround(BrickNode brick)
        {
            foreach (CardinalDirection direction in AllDirections)
            {
                Vector2Int neighborPosition = brick.GridPosition + Offset(direction);
                if (_grid.TryGetValue(neighborPosition, out BrickNode neighbor))
                {
                    brick.SetNeighbor(direction, neighbor);
                    neighbor.SetNeighbor(Opposite(direction), brick);
                }
            }
        }

        private void RecomputeMassAndCenter()
        {
            float totalMass = 0f;
            Vector3 weightedSum = Vector3.zero;

            foreach (BrickNode brick in _connectedBricks)
            {
                float weight = brick.Weight;
                totalMass += weight;
                weightedSum += brick.transform.localPosition * weight;
            }

            TotalMass = totalMass;
            LocalCenterOfMass = totalMass > 0f ? weightedSum / totalMass : Vector3.zero;

            if (_rigidbody != null && totalMass > 0f)
            {
                // Unity's automatic center-of-mass is a geometric average weighted by collider
                // volume, not our gameplay Weight per brick - left on, it would silently overwrite
                // this every time mass/colliders change and cancel the intended Right-Heavy tilt.
                _rigidbody.automaticCenterOfMass = false;
                _rigidbody.mass = totalMass;
                _rigidbody.centerOfMass = LocalCenterOfMass;
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        [ContextMenu("Debug/Attach Test Brick Right")]
        private void DebugAttachTestBrickRight() => DebugAttachTestBrick(CardinalDirection.Right);

        [ContextMenu("Debug/Attach Test Brick Up")]
        private void DebugAttachTestBrickUp() => DebugAttachTestBrick(CardinalDirection.Up);

        private void DebugAttachTestBrick(CardinalDirection direction)
        {
            if (testBrickPrefab == null || mainBrick == null)
            {
                Debug.LogWarning("[PlayerAssembly] Assign a Test Brick prefab before using the debug attach.", this);
                return;
            }

            BrickNode instance = Instantiate(testBrickPrefab);
            if (!Attach(instance, mainBrick, direction))
            {
                Debug.LogWarning($"[PlayerAssembly] Debug attach in direction {direction} failed - cell occupied.", this);
                Destroy(instance.gameObject);
            }
        }

        [ContextMenu("Debug/Detach Brick Right Of Main")]
        private void DebugDetachBrickRight()
        {
            if (_grid.TryGetValue(mainBrick.GridPosition + Offset(CardinalDirection.Right), out BrickNode brick))
            {
                Detach(brick, DetachReason.Manual);
            }
            else
            {
                Debug.LogWarning("[PlayerAssembly] No brick attached to the right of Main-Brick to detach.", this);
            }
        }
    }
}
