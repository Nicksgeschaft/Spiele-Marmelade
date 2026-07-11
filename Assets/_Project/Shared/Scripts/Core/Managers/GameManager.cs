using System.Collections.Generic;
using SpieleMarmelade.Core.Minigames;
using SpieleMarmelade.Core.Progression;
using SpieleMarmelade.Core.StateMachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.Core.Managers
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) root of the framework. Lives in the Boot scene and owns
    /// every shared system. Minigames never talk to these systems directly - they go through
    /// <see cref="Minigames.MinigameContext"/>.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Registries")]
        [SerializeField] private MinigameRegistry minigameRegistry;
        [SerializeField] private List<AchievementDefinition> achievementDefinitions = new();
        [SerializeField] private List<ChallengeDefinition> challengeDefinitions = new();

        [Header("Sub-Managers")]
        [SerializeField] private SceneFlowManager sceneFlowManager;
        [SerializeField] private AudioManager audioManager;

        public static GameManager Instance { get; private set; }

        public SaveSystem.SaveSystem SaveSystem { get; private set; }
        public StatisticsManager StatisticsManager { get; private set; }
        public AchievementManager AchievementManager { get; private set; }
        public ChallengeManager ChallengeManager { get; private set; }
        public GameStateMachine<GameManager> StateMachine { get; private set; }
        public MinigameRegistry MinigameRegistry => minigameRegistry;
        public SceneFlowManager SceneFlowManager => sceneFlowManager;
        public AudioManager AudioManager => audioManager;

        private MinigameMetadata _activeMinigame;
        private IMinigame _activeMinigameInstance;
        private float _sessionStartTimeUnscaled;
        private int _sessionDeaths;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            SaveSystem = new SaveSystem.SaveSystem();
            SaveSystem.Load();

            StatisticsManager = new StatisticsManager(SaveSystem);
            AchievementManager = new AchievementManager(SaveSystem, achievementDefinitions, minigameRegistry);
            ChallengeManager = new ChallengeManager(SaveSystem, challengeDefinitions);

            StateMachine = new GameStateMachine<GameManager>();
            StateMachine.RegisterState(new BootState());
            StateMachine.RegisterState(new LoadingState());
            StateMachine.RegisterState(new HubState());
            StateMachine.RegisterState(new EnteringGameState());
            StateMachine.RegisterState(new PlayingState());
            StateMachine.RegisterState(new PausedState());
            StateMachine.RegisterState(new GameOverState());
            StateMachine.RegisterState(new VictoryState());
            StateMachine.RegisterState(new ReturningToHubState());
            StateMachine.RegisterState(new SavingState());
            StateMachine.RegisterState(new SettingsState());

            audioManager.Initialize(SaveSystem.Current.settings);
        }

        private void Start()
        {
            StateMachine.ChangeState(GameState.Boot, this);

            StatisticsManager.OnSessionStarted();
            ChallengeManager.RefreshActiveChallenges();
            AchievementManager.EvaluateAll();

            StateMachine.ChangeState(GameState.Loading, this);
            sceneFlowManager.LoadHub(() => StateMachine.ChangeState(GameState.Hub, this));
        }

        /// <summary>Begins loading and starting the given minigame.</summary>
        public void RequestStartGame(MinigameMetadata metadata)
        {
            if (metadata == null) return;

            StateMachine.ChangeState(GameState.EnteringGame, this);
            sceneFlowManager.EnterMinigame(metadata, OnMinigameSceneLoaded);
        }

        private void OnMinigameSceneLoaded(string sceneName)
        {
            _activeMinigame = minigameRegistry != null ? minigameRegistry.FindById(FindGameIdForScene(sceneName)) : null;
            _activeMinigameInstance = FindMinigameInstance(sceneName);
            _sessionStartTimeUnscaled = Time.unscaledTime;
            _sessionDeaths = 0;

            if (_activeMinigame != null)
            {
                StatisticsManager.OnGameStarted(_activeMinigame);
                SaveSystem.Current.AddRecentGame(_activeMinigame.gameId);

                var context = new MinigameContext(_activeMinigame, this);
                _activeMinigameInstance?.Initialize(context);
            }

            _activeMinigameInstance?.StartGame();
            StateMachine.ChangeState(GameState.Playing, this);
        }

        private string FindGameIdForScene(string sceneName)
        {
            if (minigameRegistry == null) return null;
            foreach (MinigameMetadata metadata in minigameRegistry.Games)
            {
                if (metadata != null && metadata.sceneName == sceneName)
                {
                    return metadata.gameId;
                }
            }
            return null;
        }

        private static IMinigame FindMinigameInstance(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid()) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                IMinigame minigame = root.GetComponentInChildren<IMinigame>(true);
                if (minigame != null) return minigame;
            }
            return null;
        }

        /// <summary>Called by the active minigame (via deaths tracking) whenever the player dies.</summary>
        public void RegisterDeath() => _sessionDeaths++;

        /// <summary>Called by the active minigame via <see cref="MinigameContext.CompleteGame"/>.</summary>
        public void OnMinigameCompleted(bool won)
        {
            if (_activeMinigame == null) return;

            float sessionSeconds = Time.unscaledTime - _sessionStartTimeUnscaled;
            int score = SaveSystem.Current.GetHighscore(_activeMinigame.gameId);

            StatisticsManager.OnGameCompleted(_activeMinigame, won, score, sessionSeconds, _sessionDeaths);
            AchievementManager.EvaluateAll();
            ChallengeManager.RegisterGameCompleted(won, score, sessionSeconds);

            StateMachine.ChangeState(won ? GameState.Victory : GameState.GameOver, this);
            TriggerSave();
            RequestReturnToHub();
        }

        /// <summary>Returns to the Hub, unloading the active minigame's scene.</summary>
        public void RequestReturnToHub()
        {
            StateMachine.ChangeState(GameState.ReturningToHub, this);

            _activeMinigameInstance?.UnloadGame();
            _activeMinigameInstance = null;
            _activeMinigame = null;

            sceneFlowManager.ReturnToHub(() => StateMachine.ChangeState(GameState.Hub, this));
        }

        /// <summary>Writes the save file via a transient <see cref="GameState.Saving"/> step.</summary>
        public void TriggerSave()
        {
            GameState previous = StateMachine.CurrentState;
            StateMachine.ChangeState(GameState.Saving, this);
            StateMachine.ChangeState(previous, this);
        }
    }
}
