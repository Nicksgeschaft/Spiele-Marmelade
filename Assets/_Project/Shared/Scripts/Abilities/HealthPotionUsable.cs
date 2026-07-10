using GameJamUniverse.Shared.Combat;
using UnityEngine;

namespace GameJamUniverse.Shared.Abilities
{
    // Example IUsable: a simple healing potion. Drop on the player alongside an AbilitySlot
    // (usableBehaviour = this) to wire it into any Q/R/F/quick-item slot.
    public class HealthPotionUsable : MonoBehaviour, IUsable
    {
        [SerializeField] private float healAmount = 30f;

        public bool CanUse(GameObject user)
        {
            var health = user.GetComponent<Health>();
            return health != null && !health.IsDead && health.CurrentHealth < health.MaxHealth;
        }

        public void Use(GameObject user)
        {
            user.GetComponent<Health>()?.Heal(healAmount);
        }
    }
}
