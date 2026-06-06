Shader "BackeryGame/Sprite Gradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _GradientTop ("Gradient Top", Color) = (1,0.9,0.25,1)
        _GradientBottom ("Gradient Bottom", Color) = (1,0.35,0.05,1)
        _GradientAngle ("Gradient Angle", Range(0, 360)) = 90
        _GradientScale ("Gradient Scale", Float) = 1
        _GradientOffset ("Gradient Offset", Range(-1, 1)) = 0
        _TextureStrength ("Texture Strength", Range(0, 1)) = 1
        [Enum(Multiply,0,Add,1,Overlay,2,Replace,3,Normal,4)] _GradientBlendMode ("Gradient Blend Mode", Float) = 0
        [Toggle] _UseUvGradient ("Use UV Gradient", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0

        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        BlendOp [_BlendOp]
        Blend [_SrcBlend] [_DstBlend]
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            fixed4 _Color;
            fixed4 _RendererColor;
            fixed4 _GradientTop;
            fixed4 _GradientBottom;
            float _GradientAngle;
            float _GradientScale;
            float _GradientOffset;
            float _TextureStrength;
            float _GradientBlendMode;
            float _UseUvGradient;
            float _EnableExternalAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.localPosition = v.vertex.xy;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 spriteColor = SampleSpriteTexture(i.texcoord);

                float angle = radians(_GradientAngle);
                float2 direction = float2(cos(angle), sin(angle));
                float2 gradientPosition = lerp(i.localPosition, i.texcoord - 0.5, _UseUvGradient);
                float gradientValue = dot(gradientPosition, direction) * max(_GradientScale, 0.0001) + 0.5 + _GradientOffset;
                fixed4 gradientColor = lerp(_GradientBottom, _GradientTop, saturate(gradientValue));

                fixed3 baseColor = lerp(fixed3(1.0, 1.0, 1.0), spriteColor.rgb, _TextureStrength) * i.color.rgb;
                fixed3 gradientRgb = gradientColor.rgb;
                fixed3 overlayColor = lerp(
                    2.0 * baseColor * gradientRgb,
                    1.0 - 2.0 * (1.0 - baseColor) * (1.0 - gradientRgb),
                    step(0.5, baseColor));

                fixed4 outputColor;
                if (_GradientBlendMode < 0.5)
                {
                    outputColor.rgb = baseColor * gradientRgb;
                }
                else if (_GradientBlendMode < 1.5)
                {
                    outputColor.rgb = saturate(baseColor + gradientRgb);
                }
                else if (_GradientBlendMode < 2.5)
                {
                    outputColor.rgb = overlayColor;
                }
                else if (_GradientBlendMode < 3.5)
                {
                    outputColor.rgb = gradientRgb * i.color.rgb;
                }
                else
                {
                    outputColor.rgb = lerp(baseColor, gradientRgb * i.color.rgb, gradientColor.a);
                }

                outputColor.a = spriteColor.a * gradientColor.a * i.color.a;
                outputColor.rgb *= outputColor.a;
                return outputColor;
            }
            ENDCG
        }
    }
}
