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

        [Header("── Fill-Optik (optional) ──────────────")]
        [Tooltip("Track-Bricks von links bis zum Griff, in Bau-Reihenfolge. Leer lassen, um nur den Griff zu bewegen ohne Einfärben.")]
        [SerializeField] private GameObject[] segments;
        [Tooltip("Farbe für Segmente links vom Griff (\"gefüllt\").")]
        [SerializeField] private Material filledMaterial;
        [Tooltip("Farbe für Segmente rechts vom Griff (\"leer\").")]
        [SerializeField] private Material unfilledMaterial;

        public BrickSliderValueChanged OnValueChanged = new();

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
            ApplySegmentColors(t);
            if (notify) OnValueChanged?.Invoke(value);
        }

        // Recolors the track bricks left of the handle as "filled" and the rest as "unfilled" —
        // a lot of bricks laid down in a row, one dark handle brick on top, everything from the
        // left up to the handle turned light — the brick equivalent of a filled slider track.
        private void ApplySegmentColors(float t)
        {
            if (segments == null || segments.Length == 0) return;
            int filledCount = Mathf.RoundToInt(t * segments.Length);
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;
                var mat = i < filledCount ? filledMaterial : unfilledMaterial;
                if (mat == null) continue;
                foreach (var mr in segments[i].GetComponentsInChildren<MeshRenderer>())
                    mr.sharedMaterial = mat;
            }
        }
    }
}
