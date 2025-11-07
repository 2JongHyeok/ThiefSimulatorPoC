using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using UnityEngine;

namespace ThiefSimulator.Objects
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Door : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The grid position of this door, relative to the map origin.")]
        [SerializeField] private Vector2Int _position;
        [SerializeField] private bool _isOpen = false;
        [SerializeField] private Grid _grid; // Assign in inspector

        [Header("Visuals")]
        [SerializeField] private Color _openColor = Color.green;
        [SerializeField] private Color _closedColor = Color.red;

        private SpriteRenderer _spriteRenderer;

        public Vector2Int Position => _position;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_grid == null) { Debug.LogError("[Door] Grid is not assigned in the inspector!"); }
            
            UpdateVisualState();
        }

        private void Start()
        {
            // Position the object at its logical position for consistency.
            if (_grid != null && InputManager.Instance != null)
            {
                Vector2Int absolutePos = _position + InputManager.Instance.mapOrigin;
                transform.position = _grid.GetCellCenterWorld((Vector3Int)absolutePos);
            }

            // Register itself with the manager
            if (DoorManager.Instance != null)
            {
                DoorManager.Instance.RegisterDoor(this);
            }
        }

        private void OnDestroy()
        {
            // Unregister itself from the manager
            if (DoorManager.Instance != null)
            {
                DoorManager.Instance.UnregisterDoor(this);
            }
        }

        public void SetOpen(bool state)
        {
            if (_isOpen == state) return;
            _isOpen = state;
            UpdateVisualState();
            Debug.Log($"[Door] at {Position} state changed to IsOpen: {IsOpen}");
        }

        private void UpdateVisualState()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _isOpen ? _openColor : _closedColor;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_grid == null)
            {
                Debug.LogError("[Door] Grid is not assigned in the Inspector. Please assign it.");
                return;
            }
            Grid grid = _grid;
            InputManager inputManager = InputManager.Instance;

            if (grid != null && inputManager != null)
            {
                Vector2Int absolutePos = _position + inputManager.mapOrigin;
                Vector3 worldPos = grid.GetCellCenterWorld((Vector3Int)absolutePos);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(worldPos, grid.cellSize);
            }
        }
#endif
    }
}
