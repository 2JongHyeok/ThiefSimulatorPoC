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
        [SerializeField] private PlayerInventory _playerInventory;

        private PlayerData _playerData;
        private PlayerMovement _playerMovement;
        private PlayerState _currentState = PlayerState.Idle;
        private readonly HashSet<FurnitureContainer> _furnitureInRange = new HashSet<FurnitureContainer>();
        private FurnitureContainer _currentNearbyFurniture;
        private FurnitureContainer _pendingFurnitureInteraction;
        private FurnitureContainer _activeFurniture;

        private void Awake()
        {
            _playerData = GetComponent<PlayerData>();
            _playerMovement = GetComponent<PlayerMovement>();
            if (_playerInventory == null)
            {
                _playerInventory = GetComponent<PlayerInventory>();
            }
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
            if (_inventoryUI != null)
            {
                _inventoryUI.OnItemClicked += HandleInventoryItemClicked;
            }
        }

        private void OnDisable()
        {
            InputManager.OnDirectionInput -= HandleDirectionInput;
            InputManager.OnInteractInput -= HandleInteractInput;
            InputManager.OnInventoryToggle -= HandleInventoryToggle;
            InputManager.OnCancelInput -= HandleCancelInput;
            _playerMovement.OnMovementFinished -= OnActionFinished;
            if (_inventoryUI != null)
            {
                _inventoryUI.OnItemClicked -= HandleInventoryItemClicked;
            }
        }

        public void Interact()
        {
            if (_currentState == PlayerState.Busy) { return; }
            if (TryToggleNearbyDoor()) { return; }
            if (TryOpenNearbyFurniture()) { return; }

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = _playerData.CurrentTilePosition + dir;

                if (DoorManager.Instance.IsDoorAt(neighborPos, out Door door))
                {
                    if (!door.IsOpen && door.CanToggle())
                    {
                        _currentState = PlayerState.Busy;
                        Debug.Log($"[PlayerController] Opening door at {neighborPos}. Player is now busy.");
                        TimeManager.Instance.ResetIdleTimer();
                        TimeManager.Instance.AdvanceTimeGradually(_doorOpenCost, _doorOpenInterval, () =>
                        {
                            door.SetOpen(true);
                            OnActionFinished();
                        }, true);
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

            if (!IsTargetTileWalkable(targetTile, mapOrigin, dynamicObstacles))
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

        private bool IsTargetTileWalkable(Vector2Int targetTile, Vector2Int mapOrigin, HashSet<Vector2Int> dynamicObstacles)
        {
            Vector3Int absolute = (Vector3Int)(targetTile + mapOrigin);
            bool isDoorTile = _doorTilemap != null && _doorTilemap.HasTile(absolute);
            if (isDoorTile)
            {
                if (!DoorManager.Instance.IsDoorAt(targetTile, out Door door) || !door.IsOpen)
                {
                    return false;
                }
            }
            else
            {
                bool isFloor = _floorTilemap != null && _floorTilemap.HasTile(absolute);
                bool isHideSpot = _hideSpotTilemap != null && _hideSpotTilemap.HasTile(absolute);
                if (!isFloor && !isHideSpot)
                {
                    return false;
                }
            }

            return Pathfinder.IsWalkable(targetTile, _obstacleTilemap, mapOrigin, dynamicObstacles);
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

        private void HandleInventoryItemClicked(ItemData item)
        {
            if (item == null || _playerInventory == null) { return; }

            if (_furnitureUI != null && _furnitureUI.IsVisible)
            {
                if (_furnitureUI.TryPlaceItemFromInventory(item))
                {
                    return;
                }
            }

            // Ignore clicks when no furniture UI is open.
        }

        private bool TryToggleNearbyDoor()
        {
            if (IsPlayerStandingOnDoorTile())
            {
                return false;
            }

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = _playerData.CurrentTilePosition + dir;
                if (DoorManager.Instance.IsDoorAt(neighborPos, out Door door))
                {
                    if (door.IsOpen)
                    {
                        if (!door.CanToggle()) { continue; }
                        _currentState = PlayerState.Busy;
                        Debug.Log($"[PlayerController] Closing door at {neighborPos}.");
                        TimeManager.Instance.ResetIdleTimer();
                        TimeManager.Instance.AdvanceTimeGradually(_doorOpenCost, _doorOpenInterval, () =>
                        {
                            door.SetOpen(false);
                            OnActionFinished();
                        }, true);
                        return true;
                    }
                    else
                    {
                        if (!door.CanToggle()) { continue; }
                        _currentState = PlayerState.Busy;
                        Debug.Log($"[PlayerController] Opening door at {neighborPos}. Player is now busy.");
                        TimeManager.Instance.ResetIdleTimer();
                        TimeManager.Instance.AdvanceTimeGradually(_doorOpenCost, _doorOpenInterval, () =>
                        {
                            door.SetOpen(true);
                            OnActionFinished();
                        }, true);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsPlayerStandingOnDoorTile()
        {
            if (_doorTilemap == null || InputManager.Instance == null) { return false; }
            Vector2Int currentTile = _playerData.CurrentTilePosition;
            Vector3Int absolute = (Vector3Int)(currentTile + InputManager.Instance.mapOrigin);
            return _doorTilemap.HasTile(absolute);
        }
    }
}
