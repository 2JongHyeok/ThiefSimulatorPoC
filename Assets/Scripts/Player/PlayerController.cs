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
            _playerMovement.OnMovementFinished += OnActionFinished;
        }

        private void OnDisable() 
        {
            InputManager.OnTileClicked -= HandleTileClick; 
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
                        TimeManager.Instance.AdvanceTimeGradually(_doorOpenCost, _doorOpenInterval, OnActionFinished);
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

            bool isValidTarget = _floorTilemap.HasTile((Vector3Int)absolutePos) ||
                                 (_furnitureTilemap != null && _furnitureTilemap.HasTile((Vector3Int)absolutePos)) ||
                                 (_doorTilemap != null && _doorTilemap.HasTile((Vector3Int)absolutePos));

            if (!isValidTarget) { return; }

            // Get dynamic obstacles (other NPCs) from NPCManager
            HashSet<Vector2Int> dynamicObstacles = NPCManager.Instance != null ? NPCManager.Instance.GetAllNPCPositions() : new HashSet<Vector2Int>();

            Vector2Int finalTargetPos = FindValidTargetPosition(receivedRelativePos, mapOrigin, dynamicObstacles);
            var path = Pathfinder.FindPath(_playerData.CurrentTilePosition, finalTargetPos, _obstacleTilemap, mapOrigin, dynamicObstacles);

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
    }
}

