using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Diagnostics;
using System.Reflection;

public static class ProjectWindowExtensions
{
    // Tracks the toggle state. Defaults to false (Collapse) so the first click cleans up.
    private static bool _shouldExpand = false;

    // =========================================================
    // 1. OPEN FILE LOCATION (Priority 0)
    // =========================================================
    [MenuItem("Assets/Open File Location", false, 0)]
    public static void OpenLocation()
    {
        string path = GetCurrentPath();
        string fullPath = Path.GetFullPath(path);

        if (Directory.Exists(fullPath))
        {
            Process.Start(fullPath);
        }
        else
        {
            EditorUtility.RevealInFinder(path);
        }
    }

    // =========================================================
    // 2. COPY FULL PATH (Shortcut: Ctrl + Alt + C)
    // =========================================================
    [MenuItem("Assets/Copy Full Path %&c", false, 1)]
    public static void CopyFullPath()
    {
        string relativePath = GetCurrentPath();
        
        if (string.IsNullOrEmpty(relativePath)) return;

        string fullPath = Path.GetFullPath(relativePath);
        GUIUtility.systemCopyBuffer = fullPath;

        UnityEngine.Debug.Log($"<b>Copied path:</b> {fullPath}");
    }

    // =========================================================
    // 3. TOGGLE HIERARCHY (Shortcut: Shift + A)
    // =========================================================
    [MenuItem("Tools/Toggle Hierarchy #a")] 
    public static void ToggleHierarchy()
    {
        // Get the Hierarchy Window
        var hierarchyType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        var window = EditorWindow.GetWindow(hierarchyType);

        // Get the internal method
        var setExpandedRecursive = hierarchyType.GetMethod(
            "SetExpandedRecursive", 
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        // Get all root objects
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        // Run the toggle
        foreach (GameObject root in roots)
        {
            setExpandedRecursive.Invoke(window, new object[] { root.GetInstanceID(), _shouldExpand });
        }
        
        UnityEngine.Debug.Log(_shouldExpand ? "<b>Hierarchy Expanded.</b>" : "<b>Hierarchy Collapsed.</b>");

        // Flip the state for next time
        _shouldExpand = !_shouldExpand;
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static string GetCurrentPath()
    {
        if (Selection.activeObject != null)
        {
            return AssetDatabase.GetAssetPath(Selection.activeObject);
        }

        string path = GetActiveFolderPath_Reflection();
        if (!string.IsNullOrEmpty(path))
        {
            return path;
        }

        return "Assets";
    }

    private static string GetActiveFolderPath_Reflection()
    {
        try
        {
            System.Type projectBrowserType = System.Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
            EditorWindow window = EditorWindow.focusedWindow;
            
            if (window == null || window.GetType() != projectBrowserType)
            {
                var windows = Resources.FindObjectsOfTypeAll(projectBrowserType);
                if (windows.Length > 0) window = windows[0] as EditorWindow;
            }

            if (window != null)
            {
                MethodInfo getMethod = projectBrowserType.GetMethod("GetActiveFolderPath", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getMethod != null) return (string)getMethod.Invoke(window, null);
            }
        }
        catch { return null; }
        return null;
    }
}