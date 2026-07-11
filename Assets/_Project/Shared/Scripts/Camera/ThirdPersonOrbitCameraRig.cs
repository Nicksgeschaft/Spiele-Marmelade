using SpieleMarmelade.Shared.Combat;
using UnityEngine;

namespace SpieleMarmelade.Shared.Cameras
{
    // Simple orbit follow camera for FreeThirdPersonMovement: yaw/pitch driven by the shared
    // Look input (mouse delta / right stick), no Cinemachine. Orbits the target at a fixed
    // distance and height, pulls in on wall hits (SphereCast) so it never clips through
    // geometry, and auto-frames player+target while a LockOnController target is active.
    public class ThirdPersonOrbitCameraRig : MonoBehaviour, ICameraRig
    {
        // Distance/height tuned for Spiele Marmelade's brick scale (1 brick ≈ 0.0795 world units).
        [SerializeField] private float distance        = 0.5f;
        [SerializeField] private float height           = 0.15f;
        [SerializeField] private float lookSensitivity  = 3f;
        [SerializeField] private float minPitch         = -20f;
        [SerializeField] private float maxPitch         = 60f;
        [SerializeField] private float initialYaw       = 0f;
        [SerializeField] private float initialPitch     = 20f;

        [Header("── Wandkollision ────────────────────────")]
        [SerializeField] private float collisionRadius = 0.05f;
        [SerializeField] private float collisionBuffer = 0.05f;
        [SerializeField] private float minDistance      = 0.1f;

        [Header("── Lock-On-Framing ──────────────────────")]
        [SerializeField] private float lockOnFramingSpeed = 8f;

        private static readonly RaycastHit[] _hitsBuffer = new RaycastHit[8];

        private Transform         _target;
        private PlayerInputReader _input;
        private LockOnController  _lockOn;
        private float             _yaw;
        private float             _pitch;

        public void Init(Transform target) => _target = target;

        public void Init(Transform target, PlayerInputReader input)
        {
            _target = target;
            _input  = input;
        }

        // Falls back to whatever is tagged "Player" (and that object's own input reader) so
        // this rig also works if it's dropped into a scene by hand.
        private void Awake()
        {
            _yaw   = initialYaw;
            _pitch = initialPitch;

            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _target = player.transform;
            }
            if (_input == null && _target != null)
                _input = _target.GetComponent<PlayerInputReader>();
            if (_target != null)
                _lockOn = _target.GetComponent<LockOnController>();
        }

        public void LateUpdateFollow()
        {
            if (_target == null) return;

            Transform lockOnTarget = _lockOn != null ? _lockOn.CurrentTarget : null;
            if (lockOnTarget != null)
            {
                Vector3 toTarget = lockOnTarget.position - _target.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    float desiredYaw = Quaternion.LookRotation(toTarget, Vector3.up).eulerAngles.y;
                    _yaw = Mathf.LerpAngle(_yaw, desiredYaw, lockOnFramingSpeed * Time.deltaTime);
                }
            }
            else if (_input != null)
            {
                var look = _input.LookInput;
                _yaw   += look.x * lookSensitivity * Time.deltaTime;
                _pitch  = Mathf.Clamp(_pitch - look.y * lookSensitivity * Time.deltaTime, minPitch, maxPitch);
            }

            var rot    = Quaternion.Euler(_pitch, _yaw, 0f);
            var lookAt = _target.position + Vector3.up * height;

            transform.position = lookAt - rot * Vector3.forward * ResolveDistance(lookAt, rot);
            transform.rotation = rot;
        }

        // Pulls the camera in toward lookAt if a wall sits between it and the desired position,
        // so it never clips through brick geometry. Ignores hits on the target itself.
        private float ResolveDistance(Vector3 lookAt, Quaternion rot)
        {
            Vector3 castDir = -(rot * Vector3.forward);
            int count = Physics.SphereCastNonAlloc(lookAt, collisionRadius, castDir, _hitsBuffer, distance);

            float closest = distance;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hitsBuffer[i];
                if (hit.transform == _target || hit.transform.IsChildOf(_target)) continue;
                if (hit.distance < closest) closest = hit.distance;
            }

            return Mathf.Max(closest - collisionBuffer, minDistance);
        }

        private void LateUpdate() => LateUpdateFollow();
    }
}
