using SpieleMarmelade.Shared.World;
using UnityEngine;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Wires an Options panel's slider/toggle widgets to MenuSettingsBridge. Attached to the
    // generated Options screen panel by the Menu Flow Editor. Everything here is brick-built on
    // the MenuStageRoot now (BrickSliderTrack, IconToggleButton) — no uGUI widgets left at all
    // for Options, so there's nothing left to keep aligned with a separate 2D counterpart.
    public class OptionsPanelController : MonoBehaviour
    {
        [SerializeField] private BrickSliderTrack masterVolumeSlider;
        [SerializeField] private BrickSliderTrack musicVolumeSlider;
        [SerializeField] private BrickSliderTrack sfxVolumeSlider;
        [SerializeField] private IconToggleButton fullscreenButton;

        private void OnEnable()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValue(MenuSettingsBridge.GetMasterVolume(), notify: false);
                masterVolumeSlider.OnValueChanged.AddListener(MenuSettingsBridge.SetMasterVolume);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValue(MenuSettingsBridge.GetMusicVolume(), notify: false);
                musicVolumeSlider.OnValueChanged.AddListener(MenuSettingsBridge.SetMusicVolume);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValue(MenuSettingsBridge.GetSfxVolume(), notify: false);
                sfxVolumeSlider.OnValueChanged.AddListener(MenuSettingsBridge.SetSfxVolume);
            }
            if (fullscreenButton != null)
            {
                fullscreenButton.SetIsOn(MenuSettingsBridge.GetFullscreen(), notify: false);
                fullscreenButton.OnValueChanged.AddListener(MenuSettingsBridge.SetFullscreen);
            }
        }

        private void OnDisable()
        {
            masterVolumeSlider?.OnValueChanged.RemoveListener(MenuSettingsBridge.SetMasterVolume);
            musicVolumeSlider?.OnValueChanged.RemoveListener(MenuSettingsBridge.SetMusicVolume);
            sfxVolumeSlider?.OnValueChanged.RemoveListener(MenuSettingsBridge.SetSfxVolume);
            fullscreenButton?.OnValueChanged.RemoveListener(MenuSettingsBridge.SetFullscreen);
        }
    }
}
