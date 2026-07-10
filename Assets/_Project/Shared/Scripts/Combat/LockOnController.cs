using GameJamUniverse.Shared;
using UnityEngine;

namespace GameJamUniverse.Shared.Combat
{
    // Toggleable target-lock for third-person combat: press LockOn to snap to the nearest
    // Health-bearing enemy roughly in front of the camera; press again (or let the target die
    // or leave range) to release. FreeThirdPersonMovement and ThirdPersonOrbitCameraRig both
    // read CurrentTarget from this component (GetComponent on the same player GameObject).
    public class LockOnController : MonoBehaviour
    {
        [SerializeField] private float searchRadius = 3f;
        [SerializeField] private float maxRange = 4f;
        [SerializeField] private float searchAngle = 60f;

        [Header("── Sichtlinie ────────────────────────")]
        [SerializeField] private float eyeHeight = 0.09f;
        [Tooltip("Wie lange das Ziel verdeckt sein darf, bevor das Lock-On abbricht — kurze " +
                 "Verdeckung durch eine Ecke/einen Türrahmen soll es nicht sofort lösen.")]
        [SerializeField] private float losBreakGrace = 0.3f;

        private PlayerInputReader _input;
        private Camera _cam;
        private Health _targetHealth;
        private float _losBlockedSince = -1f;

        public Transform CurrentTarget { get; private set; }

        private void Awake() => _input = GetComponent<PlayerInputReader>();

        private void OnEnable()
        {
            if (_input != null) _input.LockOnPerformed += ToggleLockOn;
        }

        private void OnDisable()
        {
            if (_input != null) _input.LockOnPerformed -= ToggleLockOn;
            Unsubscribe();
        }

        private void Update()
        {
            if (CurrentTarget == null) return;

            if (Vector3.Distance(transform.position, CurrentTarget.position) > maxRange)
            {
                ClearTarget();
                return;
            }

            TickLineOfSight();
        }

        private void TickLineOfSight()
        {
            if (HasLineOfSight(CurrentTarget))
            {
                _losBlockedSince = -1f;
                return;
            }

            if (_losBlockedSince < 0f) _losBlockedSince = Time.time;
            if (Time.time - _losBlockedSince >= losBreakGrace) ClearTarget();
        }

        // True if nothing but the target itself (or the player's own body) sits between the
        // two eye points — same self/target-exclusion trick as ThirdPersonOrbitCameraRig's
        // wall-collision raycast, just checking for any blocker instead of the closest one.
        private bool HasLineOfSight(Transform target)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 targetPoint = target.position + Vector3.up * eyeHeight;
            Vector3 toTarget = targetPoint - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.001f) return true;

            if (!Physics.Raycast(origin, toTarget.normalized, out RaycastHit hit, distance)) return true;

            return hit.transform == target || hit.transform.IsChildOf(target) ||
                   hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        private void ToggleLockOn()
        {
            if (CurrentTarget != null)
            {
                ClearTarget();
                return;
            }

            if (_cam == null) _cam = Camera.main;
            Transform best = FindBestTarget();
            if (best != null) SetTarget(best);
        }

        private Transform FindBestTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);
            Vector3 forward = _cam != null ? _cam.transform.forward : transform.forward;

            Transform best = null;
            Health bestHealth = null;
            float bestScore = float.MaxValue;

            foreach (Collider hit in hits)
            {
                var health = hit.GetComponentInParent<Health>();
                if (health == null || health.gameObject == gameObject || health.IsDead) continue;

                Vector3 toTarget = health.transform.position - transform.position;
                float distance = toTarget.magnitude;
                if (distance < 0.001f) continue;

                float angle = Vector3.Angle(forward, toTarget);
                if (angle > searchAngle) continue;

                if (!HasLineOfSight(health.transform)) continue;

                float score = distance + angle * 0.01f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = health.transform;
                    bestHealth = health;
                }
            }

            _targetHealth = bestHealth;
            return best;
        }

        private void SetTarget(Transform target)
        {
            CurrentTarget = target;
            _targetHealth?.OnDeath.AddListener(ClearTarget);
        }

        private void ClearTarget()
        {
            Unsubscribe();
            CurrentTarget = null;
        }

        private void Unsubscribe()
        {
            _targetHealth?.OnDeath.RemoveListener(ClearTarget);
            _targetHealth = null;
        }
    }
}
