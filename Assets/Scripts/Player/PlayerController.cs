using System.Collections.Generic;
using ThiefSimulator.Furniture;
using ThiefSimulator.Input;
using ThiefSimulator.Items;
using ThiefSimulator.Managers;
using ThiefSimulator.Objects;
using ThiefSimulator.Pathfinding;
using ThiefSimulator.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ThiefSimulator.Player
{
    [RequireComponent(typeof(PlayerData), typeof(PlayerMovement))]
    public class PlayerController : MonoBehaviour
    {
        private enum PlayerState { Idle, Busy }

        [Header("Action Costs")]
        [SerializeField] private int _doorOpenCost = 6;
        [SerializeField] private float _doorOpenInterval = 0.5f;

        [Header("Tilemap Layers")]
        [SerializeField] private Tilemap _floorTilemap;
        [SerializeField] private Tilemap _furnitureTilemap;
        [SerializeField] private Tilemap _doorTilemap;
        [SerializeField] private Tilemap _hideSpotTilemap;
        [SerializeField] private Tilemap _obstacleTilemap;

        [Header("UI")]
        [SerializeField] private InventoryUI _inventoryUI;
        [SerializeField] private FurnitureUI _furnitureUI;

        private PlayerData _playerData;
        private PlayerMovement _playerMovement;
        private PlayerState _currentState = PlayerState.Idle;
        private readonly HashSet<FurnitureContainer> _furnitureInRange = new HashSet<FurnitureContainer>();
        private FurnitureContainer _currentNearbyFurniture;
        private FurnitureContainer _pendingFurnitureInteraction;
        private FurnitureContainer _activeFurniture;
        private bool _inventoryClickHooked;

        private void Awake()
        {
            _playerData = GetComponent<PlayerData>();
            _playerMovement = GetComponent<PlayerMovement>();
            if (_floorTilemap == null || _obstacleTilemap == null)
            {
                Debug.LogError("[PlayerController] Floor and Obstacle Tilemaps must be assigned!");
            }
        }

        private void OnEnable()
        {
            InputManager.OnDirectionInput += HandleDirectionInput;
            InputManager.OnInteractInput += HandleInteractInput;
            InputManager.OnInventoryToggle += HandleInventoryToggle;
            InputManager.OnCancelInput += HandleCancelInput;
            _playerMovement.OnMovementFinished += OnActionFinished;
        }

        private void OnDisable()
        {
            InputManager.OnDirectionInput -= HandleDirectionInput;
            InputManager.OnInteractInput -= HandleInteractInput;
            InputManager.OnInventoryToggle -= HandleInventoryToggle;
            InputManager.OnCancelInput -= HandleCancelInput;
            _playerMovement.OnMovementFinished -= OnActionFinished;
        }

        public void Interact()
        {
            if (_currentState == PlayerState.Busy) { return; }
            if (TryOpenNearbyFurniture()) { return; }

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = _playerData.CurrentTilePosition + dir;

                if (DoorManager.Instance.IsDoorAt(neighborPos, out Door door))
                {
                    if (!door.IsOpen)
                    {
                        _currentState = PlayerState.Busy;
                        Debug.Log($"[PlayerController] Opening door at {neighborPos}. Player is now busy.");
                        door.SetOpen(true);
                        TimeManager.Instance.ResetIdleTimer();
                        TimeManager.Instance.AdvanceTimeGradually(_doorOpenCost, _doorOpenInterval, OnActionFinished, true);
                        break;
                    }
                }
            }
        }

        private void OnActionFinished()
        {
            Debug.Log("[PlayerController] Action finished. Player is now idle.");
            _currentState = PlayerState.Idle;
            TryCompletePendingFurnitureInteraction();
        }

        private Vector2Int FindValidTargetPosition(Vector2Int relativeTargetPos, Vector2Int mapOrigin, HashSet<Vector2Int> dynamicObstacles)
        {
            if (Pathfinder.IsWalkable(relativeTargetPos, _obstacleTilemap, mapOrigin, dynamicObstacles))
            {
                return relativeTargetPos;
            }

            Vector2Int bestNeighbor = new Vector2Int(-1, -1);
            float bestDistance = float.MaxValue;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int relativeNeighborPos = relativeTargetPos + dir;

                if (Pathfinder.IsWalkable(relativeNeighborPos, _obstacleTilemap, mapOrigin, dynamicObstacles))
                {
                    float distanceToPlayer = Vector2Int.Distance(_playerData.CurrentTilePosition, relativeNeighborPos);
                    if (distanceToPlayer < bestDistance)
                    {
                        bestDistance = distanceToPlayer;
                        bestNeighbor = relativeNeighborPos;
                    }
                }
            }

            if (bestNeighbor != new Vector2Int(-1, -1))
            {
                return bestNeighbor;
            }

            return relativeTargetPos;
        }

        private void HandleDirectionInput(Vector2Int direction)
        {
            if (_currentState == PlayerState.Busy) { return; }
            if (direction == Vector2Int.zero) { return; }
            if (InputManager.Instance == null) { return; }

            Vector2Int targetTile = _playerData.CurrentTilePosition + direction;
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;
            HashSet<Vector2Int> dynamicObstacles = NPCManager.Instance != null
                ? NPCManager.Instance.GetAllNPCPositions()
                : null;

            if (!Pathfinder.IsWalkable(targetTile, _obstacleTilemap, mapOrigin, dynamicObstacles))
            {
                return;
            }

            List<Vector2Int> path = new List<Vector2Int> { targetTile };
            _currentState = PlayerState.Busy;
            _playerMovement.StartMove(path);
        }

        private void HandleInteractInput()
        {
            Interact();
        }

        private bool TryOpenNearbyFurniture()
        {
            if (_currentNearbyFurniture == null) { return false; }
            OpenFurnitureUI(_currentNearbyFurniture);
            return true;
        }

        private void OpenFurnitureUI(FurnitureContainer container)
        {
            if (container == null) { return; }

            _inventoryUI?.Show();
            SubscribeInventoryClicks();
            if (_furnitureUI != null)
            {
                _furnitureUI.Show(container);
                _activeFurniture = container;
            }
        }

        private void CloseFurnitureInteraction()
        {
            if (_furnitureUI != null && _furnitureUI.IsVisible)
            {
                _furnitureUI.Hide();
            }
            _activeFurniture = null;
            UnsubscribeInventoryClicks();
        }

        private void HandleInventoryToggle()
        {
            _inventoryUI?.Toggle();
        }

        private void HandleCancelInput()
        {
            CloseFurnitureInteraction();
        }

        public void NotifyFurnitureEntered(FurnitureContainer container)
        {
            if (container == null) { return; }
            _furnitureInRange.Add(container);
            _currentNearbyFurniture = container;

            if (_pendingFurnitureInteraction == container)
            {
                OpenFurnitureUI(container);
                _pendingFurnitureInteraction = null;
            }
        }

        public void NotifyFurnitureExited(FurnitureContainer container)
        {
            if (container == null) { return; }
            _furnitureInRange.Remove(container);

            if (_currentNearbyFurniture == container)
            {
                _currentNearbyFurniture = _furnitureInRange.Count > 0 ? GetAnyFurnitureInRange() : null;
            }

            if (_activeFurniture == container)
            {
                CloseFurnitureInteraction();
            }

            if (_pendingFurnitureInteraction == container)
            {
                _pendingFurnitureInteraction = null;
            }

            if (_furnitureInRange.Count == 0)
            {
                _inventoryUI?.Hide();
                UnsubscribeInventoryClicks();
            }
        }

        private FurnitureContainer GetAnyFurnitureInRange()
        {
            foreach (FurnitureContainer container in _furnitureInRange)
            {
                return container;
            }
            return null;
        }

        private void TryCompletePendingFurnitureInteraction()
        {
            if (_pendingFurnitureInteraction == null) { return; }
            if (_furnitureInRange.Contains(_pendingFurnitureInteraction))
            {
                OpenFurnitureUI(_pendingFurnitureInteraction);
                _pendingFurnitureInteraction = null;
            }
        }

        private bool TryFindAdjacentAccessTile(FurnitureContainer container, Vector2Int mapOrigin, HashSet<Vector2Int> dynamicObstacles, out Vector2Int accessTile)
        {
            accessTile = container.RelativeGridPosition;
            if (!container.RequiresAdjacency &&
                Pathfinder.IsWalkable(container.RelativeGridPosition, _obstacleTilemap, mapOrigin, dynamicObstacles))
            {
                return true;
            }

            Vector2Int best = container.RelativeGridPosition;
            float bestDistance = float.MaxValue;
            bool found = false;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int candidate = container.RelativeGridPosition + dir;
                if (!Pathfinder.IsWalkable(candidate, _obstacleTilemap, mapOrigin, dynamicObstacles))
                {
                    continue;
                }

                float distance = Vector2Int.Distance(_playerData.CurrentTilePosition, candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                    found = true;
                }
            }

            accessTile = best;
            return found;
        }

        private void SubscribeInventoryClicks()
        {
            if (_inventoryUI == null || _inventoryClickHooked) { return; }
            _inventoryUI.OnItemClicked += HandleInventoryItemClicked;
            _inventoryClickHooked = true;
        }

        private void UnsubscribeInventoryClicks()
        {
            if (_inventoryUI == null || !_inventoryClickHooked) { return; }
            _inventoryUI.OnItemClicked -= HandleInventoryItemClicked;
            _inventoryClickHooked = false;
        }

        private void HandleInventoryItemClicked(ItemData item)
        {
            if (_furnitureUI == null || !_furnitureUI.IsVisible) { return; }
            _furnitureUI.TryPlaceItemFromInventory(item);
        }
    }
}
