namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Kept deliberately small: Generic covers MainMenu/LevelSelect/Credits/anything custom
    // (a title + optional body text + a list of buttons). Options and Pause get dedicated
    // built-in behaviour; Game isn't a panel at all — entering it hides the menu canvas and
    // activates the gameplay root.
    public enum MenuScreenKind
    {
        Generic,
        Options,
        Pause,
        Game,
    }
}
