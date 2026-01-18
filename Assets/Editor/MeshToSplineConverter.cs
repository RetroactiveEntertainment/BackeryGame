using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using System.Collections.Generic;
using System.Linq;

public class RoadFixerPro : EditorWindow
{
    [MenuItem("Tools/Fixed Distance-Sorted Spline")]
    public static void Convert()
    {
        GameObject selected = Selection.activeGameObject;
        MeshFilter mf = selected?.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh.vertexCount == 0) return;

        // 1. Get unique vertex positions (removes dummy face overlaps)
        List<Vector3> verts = mf.sharedMesh.vertices.Distinct().ToList();
        List<Vector3> sortedPath = new List<Vector3>();

        // 2. Nearest Neighbor Sorting
        // Start at the vertex closest to the object's origin
        Vector3 current = verts[0];
        sortedPath.Add(current);
        verts.RemoveAt(0);

        while (verts.Count > 0)
        {
            // Find the closest remaining vertex to our current point
            int nextIndex = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < verts.Count; i++)
            {
                float d = Vector3.Distance(current, verts[i]);
                if (d < minDist) { minDist = d; nextIndex = i; }
            }
            current = verts[nextIndex];
            sortedPath.Add(current);
            verts.RemoveAt(nextIndex);
        }

        // 3. Generate the Spline
        GameObject splineObj = new GameObject(selected.name + "_CleanPath");
        splineObj.transform.position = selected.transform.position;
        SplineContainer container = splineObj.AddComponent<SplineContainer>();
        
        foreach (var p in sortedPath)
        {
            // We use AutoSmooth immediately to match your Blender curves
            container.Spline.Add(new BezierKnot(p), TangentMode.AutoSmooth);
        }

        // 4. Set Visuals for Mobile Casual Style
        var extrude = splineObj.AddComponent<SplineExtrude>();
        extrude.Radius = 0.6f; 
        extrude.SegmentsPerUnit = 12; // Mobile optimized

        Selection.activeGameObject = splineObj;
        Debug.Log($"Sorted {sortedPath.Count} points into a clean road!");
    }
}