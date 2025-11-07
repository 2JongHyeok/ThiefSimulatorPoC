using UnityEngine;

namespace ThiefSimulator.Items
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "ThiefSimulator/Item Data")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [SerializeField, Min(0f)] private float _weight = 1f;
        [SerializeField] private Sprite _icon;

        public string DisplayName => _displayName;
        public float Weight => _weight;
        public Sprite Icon => _icon;
    }
}
