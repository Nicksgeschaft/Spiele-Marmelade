using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Polled from TetrisGameBehaviour.Update: the Tetris camera is placed by fitting the playfield
    // to the current aspect ratio, so it has to be refitted whenever the window is resized.
    public class ResolutionWatcher
    {
        private Vector2Int? _resolution;

        public delegate void ResolutionChangedHandler(Vector2Int? oldResolution, Vector2Int resolution);
        public event ResolutionChangedHandler OnResolutionChanged;

        public void Update()
        {
            Vector2Int resolution = new Vector2Int(Screen.width, Screen.height);
            if (resolution != _resolution)
            {
                Vector2Int? oldResolution = _resolution;
                _resolution = resolution;
                OnResolutionChanged?.Invoke(oldResolution, resolution);
            }
        }
    }
}
