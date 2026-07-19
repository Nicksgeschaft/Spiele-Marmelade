using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpieleMarmelade.Shared.World
{
    // Makes the individual bricks of a brick-built sign knockable: click one and it comes loose, gets
    // a Rigidbody and tumbles away. Purely decorative - unlike BrickTextButton this never fires an
    // action, so it's safe to put on a title rather than a button.
    //
    // Uses real physics (not BrickTextButton's manual integration) so the tumble looks natural. That's
    // only sane because titles live on menu screens running at normal time scale; on the Pause screen
    // (timeScale 0) PhysX would freeze them mid-air, which is why BrickTextButton hand-integrates.
    public class FallingTitleBricks : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private string knockSfxId = "brick_break";

        [Tooltip("Seitlicher Schubs, damit der Brick beim Anklicken wegkippt statt nur zu fallen.")]
        [SerializeField] private float knockImpulse = 1.2f;

        [Tooltip("Sekunden bis ein abgefallener Brick verschwindet. Er ist bis dahin längst aus dem Bild.")]
        [SerializeField] private float despawnDelay = 4f;

        private readonly List<KnockedBrick> _knocked = new();

        // Everything needed to put a brick back exactly where it started, plus when to give up on it.
        private struct KnockedBrick
        {
            public Transform Brick;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public float DespawnAt;
        }

        public void SetRaycastCamera(Camera camera) => raycastCamera = camera;

        private void Awake()
        {
            if (raycastCamera == null) raycastCamera = Camera.main;
        }

        // The Menu Flow system shows and hides screens by toggling this object, so leaving the screen
        // and coming back rebuilds the title - otherwise a sign someone had fun with once would stay
        // permanently half-demolished.
        private void OnEnable() => RestoreAll();

        private void RestoreAll()
        {
            foreach (KnockedBrick knocked in _knocked)
            {
                if (knocked.Brick == null) continue;

                // The Rigidbody has to go before re-parenting, or PhysX keeps simulating the brick
                // against its restored transform and drags it straight back out of place.
                Rigidbody body = knocked.Brick.GetComponent<Rigidbody>();
                if (body != null) Destroy(body);

                foreach (Collider brickCollider in knocked.Brick.GetComponentsInChildren<Collider>(true))
                {
                    brickCollider.enabled = true;
                }

                knocked.Brick.SetParent(transform, worldPositionStays: false);
                knocked.Brick.localPosition = knocked.LocalPosition;
                knocked.Brick.localRotation = knocked.LocalRotation;
            }

            _knocked.Clear();
        }

        private void Update()
        {
            DespawnExpired();

            if (raycastCamera == null) raycastCamera = Camera.main;
            if (raycastCamera == null || Mouse.current == null) return;
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            var ray = raycastCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance)) return;
            if (!hit.collider.transform.IsChildOf(transform)) return;

            KnockOff(DirectChildOf(hit.collider.transform));
        }

        // Timed by hand rather than with Destroy(obj, delay), because that can't be called off - and
        // leaving the screen has to be able to reclaim a brick that was still falling.
        private void DespawnExpired()
        {
            for (int i = _knocked.Count - 1; i >= 0; i--)
            {
                KnockedBrick knocked = _knocked[i];
                if (knocked.Brick != null && Time.unscaledTime < knocked.DespawnAt) continue;

                if (knocked.Brick != null) Destroy(knocked.Brick.gameObject);
                _knocked.RemoveAt(i);
            }
        }

        // The raycast can land on a mesh nested below the brick, but it's the brick (a direct child of
        // this sign) that has to come loose - detaching the mesh alone would leave a hollow brick.
        private Transform DirectChildOf(Transform hitTransform)
        {
            Transform current = hitTransform;
            while (current != null && current.parent != transform)
            {
                current = current.parent;
            }
            return current;
        }

        private void KnockOff(Transform brick)
        {
            if (brick == null || brick.GetComponent<Rigidbody>() != null) return;

            SfxPlayer.PlayUi(knockSfxId);

            _knocked.Add(new KnockedBrick
            {
                Brick = brick,
                LocalPosition = brick.localPosition,
                LocalRotation = brick.localRotation,
                DespawnAt = Time.unscaledTime + Mathf.Max(0.1f, despawnDelay),
            });

            // Unparented so the sign's own scale doesn't apply to a body PhysX is simulating, and so
            // the brick keeps falling independently of whatever the title does.
            brick.SetParent(null, worldPositionStays: true);

            // The menu stage floats far above the actual level, so a brick left solid would spend a
            // few seconds falling and then land in the middle of the game. It only needs to tumble
            // prettily on its way off screen - there's nothing up here for it to hit anyway.
            foreach (Collider brickCollider in brick.GetComponentsInChildren<Collider>(true))
            {
                brickCollider.enabled = false;
            }

            var body = brick.gameObject.AddComponent<Rigidbody>();
            body.AddForce(new Vector3(Random.Range(-1f, 1f), 0.4f, 0f).normalized * knockImpulse, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * knockImpulse, ForceMode.Impulse);
        }
    }
}
