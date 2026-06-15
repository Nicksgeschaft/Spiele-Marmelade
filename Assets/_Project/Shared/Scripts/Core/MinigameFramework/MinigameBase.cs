using UnityEngine;

namespace GameJamUniverse.Core.Minigames
{
    /// <summary>
    /// Convenience base class for minigame entry points. Implements the plumbing of
    /// <see cref="IMinigame"/> and exposes small protected hooks (OnStartGame, OnPause, ...) so
    /// concrete minigames only override what they need.
    /// </summary>
    public abstract class MinigameBase : MonoBehaviour, IMinigame
    {
        [SerializeField] private MinigameMetadata metadata;

        public MinigameMetadata Metadata => metadata;
        protected MinigameContext Context { get; private set; }
        protected bool IsPaused { get; private set; }

        public virtual void Initialize(MinigameContext context)
        {
            Context = context;
            OnInitialize();
        }

        public virtual void StartGame() => OnStartGame();

        public virtual void PauseGame()
        {
            IsPaused = true;
            OnPause();
        }

        public virtual void ResumeGame()
        {
            IsPaused = false;
            OnResume();
        }

        public virtual void RestartGame() => OnRestart();

        public virtual void QuitGame()
        {
            OnQuit();
            Context?.ReturnToHub();
        }

        public virtual void SaveProgress() => OnSaveProgress();

        public virtual void LoadProgress() => OnLoadProgress();

        public virtual void UnloadGame() => OnUnload();

        protected virtual void OnInitialize() { }
        protected virtual void OnStartGame() { }
        protected virtual void OnPause() { }
        protected virtual void OnResume() { }
        protected virtual void OnRestart() { }
        protected virtual void OnQuit() { }
        protected virtual void OnSaveProgress() { }
        protected virtual void OnLoadProgress() { }
        protected virtual void OnUnload() { }
    }
}
