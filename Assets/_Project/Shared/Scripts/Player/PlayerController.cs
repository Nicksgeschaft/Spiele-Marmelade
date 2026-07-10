using UnityEngine;
using UnityEngine.Events;

namespace GameJamUniverse.Shared
{
    /// <summary>
    /// Thin per-frame orchestrator: reads input via <see cref="PlayerInputReader"/> and delegates
    /// actual locomotion to whichever <see cref="IPlayerMovement"/> component is attached
    /// (e.g. PlatformerMovement) — swap that component to change how the player moves without
    /// touching this script. Also exposes simple UnityEvent action slots for jam prototyping.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        // ── Action Slots ──────────────────────────────────────────────────────
        // Mapped to the shared "Player" action map (InputSystem_Actions) so the same slots
        // work with keyboard, gamepad and touch bindings alike.
        [Header("── Action Slots ─────────────────────────")]
        [Tooltip("Attack action (mouse left click / gamepad west / Enter).")]
        public UnityEvent OnLeftClick;

        [Tooltip("Interact action (E / gamepad north, hold).")]
        public UnityEvent OnRightClick;

        [Tooltip("Crouch action (C / gamepad east).")]
        public UnityEvent OnAction3;

        [Tooltip("Sprint action (Left Shift / gamepad left stick press).")]
        public UnityEvent OnAction4;

        private PlayerInputReader _input;
        private IPlayerMovement   _movement;

        private void Awake()
        {
            _input    = GetComponent<PlayerInputReader>();
            _movement = GetComponent<IPlayerMovement>();

            if (_movement == null)
                Debug.LogWarning($"[PlayerController] No IPlayerMovement component found on '{name}'. " +
                                  "Add one (e.g. PlatformerMovement) to make this player move.");
            else
                _movement.Init(transform, _input);
        }

        private void OnEnable()
        {
            if (_input == null) return;
            _input.AttackPerformed   += OnLeftClick.Invoke;
            _input.InteractPerformed += OnRightClick.Invoke;
            _input.CrouchPerformed   += OnAction3.Invoke;
            _input.SprintPerformed   += OnAction4.Invoke;
        }

        private void OnDisable()
        {
            if (_input == null) return;
            _input.AttackPerformed   -= OnLeftClick.Invoke;
            _input.InteractPerformed -= OnRightClick.Invoke;
            _input.CrouchPerformed   -= OnAction3.Invoke;
            _input.SprintPerformed   -= OnAction4.Invoke;
        }

        private void Update() => _movement?.MovementTick(Time.deltaTime);

        // ── Public helpers ────────────────────────────────────────────────────
        public Vector3 Velocity   => _movement?.Velocity ?? Vector3.zero;
        public bool    IsGrounded => _movement?.IsGrounded ?? false;
    }
}
