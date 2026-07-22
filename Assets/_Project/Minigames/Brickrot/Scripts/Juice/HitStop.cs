using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Very short time freeze on heavy impacts. A couple of frames of stillness is what makes an
    // explosion land as a punch rather than a puff — the eye reads the pause as weight.
    //
    // Deliberately conservative, because timeScale is global state:
    //  - it never fires while the game is already stopped (i.e. paused), so it can't resurrect a
    //    paused game by "restoring" timeScale to 1
    //  - overlapping calls extend the freeze instead of stacking restores
    //  - the runner restores timeScale if it is destroyed mid-freeze (scene change, play-mode
    //    exit), so a freeze can never leak out and leave the game stuck
    public static class HitStop
    {
        private static Runner _runner;

        /// <summary>
        /// Freezes for <paramref name="seconds"/> of real time. No-op if the game is already
        /// stopped or a longer freeze is still running.
        /// </summary>
        public static void Freeze(float seconds)
        {
            if (seconds <= 0f) return;
            if (Time.timeScale <= 0.001f) return; // paused — leave it alone

            if (_runner == null)
            {
                var go = new GameObject("[HitStop]") { hideFlags = HideFlags.HideAndDontSave };
                _runner = go.AddComponent<Runner>();
            }

            _runner.Freeze(seconds);
        }

        private class Runner : MonoBehaviour
        {
            private float _restoreTo = 1f;
            private float _endUnscaledTime;
            private bool _frozen;

            public void Freeze(float seconds)
            {
                if (!_frozen)
                {
                    _restoreTo = Time.timeScale;
                    _frozen = true;
                }

                // Extend rather than restart, so a burst of hits doesn't cut a freeze short.
                _endUnscaledTime = Mathf.Max(_endUnscaledTime, Time.unscaledTime + seconds);
                Time.timeScale = 0f;
            }

            private void Update()
            {
                if (!_frozen) return;
                if (Time.unscaledTime < _endUnscaledTime) return;

                Restore();
            }

            private void Restore()
            {
                _frozen = false;
                // Only take back control if nothing else changed timeScale meanwhile (e.g. a pause
                // menu opening during the freeze).
                if (Time.timeScale <= 0.001f) Time.timeScale = _restoreTo;
            }

            private void OnDisable() => Restore();
        }
    }
}
