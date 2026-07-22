using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Purple (Time) bricks are a one-shot pickup, not an ongoing stat modifier: attaching one adds a
    // fixed chunk of time straight onto the round timer, and losing one takes that time back off
    // again - so the granted seconds are only ever borrowed for as long as the brick is carried.
    //
    // RoundTimerBar derives both the remaining time and the visible bar from one brick list, so these
    // two calls keep clock and bar in sync on their own. Sits next to PlayerAssembly and listens the
    // same way PlayerHazardResponder does.
    [RequireComponent(typeof(PlayerAssembly))]
    public class TimeBrickBonus : MonoBehaviour
    {
        [Tooltip("Seconds added to the round timer per Time Brick picked up - and taken back off when " +
                 "one is lost.")]
        [SerializeField] private float secondsPerBrick = 15f;

        private PlayerAssembly _assembly;
        private RoundTimerBar _timerBar;

        private void Awake() => _assembly = GetComponent<PlayerAssembly>();

        private void OnEnable()
        {
            _assembly.OnBrickAttached += HandleBrickAttached;
            _assembly.OnBricksDetached += HandleBricksDetached;
        }

        private void OnDisable()
        {
            _assembly.OnBrickAttached -= HandleBrickAttached;
            _assembly.OnBricksDetached -= HandleBricksDetached;
        }

        private void HandleBrickAttached(BrickNode brick)
        {
            if (brick.Color != BrickColor.Purple) return;
            Timer?.AddSeconds(secondsPerBrick);
        }

        // A single hazard hit can take several bricks with it (anything the flood fill cuts off), so
        // this counts the purple ones in the batch rather than assuming one.
        private void HandleBricksDetached(IReadOnlyList<BrickNode> detached)
        {
            int lostTimeBricks = 0;
            foreach (BrickNode brick in detached)
            {
                if (brick != null && brick.Color == BrickColor.Purple) lostTimeBricks++;
            }

            if (lostTimeBricks == 0) return;
            Timer?.RemoveSeconds(secondsPerBrick * lostTimeBricks);
        }

        private RoundTimerBar Timer
        {
            get
            {
                if (_timerBar == null) _timerBar = FindFirstObjectByType<RoundTimerBar>();
                return _timerBar;
            }
        }
    }
}
