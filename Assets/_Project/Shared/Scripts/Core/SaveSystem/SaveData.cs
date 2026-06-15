using System;
using System.Collections.Generic;

namespace GameJamUniverse.Core.SaveSystem
{
    [Serializable]
    public struct StringIntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct StringFloatEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class AchievementProgressEntry
    {
        public string achievementId;
        public float currentValue;
        public bool unlocked;
        public string unlockedAtUtc;
    }

    [Serializable]
    public class ChallengeProgressEntry
    {
        public string challengeId;
        public float currentValue;
        public bool completed;
        public string expiresAtUtc;
    }

    /// <summary>
    /// Root save container. JsonUtility-friendly: dictionaries are represented as lists of
    /// key/value entries so the whole graph can round-trip through JsonUtility without extra
    /// package dependencies. Bump <see cref="CurrentVersion"/> and extend
    /// <see cref="SaveMigrator"/> whenever the schema changes.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        public PlayerProfileData profile = new();
        public SettingsData settings = new();
        public StatisticsData statistics = new();

        public List<string> unlockedGameIds = new();
        public List<string> favoriteGameIds = new();
        public List<string> recentGameIds = new();
        public List<string> completedChallengeIds = new();
        public List<string> playedGenres = new();

        public List<AchievementProgressEntry> achievements = new();
        public List<ChallengeProgressEntry> challenges = new();
        public List<StringFloatEntry> playtimePerGameSeconds = new();
        public List<StringIntEntry> highscores = new();
        public List<StringFloatEntry> bestTimesSeconds = new();

        public const int MaxRecentGames = 10;

        public bool IsGameUnlocked(string gameId) => unlockedGameIds.Contains(gameId);

        public void UnlockGame(string gameId)
        {
            if (!unlockedGameIds.Contains(gameId))
            {
                unlockedGameIds.Add(gameId);
            }
        }

        public bool IsFavorite(string gameId) => favoriteGameIds.Contains(gameId);

        public void SetFavorite(string gameId, bool isFavorite)
        {
            if (isFavorite)
            {
                if (!favoriteGameIds.Contains(gameId))
                {
                    favoriteGameIds.Add(gameId);
                }
            }
            else
            {
                favoriteGameIds.Remove(gameId);
            }
        }

        public void AddRecentGame(string gameId)
        {
            recentGameIds.Remove(gameId);
            recentGameIds.Insert(0, gameId);
            while (recentGameIds.Count > MaxRecentGames)
            {
                recentGameIds.RemoveAt(recentGameIds.Count - 1);
            }
        }

        public int GetHighscore(string gameId)
        {
            int index = highscores.FindIndex(e => e.key == gameId);
            return index >= 0 ? highscores[index].value : 0;
        }

        public void SetHighscore(string gameId, int score)
        {
            int index = highscores.FindIndex(e => e.key == gameId);
            if (index >= 0)
            {
                if (score <= highscores[index].value) return;
                highscores[index] = new StringIntEntry { key = gameId, value = score };
            }
            else
            {
                highscores.Add(new StringIntEntry { key = gameId, value = score });
            }
        }

        public float GetBestTime(string gameId)
        {
            int index = bestTimesSeconds.FindIndex(e => e.key == gameId);
            return index >= 0 ? bestTimesSeconds[index].value : 0f;
        }

        public void SetBestTime(string gameId, float seconds)
        {
            int index = bestTimesSeconds.FindIndex(e => e.key == gameId);
            if (index >= 0)
            {
                if (bestTimesSeconds[index].value > 0f && seconds >= bestTimesSeconds[index].value) return;
                bestTimesSeconds[index] = new StringFloatEntry { key = gameId, value = seconds };
            }
            else
            {
                bestTimesSeconds.Add(new StringFloatEntry { key = gameId, value = seconds });
            }
        }

        public float GetPlaytime(string gameId)
        {
            int index = playtimePerGameSeconds.FindIndex(e => e.key == gameId);
            return index >= 0 ? playtimePerGameSeconds[index].value : 0f;
        }

        public void AddPlaytime(string gameId, float seconds)
        {
            int index = playtimePerGameSeconds.FindIndex(e => e.key == gameId);
            if (index >= 0)
            {
                StringFloatEntry entry = playtimePerGameSeconds[index];
                entry.value += seconds;
                playtimePerGameSeconds[index] = entry;
            }
            else
            {
                playtimePerGameSeconds.Add(new StringFloatEntry { key = gameId, value = seconds });
            }
        }

        public AchievementProgressEntry GetOrCreateAchievement(string achievementId)
        {
            AchievementProgressEntry entry = achievements.Find(e => e.achievementId == achievementId);
            if (entry == null)
            {
                entry = new AchievementProgressEntry { achievementId = achievementId };
                achievements.Add(entry);
            }
            return entry;
        }

        public void RegisterGenrePlayed(string genre)
        {
            if (!playedGenres.Contains(genre))
            {
                playedGenres.Add(genre);
            }
        }

        public ChallengeProgressEntry GetOrCreateChallenge(string challengeId)
        {
            ChallengeProgressEntry entry = challenges.Find(e => e.challengeId == challengeId);
            if (entry == null)
            {
                entry = new ChallengeProgressEntry { challengeId = challengeId };
                challenges.Add(entry);
            }
            return entry;
        }
    }
}
