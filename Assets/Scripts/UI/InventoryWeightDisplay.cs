using System.Collections.Generic;
using ThiefSimulator.Player;
using TMPro;
using UnityEngine;

namespace ThiefSimulator.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class InventoryWeightDisplay : MonoBehaviour
    {
        [SerializeField] private PlayerInventory _playerInventory;
        [SerializeField] private string _format = "Weight: {0:0.0}/{1:0.0}";
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _overweightColor = Color.red;

        private TextMeshProUGUI _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
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

        private void HandleInventoryChanged(IReadOnlyList<Items.ItemData> _, float __)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_label == null || _playerInventory == null) { return; }

            float current = _playerInventory.CurrentWeight;
            float capacity = Mathf.Max(0.01f, _playerInventory.CarryCapacity);

            _label.text = string.Format(_format, current, capacity);
            _label.color = _playerInventory.IsOverweight ? _overweightColor : _normalColor;
        }

        // Usage in Unity:
        // 1. Attach to a TextMeshProUGUI element on the inventory panel.
        // 2. Assign PlayerInventory (optional, auto-detects first instance).
        // 3. Customize format/colors as needed.
    }
}
