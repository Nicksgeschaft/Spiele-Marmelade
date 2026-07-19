using UnityEngine;

namespace SpieleMarmelade.Shared.UI
{
    // Paints the in-game player with whatever colours were picked in the Character Creator. Put this
    // anywhere at or above the character's visual - it searches its own children for the Head/Body/
    // Feet parts, so it works both on the visual itself and on a player root that has the visual
    // somewhere below it.
    //
    // Runs in Start rather than Awake so it lands after any component that swaps the visual's
    // materials on startup (BrickNode does exactly that for collectable bricks); painting first would
    // just be overwritten.
    public class CharacterAppearance : MonoBehaviour
    {
        private void Start() => Apply();

        /// <summary>Re-reads the saved colours and repaints. Call after changing them at runtime.</summary>
        public void Apply() => CharacterLook.ApplySaved(transform);
    }
}
