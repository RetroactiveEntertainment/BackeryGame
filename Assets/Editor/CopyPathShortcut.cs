using UnityEngine;
using UnityEditor;

public class CopyPathShortcut : Editor
{
    // Adds the option to the right-click menu in the Project window
    [MenuItem("Assets/Copy System Path", false, 20)]
    private static void CopySystemPath()
    {
        // Get the path of the selected object relative to the Project folder
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        
        if (string.IsNullOrEmpty(assetPath))
        {
            Debug.LogWarning("No asset selected to copy path.");
            return;
        }

        // Convert the project-relative path to a full Windows/Mac system path
        string fullPath = System.IO.Path.GetFullPath(assetPath);
        
        // Copy to clipboard
        GUIUtility.systemCopyBuffer = fullPath;

        Debug.Log($"<color=green>Path copied to clipboard:</color> {fullPath}");
    }
}