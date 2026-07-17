using SpieleMarmelade.World;
using UnityEngine;

namespace SpieleMarmelade.Shared.Movement
{
    // Top-down grid movement (Bomberman-style): moves continuously along one axis at a time —
    // whichever input axis is stronger — and gently snaps the cross axis onto the brick grid
    // (WorldConstants.PlateWidth) so the player stays centered on a lane. No jump/gravity: the
    // world is assumed flat here. Drives a CharacterController for collision only.
    [RequireComponent(typeof(CharacterController))]
    public class GridMovement : MonoBehaviour, IPlayerMovement
    {
        [Tooltip("World units per second.")]
        [SerializeField] private float moveSpeed = 0.4f;

        [Tooltip("How quickly the cross axis snaps back onto the grid lane.")]
        [SerializeField] private float snapSpeed = 6f;

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
            var pos = _cc.transform.position;
            float dx, dz;

            if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.y))
            {
                dx = raw.x * moveSpeed * dt;
                float targetZ = Mathf.Round(pos.z / WorldConstants.PlateDepth) * WorldConstants.PlateDepth;
                dz = Mathf.MoveTowards(pos.z, targetZ, snapSpeed * WorldConstants.PlateDepth * dt) - pos.z;
            }
            else
            {
                dz = raw.y * moveSpeed * dt;
                float targetX = Mathf.Round(pos.x / WorldConstants.PlateWidth) * WorldConstants.PlateWidth;
                dx = Mathf.MoveTowards(pos.x, targetX, snapSpeed * WorldConstants.PlateWidth * dt) - pos.x;
            }

            _cc.Move(new Vector3(dx, -2f * dt, dz));

            var flat = new Vector3(dx, 0f, dz);
            if (flat.sqrMagnitude > 0.0001f)
            {
                var target = Quaternion.LookRotation(flat.normalized, Vector3.up);
                _cc.transform.rotation = Quaternion.Slerp(_cc.transform.rotation, target, rotationSpeed * dt);
            }
        }
    }
}
