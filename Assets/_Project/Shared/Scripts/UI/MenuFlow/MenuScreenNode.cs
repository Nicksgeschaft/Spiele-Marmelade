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

        public List<MenuButtonDef> buttons = new();

        // Editor-only: node position on the Menu Flow Editor canvas.
        public Vector2 editorPosition;
    }
}
