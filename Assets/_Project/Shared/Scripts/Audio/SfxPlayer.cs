using SpieleMarmelade.Core.Managers;

namespace SpieleMarmelade.Shared.Audio
{
    // Centralizes the dual-mode-safe "look up AudioManager, no-op if none / id empty" lookup so
    // gameplay components can just call SfxPlayer.Play(id) instead of repeating the null-chain
    // everywhere. Safe in a minigame scene opened directly without Boot/GameManager.
    public static class SfxPlayer
    {
        // Normally the Boot scene's GameManager owns audio. When a minigame scene is played on its
        // own there is no GameManager, so fall back to whatever StandaloneAudioBootstrap spun up -
        // without it, every call here would silently do nothing and the scene would just be mute.
        private static AudioManager Manager
        {
            get
            {
                AudioManager fromGameManager = GameManager.Instance != null ? GameManager.Instance.AudioManager : null;
                return fromGameManager != null ? fromGameManager : StandaloneAudioBootstrap.StandaloneAudioManager;
            }
        }

        public static void Play(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            Manager?.PlaySfx(sfxId);
        }

        public static void PlayUi(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            Manager?.PlayUi(sfxId);
        }

        public static void PlayMusic(string sfxId, bool restartIfAlreadyPlaying = false)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            Manager?.PlayMusic(sfxId, restartIfAlreadyPlaying);
        }

        public static void PlayAmbient(string sfxId, bool restartIfAlreadyPlaying = false)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            Manager?.PlayAmbient(sfxId, restartIfAlreadyPlaying);
        }
    }
}
