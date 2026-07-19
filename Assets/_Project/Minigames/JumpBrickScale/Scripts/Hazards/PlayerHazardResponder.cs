using System;
using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
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
        private float _invulnerableUntil;

        // The event only reports the collider that entered, not the hazard it entered - but each
        // hazard carries its own hit sound, so the handler has to know which one fired. Subscribing
        // through a per-hazard closure supplies that; the delegate is kept here because unsubscribing
        // needs the exact same instance back.
        private readonly Dictionary<UniversalHazard, Action<GameObject>> _subscriptions = new();

        /// <summary>True while the player can't take another hit.</summary>
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        private void Awake() => _assembly = GetComponent<PlayerAssembly>();

        private void Start()
        {
            // Subscribed in Start rather than Awake so hazards created during scene load exist by now.
            foreach (UniversalHazard hazard in FindObjectsByType<UniversalHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Register(hazard);
            }
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<UniversalHazard, Action<GameObject>> subscription in _subscriptions)
            {
                if (subscription.Key != null) subscription.Key.OnLavaCollision -= subscription.Value;
            }
            _subscriptions.Clear();
        }

        /// <summary>Hook for hazards spawned after Start (moving/pooled ones).</summary>
        public void Register(UniversalHazard hazard)
        {
            if (hazard == null || _subscriptions.ContainsKey(hazard)) return;

            Action<GameObject> handler = touched => HandleHazardHit(hazard, touched);
            _subscriptions[hazard] = handler;
            hazard.OnLavaCollision += handler;
        }

        private void HandleHazardHit(UniversalHazard hazard, GameObject touched)
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

            // Played here rather than in UniversalHazard so it fires once per hit that actually costs
            // a brick - a lava pool is five separate triggers, and sounding each one would stack five
            // copies of the same hiss on top of each other.
            if (hazard != null) SfxPlayer.Play(hazard.hitSfxId);

            if (logHits) Debug.Log($"[PlayerHazardResponder] Hazard knocked off '{brick.name}'.", this);

            // Detach handles the rest: anything cut off from the Main-Brick by this loss goes with it,
            // each piece gets its outward impulse, and they despawn on PlayerAssembly's timer.
            _assembly.Detach(brick, DetachReason.Collapse);
        }
    }
}
