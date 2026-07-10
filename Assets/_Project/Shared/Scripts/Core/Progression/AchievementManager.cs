using System;
using System.Collections.Generic;
using System.Globalization;
using GameJamUniverse.Core.Minigames;
using GameJamUniverse.Core.SaveSystem;
using UnityEngine;

namespace GameJamUniverse.Core.Progression
{
    /// <summary>
    /// Evaluates <see cref="AchievementDefinition"/>s against the current <see cref="SaveData"/>
    /// and unlocks them the moment their condition is met. Call <see cref="EvaluateAll"/> after
    /// any stat-changing event (game completed, game unlocked, ...).
    /// </summary>
    public class AchievementManager
    {
        private readonly SaveSystem.SaveSystem _saveSystem;
        private readonly List<AchievementDefinition> _definitions;
        private readonly MinigameRegistry _registry;

        public event Action<AchievementDefinition> AchievementUnlocked;

        public AchievementManager(SaveSystem.SaveSystem saveSystem, List<AchievementDefinition> definitions, MinigameRegistry registry)
        {
            _saveSystem = saveSystem;
            _definitions = definitions ?? new List<AchievementDefinition>();
            _registry = registry;
        }

        public IReadOnlyList<AchievementDefinition> Definitions => _definitions;

        public AchievementProgressEntry GetProgress(string achievementId) =>
            _saveSystem.Current.GetOrCreateAchievement(achievementId);

        /// <summary>Re-checks every achievement and unlocks any that newly qualify.</summary>
        public void EvaluateAll()
        {
            SaveData save = _saveSystem.Current;
            StatisticsData stats = save.statistics;
            int registeredGames = _registry != null ? _registry.Games.Count : 0;

            foreach (AchievementDefinition def in _definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.achievementId)) continue;

                AchievementProgressEntry progress = save.GetOrCreateAchievement(def.achievementId);
                if (progress.unlocked) continue;

                float current = def.statKey switch
                {
                    AchievementStatKey.GamesPlayed => stats.gamesPlayed,
                    AchievementStatKey.GamesCompleted => stats.gamesCompleted,
                    AchievementStatKey.TotalScore => stats.totalScore,
                    AchievementStatKey.WinsWithoutDeath => stats.winsWithoutDeath,
                    AchievementStatKey.FastestCompletionSeconds => stats.fastestCompletionSeconds,
                    AchievementStatKey.AllGamesCompleted => stats.gamesCompleted,
                    AchievementStatKey.AllGamesUnlocked => save.unlockedGameIds.Count,
                    AchievementStatKey.DistinctGenresPlayed => save.playedGenres.Count,
                    AchievementStatKey.CustomEvent => save.GetEventCounter(def.eventKey),
                    _ => 0f
                };
                progress.currentValue = current;

                float target = def.statKey switch
                {
                    AchievementStatKey.AllGamesCompleted => Mathf.Max(1, registeredGames),
                    AchievementStatKey.AllGamesUnlocked => Mathf.Max(1, registeredGames),
                    _ => def.targetValue
                };

                bool reached = def.lowerIsBetter
                    ? current > 0f && current <= target
                    : current >= target;

                if (reached)
                {
                    progress.unlocked = true;
                    progress.unlockedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                    stats.achievementsUnlocked++;
                    AchievementUnlocked?.Invoke(def);
                }
            }
        }

        /// <summary>
        /// Reports that a named gameplay event occurred (e.g. "EnemyKilled"), adding
        /// <paramref name="amount"/> to its running counter and re-evaluating achievements.
        /// This is the generic hook: any <see cref="AchievementDefinition"/> with
        /// <c>statKey == CustomEvent</c> and a matching <c>eventKey</c> reacts to it without
        /// needing new C# code. Typically called via <see cref="AchievementEventHook"/> wired to
        /// a UnityEvent (e.g. Health.OnDeath) in the Inspector.
        /// </summary>
        public void ReportEvent(string eventKey, float amount = 1f)
        {
            if (string.IsNullOrEmpty(eventKey)) return;

            _saveSystem.Current.AddEventCounter(eventKey, amount);
            EvaluateAll();
        }
    }
}
