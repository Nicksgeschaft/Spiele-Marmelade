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
        // Actual attach placement measures the bricks' colliders (see PlaceBrickAtCell); these two are
        // the nominal cell size, used for the grid fallback and the target-cell geometry sweep.
        [Tooltip("Nominal cell size along the jump axis (world Y).")]
        [SerializeField] private float gridStepVertical = 0.96f;
        [Tooltip("Nominal cell size along the horizontal axis (world X).")]
        [SerializeField] private float gridStepHorizontal = 0.795f;

        [Tooltip("How far a brick's studs sink into the brick above it when stacking. At this project's " +
                 "x10 brick scale the mesh is 1.14 tall but only 0.96 of that is structural, so the studs " +
                 "are the remaining 0.18. Raise this if studs still peek out, lower it if bricks overlap too much.")]
        [SerializeField] private float studOverlap = 0.18f;

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

        // Places the brick one cell from the receiver: horizontally by a full brick width, vertically
        // by the brick's structural height so studs sink into the brick above. AutoSyncTransforms is
        // off here, so sync before reading bounds.
        private void PlaceBrickAtCell(Transform brickTransform, BrickNode brick, BrickNode receiver, CardinalDirection direction)
        {
            Collider receiverCollider = ResolveBrickCollider(receiver);
            Collider brickCollider = ResolveBrickCollider(brick);
            if (receiverCollider == null || brickCollider == null)
            {
                // No collider to align against - fall back to a plain grid transform placement.
                brickTransform.localPosition = GridToLocalPosition(receiver.GridPosition + Offset(direction));
                return;
            }

            Physics.SyncTransforms();

            // Everything below is computed in assembly-local space. World-space AABB bounds would be
            // wrong the moment the assembly tips over (the AABB grows and its "bottom" stops being the
            // brick's bottom face), which showed up as bricks sliding out of line mid-air.
            Vector3 receiverCenter = transform.InverseTransformPoint(receiverCollider.bounds.center);
            Vector3 brickCenter = transform.InverseTransformPoint(brickCollider.bounds.center);
            Vector3 receiverHalf = LocalHalfExtents(receiverCollider);
            Vector3 brickHalf = LocalHalfExtents(brickCollider);

            // Steps are derived from the two bricks' own half sizes rather than a fixed constant, so
            // they stay correct even when the main brick's collider differs in size from a world
            // brick's (which is what left a visible stud gap under the main brick before).
            //   horizontal: halfA + halfB          -> side faces exactly flush
            //   vertical:   halfA + halfB - stud   -> the lower brick's studs sink into the one above
            Vector3 targetCenter = receiverCenter;
            switch (direction)
            {
                case CardinalDirection.Right:
                case CardinalDirection.Left:
                    float horizontalStep = receiverHalf.x + brickHalf.x;
                    targetCenter.x += direction == CardinalDirection.Right ? horizontalStep : -horizontalStep;
                    // Line the bottom faces up so both bricks stand on the same level.
                    targetCenter.y = receiverCenter.y - receiverHalf.y + brickHalf.y;
                    break;

                case CardinalDirection.Up:
                    targetCenter.y += receiverHalf.y + brickHalf.y - studOverlap;
                    break;

                case CardinalDirection.Down:
                    targetCenter.y -= receiverHalf.y + brickHalf.y - studOverlap;
                    break;
            }
            targetCenter.z = receiverCenter.z;

            // Move the transform by however far its collider centre currently sits from the target.
            Vector3 colliderOffset = brickCenter - brickTransform.localPosition;
            brickTransform.localPosition = targetCenter - colliderOffset;
        }

        // Half size of the collider box along the brick's own axes. Since every brick is a direct
        // child of the assembly with identity local rotation, these axes are the assembly's axes -
        // so unlike bounds.extents this stays correct while the assembly is rotated.
        private static Vector3 LocalHalfExtents(Collider collider)
        {
            if (collider is BoxCollider box)
            {
                return Vector3.Scale(box.size, collider.transform.lossyScale) * 0.5f;
            }
            return collider.bounds.extents;
        }

        // A rebuilt brick can carry several colliders (e.g. a sizing "physic brick" inside the visual
        // plus a nested brick prefab). Prefer the one on the BrickNode's own GameObject so the choice
        // is predictable, and only fall back to searching children.
        private static Collider ResolveBrickCollider(BrickNode brick)
        {
            Collider own = brick.GetComponent<Collider>();
            return own != null ? own : brick.GetComponentInChildren<Collider>();
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
