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
        [SerializeField, Tooltip("Used when InputManager is not available. Matches InputManager.mapOrigin.")]
        private Vector2Int _editorMapOrigin;

        [Header("Visuals")]
        [SerializeField] private GameObject _openHighlight;
        [SerializeField] private GameObject _closedHighlight;
        [SerializeField] private Color _originalColor = Color.white;

        private SpriteRenderer _spriteRenderer;
        private int _lastToggleMinute = -1;
        [SerializeField, Tooltip("Minimum in-game minutes between toggles.")] private int _toggleCooldownMinutes = 1;

        public Vector2Int Position => _position;
        public bool IsOpen => _isOpen;
        public bool CanToggle()
        {
            if (TimeManager.Instance == null) { return true; }
            if (_lastToggleMinute < 0) { return true; }
            return TimeManager.Instance.TotalMinutes - _lastToggleMinute >= _toggleCooldownMinutes;
        }

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_grid == null) { Debug.LogError("[Door] Grid is not assigned in the inspector!"); }

            UpdateVisualState();
        }

        private void Start()
        {
            // Position the object at its logical position for consistency.
            if (_grid != null)
            {
                Vector2Int mapOrigin = InputManager.Instance != null ? InputManager.Instance.mapOrigin : _editorMapOrigin;
                Vector3Int currentCell = _grid.WorldToCell(transform.position);
                Vector2Int absolute = (Vector2Int)currentCell;
                if (InputManager.Instance != null)
                {
                    _position = absolute - mapOrigin;
                    _editorMapOrigin = mapOrigin;
                }
                Vector2Int targetAbsolute = _position + mapOrigin;
                transform.position = _grid.GetCellCenterWorld((Vector3Int)targetAbsolute);
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
            if (TimeManager.Instance != null)
            {
                _lastToggleMinute = TimeManager.Instance.TotalMinutes;
            }
            UpdateVisualState();
            Debug.Log($"[Door] at {Position} state changed to IsOpen: {IsOpen}");
        }

        private void UpdateVisualState()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originalColor;
            }

            ApplyHighlightState();
        }

        private void SetHighlights(bool openActive, bool closedActive)
        {
            if (_openHighlight != null)
            {
                _openHighlight.SetActive(openActive);
            }
            if (_closedHighlight != null)
            {
                _closedHighlight.SetActive(closedActive);
            }
        }

        private void ApplyHighlightState()
        {
            SetHighlights(_isOpen, !_isOpen);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_grid == null)
            {
                _grid = GetComponentInParent<Grid>();
            }
            if (!Application.isPlaying)
            {
                if (_grid == null) { return; }
                if (InputManager.Instance == null && _editorMapOrigin == Vector2Int.zero)
                {
                    return;
                }
            }
            ApplyRelativePosition();
        }

        [ContextMenu("Capture Relative Position")]
        private void CaptureRelativePosition()
        {
            if (_grid == null) { return; }
            Vector3Int cell = _grid.WorldToCell(transform.position);
            Vector2Int absolute = (Vector2Int)cell;
            Vector2Int mapOrigin = InputManager.Instance != null ? InputManager.Instance.mapOrigin : _editorMapOrigin;
            _position = absolute - mapOrigin;
        }

        private void ApplyRelativePosition()
        {
            if (_grid == null) { return; }
            Vector2Int mapOrigin = InputManager.Instance != null ? InputManager.Instance.mapOrigin : _editorMapOrigin;
            Vector2Int absolute = _position + mapOrigin;
            transform.position = _grid.GetCellCenterWorld((Vector3Int)absolute);
        }

        private void OnDrawGizmosSelected()
        {
            Grid grid = _grid != null ? _grid : GetComponentInParent<Grid>();
            if (grid == null) { return; }

            Vector2Int mapOrigin = InputManager.Instance != null ? InputManager.Instance.mapOrigin : _editorMapOrigin;
            Vector2Int absolutePos = _position + mapOrigin;
            Vector3 worldPos = grid.GetCellCenterWorld((Vector3Int)absolutePos);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(worldPos, grid.cellSize);
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawCube(worldPos, grid.cellSize * 0.9f);
        }
#endif
    }
}
