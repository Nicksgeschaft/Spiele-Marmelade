using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Owns the brick grid/graph for one player: registers the Main-Brick at (0,0), attaches new
    // bricks at cardinal offsets, and keeps total mass / local center of mass (and the Rigidbody
    // that reads them) in sync. Collision-triggered attachment lives in a later step - for now
    // Attach() is called directly (see the Debug context menu below) to validate the grid/mass math.
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

        public event Action<BrickNode> OnBrickAttached;
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

        public Vector3 GridToLocalPosition(Vector2Int gridPosition) =>
            new(gridPosition.y * gridStepHorizontal, gridPosition.x * gridStepVertical, 0f);

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

            Transform brickTransform = brick.transform;
            brickTransform.SetParent(transform, worldPositionStays: false);
            brickTransform.localPosition = GridToLocalPosition(targetPosition);
            brickTransform.localRotation = Quaternion.identity;
            SetLayerRecursively(brick.gameObject, mainBrick.gameObject.layer);

            RegisterBrick(brick, targetPosition);
            LinkNeighborsAround(brick);
            RecomputeMassAndCenter();

            OnBrickAttached?.Invoke(brick);
            OnAssemblyChanged?.Invoke();
            return true;
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
    }
}
