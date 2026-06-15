using GameJamUniverse.Core.SaveSystem;
using UnityEngine;

namespace GameJamUniverse.Core.Managers
{
    /// <summary>
    /// Owns the four audio channels (Music, SFX, UI, Ambient) and exposes simple
    /// id-based playback backed by an <see cref="AudioLibrary"/>. Volumes are driven by
    /// <see cref="SettingsData"/>. An AudioMixer with matching groups can be layered in later
    /// for ducking/snapshots without changing this API.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioLibrary sharedLibrary;

        private AudioLibrary _activeLibrary;
        private AudioSource _music;
        private AudioSource _ambient;
        private AudioSource _sfx;
        private AudioSource _ui;
        private SettingsData _settings;

        public void Initialize(SettingsData settings)
        {
            _settings = settings;
            _activeLibrary = sharedLibrary;

            _music = CreateSource("MusicSource", loop: true);
            _ambient = CreateSource("AmbientSource", loop: true);
            _sfx = CreateSource("SfxSource", loop: false);
            _ui = CreateSource("UiSource", loop: false);

            ApplyVolumes();
        }

        /// <summary>Swaps in a minigame-specific library; pass null to fall back to the shared one.</summary>
        public void PushLibrary(AudioLibrary minigameLibrary)
        {
            _activeLibrary = minigameLibrary != null ? minigameLibrary : sharedLibrary;
        }

        public void ApplyVolumes()
        {
            if (_settings == null) return;

            _music.volume = _settings.masterVolume * _settings.musicVolume;
            _ambient.volume = _settings.masterVolume * _settings.ambientVolume;
            _sfx.volume = _settings.masterVolume * _settings.sfxVolume;
            _ui.volume = _settings.masterVolume * _settings.uiVolume;
        }

        public void PlayMusic(string id, bool restartIfAlreadyPlaying = false)
        {
            AudioClip clip = _activeLibrary != null ? _activeLibrary.FindMusic(id) : null;
            if (clip == null) return;
            if (!restartIfAlreadyPlaying && _music.isPlaying && _music.clip == clip) return;

            _music.clip = clip;
            _music.Play();
        }

        public void StopMusic() => _music.Stop();

        public void PlayAmbient(string id, bool restartIfAlreadyPlaying = false)
        {
            AudioClip clip = _activeLibrary != null ? _activeLibrary.FindAmbient(id) : null;
            if (clip == null) return;
            if (!restartIfAlreadyPlaying && _ambient.isPlaying && _ambient.clip == clip) return;

            _ambient.clip = clip;
            _ambient.Play();
        }

        public void StopAmbient() => _ambient.Stop();

        public void PlaySfx(string id)
        {
            AudioClip clip = _activeLibrary != null ? _activeLibrary.FindSfx(id) : null;
            if (clip != null) _sfx.PlayOneShot(clip);
        }

        public void PlayUi(string id)
        {
            AudioClip clip = _activeLibrary != null ? _activeLibrary.FindUi(id) : null;
            if (clip != null) _ui.PlayOneShot(clip);
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            return source;
        }
    }
}
