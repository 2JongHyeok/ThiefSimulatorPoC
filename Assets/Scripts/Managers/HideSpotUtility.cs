using ThiefSimulator.Input;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ThiefSimulator.Utilities
{
    public static class HideSpotUtility
    {
        public static bool IsPositionHidden(Vector2Int relativePosition, Tilemap hideSpotTilemap, Vector2Int mapOrigin)
        {
            if (hideSpotTilemap == null) { return false; }
            Vector3Int absolute = (Vector3Int)(relativePosition + mapOrigin);
            return hideSpotTilemap.HasTile(absolute);
        }

        public static bool IsPositionHidden(Vector2Int relativePosition, Tilemap hideSpotTilemap)
        {
            if (InputManager.Instance == null) { return false; }
            return IsPositionHidden(relativePosition, hideSpotTilemap, InputManager.Instance.mapOrigin);
        }
    }
}
