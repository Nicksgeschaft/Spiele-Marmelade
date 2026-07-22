using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
  public class TetrisGameBehaviour : MonoBehaviour
  {
    private static readonly Vector2 Percentage = Vector2.one * 100.0f;
    private static readonly Vector2 PaddingPercentage = new Vector2(5.0f, 20.0f);

    [SerializeField]
    private TetrisGameConfig config;
    
    private ResolutionWatcher.ResolutionChangedHandler _resolutionChangedHandler;
    private readonly ResolutionWatcher _resolutionWatcher = new ResolutionWatcher();

    private TetrisGame _tetrisGame;
    private TetrisPieceInput _pieceInput;
    
    public ITetrisGameConfig Config => config;

    private void Awake()
    {
      SpawnBorder();
      AutoFitCamera();

      _resolutionChangedHandler = (_, _) => AutoFitCamera();
      _resolutionWatcher.OnResolutionChanged += _resolutionChangedHandler;
      _resolutionWatcher.Update();
    }

    private void OnEnable()
    {
      _tetrisGame = new TetrisGame(config);
      _pieceInput ??= new TetrisPieceInput();
    }

    private void OnDisable()
    {
      _tetrisGame.Dispose();
      _tetrisGame = null;
    }

    private void OnDestroy()
    {
      _resolutionWatcher.OnResolutionChanged -= _resolutionChangedHandler;
    }

    private void Update()
    {
      _resolutionWatcher.Update();

      if (_tetrisGame != null)
      {
        // Polled rather than pushed by PlayerInput SendMessages (how the original did it), so
        // this half needs no PlayerInput component on the scene object at all.
        _tetrisGame.SetMoveInput(_pieceInput.Read());
        _tetrisGame.Update();
      }
    }

    /// <summary>
    /// centers the camera on this GameObject so that the entire border area is visible
    /// </summary>
    private void AutoFitCamera()
    {
      Camera cam = config.TetrisCamera;
      // grow the bounds by the given PaddingPercentage to leave some room around the edges
      Vector2 borderExtent = Grid.GetGridBounds(config, Grid.GridType.IncludingBorder) * (Percentage + PaddingPercentage) / 100.0f * 0.5f;
      cam.transform.position = transform.position + Vector3.up * cam.CalculateDistanceToFit(borderExtent);
      // Was Unity.Mathematics' quaternion.LookRotation in the original project — same argument
      // order, but com.unity.mathematics isn't a dependency here and this was its only use.
      cam.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
    }

    private void SpawnBorderAt(Vector2Int borderSize, int x, int y)
    {
      GameObject border = GameObject.Instantiate(config.BorderStudPrefab, transform);
      border.name = $"{x},{y}";
      
      Vector2 offset = Grid.GridToWorld(x, y, borderSize);
      border.transform.position = transform.position + offset.XZ();
    }

    private void SpawnBorder()
    {
      Vector2Int borderSize = Grid.GetGridSize(config, Grid.GridType.IncludingBorder);

      for (int x = 0; x < borderSize.x; ++x)
      {
        SpawnBorderAt(borderSize, x, 0);
      }
      
      for (int i = 0; i < 2; ++i)
      {
        for (int y = 1; y < borderSize.y; ++y)
        {
          SpawnBorderAt(borderSize, i * (borderSize.x - 1), y);
        }
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      Gizmos.color = Color.yellow;
      UnityEditor.Handles.color = Color.yellow;
      IEnumerable<GameObject> gridObjects = _tetrisGame?.Grid?.GridObjects;
      if (gridObjects != null)
      {
        foreach (GameObject grid in gridObjects)
        {
          if (grid)
          {
            Vector2Int gridPosition = Grid.WorldToGridRounded(grid.transform.position.XZ(), Grid.GetGridSize(config, Grid.GridType.PiecesOnly));
            UnityEditor.Handles.Label(grid.transform.position, $"{gridPosition.x}, {gridPosition.y}"); 
          }
        }
      }
    }
#endif
  }
}
