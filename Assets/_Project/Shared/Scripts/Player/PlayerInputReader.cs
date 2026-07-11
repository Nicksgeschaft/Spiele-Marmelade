using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Shared
{
    // Reads the shared "Player" action map from the project-wide Input Actions asset
    // (InputSystem_Actions, already registered under Project Settings > Input System Package)
    // instead of polling Keyboard/Mouse directly. No Inspector wiring needed — works out of
    // the box on any GameObject, including ones created at runtime by editor tools/wizards.
    //
    // Assumes a single active player (typical for a jam prototype): the shared map is
    // enabled/disabled per instance, which is fine as long as only one PlayerInputReader is
    // alive at a time.
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction _move, _look, _jump, _attack, _interact, _crouch, _sprint, _block, _lockOn, _climb;
        private InputAction _ability1, _ability2, _abilitySpecial, _quickSlot1, _quickSlot2, _quickSlot3, _quickSlot4;
        private InputAction _toggleInventory, _toggleCharacter, _toggleMap;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool SprintPressedThisFrame { get; private set; }
        public bool BlockHeld { get; private set; }
        public bool BlockPressedThisFrame { get; private set; }
        public bool ClimbHeld { get; private set; }

        public event Action AttackPerformed;
        public event Action InteractPerformed;
        public event Action CrouchPerformed;
        public event Action SprintPerformed;
        public event Action LockOnPerformed;
        public event Action Ability1Performed;
        public event Action Ability2Performed;
        public event Action AbilitySpecialPerformed;
        public event Action QuickSlot1Performed;
        public event Action QuickSlot2Performed;
        public event Action QuickSlot3Performed;
        public event Action QuickSlot4Performed;
        public event Action ToggleInventoryPerformed;
        public event Action ToggleCharacterPerformed;
        public event Action ToggleMapPerformed;

        private void Awake()
        {
            var map = InputSystem.actions != null ? InputSystem.actions.FindActionMap("Player") : null;
            if (map == null)
            {
                Debug.LogWarning("[PlayerInputReader] No project-wide 'Player' action map found. " +
                                  "Check Project Settings > Input System Package > Input Actions.");
                return;
            }

            _move           = map.FindAction("Move");
            _look           = map.FindAction("Look");
            _jump           = map.FindAction("Jump");
            _attack         = map.FindAction("Attack");
            _interact       = map.FindAction("Interact");
            _crouch         = map.FindAction("Crouch");
            _sprint         = map.FindAction("Sprint");
            _block          = map.FindAction("Block");
            _lockOn         = map.FindAction("LockOn");
            _ability1       = map.FindAction("Ability1");
            _ability2       = map.FindAction("Ability2");
            _abilitySpecial = map.FindAction("AbilitySpecial");
            _quickSlot1     = map.FindAction("QuickSlot1");
            _quickSlot2     = map.FindAction("QuickSlot2");
            _quickSlot3     = map.FindAction("QuickSlot3");
            _quickSlot4     = map.FindAction("QuickSlot4");
            _climb          = map.FindAction("Climb");
            _toggleInventory = map.FindAction("ToggleInventory");
            _toggleCharacter = map.FindAction("ToggleCharacter");
            _toggleMap       = map.FindAction("ToggleMap");
        }

        private void OnEnable()
        {
            InputSystem.actions?.FindActionMap("Player")?.Enable();
            if (_attack         != null) _attack.performed         += OnAttack;
            if (_interact       != null) _interact.performed       += OnInteract;
            if (_crouch         != null) _crouch.performed         += OnCrouchPerformed;
            if (_sprint         != null) _sprint.performed         += OnSprintPerformed;
            if (_lockOn         != null) _lockOn.performed         += OnLockOn;
            if (_ability1       != null) _ability1.performed       += OnAbility1;
            if (_ability2       != null) _ability2.performed       += OnAbility2;
            if (_abilitySpecial != null) _abilitySpecial.performed += OnAbilitySpecial;
            if (_quickSlot1     != null) _quickSlot1.performed     += OnQuickSlot1;
            if (_quickSlot2     != null) _quickSlot2.performed     += OnQuickSlot2;
            if (_quickSlot3     != null) _quickSlot3.performed     += OnQuickSlot3;
            if (_quickSlot4     != null) _quickSlot4.performed     += OnQuickSlot4;
            if (_toggleInventory != null) _toggleInventory.performed += OnToggleInventory;
            if (_toggleCharacter != null) _toggleCharacter.performed += OnToggleCharacter;
            if (_toggleMap       != null) _toggleMap.performed       += OnToggleMap;
        }

        private void OnDisable()
        {
            if (_attack         != null) _attack.performed         -= OnAttack;
            if (_interact       != null) _interact.performed       -= OnInteract;
            if (_crouch         != null) _crouch.performed         -= OnCrouchPerformed;
            if (_sprint         != null) _sprint.performed         -= OnSprintPerformed;
            if (_lockOn         != null) _lockOn.performed         -= OnLockOn;
            if (_ability1       != null) _ability1.performed       -= OnAbility1;
            if (_ability2       != null) _ability2.performed       -= OnAbility2;
            if (_abilitySpecial != null) _abilitySpecial.performed -= OnAbilitySpecial;
            if (_quickSlot1     != null) _quickSlot1.performed     -= OnQuickSlot1;
            if (_quickSlot2     != null) _quickSlot2.performed     -= OnQuickSlot2;
            if (_quickSlot3     != null) _quickSlot3.performed     -= OnQuickSlot3;
            if (_quickSlot4     != null) _quickSlot4.performed     -= OnQuickSlot4;
            if (_toggleInventory != null) _toggleInventory.performed -= OnToggleInventory;
            if (_toggleCharacter != null) _toggleCharacter.performed -= OnToggleCharacter;
            if (_toggleMap       != null) _toggleMap.performed       -= OnToggleMap;
        }

        private void Update()
        {
            MoveInput              = _move?.ReadValue<Vector2>() ?? Vector2.zero;
            LookInput              = _look?.ReadValue<Vector2>() ?? Vector2.zero;
            JumpPressedThisFrame   = _jump != null && _jump.WasPressedThisFrame();
            JumpHeld               = _jump != null && _jump.IsPressed();
            CrouchHeld             = _crouch != null && _crouch.IsPressed();
            SprintHeld             = _sprint != null && _sprint.IsPressed();
            SprintPressedThisFrame = _sprint != null && _sprint.WasPressedThisFrame();
            BlockHeld              = _block != null && _block.IsPressed();
            BlockPressedThisFrame  = _block != null && _block.WasPressedThisFrame();
            ClimbHeld              = _climb != null && _climb.IsPressed();
        }

        private void OnAttack(InputAction.CallbackContext ctx)          => AttackPerformed?.Invoke();
        private void OnInteract(InputAction.CallbackContext ctx)        => InteractPerformed?.Invoke();
        private void OnCrouchPerformed(InputAction.CallbackContext ctx) => CrouchPerformed?.Invoke();
        private void OnSprintPerformed(InputAction.CallbackContext ctx) => SprintPerformed?.Invoke();
        private void OnLockOn(InputAction.CallbackContext ctx)          => LockOnPerformed?.Invoke();
        private void OnAbility1(InputAction.CallbackContext ctx)        => Ability1Performed?.Invoke();
        private void OnAbility2(InputAction.CallbackContext ctx)        => Ability2Performed?.Invoke();
        private void OnAbilitySpecial(InputAction.CallbackContext ctx)  => AbilitySpecialPerformed?.Invoke();
        private void OnQuickSlot1(InputAction.CallbackContext ctx)      => QuickSlot1Performed?.Invoke();
        private void OnQuickSlot2(InputAction.CallbackContext ctx)      => QuickSlot2Performed?.Invoke();
        private void OnQuickSlot3(InputAction.CallbackContext ctx)      => QuickSlot3Performed?.Invoke();
        private void OnQuickSlot4(InputAction.CallbackContext ctx)      => QuickSlot4Performed?.Invoke();
        private void OnToggleInventory(InputAction.CallbackContext ctx) => ToggleInventoryPerformed?.Invoke();
        private void OnToggleCharacter(InputAction.CallbackContext ctx) => ToggleCharacterPerformed?.Invoke();
        private void OnToggleMap(InputAction.CallbackContext ctx)       => ToggleMapPerformed?.Invoke();
    }
}
