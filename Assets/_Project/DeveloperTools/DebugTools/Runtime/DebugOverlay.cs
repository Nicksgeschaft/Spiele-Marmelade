using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace SpieleMarmelade.DevTools
{
    /// <summary>
    /// Always-available debug HUD. Press F1 to toggle FPS and memory usage. Attach to a
    /// persistent object (e.g. the Boot scene's GameManager) so it survives scene loads.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private bool _visible;
        private float _fps;
        private float _fpsAccumulator;
        private int _fpsFrames;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                _visible = !_visible;
            }

            _fpsFrames++;
            _fpsAccumulator += Time.unscaledDeltaTime;
            if (_fpsAccumulator >= 0.5f)
            {
                _fps = _fpsFrames / _fpsAccumulator;
                _fpsFrames = 0;
                _fpsAccumulator = 0f;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;

            long memoryMb = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            string text = $"FPS: {_fps:F1}\nMemory: {memoryMb} MB\n[F1] toggle";

            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = Color.white }
            };

            GUI.Box(new Rect(10, 10, 180, 70), text, style);
        }
    }
}
