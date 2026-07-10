using UnityEngine;

namespace GameJamUniverse.Shared.Movement
{
    // Free top-down movement (Zelda / twin-stick style): 8-directional, no grid snapping.
    // Turns the character to face whichever direction it's currently moving. Flat world
    // assumed, same as GridMovement — no jump/gravity handling.
    [RequireComponent(typeof(CharacterController))]
    public class TopDownFreeMovement : MonoBehaviour, IPlayerMovement
    {
        [Tooltip("World units per second.")]
        [SerializeField] private float moveSpeed = 0.4f;

        [Tooltip("Facing-turn smoothing. Higher = snappier.")]
        [SerializeField] private float rotationSpeed = 12f;

        private CharacterController _cc;
        private PlayerInputReader   _input;

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

            Vector2 raw = _input.MoveInput;
            var moveDir = new Vector3(raw.x, 0f, raw.y);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            _cc.Move(new Vector3(moveDir.x * moveSpeed, -2f, moveDir.z * moveSpeed) * dt);

            if (moveDir.sqrMagnitude > 0.0001f)
            {
                var target = Quaternion.LookRotation(moveDir, Vector3.up);
                _cc.transform.rotation = Quaternion.Slerp(_cc.transform.rotation, target, rotationSpeed * dt);
            }
        }
    }
}
