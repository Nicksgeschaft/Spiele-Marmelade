namespace GameJamUniverse.Core.Minigames
{
    /// <summary>
    /// Contract every minigame module must implement. The Hub/GameManager only ever talks to
    /// minigames through this interface - never through concrete types - so new minigames can
    /// be added without touching shared systems.
    /// </summary>
    public interface IMinigame
    {
        MinigameMetadata Metadata { get; }

        void Initialize(MinigameContext context);
        void StartGame();
        void PauseGame();
        void ResumeGame();
        void RestartGame();
        void QuitGame();
        void SaveProgress();
        void LoadProgress();
        void UnloadGame();
    }
}
