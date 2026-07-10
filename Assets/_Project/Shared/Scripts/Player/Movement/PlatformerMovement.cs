using UnityEngine;

namespace GameJamUniverse.Shared.Movement
{
    // 2.5D side-scroller movement: horizontal run along X, gravity + jump on Y, depth (Z)
    // stays locked. Drives a CharacterController on the same GameObject as PlayerController.
    [RequireComponent(typeof(CharacterController))]
    public class PlatformerMovement : MonoBehaviour, IPlayerMovement
    {
        // Defaults are tuned for GameJam Universe's brick scale (1 brick ≈ 0.0795 world
        // units — see WorldConstants), not a 1-unit-= 1-meter world. moveSpeed and jumpHeight
        // are scaled down together with gravity so the jump arc timing still feels right.
        [Header("── Movement ──────────────────────────")]
        [Tooltip("World units per second.")]
        [SerializeField] private float moveSpeed = 0.5f;

        [Tooltip("Facing-flip smoothing. Higher = snappier.")]
        [SerializeField] private float rotationSpeed = 12f;

        [Header("── Jump ─────────────────────────────────")]
        [SerializeField] private bool canJump = true;
        [Tooltip("Apex height in world units.")]
        [SerializeField] private float jumpHeight = 0.2f;
        [Tooltip("Downward acceleration (negative), scaled to match jumpHeight's timing.")]
        [SerializeField] private float gravity = -2f;

        private CharacterController _cc;
        private PlayerInputReader   _input;
        private float               _verticalVelocity;

        public Vector3 Velocity   => _cc.velocity;
        public bool    IsGrounded => _cc.isGrounded;

        public void Init(Transform playerTransform, PlayerInputReader input)
        {
            _cc    = playerTransform.GetComponent<CharacterController>();
            _input = input;
        }

        public void MovementTick(float dt)
        {
            if (_cc == null || _input == null) return;

            float x = _input.MoveInput.x;
            var   move = new Vector3(Mathf.Clamp(x, -1f, 1f), 0f, 0f) * moveSpeed;

            if (_cc.isGrounded)
            {
                _verticalVelocity = -2f; // keep grounded
                if (canJump && _input.JumpPressedThisFrame)
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _verticalVelocity += gravity * dt;
            move.y             = _verticalVelocity;

            _cc.Move(move * dt);

            if (Mathf.Abs(x) > 0.01f)
            {
                var facing = x > 0f ? Vector3.right : Vector3.left;
                var target = Quaternion.LookRotation(facing, Vector3.up);
                _cc.transform.rotation = Quaternion.Slerp(_cc.transform.rotation, target, rotationSpeed * dt);
            }
        }
    }
}
