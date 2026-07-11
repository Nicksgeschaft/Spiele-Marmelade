using System;
using UnityEngine;

namespace SpieleMarmelade.Core.SaveSystem
{
    [Serializable]
    public class SettingsData
    {
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float uiVolume = 1f;
        [Range(0f, 1f)] public float ambientVolume = 0.8f;

        public bool fullscreen = true;
        public int resolutionWidth = 1920;
        public int resolutionHeight = 1080;
        public bool controllerVibration = true;
        public string language = "en";
    }
}
