using UnityEngine;

namespace ThiefSimulator.Police
{
    /// <summary>
    /// Data container for police officers.
    /// </summary>
    public class PoliceOfficerData : MonoBehaviour
    {
        public Vector2Int CurrentTilePosition { get; private set; }

        public void SetTilePosition(Vector2Int newPosition)
        {
            if (CurrentTilePosition == newPosition) { return; }
            Vector2Int previous = CurrentTilePosition;
            CurrentTilePosition = newPosition;
            Debug.Log($"[PoliceOfficerData] {name} moved from {previous} to {CurrentTilePosition}.");
        }
    }
}
