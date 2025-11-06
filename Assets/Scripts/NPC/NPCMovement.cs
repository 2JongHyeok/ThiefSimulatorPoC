using System;
using System.Collections;
using System.Collections.Generic;
using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using UnityEngine;

namespace ThiefSimulator.NPC
{
    [RequireComponent(typeof(NPCData), typeof(NPCController))]
    public class NPCMovement : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How many seconds it takes for the NPC to move one tile.")]
        [SerializeField] private float _secondsPerTile = 0.5f;

        [Header("Dependencies")]
        [SerializeField] private Grid _grid;

        public event Action OnMovementFinished;

        private NPCData _npcData;
        private NPCController _npcController; // Added
        private bool _isMoving = false;

        private void Awake()
        {
            _npcData = GetComponent<NPCData>();
            _npcController = GetComponent<NPCController>(); // Added
            if (_grid == null) { Debug.LogError("[NPCMovement] Grid is not assigned in the inspector!"); }
        }

        /// <summary>
        /// Starts the movement process along a given path.
        /// </summary>
        /// <param name="path">A list of grid coordinates to move through.</param>
        public void StartMove(List<Vector2Int> path)
        {
            if (_isMoving) { return; }
            if (path == null || path.Count == 0) { 
                // If path is just one point, it means stand still at current position
                if (path != null && path.Count == 1 && path[0] == _npcData.CurrentTilePosition)
                {
                    // Already at target, just finish movement
                    OnMovementFinished?.Invoke();
                    return;
                }
                Debug.LogWarning("[NPCMovement] Attempted to move with an empty path.");
                OnMovementFinished?.Invoke(); // Signal finished even if no movement
                return;
            }

            StartCoroutine(MoveRoutine(path));
        }

        private IEnumerator MoveRoutine(List<Vector2Int> path)
        {
            _isMoving = true;
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin; // Assuming InputManager's mapOrigin is universal

            foreach (var relativeTargetTile in path)
            {
                Vector2Int oldRelativePosition = _npcData.CurrentTilePosition; // Stored old position

                Vector3 startWorldPos = transform.position;
                Vector2Int absoluteTargetTile = relativeTargetTile + mapOrigin;
                Vector3 targetWorldPos = _grid.GetCellCenterWorld((Vector3Int)absoluteTargetTile);

                float elapsedTime = 0f;
                while (elapsedTime < _secondsPerTile)
                {
                    transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, elapsedTime / _secondsPerTile);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                // Snap to final position and update data
                transform.position = targetWorldPos;
                _npcData.SetTilePosition(relativeTargetTile);

                // Notify manager of position change
                _npcController.NotifyPositionChanged(oldRelativePosition, relativeTargetTile); // Added
            }

            _isMoving = false;
            OnMovementFinished?.Invoke();
        }
    }
}
