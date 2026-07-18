using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // A round brick placed in the level as a point pickup. Idles with a gentle bob and spin so it
    // reads as collectable, then on touch flies to the PointStack in the corner of the screen and
    // settles onto the pile.
    //
    // The flight re-reads the target slot's world position every frame instead of caching it once,
    // because the stack rides on the moving camera - a cached point would be stale by the time the
    // brick arrives.
    [RequireComponent(typeof(Collider))]
    public class PointCollectable : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        private enum State { Idle, Flying, Stacked }

        [Header("Idle Motion")]
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float spinDegreesPerSecond = 60f;

        [Header("Collect Flight")]
        [SerializeField] private float flightDuration = 0.55f;
        [Tooltip("How far the brick arcs upward on its way to the stack. Pure juice.")]
        [SerializeField] private float arcHeight = 1.2f;
        [SerializeField] private float flightSpinDegreesPerSecond = 540f;
        [Tooltip("Eases the flight. Flat start + fast finish reads as being sucked in.")]
        [SerializeField] private AnimationCurve flightEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Extra scale at the midpoint of the flight, for a little pop.")]
        [SerializeField] private float midFlightScalePunch = 1.25f;

        [Header("Debug")]
        [Tooltip("Logs every collider that touches this pickup and whether it was recognised as the player.")]
        [SerializeField] private bool logCollectDebug;

        private State _state = State.Idle;
        private Vector3 _idleOrigin;
        private float _bobPhase;

        private void Start()
        {
            _idleOrigin = transform.position;
            // Desynchronise bobbing so a row of pickups doesn't pulse in lockstep.
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_state != State.Idle) return;

            float bob = Mathf.Sin(Time.time * bobSpeed + _bobPhase) * bobAmplitude;
            transform.position = _idleOrigin + Vector3.up * bob;
            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryCollect(other);
        }

        // Also handled on solid collision, so the pickup still works if its collider is left
        // non-trigger in a scene.
        private void OnCollisionEnter(Collision collision)
        {
            TryCollect(collision.collider);
        }

        private void TryCollect(Collider other)
        {
            if (_state != State.Idle) return;

            if (logCollectDebug)
            {
                Debug.Log($"[PointCollectable] '{name}' touched by '{other.name}' " +
                          $"(layer {other.gameObject.layer}, tag '{other.tag}'), " +
                          $"PlayerAssembly in parents: {other.GetComponentInParent<PlayerAssembly>() != null}", this);
            }

            if (!IsPlayer(other))
            {
                return;
            }

            PointStack stack = PointStack.Instance;
            if (stack == null)
            {
                Debug.LogWarning($"[PointCollectable] '{name}' collected but no PointStack exists in the scene.", this);
                return;
            }

            _state = State.Flying;

            // Stop blocking/re-triggering the moment it's claimed.
            foreach (Collider ownCollider in GetComponentsInChildren<Collider>())
            {
                ownCollider.enabled = false;
            }

            StartCoroutine(FlyToStack(stack, stack.ReserveSlot(MeasureUnitSize())));
        }

        // The brick's size when this transform's scale is 1, read straight off the meshes. Measuring
        // beats a hand-typed constant here: the visual is a x10-scaled child, and a spacing number
        // that doesn't match it is invisible in the Inspector but stacks every pickup on one spot.
        private Vector3 MeasureUnitSize()
        {
            Bounds combined = default;
            bool any = false;

            foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;

                // Mesh bounds -> this object's local space, so the result is independent of where the
                // pickup currently sits or how it's spinning.
                Bounds meshBounds = filter.sharedMesh.bounds;
                Vector3 center = transform.InverseTransformPoint(filter.transform.TransformPoint(meshBounds.center));
                Vector3 extents = transform.InverseTransformVector(filter.transform.TransformVector(meshBounds.extents));
                extents = new Vector3(Mathf.Abs(extents.x), Mathf.Abs(extents.y), Mathf.Abs(extents.z));

                var local = new Bounds(center, extents * 2f);
                if (!any)
                {
                    combined = local;
                    any = true;
                }
                else
                {
                    combined.Encapsulate(local);
                }
            }

            return any ? combined.size : Vector3.one;
        }

        // Accepts any part of the player: the assembly component sits on PlayerRoot, so main brick and
        // attached bricks alike resolve through it. The tag is a fallback for setups where the
        // touching collider lives outside that hierarchy (e.g. a separate visual/physics rig).
        private static bool IsPlayer(Collider other)
        {
            if (other.GetComponentInParent<PlayerAssembly>() != null) return true;
            return other.CompareTag(PlayerTag) || (other.attachedRigidbody != null &&
                                                   other.attachedRigidbody.CompareTag(PlayerTag));
        }

        private IEnumerator FlyToStack(PointStack stack, Vector3 slotLocalPosition)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 startScale = transform.localScale;

            // Absolute, not relative to the level instance's own scale: the stack's slot spacing is
            // derived from one brick size, so every pickup has to arrive at that same size no matter
            // how big or small it was placed in the level. Scaling relatively would let a resized
            // pickup land oversized and overlap its neighbours.
            Vector3 targetScale = Vector3.one * stack.StackedScale;

            float elapsed = 0f;
            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float linear = Mathf.Clamp01(elapsed / flightDuration);
                float eased = flightEase.Evaluate(linear);

                // Re-read every frame: the stack moves with the camera while the brick is in the air.
                Vector3 target = stack.transform.TransformPoint(slotLocalPosition);

                Vector3 position = Vector3.Lerp(startPosition, target, eased);
                position += Vector3.up * (arcHeight * Mathf.Sin(linear * Mathf.PI));
                transform.position = position;

                transform.rotation = startRotation * Quaternion.Euler(0f, flightSpinDegreesPerSecond * elapsed, 0f);

                float punch = 1f + (midFlightScalePunch - 1f) * Mathf.Sin(linear * Mathf.PI);
                transform.localScale = Vector3.Lerp(startScale, targetScale, eased) * punch;

                yield return null;
            }

            // Park it in the stack so it keeps following the camera from here on.
            _state = State.Stacked;
            transform.SetParent(stack.transform, worldPositionStays: false);
            transform.localPosition = slotLocalPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = targetScale;
        }
    }
}
