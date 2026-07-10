using UnityEngine;

namespace GameJamUniverse.Shared.Abilities
{
    // Contract for anything a player can "use" from an ability/quick-item slot (Q/R/F, 1-4).
    // Implement on any MonoBehaviour and assign it into an AbilitySlot in the Inspector — new
    // abilities/items never require changes to AbilitySlot or PlayerInputReader.
    public interface IUsable
    {
        bool CanUse(GameObject user);
        void Use(GameObject user);
    }
}
