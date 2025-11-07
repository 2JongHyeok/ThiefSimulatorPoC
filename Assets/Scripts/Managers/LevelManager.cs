using ThiefSimulator.Input;
using ThiefSimulator.Player;
using UnityEngine;

namespace ThiefSimulator.Managers
{
    /// <summary>
    /// Manages level setup, including player starting position using map-relative coordinates.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Player Setup")]
        [SerializeField] private PlayerData _playerData;
        [Tooltip("The player's starting position relative to the map origin.")]
        [SerializeField] private Vector2Int _playerRelativeStartPos = new Vector2Int(0, 0);

        [Header("Map Information")]
        [SerializeField] private Grid _grid;

        void Start()
        {
            if (_playerData == null)
            {
                Debug.LogError("[LevelManager] PlayerData is not assigned!");
                return;
            }
            if (_grid == null)
            {
                Debug.LogError("[LevelManager] Grid is not assigned in the Inspector. Please assign it.");
            }

            InitializePlayerPosition();
        }

        private void InitializePlayerPosition()
        {
            // 1. Get the map origin from the InputManager
            Vector2Int mapOrigin = InputManager.Instance.mapOrigin;

            // 2. Calculate the absolute start position
            Vector2Int absoluteStartPos = _playerRelativeStartPos + mapOrigin;

            // 3. Set the visual position of the player GameObject using the absolute position
            Vector3 worldPosition = _grid.GetCellCenterWorld((Vector3Int)absoluteStartPos);
            _playerData.transform.position = worldPosition;

            // 4. Initialize PlayerData with the already known relative position
            _playerData.SetTilePosition(_playerRelativeStartPos);

            Debug.Log($"[LevelManager] Player initialized. Relative Start: {_playerRelativeStartPos}, Absolute Start: {absoluteStartPos}, World Position: {worldPosition}");
        }
    }
}
