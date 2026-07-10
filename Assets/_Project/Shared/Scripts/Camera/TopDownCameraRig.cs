using UnityEngine;

namespace GameJamUniverse.Shared.Cameras
{
    // Top-down follow camera shared by GridMovement and TopDownFreeMovement: stays above the
    // target at a fixed offset/angle, following X/Z position.
    public class TopDownCameraRig : MonoBehaviour, ICameraRig
    {
        // Offset tuned for GameJam Universe's brick scale (1 brick ≈ 0.0795 world units).
        [SerializeField] private Vector3 offset = new(0f, 0.9f, -0.35f);
        [SerializeField] private float   smoothTime = 0.15f;

        private Transform _target;
        private Vector3   _velocity;

        public void Init(Transform target) => _target = target;

        // Falls back to whatever is tagged "Player" so this rig also works if it's dropped
        // into a scene by hand instead of via the New Game wizard.
        private void Awake()
        {
            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _target = player.transform;
            }
        }

        public void LateUpdateFollow()
        {
            if (_target == null) return;
            var desired = _target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
            transform.LookAt(_target.position, Vector3.up);
        }

        private void LateUpdate() => LateUpdateFollow();
    }
}
