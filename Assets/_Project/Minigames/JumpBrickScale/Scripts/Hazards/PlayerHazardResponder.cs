using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Turns a hazard hit into lost bricks. Sits on PlayerRoot next to PlayerAssembly and listens to
    // every UniversalHazard in the scene (lava now, spikes or moving hazards later - the hazard side
    // stays generic, all the consequences live here).
    //
    // The invulnerability window is what makes a wide hazard behave sanely: a lava pool is five
    // separate trigger segments, so walking in fires several hits within the same instant. Without a
    // cooldown that would strip several bricks at once instead of one.
    [RequireComponent(typeof(PlayerAssembly))]
    public class PlayerHazardResponder : MonoBehaviour
    {
        [Tooltip("Seconds of invulnerability after taking a hit. Covers every damage source, so " +
                 "overlapping hazard segments can't each land their own hit.")]
        [SerializeField] private float damageCooldown = 1f;

        [Tooltip("Log each hazard hit and why it was or wasn't applied.")]
        [SerializeField] private bool logHits;

        private PlayerAssembly _assembly;
        private UniversalHazard[] _hazards;
        private float _invulnerableUntil;

        /// <summary>True while the player can't take another hit.</summary>
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        private void Awake() => _assembly = GetComponent<PlayerAssembly>();

        private void Start()
        {
            // Subscribed in Start rather than Awake so hazards created during scene load exist by now.
            _hazards = FindObjectsByType<UniversalHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (UniversalHazard hazard in _hazards)
            {
                hazard.OnLavaCollision += HandleHazardHit;
            }
        }

        private void OnDestroy()
        {
            if (_hazards == null) return;
            foreach (UniversalHazard hazard in _hazards)
            {
                if (hazard != null) hazard.OnLavaCollision -= HandleHazardHit;
            }
        }

        /// <summary>Hook for hazards spawned after Start (moving/pooled ones).</summary>
        public void Register(UniversalHazard hazard)
        {
            if (hazard == null) return;
            hazard.OnLavaCollision -= HandleHazardHit;
            hazard.OnLavaCollision += HandleHazardHit;
        }

        private void HandleHazardHit(GameObject touched)
        {
            if (touched == null || IsInvulnerable) return;

            // The hazard reports whichever collider entered it, so this is the brick that actually
            // touched - not just "the player".
            BrickNode brick = touched.GetComponentInParent<BrickNode>();
            if (brick == null) return;

            // Ignore bricks belonging to some other assembly, and anything already detached.
            if (brick.GetComponentInParent<PlayerAssembly>() != _assembly) return;

            if (brick.IsMainBrick)
            {
                // Docs section 6.3: the Main-Brick is never detached - losing it is meant to be
                // PlayerDeath, which doesn't exist yet, so for now the hit is simply ignored.
                if (logHits) Debug.Log("[PlayerHazardResponder] Main-Brick hit a hazard - ignored (no death yet).", this);
                return;
            }

            _invulnerableUntil = Time.time + damageCooldown;

            if (logHits) Debug.Log($"[PlayerHazardResponder] Hazard knocked off '{brick.name}'.", this);

            // Detach handles the rest: anything cut off from the Main-Brick by this loss goes with it,
            // each piece gets its outward impulse, and they despawn on PlayerAssembly's timer.
            _assembly.Detach(brick, DetachReason.Collapse);
        }
    }
}
