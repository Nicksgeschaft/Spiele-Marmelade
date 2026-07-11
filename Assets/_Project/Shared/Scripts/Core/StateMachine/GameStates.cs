using SpieleMarmelade.Core.Managers;
using UnityEngine;

namespace SpieleMarmelade.Core.StateMachine
{
    // Concrete states are intentionally thin: the actual flow (scene loading, save/load,
    // minigame lifecycle calls) is orchestrated explicitly by GameManager. States only carry
    // the small side effects that *must* happen on enter/exit (pausing time, writing the save
    // file) and exist so UI can react to GameStateMachine.StateChanged.

    public class BootState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Boot;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class LoadingState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Loading;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class HubState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Hub;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class EnteringGameState : IGameState<GameManager>
    {
        public GameState StateId => GameState.EnteringGame;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class PlayingState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Playing;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class PausedState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Paused;
        public void Enter(GameManager context) => Time.timeScale = 0f;
        public void Exit(GameManager context) => Time.timeScale = 1f;
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class GameOverState : IGameState<GameManager>
    {
        public GameState StateId => GameState.GameOver;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class VictoryState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Victory;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class ReturningToHubState : IGameState<GameManager>
    {
        public GameState StateId => GameState.ReturningToHub;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    /// <summary>Transient state: writes the save file on enter, then the caller switches back.</summary>
    public class SavingState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Saving;
        public void Enter(GameManager context) => context.SaveSystem.Save();
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }

    public class SettingsState : IGameState<GameManager>
    {
        public GameState StateId => GameState.Settings;
        public void Enter(GameManager context) { }
        public void Exit(GameManager context) { }
        public void Tick(GameManager context, float deltaTime) { }
    }
}
