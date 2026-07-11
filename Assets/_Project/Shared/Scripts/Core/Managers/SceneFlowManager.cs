using System;
using System.Collections;
using SpieleMarmelade.Core.Minigames;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.Core.Managers
{
    /// <summary>
    /// Additive scene loading/unloading for the Hub <-> Minigame flow. The Boot scene (holding
    /// the persistent GameManager) is never unloaded; Hub and minigame scenes are loaded and
    /// unloaded additively on top of it.
    /// </summary>
    public class SceneFlowManager : MonoBehaviour
    {
        public const string HubSceneName = "Hub";

        private string _currentMinigameScene;

        public event Action HubLoaded;
        public event Action<string> MinigameLoaded;
        public event Action MinigameUnloaded;

        public void LoadHub(Action onLoaded = null) => StartCoroutine(LoadHubRoutine(onLoaded));

        public void EnterMinigame(MinigameMetadata metadata, Action<string> onLoaded = null) =>
            StartCoroutine(EnterMinigameRoutine(metadata, onLoaded));

        public void ReturnToHub(Action onLoaded = null) => StartCoroutine(ReturnToHubRoutine(onLoaded));

        private IEnumerator LoadHubRoutine(Action onLoaded)
        {
            if (!IsSceneLoaded(HubSceneName))
            {
                yield return SceneManager.LoadSceneAsync(HubSceneName, LoadSceneMode.Additive);
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(HubSceneName));
            HubLoaded?.Invoke();
            onLoaded?.Invoke();
        }

        private IEnumerator EnterMinigameRoutine(MinigameMetadata metadata, Action<string> onLoaded)
        {
            yield return SceneManager.LoadSceneAsync(metadata.sceneName, LoadSceneMode.Additive);

            Scene minigameScene = SceneManager.GetSceneByName(metadata.sceneName);
            SceneManager.SetActiveScene(minigameScene);
            _currentMinigameScene = metadata.sceneName;

            if (IsSceneLoaded(HubSceneName))
            {
                yield return SceneManager.UnloadSceneAsync(HubSceneName);
            }

            MinigameLoaded?.Invoke(metadata.sceneName);
            onLoaded?.Invoke(metadata.sceneName);
        }

        private IEnumerator ReturnToHubRoutine(Action onLoaded)
        {
            if (!IsSceneLoaded(HubSceneName))
            {
                yield return SceneManager.LoadSceneAsync(HubSceneName, LoadSceneMode.Additive);
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(HubSceneName));

            if (!string.IsNullOrEmpty(_currentMinigameScene) && IsSceneLoaded(_currentMinigameScene))
            {
                yield return SceneManager.UnloadSceneAsync(_currentMinigameScene);
            }
            _currentMinigameScene = null;

            MinigameUnloaded?.Invoke();
            HubLoaded?.Invoke();
            onLoaded?.Invoke();
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
