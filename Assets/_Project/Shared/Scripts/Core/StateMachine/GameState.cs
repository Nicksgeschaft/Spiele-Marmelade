namespace SpieleMarmelade.Core.StateMachine
{
    /// <summary>
    /// Global application states. Drives which systems/UI are active and how scenes flow.
    /// </summary>
    public enum GameState
    {
        Boot,
        Loading,
        Hub,
        EnteringGame,
        Playing,
        Paused,
        GameOver,
        Victory,
        ReturningToHub,
        Saving,
        Settings
    }
}
