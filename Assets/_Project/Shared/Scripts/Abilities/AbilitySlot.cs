using SpieleMarmelade.Shared.Audio;
using UnityEngine;

namespace SpieleMarmelade.Shared.Abilities
{
    public enum SlotKey
    {
        Ability1,
        Ability2,
        AbilitySpecial,
        QuickSlot1,
        QuickSlot2,
        QuickSlot3,
        QuickSlot4
    }

    // One reusable component for every ability/quick-item button (Q/R/F, 1-4) — which physical
    // key it reacts to is picked via slotKey, what it actually does is whatever IUsable
    // component is dropped into usableBehaviour. Add one instance per slot to the player;
    // leave usableBehaviour empty on slots that aren't wired to anything yet.
    [RequireComponent(typeof(PlayerInputReader))]
    public class AbilitySlot : MonoBehaviour
    {
        [SerializeField] private SlotKey slotKey;
        [Tooltip("Muss eine Komponente sein, die IUsable implementiert (z. B. HealthPotionUsable).")]
        [SerializeField] private MonoBehaviour usableBehaviour;
        [SerializeField] private float cooldown = 0.5f;
        [SerializeField] private string useSfxId;

        private PlayerInputReader _input;
        private IUsable _usable;
        private float _nextUseTime;

        private void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _usable = usableBehaviour as IUsable;
            if (usableBehaviour != null && _usable == null)
            {
                Debug.LogWarning($"[AbilitySlot] '{usableBehaviour.GetType().Name}' auf '{name}' " +
                                  "implementiert IUsable nicht — Slot bleibt wirkungslos.");
            }
        }

        private void OnEnable()
        {
            if (_input == null) return;
            switch (slotKey)
            {
                case SlotKey.Ability1: _input.Ability1Performed += OnSlotPressed; break;
                case SlotKey.Ability2: _input.Ability2Performed += OnSlotPressed; break;
                case SlotKey.AbilitySpecial: _input.AbilitySpecialPerformed += OnSlotPressed; break;
                case SlotKey.QuickSlot1: _input.QuickSlot1Performed += OnSlotPressed; break;
                case SlotKey.QuickSlot2: _input.QuickSlot2Performed += OnSlotPressed; break;
                case SlotKey.QuickSlot3: _input.QuickSlot3Performed += OnSlotPressed; break;
                case SlotKey.QuickSlot4: _input.QuickSlot4Performed += OnSlotPressed; break;
            }
        }

        private void OnDisable()
        {
            if (_input == null) return;
            switch (slotKey)
            {
                case SlotKey.Ability1: _input.Ability1Performed -= OnSlotPressed; break;
                case SlotKey.Ability2: _input.Ability2Performed -= OnSlotPressed; break;
                case SlotKey.AbilitySpecial: _input.AbilitySpecialPerformed -= OnSlotPressed; break;
                case SlotKey.QuickSlot1: _input.QuickSlot1Performed -= OnSlotPressed; break;
                case SlotKey.QuickSlot2: _input.QuickSlot2Performed -= OnSlotPressed; break;
                case SlotKey.QuickSlot3: _input.QuickSlot3Performed -= OnSlotPressed; break;
                case SlotKey.QuickSlot4: _input.QuickSlot4Performed -= OnSlotPressed; break;
            }
        }

        private void OnSlotPressed()
        {
            if (_usable == null || Time.time < _nextUseTime || !_usable.CanUse(gameObject)) return;

            _nextUseTime = Time.time + cooldown;
            _usable.Use(gameObject);
            SfxPlayer.Play(useSfxId);
        }
    }
}
