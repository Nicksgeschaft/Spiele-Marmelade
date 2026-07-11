using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Shared.World
{
    [System.Serializable]
    public class BrickSliderValueChanged : UnityEvent<float> { }

    // A track of background bricks (see BrickBarBuilderWindow) plus one draggable "handle"
    // brick on top — click-and-drag the handle along `axis` to set a value, the brick-built
    // alternative to a uGUI Slider. Assumes a flat, camera-facing layout (built for the Menu
    // Flow stage): drag tracking raycasts against the plane through this transform (normal =
    // transform.forward) rather than general 3D closest-point-on-line math.
    public class BrickSliderTrack : MonoBehaviour
    {
        [SerializeField] private Transform handle;
        [SerializeField] private Vector3 axis = Vector3.right;
        [SerializeField] private float trackLength = 1f;
        [Tooltip("Muss zugewiesen werden (z. B. die MenuCamera) — sonst fällt es auf Camera.main zurück.")]
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float minValue = 0f;
        [SerializeField] private float maxValue = 1f;
        [SerializeField] private float value;
        [SerializeField] private string dragStartSfxId;

        public BrickSliderValueChanged OnValueChanged;

        private bool _dragging;
        private Vector3 _axisWorld;

        public float Value => value;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
            _axisWorld = transform.TransformDirection(axis.normalized);
            SetValue(value, notify: false);
        }

        private void Update()
        {
            if (Mouse.current == null || handle == null) return;
            if (raycastCamera == null) raycastCamera = Camera.main;
            if (raycastCamera == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out var hit) && hit.collider.transform.IsChildOf(handle))
                {
                    _dragging = true;
                    SfxPlayer.PlayUi(dragStartSfxId);
                }
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame) _dragging = false;

            if (_dragging) DragUpdate();
        }

        private void DragUpdate()
        {
            var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(transform.forward, transform.position);
            if (!plane.Raycast(ray, out float enter)) return;

            Vector3 hitPoint = ray.GetPoint(enter);
            float distance = Vector3.Dot(hitPoint - transform.position, _axisWorld);
            float t = trackLength > 0f ? Mathf.Clamp01(distance / trackLength) : 0f;
            SetValue(Mathf.Lerp(minValue, maxValue, t));
        }

        public void SetValue(float newValue, bool notify = true)
        {
            value = Mathf.Clamp(newValue, minValue, maxValue);
            float t = maxValue > minValue ? (value - minValue) / (maxValue - minValue) : 0f;
            if (handle != null) handle.position = transform.position + _axisWorld * (t * trackLength);
            if (notify) OnValueChanged?.Invoke(value);
        }
    }
}
