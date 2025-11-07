using System;
using System.Collections;
using ThiefSimulator.Input;
using UnityEngine;

namespace ThiefSimulator.Police
{
    [RequireComponent(typeof(PoliceOfficerData))]
    public class PoliceOfficerMovement : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _secondsPerTile = 0.35f;

        [Header("Dependencies")]
        [SerializeField] private Grid _grid;

        public event Action OnMovementFinished;

        public bool IsMoving { get; private set; }

        private PoliceOfficerData _data;
        private Coroutine _moveRoutine;

        private void Awake()
        {
            _data = GetComponent<PoliceOfficerData>();
            if (_grid == null)
            {
                Debug.LogError("[PoliceOfficerMovement] Grid is not assigned in the Inspector. Please assign it.");
            }
            if (_grid == null)
            {
                Debug.LogError("[PoliceOfficerMovement] Grid reference is missing.");
            }
        }

        public void MoveOneStep(Vector2Int targetRelativeTile)
        {
            if (IsMoving) { return; }
            if (_grid == null || InputManager.Instance == null)
            {
                Debug.LogWarning("[PoliceOfficerMovement] Cannot move because Grid or InputManager is missing.");
                return;
            }

            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
            }
            _moveRoutine = StartCoroutine(MoveRoutine(targetRelativeTile));
        }

        private IEnumerator MoveRoutine(Vector2Int targetRelativeTile)
        {
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;
            Vector3 startWorld = transform.position;
            Vector3 targetWorld = _grid.GetCellCenterWorld((Vector3Int)(targetRelativeTile + mapOrigin));

            IsMoving = true;
            float elapsed = 0f;

            while (elapsed < _secondsPerTile)
            {
                transform.position = Vector3.Lerp(startWorld, targetWorld, elapsed / _secondsPerTile);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetWorld;
            _data.SetTilePosition(targetRelativeTile);
            IsMoving = false;
            _moveRoutine = null;
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
    }
}
