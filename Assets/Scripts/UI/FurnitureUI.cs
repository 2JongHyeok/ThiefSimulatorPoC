using System.Collections.Generic;
using ThiefSimulator.Furniture;
using ThiefSimulator.Items;
using ThiefSimulator.Player;
using TMPro;
using UnityEngine;

namespace ThiefSimulator.UI
{
    public class FurnitureUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private Transform _furnitureContentRoot;
        [SerializeField] private InventoryItemButton _itemButtonPrefab;

        private readonly List<InventoryItemButton> _furnitureButtons = new List<InventoryItemButton>();
        private FurnitureContainer _currentContainer;

        private void Awake()
        {
            Hide();
        }

        public void Show(FurnitureContainer container)
        {
            if (container == null) { return; }
            if (_playerInventory == null)
            {
                _playerInventory = FindObjectOfType<PlayerInventory>();
            }

            _currentContainer = container;
            SetPanelState(true);

            if (_titleLabel != null)
            {
                _titleLabel.text = container.ContainerName;
            }

            RefreshFurnitureList();
        }

        public bool IsVisible => _panelRoot != null && _panelRoot.activeSelf;
        public FurnitureContainer CurrentContainer => _currentContainer;

        public void Hide()
        {
            SetPanelState(false);
            _currentContainer = null;
            ClearButtons(_furnitureButtons);
        }

        private void RefreshFurnitureList()
        {
            ClearButtons(_furnitureButtons);
            if (_furnitureContentRoot == null || _itemButtonPrefab == null || _currentContainer == null)
            {
                return;
            }

            foreach (ItemData item in _currentContainer.Items)
            {
                ItemData capturedItem = item;
                InventoryItemButton button = Instantiate(_itemButtonPrefab, _furnitureContentRoot);
                button.Initialize(capturedItem, _ => TakeItemFromFurniture(capturedItem));
                _furnitureButtons.Add(button);
            }
        }

        private void TakeItemFromFurniture(ItemData item)
        {
            if (_currentContainer == null || _playerInventory == null || item == null) { return; }
            if (!_currentContainer.TryRemoveItem(item)) { return; }
            _playerInventory.TryAddItem(item);
            RefreshFurnitureList();
        }

        public bool TryPlaceItemFromInventory(ItemData item)
        {
            if (_currentContainer == null || _playerInventory == null || item == null) { return false; }
            if (!_playerInventory.RemoveItem(item)) { return false; }
            _currentContainer.AddItem(item);
            RefreshFurnitureList();
            return true;
        }

        private void ClearButtons(List<InventoryItemButton> buttons)
        {
            foreach (InventoryItemButton button in buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            buttons.Clear();
        }

        private void SetPanelState(bool isActive)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(isActive);
            }
        }

        // Usage in Unity:
        // 1. FurniturePanel 하나에 제목 + 리스트를 구성하고, 이 스크립트에 Panel Root, Title, Content Root, PlayerInventory, ItemButton 프리팹을 연결하세요.
        // 2. PlayerController가 Show(container)를 호출하면 패널이 열리고, 플레이어 인벤토리는 별도의 InventoryUI에서 표시/입력됩니다.
    }
}
