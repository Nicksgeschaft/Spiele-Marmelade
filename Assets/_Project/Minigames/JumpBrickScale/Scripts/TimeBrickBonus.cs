using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Purple (Time) bricks are a one-shot pickup, not an ongoing stat modifier: the moment one attaches,
    // it adds a fixed chunk of time straight onto the round timer. RoundTimerBar.AddSeconds derives both
    // the actual remaining time and the visible bar length from the same brick list, so this single call
    // keeps them in sync automatically. Sits next to PlayerAssembly and listens the same way
    // PlayerHazardResponder does.
    [RequireComponent(typeof(PlayerAssembly))]
    public class TimeBrickBonus : MonoBehaviour
    {
        [Tooltip("Seconds added to the round timer per Time Brick picked up.")]
        [SerializeField] private float secondsPerBrick = 15f;

        private PlayerAssembly _assembly;
        private RoundTimerBar _timerBar;

        private void Awake() => _assembly = GetComponent<PlayerAssembly>();

        private void OnEnable() => _assembly.OnBrickAttached += HandleBrickAttached;

        private void OnDisable() => _assembly.OnBrickAttached -= HandleBrickAttached;

        private void HandleBrickAttached(BrickNode brick)
        {
            if (brick.Color != BrickColor.Purple) return;

            if (_timerBar == null) _timerBar = FindFirstObjectByType<RoundTimerBar>();
            _timerBar?.AddSeconds(secondsPerBrick);
        }
    }
}
