using System.Collections.Generic;
using ThiefSimulator.Input;
using ThiefSimulator.Managers;
using ThiefSimulator.Objects;
using ThiefSimulator.Pathfinding;
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
        
        private PlayerData _playerData;
        private PlayerMovement _playerMovement;
        private PlayerState _currentState = PlayerState.Idle;

        private void Awake()
        {
            _playerData = GetComponent<PlayerData>();
            _playerMovement = GetComponent<PlayerMovement>();
            if (_floorTilemap == null || _obstacleTilemap == null) { 
                Debug.LogError("[PlayerController] Floor and Obstacle Tilemaps must be assigned!"); 
            }
        }

        private void OnEnable() 
        {
            InputManager.OnTileClicked += HandleTileClick; 
            InputManager.OnDirectionInput += HandleDirectionInput;
            InputManager.OnInteractInput += HandleInteractInput;
            _playerMovement.OnMovementFinished += OnActionFinished;
        }

        private void OnDisable() 
        {
            InputManager.OnTileClicked -= HandleTileClick; 
            InputManager.OnDirectionInput -= HandleDirectionInput;
            InputManager.OnInteractInput -= HandleInteractInput;
            _playerMovement.OnMovementFinished -= OnActionFinished;
        }

        public void Interact()
        {
            if (_currentState == PlayerState.Busy) return;

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var dir in directions)
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

        private void HandleTileClick(Vector2Int receivedRelativePos)
        {
            if (_currentState == PlayerState.Busy) return;

            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;
            Vector2Int absolutePos = receivedRelativePos + mapOrigin;

            // Get dynamic obstacles (other NPCs) from NPCManager
            HashSet<Vector2Int> dynamicObstacles = NPCManager.Instance != null ? NPCManager.Instance.GetAllNPCPositions() : new HashSet<Vector2Int>();

            // Check if the clicked tile is a door
            Door clickedDoor = null;
            bool isDoor = DoorManager.Instance != null && DoorManager.Instance.IsDoorAt(receivedRelativePos, out clickedDoor);

            Vector2Int finalTargetPos = receivedRelativePos;
            HashSet<Vector2Int> pathfindingTemporarilyWalkableDoors = null;

            if (isDoor && clickedDoor != null && !clickedDoor.IsOpen)
            {
                // If it's a closed door, we want to pathfind to the tile *before* the door.
                // Temporarily treat the door as walkable to find a path *through* it.
                pathfindingTemporarilyWalkableDoors = new HashSet<Vector2Int> { clickedDoor.Position };

                // Find a path to the door's position, assuming it's temporarily walkable
                var pathThroughDoor = Pathfinder.FindPath(
                    _playerData.CurrentTilePosition,
                    clickedDoor.Position,
                    _obstacleTilemap,
                    mapOrigin,
                    dynamicObstacles,
                    pathfindingTemporarilyWalkableDoors
                );

                if (pathThroughDoor != null && pathThroughDoor.Count > 0)
                {
                    // The target is the tile *before* the door in the path.
                    // If the path is just the door itself, it means we are already adjacent or on the door.
                    if (pathThroughDoor.Count > 1)
                    {
                        finalTargetPos = pathThroughDoor[pathThroughDoor.Count - 2]; // Second to last tile
                    }
                    else // Player is already adjacent to the door, no further movement needed towards the door itself.
                    {
                        finalTargetPos = _playerData.CurrentTilePosition; // Stay at current position
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayerController] Cannot find path to closed door at {clickedDoor.Position} even when temporarily walkable.");
                    return; // Cannot reach the door even if it's temporarily walkable
                }
            }
            else if (!isDoor)
            {
                // If it's not a door, or it's an open door, check if the target is walkable.
                // If the target itself is not walkable, try to find a valid neighbor.
                finalTargetPos = FindValidTargetPosition(receivedRelativePos, mapOrigin, dynamicObstacles);
            }
            // If it's an open door, finalTargetPos remains receivedRelativePos, and pathfindingTemporarilyWalkableDoors is null.

            bool isValidTarget = _floorTilemap.HasTile((Vector3Int)absolutePos) ||
                                 (_furnitureTilemap != null && _furnitureTilemap.HasTile((Vector3Int)absolutePos)) ||
                                 (_doorTilemap != null && _doorTilemap.HasTile((Vector3Int)absolutePos)) ||
                                 (_hideSpotTilemap != null && _hideSpotTilemap.HasTile((Vector3Int)absolutePos));

            if (!isValidTarget && !isDoor) { return; } // Only return if not a door and not a valid tile type.

            var path = Pathfinder.FindPath(_playerData.CurrentTilePosition, finalTargetPos, _obstacleTilemap, mapOrigin, dynamicObstacles, pathfindingTemporarilyWalkableDoors);

            if (path != null && path.Count > 0)
            {
                _currentState = PlayerState.Busy;
                Debug.Log("[PlayerController] Path found. Player is now busy.");
                _playerMovement.StartMove(path);
            }
        }

        private void OnActionFinished()
        {
            Debug.Log("[PlayerController] Action finished. Player is now idle.");
            _currentState = PlayerState.Idle;
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

            foreach (var dir in directions)
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

            if (bestNeighbor != new Vector2Int(-1,-1))
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
            HashSet<Vector2Int> dynamicObstacles = NPCManager.Instance != null ? NPCManager.Instance.GetAllNPCPositions() : null;

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
    }
}
