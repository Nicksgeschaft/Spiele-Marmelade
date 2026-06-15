using GameJamUniverse.Core.Managers;

namespace GameJamUniverse.Core.Minigames
{
    /// <summary>
    /// Handed to a minigame's <see cref="IMinigame.Initialize"/>. Gives the minigame a narrow,
    /// game-scoped view of the shared systems instead of a raw reference to everything in
    /// <see cref="GameManager"/>.
    /// </summary>
    public class MinigameContext
    {
        public MinigameMetadata Metadata { get; }
        private readonly GameManager _gameManager;

        public MinigameContext(MinigameMetadata metadata, GameManager gameManager)
        {
            Metadata = metadata;
            _gameManager = gameManager;
        }

        public int Highscore => _gameManager.SaveSystem.Current.GetHighscore(Metadata.gameId);
        public float BestTime => _gameManager.SaveSystem.Current.GetBestTime(Metadata.gameId);

        public void ReportScore(int score) =>
            _gameManager.SaveSystem.Current.SetHighscore(Metadata.gameId, score);

        public void ReportTime(float seconds) =>
            _gameManager.SaveSystem.Current.SetBestTime(Metadata.gameId, seconds);

        /// <summary>Call when the player wins/loses to update stats, achievements and save.</summary>
        public void CompleteGame(bool won) => _gameManager.OnMinigameCompleted(won);

        /// <summary>Returns to the Hub, unloading this minigame's scene.</summary>
        public void ReturnToHub() => _gameManager.RequestReturnToHub();
    }
}
