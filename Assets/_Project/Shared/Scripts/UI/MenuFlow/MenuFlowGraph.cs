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
        public bool buttonHasBackground = true;
        [Tooltip("Überschreibt buttonBackgroundColor, falls gesetzt. Leer lassen für automatischen Fallback (M_Brick_Black) bzw. die Farbe unten.")]
        public Material buttonBackgroundMaterial;
        [Tooltip("Nur benutzt, wenn buttonBackgroundMaterial leer ist und buttonHasBackground an ist.")]
        public Color buttonBackgroundColor = new(0.05f, 0.05f, 0.05f);

        [Tooltip("Überschreibt buttonLetterColors, falls gesetzt. Leer lassen für automatischen Fallback (M_Special_GlowWhite) bzw. die Farben unten.")]
        public Material buttonLetterMaterial;
        [Tooltip("Mehrere Farben: jeder Buchstabe nimmt reihum die nächste Farbe (z.B. für einen Regenbogen-Effekt). Ein Eintrag = einheitliche Farbe. Leer + kein Material = automatischer Fallback.")]
        public List<Color> buttonLetterColors = new();

        [Header("Options-Slider")]
        public Color sliderFilledColor = Color.white;
        public Color sliderUnfilledColor = new(0.15f, 0.15f, 0.15f);

        public MenuScreenNode FindScreen(string id) =>
            string.IsNullOrEmpty(id) ? null : screens.Find(s => s.id == id);
    }
}
