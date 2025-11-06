using UnityEngine;

namespace ThiefSimulator.NPC
{
    /// <summary>
    /// Stores and manages the NPC's core data, such as position.
    /// This is a pure data container. Its data is set by other systems.
    /// </summary>
    public class NPCData : MonoBehaviour
    {
        /// <summary>
        /// The NPC's current position on the tile grid, relative to the map origin.
        /// </summary>
        public Vector2Int CurrentTilePosition { get; private set; }

        /// <summary>
        /// Updates the NPC's tile position.
        /// This should only be called by trusted systems like an NPCManager or NPCMovement.
        /// </summary>
        /// <param name="newPosition">The new relative grid position.</param>
        public void SetTilePosition(Vector2Int newPosition)
        {
            if (CurrentTilePosition == newPosition) return;

            var oldPosition = CurrentTilePosition;
            CurrentTilePosition = newPosition;
            Debug.Log($"[NPCData] Position updated from {oldPosition} to: {CurrentTilePosition}");
        }
    }
}
