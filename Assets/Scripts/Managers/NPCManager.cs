using System.Collections;
using System.Collections.Generic;
using ThiefSimulator.NPC;
using UnityEngine;

namespace ThiefSimulator.Managers
{
    [DefaultExecutionOrder(-200)]
    public class NPCManager : MonoBehaviour
    {
        public static NPCManager Instance { get; private set; }

        [Header("Configuration")]
        // Removed: [SerializeField] private NPCSchedule _npcSchedule;
        // Removed: [SerializeField] private int _patrolRadius = 6;

        private List<NPCController> _allNPCs;
        private Dictionary<Vector2Int, NPCController> _npcPositionLookup;
        private int _lastCheckedHour = -1;
        private int _lastProcessedAbsoluteMinute = -1;
        private Coroutine _subscriptionRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[NPCManager] Awake called.");
            _allNPCs = new List<NPCController>();
            _npcPositionLookup = new Dictionary<Vector2Int, NPCController>();
        }

        private void OnEnable()
        {
            TrySubscribeToTimeManager();
        }

        private void OnDisable()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
            }
            if (_subscriptionRoutine != null)
            {
                StopCoroutine(_subscriptionRoutine);
                _subscriptionRoutine = null;
            }
        }

        private void TrySubscribeToTimeManager()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
                TimeManager.Instance.OnTimeChanged += HandleTimeChanged;
                if (_subscriptionRoutine != null)
                {
                    StopCoroutine(_subscriptionRoutine);
                    _subscriptionRoutine = null;
                }

                var (hour, minute) = TimeManager.Instance.GetCurrentTime();
                _lastCheckedHour = hour;
                _lastProcessedAbsoluteMinute = hour * 60 + minute;
                Debug.Log($"[NPCManager] Subscribed to TimeManager. Baseline time set to {hour:D2}:{minute:D2}.");
                UpdateAllNPCSchedules(hour, minute);
            }
            else if (_subscriptionRoutine == null)
            {
                _subscriptionRoutine = StartCoroutine(WaitForTimeManager());
            }
        }

        private IEnumerator WaitForTimeManager()
        {
            Debug.Log("[NPCManager] Waiting for TimeManager to initialize...");
            while (TimeManager.Instance == null)
            {
                yield return null;
            }
            _subscriptionRoutine = null;
            TrySubscribeToTimeManager();
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

            if (TimeManager.Instance != null)
            {
                var (hour, minute) = TimeManager.Instance.GetCurrentTime();
                npc.UpdateSchedule(hour, minute);
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
        /// Handles time changes broadcast by the TimeManager.
        /// </summary>
        private void HandleTimeChanged(int hour, int minute)
        {
            if (_lastProcessedAbsoluteMinute < 0)
            {
                _lastProcessedAbsoluteMinute = hour * 60 + minute;
                return;
            }

            int previousModulo = _lastProcessedAbsoluteMinute % (24 * 60);
            int currentModulo = (hour * 60) + minute;
            int deltaMinutes = currentModulo - previousModulo;
            if (deltaMinutes < 0)
            {
                deltaMinutes += 24 * 60;
            }

            if (deltaMinutes == 0)
            {
                return;
            }

            for (int step = 1; step <= deltaMinutes; step++)
            {
                int absoluteMinute = _lastProcessedAbsoluteMinute + step;
                int processedHour = (absoluteMinute / 60) % 24;
                int processedMinute = absoluteMinute % 60;

                if (processedHour != _lastCheckedHour)
                {
                    _lastCheckedHour = processedHour;
                    UpdateAllNPCSchedules(processedHour, processedMinute);
                    Debug.Log($"[NPCManager] Hour changed to {processedHour:D2}:00. Commanding all NPCs to update their schedules.");
                }

                TickAllNPCs(processedHour, processedMinute);
            }

            _lastProcessedAbsoluteMinute += deltaMinutes;
        }

        private void UpdateAllNPCSchedules(int hour, int minute)
        {
            foreach (NPCController npc in _allNPCs)
            {
                npc.UpdateSchedule(hour, minute);
            }
        }

        private void TickAllNPCs(int hour, int minute)
        {
            foreach (NPCController npc in _allNPCs)
            {
                npc.TickMinute(hour, minute);
            }
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
