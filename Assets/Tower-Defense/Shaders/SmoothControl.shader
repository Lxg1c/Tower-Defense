Shader "TowerDefense/UI/SmoothControl"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Shape ("Shape: disc, ring, stroke", Float) = 0
        _Aspect ("Capsule width / height", Float) = 1
        _CornerRadius ("Panel corner radius", Range(0.001,0.5)) = 0.04
        _RingWidth ("Ring width", Range(0.001,0.2)) = 0.018
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 local : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            fixed4 _Color;
            float _Shape, _RingWidth, _Aspect, _CornerRadius;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Inset coverage by one output pixel: the mesh edge never cuts off the AA fringe.
                float2 pixel = max(fwidth(i.uv), float2(0.00001,0.00001));
                float coverage;
                if (_Shape > 4.5)
                {
                    float2 p = (i.uv - 0.5) * float2(_Aspect,1);
                    float2 q = abs(p) - float2(_Aspect*0.5,0.5) + _CornerRadius;
                    float distance = length(max(q,0)) + min(max(q.x,q.y),0) - _CornerRadius;
                    float aa = max(fwidth(distance),0.00001);
                    coverage = 1-smoothstep(-aa*1.5,-aa*0.5,distance);
                    if (_Shape > 5.5)
                        coverage *= smoothstep(-_RingWidth-aa*0.5,-_RingWidth+aa*0.5,distance);
                }
                else if (_Shape > 2.5)
                {
                    float2 p = (i.uv - 0.5) * float2(_Aspect,1);
                    p.x = max(abs(p.x) - max(0,(_Aspect-1)*0.5),0);
                    float distance = length(p);
                    float aa = max(fwidth(distance),0.00001);
                    float outer = 0.5-aa;
                    coverage = 1-smoothstep(outer-aa*0.5,outer+aa*0.5,distance);
                    if (_Shape > 3.5)
                        coverage *= smoothstep(outer-_RingWidth-aa*0.5,outer-_RingWidth+aa*0.5,distance);
                }
                else if (_Shape > 1.5)
                {
                    float2 edge = min(i.uv, 1-i.uv) / pixel;
                    coverage = saturate(min(edge.x,edge.y) - 0.5);
                }
                else
                {
                    float radius = length(i.uv - 0.5);
                    float aa = max(fwidth(radius),0.00001);
                    float outer = 0.5 - aa;
                    coverage = 1-smoothstep(outer-aa*0.5,outer+aa*0.5,radius);
                    if (_Shape > 0.5)
                        coverage *= smoothstep(outer-_RingWidth-aa*0.5,outer-_RingWidth+aa*0.5,radius);
                }
                fixed4 color = i.color;
                color.a *= coverage;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
