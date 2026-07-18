using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Core.Managers
{
    /// <summary>
    /// Central, named lookup of audio clips per channel. Minigames can ship their own
    /// <see cref="AudioLibrary"/> asset and pass it to <c>AudioManager.PushLibrary</c> for
    /// game-specific sounds without touching the shared library.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Spiele Marmelade/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public AudioClip clip;

            [Tooltip("Per-Clip-Lautstärke. Wird mit der Kanal-Lautstärke multipliziert, um unterschiedlich " +
                     "laut gemasterte Dateien auszugleichen. 1 = Original, darunter leiser, darüber lauter. " +
                     "Achtung: Über 1 kann übersteuern (Clipping) - im Zweifel lieber die anderen Clips " +
                     "leiser stellen, statt einen einzelnen hochzuziehen.")]
            [Range(0f, 2f)] public float volume = 1f;
        }

        public List<Entry> music = new();
        public List<Entry> sfx = new();
        public List<Entry> ui = new();
        public List<Entry> ambient = new();

        public Entry FindMusicEntry(string id) => FindEntry(music, id);
        public Entry FindSfxEntry(string id) => FindEntry(sfx, id);
        public Entry FindUiEntry(string id) => FindEntry(ui, id);
        public Entry FindAmbientEntry(string id) => FindEntry(ambient, id);

        public AudioClip FindMusic(string id) => FindEntry(music, id)?.clip;
        public AudioClip FindSfx(string id) => FindEntry(sfx, id)?.clip;
        public AudioClip FindUi(string id) => FindEntry(ui, id)?.clip;
        public AudioClip FindAmbient(string id) => FindEntry(ambient, id)?.clip;

        private static Entry FindEntry(List<Entry> entries, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == id)
                {
                    return entries[i];
                }
            }
            return null;
        }
    }
}
