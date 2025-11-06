using UnityEngine;

namespace ThiefSimulator.Player
{
    /// <summary>
    /// Stores and manages the player's core data, such as position.
    /// This is a pure data container. Its data is set by other systems.
    /// </summary>
    public class PlayerData : MonoBehaviour
    {
        /// <summary>
        /// The player's current position on the tile grid, relative to the map origin.
        /// </summary>
        public Vector2Int CurrentTilePosition { get; private set; }

        /// <summary>
        /// Updates the player's tile position.
        /// This should only be called by trusted systems like a LevelManager or MovementSystem.
        /// </summary>
        /// <param name="newPosition">The new relative grid position.</param>
        public void SetTilePosition(Vector2Int newPosition)
        {
            if (CurrentTilePosition == newPosition) return;

            var oldPosition = CurrentTilePosition;
            CurrentTilePosition = newPosition;
            Debug.Log($"[PlayerData] Position updated from {oldPosition} to: {CurrentTilePosition}");
        }
    }
}
