using System;

namespace SpieleMarmelade.Core.SaveSystem
{
    [Serializable]
    public class StatisticsData
    {
        public int totalSessions;
        public int gamesPlayed;
        public int gamesCompleted;
        public string favoriteGenre = "";
        public float averageSessionTimeSeconds;
        public float totalPlaytimeSeconds;
        public int totalScore;
        public int deaths;
        public int wins;
        public int losses;
        public int achievementsUnlocked;
        public int winsWithoutDeath;

        /// <summary>Fastest "best time" across all minigames, in seconds. 0 = none recorded yet.</summary>
        public float fastestCompletionSeconds;
    }
}
