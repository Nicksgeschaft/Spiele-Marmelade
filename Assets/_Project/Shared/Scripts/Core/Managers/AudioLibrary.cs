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
        }

        public List<Entry> music = new();
        public List<Entry> sfx = new();
        public List<Entry> ui = new();
        public List<Entry> ambient = new();

        public AudioClip FindMusic(string id) => Find(music, id);
        public AudioClip FindSfx(string id) => Find(sfx, id);
        public AudioClip FindUi(string id) => Find(ui, id);
        public AudioClip FindAmbient(string id) => Find(ambient, id);

        private static AudioClip Find(List<Entry> entries, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == id)
                {
                    return entries[i].clip;
                }
            }
            return null;
        }
    }
}
