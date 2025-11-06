using System.Collections;
using System.Collections.Generic;
using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using ThiefSimulator.Pathfinding;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ThiefSimulator.NPC
{
    [RequireComponent(typeof(NPCData), typeof(NPCMovement))]
    public class NPCController : MonoBehaviour
    {
        private enum NPCState { Idle, Busy, Patrolling }

        [Header("Schedule")]
        [SerializeField] private NPCSchedule _npcSchedule;
        [SerializeField] private int _patrolRadius = 6;

        [Header("Dependencies")]
        [SerializeField] private Tilemap _obstacleTilemap;
        [SerializeField] private Grid _grid;

        [Header("Patrol Settings")]
        [SerializeField] private float _patrolInterval = 2f; // Time NPC waits at a patrol point
        
        private NPCData _npcData;
        private NPCMovement _npcMovement;
        private NPCState _currentState = NPCState.Idle;
        private Coroutine _patrolCoroutine;

        private void Awake()
        {
            _npcData = GetComponent<NPCData>();
            _npcMovement = GetComponent<NPCMovement>();
            if (_obstacleTilemap == null) { Debug.LogError("[NPCController] Obstacle Tilemap is not assigned!"); }
            if (_grid == null) { Debug.LogError("[NPCController] Grid is not assigned!"); }
            // Register with NPCManager
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.RegisterNPC(this, _npcData.CurrentTilePosition);
            }
        }

        private void OnEnable()
        {
            _npcMovement.OnMovementFinished += OnMovementFinished;
        }

        private void OnDisable()
        {
            _npcMovement.OnMovementFinished -= OnMovementFinished;
        }

        private void OnDestroy()
        {
            // Unregister from NPCManager
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.UnregisterNPC(this, _npcData.CurrentTilePosition);
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
        /// Commands the NPC to update its current schedule.
        /// Called by NPCManager every hour.
        /// </summary>
        public void UpdateSchedule(int currentHour, int currentMinute)
        {
            if (_npcSchedule == null)
            {
                Debug.LogWarning($"[NPCController] NPC {name} has no NPCSchedule assigned.");
                return;
            }
            
            int blockIndex = currentHour / 2; // Get the index for the current 2-hour block

            if (blockIndex >= _npcSchedule.hourlyTargetAreas.Count)
            {
                Debug.LogWarning($"[NPCController] Schedule not defined for hour block {currentHour:D2}:00. Block index {blockIndex} out of bounds for NPC {name}.");
                return; // No schedule for this block
            }

            Vector2Int targetArea = _npcSchedule.hourlyTargetAreas[blockIndex];
            
            Debug.Log($"[NPCController] {name} received new schedule for {currentHour:D2}:00 block. Target Area: {targetArea}");
            StartPatrol(targetArea, _patrolRadius);
        }

        public void StartPatrol(Vector2Int center, int radius)
        {
            if (_currentState == NPCState.Busy || _currentState == NPCState.Patrolling)
            {
                Debug.LogWarning("[NPCController] NPC is already busy or patrolling, ignoring new patrol command.");
                // If a patrol is active, stop it to accept new patrol command
                if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
                _currentState = NPCState.Idle; // Reset state to accept new command
            }

            _currentState = NPCState.Busy; // Busy while moving to the center
            _patrolCoroutine = StartCoroutine(PatrolRoutine(center, radius));
        }

        private IEnumerator PatrolRoutine(Vector2Int center, int radius)
        {
            // First, move to the center of the patrol area
            List<Vector2Int> pathToCenter = Pathfinder.FindPath(_npcData.CurrentTilePosition, center, _obstacleTilemap, InputManager.Instance.mapOrigin, NPCManager.Instance.GetAllNPCPositions());
            if (pathToCenter != null && pathToCenter.Count > 0)
            {
                _npcMovement.StartMove(pathToCenter);
                yield return new WaitUntil(() => _currentState == NPCState.Idle); // Wait for movement to finish
            }
            else
            {
                Debug.LogWarning($"[NPCController] Could not path to patrol center {center}. NPC will remain idle.");
                _currentState = NPCState.Idle;
                yield break;
            }

            _currentState = NPCState.Patrolling; // Now patrolling within the area
            Debug.Log($"[NPCController] Reached patrol center {center}. Starting patrol within radius {radius}.");

            while (true) // Patrol indefinitely until a new command is given
            {
                Vector2Int randomPatrolPoint = GetRandomWalkablePointInRadius(center, radius);
                if (randomPatrolPoint == _npcData.CurrentTilePosition) // Already there
                {
                    yield return new WaitForSeconds(_patrolInterval); // Wait before picking new point
                    continue;
                }

                List<Vector2Int> pathToPatrolPoint = Pathfinder.FindPath(_npcData.CurrentTilePosition, randomPatrolPoint, _obstacleTilemap, InputManager.Instance.mapOrigin, NPCManager.Instance.GetAllNPCPositions());
                if (pathToPatrolPoint != null && pathToPatrolPoint.Count > 0)
                {
                    _npcMovement.StartMove(pathToPatrolPoint);
                    yield return new WaitUntil(() => _currentState == NPCState.Idle); // Wait for movement to finish
                }
                else
                {
                    Debug.LogWarning($"[NPCController] Could not path to random patrol point {randomPatrolPoint}. Picking another.");
                    yield return new WaitForSeconds(0.5f); // Short wait to prevent busy loop
                }

                yield return new WaitForSeconds(_patrolInterval); // Wait before picking new point
            }
        }

        private Vector2Int GetRandomWalkablePointInRadius(Vector2Int center, int radius)
        {
            Vector2Int randomPoint;
            int attempts = 0;
            const int maxAttempts = 50;

            do
            {
                randomPoint = center + new Vector2Int(Random.Range(-radius, radius + 1), Random.Range(-radius, radius + 1));
                attempts++;
            } while (!Pathfinder.IsWalkable(randomPoint, _obstacleTilemap, InputManager.Instance.mapOrigin, NPCManager.Instance.GetAllNPCPositions()) && attempts < maxAttempts);

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning($"[NPCController] Failed to find a walkable patrol point near {center} within {radius} radius after {maxAttempts} attempts. Returning center.");
                return center; // Fallback
            }
            return randomPoint;
        }

        /// <summary>
        /// Sets a new path for the NPC to follow.
        /// </summary>
        /// <param name="path">The list of relative grid positions for the NPC to move along.</param>
        public void SetPath(List<Vector2Int> path)
        {
            if (_currentState == NPCState.Busy || _currentState == NPCState.Patrolling)
            {
                Debug.LogWarning("[NPCController] NPC is busy or patrolling, ignoring new path command.");
                // If a patrol is active, stop it to accept new path command
                if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
                _currentState = NPCState.Idle; // Reset state to accept new command
            }

            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("[NPCController] Received empty path. NPC will remain idle.");
                return;
            }

            _currentState = NPCState.Busy; // Busy while moving along the path
            _npcMovement.StartMove(path);
        }

        /// <summary>
        /// Called when the NPC finishes its movement.
        /// </summary>
        private void OnMovementFinished()
        {
            // If patrolling, it will pick a new point. Otherwise, it becomes idle.
            if (_currentState != NPCState.Patrolling)
            {
                _currentState = NPCState.Idle;
                Debug.Log("[NPCController] Movement finished. NPC is now idle.");
            }
            // If patrolling, the PatrolRoutine will continue and pick a new point.
        }

        /// <summary>
        /// Sets the NPC to a specific position without movement animation.
        /// Used for initial placement or teleportation.
        /// </summary>
        /// <param name="relativePosition">The relative grid position to place the NPC.</param>
        public void PlaceNPC(Vector2Int relativePosition)
        {
            _npcData.SetTilePosition(relativePosition);
            // Update visual position immediately
            Grid grid = FindObjectOfType<Grid>(); // Temporarily using FindObjectOfType for initial placement
            if (grid != null && InputManager.Instance != null)
            {
                Vector2Int absolutePos = relativePosition + InputManager.Instance.mapOrigin;
                transform.position = grid.GetCellCenterWorld((Vector3Int)absolutePos);
            }
            Debug.Log($"[NPCController] NPC placed at {relativePosition}.");
        }
    }
}
