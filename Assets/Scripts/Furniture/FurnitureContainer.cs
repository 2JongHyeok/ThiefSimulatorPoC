using System.Collections.Generic;
using ThiefSimulator.Items;
using ThiefSimulator.Player;
using ThiefSimulator.Input;
using UnityEngine;

namespace ThiefSimulator.Furniture
{
    [RequireComponent(typeof(Collider2D))]
    public class FurnitureContainer : MonoBehaviour
    {
        [SerializeField] private string _containerName = "Furniture";
        [SerializeField] private List<ItemData> _initialItems = new List<ItemData>();
        [SerializeField, Tooltip("Whether the player must be adjacent to interact.")]
        private bool _requiresAdjacency = true;
        [SerializeField, Tooltip("Grid position relative to the map origin. Used for interaction checks.")]
        private Vector2Int _relativeGridPosition;
        [SerializeField] private Grid _grid;
        [SerializeField, Tooltip("Used in the editor when InputManager is unavailable. Matches InputManager.mapOrigin.")]
        private Vector2Int _editorMapOrigin = Vector2Int.zero;
        [SerializeField] private GameObject _highlightObject;
        [SerializeField] private Color _highlightColor = Color.yellow;
        [SerializeField] private SpriteRenderer _highlightRenderer;

        private readonly List<ItemData> _items = new List<ItemData>();
        private Collider2D _trigger;

        public IReadOnlyList<ItemData> Items => _items;
        public string ContainerName => _containerName;
        public bool RequiresAdjacency => _requiresAdjacency;
        public Vector2Int RelativeGridPosition => _relativeGridPosition;

        private void Awake()
        {
            _items.Clear();
            if (_initialItems != null)
            {
                foreach (ItemData item in _initialItems)
                {
                    if (item != null)
                    {
                        _items.Add(item);
                    }
                }
            }

            if (FurnitureManager.Instance != null)
            {
                FurnitureManager.Instance.RegisterFurniture(this, _relativeGridPosition);
            }

            _trigger = GetComponent<Collider2D>();
            if (_trigger != null)
            {
                _trigger.isTrigger = true;
            }

            SetHighlight(false);
        }

        private void Start()
        {
            if (_grid != null && InputManager.Instance != null)
            {
                Vector2Int absolute = _relativeGridPosition + InputManager.Instance.mapOrigin;
                transform.position = _grid.GetCellCenterWorld((Vector3Int)absolute);
                _editorMapOrigin = InputManager.Instance.mapOrigin;
            }
            else
            {
                ApplyRelativePosition();
            }
        }

        private void OnDestroy()
        {
            if (FurnitureManager.Instance != null)
            {
                FurnitureManager.Instance.UnregisterFurniture(_relativeGridPosition);
            }
        }

        public bool TryRemoveItem(ItemData item)
        {
            return item != null && _items.Remove(item);
        }

        public void AddItem(ItemData item)
        {
            if (item == null) { return; }
            _items.Add(item);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<PlayerController>(out var player)) { return; }
            player.NotifyFurnitureEntered(this);
            SetHighlight(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<PlayerController>(out var player)) { return; }
            player.NotifyFurnitureExited(this);
            SetHighlight(false);
        }

        private void SetHighlight(bool state)
        {
            if (_highlightObject != null)
            {
                _highlightObject.SetActive(state);
            }
            else if (_highlightRenderer != null)
            {
                _highlightRenderer.enabled = state;
                _highlightRenderer.color = _highlightColor;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_grid == null)
            {
                _grid = GetComponentInParent<Grid>();
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
            _relativeGridPosition = absolute - mapOrigin;
        }

        private void ApplyRelativePosition()
        {
            if (_grid == null) { return; }
            Vector2Int mapOrigin = InputManager.Instance != null ? InputManager.Instance.mapOrigin : _editorMapOrigin;
            Vector2Int absolute = _relativeGridPosition + mapOrigin;
            transform.position = _grid.GetCellCenterWorld((Vector3Int)absolute);
        }

        private void OnDrawGizmosSelected()
        {
            Grid gridRef = _grid != null ? _grid : GetComponentInParent<Grid>();
            if (gridRef == null) { return; }

            Vector2Int mapOrigin = InputManager.Instance != null ? InputManager.Instance.mapOrigin : _editorMapOrigin;
            Vector2Int absolute = _relativeGridPosition + mapOrigin;
            Vector3 worldCenter = gridRef.GetCellCenterWorld((Vector3Int)absolute);

            Vector3 size = gridRef.cellSize;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(worldCenter, size);

            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawCube(worldCenter, size * 0.95f);
        }
#endif

        // Usage in Unity:
        // 1. Attach to any furniture GameObject, assign initial items in the Inspector.
        // 2. Ensure the object also registers with FurnitureManager for lookup.
    }
}
