using UnityEngine;

public class ShowMyPath : MonoBehaviour
{
    void OnDrawGizmos()
    {
        MeshFilter mf = GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        Gizmos.color = Color.cyan;
        foreach (var v in mf.sharedMesh.vertices)
        {
            // Draws a diamond at every vertex so you can see your path!
            Gizmos.DrawIcon(transform.TransformPoint(v), "d_scenepicking_pickable", true);
        }
    }
}