using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    public readonly struct GridGroup
    {
        private readonly Vector2Int _index;
        public readonly IEnumerable<GameObject> GameObjects;

        public int? Row => _index.x < 0 ? null : _index.y;
        public int? Column => _index.y < 0 ? null : _index.x;
        
        public GridGroup(Vector2Int index, IEnumerable<GameObject> gameObjects)
        {
            _index = index;
            GameObjects = gameObjects;
        }
    }
    
    public class Grid
    {
        private readonly ITetrisGameConfig _config;
        private readonly GameObject[, ] _grid;

        public IEnumerable<GameObject> GridObjects => _grid.ToEnumerable<GameObject>();

        public IEnumerable<GameObject> GetObjectsInRow(int row) =>
            Enumerable
                .Range(0, _grid.GetLength(0))
                .Select(column => _grid[column, row]);
        
        public IEnumerable<GridGroup> GridObjectsByColumn => _grid.ToEnumerable<GameObject>()
            .GroupBy(item => item.GetComponent<StudDataComponent>().Offset.x)
            .Select(group => new GridGroup(new Vector2Int(group.Key, -1), group));
        
        public IEnumerable<GridGroup> GridObjectsByRow => _grid.ToEnumerable<GameObject>()
            .GroupBy(item => item.GetComponent<StudDataComponent>().Offset.y)
            .Select(group => new GridGroup(new Vector2Int(-1, group.Key), group));
        
        public Grid(ITetrisGameConfig config)
        {
            _config = config;
            _grid = new GameObject[_config.GridSizeInStuds.x, _config.GridSizeInStuds.y];
        }

        public void Clear(List<ConnectedGroup> connectedGroups)
        {
            foreach (ConnectedGroup connectedGroup in connectedGroups)
            {
                if (connectedGroup.StudColor != StudColor.Damage)
                {
                    foreach (var gridPosition in connectedGroup.GridPositions)
                    {
                        if (_grid[gridPosition.x, gridPosition.y])
                        {
                            Debug.Log($"Destory grid {gridPosition.x}, {gridPosition.y}");
                            UnityEngine.Object.Destroy(_grid[gridPosition.x, gridPosition.y]);
                            _grid[gridPosition.x, gridPosition.y] = null;
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            for (int x = 0; x < _grid.GetLength(0); ++x)
            {
                for (int y = 0; y < _grid.GetLength(1); ++y)
                {
                    if (_grid[x, y])
                    {
                        Debug.Log($"Reset grid {x}, {y}");
                        UnityEngine.Object.Destroy(_grid[x, y]);
                        _grid[x, y] = null;
                    }
                }
            }
        }

        public bool AddIntoGrid(GameObject gameObject, out Vector2Int gridPosition)
        {
            Vector2Int gridSize = GetGridSize(_config, GridType.PiecesOnly);

            Vector3 offset = gameObject.transform.position;// - tetrisGameBehaviour.transform.position;
            gridPosition = WorldToGridRounded(offset.XZ(), gridSize);

            if (gridPosition.y >= gridSize.y)
            {
                return false;
            }

            Debug.Assert(GetGridObject(gridPosition) == null);
            _grid[gridPosition.x, gridPosition.y] = gameObject;

            gameObject.transform.parent = null;

            gameObject.GetComponent<StudDataComponent>().Offset = gridPosition;
            gameObject.name = $"{gridPosition.x}, {gridPosition.y}";
            
            return true;
        }

        public GameObject GetGridObject(Vector2Int gridPosition)
        {
            return GetGridObject(gridPosition.x, gridPosition.y);
        }

        public GameObject GetGridObject(int x, int y)
        {
            if (x < 0 || x >= _grid.GetLength(0) || y < 0 || y >= _grid.GetLength(1))
            {
                return null;
            }

            return _grid[x, y];
        }

        public IEnumerable<SandFallPhysicsComponent> CreateUnsupportedStudsSandFallPhysicsComponents()
        {
            Vector2Int gridSize = GetGridSize(_config, GridType.PiecesOnly);
            
            for (int x = 0; x < _grid.GetLength(0); ++x)
            {
                // we can start with y = 1 because the bottom row will never start falling
                int? startY = null;
                for (int y = 1; y < _grid.GetLength(1); ++y)
                {
                    if (_grid[x, y] && !_grid[x, y - 1] && !startY.HasValue)
                    {
                        startY = y;
                    }
                    else if (!_grid[x, y] && startY.HasValue)
                    {
                        Debug.Log($"creating unsupported piece column {x} from rows {startY.Value}..{y - 1}");
                        
                        // yield an item for this connected part of the column
                        // there may be multiple items per column in case different non-neighbour rows are completed
                        SandFallPhysicsComponent sandFallPhysicsComponent = SandFallPhysicsComponent.Spawn("UnsupportedPiece");
                        sandFallPhysicsComponent.transform.position = GridToWorld(x, gridSize.y, gridSize).XZ();
                        for (int y2 = startY.Value; y2 < y; ++y2)
                        {
                            _grid[x, y2].transform.parent = sandFallPhysicsComponent.transform;
                            sandFallPhysicsComponent.AddExistingStud(_grid[x, y2], Vector2Int.up * (y2 - gridSize.y));
                            _grid[x, y2] = null;
                        }

                        sandFallPhysicsComponent.name += $" {x} {sandFallPhysicsComponent.transform.childCount}";
                        yield return sandFallPhysicsComponent;
                        
                        // reset startY for the next run (in case there is one)
                        startY = null;
                    }
                }
            }
        }

        public void Dump()
        {
            IEnumerable<string> RowParts(int row)
            {
                for (int x = 0; x < _grid.GetLength(0); ++x)
                {
                    yield return _grid[x, row] ? _grid[x, row].GetComponent<StudDataComponent>().GetColor(_config).ToString() : "null";
                }
            }

            IEnumerable<string> Rows()
            {
                for (int y = _grid.GetLength(1) - 1; y >= 0; --y)
                {
                    yield return $"{y}: {string.Join(',', RowParts(y))}";
                }
            }

            Debug.Log(string.Join('\n', Rows()));
        }
        
        public enum GridType
        {
            IncludingBorder,
            // the grid only containing the actual tetris pieces (exactly TetrisGameConfig.gridSizeInStuds)
            PiecesOnly
        }
        
        public static Vector2 GetGridBounds(ITetrisGameConfig config, GridType gridType)
        {
            return GetGridBounds(GetGridSize(config, gridType));
        }
        
        public static Vector2 GetGridBounds(Vector2Int gridSizeInt)
        {
            Vector2 gridSize = gridSizeInt;
            gridSize.Scale(BrickUtils.UnityStudSize);
            return gridSize;
        }

        public static Vector2Int GetGridSize(ITetrisGameConfig config, GridType gridType)
        {
            return gridType switch
            {
                GridType.IncludingBorder => config.GridSizeInStuds + Vector2Int.one * 2,
                GridType.PiecesOnly => config.GridSizeInStuds,
                _ => throw new ArgumentOutOfRangeException(nameof(gridType), gridType, null)
            };
        }

        /// <summary>
        /// inverse of WorldToGrid
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="gridSize">should either be TetrisGameConfig.gridSizeInStuds or TetrisGameConfig.gridSizeInStuds + (2, 2)</param>
        /// <returns></returns>
        public static Vector2 GridToWorld(int x, int y, Vector2Int gridSize)
        {
            return GridToWorld(new Vector2Int(x, y), gridSize);
        }

        /// <summary>
        /// inverse of WorldToGrid
        /// </summary>
        /// <param name="gridPos"></param>
        /// <param name="gridSize">should either be TetrisGameConfig.gridSizeInStuds or TetrisGameConfig.gridSizeInStuds + (2, 2)</param>
        /// <returns></returns>
        public static Vector2 GridToWorld(Vector2Int gridPos, Vector2Int gridSize)
        {
            Vector2 gridBounds = GetGridBounds(gridSize);
            Vector2 halfBounds = gridBounds * 0.5f;
            
            Vector2Int gridSizeMinusOne = gridSize - Vector2Int.one;
            Vector2 position = gridPos;
            position = position.InverseScale(gridSizeMinusOne);
            position -= VectorExtensions.Half;
            position.Scale(BrickUtils.UnityStudSize);
            position.Scale(gridSizeMinusOne);
            return position;
        }

        /// <summary>
        /// inverse of GridToWorld
        /// rounds to the nearest integer
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static Vector2Int WorldToGridRounded(Vector2 position, Vector2Int gridSize)
        {
            return WorldToGrid(position, gridSize).RoundToInt();
        }

        /// <summary>
        /// inverse of GridToWorld
        /// ceils and floors the vector2 and returns both if they are different or just one if they are equal
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        public static Vector2Int[] WorldToGridCeiledFloored(Vector2 position, Vector2Int gridSize)
        {
            Vector2 gridPosition = WorldToGrid(position, gridSize);
            Vector2Int floored = gridPosition.FloorToInt();
            Vector2Int ceiled = gridPosition.CeilToInt();
            Debug.Assert(floored.x == ceiled.x);
            return floored != ceiled ? new Vector2Int[] { ceiled, floored } : new Vector2Int[] { floored };
        }

        /// <summary>
        /// inverse of GridToWorld
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        private static Vector2 WorldToGrid(Vector2 position, Vector2Int gridSize)
        {
            Vector2Int gridSizeMinusOne = gridSize - Vector2Int.one;
            position = position.InverseScale(gridSizeMinusOne);
            position = position.InverseScale(BrickUtils.UnityStudSize);
            position += VectorExtensions.Half;
            position.Scale(gridSizeMinusOne);
            return position;
        }
    }
}
