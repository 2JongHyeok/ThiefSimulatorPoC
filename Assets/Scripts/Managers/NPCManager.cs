using System.Collections.Generic;
using ThiefSimulator.NPC;
using UnityEngine;

namespace ThiefSimulator.Managers
{
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        [Header("Configuration")]
        // Removed: [SerializeField] private NPCSchedule _npcSchedule;
        // Removed: [SerializeField] private int _patrolRadius = 6;

        private List<NPCController> _allNPCs;
        private Dictionary<Vector2Int, NPCController> _npcPositionLookup;
        private int _lastCheckedHour = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _allNPCs = new List<NPCController>();
            _npcPositionLookup = new Dictionary<Vector2Int, NPCController>();
        }

        private void OnEnable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged += HandleTimeChanged;
            }
            else
            {
                Debug.LogError("[NPCManager] TimeManager.Instance is null at OnEnable. Make sure TimeManager loads before NPCManager.");
            }
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
            }
        }

        /// <summary>
        /// Registers an NPC with the manager.
        /// </summary>
        /// <param name="npc">The NPCController to register.</param>
        /// <param name="position">The current relative grid position of the NPC.</param>
        public void RegisterNPC(NPCController npc, Vector2Int position)
        {
            if (!_allNPCs.Contains(npc))
            {
                _allNPCs.Add(npc);
            }

            if (_npcPositionLookup.ContainsKey(position))
            {
                Debug.LogWarning($"[NPCManager] Duplicate NPC position {position}. Overwriting existing entry.");
                _npcPositionLookup[position] = npc;
            }
            else
            {
                _npcPositionLookup.Add(position, npc);
            }
        }

        /// <summary>
        /// Unregisters an NPC from the manager.
        /// </summary>
        /// <param name="npc">The NPCController to unregister.</param>
        /// <param name="position">The current relative grid position of the NPC.</param>
        public void UnregisterNPC(NPCController npc, Vector2Int position)
        {
            if (_allNPCs.Contains(npc))
            {
                _allNPCs.Remove(npc);
            }
            if (_npcPositionLookup.ContainsKey(position) && _npcPositionLookup[position] == npc)
            {
                _npcPositionLookup.Remove(position);
            }
        }

        /// <summary>
        /// Updates an NPC's position in the lookup dictionary.
        /// </summary>
        /// <param name="npc">The NPCController whose position changed.</param>
        /// <param name="oldPosition">The NPC's previous relative grid position.</param>
        /// <param name="newPosition">The NPC's new relative grid position.</param>
        public void UpdateNPCPosition(NPCController npc, Vector2Int oldPosition, Vector2Int newPosition)
        {
            if (_npcPositionLookup.ContainsKey(oldPosition) && _npcPositionLookup[oldPosition] == npc)
            {
                _npcPositionLookup.Remove(oldPosition);
            }
            if (_npcPositionLookup.ContainsKey(newPosition))
            {
                Debug.LogWarning($"[NPCManager] NPC {npc.name} moved to {newPosition}, but another NPC is already there. Overwriting.");
            }
            _npcPositionLookup[newPosition] = npc;
        }

        /// <summary>
        /// Handles hourly updates for NPC schedules.
        /// </summary>
        private void HandleTimeChanged(int hour, int minute)
        {
            if (hour == _lastCheckedHour) return; // Only trigger on hour change

            _lastCheckedHour = hour;

            foreach (NPCController npc in _allNPCs)
            {
                npc.UpdateSchedule(hour, minute);
            }

            Debug.Log($"[NPCManager] Hour changed to {hour:D2}:00. Commanding all NPCs to update their schedules.");
        }

        /// <summary>
        /// Gets a HashSet of all current NPC positions.
        /// </summary>
        public HashSet<Vector2Int> GetAllNPCPositions()
        {
            return new HashSet<Vector2Int>(_npcPositionLookup.Keys);
        }

        /// <summary>
        /// Gets the NPCController at a specific relative grid position.
        /// </summary>
        /// <param name="position">The relative grid position to check.</param>
        /// <returns>The NPCController at the position, or null if none exists.</returns>
        public NPCController GetNPCAt(Vector2Int position)
        {
            _npcPositionLookup.TryGetValue(position, out NPCController npc);
            return npc;
        }
    }
}
