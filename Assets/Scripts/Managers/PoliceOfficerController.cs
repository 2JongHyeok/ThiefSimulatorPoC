using System.Collections.Generic;
using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using ThiefSimulator.Pathfinding;
using ThiefSimulator.Player;
using ThiefSimulator.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ThiefSimulator.Police
{
    [RequireComponent(typeof(PoliceOfficerData), typeof(PoliceOfficerMovement))]
    public class PoliceOfficerController : MonoBehaviour
    {
        private enum PoliceState
        {
            Hidden,
            Deploying,
            Patrolling,
            Returning,
            Cooldown
        }

        [Header("Setup")]
        [SerializeField] private Vector2Int _baseRelativePosition;
        [SerializeField] private Tilemap _obstacleTilemap;
        [SerializeField] private Grid _grid;
        [SerializeField] private GameObject _visualRoot;

        [Header("Behavior")]
        [SerializeField] private int _tilesPerMinute = 2;
        [SerializeField] private int _patrolRadius = 10;
        [SerializeField] private int _patrolDurationMinutes = 30;
        [SerializeField] private int _hideDelayMinutes = 10;
        [SerializeField] private int _detectionRangeInTiles = 5; // 10x10 area
        [SerializeField] private int _captureRadius = 1;
        [SerializeField] private Tilemap _hideSpotTilemap;

        [Header("Detection Overlay")]
        [SerializeField] private Color _overlayColor = new Color(1f, 0f, 0f, 0.15f);
        [SerializeField] private int _overlaySortingOrder = 3;

        private PoliceOfficerData _data;
        private PoliceOfficerMovement _movement;
        private PoliceState _currentState = PoliceState.Hidden;
        private Queue<Vector2Int> _currentPath = new Queue<Vector2Int>();
        private Vector2Int _lastDetectionTile;
        private int _activeUntilMinute = -1;
        private int _cooldownEndMinute = -1;
        private int _pendingStepBudget;
        private int _lastProcessedMinute = -1;
        private PlayerData _playerData;
        private SpriteRenderer _detectionOverlayRenderer;
        private static Sprite _sharedOverlaySprite;
        [Header("Editor Gizmo")]
        [SerializeField] private bool _drawPlacementGizmo = true;
        [SerializeField] private Color _gizmoColor = new Color(1f, 0f, 0f, 0.3f);
        private bool _playerSpotted;

        private void Awake()
        {
            _data = GetComponent<PoliceOfficerData>();
            _movement = GetComponent<PoliceOfficerMovement>();
            _movement.OnMovementFinished += HandleMovementFinished;
            if (_grid == null)
            {
                Debug.LogError("[PoliceOfficerController] Grid is not assigned in the Inspector. Please assign it.");
            }
            if (_grid == null)
            {
                Debug.LogError("[PoliceOfficerController] Grid reference missing.");
            }

            if (_visualRoot == null)
            {
                _visualRoot = transform.GetChild(0)?.gameObject;
            }

            InitializeBasePosition();
            SetVisibility(false);
            CreateDetectionOverlay();
            TryRegisterWithManager();
            if (_playerData == null)
            {
                Debug.LogError("[PoliceOfficerController] PlayerData is not assigned in the Inspector. Please assign it.");
            }
        }

        private void OnEnable()
        {
            TryRegisterWithManager();
        }

        private void OnDestroy()
        {
            if (_movement != null)
            {
                _movement.OnMovementFinished -= HandleMovementFinished;
            }
            if (PoliceManager.Instance != null)
            {
                PoliceManager.Instance.UnregisterOfficer(this);
            }

            if (_detectionOverlayRenderer != null && Application.isPlaying)
            {
                Destroy(_detectionOverlayRenderer.gameObject);
            }
        }

        public void TickMinute(int absoluteMinute)
        {
            _lastProcessedMinute = absoluteMinute;

            if (_currentState == PoliceState.Hidden) { return; }

            if (_currentState == PoliceState.Cooldown)
            {
                if (absoluteMinute >= _cooldownEndMinute)
                {
                    HideOfficer();
                }
                return;
            }

            if (_currentState == PoliceState.Patrolling && absoluteMinute >= _activeUntilMinute)
            {
                BeginReturnToBase();
            }

            _pendingStepBudget += _tilesPerMinute;
            TryIssueNextStep();
            CheckForPlayerInteraction();
        }

        public void OnPlayerDetected(Vector2Int detectionTile, int absoluteMinute)
        {
            _lastDetectionTile = detectionTile;
            _activeUntilMinute = absoluteMinute + _patrolDurationMinutes;
            _playerSpotted = true;

            if (_currentState == PoliceState.Hidden || _currentState == PoliceState.Cooldown)
            {
                DeployFromBase();
            }

            if (_data.CurrentTilePosition == detectionTile)
            {
                EnterPatrolState();
                QueueRandomPatrolDestination();
                return;
            }

            BuildPathTo(detectionTile);
            _currentState = PoliceState.Deploying;
            _pendingStepBudget = _tilesPerMinute;
            TryIssueNextStep();
        }

        private void InitializeBasePosition()
        {
            if (_grid == null) { return; }
            Vector2Int mapOrigin = ResolveMapOrigin();
            Vector3 worldPos = _grid.GetCellCenterWorld((Vector3Int)(_baseRelativePosition + mapOrigin));
            transform.position = worldPos;
            _data.SetTilePosition(_baseRelativePosition);
            _lastDetectionTile = _baseRelativePosition;
        }

        private void DeployFromBase()
        {
            SetVisibility(true);
            _data.SetTilePosition(_baseRelativePosition);
            _currentState = PoliceState.Deploying;
        }

        private void BeginReturnToBase()
        {
            BuildPathTo(_baseRelativePosition);
            _currentState = PoliceState.Returning;
            _playerSpotted = false;
        }

        private void EnterPatrolState()
        {
            _currentState = PoliceState.Patrolling;
            _pendingStepBudget = 0;
        }

        private void EnterCooldown()
        {
            _currentState = PoliceState.Cooldown;
            _cooldownEndMinute = _lastProcessedMinute + _hideDelayMinutes;
            _pendingStepBudget = 0;
            _playerSpotted = false;
        }

        private void HideOfficer()
        {
            _currentState = PoliceState.Hidden;
            _currentPath.Clear();
            _pendingStepBudget = 0;
            SetVisibility(false);
            InitializeBasePosition();
            _playerSpotted = false;
        }

        private int GetDetectionWidthInTiles()
        {
            return Mathf.Max(1, (_detectionRangeInTiles * 2) + 1);
        }

        private Vector2Int ResolveMapOrigin()
        {
            InputManager input = InputManager.Instance;
#if UNITY_EDITOR
            if (input == null && !Application.isPlaying)
            {
                input = InputManager.Instance;
            }
#endif
            return input != null ? input.mapOrigin : Vector2Int.zero;
        }

        private void CreateDetectionOverlay()
        {
            if (_grid == null) { return; }

            if (_detectionOverlayRenderer == null)
            {
                Transform existing = transform.Find("DetectionOverlay");
                if (existing != null)
                {
                    _detectionOverlayRenderer = existing.GetComponent<SpriteRenderer>();
                }
            }

            if (_sharedOverlaySprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _sharedOverlaySprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f));
                _sharedOverlaySprite.hideFlags = HideFlags.HideAndDontSave;
                _sharedOverlaySprite.name = "PoliceDetectionOverlay";
            }

            if (_detectionOverlayRenderer == null)
            {
                GameObject overlay = new GameObject("DetectionOverlay");
                overlay.transform.SetParent(transform, false);
                _detectionOverlayRenderer = overlay.AddComponent<SpriteRenderer>();
            }

            _detectionOverlayRenderer.sprite = _sharedOverlaySprite;
            _detectionOverlayRenderer.sortingOrder = _overlaySortingOrder;

            RefreshDetectionOverlay();
        }

        private void RefreshDetectionOverlay()
        {
            if (_detectionOverlayRenderer == null || _grid == null) { return; }
            _detectionOverlayRenderer.color = _overlayColor;

            Vector3 cellSize = _grid.cellSize;
            Vector3 worldScale = new Vector3(
                cellSize.x * GetDetectionWidthInTiles() * 200,
                cellSize.y * GetDetectionWidthInTiles() * 200,
                1f);

            _detectionOverlayRenderer.transform.localScale = worldScale;
            _detectionOverlayRenderer.transform.localPosition = Vector3.back * 0.01f;
        }

        private void BuildPathTo(Vector2Int target)
        {
            if (_obstacleTilemap == null || InputManager.Instance == null)
            {
                Debug.LogWarning("[PoliceOfficerController] Cannot build path. Missing tilemap or InputManager.");
                return;
            }

            List<Vector2Int> path = Pathfinder.FindPath(
                _data.CurrentTilePosition,
                target,
                _obstacleTilemap,
                InputManager.Instance.mapOrigin,
                null,
                null,
                true);

            if (path == null)
            {
                Debug.LogWarning($"[PoliceOfficerController] {name} could not find path to {target}.");
                return;
            }

            _currentPath = new Queue<Vector2Int>(path);
        }

        private void TryIssueNextStep()
        {
            if (_pendingStepBudget <= 0) { return; }
            if (_movement.IsMoving) { return; }

            if (_currentPath.Count == 0)
            {
                if (_currentState == PoliceState.Patrolling)
                {
                    QueueRandomPatrolDestination();
                    if (_currentPath.Count == 0) { return; }
                }
                else if (_currentState == PoliceState.Returning)
                {
                    if (_data.CurrentTilePosition == _baseRelativePosition)
                    {
                        EnterCooldown();
                    }
                    return;
                }
                else
                {
                    return;
                }
            }

            Vector2Int next = _currentPath.Dequeue();
            _pendingStepBudget--;
            _movement.MoveOneStep(next);

            if (_currentPath.Count == 0)
            {
                if (_currentState == PoliceState.Deploying)
                {
                    EnterPatrolState();
                }
                else if (_currentState == PoliceState.Returning && _data.CurrentTilePosition == _baseRelativePosition)
                {
                    EnterCooldown();
                }
            }
        }

        private void QueueRandomPatrolDestination()
        {
            if (InputManager.Instance == null) { return; }
            const int maxAttempts = 30;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2Int candidate = _lastDetectionTile + new Vector2Int(
                    Random.Range(-_patrolRadius, _patrolRadius + 1),
                    Random.Range(-_patrolRadius, _patrolRadius + 1));

                if (!IsWithinDetectionRange(_lastDetectionTile, candidate, _patrolRadius)) { continue; }

                if (Pathfinder.IsWalkable(candidate, _obstacleTilemap, InputManager.Instance.mapOrigin, null, null, true))
                {
                    BuildPathTo(candidate);
                    return;
                }
            }
        }

        private void HandleMovementFinished()
        {
            TryIssueNextStep();
            CheckForPlayerInteraction();
        }

        private void SetVisibility(bool isVisible)
        {
            if (_visualRoot != null)
            {
                _visualRoot.SetActive(isVisible);
            }
        }

        private void CheckForPlayerInteraction()
        {
            if (PoliceManager.Instance == null) { return; }
            PlayerData player = _playerData ?? PoliceManager.Instance.PlayerData;
            if (player == null) { return; }

            Vector2Int playerTile = player.CurrentTilePosition;
            Vector2Int officerTile = _data.CurrentTilePosition;
            bool playerHidden = HideSpotUtility.IsPositionHidden(playerTile, _hideSpotTilemap);
            bool hasLOS = HasLineOfSight(officerTile, playerTile);
            bool playerInDetectionRange = IsWithinDetectionRange(officerTile, playerTile, _detectionRangeInTiles);

            if (!playerHidden && hasLOS && playerInDetectionRange)
            {
                PoliceManager.Instance.ReportDetection(playerTile);
            }

            if (!playerHidden && IsWithinDetectionRange(officerTile, playerTile, _captureRadius))
            {
                PoliceManager.Instance.NotifyPlayerCaught(this);
            }

            if (playerHidden && hasLOS && playerInDetectionRange && _playerSpotted)
            {
                PoliceManager.Instance.NotifyPlayerCaught(this);
            }
        }

        private bool IsWithinDetectionRange(Vector2Int origin, Vector2Int target, int range)
        {
            int dx = Mathf.Abs(origin.x - target.x);
            int dy = Mathf.Abs(origin.y - target.y);
            return dx <= range && dy <= range;
        }

        private bool HasLineOfSight(Vector2Int origin, Vector2Int target)
        {
            if (_obstacleTilemap == null || InputManager.Instance == null) { return true; }
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;
            bool first = true;
            foreach (Vector2Int tile in EnumerateLine(origin, target))
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                Vector2Int absolute = tile + mapOrigin;
                if (_obstacleTilemap.HasTile((Vector3Int)absolute))
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerable<Vector2Int> EnumerateLine(Vector2Int start, Vector2Int end)
        {
            int x0 = start.x;
            int y0 = start.y;
            int x1 = end.x;
            int y1 = end.y;
            int dx = Mathf.Abs(x1 - x0);
            int sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0);
            int sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;

            while (true)
            {
                yield return new Vector2Int(x0, y0);
                if (x0 == x1 && y0 == y1) { break; }
                int e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private void TryRegisterWithManager()
        {
            if (PoliceManager.Instance == null) { return; }
            PoliceManager.Instance.RegisterOfficer(this);
            if (_playerData == null)
            {
                _playerData = PoliceManager.Instance.PlayerData;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // _grid should be assigned in the Inspector.

            if (!Application.isPlaying)
            {
                CreateDetectionOverlay();
            }
            else if (_detectionOverlayRenderer != null)
            {
                RefreshDetectionOverlay();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_drawPlacementGizmo) { return; }
            if (_grid == null) { Debug.LogError("[PoliceOfficerController] Grid is not assigned. Cannot get path."); return; }
            if (_grid == null) { return; }

            Vector3 cellSize = _grid.cellSize;
            Vector3 areaSize = new Vector3(cellSize.x * GetDetectionWidthInTiles(), cellSize.y * GetDetectionWidthInTiles(), 0.05f);
            Vector3 center = _grid.GetCellCenterWorld((Vector3Int)(_baseRelativePosition + ResolveMapOrigin()));

            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, _gizmoColor.a * 0.5f);
            Gizmos.DrawCube(center, areaSize);
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireCube(center, areaSize);
        }
#endif
    }
}
