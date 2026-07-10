using UnityEngine;

namespace GameJamUniverse.Shared.Items
{
    // Designer-authored item definition (health potion, key, quest item, ...). One asset per
    // item type; Inventory holds counts of these, never duplicates the data itself.
    [CreateAssetMenu(fileName = "Item_", menuName = "GameJam Universe/Item")]
    public class Item : ScriptableObject
    {
        [Tooltip("Stable unique id, e.g. 'health_potion'. Never reuse or change once shipped.")]
        public string itemId;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public bool stackable = true;
        public int maxStack = 99;
    }
}
