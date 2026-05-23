Shader "Hidden/Paxie/ScreenSpaceContactAO"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ScreenSpaceContactAO"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ContactAOIntensity;
            float _ContactAORadiusPixels;
            float _ContactAODepthBias;
            float _ContactAOThickness;
            float _ContactAOContrast;
            float _ContactAOLuminanceProtection;
            float _ContactAODirectionalStrength;
            float2 _ContactAODirectionalOffsetPixels;

            static const float2 kOffsets[12] =
            {
                float2( 1.000,  0.000),
                float2(-1.000,  0.000),
                float2( 0.000,  1.000),
                float2( 0.000, -1.000),
                float2( 0.707,  0.707),
                float2(-0.707,  0.707),
                float2( 0.707, -0.707),
                float2(-0.707, -0.707),
                float2( 0.382,  0.924),
                float2(-0.924,  0.382),
                float2( 0.924, -0.382),
                float2(-0.382, -0.924)
            };

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord.xy;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float centerDepth = LinearDepthAt(uv);
                float2 texel = rcp(_ScreenSize.xy);
                float radius = max(_ContactAORadiusPixels, 0.5);

                float occlusion = 0.0;
                float weightTotal = 0.0;

                [unroll]
                for (int i = 0; i < 12; i++)
                {
                    float ring = (i < 4) ? 0.55 : ((i < 8) ? 1.0 : 1.45);
                    float2 sampleUv = saturate(uv + kOffsets[i] * texel * radius * ring);
                    float sampleDepth = LinearDepthAt(sampleUv);

                    float closerAmount = centerDepth - sampleDepth - _ContactAODepthBias;
                    float contact = smoothstep(0.0, max(_ContactAOThickness, 0.0001), closerAmount);
                    float weight = rcp(0.45 + ring);

                    occlusion += contact * weight;
                    weightTotal += weight;
                }

                occlusion = saturate(occlusion / max(weightTotal, 0.0001));
                occlusion = pow(occlusion, max(_ContactAOContrast, 0.001));

                float directionalOcclusion = 0.0;

                [unroll]
                for (int j = 1; j <= 4; j++)
                {
                    float stepWeight = (5.0 - j) * 0.25;
                    float2 sampleUv = saturate(uv + _ContactAODirectionalOffsetPixels * texel * (j * 0.55));
                    float sampleDepth = LinearDepthAt(sampleUv);
                    float closerAmount = centerDepth - sampleDepth - _ContactAODepthBias;
                    float contact = smoothstep(0.0, max(_ContactAOThickness * 1.25, 0.0001), closerAmount);
                    directionalOcclusion += contact * stepWeight;
                }

                occlusion = saturate(occlusion + directionalOcclusion * 0.45 * _ContactAODirectionalStrength);

                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                float protection = lerp(1.0, saturate(luminance * 1.35), _ContactAOLuminanceProtection);
                float ao = saturate(1.0 - occlusion * _ContactAOIntensity * protection);

                color.rgb *= ao;
                return color;
            }
            ENDHLSL
        }
    }
}
