using SpieleMarmelade.Core.Managers;
using SpieleMarmelade.Core.SaveSystem;
using SpieleMarmelade.Shared.UI.MenuFlow;
using UnityEngine;

namespace SpieleMarmelade.Shared.Audio
{
    // Audio normally comes up in the Boot scene: GameManager creates the AudioManager and calls
    // Initialize, which is what actually spawns the AudioSources. Open a minigame scene directly
    // (the usual way to iterate on a level) and none of that exists - SfxPlayer's null-safe chain
    // then silently does nothing, so there is no music and no error to explain why.
    //
    // This fills that gap for standalone play only: if a GameManager is already alive, it steps
    // aside completely and changes nothing about the shipped boot flow.
    [DefaultExecutionOrder(-100)]
    public class StandaloneAudioBootstrap : MonoBehaviour
    {
        [Tooltip("Library used for the stand-in AudioManager. Point this at the same AudioLibrary the " +
                 "Boot scene uses so ids resolve identically.")]
        [SerializeField] private AudioLibrary library;

        private void Awake()
        {
            // The real boot flow is running - leave everything to it.
            if (GameManager.Instance != null) return;

            if (library == null)
            {
                Debug.LogWarning($"[StandaloneAudioBootstrap] No AudioLibrary assigned on '{name}', " +
                                 "so nothing can be resolved by id while playing this scene directly.", this);
                return;
            }

            var host = new GameObject("StandaloneAudio (editor/direct play only)");
            host.transform.SetParent(transform, false);

            var audioManager = host.AddComponent<AudioManager>();

            // Seeded from the same standalone store the Options sliders read/write (PlayerPrefs via
            // MenuSettingsBridge), so the sliders and what you actually hear agree from the start.
            StandaloneSettings = new SettingsData
            {
                masterVolume = MenuSettingsBridge.GetMasterVolume(),
                musicVolume = MenuSettingsBridge.GetMusicVolume(),
                sfxVolume = MenuSettingsBridge.GetSfxVolume(),
            };
            audioManager.Initialize(StandaloneSettings);

            // Initialize() picked up the (unassigned) serialized library, so point it at ours.
            audioManager.PushLibrary(library);

            StandaloneAudioManager = audioManager;
        }

        private void OnDestroy()
        {
            if (StandaloneAudioManager != null && StandaloneAudioManager.gameObject == gameObject)
            {
                StandaloneAudioManager = null;
                StandaloneSettings = null;
            }
        }

        /// <summary>The stand-in manager, if one had to be created. Null during a normal boot.</summary>
        public static AudioManager StandaloneAudioManager { get; private set; }

        /// <summary>Settings object backing the stand-in manager's channel volumes, so the Options
        /// sliders have something to write to while playing a scene directly.</summary>
        public static SettingsData StandaloneSettings { get; private set; }
    }
}
