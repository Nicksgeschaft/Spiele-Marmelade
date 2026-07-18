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

        [Tooltip("Layers a wall jump can push off. The PlayerAssembly layer is excluded automatically.")]
        [SerializeField] private LayerMask wallMask = ~0;

        private float _lastJumpPressedTime = float.NegativeInfinity;
        private float _lastGroundedTime = float.NegativeInfinity;

        // Abilities unlocked by bricks. All reset on landing, so they're once-per-airtime.
        private bool _airJumpUsed;
        private bool _wallJumpUsed;

        // Dash: double-tapping A/D/S. Tracked per direction so tapping A then D doesn't count.
        private float _lastTapTime = float.NegativeInfinity;
        private int _lastTapDirection;
        private float _dashEndTime;
        private float _dashReadyTime;
        private Vector3 _dashVelocity;

        private bool IsDashing => Time.time < _dashEndTime;

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

            DetectDashInput();
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
                // Landing restocks the once-per-airtime abilities.
                _airJumpUsed = false;
                _wallJumpUsed = false;
            }

            // A dash overrides normal movement and gravity for its short duration, which is what makes
            // it read as a dash rather than a nudge.
            if (IsDashing)
            {
                _rigidbody.linearVelocity = _dashVelocity;
                return;
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
            if (!jumpBuffered) return;

            bool coyoteActive = Time.time - _lastGroundedTime <= Stats.CoyoteTime;

            // Priority: real ground jump, then pushing off a wall, then the mid-air jump. A wall is
            // worth more than the air jump because it can be repeated by alternating walls.
            if (isGrounded || coyoteActive)
            {
                // Consume the coyote window too, so it can't chain into a second jump.
                _lastGroundedTime = float.NegativeInfinity;
                PerformJump(Stats.JumpHeight, 0f);
                return;
            }

            if (Stats.CanWallJump && !_wallJumpUsed && TryGetWallDirection(out float wallNormalX))
            {
                _wallJumpUsed = true;
                PerformJump(Stats.WallJumpHeight, wallNormalX * Stats.WallJumpPush);
                return;
            }

            if (Stats.CanAirJump && !_airJumpUsed)
            {
                _airJumpUsed = true;
                PerformJump(Stats.AirJumpHeight, 0f);
            }
        }

        // horizontalPush is the sideways kick used by a wall jump; 0 for a normal jump.
        private void PerformJump(float height, float horizontalPush)
        {
            if (height <= 0f) return;

            _lastJumpPressedTime = float.NegativeInfinity;

            Vector3 velocity = _rigidbody.linearVelocity;
            // Wipe any downward speed first, so a jump taken while falling reaches its full height.
            if (velocity.y < 0f) velocity.y = 0f;
            if (Mathf.Abs(horizontalPush) > 0.0001f) velocity.x = horizontalPush;
            _rigidbody.linearVelocity = velocity;

            float jumpVelocity = Mathf.Sqrt(2f * Stats.GravityMagnitude * height);
            Vector3 jumpImpulse = WorldUp * (jumpVelocity * _rigidbody.mass);
            Vector3 worldCenter = mainBrickCenter != null ? mainBrickCenter.position : transform.position;
            _rigidbody.AddForceAtPosition(jumpImpulse, worldCenter, ForceMode.Impulse);
        }

        // Looks for a wall beside any collider of the assembly. Returns the direction to push AWAY
        // from it (+1 = wall on the left, so push right).
        private bool TryGetWallDirection(out float pushDirection)
        {
            pushDirection = 0f;

            int playerLayer = LayerMask.NameToLayer("PlayerAssembly");
            int mask = playerLayer >= 0 ? wallMask.value & ~(1 << playerLayer) : wallMask.value;

            foreach (Collider brickCollider in GetComponentsInChildren<Collider>())
            {
                if (brickCollider.isTrigger) continue;

                Bounds bounds = brickCollider.bounds;
                Vector3 halfExtents = new(0.02f, bounds.extents.y * 0.8f, bounds.extents.z * 0.8f);
                float distance = bounds.extents.x + Stats.WallCheckDistance;

                if (Physics.BoxCast(bounds.center, halfExtents, Vector3.left, out _,
                        Quaternion.identity, distance, mask, QueryTriggerInteraction.Ignore))
                {
                    pushDirection = 1f;
                    return true;
                }
                if (Physics.BoxCast(bounds.center, halfExtents, Vector3.right, out _,
                        Quaternion.identity, distance, mask, QueryTriggerInteraction.Ignore))
                {
                    pushDirection = -1f;
                    return true;
                }
            }

            return false;
        }

        // Double-tap A / D / S starts a dash left / right / down. Tracked per direction so tapping two
        // different keys in quick succession doesn't count as a double-tap.
        private void DetectDashInput()
        {
            if (!Stats.CanDash || Time.time < _dashReadyTime) return;

            int direction = ReadDashTapDirection();
            if (direction == 0) return;

            bool sameDirection = direction == _lastTapDirection;
            bool withinWindow = Time.time - _lastTapTime <= Stats.DoubleTapWindow;

            if (sameDirection && withinWindow)
            {
                StartDash(direction);
                _lastTapTime = float.NegativeInfinity;
                _lastTapDirection = 0;
                return;
            }

            _lastTapTime = Time.time;
            _lastTapDirection = direction;
        }

        // -1 = left, 1 = right, 2 = down. Only fires on the frame a direction is newly pressed.
        private int ReadDashTapDirection()
        {
            Vector2 move = _input.MoveInput;
            bool leftHeld = move.x < -0.5f;
            bool rightHeld = move.x > 0.5f;
            bool downHeld = move.y < -0.5f;

            int direction = 0;
            if (leftHeld && !_leftWasHeld) direction = -1;
            else if (rightHeld && !_rightWasHeld) direction = 1;
            else if (downHeld && !_downWasHeld) direction = 2;

            _leftWasHeld = leftHeld;
            _rightWasHeld = rightHeld;
            _downWasHeld = downHeld;
            return direction;
        }

        private void StartDash(int direction)
        {
            Vector3 velocity = direction switch
            {
                -1 => Vector3.left * Stats.DashSpeed,
                1 => Vector3.right * Stats.DashSpeed,
                _ => Vector3.down * Stats.DashSpeed,
            };

            _dashVelocity = velocity;
            _dashEndTime = Time.time + Stats.DashDuration;
            _dashReadyTime = _dashEndTime + Stats.DashCooldown;
        }

        private bool _leftWasHeld;
        private bool _rightWasHeld;
        private bool _downWasHeld;
    }
}
