using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

namespace ThiefSimulator.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("The absolute grid coordinate that should be treated as the (0,0) origin of the map.")]
        public Vector2Int mapOrigin;

        [SerializeField] private Grid _grid;
        [SerializeField] public Tilemap _obstacleTilemap;

        public static event Action<Vector2Int> OnTileClicked;
        public static event Action<Vector2Int> OnDirectionInput;
        public static event Action OnInteractInput;
        public static event Action OnInventoryToggle;
        public static event Action OnCancelInput;

        private Camera _mainCamera;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _mainCamera = Camera.main;
            if (_grid == null) { Debug.LogError("[InputManager] Grid is not assigned in the inspector!"); }
            Debug.Log("[InputManager] Initialized.");
        }

        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick(Mouse.current.position.ReadValue());
            }

            HandleKeyboard();
        }

        private void HandleClick(Vector2 screenPosition)
        {
            if (_grid == null) return;

            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
            Vector3Int cellPosition = _grid.WorldToCell(worldPosition);
            Vector2Int absoluteGridPosition = (Vector2Int)cellPosition;
            Vector2Int relativeGridPosition = absoluteGridPosition - mapOrigin;

            OnTileClicked?.Invoke(relativeGridPosition);
        }

        private void HandleKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) { return; }

            if (keyboard.wKey.wasPressedThisFrame)
            {
                OnDirectionInput?.Invoke(Vector2Int.up);
            }
            if (keyboard.sKey.wasPressedThisFrame)
            {
                OnDirectionInput?.Invoke(Vector2Int.down);
            }
            if (keyboard.aKey.wasPressedThisFrame)
            {
                OnDirectionInput?.Invoke(Vector2Int.left);
            }
            if (keyboard.dKey.wasPressedThisFrame)
            {
                OnDirectionInput?.Invoke(Vector2Int.right);
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                OnInteractInput?.Invoke();
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                OnInventoryToggle?.Invoke();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                OnCancelInput?.Invoke();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // For the gizmo, we can use the assigned grid or try to find it for editor convenience.
            if (_grid == null)
            {
                Debug.LogError("[InputManager] Grid is not assigned in the Inspector. Please assign it.");
                return;
            }
            Grid grid = _grid;
            if (grid == null) return;
            Vector3 originWorldPos = grid.GetCellCenterWorld((Vector3Int)mapOrigin);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(originWorldPos, grid.cellSize);
        }
#endif
    }
}
