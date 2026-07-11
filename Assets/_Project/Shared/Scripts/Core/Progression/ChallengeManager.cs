using System;
using System.Collections.Generic;
using System.Globalization;
using SpieleMarmelade.Core.SaveSystem;

namespace SpieleMarmelade.Core.Progression
{
    /// <summary>
    /// Tracks progress on <see cref="ChallengeDefinition"/>s and rotates them on a schedule
    /// based on <see cref="ChallengeType"/> (Daily = 1 day, Weekly = 7 days, Monthly = 30 days;
    /// Event/Seasonal are long-lived and rotated manually by swapping definitions).
    /// </summary>
    public class ChallengeManager
    {
        private readonly SaveSystem.SaveSystem _saveSystem;
        private readonly List<ChallengeDefinition> _definitions;

        public event Action<ChallengeDefinition> ChallengeCompleted;

        public ChallengeManager(SaveSystem.SaveSystem saveSystem, List<ChallengeDefinition> definitions)
        {
            _saveSystem = saveSystem;
            _definitions = definitions ?? new List<ChallengeDefinition>();
        }

        public IReadOnlyList<ChallengeDefinition> Definitions => _definitions;

        public ChallengeProgressEntry GetProgress(string challengeId) =>
            _saveSystem.Current.GetOrCreateChallenge(challengeId);

        /// <summary>Call once on boot/hub entry to roll over expired challenges.</summary>
        public void RefreshActiveChallenges()
        {
            foreach (ChallengeDefinition def in _definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.challengeId)) continue;
                EnsureActive(def, _saveSystem.Current.GetOrCreateChallenge(def.challengeId));
            }
        }

        /// <summary>Feeds the result of a finished minigame session into all matching challenges.</summary>
        public void RegisterGameCompleted(bool won, int scoreEarned, float sessionSeconds)
        {
            SaveData save = _saveSystem.Current;

            foreach (ChallengeDefinition def in _definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.challengeId)) continue;

                ChallengeProgressEntry progress = save.GetOrCreateChallenge(def.challengeId);
                EnsureActive(def, progress);
                if (progress.completed) continue;

                progress.currentValue += def.metric switch
                {
                    ChallengeMetric.GamesPlayed => 1f,
                    ChallengeMetric.ScoreEarned => scoreEarned,
                    ChallengeMetric.Wins => won ? 1f : 0f,
                    ChallengeMetric.PlaytimeSeconds => sessionSeconds,
                    _ => 0f
                };

                if (progress.currentValue >= def.targetValue)
                {
                    progress.completed = true;
                    save.completedChallengeIds.Add(def.challengeId);
                    save.profile.coins += def.rewardCoins;
                    ChallengeCompleted?.Invoke(def);
                }
            }
        }

        private static TimeSpan PeriodFor(ChallengeType type) => type switch
        {
            ChallengeType.Daily => TimeSpan.FromDays(1),
            ChallengeType.Weekly => TimeSpan.FromDays(7),
            ChallengeType.Monthly => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(365) // Event/Seasonal: curated manually, rarely auto-expires
        };

        private void EnsureActive(ChallengeDefinition def, ChallengeProgressEntry progress)
        {
            DateTime now = DateTime.UtcNow;
            bool expired = string.IsNullOrEmpty(progress.expiresAtUtc)
                || !DateTime.TryParse(progress.expiresAtUtc, CultureInfo.InvariantCulture,
                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime expires)
                || now >= expires;

            if (!expired) return;

            progress.currentValue = 0f;
            if (progress.completed)
            {
                progress.completed = false;
                _saveSystem.Current.completedChallengeIds.Remove(def.challengeId);
            }
            progress.expiresAtUtc = (now + PeriodFor(def.type)).ToString("o", CultureInfo.InvariantCulture);
        }
    }
}
