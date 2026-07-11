using UnityEngine;

namespace SpieleMarmelade.Shared.Cameras
{
    // Fixed-depth follow camera for 2.5D side-scrollers: keeps a constant offset from the
    // target on X/Y, locks Z so the camera never drifts along the depth axis.
    public class SideScrollCameraRig : MonoBehaviour, ICameraRig
    {
        // Offset tuned for Spiele Marmelade's brick scale (1 brick ≈ 0.0795 world units).
        [SerializeField] private Vector3 offset = new(0f, 0.2f, -0.8f);
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
            var desired = new Vector3(_target.position.x + offset.x, _target.position.y + offset.y, offset.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        private void LateUpdate() => LateUpdateFollow();
    }
}
