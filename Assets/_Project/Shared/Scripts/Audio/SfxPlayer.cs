using SpieleMarmelade.Core.Managers;

namespace SpieleMarmelade.Shared.Audio
{
    // Centralizes the dual-mode-safe "look up AudioManager, no-op if none / id empty" lookup so
    // gameplay components can just call SfxPlayer.Play(id) instead of repeating the null-chain
    // everywhere. Safe in a minigame scene opened directly without Boot/GameManager.
    public static class SfxPlayer
    {
        public static void Play(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            GameManager.Instance?.AudioManager?.PlaySfx(sfxId);
        }

        public static void PlayUi(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            GameManager.Instance?.AudioManager?.PlayUi(sfxId);
        }

        public static void PlayMusic(string sfxId, bool restartIfAlreadyPlaying = false)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            GameManager.Instance?.AudioManager?.PlayMusic(sfxId, restartIfAlreadyPlaying);
        }

        public static void PlayAmbient(string sfxId, bool restartIfAlreadyPlaying = false)
        {
            if (string.IsNullOrEmpty(sfxId)) return;
            GameManager.Instance?.AudioManager?.PlayAmbient(sfxId, restartIfAlreadyPlaying);
        }
    }
}
