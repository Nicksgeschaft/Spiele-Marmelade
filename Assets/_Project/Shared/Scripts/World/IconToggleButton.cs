using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Shared.World
{
    [System.Serializable]
    public class IconToggleValueChanged : UnityEvent<bool> { }

    // A clickable brick-built icon that toggles a bool state and swaps between two icon
    // GameObjects to reflect it — e.g. the Options screen's Fullscreen control: shows the "go
    // fullscreen" icon while windowed, swaps to the "restore window" icon once fullscreen. No
    // separate uGUI Toggle/checkbox involved — clicking the icon itself IS the button, same
    // click-detection pattern as BrickTextButton (Physics.Raycast against a Collider somewhere
    // on this object or its children).
    public class IconToggleButton : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float  maxDistance = 100f;
        [SerializeField] private string clickSfxId = "click";

        [Tooltip("Gezeigt, solange der Zustand AUS ist (Klick schaltet EIN).")]
        [SerializeField] private GameObject offIcon;
        [Tooltip("Gezeigt, solange der Zustand EIN ist (Klick schaltet AUS).")]
        [SerializeField] private GameObject onIcon;

        public IconToggleValueChanged OnValueChanged = new();

        private bool _isOn;

        public bool IsOn => _isOn;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
            ApplyIcons();
        }

        public void SetIsOn(bool value, bool notify = true)
        {
            _isOn = value;
            ApplyIcons();
            if (notify) OnValueChanged?.Invoke(_isOn);
        }

        private void ApplyIcons()
        {
            if (offIcon != null) offIcon.SetActive(!_isOn);
            if (onIcon != null) onIcon.SetActive(_isOn);
        }

        private void Update()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
            if (raycastCamera == null || Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, maxDistance) && hit.collider.transform.IsChildOf(transform))
            {
                SfxPlayer.PlayUi(clickSfxId);
                SetIsOn(!_isOn);
            }
        }
    }
}
