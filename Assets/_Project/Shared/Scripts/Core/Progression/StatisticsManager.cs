using System.Collections.Generic;
using SpieleMarmelade.Core.Minigames;
using SpieleMarmelade.Core.SaveSystem;

namespace SpieleMarmelade.Core.Progression
{
    /// <summary>
    /// Aggregates per-session results into the persisted <see cref="StatisticsData"/>.
    /// </summary>
    public class StatisticsManager
    {
        private readonly SaveSystem.SaveSystem _saveSystem;

        public StatisticsManager(SaveSystem.SaveSystem saveSystem)
        {
            _saveSystem = saveSystem;
        }

        public StatisticsData Data => _saveSystem.Current.statistics;

        public void OnSessionStarted()
        {
            Data.totalSessions++;
        }

        public void OnGameStarted(MinigameMetadata metadata)
        {
            Data.gamesPlayed++;
            _saveSystem.Current.profile.gamesPlayed++;
            _saveSystem.Current.RegisterGenrePlayed(metadata.genre.ToString());
        }

        /// <summary>
        /// Records the result of a finished session and updates derived stats
        /// (favorite genre, fastest completion, playtime, etc.).
        /// </summary>
        public void OnGameCompleted(MinigameMetadata metadata, bool won, int scoreEarned, float sessionSeconds, int deathsThisSession)
        {
            SaveData save = _saveSystem.Current;
            StatisticsData stats = save.statistics;

            stats.totalScore += scoreEarned;
            stats.deaths += deathsThisSession;
            stats.totalPlaytimeSeconds += sessionSeconds;
            stats.averageSessionTimeSeconds = stats.totalSessions > 0
                ? stats.totalPlaytimeSeconds / stats.totalSessions
                : sessionSeconds;

            save.profile.totalPlaytimeSeconds += sessionSeconds;
            save.AddPlaytime(metadata.gameId, sessionSeconds);

            if (won)
            {
                stats.wins++;
                stats.gamesCompleted++;
                save.profile.gamesCompleted++;
                save.UnlockGame(metadata.gameId);

                if (deathsThisSession == 0)
                {
                    stats.winsWithoutDeath++;
                }

                float bestTime = save.GetBestTime(metadata.gameId);
                if (bestTime > 0f && (stats.fastestCompletionSeconds <= 0f || bestTime < stats.fastestCompletionSeconds))
                {
                    stats.fastestCompletionSeconds = bestTime;
                }
            }
            else
            {
                stats.losses++;
            }

            UpdateFavoriteGenre();
        }

        private void UpdateFavoriteGenre()
        {
            // Phase 1: most recently played genre acts as a simple placeholder.
            // Replace with per-genre playtime tracking once more minigames exist.
            List<string> played = _saveSystem.Current.playedGenres;
            if (played.Count > 0)
            {
                Data.favoriteGenre = played[played.Count - 1];
            }
        }
    }
}
