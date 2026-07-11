using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Authored in the Menu Flow Editor (Tools/Game Creation/Menu Flow Editor), one per minigame.
    // Describes which screens exist and how their buttons connect — the editor's "Generate"
    // step turns this into the actual Canvas/panels in the minigame's scene.
    [CreateAssetMenu(menuName = "Spiele Marmelade/Menu Flow Graph", fileName = "MenuFlow_")]
    public class MenuFlowGraph : ScriptableObject
    {
        public List<MenuScreenNode> screens = new();
        public string startScreenId;

        [Header("Brick-Text-Buttons")]
        [Tooltip("Leer lassen für einen automatischen Fallback (M_Brick_Black).")]
        public Material buttonBackgroundMaterial;
        [Tooltip("Leer lassen für einen automatischen Fallback (M_Special_GlowWhite).")]
        public Material buttonLetterMaterial;

        public MenuScreenNode FindScreen(string id) =>
            string.IsNullOrEmpty(id) ? null : screens.Find(s => s.id == id);
    }
}
