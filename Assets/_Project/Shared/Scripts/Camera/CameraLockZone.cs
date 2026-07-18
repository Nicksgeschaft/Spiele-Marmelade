using UnityEngine;

namespace SpieleMarmelade.Shared.Cameras
{
    // Trigger volume that locks a SideScrollCameraRig onto lockAnchor once the player enters it -
    // typically placed at a level's exit so the ending shot holds even if the player walks back.
    [RequireComponent(typeof(Collider))]
    public class CameraLockZone : MonoBehaviour
    {
        [SerializeField] private SideScrollCameraRig cameraRig;
        [SerializeField] private Transform lockAnchor;
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (cameraRig == null || !other.CompareTag(playerTag))
            {
                return;
            }

            cameraRig.LockAt(lockAnchor != null ? lockAnchor : transform);
        }
    }
}
