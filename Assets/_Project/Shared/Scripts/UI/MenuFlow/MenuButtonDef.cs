using System;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    public enum MenuSpecialAction
    {
        None,
        QuitApp,
        ResumeGame,
        RestartGame,
    }

    [Serializable]
    public class MenuButtonDef
    {
        public string label = "Button";

        // Empty/null = no screen navigation — specialAction (if any) drives the click instead.
        public string targetScreenId;

        public MenuSpecialAction specialAction = MenuSpecialAction.None;
    }
}
