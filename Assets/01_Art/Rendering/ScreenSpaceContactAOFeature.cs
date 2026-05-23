using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public sealed class ScreenSpaceContactAOFeature : ScriptableRendererFeature
{
    [Serializable]
    public sealed class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        [Range(0f, 4f)] public float intensity = 1.35f;
        [Range(0.5f, 12f)] public float radiusPixels = 4.5f;
        [Range(0f, 0.08f)] public float depthBias = 0.01f;
        [Range(0.01f, 1.5f)] public float thickness = 0.38f;
        [Range(0.5f, 4f)] public float contrast = 1.45f;
        [Range(0f, 1f)] public float luminanceProtection = 0.2f;
        [Range(0f, 4f)] public float directionalStrength = 1.4f;
        public Vector2 directionalOffsetPixels = new Vector2(-4f, 4f);
    }

    [SerializeField] private Shader shader;
    [SerializeField] private Settings settings = new Settings();

    private Material material;
    private ContactAOPass pass;

    public override void Create()
    {
        if (shader == null)
        {
            shader = Shader.Find("Hidden/Paxie/ScreenSpaceContactAO");
        }

        if (material == null && shader != null)
        {
            material = CoreUtils.CreateEngineMaterial(shader);
        }

        pass = new ContactAOPass(settings, material)
        {
            renderPassEvent = settings.renderPassEvent,
            requiresIntermediateTexture = true
        };

        pass.ConfigureInput(ScriptableRenderPassInput.Depth);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null || renderingData.cameraData.cameraType == CameraType.Preview)
        {
            return;
        }

        pass.UpdateSettings(settings, material);
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        CoreUtils.Destroy(material);
    }

    private sealed class ContactAOPass : ScriptableRenderPass
    {
        private static readonly int IntensityId = Shader.PropertyToID("_ContactAOIntensity");
        private static readonly int RadiusPixelsId = Shader.PropertyToID("_ContactAORadiusPixels");
        private static readonly int DepthBiasId = Shader.PropertyToID("_ContactAODepthBias");
        private static readonly int ThicknessId = Shader.PropertyToID("_ContactAOThickness");
        private static readonly int ContrastId = Shader.PropertyToID("_ContactAOContrast");
        private static readonly int LuminanceProtectionId = Shader.PropertyToID("_ContactAOLuminanceProtection");
        private static readonly int DirectionalStrengthId = Shader.PropertyToID("_ContactAODirectionalStrength");
        private static readonly int DirectionalOffsetPixelsId = Shader.PropertyToID("_ContactAODirectionalOffsetPixels");
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");
        private static readonly int BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");

        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        private Settings settings;
        private Material material;
        private RTHandle colorCopy;

        public ContactAOPass(Settings settings, Material material)
        {
            this.settings = settings;
            this.material = material;
            profilingSampler = new ProfilingSampler("Screen Space Contact AO");
        }

        public void UpdateSettings(Settings settings, Material material)
        {
            this.settings = settings;
            this.material = material;
            renderPassEvent = settings.renderPassEvent;
        }

#if URP_COMPATIBILITY_MODE
#pragma warning disable 618, 672
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor copyDescriptor = cameraTextureDescriptor;
            copyDescriptor.depthBufferBits = 0;
            copyDescriptor.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(ref colorCopy, copyDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ScreenSpaceContactAOColorCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || colorCopy == null)
            {
                return;
            }

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            CommandBuffer cmd = CommandBufferPool.Get("Screen Space Contact AO");

            using (new ProfilingScope(cmd, profilingSampler))
            {
                material.SetFloat(IntensityId, settings.intensity);
                material.SetFloat(RadiusPixelsId, settings.radiusPixels);
                material.SetFloat(DepthBiasId, settings.depthBias);
                material.SetFloat(ThicknessId, settings.thickness);
                material.SetFloat(ContrastId, settings.contrast);
                material.SetFloat(LuminanceProtectionId, settings.luminanceProtection);
                material.SetFloat(DirectionalStrengthId, settings.directionalStrength);
                material.SetVector(DirectionalOffsetPixelsId, settings.directionalOffsetPixels);

                Blitter.BlitCameraTexture(cmd, source, colorCopy);
                Blitter.BlitCameraTexture(cmd, colorCopy, source, material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore 618, 672
#endif

        public void Dispose()
        {
            colorCopy?.Release();
            colorCopy = null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null)
            {
                return;
            }

            UniversalResourceData resources = frameData.Get<UniversalResourceData>();

            if (!resources.activeColorTexture.IsValid() || !resources.cameraDepthTexture.IsValid())
            {
                return;
            }

            TextureDesc copyDesc = renderGraph.GetTextureDesc(resources.activeColorTexture);
            copyDesc.name = "_ScreenSpaceContactAOColorCopy";
            copyDesc.clearBuffer = false;
            copyDesc.msaaSamples = MSAASamples.None;

            TextureHandle colorCopy = renderGraph.CreateTexture(copyDesc);
            renderGraph.AddBlitPass(resources.activeColorTexture, colorCopy, Vector2.one, Vector2.zero, passName: "Copy Color For Contact AO");

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space Contact AO", out PassData passData, profilingSampler))
            {
                passData.inputColor = colorCopy;
                passData.material = material;
                passData.propertyBlock = propertyBlock;
                passData.intensity = settings.intensity;
                passData.radiusPixels = settings.radiusPixels;
                passData.depthBias = settings.depthBias;
                passData.thickness = settings.thickness;
                passData.contrast = settings.contrast;
                passData.luminanceProtection = settings.luminanceProtection;
                passData.directionalStrength = settings.directionalStrength;
                passData.directionalOffsetPixels = settings.directionalOffsetPixels;

                builder.UseTexture(colorCopy, AccessFlags.Read);
                builder.UseTexture(resources.cameraDepthTexture, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.propertyBlock.Clear();
                    data.propertyBlock.SetTexture(BlitTextureId, data.inputColor);
                    data.propertyBlock.SetVector(BlitScaleBiasId, new Vector4(1f, 1f, 0f, 0f));
                    data.propertyBlock.SetFloat(IntensityId, data.intensity);
                    data.propertyBlock.SetFloat(RadiusPixelsId, data.radiusPixels);
                    data.propertyBlock.SetFloat(DepthBiasId, data.depthBias);
                    data.propertyBlock.SetFloat(ThicknessId, data.thickness);
                    data.propertyBlock.SetFloat(ContrastId, data.contrast);
                    data.propertyBlock.SetFloat(LuminanceProtectionId, data.luminanceProtection);
                    data.propertyBlock.SetFloat(DirectionalStrengthId, data.directionalStrength);
                    data.propertyBlock.SetVector(DirectionalOffsetPixelsId, data.directionalOffsetPixels);

                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0, MeshTopology.Triangles, 3, 1, data.propertyBlock);
                });
            }
        }

        private sealed class PassData
        {
            public TextureHandle inputColor;
            public Material material;
            public MaterialPropertyBlock propertyBlock;
            public float intensity;
            public float radiusPixels;
            public float depthBias;
            public float thickness;
            public float contrast;
            public float luminanceProtection;
            public float directionalStrength;
            public Vector2 directionalOffsetPixels;
        }
    }
}
