Shader "Tower Defense/Ground Light"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0.13
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-8" "RenderType"="Transparent" }
        Pass
        {
            Name "GroundLight"
            Blend SrcAlpha One
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Strength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv * 2.0 - 1.0;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half falloff = 1.0h - smoothstep(0.03h, 1.0h, dot(input.uv, input.uv));
                return half4(input.color.rgb, input.color.a * _Strength * falloff * falloff);
            }
            ENDHLSL
        }
    }
}
