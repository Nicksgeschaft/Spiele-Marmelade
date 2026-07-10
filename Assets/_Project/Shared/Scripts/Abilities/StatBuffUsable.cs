using GameJamUniverse.Shared.Stats;
using UnityEngine;

namespace GameJamUniverse.Shared.Abilities
{
    // Example IUsable: a timed buff potion. Drop on the player alongside an AbilitySlot
    // (usableBehaviour = this) and a CharacterStats component to wire it into any slot.
    public class StatBuffUsable : MonoBehaviour, IUsable
    {
        [SerializeField] private StatType statType = StatType.MoveSpeed;
        [SerializeField] private StatModifierMode mode = StatModifierMode.PercentAdd;
        [SerializeField] private float value = 0.5f;
        [SerializeField] private float duration = 5f;

        public bool CanUse(GameObject user) => user.GetComponent<CharacterStats>() != null;

        public void Use(GameObject user)
        {
            var stats = user.GetComponent<CharacterStats>();
            if (stats == null) return;

            stats.AddModifier(new StatModifier
            {
                type = statType,
                mode = mode,
                value = value,
                duration = duration,
                sourceId = $"{nameof(StatBuffUsable)}_{GetInstanceID()}"
            });
        }
    }
}
