using SpieleMarmelade.Core.Minigames;
using SpieleMarmelade.Minigames.Brickrot.Survivor;
using SpieleMarmelade.Minigames.Brickrot.Tetris;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Entry point for the hub. Brickrot is an endless run scored by how long you last.
    //
    // There is NO player health bar. The Tetris field IS the health: every hit the player takes
    // drops a black damage brick into the Tetris half (SurvivorEvents.OnTakeDamage → TetrisGame),
    // and when that field fills to the top the run is over (TetrisEvents.OnGameOver). This
    // controller therefore only watches those two events — it does not track or subtract health.
    //
    // Context is null when the scene is played directly (no Boot/GameManager), which is the normal
    // way to iterate — hence the null-conditional calls.
    public class BrickrotMinigameController : MinigameBase
    {
        [Tooltip("Damage-Events der Gegner. Muss dasselbe Asset sein, das auch Gegner und " +
                 "TetrisGameConfig benutzen — hierüber landen Treffer als schwarze Steine im Tetris.")]
        [SerializeField] private SurvivorEvents survivorEvents;

        [Tooltip("Tetris-Events. Muss dasselbe Asset sein, das auch TetrisGameConfig und die " +
                 "Bridge benutzen — das volle Feld beendet den Run.")]
        [SerializeField] private TetrisEvents tetrisEvents;

        [Header("Juice bei Treffer")]
        [Tooltip("Kamera-Ruckler wenn der Spieler getroffen wird (ein schwarzer Stein).")]
        [SerializeField] private float hurtShake = 0.4f;

        [Tooltip("Zeitstopp beim Treffer, in Sekunden Echtzeit.")]
        [SerializeField] private float hurtHitStop = 0.04f;

        [Tooltip("Kamera-Ruckler beim endgültigen Game Over (Feld voll).")]
        [SerializeField] private float gameOverShake = 1f;

        private HitFeedback _playerFeedback;
        private float _startTime;
        private bool _finished;

        /// <summary>Seconds survived so far — this is the score.</summary>
        public float SurvivedSeconds => _finished ? 0f : Time.time - _startTime;

        protected override void OnStartGame()
        {
            _startTime = Time.time;
            _finished = false;
        }

        private void Awake()
        {
            // Playing the scene directly never routes through StartGame, so seed the clock here too.
            _startTime = Time.time;
        }

        private void OnEnable()
        {
            if (survivorEvents != null) survivorEvents.OnTakeDamage += HandlePlayerHit;
            if (tetrisEvents != null) tetrisEvents.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            if (survivorEvents != null) survivorEvents.OnTakeDamage -= HandlePlayerHit;
            if (tetrisEvents != null) tetrisEvents.OnGameOver -= HandleGameOver;
        }

        private void Start()
        {
            // Found by tag rather than serialized, so the scene needs no extra wiring — the player
            // is already tagged for the enemies' targeting.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            _playerFeedback = player.GetComponent<HitFeedback>();
            if (_playerFeedback == null) _playerFeedback = player.AddComponent<HitFeedback>();
        }

        // Player got hit → a black brick is already on its way into the Tetris field (TetrisGame
        // listens to the same event). All we add here is the feel of being hit; no health changes.
        private void HandlePlayerHit(int damage)
        {
            if (_finished) return;

            if (_playerFeedback != null) _playerFeedback.Play();
            CameraShake.Add(hurtShake);
            HitStop.Freeze(hurtHitStop);
        }

        // The Tetris field reached the top — the only way to lose. TetrisGame stops spawning at
        // that point, so without ending the run here it would just hang.
        private void HandleGameOver()
        {
            if (_finished) return;

            CameraShake.Add(gameOverShake);
            EndRun();
        }

        // Survived time is the score, so a longer run beats a shorter one even though every run
        // ends in a full field.
        private void EndRun()
        {
            _finished = true;
            float survived = Time.time - _startTime;

            Context?.ReportScore(Mathf.FloorToInt(survived));
            Context?.ReportTime(survived);
            Context?.CompleteGame(false);
        }
    }
}
