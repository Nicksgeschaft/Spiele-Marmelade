using UnityEngine;

namespace SpieleMarmelade.Shared.Cameras
{
    // Fixed-depth follow camera for 2.5D side-scrollers: keeps a constant offset from the
    // target on X/Y, locks Z so the camera never drifts along the depth axis. Bounds and the
    // end-of-level lock are both optional and off by default, so existing users of this rig are
    // unaffected - see CameraLockZone for the trigger that calls LockAt().
    public class SideScrollCameraRig : MonoBehaviour, ICameraRig
    {
        // Offset tuned for Spiele Marmelade's brick scale (1 brick ≈ 0.0795 world units).
        [SerializeField] private Vector3 offset = new(0f, 0.2f, -0.8f);
        [SerializeField] private float   smoothTime = 0.15f;

        [Header("Bounds (optional)")]
        [SerializeField] private bool  useBounds;
        [SerializeField] private float minX;
        [SerializeField] private float maxX;
        [SerializeField] private float minY;
        [SerializeField] private float maxY;

        private Transform _target;
        private Vector3   _velocity;
        private Transform _lockAnchor;

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

        // Locks the rig onto anchor - once locked it stays locked until Unlock() is called
        // (there's no automatic revert, matching a level-end shot that should hold even if the
        // player walks back). See CameraLockZone.
        public void LockAt(Transform anchor) => _lockAnchor = anchor;

        public void Unlock() => _lockAnchor = null;

        public void LateUpdateFollow()
        {
            Vector3 desired;
            if (_lockAnchor != null)
            {
                desired = _lockAnchor.position;
            }
            else
            {
                if (_target == null) return;
                desired = new Vector3(_target.position.x + offset.x, _target.position.y + offset.y, offset.z);
                if (useBounds)
                {
                    desired.x = Mathf.Clamp(desired.x, minX, maxX);
                    desired.y = Mathf.Clamp(desired.y, minY, maxY);
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        private void LateUpdate() => LateUpdateFollow();
    }
}
