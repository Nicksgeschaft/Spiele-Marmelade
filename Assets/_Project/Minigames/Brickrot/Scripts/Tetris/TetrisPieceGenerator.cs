using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    public class TetrisPieceGenerator
    {
        private readonly ITetrisGameConfig _config;

        private readonly System.Random _random = new System.Random();
        private readonly Action<GameObject, SandFallPhysicsComponent>[] _pieceShapeGenerators;

        public TetrisPieceGenerator(ITetrisGameConfig config)
        {
            _config = config;

            _pieceShapeGenerators = new Action<GameObject, SandFallPhysicsComponent>[]
            {
                GenerateI,
                GenerateL,
                GenerateS,
                GenerateT,
                GenerateO
            };
        }
        
        /// <summary>
        /// spawn a brick when there has never been a brick previously or when the current piece hits something on its way down
        /// </summary>
        public SandFallPhysicsComponent SpawnPiece()
        {
            SandFallPhysicsComponent sandFallPhysicsComponent = SandFallPhysicsComponent.Spawn("Piece");
            
            Vector2Int gridSize = Grid.GetGridSize(_config, Grid.GridType.PiecesOnly);
            // do not spawn randomly which could cause the piece to overlap the borders 
            // int startX = gridSize.x * Random.Range(0, gridSize.x);
            // for even gridsizes randomly spawn on the left or right position closest to the center
            // for odd gridsizes always spawn exactly on the center position
            int startX = gridSize.x % 2 == 0 ? (gridSize.x + RandomUtils.ZeroOrOne()) / 2 : (gridSize.x + 1) / 2;
            sandFallPhysicsComponent.transform.position = Grid.GridToWorld(startX, gridSize.y, gridSize).XZ();

            Action<GameObject, SandFallPhysicsComponent> pieceShapeGenerator = _pieceShapeGenerators.RandomElement(_random);
            GameObject forcedPrefab = _config.ForcedPrefab;
            GameObject randomPrefab = forcedPrefab ? forcedPrefab : _config.PiecePrefabs.RandomElement(_random);
            pieceShapeGenerator(randomPrefab, sandFallPhysicsComponent);

            return sandFallPhysicsComponent;
        }

        public IEnumerable<SandFallPhysicsComponent> SpawnDamage(ref int damage, Func<List<Vector2Int[]>, Vector2Int, Vector2Int, SandFallPhysicsComponent, bool> isOccupied)
        {
            Vector2Int gridSize = Grid.GetGridSize(_config, Grid.GridType.PiecesOnly);

            using var pooledObject = ListPool<int>.Get(out List<int> availableColumns);
            availableColumns.AddRange(
                Enumerable
                    .Range(0, gridSize.x)
                    .Where(x => !isOccupied(new List<Vector2Int[]>() { new Vector2Int[] { new Vector2Int(x, gridSize.y) } }, Vector2Int.zero, gridSize, null))
            );

            if (availableColumns.Count == 0)
            {
                return Array.Empty<SandFallPhysicsComponent>();
            }

            using var pooledObject2 = DictionaryPool<int, SandFallPhysicsComponent>.Get(out Dictionary<int, SandFallPhysicsComponent> components);

            for (int i = 0; i < damage; ++i)
            {
                int column = availableColumns[Random.Range(0, availableColumns.Count)];
                if (!components.TryGetValue(column, out SandFallPhysicsComponent component))
                {
                    component = SandFallPhysicsComponent.Spawn("Damage");
                    component.transform.position = Grid.GridToWorld(column, gridSize.y, gridSize).XZ();
                    
                    components.Add(column, component);
                }
                
                // studs stack on top of each other
                component.AddStud(_config.DamagePrefabs.RandomElement(_random), Vector2Int.up * component.Studs.Count());
            }

            damage = 0;
            return components.Values.ToArray();
        }

        private static void GenerateI(GameObject prefab, SandFallPhysicsComponent sandFallPhysicsComponent)
        {
            int direction = RandomUtils.ZeroOrOne();
            sandFallPhysicsComponent.name = direction == 0 ? "|" : "-";
            Vector2Int offset = new Vector2Int(direction, 1 - direction);

            for (int i = 0; i < 4; ++i)
            {
                sandFallPhysicsComponent.AddStud(prefab, offset * i);
            }
        }

        private static void GenerateO(GameObject prefab, SandFallPhysicsComponent sandFallPhysicsComponent)
        {
            sandFallPhysicsComponent.name = "O";
            
            for (int i = 0; i < 2; ++i)
            {
                for (int j = 0; j < 2; ++j)
                {
                    sandFallPhysicsComponent.AddStud(prefab, Vector2Int.right * i + Vector2Int.up * j);
                }
            }
        }

        private static void GenerateT(GameObject prefab, SandFallPhysicsComponent sandFallPhysicsComponent)
        {
            if (RandomUtils.Choice())
            {
                int direction = RandomUtils.OneOrNegativeOne();
                sandFallPhysicsComponent.name = direction < 0 ? "T (left)" : "T (right)";
                
                for (int i = 0; i < 3; ++i)
                {
                    sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up * i);
                }
                
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up + Vector2Int.right * direction);
            }
            else
            {
                int orientation = RandomUtils.ZeroOrOne();
                
                sandFallPhysicsComponent.name = orientation == 0 ? "T (upside down)" : "T";
                
                for (int i = -1; i < 2; ++i)
                {
                    sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up * orientation + Vector2Int.right * i);
                }
            
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up * (1 - orientation));
            }
        }

        private static void GenerateL(GameObject prefab, SandFallPhysicsComponent sandFallPhysicsComponent)
        {
            int orientation = RandomUtils.OneOrNegativeOne();
            if (RandomUtils.Choice())
            {
                sandFallPhysicsComponent.name = orientation == -1 ? "L (tall, left)" : "L (tall, right)";

                for (int i = 0; i < 3; ++i)
                {
                    sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up * i);
                }

                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.right * orientation);
            }
            else
            {
                sandFallPhysicsComponent.name = orientation == -1 ? "L (flat, left)" : "L (flat, right)";

                for (int i = -1; i < 2; ++i)
                {
                    sandFallPhysicsComponent.AddStud(prefab, Vector2Int.right * i);
                }

                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.right * orientation + Vector2Int.up);
            }
        }

        private static void GenerateS(GameObject prefab, SandFallPhysicsComponent sandFallPhysicsComponent)
        {
            if (RandomUtils.Choice())
            {
                int direction = RandomUtils.OneOrNegativeOne();
                sandFallPhysicsComponent.name = direction < 0 ? "S (tall, left)" : "S (flat, right)";
                
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.zero);
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up);
                
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up + Vector2Int.right * direction);
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up * 2 + Vector2Int.right * direction);
            }
            else
            {
                int direction = RandomUtils.ZeroOrOne();
                sandFallPhysicsComponent.name = direction == 0 ? "S (flat, bottom left, top right)" : "S (flat, top left, bottom right)";
            
                // the center bottom and top elements always exist
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.zero);
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.up);
            
                // add either bottom left and top right, or top left and bottom right elements
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.left + Vector2Int.up * direction);
                sandFallPhysicsComponent.AddStud(prefab, Vector2Int.right + Vector2Int.up * (1 - direction));
            }
        }
    }
}
