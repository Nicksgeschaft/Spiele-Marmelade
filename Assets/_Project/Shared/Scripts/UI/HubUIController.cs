using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SpieleMarmelade.Core.Managers;
using SpieleMarmelade.Core.Minigames;
using SpieleMarmelade.Core.Progression;
using SpieleMarmelade.Core.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace SpieleMarmelade.Shared.UI
{
    /// <summary>
    /// Top-level Hub UI: a tab bar over a stack of section panels.
    /// </summary>
    public class HubUIController : MonoBehaviour
    {
        private enum Tab
        {
            Play, New, Favorites, Recent, Challenges, Achievements, Statistics, Collection, Settings, Credits
        }

        [Header("Tabs (Play, New, Favorites, Recent, Challenges, Achievements, Statistics, Collection, Settings, Credits)")]
        [SerializeField] private Button[] tabButtons = new Button[10];

        [Header("Panels (same order as tabs)")]
        [SerializeField] private GameObject[] tabPanels = new GameObject[10];

        [Header("Play Panel")]
        [SerializeField] private Transform gameCardContainer;
        [SerializeField] private GameCardView gameCardPrefab;

        [Header("New / Favorites / Recent / Collection Panels")]
        [SerializeField] private Transform newGameCardContainer;
        [SerializeField] private Transform favoritesGameCardContainer;
        [SerializeField] private Transform recentGameCardContainer;
        [SerializeField] private Transform collectionGameCardContainer;

        [Header("Challenges / Achievements Panels")]
        [SerializeField] private Transform challengesCardContainer;
        [SerializeField] private Transform achievementsCardContainer;
        [SerializeField] private ProgressCardView progressCardPrefab;

        [Header("Statistics Panel")]
        [SerializeField] private Text statisticsText;

        [Header("Settings Panel")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider uiVolumeSlider;
        [SerializeField] private Slider ambientVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle controllerVibrationToggle;

        private void Awake()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                if (tabButtons[i] != null)
                {
                    tabButtons[i].onClick.AddListener(() => ShowTab(index));
                }
            }

            BindSettingsControls();
        }

        private void OnEnable()
        {
            PopulatePlayPanel();
            RefreshStatistics();
            ShowTab((int)Tab.Play);
        }

        private void ShowTab(int index)
        {
            for (int i = 0; i < tabPanels.Length; i++)
            {
                if (tabPanels[i] != null)
                {
                    tabPanels[i].SetActive(i == index);
                }
            }

            switch ((Tab)index)
            {
                case Tab.New: PopulateNewPanel(); break;
                case Tab.Favorites: PopulateFavoritesPanel(); break;
                case Tab.Recent: PopulateRecentPanel(); break;
                case Tab.Challenges: PopulateChallengesPanel(); break;
                case Tab.Achievements: PopulateAchievementsPanel(); break;
                case Tab.Statistics: RefreshStatistics(); break;
                case Tab.Collection: PopulateCollectionPanel(); break;
                case Tab.Settings: PopulateSettingsPanel(); break;
            }
        }

        private void PopulatePlayPanel()
        {
            MinigameRegistry registry = GameManager.Instance != null ? GameManager.Instance.MinigameRegistry : null;
            IEnumerable<MinigameMetadata> games = registry != null ? registry.Games : Enumerable.Empty<MinigameMetadata>();
            PopulateGameCardPanel(gameCardContainer, games, showNewBadgeIfLocked: false, showStats: false);
        }

        private void PopulateNewPanel()
        {
            MinigameRegistry registry = GameManager.Instance != null ? GameManager.Instance.MinigameRegistry : null;
            SaveData save = GameManager.Instance != null ? GameManager.Instance.SaveSystem.Current : null;

            IEnumerable<MinigameMetadata> games = (registry != null && save != null)
                ? registry.Games.Where(g => g != null && !save.IsGameUnlocked(g.gameId))
                : Enumerable.Empty<MinigameMetadata>();

            PopulateGameCardPanel(newGameCardContainer, games, showNewBadgeIfLocked: true, showStats: false);
        }

        private void PopulateFavoritesPanel()
        {
            MinigameRegistry registry = GameManager.Instance != null ? GameManager.Instance.MinigameRegistry : null;
            SaveData save = GameManager.Instance != null ? GameManager.Instance.SaveSystem.Current : null;

            IEnumerable<MinigameMetadata> games = (registry != null && save != null)
                ? registry.Games.Where(g => g != null && save.IsFavorite(g.gameId))
                : Enumerable.Empty<MinigameMetadata>();

            PopulateGameCardPanel(favoritesGameCardContainer, games, showNewBadgeIfLocked: false, showStats: false);
        }

        private void PopulateRecentPanel()
        {
            MinigameRegistry registry = GameManager.Instance != null ? GameManager.Instance.MinigameRegistry : null;
            SaveData save = GameManager.Instance != null ? GameManager.Instance.SaveSystem.Current : null;

            List<MinigameMetadata> games = new();
            if (registry != null && save != null)
            {
                foreach (string gameId in save.recentGameIds)
                {
                    MinigameMetadata metadata = registry.FindById(gameId);
                    if (metadata != null) games.Add(metadata);
                }
            }

            PopulateGameCardPanel(recentGameCardContainer, games, showNewBadgeIfLocked: false, showStats: false);
        }

        private void PopulateCollectionPanel()
        {
            MinigameRegistry registry = GameManager.Instance != null ? GameManager.Instance.MinigameRegistry : null;
            IEnumerable<MinigameMetadata> games = registry != null ? registry.Games : Enumerable.Empty<MinigameMetadata>();
            PopulateGameCardPanel(collectionGameCardContainer, games, showNewBadgeIfLocked: false, showStats: true);
        }

        private void PopulateGameCardPanel(Transform container, IEnumerable<MinigameMetadata> games, bool showNewBadgeIfLocked, bool showStats)
        {
            ClearContainer(container);
            if (container == null || gameCardPrefab == null) return;

            SaveData save = GameManager.Instance != null ? GameManager.Instance.SaveSystem.Current : null;

            foreach (MinigameMetadata metadata in games)
            {
                if (metadata == null) continue;

                bool isFavorite = save != null && save.IsFavorite(metadata.gameId);
                bool showNewBadge = showNewBadgeIfLocked && save != null && !save.IsGameUnlocked(metadata.gameId);
                string statsLine = showStats && save != null ? BuildStatsLine(save, metadata) : null;

                GameCardView card = Instantiate(gameCardPrefab, container);
                card.Bind(metadata, OnPlayRequested, isFavorite, OnToggleFavorite, showNewBadge, statsLine);
            }
        }

        private static string BuildStatsLine(SaveData save, MinigameMetadata metadata)
        {
            string status = save.IsGameUnlocked(metadata.gameId) ? "Unlocked" : "Locked";
            int highscore = save.GetHighscore(metadata.gameId);
            float bestTime = save.GetBestTime(metadata.gameId);
            float playtime = save.GetPlaytime(metadata.gameId);
            return $"{status}   Highscore: {highscore}   Best: {bestTime:F1}s   Playtime: {playtime:F0}s";
        }

        private void OnPlayRequested(MinigameMetadata metadata)
        {
            GameManager.Instance?.RequestStartGame(metadata);
        }

        private void OnToggleFavorite(MinigameMetadata metadata, bool isFavorite)
        {
            if (GameManager.Instance == null) return;

            GameManager.Instance.SaveSystem.Current.SetFavorite(metadata.gameId, isFavorite);
            GameManager.Instance.SaveSystem.Save();
        }

        private void PopulateChallengesPanel()
        {
            ClearContainer(challengesCardContainer);
            if (challengesCardContainer == null || progressCardPrefab == null || GameManager.Instance == null) return;

            ChallengeManager manager = GameManager.Instance.ChallengeManager;
            foreach (ChallengeDefinition def in manager.Definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.challengeId)) continue;

                ChallengeProgressEntry progress = manager.GetProgress(def.challengeId);
                string status = progress.completed ? $"Completed (+{def.rewardCoins} coins)" : "Active";

                ProgressCardView card = Instantiate(progressCardPrefab, challengesCardContainer);
                card.Bind($"{def.type}: {def.metric}", def.description, progress.currentValue, def.targetValue, progress.completed, status);
            }
        }

        private void PopulateAchievementsPanel()
        {
            ClearContainer(achievementsCardContainer);
            if (achievementsCardContainer == null || progressCardPrefab == null || GameManager.Instance == null) return;

            MinigameRegistry registry = GameManager.Instance.MinigameRegistry;
            int registeredGames = registry != null ? Mathf.Max(1, registry.Games.Count) : 1;

            AchievementManager manager = GameManager.Instance.AchievementManager;
            foreach (AchievementDefinition def in manager.Definitions)
            {
                if (def == null || string.IsNullOrEmpty(def.achievementId)) continue;

                AchievementProgressEntry progress = manager.GetProgress(def.achievementId);
                float target = (def.statKey == AchievementStatKey.AllGamesCompleted || def.statKey == AchievementStatKey.AllGamesUnlocked)
                    ? registeredGames
                    : def.targetValue;

                string status = progress.unlocked ? $"Unlocked {FormatDate(progress.unlockedAtUtc)}" : "Locked";

                ProgressCardView card = Instantiate(progressCardPrefab, achievementsCardContainer);
                card.Bind(def.title, def.description, progress.currentValue, target, progress.unlocked, status);
            }
        }

        private static string FormatDate(string utcIso)
        {
            return DateTimeOffset.TryParse(utcIso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
                ? parsed.ToLocalTime().ToString("yyyy-MM-dd")
                : "";
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void RefreshStatistics()
        {
            if (statisticsText == null || GameManager.Instance == null) return;

            StatisticsData stats = GameManager.Instance.SaveSystem.Current.statistics;
            statisticsText.text =
                $"Sessions: {stats.totalSessions}\n" +
                $"Games Played: {stats.gamesPlayed}\n" +
                $"Games Completed: {stats.gamesCompleted}\n" +
                $"Favorite Genre: {stats.favoriteGenre}\n" +
                $"Total Playtime: {stats.totalPlaytimeSeconds:F0}s\n" +
                $"Total Score: {stats.totalScore}\n" +
                $"Wins: {stats.wins}   Losses: {stats.losses}\n" +
                $"Achievements Unlocked: {stats.achievementsUnlocked}";
        }

        private void BindSettingsControls()
        {
            BindVolumeSlider(masterVolumeSlider, s => s.masterVolume, (s, v) => s.masterVolume = v);
            BindVolumeSlider(musicVolumeSlider, s => s.musicVolume, (s, v) => s.musicVolume = v);
            BindVolumeSlider(sfxVolumeSlider, s => s.sfxVolume, (s, v) => s.sfxVolume = v);
            BindVolumeSlider(uiVolumeSlider, s => s.uiVolume, (s, v) => s.uiVolume = v);
            BindVolumeSlider(ambientVolumeSlider, s => s.ambientVolume, (s, v) => s.ambientVolume = v);

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(value =>
                {
                    ApplySettingsChange(s => s.fullscreen = value);
                    Screen.fullScreen = value;
                });
            }

            if (controllerVibrationToggle != null)
            {
                controllerVibrationToggle.onValueChanged.AddListener(value =>
                    ApplySettingsChange(s => s.controllerVibration = value));
            }
        }

        private void BindVolumeSlider(Slider slider, System.Func<SettingsData, float> getter, System.Action<SettingsData, float> setter)
        {
            if (slider == null) return;

            slider.onValueChanged.AddListener(value =>
            {
                ApplySettingsChange(s => setter(s, value));
                GameManager.Instance?.AudioManager.ApplyVolumes();
            });
        }

        private void ApplySettingsChange(System.Action<SettingsData> apply)
        {
            if (GameManager.Instance == null) return;

            apply(GameManager.Instance.SaveSystem.Current.settings);
            GameManager.Instance.SaveSystem.Save();
        }

        private void PopulateSettingsPanel()
        {
            if (GameManager.Instance == null) return;

            SettingsData settings = GameManager.Instance.SaveSystem.Current.settings;

            masterVolumeSlider?.SetValueWithoutNotify(settings.masterVolume);
            musicVolumeSlider?.SetValueWithoutNotify(settings.musicVolume);
            sfxVolumeSlider?.SetValueWithoutNotify(settings.sfxVolume);
            uiVolumeSlider?.SetValueWithoutNotify(settings.uiVolume);
            ambientVolumeSlider?.SetValueWithoutNotify(settings.ambientVolume);
            fullscreenToggle?.SetIsOnWithoutNotify(settings.fullscreen);
            controllerVibrationToggle?.SetIsOnWithoutNotify(settings.controllerVibration);
        }
    }
}
