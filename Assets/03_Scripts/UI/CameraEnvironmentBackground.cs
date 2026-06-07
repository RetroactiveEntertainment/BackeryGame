using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraEnvironmentBackground : MonoBehaviour
{
    [SerializeField] private Texture2D environmentTexture;
    [SerializeField] private float backgroundDistance = 900f;
    [SerializeField] private float coverPadding = 1.05f;

    private const string BackgroundName = "EnvironmentBackground";

    private Camera targetCamera;
    private Transform backgroundTransform;
    private MeshRenderer backgroundRenderer;
    private MeshFilter backgroundFilter;
    private Material backgroundMaterial;

    private static readonly string[] TextureProperties = { "_BaseMap", "_MainTex", "_BaseColorMap" };

    private void OnEnable()
    {
        targetCamera = GetComponent<Camera>();
        EnsureBackground();
        UpdateBackground();
    }

    private void OnDisable()
    {
        if (backgroundMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(backgroundMaterial);
            else
                DestroyImmediate(backgroundMaterial);
        }
    }

    private void LateUpdate()
    {
        EnsureBackground();
        UpdateBackground();
    }

    private void OnValidate()
    {
        targetCamera = GetComponent<Camera>();
        EnsureBackground();
        UpdateBackground();
    }

    private void EnsureBackground()
    {
        if (backgroundTransform == null)
        {
            Transform existing = transform.Find(BackgroundName);
            if (existing != null)
            {
                backgroundTransform = existing;
            }
            else
            {
                GameObject background = new GameObject(BackgroundName);
                background.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
                backgroundTransform = background.transform;
                backgroundTransform.SetParent(transform, false);
            }
        }

        backgroundFilter = backgroundTransform.GetComponent<MeshFilter>();
        if (backgroundFilter == null)
            backgroundFilter = backgroundTransform.gameObject.AddComponent<MeshFilter>();

        backgroundRenderer = backgroundTransform.GetComponent<MeshRenderer>();
        if (backgroundRenderer == null)
            backgroundRenderer = backgroundTransform.gameObject.AddComponent<MeshRenderer>();

        if (backgroundFilter.sharedMesh == null)
            backgroundFilter.sharedMesh = CreateQuadMesh();

        if (backgroundMaterial == null)
        {
            Shader shader = FindBackgroundShader();
            if (shader == null)
            {
                Debug.LogWarning("Environment background shader could not be found. Add an unlit/sprite shader to Always Included Shaders.");
                return;
            }

            backgroundMaterial = new Material(shader);
            backgroundMaterial.name = "Environment Background Material";
            backgroundMaterial.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            backgroundMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry - 10;
            SetMaterialFloatIfPresent(backgroundMaterial, "_Cull", 0f);
            SetMaterialFloatIfPresent(backgroundMaterial, "_Surface", 0f);
            SetMaterialFloatIfPresent(backgroundMaterial, "_ZWrite", 1f);
            SetMaterialFloatIfPresent(backgroundMaterial, "_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        backgroundRenderer.sharedMaterial = backgroundMaterial;
        backgroundRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        backgroundRenderer.receiveShadows = false;
        backgroundRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private void UpdateBackground()
    {
        if (targetCamera == null || backgroundTransform == null || backgroundMaterial == null || environmentTexture == null)
            return;

        SetMaterialTexture(backgroundMaterial, environmentTexture);

        float distance = Mathf.Clamp(backgroundDistance, targetCamera.nearClipPlane + 0.01f, targetCamera.farClipPlane - 1f);
        backgroundTransform.localPosition = new Vector3(0f, 0f, distance);
        backgroundTransform.localRotation = Quaternion.identity;

        float viewHeight;
        float viewWidth;
        if (targetCamera.orthographic)
        {
            viewHeight = targetCamera.orthographicSize * 2f;
            viewWidth = viewHeight * targetCamera.aspect;
        }
        else
        {
            viewHeight = 2f * backgroundTransform.localPosition.z * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            viewWidth = viewHeight * targetCamera.aspect;
        }

        float textureAspect = (float)environmentTexture.width / environmentTexture.height;
        float viewAspect = viewWidth / viewHeight;
        float backgroundWidth;
        float backgroundHeight;

        if (textureAspect > viewAspect)
        {
            backgroundHeight = viewHeight;
            backgroundWidth = viewHeight * textureAspect;
        }
        else
        {
            backgroundWidth = viewWidth;
            backgroundHeight = viewWidth / textureAspect;
        }

        backgroundTransform.localScale = new Vector3(backgroundWidth * coverPadding, backgroundHeight * coverPadding, 1f);
    }

    private static Shader FindBackgroundShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
            return shader;

        shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader != null)
            return shader;

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
            return shader;

        return Shader.Find("Unlit/Texture");
    }

    private static void SetMaterialTexture(Material material, Texture2D texture)
    {
        material.mainTexture = texture;
        for (int i = 0; i < TextureProperties.Length; i++)
        {
            if (material.HasProperty(TextureProperties[i]))
                material.SetTexture(TextureProperties[i], texture);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);
    }

    private static void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }

    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Environment Background Quad";
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }
}
