using SpieleMarmelade.Shared.Audio;
using SpieleMarmelade.Shared.Combat;
using SpieleMarmelade.Shared.Stats;
using SpieleMarmelade.World;
using UnityEngine;

namespace SpieleMarmelade.Shared.Movement
{
    // Full 3D movement relative to the active camera's facing (pressing "forward" moves the
    // way the camera looks, projected onto the ground plane). Includes jump/gravity like
    // PlatformerMovement. Turns the character to face its movement direction — unless a
    // LockOnController on the same object has an active target, in which case it always faces
    // the target and movement becomes a strafe (needed for blocking/parrying to make sense).
    [RequireComponent(typeof(CharacterController))]
    public class FreeThirdPersonMovement : MonoBehaviour, IPlayerMovement
    {
        [Header("── Movement ──────────────────────────")]
        [SerializeField] private float moveSpeed = 0.5f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float sprintMultiplier = 1.6f;

        [Header("── Jump ─────────────────────────────────")]
        [SerializeField] private bool canJump = true;
        [SerializeField] private float jumpHeight = 0.2f;
        [SerializeField] private float gravity = -2f;

        [Header("── Brick-Grid Feel ───────────────────────")]
        [Tooltip("Zieht die Bewegung leicht in Richtung der nächsten Brick-Rasterzelle. Kann bei " +
                 "normalem Lauftempo ruckelig wirken (die 'nächste Zelle' springt beim Überqueren " +
                 "der Zellmitte um) — bei Bedarf einfach ausschalten.")]
        [SerializeField] private bool useGridSnap = false;
        [Tooltip("0 = kein Einrasten, 1 = hart aufs Raster geklemmt. Klein halten für ein subtiles Gefühl.")]
        [SerializeField, Range(0f, 1f)] private float gridSnapStrength = 0.15f;

        [Header("── Dodge Roll (nur bei aktivem Lock-On) ──")]
        [SerializeField] private float dodgeSpeed = 1.5f;
        [SerializeField] private float dodgeDuration = 0.25f;

        [Header("── Brick-Klettern (Strg) ─────────────────")]
        [SerializeField] private float climbCheckDistance = 0.12f;
        [SerializeField] private float climbSpeed = 0.35f;
        [SerializeField] private float climbJumpOffDistance = 0.15f;

        [Header("── Sound ─────────────────────────────")]
        [SerializeField] private string jumpSfxId;
        [SerializeField] private string dodgeSfxId;
        [SerializeField] private string climbStartSfxId;

        private CharacterController _cc;
        private PlayerInputReader _input;
        private LockOnController _lockOn;
        private Health _health;
        private CharacterStats _stats;
        private Camera _cam;
        private float _verticalVelocity;

        private bool _dodging;
        private float _dodgeEndTime;
        private Vector3 _dodgeDirection;

        private bool _climbing;
        private Vector3 _climbNormal;

        public Vector3 Velocity => _cc.velocity;
        public bool IsGrounded => _cc.isGrounded;

        public void Init(Transform playerTransform, PlayerInputReader input)
        {
            _cc = playerTransform.GetComponent<CharacterController>();
            _input = input;
            _lockOn = playerTransform.GetComponent<LockOnController>();
            _health = playerTransform.GetComponent<Health>();
            _stats = playerTransform.GetComponent<CharacterStats>();
        }

        public void MovementTick(float dt)
        {
            if (_cc == null || _input == null) return;
            if (_cam == null) _cam = Camera.main;

            if (_dodging)
            {
                TickDodge(dt);
                return;
            }

            if (_climbing)
            {
                TickClimb(dt);
                return;
            }

            if (_input.ClimbHeld) TryStartClimb();
            if (_climbing)
            {
                TickClimb(dt);
                return;
            }

            Vector2 raw = _input.MoveInput;
            Vector3 camForward, camRight;
            if (_cam != null)
            {
                camForward = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized;
                camRight = Vector3.ProjectOnPlane(_cam.transform.right, Vector3.up).normalized;
            }
            else
            {
                camForward = Vector3.forward;
                camRight = Vector3.right;
            }

            var moveDir = camForward * raw.y + camRight * raw.x;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            bool lockedOn = _lockOn != null && _lockOn.CurrentTarget != null;

            // Shift-tap while locked on triggers a dodge roll instead of the usual sprint-hold.
            if (lockedOn && _input.SprintPressedThisFrame)
            {
                StartDodge(moveDir);
                return;
            }

            // If a CharacterStats component defines a MoveSpeed stat, it (base + modifiers,
            // e.g. a timed buff from StatBuffUsable) overrides the serialized moveSpeed above.
            float baseSpeed = _stats != null && _stats.HasStat(StatType.MoveSpeed) ? _stats.GetStat(StatType.MoveSpeed) : moveSpeed;
            float speed = baseSpeed * (!lockedOn && _input.SprintHeld ? sprintMultiplier : 1f);
            var move = moveDir * speed;
            move = ApplyGridSnap(move, dt);

            if (_cc.isGrounded)
            {
                _verticalVelocity = -2f;
                if (canJump && _input.JumpPressedThisFrame)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    SfxPlayer.Play(jumpSfxId);
                }
            }
            _verticalVelocity += gravity * dt;
            move.y = _verticalVelocity;

