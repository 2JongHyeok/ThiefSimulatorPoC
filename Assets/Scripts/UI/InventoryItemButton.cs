using System;
using ThiefSimulator.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThiefSimulator.UI
{
    [RequireComponent(typeof(Button))]
    public class InventoryItemButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private string _format = "{0} ({1:0.0}kg)";

        private ItemData _item;
        private Action<ItemData> _onClicked;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (TryGetComponent<Button>(out var button))
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Initialize(ItemData item, Action<ItemData> onClicked)
        {
            _item = item;
            _onClicked = onClicked;
            UpdateLabel();
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_item);
        }

        private void UpdateLabel()
        {
            if (_label == null)
            {
                return;
            }

            if (_item == null)
            {
                _label.text = "(Unknown)";
                return;
            }

            _label.text = string.Format(_format, _item.DisplayName, _item.Weight);
        }
    }
}
