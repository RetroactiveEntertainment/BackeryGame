#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BlenderSceneViewControls
{
    // --- Panning State and Speed ---
    private static bool isPanning = false;
    private static Vector2 lastMousePosition;
    private const float PanSpeed = 0.0025f;
    private const float X_DIRECTION = -1f;
    private const float Y_DIRECTION = 1f;

    // --- Snapping State ---
    // RESTORED: SnapClickThreshold for click detection stability
    private const float SnapClickThreshold = 5f; 
    private static Vector2 mouseDownPosition;
    private static bool isMouseDown = false;

    // Define the six standard view rotations
    private static readonly Quaternion[] SnapRotations = new Quaternion[]
    {
        Quaternion.Euler(0, 0, 0),        // Front (+Z)
        Quaternion.Euler(0, 180, 0),      // Back (-Z)
        Quaternion.Euler(90, 0, 0),       // Top (+Y)
        Quaternion.Euler(-90, 0, 0),      // Bottom (-Y)
        Quaternion.Euler(0, 90, 0),       // Right (+X)
        Quaternion.Euler(0, -90, 0)       // Left (-X)
    };


    static BlenderSceneViewControls()
    {
        SceneView.beforeSceneGui -= OnSceneGUI;
        SceneView.beforeSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event currentEvent = Event.current;
        Camera sceneCamera = sceneView.camera; 

        if (sceneCamera == null) return;

        // --- MMB DOWN LOGIC (Track Click Start) ---
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 2)
        {
            isMouseDown = true;
            mouseDownPosition = currentEvent.mousePosition;
            lastMousePosition = currentEvent.mousePosition;
            
            // If no modifiers are held (potential Orbit start)
            if (!currentEvent.shift && !currentEvent.alt)
            {
                // Ortho-to-Perspective Switch (WORKING FIX)
                if (sceneView.orthographic)
                {
                    sceneView.orthographic = false;
                }
            } else if (currentEvent.shift)
            {
                // Start Shift + MMB Pan
                isPanning = true;
                currentEvent.Use(); 
            }
        }

        // --- MMB UP LOGIC (Restored click detection) ---
        if (currentEvent.type == EventType.MouseUp && currentEvent.button == 2)
        {
            if (isMouseDown)
            {
                // RESTORED: Check if the MMB input was a click (minimal movement)
                bool isClick = Vector2.Distance(mouseDownPosition, currentEvent.mousePosition) < SnapClickThreshold;
                
                if (isClick && currentEvent.alt)
                {
                    currentEvent.Use(); 
                    SnapToNearestView(sceneView);
                    
                    // Clear the Alt modifier state 
                    currentEvent.modifiers &= ~EventModifiers.Alt;
                }
            }
            isMouseDown = false;
            isPanning = false; 
        }
        
        // --- MMB DRAG LOGIC ---
        if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 2)
        {
            // 1. Shift + MMB Pan
            if (currentEvent.shift && isPanning)
            {
                PerformCustomPan(sceneView, currentEvent, sceneCamera);
                return; 
            }

            // 2. MMB Drag (Ortho Pan)
            if (!currentEvent.shift && !currentEvent.alt)
            {
                // This handles MMB-only drag. If the camera is in Ortho mode, it PANS.
                if (sceneView.orthographic)
                {
                    PerformCustomPan(sceneView, currentEvent, sceneCamera);
                    return;
                }
                
                // If not orthographic, we let the native MMB orbit take over.
            }
        }
    }
    
    // Pan implementation
    private static void PerformCustomPan(SceneView sceneView, Event currentEvent, Camera sceneCamera)
    {
        currentEvent.Use();
        
        Vector2 delta = currentEvent.mousePosition - lastMousePosition;
        Vector3 worldRight = sceneCamera.transform.right;
        Vector3 worldUp = sceneCamera.transform.up;

        Vector3 move = (worldRight * delta.x * X_DIRECTION * PanSpeed * sceneView.size) + 
                       (worldUp * delta.y * Y_DIRECTION * PanSpeed * sceneView.size);

        sceneView.pivot += move;
        lastMousePosition = currentEvent.mousePosition;
        sceneView.Repaint();
    }

    // Snap implementation
    private static void SnapToNearestView(SceneView sceneView)
    {
        Quaternion currentRotation = sceneView.rotation;
        float smallestAngle = float.MaxValue;
        Quaternion targetRotation = currentRotation;
        
        foreach (Quaternion snapRotation in SnapRotations)
        {
            float angle = Quaternion.Angle(currentRotation, snapRotation);
            
            if (angle < smallestAngle)
            {
                smallestAngle = angle;
                targetRotation = snapRotation;
            }
        }
        
        sceneView.orthographic = true;
        sceneView.LookAt(sceneView.pivot, targetRotation);
        sceneView.Repaint();
    }
}
#endif