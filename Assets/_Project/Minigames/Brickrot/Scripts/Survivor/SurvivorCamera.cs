using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Top-down follow camera for the Survivor half.
    //
    // Mirrors the project's shared TopDownCameraRig (offset + SmoothDamp + LookAt) so it follows and
    // frames the player exactly like every other top-down minigame here — then layers the Brickrot
    // camera shake on top. The original port sat at height 12 and never looked at the player, which
    // at brick scale (1 brick ≈ 0.0795 units) put the camera 150× too far out; hence "hängt nicht
    // richtig dran".
    //
    // Owns both position and rotation every frame, so the shake offset/roll can be added on top
    // without CameraShake ever writing the transform itself — they can't fight.
    public class SurvivorCamera : MonoBehaviour
    {
        [Tooltip("Kamera-Versatz zum Spieler, in Weltunits. Standard = geteilter TopDownCameraRig " +
                 "(leicht hinter und über dem Spieler, auf Brick-Maßstab abgestimmt).")]
        [SerializeField] private Vector3 offset = new(0f, 0.9f, -0.35f);

        [Tooltip("Nachzieh-Dämpfung (SmoothDamp). Höher = träger.")]
        [SerializeField] private float smoothTime = 0.15f;

        private Transform target;
        private CameraShake shake;
        private Vector3 followPosition;
        private Vector3 velocity;

        private void Awake()
        {
            shake = GetComponent<CameraShake>();
            if (shake == null) shake = gameObject.AddComponent<CameraShake>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;

            // Start already framed instead of whipping in from wherever the camera was placed.
            if (target != null)
            {
                followPosition = target.position + offset;
                transform.position = followPosition;
                transform.LookAt(target.position, Vector3.up);
            }
        }

        private void LateUpdate()
        {
            // LateUpdate so the camera reads the player's final position for this frame rather than
            // trailing it by one.
            if (target == null) return;

            followPosition = Vector3.SmoothDamp(followPosition, target.position + offset, ref velocity, smoothTime);

            transform.position = followPosition + (shake != null ? shake.CurrentOffset : Vector3.zero);

            // LookAt sets the base aim each frame; the shake roll rides on top of it.
            transform.LookAt(target.position, Vector3.up);
            if (shake != null && shake.CurrentRoll != 0f)
            {
                transform.rotation *= Quaternion.Euler(0f, 0f, shake.CurrentRoll);
            }
        }
    }
}
