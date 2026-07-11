using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Shared.Items
{
    [Serializable]
    public struct InventorySlot
    {
        public Item item;
        public int count;
    }

    // Runtime-only item storage (not persisted via SaveData — same category as Health/
    // CharacterStats: in-run state, not meta-progression). Add to the player alongside
    // ItemPickup/AbilitySlot; UI (PlayerHudScreensController) listens to OnChanged to refresh.
    public class Inventory : MonoBehaviour
    {
        private readonly List<InventorySlot> _slots = new();

        public event Action OnChanged;

        public IReadOnlyList<InventorySlot> Slots => _slots;

        public void AddItem(Item item, int count = 1)
        {
            if (item == null || count <= 0) return;

            if (item.stackable)
            {
                int index = _slots.FindIndex(s => s.item == item);
                if (index >= 0)
                {
                    InventorySlot slot = _slots[index];
                    slot.count = Mathf.Min(slot.count + count, item.maxStack);
                    _slots[index] = slot;
                    OnChanged?.Invoke();
                    return;
                }
            }

            _slots.Add(new InventorySlot { item = item, count = Mathf.Min(count, item.maxStack) });
            OnChanged?.Invoke();
        }

        public bool RemoveItem(Item item, int count = 1)
        {
            if (item == null || count <= 0) return false;

            int index = _slots.FindIndex(s => s.item == item);
            if (index < 0 || _slots[index].count < count) return false;

            InventorySlot slot = _slots[index];
            slot.count -= count;
            if (slot.count <= 0) _slots.RemoveAt(index);
            else _slots[index] = slot;

            OnChanged?.Invoke();
            return true;
        }

        public int GetCount(Item item)
        {
            if (item == null) return 0;
            int index = _slots.FindIndex(s => s.item == item);
            return index >= 0 ? _slots[index].count : 0;
        }
    }
}
