using GameJamUniverse.Core.Managers;
using UnityEngine;

namespace GameJamUniverse.Shared.Audio
{
    public enum AudioChannel
    {
        Sfx,
        Ui,
        Music,
        Ambient
    }

    // No-code sound wiring: drop on any GameObject, set sfxId, then hook Play() up to any
    // existing UnityEvent (Health.OnDamaged, LevelExitTrigger.OnPlayerReached, Button.onClick, ...).
    // Safe to use in a minigame scene opened directly without Boot/GameManager (no-ops instead of throwing).
    public class SfxTrigger : MonoBehaviour
    {
        [SerializeField] private string sfxId;
        [SerializeField] private AudioChannel channel = AudioChannel.Sfx;

        /// <summary>Plays the configured sfxId on the configured channel. Wire this to a UnityEvent in the Inspector.</summary>
        public void Play() => PlayId(sfxId);

        /// <summary>Plays an id supplied at call time instead of the configured sfxId (for dynamic sounds from code).</summary>
        public void PlayId(string id)
        {
            AudioManager audio = GameManager.Instance?.AudioManager;
            if (audio == null || string.IsNullOrEmpty(id)) return;

            switch (channel)
            {
                case AudioChannel.Ui: audio.PlayUi(id); break;
                case AudioChannel.Music: audio.PlayMusic(id); break;
                case AudioChannel.Ambient: audio.PlayAmbient(id); break;
                default: audio.PlaySfx(id); break;
            }
        }
    }
}
