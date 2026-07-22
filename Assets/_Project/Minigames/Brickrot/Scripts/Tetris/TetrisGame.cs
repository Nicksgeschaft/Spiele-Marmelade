using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using SpieleMarmelade.Minigames.Brickrot.Survivor; // ✅ statt Assets.Scripts.Survivor

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    public readonly struct ConnectedGroup
    {
        private readonly HashSet<Vector2Int> _gridPositions;
        private readonly HashSet<int> _rows;
        private readonly HashSet<int> _columns;

        public StudColor? StudColor { get; }

        public HashSet<Vector2Int> GridPositions => _gridPositions;

        public ConnectedGroup(StudColor? studColor)
        {
            StudColor = studColor;
            _gridPositions = new HashSet<Vector2Int>();
            _rows = new HashSet<int>();
            _columns = new HashSet<int>();
        }

        public void Add(Vector2Int gridPosition)
        {
            _gridPositions.Add(gridPosition);
            _rows.Add(gridPosition.y);
            _columns.Add(gridPosition.x);
        }

        public bool ConnectsLeftRight(ITetrisGameConfig config)
        {
            Vector2Int gridSize = Grid.GetGridSize(config, Grid.GridType.PiecesOnly);
            return _columns.Contains(0) && _columns.Contains(gridSize.x - 1);
        }

        public bool ConnectsTopBottom(ITetrisGameConfig config)
        {
            Vector2Int gridSize = Grid.GetGridSize(config, Grid.GridType.PiecesOnly);
            return _rows.Contains(0) && _rows.Contains(gridSize.y - 1);
        }
    }

    public class TetrisGame : IDisposable
    {
        private readonly ITetrisGameConfig _config;

        private readonly Grid _grid;
        private readonly TetrisPieceGenerator _generator;

        private SandFallPhysicsComponent _currentControlledPiece;
        private readonly List<SandFallPhysicsComponent> _currentFallingPieces = new List<SandFallPhysicsComponent>();
        private float _moveInput;
        private double? _lastMoveTime;
        private uint _pendingDamage;

        private static readonly Vector2Int[] GridNeighbourOffsets =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
        };

        public Grid Grid => _grid;

        public TetrisGame(ITetrisGameConfig config)
        {
            _config = config;

            _grid = new Grid(_config);
            _generator = new TetrisPieceGenerator(_config);

            _config.SurvivorEvents.OnTakeDamage += SurvivorEventsOnOnTakeDamage;
        }

        public void Dispose()
        {
            _config.SurvivorEvents.OnTakeDamage -= SurvivorEventsOnOnTakeDamage;
            Reset();
        }

        public void Update()
        {
            Vector2Int gridSize = Grid.GetGridSize(_config, Grid.GridType.PiecesOnly);

            if (!_currentControlledPiece && !HasStudInTopRow(gridSize))
            {
                _currentControlledPiece = _generator.SpawnPiece();
                _currentFallingPieces.Add(_currentControlledPiece);

                StudColor? studColor = _currentControlledPiece.GetComponentInChildren<StudDataComponent>().GetColor(_config);
                if (studColor.HasValue)
                {
                    // One brick, one announcement. TetrisToSurvivorBridge turns this into exactly
                    // one ability — see ITetrisGameConfig for why nothing is triggered here.
                    _config.TetrisEvents.InvokeBrickSpawned(studColor.Value);
                }
            }
            else
            {
                HandlePieceMovement(gridSize);

                Dictionary<StudColor?, List<FallResult>> relevantGridChanges = HandleFalling(gridSize);
                if (relevantGridChanges == null)
                {
                    _config.TetrisEvents.InvokeGameOver();
                    return; 
                }

#if UNITY_EDITOR
                if (relevantGridChanges.Count > 0)
                {
                    _grid.Dump();
                }
#endif


                Dictionary<StudColor?, List<ConnectedGroup>> groups = ComputeConnectedGroups(gridSize, relevantGridChanges);
                foreach (var colorGroup in groups)
                {
                    if (colorGroup.Key.HasValue)
                    {
                        int numberOfGroups = colorGroup.Value.Count;
                        _config.TetrisEvents.InvokeLevelUpSkill(colorGroup.Key.Value, numberOfGroups);
                    }

                    _grid.Clear(colorGroup.Value);
                }

                _currentFallingPieces.AddRange(_grid.CreateUnsupportedStudsSandFallPhysicsComponents());

                if (_pendingDamage > 0)
                {
                    int damage = (int)Math.Min(int.MaxValue, _pendingDamage);
                    _currentFallingPieces.AddRange(_generator.SpawnDamage(ref damage, IsOccupied));
                    _pendingDamage = (uint)Math.Max(0, damage);
                }
            }
        }

        /// <summary>Horizontal piece input, -1..1. Pushed in every frame by TetrisGameBehaviour.</summary>
        public void SetMoveInput(float value)
        {
            _moveInput = value;
        }

        private void SurvivorEventsOnOnTakeDamage(int damage)
        {
            _pendingDamage += (uint)Math.Max(0, damage);
        }

        private bool HasStudInTopRow(Vector2Int gridSize)
        {
            return Enumerable
                .Range(0, gridSize.x)
                .Any(x => IsOccupied(new List<Vector2Int[]>() { new Vector2Int[] { new Vector2Int(x, gridSize.y) } }, Vector2Int.zero, gridSize, null));
        }

        private void HandlePieceMovement(Vector2Int gridSize)
        {
            if (_moveInput != 0.0f)
            {
                float timeToMoveStep = BrickUtils.UnityStudSize.x / _config.MoveSpeed;
                if (!_lastMoveTime.HasValue || Time.timeAsDouble - _lastMoveTime.Value >= timeToMoveStep)
                {
                    using var pooledObjectLeft = ListPool<Vector2Int[]>.Get(out List<Vector2Int[]> left);
                    using var pooledObjectRight = ListPool<Vector2Int[]>.Get(out List<Vector2Int[]> right);
                    foreach (Stud stud in _currentControlledPiece.Studs)
                    {
                        Vector2Int[] gridPositions = Grid.WorldToGridCeiledFloored(stud.GameObject.transform.position.XZ(), gridSize);

                        if (left.Count == 0 || gridPositions[0].x <= left[0][0].x)
                        {
                            if (left.Count > 0 && gridPositions[0].x < left[0][0].x)
                                left.Clear();

                            left.Add(gridPositions);
                        }

                        if (right.Count == 0 || gridPositions[0].x >= right[0][0].x)
                        {
                            if (right.Count > 0 && gridPositions[0].x > right[0][0].x)
                                right.Clear();

                            right.Add(gridPositions);
                        }
                    }

                    bool canMove = (_moveInput < 0.0f && !IsOccupied(left, Vector2Int.left, gridSize, _currentControlledPiece))
                                   || (_moveInput > 0.0f && !IsOccupied(right, Vector2Int.right, gridSize, _currentControlledPiece));

                    Vector2Int? pieceSize = _currentControlledPiece.SizeX;
                    if (pieceSize.HasValue && canMove)
                    {
                        Vector2Int allowedGridPositionsX = new Vector2Int(0, gridSize.x - 1) - pieceSize!.Value;
                        float minimumX = Grid.GridToWorld(allowedGridPositionsX.x, 0, gridSize).x;
                        float maximumX = Grid.GridToWorld(allowedGridPositionsX.y, 0, gridSize).x;
                        float moveDelta = _moveInput * BrickUtils.UnityStudSize.x;
                        Vector3 position = _currentControlledPiece.transform.position;
                        position.x = Mathf.Clamp(position.x + moveDelta, minimumX, maximumX);
                        _currentControlledPiece.transform.position = position;

                        _lastMoveTime = Time.timeAsDouble;
                    }
                }
            }
        }

        private bool IsOccupied(List<Vector2Int[]> gridPositionsList, Vector2Int offset, Vector2Int gridSize, SandFallPhysicsComponent ignored)
        {
            foreach (Vector2Int[] gridPositions in gridPositionsList)
            {
                foreach (Vector2Int gridPosition in gridPositions)
                {
                    if (_grid.GetGridObject(gridPosition + offset))
                        return true;
                }

                foreach (SandFallPhysicsComponent fallingPiece in _currentFallingPieces)
                {
                    if (fallingPiece != ignored)
                    {
                        foreach (Stud stud in fallingPiece.Studs)
                        {
                            Vector2Int[] otherGridPositions = Grid.WorldToGridCeiledFloored(stud.GameObject.transform.position.XZ(), gridSize);
                            if (gridPositions.Select(x => x + offset).Intersect(otherGridPositions).Any())
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private Dictionary<StudColor?, List<FallResult>> HandleFalling(Vector2Int gridSize)
        {
            Dictionary<StudColor?, List<FallResult>> relevantGridChanges = new Dictionary<StudColor?, List<FallResult>>();

            _currentFallingPieces.Sort((a, b) => SortSandFallPhysicsComponents(a, b, gridSize));

            for (int i = _currentFallingPieces.Count - 1; i >= 0; i--)
            {
                SandFallPhysicsComponent fallingPiece = _currentFallingPieces[i];
                FallResult result = fallingPiece.Fall(_config, _grid);
                if (result.HasLost)
                {
                    Debug.Assert(fallingPiece == _currentControlledPiece);
                    return null;
                }

                if (result.HitGrid)
                {
                    bool wasControlled = fallingPiece == _currentControlledPiece;

                    if (wasControlled)
                    {
                        _currentControlledPiece = null;
                        _lastMoveTime = null;
                    }

                    StudColor? color = _grid
                        .GetGridObject(result.GridPositions.First())
                        .GetComponent<StudDataComponent>().GetColor(_config);

                    if (color.HasValue)
                    {
                        // ❌ Entfernt: kein Trigger mehr beim Landen (du willst Spawn-only)
                        // if (wasControlled) TriggerAbilityForColor(color.Value);

                        if (!relevantGridChanges.TryGetValue(color, out List<FallResult> fallResults))
                            relevantGridChanges.Add(color, new List<FallResult>() { result });
                        else
                            fallResults.Add(result);
                    }
                }

                if (fallingPiece.IsEmpty)
                {
                    UnityEngine.Object.Destroy(fallingPiece.gameObject);
                    _currentFallingPieces.RemoveAtSwapBack(i);
                }
            }

            return relevantGridChanges;
        }

        private int SortSandFallPhysicsComponents(SandFallPhysicsComponent a, SandFallPhysicsComponent b, Vector2Int gridSize)
        {
            (int row, float position) lowestA = CalculateLowestRow(a, gridSize);
            (int row, float position) lowestB = CalculateLowestRow(b, gridSize);
            int compareRow = lowestA.row.CompareTo(lowestB.row);
            if (compareRow != 0) return compareRow;
            return lowestA.position.CompareTo(lowestB.position);
        }

        private (int row, float position) CalculateLowestRow(SandFallPhysicsComponent sandFallPhysicsComponent, Vector2Int gridSize)
        {
            Vector2 position = sandFallPhysicsComponent.StudsPerColumnFromBottomToTop.First().Studs.First().GameObject.transform.position.XZ();
            Vector2Int[] gridPositions = Grid.WorldToGridCeiledFloored(position, gridSize);
            return (gridPositions[^1].y, position.y);
        }

        private static Func<GameObject, bool> MakePredicate(ITetrisGameConfig config, StudColor? relevantColor)
        {
            switch (relevantColor)
            {
                case StudColor.Damage:
                    return _ => false;
                case StudColor.White:
                    {
                        bool Predicate(GameObject x)
                        {
                            if (!x) return false;
                            StudColor? studColor = x.GetComponent<StudDataComponent>().GetColor(config);
                            return studColor == StudColor.Damage || studColor == StudColor.White;
                        }
                        return Predicate;
                    }
                default:
                    {
                        bool Predicate(GameObject x) => x && x.GetComponent<StudDataComponent>().GetColor(config) == relevantColor;
                        return Predicate;
                    }
            }
        }
        private Dictionary<StudColor?, List<ConnectedGroup>> ComputeConnectedGroups(
    Vector2Int gridSize,
    Dictionary<StudColor?, List<FallResult>> relevantGridChanges)
        {
            Dictionary<StudColor?, List<ConnectedGroup>> result = new Dictionary<StudColor?, List<ConnectedGroup>>();

            foreach (var relevantGridChange in relevantGridChanges)
            {
                List<ConnectedGroup> connectedGroups = new List<ConnectedGroup>();

                Func<GameObject, bool> predicate = MakePredicate(_config, relevantGridChange.Key);
                for (int i = 0; i < relevantGridChange.Value.Count; ++i)
                {
                    FallResult fallResult = relevantGridChange.Value[i];

                    ConnectedGroup connectedGroup = new ConnectedGroup(relevantGridChange.Key);
                    using var pooledObject3 = HashSetPool<Vector2Int>.Get(out HashSet<Vector2Int> visitedGridPositions);
                    using var pooledObject4 = HashSetPool<Vector2Int>.Get(out HashSet<Vector2Int> openGridPositions);

                    foreach (var gridPosition in fallResult.GridPositions)
                    {
                        openGridPositions.Remove(gridPosition);
                        visitedGridPositions.Add(gridPosition);

                        connectedGroup.Add(gridPosition);

                        RemoveMerged(relevantGridChange.Value, i, connectedGroup.GridPositions);
                        AddNeighbours(gridPosition, visitedGridPositions, openGridPositions);
                    }

                    while (openGridPositions.Count > 0)
                    {
                        Vector2Int gridPosition = openGridPositions.First();
                        openGridPositions.Remove(gridPosition);
                        visitedGridPositions.Add(gridPosition);

                        if (predicate(_grid.GetGridObject(gridPosition)))
                        {
                            connectedGroup.Add(gridPosition);

                            RemoveMerged(relevantGridChange.Value, i, connectedGroup.GridPositions);
                            AddNeighbours(gridPosition, visitedGridPositions, openGridPositions);
                        }
                    }

                    if (connectedGroup.ConnectsLeftRight(_config) || connectedGroup.ConnectsTopBottom(_config))
                    {
                        connectedGroups.Add(connectedGroup);
                    }
                }

                if (connectedGroups.Count > 0)
                {
                    Debug.Log($"{relevantGridChange.Key} found {connectedGroups.Count} connected groups");
                    result.Add(relevantGridChange.Key, connectedGroups);
                }
            }

            return result;
        }

        private void RemoveMerged(List<FallResult> fallResults, int i, HashSet<Vector2Int> currentGroup)
        {
            for (int j = fallResults.Count - 1; j > i; --j)
            {
                FallResult otherFallResult = fallResults[j];
                if (currentGroup.Overlaps(otherFallResult.GridPositions))
                {
                    fallResults.RemoveAtSwapBack(j);
                }
            }
        }

        private void AddNeighbours(Vector2Int gridPosition, HashSet<Vector2Int> visitedGridPositions, HashSet<Vector2Int> openGridPositions)
        {
            for (int i = 0; i < 4; ++i)
            {
                Vector2Int neighbourGridPosition = gridPosition + GridNeighbourOffsets[i];
                if (!visitedGridPositions.Contains(neighbourGridPosition))
                {
                    openGridPositions.Add(neighbourGridPosition);
                }
            }
        }
        private void Reset()
        {
            _grid.Reset();

            if (_currentControlledPiece)
            {
                UnityEngine.Object.Destroy(_currentControlledPiece.gameObject);
                _currentControlledPiece = null;
            }

            foreach (SandFallPhysicsComponent sandFallPhysicsComponent in _currentFallingPieces)
            {
                if (sandFallPhysicsComponent)
                {
                    UnityEngine.Object.Destroy(sandFallPhysicsComponent.gameObject);
                }
            }
            _currentFallingPieces.Clear();

            _moveInput = 0.0f;
            _lastMoveTime = null;

            _pendingDamage = 0;
        }



        // ... Rest unverändert (ComputeConnectedGroups, Reset, etc.)
        // (lass den Rest deines Codes 그대로)
    }
}
