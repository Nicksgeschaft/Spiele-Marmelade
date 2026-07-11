using UnityEngine;

namespace SpieleMarmelade.Shared.Audio
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
            switch (channel)
            {
                case AudioChannel.Ui: SfxPlayer.PlayUi(id); break;
                case AudioChannel.Music: SfxPlayer.PlayMusic(id); break;
                case AudioChannel.Ambient: SfxPlayer.PlayAmbient(id); break;
                default: SfxPlayer.Play(id); break;
            }
        }
    }
}
