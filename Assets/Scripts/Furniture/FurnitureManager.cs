using System.Collections.Generic;
using UnityEngine;

namespace ThiefSimulator.Furniture
{
    public class FurnitureManager : MonoBehaviour
    {
        public static FurnitureManager Instance { get; private set; }

        private readonly Dictionary<Vector2Int, FurnitureContainer> _containers = new Dictionary<Vector2Int, FurnitureContainer>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterFurniture(FurnitureContainer container, Vector2Int relativePosition)
        {
            if (container == null) { return; }
            if (_containers.ContainsKey(relativePosition))
            {
                Debug.LogWarning($"[FurnitureManager] Overwriting furniture at {relativePosition}");
            }
            _containers[relativePosition] = container;
        }

        public void UnregisterFurniture(Vector2Int relativePosition)
        {
            if (_containers.ContainsKey(relativePosition))
            {
                _containers.Remove(relativePosition);
            }
        }

        public bool TryGetFurniture(Vector2Int relativePosition, out FurnitureContainer container)
        {
            return _containers.TryGetValue(relativePosition, out container);
        }
    }
}
