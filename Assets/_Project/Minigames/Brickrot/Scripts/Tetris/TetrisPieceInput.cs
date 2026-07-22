using UnityEngine;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    // Horizontal axis that slides the falling Tetris piece.
    //
    // The Survivor half already owns "Move" (WASD / left stick), so this half needs an axis of its
    // own — but the project-wide "Player" map has no second directional axis. So: use an optional
    // "MoveTetrisPiece" action if the project defines one, and otherwise fall back to the arrow
    // keys and the right stick.
    //
    // The fallback is the one place in this minigame that touches Keyboard/Gamepad directly,
    // against the usual PlayerInputReader convention. It's deliberate: it makes the minigame
    // playable the moment it's dropped into a scene, and it upgrades itself the moment someone
    // adds the action to InputSystem_Actions — no code change needed.
    public class TetrisPieceInput
    {
        public const string ActionName = "MoveTetrisPiece";

        // Below this the right stick counts as centred, so a drifting stick doesn't creep the
        // piece sideways forever.
        private const float StickDeadzone = 0.35f;

        private readonly InputAction _action;

        public TetrisPieceInput()
        {
            _action = InputSystem.actions != null
                ? InputSystem.actions.FindActionMap("Player")?.FindAction(ActionName)
                : null;

            if (_action != null)
            {
                _action.Enable();
                return;
            }

            Debug.Log($"[Brickrot] Keine '{ActionName}'-Action gefunden — Tetris läuft auf " +
                      "Pfeiltasten / rechtem Stick. Lege die Action in InputSystem_Actions an, " +
                      "um sie frei zu belegen.");
        }

        /// <summary>-1 = links, +1 = rechts, 0 = keine Eingabe.</summary>
        public float Read()
        {
            if (_action != null) return _action.ReadValue<float>();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float axis = 0f;
                if (keyboard.leftArrowKey.isPressed) axis -= 1f;
                if (keyboard.rightArrowKey.isPressed) axis += 1f;
                if (axis != 0f) return axis;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                float x = pad.rightStick.x.ReadValue();
                if (Mathf.Abs(x) >= StickDeadzone) return Mathf.Sign(x);
            }

            return 0f;
        }
    }
}
