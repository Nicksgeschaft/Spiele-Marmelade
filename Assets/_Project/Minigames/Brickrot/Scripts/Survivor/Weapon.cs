using System;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // A colour-keyed ability slot with its own cooldown. The colour is what ties it to the Tetris
    // half — the colour of the brick that spawned decides which weapon fires (see
    // TetrisToSurvivorBridge).
    //
    // Plain C# rather than a MonoBehaviour so a component can hold a whole list of these. That
    // only works with [Serializable]: without it Unity ignores the [SerializeField] fields
    // entirely and the slot never appears in the Inspector — which is the state this class
    // arrived in from the original project, where it was written but never wired up.
    [Serializable]
    public class Weapon
    {
        [SerializeField] private Color color = Color.white;

        [Tooltip("Sekunden zwischen zwei Schüssen. 0 = keine Wartezeit.")]
        [Min(0f)] [SerializeField] private float cooldown = 1f;

        [Tooltip("Startet die Waffe abgekühlt (feuert sofort) statt erst nach einem vollen Cooldown.")]
        [SerializeField] private bool readyOnStart = true;

        // Runtime only, deliberately not serialized — a cooldown carried across a reload would be
        // meaningless.
        private float _remaining;
        private bool _initialised;

        public Color WeaponColor
        {
            get => color;
            set => color = value;
        }

        public float Cooldown
        {
            get => cooldown;
            set => cooldown = Mathf.Max(0f, value);
        }

        /// <summary>True while the weapon still has to wait before it can fire again.</summary>
        public bool IsOnCooldown => _remaining > 0f;

        /// <summary>How much of the cooldown is still to run, 0..1. Useful for a UI radial.</summary>
        public float CooldownFraction => cooldown <= 0f ? 0f : Mathf.Clamp01(_remaining / cooldown);

        /// <summary>
        /// Advances the cooldown and returns true on the frame the weapon becomes ready, starting
        /// the next cooldown as it does. Call once per frame with Time.deltaTime.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            // No cooldown configured means no gate at all.
            if (cooldown <= 0f) return true;

            if (!_initialised)
            {
                _initialised = true;
                _remaining = readyOnStart ? 0f : cooldown;
            }

            _remaining -= deltaTime;
            if (_remaining > 0f) return false;

            // Carry the overshoot into the next cycle rather than discarding it. The original
            // refilled to exactly `cooldown` here, which threw away up to a frame every cycle —
            // so the weapon fired measurably slower than its configured rate, and worse the lower
            // the frame rate.
            _remaining += cooldown;

            // A frame longer than the whole cooldown (or a hitch) would otherwise leave this
            // negative and let the weapon bank up shots. One shot per Tick, at most.
            if (_remaining < 0f) _remaining = cooldown;

            return true;
        }

        /// <summary>Puts the weapon on a fresh full cooldown — it cannot fire until that elapses.</summary>
        public void StartCooldown()
        {
            _initialised = true;
            _remaining = cooldown;
        }

        /// <summary>Makes the weapon fire on the next <see cref="Tick"/>.</summary>
        public void MakeReady()
        {
            _initialised = true;
            _remaining = 0f;
        }
    }
}
