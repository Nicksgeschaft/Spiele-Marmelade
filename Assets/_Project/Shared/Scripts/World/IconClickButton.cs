using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Shared.World
{
    // A brick-built icon that acts as a plain push button - same click detection as IconToggleButton
    // (Physics.Raycast against a Collider on this object or its children) but it just fires
    // OnClicked, with no state and no icon swapping. Used for the Character Creator's arrows, where
    // clicking repeatedly is the point.
    //
    // Deliberately not BrickTextButton: that one shatters itself and delays the action by over a
    // second, which is right for a one-way menu jump and wrong for a control you click six times in a
    // row.
    public class IconClickButton : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float  maxDistance = 100f;
        [SerializeField] private string clickSfxId = "click";

        [Header("── Hover ──────────────────────────────")]
        [SerializeField] private float hoverScale = 1.2f;
        [SerializeField] private float hoverLerpSpeed = 12f;

        [Tooltip("Kurzer Stauch-Effekt beim Klicken, damit der Klick sichtbar quittiert wird.")]
        [SerializeField] private float pressScale = 0.85f;
        [SerializeField] private float pressRecoverSpeed = 8f;

        public UnityEvent OnClicked = new();

        private Vector3 _restScale;
        private float _pressAmount;

        public void SetRaycastCamera(Camera camera) => raycastCamera = camera;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
            _restScale = transform.localScale;
        }

        // The menu shows/hides screens by toggling GameObjects, so a button left mid-press when its
        // screen was hidden has to come back at rest rather than still squashed.
        private void OnEnable()
        {
            _pressAmount = 0f;
            transform.localScale = _restScale;
        }

        private void Update()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
            if (raycastCamera == null || Mouse.current == null) return;

            var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            bool hovering = Physics.Raycast(ray, out RaycastHit hit, maxDistance)
                            && hit.collider.transform.IsChildOf(transform);

            // Unscaled time throughout: these buttons have to stay responsive on menu screens shown
            // while the game is paused (Time.timeScale = 0).
            _pressAmount = Mathf.MoveTowards(_pressAmount, 0f, Time.unscaledDeltaTime * pressRecoverSpeed);

            float target = hovering ? hoverScale : 1f;
            target *= Mathf.Lerp(1f, pressScale, _pressAmount);
            transform.localScale = Vector3.Lerp(transform.localScale, _restScale * target,
                Time.unscaledDeltaTime * hoverLerpSpeed);

            if (hovering && Mouse.current.leftButton.wasPressedThisFrame)
            {
                _pressAmount = 1f;
                SfxPlayer.PlayUi(clickSfxId);
                OnClicked?.Invoke();
            }
        }
    }
}
