namespace SpieleMarmelade.Core.Progression
{
    /// <summary>
    /// Which running statistic an <see cref="AchievementDefinition"/> tracks. The "All..."
    /// variants compare against the live minigame registry instead of a fixed target.
    /// </summary>
    public enum AchievementStatKey
    {
        GamesPlayed,
        GamesCompleted,
        TotalScore,
        WinsWithoutDeath,

        /// <summary>Lower is better; unlocked when 0 &lt; value &lt;= targetValue.</summary>
        FastestCompletionSeconds,

        /// <summary>Unlocked when gamesCompleted &gt;= number of registered minigames.</summary>
        AllGamesCompleted,

        /// <summary>Unlocked when unlockedGameIds.Count &gt;= number of registered minigames.</summary>
        AllGamesUnlocked,

        /// <summary>Number of distinct genres played at least once.</summary>
        DistinctGenresPlayed,

        /// <summary>Generic, code-free hook: compares against a named counter reported via
        /// <see cref="AchievementManager.ReportEvent"/> and <see cref="AchievementDefinition.eventKey"/>.
        /// Lets new achievements be authored for arbitrary gameplay events without new C# code.</summary>
        CustomEvent
    }
}
