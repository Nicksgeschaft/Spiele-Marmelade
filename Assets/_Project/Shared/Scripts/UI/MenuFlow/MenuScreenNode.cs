using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    [Serializable]
    public class MenuScreenNode
    {
        public string id = Guid.NewGuid().ToString("N");
        public MenuScreenKind kind = MenuScreenKind.Generic;
        public string title = "Screen";

        [TextArea] public string bodyText;

        [Header("Titel")]
        [Tooltip("Größe des Brick-Titels. 1 = normal (wie die Buttons). Für den Spieltitel im Hauptmenü höher stellen.")]
        public float titleScale = 1f;

        [Tooltip("Jeder einzelne Brick des Titels bekommt eine zufällige Farbe aus der Palette, " +
                 "statt einer Farbe pro Buchstabe. Gedacht für den großen Spieltitel.")]
        public bool titleRandomBrickColors;

        [Tooltip("Einzelne Bricks des Titels lassen sich anklicken und fallen dann herunter. " +
                 "Reine Spielerei - der Titel ist kein Button.")]
        public bool titleBricksFall;

        public List<MenuButtonDef> buttons = new();

        // Editor-only: node position on the Menu Flow Editor canvas.
        public Vector2 editorPosition;
    }
}
