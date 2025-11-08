using System;
using System.Collections.Generic;
using ThiefSimulator.Items;
using ThiefSimulator.Player;
using UnityEngine;

namespace ThiefSimulator.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private InventoryItemButton _itemButtonPrefab;

        private readonly List<InventoryItemButton> _spawnedButtons = new List<InventoryItemButton>();

        public event Action<ItemData> OnItemClicked;

        private void Awake()
        {
            if (_panelRoot == null)
            {
                _panelRoot = gameObject;
            }
        }

        private void OnEnable()
        {
            if (_playerInventory == null)
            {
                _playerInventory = FindObjectOfType<PlayerInventory>();
            }

            if (_playerInventory != null)
            {
                _playerInventory.OnInventoryChanged += HandleInventoryChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_playerInventory != null)
            {
                _playerInventory.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;

        public void Toggle()
        {
            if (IsVisible) { Hide(); }
            else { Show(); }
        }

        public void Show()
        {
            if (_panelRoot == null) { return; }
            _panelRoot.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_panelRoot == null) { return; }
            _panelRoot.SetActive(false);
        }

        private void HandleInventoryChanged(IReadOnlyList<ItemData> items, float _)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_contentRoot == null || _itemButtonPrefab == null || _playerInventory == null)
            {
                return;
            }

            foreach (var button in _spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _spawnedButtons.Clear();

            foreach (ItemData item in _playerInventory.Items)
            {
                InventoryItemButton button = Instantiate(_itemButtonPrefab, _contentRoot);
                button.Initialize(item, HandleItemClicked);
                _spawnedButtons.Add(button);
            }
        }

        private void HandleItemClicked(ItemData item)
        {
            Debug.Log($"[InventoryUI] Clicked {item.DisplayName}");
            OnItemClicked?.Invoke(item);
        }

        // Usage in Unity:
        // 1. Create a UI panel with a Vertical Layout + Content root.
        // 2. Assign the panel root, content, and an InventoryItemButton prefab (with TMP text and Button).
        // 3. Attach this script and PlayerInventory reference; Tab key will toggle visibility.
    }
}
