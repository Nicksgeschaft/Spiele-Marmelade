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
    //
    // Everything is computed in this transform's LOCAL space, which is what makes it survive being
    // scaled (the Options rows scale the slider twice over). An earlier version positioned the
    // handle in world space via TransformDirection - that ignores scale, so the handle drifted out
    // of the track as soon as any parent scaling was involved, and the generator had to bake the
    // scale factors into trackLength by hand to compensate.
    public class BrickSliderTrack : MonoBehaviour
    {
        [SerializeField] private Transform handle;
        [SerializeField] private Vector3 axis = Vector3.right;
        [Tooltip("Length of the track in LOCAL units. Ignored when Segments are assigned - the real " +
                 "length is measured from the first and last segment instead.")]
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

        public float Value => value;

        // Measured from the actual track bricks so it always matches what's on screen, whatever the
        // slider is scaled by. Falls back to the serialized value only for hand-built sliders that
        // have no segments assigned.
        private float LocalTrackLength
        {
            get
            {
                if (segments == null || segments.Length < 2) return trackLength;

                Transform first = segments[0] != null ? segments[0].transform : null;
                Transform last = segments[^1] != null ? segments[^1].transform : null;
                if (first == null || last == null) return trackLength;

                return Mathf.Abs(Vector3.Dot(last.localPosition - first.localPosition, axis.normalized));
            }
        }

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
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

            // Convert the hit into local space first, so the same maths works at any scale.
            Vector3 localHit = transform.InverseTransformPoint(ray.GetPoint(enter));
            float distance = Vector3.Dot(localHit, axis.normalized);
            float length = LocalTrackLength;
            float t = length > 0f ? Mathf.Clamp01(distance / length) : 0f;
            SetValue(Mathf.Lerp(minValue, maxValue, t));
        }

        public void SetValue(float newValue, bool notify = true)
        {
            value = Mathf.Clamp(newValue, minValue, maxValue);
            float t = maxValue > minValue ? (value - minValue) / (maxValue - minValue) : 0f;

            if (handle != null)
            {
                // Replace only the along-axis component, so the handle keeps whatever offset it was
                // authored with on the other axes (it sits slightly in front to avoid z-fighting
                // with the track brick underneath - overwriting the full position lost that).
                Vector3 axisN = axis.normalized;
                Vector3 local = handle.localPosition;
                float currentAlongAxis = Vector3.Dot(local, axisN);
                handle.localPosition = local + axisN * (t * LocalTrackLength - currentAlongAxis);
            }

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
