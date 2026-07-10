using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GameJamUniverse.Shared.World
{
    // Makes a brick-built sign/logo clickable as a genuine 3D in-world button — no uGUI/Canvas
    // involved. Needs a Collider covering the whole text somewhere on this object or its
    // children (the Brick Text Generator adds one automatically when "Als Button nutzbar" is
    // checked). Uses the new Input System exclusively (Active Input Handling = Input System
    // Package in this project), never the legacy Input class.
    public class BrickTextButton : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float  maxDistance = 100f;

        public UnityEvent OnClicked;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            if (raycastCamera == null) raycastCamera = Camera.main;
            if (raycastCamera == null) return;

            var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, maxDistance) && hit.collider.transform.IsChildOf(transform))
                OnClicked?.Invoke();
        }
    }
}
