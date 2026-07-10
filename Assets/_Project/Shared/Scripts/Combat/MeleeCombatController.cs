using GameJamUniverse.Shared.Stats;
using UnityEngine;

namespace GameJamUniverse.Shared.Combat
{
    // OneHanded weapons can block/parry (RMB held); TwoHanded weapons trade that away for a
    // single heavy attack on RMB press instead — matches "Mit Zweihandwaffen stattdessen
    // schwerer Angriff" from the original combat design.
    public enum WeaponType
    {
        OneHanded,
        TwoHanded
    }

    // Player-side melee controller: 3-hit ground combo, block (partial damage reduction while
    // held) with a short parry window right after Block is pressed (full negation instead of
    // partial), and a distinct jump attack while airborne. Wired to PlayerController.OnLeftClick
    // (the shared Attack action) as a persistent listener on the player prefab — same slot
    // SwordAttack used to occupy, see Player_ThirdPerson.prefab.
    public class MeleeCombatController : MonoBehaviour
    {
        [Header("── Waffe ─────────────────────────────")]
        [SerializeField] private WeaponType weaponType = WeaponType.OneHanded;

        [Header("── Attack ────────────────────────────")]
        [SerializeField] private MeleeHitbox hitbox;
        [SerializeField] private float swingDuration = 0.25f;
        [SerializeField] private float cooldown = 0.4f;

        [Header("── Combo ─────────────────────────────")]
        [Tooltip("Zeitfenster nach einem Schlag, in dem ein weiterer Angriff die Kombo fortsetzt statt sie zurückzusetzen.")]
        [SerializeField] private float comboWindow = 0.8f;
        [SerializeField] private float[] comboDamageMultipliers = { 1f, 1f, 1.5f };

        [Header("── Sprungangriff ─────────────────────")]
        [SerializeField] private float jumpAttackDamageMultiplier = 1.5f;

        [Header("── Block & Parry (nur Einhänder) ─────")]
        [Tooltip("Schaden-Anteil, der beim normalen Blocken noch durchkommt (0 = kein Schaden, 1 = kein Block-Effekt).")]
        [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;
        [Tooltip("Kurzes Fenster direkt nach Block-Druck: Treffer darin werden zu 100% negiert (Parry) statt nur reduziert.")]
        [SerializeField] private float parryWindow = 0.2f;

        [Header("── Schwerer Angriff (nur Zweihänder) ─")]
        [SerializeField] private float heavyAttackDamageMultiplier = 2f;
        [SerializeField] private float heavyAttackCooldown = 1f;
        [SerializeField] private float heavyAttackSwingDuration = 0.4f;

        private CharacterStats _stats;
        private Health _health;
        private IPlayerMovement _movement;
        private PlayerInputReader _input;

        private float _nextAttackTime;
        private int _comboStep;
        private float _comboExpireTime;

        private bool _blocking;
        private float _blockStartTime = -999f;

        private void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _health = GetComponent<Health>();
            _movement = GetComponent<IPlayerMovement>();
            _input = GetComponent<PlayerInputReader>();
        }

        private void OnDisable()
        {
            if (_health != null) _health.DamageMultiplier = 1f;
        }

        private void Update()
        {
            if (_input == null) return;

            if (weaponType == WeaponType.TwoHanded)
            {
                // Two hands on the weapon means no free hand for a shield — RMB is a heavy
                // attack instead of block, so damage is never reduced here.
                if (_health != null) _health.DamageMultiplier = 1f;
                if (_input.BlockPressedThisFrame) TryHeavyAttack();
                return;
            }

            if (_health == null) return;

            bool blocking = _input.BlockHeld;
            if (blocking && !_blocking) _blockStartTime = Time.time;
            _blocking = blocking;

            if (!_blocking)
            {
                _health.DamageMultiplier = 1f;
                return;
            }

            bool inParryWindow = Time.time - _blockStartTime <= parryWindow;
            _health.DamageMultiplier = inParryWindow ? 0f : blockDamageMultiplier;
        }

        public void OnAttackPressed()
        {
            if (hitbox == null || Time.time < _nextAttackTime || _blocking) return;

            _nextAttackTime = Time.time + cooldown;

            float multiplier;
            bool airborne = _movement != null && !_movement.IsGrounded;
            if (airborne)
            {
                multiplier = jumpAttackDamageMultiplier;
                ResetCombo();
            }
            else
            {
                if (Time.time > _comboExpireTime) _comboStep = 0;
                multiplier = comboDamageMultipliers[_comboStep % comboDamageMultipliers.Length];
                _comboStep = (_comboStep + 1) % comboDamageMultipliers.Length;
                _comboExpireTime = Time.time + comboWindow;
            }

            hitbox.Activate(swingDuration, GetBaseDamage() * multiplier);
        }

        private void TryHeavyAttack()
        {
            if (hitbox == null || Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + heavyAttackCooldown;
            hitbox.Activate(heavyAttackSwingDuration, GetBaseDamage() * heavyAttackDamageMultiplier);
            ResetCombo();
        }

        // If a CharacterStats component defines a Damage stat, it (base + modifiers) overrides
        // MeleeHitbox's own serialized damage as the baseline for every multiplier above.
        private float GetBaseDamage() =>
            _stats != null && _stats.HasStat(StatType.Damage) ? _stats.GetStat(StatType.Damage) : hitbox.Damage;

        private void ResetCombo()
        {
            _comboStep = 0;
            _comboExpireTime = 0f;
        }
    }
}
