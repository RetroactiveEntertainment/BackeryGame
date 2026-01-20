using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class QuickMaterialCreator
{
    [MenuItem("Assets/Create Smart Material", false, 10)]
    private static void CreateSmartMaterial()
    {
        Object[] selectedObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (selectedObjects.Length == 0) return;

        Dictionary<string, List<Texture2D>> textureGroups = new Dictionary<string, List<Texture2D>>();

        foreach (Object obj in selectedObjects)
        {
            Texture2D tex = (Texture2D)obj;
            string baseName = GetBaseName(tex.name);

            if (!textureGroups.ContainsKey(baseName))
                textureGroups[baseName] = new List<Texture2D>();
            
            textureGroups[baseName].Add(tex);
        }

        foreach (var group in textureGroups)
        {
            CreateMaterialForGroup(group.Key, group.Value);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateMaterialForGroup(string baseName, List<Texture2D> textures)
    {
        string path = AssetDatabase.GetAssetPath(textures[0]);
        string directory = Path.GetDirectoryName(path);
        string matPath = Path.Combine(directory, "M_" + baseName + ".mat");

        // Try to find URP Lit first, fallback to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        
        Material mat = new Material(shader);

        string[] albedoNaming = { "_AlbedoTransparency", "_Albedo", "_BaseColor", "_Diffuse" };
        string[] normalNaming = { "_Normal", "_Normals", "_Nrm", "_Bump" };
        string[] metallicNaming = { "_MetallicSmoothness", "_Metallic", "_Metalness" };
        string[] aoNaming = { "_AO", "_AmbientOcclusion", "_Occlusion" };
        string[] heightNaming = { "_Height", "_Displacement" };

        foreach (Texture2D tex in textures)
        {
            string name = tex.name;

            if (ContainsAny(name, albedoNaming))
                AssignTexture(mat, tex, "_BaseMap", "_MainTex");
            
            else if (ContainsAny(name, normalNaming))
                AssignTexture(mat, tex, "_BumpMap", "_BumpMap");
            
            else if (ContainsAny(name, metallicNaming))
            {
                AssignTexture(mat, tex, "_MetallicGlossMap", "_MetallicGlossMap");
                // Set smoothness to 1 so the texture map takes full control
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f);
            }
            
            else if (ContainsAny(name, aoNaming))
                AssignTexture(mat, tex, "_OcclusionMap", "_OcclusionMap");

            else if (ContainsAny(name, heightNaming))
                AssignTexture(mat, tex, "_ParallaxMap", "_ParallaxMap");
        }

        AssetDatabase.CreateAsset(mat, matPath);
    }

    private static bool ContainsAny(string name, string[] suffixes)
    {
        return suffixes.Any(s => name.EndsWith(s, System.StringComparison.OrdinalIgnoreCase));
    }

    private static void AssignTexture(Material mat, Texture2D tex, string urpProp, string stdProp)
    {
        if (mat.HasProperty(urpProp)) mat.SetTexture(urpProp, tex);
        else if (mat.HasProperty(stdProp)) mat.SetTexture(stdProp, tex);
    }

    private static string GetBaseName(string fullName)
    {
        string[] allSuffixes = { 
            "_AlbedoTransparency", "_Albedo", "_BaseColor", "_Diffuse",
            "_MetallicSmoothness", "_Metallic", "_Metalness",
            "_Normal", "_Normals", "_Nrm", "_Bump",
            "_AO", "_AmbientOcclusion", "_Occlusion",
            "_Height", "_Displacement"
        };

        string baseName = fullName;
        foreach (string s in allSuffixes)
        {
            if (baseName.EndsWith(s, System.StringComparison.OrdinalIgnoreCase))
            {
                baseName = baseName.Substring(0, baseName.Length - s.Length);
                break;
            }
        }
        return baseName;
    }
}