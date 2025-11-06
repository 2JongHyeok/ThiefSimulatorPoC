using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using ThiefSimulator.Input;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        // Get the target InputManager instance
        InputManager inputManager = (InputManager)target;

        // Add a button to the inspector
        if (GUILayout.Button("Auto-Detect Map Origin"))
        {
            DetectAndSetOrigin(inputManager);
        }
    }

    private void DetectAndSetOrigin(InputManager inputManager)
    {
        // Find the first active Tilemap in the scene.
        Tilemap tilemap = FindObjectOfType<Tilemap>();

        if (tilemap == null)
        {
            Debug.LogError("Auto-Detect Failed: No Tilemap found in the scene.");
            return;
        }

        // This ensures the bounds are up-to-date, including only cells with tiles.
        tilemap.CompressBounds();
        
        BoundsInt bounds = tilemap.cellBounds;
        Vector2Int origin = (Vector2Int)bounds.min;

        // Set the value on the target script
        inputManager.mapOrigin = origin;

        // Mark the object as "dirty" to ensure the change is saved
        EditorUtility.SetDirty(inputManager);

        Debug.Log($"Map Origin automatically set to: {origin}");
    }
}
