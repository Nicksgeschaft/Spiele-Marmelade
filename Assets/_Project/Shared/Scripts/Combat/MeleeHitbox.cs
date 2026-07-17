using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
using UnityEngine;

namespace SpieleMarmelade.Shared.Combat
{
    // A trigger volume that deals damage to Health components it touches while active. Call
    // Activate() for a short window (e.g. during an attack swing) — each target is only hit
    // once per activation, even if it stays inside the volume for the whole window.
    //
    // Only the Collider itself is toggled, not the GameObject — this typically lives on the
    // same object as the visible weapon mesh (e.g. SwordBlade), which should stay visible and
    // animatable (see SwordSwingAnimator) at all times instead of blinking in/out with the hit
    // window. Re-enabling an already-overlapping collider reliably fires OnTriggerEnter again
    // in Unity's physics system, so no extra "already inside" bookkeeping is needed.
    [RequireComponent(typeof(Collider))]
    public class MeleeHitbox : MonoBehaviour
    {
        [SerializeField] private float damage = 20f;
        [SerializeField] private string hitSfxId;

        /// <summary>Current base damage (before any caller-supplied override). Lets attack
        /// controllers (e.g. combo multipliers) scale a known baseline instead of guessing.</summary>
        public float Damage => damage;

        private Collider _collider;
        private readonly HashSet<Health> _hitThisActivation = new();
        private float _activeUntil = -1f;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            _collider.enabled = false;
        }

        public void Activate(float duration, float? damageOverride = null)
        {
            if (damageOverride.HasValue) damage = damageOverride.Value;
            _hitThisActivation.Clear();
            _activeUntil = Time.time + duration;
            _collider.enabled = true;
        }

        private void Update()
        {
            if (_collider.enabled && Time.time >= _activeUntil) _collider.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health == null || _hitThisActivation.Contains(health)) return;

            _hitThisActivation.Add(health);
            health.TakeDamage(damage);
            SfxPlayer.Play(hitSfxId);
        }
    }
}
