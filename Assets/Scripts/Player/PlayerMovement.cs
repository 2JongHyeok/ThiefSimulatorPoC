using System;
using System.Collections;
using System.Collections.Generic;
using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using UnityEngine;

namespace ThiefSimulator.Player
{
    [RequireComponent(typeof(PlayerData))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("How many seconds it takes to move one tile.")]
        [SerializeField] private float _secondsPerTile = 0.5f;
        [SerializeField] private int _normalMoveCost = 1;
        [SerializeField] private int _encumberedMoveCost = 2;

        [Header("Dependencies")]
        [SerializeField] private Grid _grid;
        [SerializeField] private PlayerInventory _playerInventory;

        public event Action OnMovementFinished;

        private PlayerData _playerData;
        private bool _isMoving = false;

        private void Awake()
        {
            _playerData = GetComponent<PlayerData>();
            if (_grid == null) { Debug.LogError("[PlayerMovement] Grid is not assigned in the inspector!"); }
            if (_playerInventory == null)
            {
                _playerInventory = GetComponent<PlayerInventory>();
            }
        }

        public void StartMove(List<Vector2Int> path)
        {
            if (_isMoving) { return; }
            if (path == null || path.Count == 0) { return; }
            StartCoroutine(MoveRoutine(path));
        }

        private IEnumerator MoveRoutine(List<Vector2Int> path)
        {
            _isMoving = true;
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;

            foreach (var relativeTargetTile in path)
            {
                int cost = _playerInventory != null && _playerInventory.IsOverweight ? _encumberedMoveCost : _normalMoveCost;
                TimeManager.Instance.ResetIdleTimer();
                TimeManager.Instance.AdvanceTime(cost);

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

                transform.position = targetWorldPos;
                _playerData.SetTilePosition(relativeTargetTile);
            }

            _isMoving = false;
            OnMovementFinished?.Invoke();
        }
    }
}
