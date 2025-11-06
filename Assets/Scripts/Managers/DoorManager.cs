using System.Collections.Generic;
using ThiefSimulator.Objects;
using UnityEngine;

namespace ThiefSimulator.Managers
{
    public class DoorManager : MonoBehaviour
    {
        public static DoorManager Instance { get; private set; }

        private Dictionary<Vector2Int, Door> _doorLookup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _doorLookup = new Dictionary<Vector2Int, Door>();
        }

        public void RegisterDoor(Door door)
        {
            if (_doorLookup.ContainsKey(door.Position))
            {
                Debug.LogWarning($"[DoorManager] Duplicate door found at position {door.Position}. Overwriting existing entry.");
                _doorLookup[door.Position] = door;
            }
            else
            {
                _doorLookup.Add(door.Position, door);
            }
        }

        public void UnregisterDoor(Door door)
        {
            if (_doorLookup.ContainsKey(door.Position))
            {
                _doorLookup.Remove(door.Position);
            }
        }

        public bool IsDoorAt(Vector2Int relativePosition, out Door door)
        {
            return _doorLookup.TryGetValue(relativePosition, out door);
        }
    }
}
