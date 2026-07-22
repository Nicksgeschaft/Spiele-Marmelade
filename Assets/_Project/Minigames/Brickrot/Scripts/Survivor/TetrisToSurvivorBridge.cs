using UnityEngine;
using SpieleMarmelade.Minigames.Brickrot.Tetris;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // The link that makes this a hybrid rather than two games in one window: every brick the
    // Tetris half spawns fires the matching ability on the Survivor half. Colour picks the
    // ability, so what you stack on the right decides what you shoot with on the left.
    //
    // One brick = one ability. This is the ONLY place that turns a spawned brick into an ability;
    // TetrisGame just announces the spawn (see ITetrisGameConfig). Damage bricks never get here at
    // all — they're spawned on a separate path that doesn't raise OnBrickSpawned, which is exactly
    // right: those are the damage you take, not an ability you get.
    public class TetrisToSurvivorBridge : MonoBehaviour
    {
        [Header("Tetris")]
        public TetrisEvents tetrisEvents;

        [Header("Survivor Abilities (auf deinem PlayerController)")]
        public NatureScytheAttack nature;
        public WaveAuraAttack wave;
        public FireShotAttack fire;

        [Tooltip("Anti-Spam-Sperre in Sekunden. 0 = jeder Stein löst garantiert aus. Höhere Werte " +
                 "verschlucken Fähigkeiten, wenn zwei Steine schnell hintereinander kommen.")]
        [Min(0f)] public float cooldown = 0f;

        private float _nextAllowed;

        private void OnEnable()
        {
            if (tetrisEvents != null) tetrisEvents.OnBrickSpawned += HandleBrickSpawned;
        }

        private void OnDisable()
        {
            if (tetrisEvents != null) tetrisEvents.OnBrickSpawned -= HandleBrickSpawned;
        }

        private void HandleBrickSpawned(StudColor color)
        {
            if (cooldown > 0f)
            {
                if (Time.time < _nextAllowed) return;
                _nextAllowed = Time.time + cooldown;
            }

            // TriggerFromTetris rather than Fire: it honours each ability's Frequency upgrade, so
            // "one brick = one trigger" still lets an upgraded ability fire several shots per
            // brick. Calling Fire() directly would silently make that upgrade do nothing.
            switch (color)
            {
                case StudColor.Green:
                    if (nature != null) nature.TriggerFromTetris();
                    break;
                case StudColor.Blue:
                    if (wave != null) wave.TriggerFromTetris();
                    break;
                case StudColor.Red:
                    if (fire != null) fire.TriggerFromTetris();
                    break;

                // White and Damage deliberately do nothing. Damage bricks are the punishment for
                // taking hits in the Survivor half — they clog the playfield, they don't arm you.
                case StudColor.White:
                case StudColor.Damage:
                default:
                    break;
            }
        }
    }
}
