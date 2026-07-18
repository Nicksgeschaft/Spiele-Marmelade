using SpieleMarmelade.Shared;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Physics-driven movement for the brick assembly: custom gravity and jump along world +Y,
    // horizontal run along world X, depth (Z) stays frozen via the Rigidbody constraints on the
    // prefab. See Docs/BrickMovementController_Anforderungen_v0.2.md section 4 for the exact
    // rules this implements.
    [RequireComponent(typeof(Rigidbody), typeof(PlayerInputReader), typeof(PlayerGroundSensor))]
    [RequireComponent(typeof(StatAggregator))]
    public class PlayerMovementController : MonoBehaviour
    {
        private static readonly Vector3 WorldUp = Vector3.up;

        // Small constant downward speed to keep ground contact alive without letting gravity
        // build up an ever-increasing dig-in velocity while resting (that dig-in/push-out cycle
        // is what caused the random diagonal drift on multi-piece ground).
        private const float GroundedStickSpeed = 0.5f;

        [Tooltip("Jump impulse and future assembly torque are applied here. Defaults to this transform if left empty.")]
        [SerializeField] private Transform mainBrickCenter;

        private Rigidbody _rigidbody;
        private PlayerInputReader _input;
        private PlayerGroundSensor _groundSensor;
        private StatAggregator _statAggregator;
        private bool _rigidbodyConfigured;

        private float _lastJumpPressedTime = float.NegativeInfinity;
        private float _lastGroundedTime = float.NegativeInfinity;

        // Only ever read from Update/FixedUpdate, never from Awake - StatAggregator.Current
        // depends on PlayerAssembly having already registered the Main-Brick, and Unity doesn't
        // guarantee Awake() order between sibling components.
        private PlayerRuntimeStats Stats => _statAggregator.Current;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _input = GetComponent<PlayerInputReader>();
            _groundSensor = GetComponent<PlayerGroundSensor>();
            _statAggregator = GetComponent<StatAggregator>();
        }

        private void Update()
        {
            // JumpPressed is an edge, not a level - buffer its timestamp here so a FixedUpdate
            // that lands a tick or two later can still consume it (Docs section 4.1, step 1-2).
            if (_input.JumpPressedThisFrame)
            {
                _lastJumpPressedTime = Time.time;
            }
        }

        private void FixedUpdate()
        {
            if (!_rigidbodyConfigured)
            {
                _rigidbody.maxAngularVelocity = Stats.MaxAngularVelocity;
                _rigidbody.angularDamping = Stats.AngularDrag;
                _rigidbodyConfigured = true;
            }

            bool isGrounded = _groundSensor.IsGrounded;
            if (isGrounded)
            {
                _lastGroundedTime = Time.time;
            }

            ApplyHorizontalMovement(isGrounded);
            ApplyGravity(isGrounded);
            TryConsumeBufferedJump(isGrounded);
        }

        private void ApplyHorizontalMovement(bool isGrounded)
        {
            // W/S (MoveInput.y) are intentionally ignored - MVP has no vertical/depth translation.
            float inputX = Mathf.Clamp(_input.MoveInput.x, -1f, 1f);
            float desiredVelocityX = inputX * Stats.MoveSpeed;

            float acceleration = Mathf.Abs(inputX) > 0.0001f ? Stats.GroundAcceleration : Stats.GroundDeceleration;
            if (!isGrounded)
            {
                acceleration *= Stats.AirControl;
            }

            Vector3 velocity = _rigidbody.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocityX, acceleration * Time.fixedDeltaTime);
            _rigidbody.linearVelocity = velocity;
        }

        private void ApplyGravity(bool isGrounded)
        {
            Vector3 velocity = _rigidbody.linearVelocity;

            // While grounded and not rising (i.e. not mid-jump-impulse), hold a small constant
            // downward speed instead of letting gravity keep accumulating into the floor.
            if (isGrounded && velocity.y <= 0f)
            {
                velocity.y = -GroundedStickSpeed;
                _rigidbody.linearVelocity = velocity;
                return;
            }

            float gravity = Stats.GravityMagnitude;
            if (velocity.y < 0f)
            {
                gravity *= Stats.FallGravityMultiplier;
            }
            else if (velocity.y > 0f && !_input.JumpHeld)
            {
                gravity *= Stats.LowJumpGravityMultiplier;
            }

            velocity.y = Mathf.Max(velocity.y - gravity * Time.fixedDeltaTime, -Stats.MaxFallSpeed);
            _rigidbody.linearVelocity = velocity;
        }

        private void TryConsumeBufferedJump(bool isGrounded)
        {
            bool jumpBuffered = Time.time - _lastJumpPressedTime <= Stats.JumpBufferTime;
            bool coyoteActive = Time.time - _lastGroundedTime <= Stats.CoyoteTime;
            if (!jumpBuffered || !(isGrounded || coyoteActive))
            {
                return;
            }

            // Consume both so a single press can't trigger twice and coyote time can't chain into a double jump.
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;

            Vector3 velocity = _rigidbody.linearVelocity;
            if (velocity.y < 0f)
            {
                velocity.y = 0f;
                _rigidbody.linearVelocity = velocity;
            }

            float jumpVelocity = Mathf.Sqrt(2f * Stats.GravityMagnitude * Stats.JumpHeight);
            Vector3 jumpImpulse = WorldUp * (jumpVelocity * _rigidbody.mass);
            Vector3 worldCenter = mainBrickCenter != null ? mainBrickCenter.position : transform.position;
            _rigidbody.AddForceAtPosition(jumpImpulse, worldCenter, ForceMode.Impulse);
        }
    }
}
