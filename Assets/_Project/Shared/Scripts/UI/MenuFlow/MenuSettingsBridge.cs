using System;
using SpieleMarmelade.Core.Managers;
using SpieleMarmelade.Core.SaveSystem;
using UnityEngine;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Reads/writes audio + display settings through the real SaveSystem/AudioManager when
    // GameManager is present (Hub flow — values persist app-wide, same as HubUIController's
    // Settings tab), and falls back to PlayerPrefs + direct engine APIs when it isn't (a
    // minigame scene opened and played standalone, without Boot/Hub loaded — the common
    // fast-iteration workflow during a jam). Every Options panel should go through here
    // instead of touching GameManager/SaveSystem directly.
    public static class MenuSettingsBridge
    {
        private const string MasterKey     = "GJU_MasterVolume";
        private const string MusicKey      = "GJU_MusicVolume";
        private const string SfxKey        = "GJU_SfxVolume";
        private const string FullscreenKey = "GJU_Fullscreen";

        public static float GetMasterVolume() =>
            GameManager.Instance != null
                ? GameManager.Instance.SaveSystem.Current.settings.masterVolume
                : PlayerPrefs.GetFloat(MasterKey, 1f);

        public static float GetMusicVolume() =>
            GameManager.Instance != null
                ? GameManager.Instance.SaveSystem.Current.settings.musicVolume
                : PlayerPrefs.GetFloat(MusicKey, 0.8f);

        public static float GetSfxVolume() =>
            GameManager.Instance != null
                ? GameManager.Instance.SaveSystem.Current.settings.sfxVolume
                : PlayerPrefs.GetFloat(SfxKey, 1f);

        public static bool GetFullscreen() =>
            GameManager.Instance != null
                ? GameManager.Instance.SaveSystem.Current.settings.fullscreen
                : PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        public static void SetMasterVolume(float value) => Apply(MasterKey, value, s => s.masterVolume = value);
        public static void SetMusicVolume(float value)  => Apply(MusicKey, value, s => s.musicVolume = value);
        public static void SetSfxVolume(float value)    => Apply(SfxKey, value, s => s.sfxVolume = value);

        public static void SetFullscreen(bool value)
        {
            Screen.fullScreen = value;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveSystem.Current.settings.fullscreen = value;
                GameManager.Instance.SaveSystem.Save();
            }
            else
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private static void Apply(string prefsKey, float value, Action<SettingsData> setter)
        {
            if (GameManager.Instance != null)
            {
                setter(GameManager.Instance.SaveSystem.Current.settings);
                GameManager.Instance.SaveSystem.Save();
                GameManager.Instance.AudioManager.ApplyVolumes();
            }
            else
            {
                // No AudioManager to route separate Music/SFX buses through standalone — the
                // values are still saved so they take effect once this runs under GameManager.
                PlayerPrefs.SetFloat(prefsKey, value);
                PlayerPrefs.Save();
                AudioListener.volume = GetMasterVolume();
            }
        }
    }
}