            _cc.Move(move * dt);

            Vector3 facing = lockedOn ? _lockOn.CurrentTarget.position - transform.position : moveDir;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f)
            {
                var target = Quaternion.LookRotation(facing, Vector3.up);
                _cc.transform.rotation = Quaternion.Slerp(_cc.transform.rotation, target, rotationSpeed * dt);
            }
        }

        // Gently biases the move vector toward the nearest brick-grid cell each frame — not a
        // hard snap, just enough to keep footing/collisions feeling clean without the player
        // noticing they're being nudged. Stays CharacterController-safe since we only reshape
        // the input to _cc.Move(), never teleport the transform directly.
        private Vector3 ApplyGridSnap(Vector3 move, float dt)
        {
            if (!useGridSnap || gridSnapStrength <= 0f || move.sqrMagnitude < 0.0001f || dt <= 0f) return move;

            Vector3 current = _cc.transform.position;
            Vector3 nextPos = current + move * dt;
            float snappedX = Mathf.Round(nextPos.x / WorldConstants.PlateWidth) * WorldConstants.PlateWidth;
            float snappedZ = Mathf.Round(nextPos.z / WorldConstants.PlateDepth) * WorldConstants.PlateDepth;
            Vector3 biasedNext = Vector3.Lerp(nextPos, new Vector3(snappedX, nextPos.y, snappedZ), gridSnapStrength);

            Vector3 biasedMove = (biasedNext - current) / dt;
            biasedMove.y = move.y;
            return biasedMove;
        }

        private void StartDodge(Vector3 inputDir)
        {
            _dodging = true;
            _dodgeEndTime = Time.time + dodgeDuration;
            _dodgeDirection = inputDir.sqrMagnitude > 0.0001f ? inputDir.normalized : -transform.forward;
            if (_health != null) _health.IsInvulnerable = true;
            SfxPlayer.Play(dodgeSfxId);
        }

        private void TickDodge(float dt)
        {
            _cc.Move(_dodgeDirection * dodgeSpeed * dt);

            if (Time.time >= _dodgeEndTime)
            {
                _dodging = false;
                if (_health != null) _health.IsInvulnerable = false;
            }
        }

        private void TryStartClimb()
        {
            Vector3 origin = _cc.transform.position + Vector3.up * (_cc.height * 0.5f);
            if (Physics.Raycast(origin, _cc.transform.forward, out RaycastHit hit, climbCheckDistance) &&
                hit.collider.GetComponentInParent<ClimbableSurface>() != null)
            {
                _climbing = true;
                _climbNormal = hit.normal;
                _verticalVelocity = 0f;
                SfxPlayer.Play(climbStartSfxId);
            }
        }

        private void TickClimb(float dt)
        {
            if (!_input.ClimbHeld)
            {
                _climbing = false;
                return;
            }

            if (_input.JumpPressedThisFrame)
            {
                _climbing = false;
                _cc.Move(_climbNormal * climbJumpOffDistance);
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                return;
            }

            Vector2 raw = _input.MoveInput;
            Vector3 wallRight = Vector3.Cross(Vector3.up, _climbNormal).normalized;
            Vector3 move = Vector3.up * raw.y * climbSpeed + wallRight * raw.x * climbSpeed;
            _cc.Move(move * dt);

            // Re-check the wall is still directly ahead — lets the climb follow slightly
            // uneven wall runs and ends the climb cleanly once the wall runs out.
            Vector3 origin = _cc.transform.position + Vector3.up * (_cc.height * 0.5f);
            if (Physics.Raycast(origin, _cc.transform.forward, out RaycastHit hit, climbCheckDistance) &&
                hit.collider.GetComponentInParent<ClimbableSurface>() != null)
            {
                _climbNormal = hit.normal;
                _cc.transform.rotation = Quaternion.LookRotation(-_climbNormal, Vector3.up);
            }
            else
            {
                _climbing = false;
            }
        }
    }
}
