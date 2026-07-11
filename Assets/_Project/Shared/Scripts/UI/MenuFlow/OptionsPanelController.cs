using UnityEngine;
using UnityEngine.UI;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Wires an Options panel's slider/toggle widgets to MenuSettingsBridge. Attached to the
    // generated Options screen panel by the Menu Flow Editor.
    public class OptionsPanelController : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle fullscreenToggle;

        private void OnEnable()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(MenuSettingsBridge.GetMasterVolume());
                masterVolumeSlider.onValueChanged.AddListener(MenuSettingsBridge.SetMasterVolume);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(MenuSettingsBridge.GetMusicVolume());
                musicVolumeSlider.onValueChanged.AddListener(MenuSettingsBridge.SetMusicVolume);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(MenuSettingsBridge.GetSfxVolume());
                sfxVolumeSlider.onValueChanged.AddListener(MenuSettingsBridge.SetSfxVolume);
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(MenuSettingsBridge.GetFullscreen());
                fullscreenToggle.onValueChanged.AddListener(MenuSettingsBridge.SetFullscreen);
            }
        }

        private void OnDisable()
        {
            masterVolumeSlider?.onValueChanged.RemoveListener(MenuSettingsBridge.SetMasterVolume);
            musicVolumeSlider?.onValueChanged.RemoveListener(MenuSettingsBridge.SetMusicVolume);
            sfxVolumeSlider?.onValueChanged.RemoveListener(MenuSettingsBridge.SetSfxVolume);
            fullscreenToggle?.onValueChanged.RemoveListener(MenuSettingsBridge.SetFullscreen);
        }
    }
}
