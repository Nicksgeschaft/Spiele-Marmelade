using UnityEngine;
using UnityEngine.Events;

namespace GameJamUniverse.Shared.World
{
    // Generic "reached the goal" trigger — not tied to any specific genre, reusable across
    // archetypes (dungeon exit, platformer goal, etc.). Fires once when something tagged
    // "Player" enters, then disables itself so it can't fire again.
    [RequireComponent(typeof(Collider))]
    public class LevelExitTrigger : MonoBehaviour
    {
        public UnityEvent OnPlayerReached;

        private bool _fired;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (_fired || !other.CompareTag("Player")) return;

            _fired = true;
            OnPlayerReached?.Invoke();
        }
    }
}
