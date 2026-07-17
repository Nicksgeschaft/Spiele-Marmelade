using System.Collections;
using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Shared.World
{
    // Makes a brick-built sign/logo clickable as a genuine 3D in-world button — no uGUI/Canvas
    // involved. Needs a Collider covering the whole text somewhere on this object or its
    // children (the Brick Text Generator adds one automatically when "Als Button nutzbar" is
    // checked). Uses the new Input System exclusively (Active Input Handling = Input System
    // Package in this project), never the legacy Input class.
    //
    // Juicy by default: grows on hover, and on click the actual bricks it's built from fly apart
    // (manually integrated, not Rigidbody/PhysX) before OnClicked fires — gives menu navigation a
    // beat of weight instead of firing instantly. Runs on unscaled time throughout, since these
    // buttons must stay fully responsive on the Pause screen where Time.timeScale is 0.
    public class BrickTextButton : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float  maxDistance = 100f;
        [SerializeField] private string clickSfxId;

        [Header("── Hover ──────────────────────────────")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float hoverLerpSpeed = 10f;

        [Header("── Klick-Zerfall ──────────────────────")]
        [Tooltip("Wie stark die Bricks beim Klick auseinanderfliegen.")]
        [SerializeField] private float shatterForce = 1.5f;
        [Tooltip("Wie stark sich die Bricks beim Auseinanderfliegen drehen.")]
        [SerializeField] private float shatterTorque = 180f;
        [Tooltip("Wartezeit nach dem Zerfall, bevor OnClicked tatsächlich feuert (Sekunden).")]
        [SerializeField] private float actionDelay = 1.2f;
        [SerializeField] private float gravity = 4f;

        public UnityEvent OnClicked;

        private Vector3 _restScale;
        private bool _activated;

        private List<Transform> _bricks;
        private Vector3[] _restLocalPositions;
        private Quaternion[] _restLocalRotations;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
            _restScale = transform.localScale;

            _bricks = CollectBricks();
            _restLocalPositions = new Vector3[_bricks.Count];
            _restLocalRotations = new Quaternion[_bricks.Count];
            for (int i = 0; i < _bricks.Count; i++)
            {
                _restLocalPositions[i] = _bricks[i].localPosition;
                _restLocalRotations[i] = _bricks[i].localRotation;
            }
        }

        private void OnEnable()
        {
            // The Menu Flow system shows/hides whole screens by toggling this GameObject's
            // active state rather than rebuilding it — without this, a button that already
            // shattered once (e.g. Resume, after Pause → Resume → Pause again) would stay in its
            // scattered end state forever instead of looking like a fresh button again.
            ResetBricks();
        }

        private void ResetBricks()
        {
            _activated = false;
            transform.localScale = _restScale;
            for (int i = 0; i < _bricks.Count; i++)
            {
                if (_bricks[i] == null) continue;
                _bricks[i].localPosition = _restLocalPositions[i];
                _bricks[i].localRotation = _restLocalRotations[i];
            }
        }

        private void Update()
        {
            if (_activated) return;
            if (raycastCamera == null) raycastCamera = Camera.main;
            if (raycastCamera == null || Mouse.current == null) return;

            var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hovering = Physics.Raycast(ray, out var hit, maxDistance) && hit.collider.transform.IsChildOf(transform);

            var targetScale = hovering ? _restScale * hoverScale : _restScale;
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * hoverLerpSpeed);

            if (hovering && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _activated = true;
                SfxPlayer.PlayUi(clickSfxId);
                StartCoroutine(ShatterAndInvoke());
            }
        }

        // Flings every brick this sign is actually built from outward/upward with a random
        // impulse, then invokes OnClicked once they've had a couple seconds to fall apart.
        // Manually integrated on unscaled time rather than Rigidbody/PhysX — Unity's physics
        // step is scaled by Time.timeScale, which would freeze the effect mid-air on the Pause
        // screen (Time.timeScale = 0) instead of animating.
        private IEnumerator ShatterAndInvoke()
        {
            var velocities = new Vector3[_bricks.Count];
            var spins = new Vector3[_bricks.Count];

            for (int i = 0; i < _bricks.Count; i++)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.3f; // bias upward so bricks visibly pop off, not just sideways
                velocities[i] = dir.normalized * shatterForce;
                spins[i] = Random.insideUnitSphere * shatterTorque;
            }

            float elapsed = 0f;
            while (elapsed < actionDelay)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;

                for (int i = 0; i < _bricks.Count; i++)
                {
                    if (_bricks[i] == null) continue;
                    velocities[i] += Vector3.down * gravity * dt;
                    _bricks[i].position += velocities[i] * dt;
                    _bricks[i].Rotate(spins[i] * dt, Space.World);
                }

                yield return null;
            }

            OnClicked?.Invoke();
        }

        private List<Transform> CollectBricks()
        {
            var bricks = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.GetComponentInChildren<Renderer>() != null) bricks.Add(child);
            }
            return bricks;
        }
    }
}
