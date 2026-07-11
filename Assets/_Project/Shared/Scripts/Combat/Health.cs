using SpieleMarmelade.Shared.Audio;
using SpieleMarmelade.Shared.Stats;
using UnityEngine;
using UnityEngine.Events;

namespace SpieleMarmelade.Shared.Combat
{
    // Generic health component used by both the player and enemies. Deliberately minimal —
    // no resistances/armor/status effects yet, just a number that goes down and an event when
    // it hits zero.
    public class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private string hitSfxId;
        [SerializeField] private string deathSfxId;

        public UnityEvent OnDamaged;
        public UnityEvent OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>While true, TakeDamage is a no-op (dodge-roll i-frames, etc.). Set from outside.</summary>
        public bool IsInvulnerable { get; set; }

        /// <summary>Scales incoming damage before it's applied — 1 = normal, 0 = fully blocked.
        /// Set by a combat controller while blocking/parrying, reset to 1 when done.</summary>
        public float DamageMultiplier { get; set; } = 1f;

        private void Awake()
        {
            // If a CharacterStats component defines a MaxHealth stat, it (base + modifiers)
            // overrides the serialized maxHealth above — otherwise this field is used as-is.
            var stats = GetComponent<CharacterStats>();
            if (stats != null && stats.HasStat(StatType.MaxHealth))
            {
                maxHealth = stats.GetStat(StatType.MaxHealth);
            }

            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f || IsInvulnerable) return;

            amount *= DamageMultiplier;
            if (amount <= 0f) return; // fully blocked/parried — no damage, no OnDamaged flash

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            OnDamaged?.Invoke();
            SfxPlayer.Play(hitSfxId);

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
                OnDeath?.Invoke();
                SfxPlayer.Play(deathSfxId);
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }
    }
}
