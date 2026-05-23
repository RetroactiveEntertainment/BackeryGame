Shader "Custom/Background/RadialGradient_ReceiveShadowAO_Unity6"
{
    Properties
    {
        _CenterColor ("Center Color", Color) = (0.23, 0.14, 0.43, 1)
        _EdgeColor ("Edge Color", Color) = (0.06, 0.03, 0.18, 1)

        _Center ("Center", Vector) = (0.5, 0.52, 0, 0)
        _Radius ("Radius", Float) = 0.75
        _Softness ("Softness", Float) = 1.4
        _Intensity ("Intensity", Float) = 1.0

        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.55
        _AOStrength ("AO Strength", Range(0,1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CenterColor;
                float4 _EdgeColor;
                float4 _Center;

                float _Radius;
                float _Softness;
                float _Intensity;

                float _ShadowStrength;
                float _AOStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                OUT.uv = IN.uv;

                OUT.shadowCoord = GetShadowCoord(positionInputs);
                OUT.screenPos = ComputeScreenPos(positionInputs.positionCS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Procedural radial gradient
                float dist = distance(IN.uv, _Center.xy);
                float t = saturate(dist / max(_Radius, 0.0001));
                t = pow(t, _Softness);

                float3 gradient = lerp(_CenterColor.rgb, _EdgeColor.rgb, t);
                gradient *= _Intensity;

                // Main light shadow
                Light mainLight = GetMainLight(IN.shadowCoord);
                float shadow = mainLight.shadowAttenuation;

                // shadow = 1 no shadow, 0 shadow
                float shadowDarken = lerp(1.0, shadow, _ShadowStrength);

                // SSAO
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(screenUV);

                float ao = aoFactor.indirectAmbientOcclusion;
                float aoDarken = lerp(1.0, ao, _AOStrength);

                float3 finalColor = gradient * shadowDarken * aoDarken;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DepthOnlyFragment(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }

            ZWrite On
            Cull Off

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings DepthNormalsVertex(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);

                return OUT;
            }

            half4 DepthNormalsFragment(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                return half4(normalWS * 0.5 + 0.5, 1.0);
            }

            ENDHLSL
        }
    }
}