using SpieleMarmelade.Shared.Audio;
using UnityEngine;

namespace SpieleMarmelade.Shared.Items
{
    // Place in a level with an Item assigned — works immediately, no wiring needed (same
    // "drop it in and it just works" pattern as LevelExitTrigger). Adds count to whatever
    // tagged "Player" walks into it, then removes itself.
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private Item item;
        [SerializeField] private int count = 1;
        [SerializeField] private string pickupSfxId;

        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (item == null || !other.CompareTag("Player")) return;

            var inventory = other.GetComponentInParent<Inventory>();
            if (inventory == null) return;

            inventory.AddItem(item, count);
            SfxPlayer.Play(pickupSfxId);
            Destroy(gameObject);
        }
    }
}
