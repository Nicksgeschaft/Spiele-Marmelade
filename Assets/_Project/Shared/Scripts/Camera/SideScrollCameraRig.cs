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

        [Tooltip("Collider marking the playable frame. The camera is kept inside it and inset by half " +
                 "the visible area, so the frame's own edge bricks never come into view. Leave empty to " +
                 "use the manual min/max values below instead.")]
        [SerializeField] private Collider boundsSource;

        [SerializeField] private float minX;
        [SerializeField] private float maxX;
        [SerializeField] private float minY;
        [SerializeField] private float maxY;

        private Transform _target;
        private Vector3   _velocity;
        private Transform _lockAnchor;
        private Camera    _camera;

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
                ApplyBounds(ref desired);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        private void LateUpdate() => LateUpdateFollow();

        // Keeps the camera inside the level frame. Clamping the camera position alone isn't enough -
        // it would stop with the frame's edge already half on screen - so the limits are pulled in by
        // half of what the camera actually sees at the play plane.
        private void ApplyBounds(ref Vector3 desired)
        {
            if (!useBounds && boundsSource == null) return;

            float loX, hiX, loY, hiY;
            if (boundsSource != null)
            {
                Bounds b = boundsSource.bounds;
                loX = b.min.x; hiX = b.max.x;
                loY = b.min.y; hiY = b.max.y;
            }
            else
            {
                loX = minX; hiX = maxX;
                loY = minY; hiY = maxY;
            }

            GetViewHalfExtents(desired.z, out float halfWidth, out float halfHeight);

            desired.x = ClampInset(desired.x, loX, hiX, halfWidth);
            desired.y = ClampInset(desired.y, loY, hiY, halfHeight);
        }

        // If the level is smaller than the view on an axis there is no valid range left, so centre on
        // it rather than letting the clamp flip and jitter between two impossible limits.
        private static float ClampInset(float value, float min, float max, float halfExtent)
        {
            float lo = min + halfExtent;
            float hi = max - halfExtent;
            return lo > hi ? (min + max) * 0.5f : Mathf.Clamp(value, lo, hi);
        }

        // Half of what the camera sees, measured at the depth the player moves on.
        private void GetViewHalfExtents(float cameraZ, out float halfWidth, out float halfHeight)
        {
            halfWidth = 0f;
            halfHeight = 0f;

            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) return;

            if (_camera.orthographic)
            {
                halfHeight = _camera.orthographicSize;
            }
            else
            {
                float planeZ = _target != null ? _target.position.z : 0f;
                float distance = Mathf.Abs(planeZ - cameraZ);
                halfHeight = Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            }

            halfWidth = halfHeight * _camera.aspect;
        }
    }
}
