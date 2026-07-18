using SpieleMarmelade.Core.SaveSystem;
using UnityEngine;

namespace SpieleMarmelade.Core.Managers
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

        // Per-clip trim of whatever is currently on the looping channels. Kept as state because
        // ApplyVolumes() recomputes those channels from scratch whenever a slider moves - without
        // remembering it, dragging a volume slider would silently wipe the clip's own trim.
        private float _musicClipVolume = 1f;
        private float _ambientClipVolume = 1f;

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

            // Looping channels fold in the current clip's own level. One-shot channels don't need it
            // here - PlaySfx/PlayUi pass it straight to PlayOneShot, which scales per sound.
            //
            // Deliberately not clamped to 1: a per-clip value above 1 is how a too-quiet track gets
            // boosted past its original level. Unity documents AudioSource.volume as 0-1, so treat
            // anything above 1 as best-effort - it can also clip. Balancing the other clips downward
            // is the safer route when a mix needs headroom.
            _music.volume = _settings.masterVolume * _settings.musicVolume * _musicClipVolume;
            _ambient.volume = _settings.masterVolume * _settings.ambientVolume * _ambientClipVolume;
            _sfx.volume = _settings.masterVolume * _settings.sfxVolume;
            _ui.volume = _settings.masterVolume * _settings.uiVolume;
        }

        public void PlayMusic(string id, bool restartIfAlreadyPlaying = false)
        {
            AudioLibrary.Entry entry = _activeLibrary != null ? _activeLibrary.FindMusicEntry(id) : null;
            if (entry == null || entry.clip == null) return;
            if (!restartIfAlreadyPlaying && _music.isPlaying && _music.clip == entry.clip) return;

            _musicClipVolume = entry.volume;
            _music.clip = entry.clip;
            ApplyVolumes();
            _music.Play();
        }

        public void StopMusic() => _music.Stop();

        public void PlayAmbient(string id, bool restartIfAlreadyPlaying = false)
        {
            AudioLibrary.Entry entry = _activeLibrary != null ? _activeLibrary.FindAmbientEntry(id) : null;
            if (entry == null || entry.clip == null) return;
            if (!restartIfAlreadyPlaying && _ambient.isPlaying && _ambient.clip == entry.clip) return;

            _ambientClipVolume = entry.volume;
            _ambient.clip = entry.clip;
            ApplyVolumes();
            _ambient.Play();
        }

        public void StopAmbient() => _ambient.Stop();

        public void PlaySfx(string id)
        {
            AudioLibrary.Entry entry = _activeLibrary != null ? _activeLibrary.FindSfxEntry(id) : null;
            if (entry != null && entry.clip != null) _sfx.PlayOneShot(entry.clip, entry.volume);
        }

        public void PlayUi(string id)
        {
            AudioLibrary.Entry entry = _activeLibrary != null ? _activeLibrary.FindUiEntry(id) : null;
            if (entry != null && entry.clip != null) _ui.PlayOneShot(entry.clip, entry.volume);
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
