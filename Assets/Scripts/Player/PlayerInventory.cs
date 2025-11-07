using System;
using System.Collections.Generic;
using ThiefSimulator.Items;
using UnityEngine;

namespace ThiefSimulator.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField, Tooltip("Maximum weight the player can carry before becoming encumbered.")]
        private float _carryCapacity = 20f;

        private readonly List<ItemData> _items = new List<ItemData>();
        private float _currentWeight;

        public event Action<IReadOnlyList<ItemData>, float> OnInventoryChanged;
        public event Action<bool> OnEncumbranceChanged;

        public IReadOnlyList<ItemData> Items => _items;
        public float CurrentWeight => _currentWeight;
        public float CarryCapacity => _carryCapacity;
        public bool IsOverweight => _currentWeight > _carryCapacity;

        public bool TryAddItem(ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[PlayerInventory] Attempted to add null item.");
                return false;
            }

            _items.Add(item);
            _currentWeight += Mathf.Max(0f, item.Weight);
            Debug.Log($"[PlayerInventory] Added {item.DisplayName}. Weight: {_currentWeight:0.0}/{_carryCapacity:0.0}");
            NotifyInventoryChanged();
            return true;
        }

        public bool RemoveItem(ItemData item)
        {
            if (item == null) { return false; }
            if (!_items.Remove(item)) { return false; }

            _currentWeight = Mathf.Max(0f, _currentWeight - Mathf.Max(0f, item.Weight));
            Debug.Log($"[PlayerInventory] Removed {item.DisplayName}. Weight: {_currentWeight:0.0}/{_carryCapacity:0.0}");
            NotifyInventoryChanged();
            return true;
        }

        private void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke(_items, _currentWeight);
            OnEncumbranceChanged?.Invoke(IsOverweight);
        }

        // Usage in Unity:
        // 1. Attach to the Player GameObject.
        // 2. Configure carry capacity in the Inspector.
        // 3. Call TryAddItem/RemoveItem from loot or furniture systems, and subscribe to events for UI updates.
    }
}
