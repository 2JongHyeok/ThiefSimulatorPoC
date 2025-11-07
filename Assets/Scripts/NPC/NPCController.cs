using System.Collections.Generic;
using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using ThiefSimulator.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ThiefSimulator.NPC
{
    /// <summary>
    /// Orchestrates NPC schedules and per-minute movement.
    /// </summary>
    [RequireComponent(typeof(NPCData), typeof(NPCMovement))]
    public class NPCController : MonoBehaviour
    {
        private enum NPCState { Idle, Busy, Patrolling }
        private enum MoveIntent { None, ToPatrolCenter, PatrolPoint, Custom }

        [Header("Schedule")]
        [SerializeField] private NPCSchedule _npcSchedule;
        [SerializeField] private int _patrolRadius = 6;

        [Header("Dependencies")]
        [SerializeField] private Tilemap _obstacleTilemap;
        [SerializeField] private Grid _grid;

        [Header("Patrol Settings")]
        [Tooltip("How many in-game minutes the NPC waits before picking the next patrol point.")]
        [SerializeField] private int _patrolIntervalMinutes = 1;

        private NPCData _npcData;
        private NPCMovement _npcMovement;
        private NPCState _currentState = NPCState.Idle;
        private MoveIntent _currentMoveIntent = MoveIntent.None;
        private Queue<Vector2Int> _currentPath = new Queue<Vector2Int>();
        private Vector2Int _currentPatrolCenter;
        private int _patrolCooldown;

        private void Awake()
        {
            Debug.Log($"[NPCController] {name} Awake called.");
            _npcData = GetComponent<NPCData>();
            _npcMovement = GetComponent<NPCMovement>();
            if (_obstacleTilemap == null) { Debug.LogError("[NPCController] Obstacle Tilemap is not assigned!"); }
            if (_grid == null) { Debug.LogError("[NPCController] Grid is not assigned!"); }

            InitializeTilePosition();

            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.RegisterNPC(this, _npcData.CurrentTilePosition);
            }
        }

        private void Start()
        {
            Vector2Int oldPosition = _npcData.CurrentTilePosition;
            InitializeTilePosition();
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.UpdateNPCPosition(this, oldPosition, _npcData.CurrentTilePosition);
            }
        }

        private void OnDestroy()
        {
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.UnregisterNPC(this, _npcData.CurrentTilePosition);
            }
        }

        private void InitializeTilePosition()
        {
            if (_grid == null || InputManager.Instance == null) { return; }

            Vector3Int cellPosition = _grid.WorldToCell(transform.position);
            Vector2Int absolutePos = (Vector2Int)cellPosition;
            Vector2Int relativePos = absolutePos - InputManager.Instance.mapOrigin;

            if (_npcData.CurrentTilePosition != relativePos)
            {
                _npcData.SetTilePosition(relativePos);
            }
        }

        public void NotifyPositionChanged(Vector2Int oldPosition, Vector2Int newPosition)
        {
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.UpdateNPCPosition(this, oldPosition, newPosition);
            }
        }

        /// <summary>
        /// Called by NPCManager whenever the game enters a new 2-hour block.
        /// </summary>
        public void UpdateSchedule(int currentHour, int currentMinute)
        {
            if (_npcSchedule == null)
            {
                Debug.LogWarning($"[NPCController] NPC {name} has no NPCSchedule assigned.");
                return;
            }

            int blockIndex = currentHour / 2;
            if (blockIndex >= _npcSchedule.hourlyTargetAreas.Count)
            {
                Debug.LogWarning($"[NPCController] Schedule not defined for hour block {currentHour:D2}:00.");
                return;
            }

            _currentPatrolCenter = _npcSchedule.hourlyTargetAreas[blockIndex];
            Debug.Log($"[NPCController] {name} received new patrol center {_currentPatrolCenter} for hour {currentHour:D2}.");

            if (_npcData.CurrentTilePosition == _currentPatrolCenter)
            {
                EnterPatrolState();
                return;
            }

            TryAssignPathTo(_currentPatrolCenter, MoveIntent.ToPatrolCenter);
        }

        /// <summary>
        /// Called once per in-game minute to progress movement.
        /// </summary>
        public void TickMinute(int currentHour, int currentMinute)
        {
            if (_currentPath.Count > 0)
            {
                Vector2Int nextTile = _currentPath.Dequeue();
                _npcMovement.MoveOneStep(nextTile);

                if (_currentPath.Count == 0)
                {
                    CompleteCurrentMoveIntent();
                }
                return;
            }

            if (_currentState != NPCState.Patrolling)
            {
                return;
            }

            if (_patrolCooldown > 0)
            {
                _patrolCooldown--;
                return;
            }

            QueueRandomPatrolDestination();
        }

        public void SetPath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("[NPCController] Received empty path. Ignoring command.");
                return;
            }

            AssignPath(path, MoveIntent.Custom);
        }

        public void PlaceNPC(Vector2Int relativePosition)
        {
            _npcData.SetTilePosition(relativePosition);
            if (_grid != null && InputManager.Instance != null)
            {
                Vector2Int absolutePos = relativePosition + InputManager.Instance.mapOrigin;
                transform.position = _grid.GetCellCenterWorld((Vector3Int)absolutePos);
            }
            Debug.Log($"[NPCController] NPC placed at {relativePosition}.");
        }

        private void QueueRandomPatrolDestination()
        {
            if (InputManager.Instance == null)
            {
                Debug.LogWarning("[NPCController] Cannot pick patrol destination. InputManager is missing.");
                return;
            }

            Vector2Int randomPoint = GetRandomWalkablePointInRadius(_currentPatrolCenter, _patrolRadius);
            if (randomPoint == _npcData.CurrentTilePosition)
            {
                _patrolCooldown = Mathf.Max(1, _patrolIntervalMinutes);
                return;
            }

            if (!TryAssignPathTo(randomPoint, MoveIntent.PatrolPoint))
            {
                _patrolCooldown = 1;
            }
        }

        private bool TryAssignPathTo(Vector2Int target, MoveIntent intent)
        {
            if (InputManager.Instance == null || NPCManager.Instance == null)
            {
                Debug.LogWarning("[NPCController] Cannot build path. InputManager or NPCManager is missing.");
                return false;
            }

            List<Vector2Int> path = Pathfinder.FindPath(
                _npcData.CurrentTilePosition,
                target,
                _obstacleTilemap,
                InputManager.Instance.mapOrigin,
                NPCManager.Instance.GetAllNPCPositions());

            if (path == null)
            {
                Debug.LogWarning($"[NPCController] Pathfinding failed for {name} to {target}.");
                return false;
            }

            if (path.Count == 0)
            {
                _currentMoveIntent = intent;
                CompleteCurrentMoveIntent();
                return true;
            }

            AssignPath(path, intent);
            return true;
        }

        private void AssignPath(List<Vector2Int> path, MoveIntent intent)
        {
            _currentPath = new Queue<Vector2Int>(path);
            _currentMoveIntent = intent;
            _currentState = NPCState.Busy;
        }

        private void CompleteCurrentMoveIntent()
        {
            switch (_currentMoveIntent)
            {
                case MoveIntent.ToPatrolCenter:
                    EnterPatrolState();
                    break;
                case MoveIntent.PatrolPoint:
                    _currentState = NPCState.Patrolling;
                    _patrolCooldown = Mathf.Max(1, _patrolIntervalMinutes);
                    break;
                default:
                    _currentState = NPCState.Idle;
                    break;
            }

            _currentMoveIntent = MoveIntent.None;
        }

        private void EnterPatrolState()
        {
            _currentState = NPCState.Patrolling;
            _patrolCooldown = 0;
            _currentMoveIntent = MoveIntent.None;
            Debug.Log($"[NPCController] {name} is patrolling around {_currentPatrolCenter}.");
        }

        private Vector2Int GetRandomWalkablePointInRadius(Vector2Int center, int radius)
        {
            if (InputManager.Instance == null || NPCManager.Instance == null)
            {
                return center;
            }

            Vector2Int randomPoint = center;
            int attempts = 0;
            const int maxAttempts = 50;

            while (attempts < maxAttempts)
            {
                randomPoint = center + new Vector2Int(Random.Range(-radius, radius + 1), Random.Range(-radius, radius + 1));
                attempts++;

                if (Pathfinder.IsWalkable(randomPoint, _obstacleTilemap, InputManager.Instance.mapOrigin, NPCManager.Instance.GetAllNPCPositions()))
                {
                    return randomPoint;
                }
            }

            Debug.LogWarning($"[NPCController] Failed to find patrol point near {center}. Staying put.");
            return center;
        }

        // Usage in Unity:
        // 1. Attach NPCController to each NPC along with NPCData and NPCMovement.
        // 2. Assign NPCSchedule, obstacle Tilemap, Grid, and patrol parameters.
        // 3. NPCManager.TickMinute will call TickMinute so NPCs move one tile per in-game minute.
    }
}
