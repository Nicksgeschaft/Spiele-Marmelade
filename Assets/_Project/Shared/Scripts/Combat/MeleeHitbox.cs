using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
using UnityEngine;

namespace SpieleMarmelade.Shared.Combat
{
    // A trigger volume that deals damage to Health components it touches while active. Call
    // Activate() for a short window (e.g. during an attack swing) — each target is only hit
    // once per activation, even if it stays inside the volume for the whole window. Lives on
    // its own child GameObject (starts inactive) so toggling it doesn't affect its parent.
    // Re-enabling an already-overlapping collider reliably fires OnTriggerEnter again in
    // Unity's physics system, so no extra "already inside" bookkeeping is needed.
    [RequireComponent(typeof(Collider))]
    public class MeleeHitbox : MonoBehaviour
    {
        [SerializeField] private float damage = 20f;
        [SerializeField] private string hitSfxId;

        /// <summary>Current base damage (before any caller-supplied override). Lets attack
        /// controllers (e.g. combo multipliers) scale a known baseline instead of guessing.</summary>
        public float Damage => damage;

        private readonly HashSet<Health> _hitThisActivation = new();
        private float _activeUntil = -1f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            gameObject.SetActive(false);
        }

        public void Activate(float duration, float? damageOverride = null)
        {
            if (damageOverride.HasValue) damage = damageOverride.Value;
            _hitThisActivation.Clear();
            _activeUntil = Time.time + duration;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (Time.time >= _activeUntil) gameObject.SetActive(false);
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
