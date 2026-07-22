using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
  public readonly struct Stud
  {
    public readonly GameObject GameObject;
    public readonly Vector2Int RelativeOffset;

    public Stud(GameObject gameObject, Vector2Int relativeOffset)
    {
      GameObject = gameObject;
      RelativeOffset = relativeOffset;
    }
  }
  
  public readonly struct OrderedStudGroup
  {
    public readonly int OffsetX;
    public readonly IOrderedEnumerable<Stud> Studs;

    public OrderedStudGroup(int offsetX, IOrderedEnumerable<Stud> studs)
    {
      OffsetX = offsetX;
      Studs = studs;
    }
  }

  public readonly struct FallResult
  {
    private readonly HashSet<Vector2Int> _gridPositions;

    public HashSet<Vector2Int> GridPositions => _gridPositions;
    public bool HitGrid => _gridPositions.Count > 0;
    public bool HasLost { get; }

    public FallResult(bool hasLost, HashSet<Vector2Int> gridPositions)
    {
      HasLost = hasLost;
      _gridPositions = gridPositions;
    }
  }

  public class SandFallPhysicsComponent : MonoBehaviour
  {
    private readonly List<Stud> _studs = new List<Stud>();

    public IEnumerable<Stud> Studs => _studs.OrderBy(x => x.RelativeOffset.x).ThenBy(x => x.RelativeOffset.y);
    public bool IsEmpty => _studs.Count == 0;
    
    public IEnumerable<OrderedStudGroup> StudsPerColumnFromBottomToTop =>
      _studs
        .GroupBy(x => x.RelativeOffset.x)
        .Select(x => new OrderedStudGroup(x.Key, x.OrderBy(e => e.RelativeOffset.y)));
    
    public Vector2Int? SizeX
    {
      get
      {
          if (_studs.Count == 0)
          {
            return null;
          }

          Vector2Int result = Vector2Int.one * _studs[0].RelativeOffset.x;

          for (int i = 1; i < _studs.Count; i++)
          {
            int x = _studs[i].RelativeOffset.x;
            if (x < result.x)
            {
              result.x = x;
            }
            if (x > result.y)
            {
              result.y = x;
            }
          }

          return result;
      }
    }
    
    public void AddStud(GameObject prefab, Vector2Int offsetInt)
    {
      GameObject stud = GameObject.Instantiate(prefab, transform);
      StudDataComponent studDataComponent = stud.AddComponent<StudDataComponent>();
      studDataComponent.Prefab = prefab;
      AddExistingStud(stud, studDataComponent, offsetInt);
    }

    public void AddExistingStud(GameObject stud, Vector2Int offsetInt)
    {
      AddExistingStud(stud, stud.GetComponent<StudDataComponent>(), offsetInt);
    }

    private void AddExistingStud(GameObject stud, StudDataComponent studDataComponent, Vector2Int offsetInt)
    {
      Debug.Assert(studDataComponent.gameObject == stud);
      
      studDataComponent.Offset = offsetInt;
      stud.name = $"{offsetInt.x},{offsetInt.y}";
      Vector2 offset = offsetInt;
      offset.Scale(BrickUtils.UnityStudSize);
      stud.transform.localPosition = offset.XZ();
      
      _studs.Add(new Stud(stud, offsetInt));
    }

    public FallResult Fall(ITetrisGameConfig config, Grid grid)
    {
      float desiredMove = -config.FallSpeedInStudsPerSecond * BrickUtils.UnityStudSize.y * Time.deltaTime;
      float actualMove = desiredMove;
      Vector2 offset = BrickUtils.UnityStudSizeOnlyY * 0.5f;
      Vector2Int gridSize = Grid.GetGridSize(config, Grid.GridType.PiecesOnly);

      using var pooledObject = HashSetPool<int>.Get(out HashSet<int> collidedColumns);
      
      // for all the groups, first determine whether we collide with the grid below (or the border)
      foreach (OrderedStudGroup group in StudsPerColumnFromBottomToTop)
      {
        Stud stud = group.Studs.First();
        // we need to apply an offset here since the studs are centered on their position
        // and so they would be overlapping already
        Vector2 position = stud.GameObject.transform.position.XZ() - offset;
        Vector2Int gridToTest = Grid.WorldToGridRounded(position, gridSize);
        if (gridToTest.y < 0 || grid.GetGridObject(gridToTest))
        {
          // calculate limited distance so that we do not overshoot
          Vector2Int finalGridPosition = new Vector2Int(gridToTest.x, Mathf.Max(gridToTest.y + 1, 0));
          float finalPosition = Grid.GridToWorld(finalGridPosition, gridSize).y;
          float maxMove = finalPosition - stud.GameObject.transform.position.z;
          if (maxMove >= desiredMove)
          {
            // the movedistance needs to be limited so this is an actual collision
            collidedColumns.Add(group.OffsetX);

            actualMove = maxMove;
          }
        }
      }

      // we can finally move all studs
      foreach (Stud stud in _studs)
      {
        stud.GameObject.transform.position += actualMove * Vector3.forward;
      }

      HashSet<Vector2Int> gridChanges = new HashSet<Vector2Int>();
      bool hasLost = false;

      // all studs from columns that have collided are fixed into the grid and removed from this component
      for (int i = _studs.Count - 1; i >= 0; --i)
      {
        Stud stud = _studs[i];
        
        if (collidedColumns.Contains(stud.RelativeOffset.x))
        {
          if (!grid.AddIntoGrid(stud.GameObject, out Vector2Int gridPosition))
          {
            hasLost = true;
          }

          _studs.RemoveAtSwapBack(i);

          gridChanges.Add(gridPosition);
        }
      }

      return new FallResult(hasLost, gridChanges);
    }

    public static SandFallPhysicsComponent Spawn(string name)
    {
      GameObject piece = new GameObject(name, new [] { typeof(SandFallPhysicsComponent) });
      return piece.GetComponent<SandFallPhysicsComponent>();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      Scene scene = SceneManager.GetActiveScene();
      TetrisGameBehaviour tetrisGameBehaviour = scene.GetRootGameObjects()
        .Select(x => x.GetComponent<TetrisGameBehaviour>())
        .FirstOrDefault(x => x != null);

      if (!tetrisGameBehaviour)
      {
        return;
      }

      Vector2Int gridSize = Grid.GetGridSize(tetrisGameBehaviour.Config, Grid.GridType.PiecesOnly);
      
      Gizmos.color = Color.magenta;
      UnityEditor.Handles.color = Color.magenta;
      foreach (Stud stud in _studs)
      {
        Vector3 offset = stud.GameObject.transform.position - tetrisGameBehaviour.transform.position;
        Vector2Int gridPosition = Grid.WorldToGridRounded(offset.XZ(), gridSize);
        UnityEditor.Handles.Label(stud.GameObject.transform.position, $"{gridPosition.x},{gridPosition.y}"); 
      }
    }
#endif
  }
}
