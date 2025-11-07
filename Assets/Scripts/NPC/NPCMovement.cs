using System;
using System.Collections;
using ThiefSimulator.Input;
using UnityEngine;

namespace ThiefSimulator.NPC
{
    /// <summary>
    /// Handles tile-by-tile movement for NPCs. Movement is triggered externally per in-game minute.
    /// </summary>
    [RequireComponent(typeof(NPCData), typeof(NPCController))]
    public class NPCMovement : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How many real-time seconds it takes to move one tile.")]
        [SerializeField] private float _secondsPerTile = 0.5f;

        [Header("Dependencies")]
        [SerializeField] private Grid _grid;

        public event Action OnMovementFinished;

        private NPCData _npcData;
        private NPCController _npcController;
        private Coroutine _moveCoroutine;

        private void Awake()
        {
            _npcData = GetComponent<NPCData>();
            _npcController = GetComponent<NPCController>();
            if (_grid == null) { Debug.LogError("[NPCMovement] Grid is not assigned in the inspector!"); }
        }

        /// <summary>
        /// Moves exactly one tile, using the same interpolation style as the Player.
        /// </summary>
        public void MoveOneStep(Vector2Int targetRelativeTile)
        {
            if (InputManager.Instance == null || _grid == null)
            {
                Debug.LogWarning("[NPCMovement] Cannot move. Grid or InputManager is missing.");
                return;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }
            _moveCoroutine = StartCoroutine(MoveRoutine(targetRelativeTile));
        }

        private IEnumerator MoveRoutine(Vector2Int targetRelativeTile)
        {
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;
            Vector3 startWorldPos = transform.position;
            Vector2Int absoluteTargetTile = targetRelativeTile + mapOrigin;
            Vector3 targetWorldPos = _grid.GetCellCenterWorld((Vector3Int)absoluteTargetTile);

            float elapsedTime = 0f;
            while (elapsedTime < _secondsPerTile)
            {
                transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, elapsedTime / _secondsPerTile);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.position = targetWorldPos;
            Vector2Int oldRelative = _npcData.CurrentTilePosition;
            _npcData.SetTilePosition(targetRelativeTile);
            _npcController.NotifyPositionChanged(oldRelative, targetRelativeTile);

            _moveCoroutine = null;
            OnMovementFinished?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_grid == null)
            {
                _grid = GetComponentInParent<Grid>();
            }
        }
#endif

        // Usage in Unity:
        // 1. Attach to an NPC GameObject alongside NPCData and NPCController.
        // 2. Assign the shared Grid reference and tweak Seconds Per Tile to match player feel.
        // 3. Let NPCController call MoveOneStep when TimeManager advances minutes.
    }
}
