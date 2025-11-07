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
            if (_label != null)
            {
                _label.text = item != null ? item.DisplayName : "(Unknown)";
            }
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_item);
        }
    }
}
