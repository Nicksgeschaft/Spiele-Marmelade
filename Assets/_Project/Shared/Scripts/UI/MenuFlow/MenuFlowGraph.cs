using System.Collections.Generic;
using UnityEngine;

namespace GameJamUniverse.Shared.UI.MenuFlow
{
    // Authored in the Menu Flow Editor (Tools/GameJam/Menu Flow Editor), one per minigame.
    // Describes which screens exist and how their buttons connect — the editor's "Generate"
    // step turns this into the actual Canvas/panels in the minigame's scene.
    [CreateAssetMenu(menuName = "GameJam Universe/Menu Flow Graph", fileName = "MenuFlow_")]
    public class MenuFlowGraph : ScriptableObject
    {
        public List<MenuScreenNode> screens = new();
        public string startScreenId;

        public MenuScreenNode FindScreen(string id) =>
            string.IsNullOrEmpty(id) ? null : screens.Find(s => s.id == id);
    }
}
