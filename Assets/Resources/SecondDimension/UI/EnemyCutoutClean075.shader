Shader "SecondDimension/UI/EnemyCutoutClean075"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _RemasterAccentColor098 ("Exact Myrmidon Crystal Accent", Color) = (1,1,1,1)
        _RemasterAccentAmount098 ("Exact Myrmidon Accent Amount", Range(0, 1)) = 0
        _AlphaFloor ("Transparent Fringe Floor", Range(0, 0.5)) = 0.08
        _AlphaFeather ("Clean Edge Feather", Range(0.001, 0.5)) = 0.16

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "EnemyCutoutClean075"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _RemasterAccentColor098;
            float _RemasterAccentAmount098;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _AlphaFloor;
            float _AlphaFeather;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(output.worldPosition);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 PreferMoreOpaque075(fixed4 current, fixed4 candidate)
            {
                return candidate.a > current.a ? candidate : current;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;

                // Pull edge RGB from the nearest more-opaque texel. This removes the
                // colored low-alpha matte baked into generated transparent cutouts
                // without expanding their silhouette or changing the source alpha.
                fixed4 interior = source;
                float2 texel = _MainTex_TexelSize.xy;
                interior = PreferMoreOpaque075(interior,
                    tex2D(_MainTex, input.texcoord + float2(texel.x, 0)) + _TextureSampleAdd);
                interior = PreferMoreOpaque075(interior,
                    tex2D(_MainTex, input.texcoord - float2(texel.x, 0)) + _TextureSampleAdd);
                interior = PreferMoreOpaque075(interior,
                    tex2D(_MainTex, input.texcoord + float2(0, texel.y)) + _TextureSampleAdd);
                interior = PreferMoreOpaque075(interior,
                    tex2D(_MainTex, input.texcoord - float2(0, texel.y)) + _TextureSampleAdd);

                float edgeRepair = saturate((interior.a - source.a) * 5.0);
                fixed3 repairedRgb = lerp(source.rgb, interior.rgb, edgeRepair);
                // Default disabled: all ordinary sprites preserve their original
                // shader arithmetic/output. Only a caller-owned exact Myrmidon
                // material enables this purple-crystal accent; alpha is untouched.
                if (_RemasterAccentAmount098 > 0.0)
                {
                    float peak098 = max(repairedRgb.r, max(repairedRgb.g, repairedRgb.b));
                    float low098 = min(repairedRgb.r, min(repairedRgb.g, repairedRgb.b));
                    float saturation098 = (peak098 - low098) / max(peak098, 0.001);
                    float purple098 = min(repairedRgb.r, repairedRgb.b) - repairedRgb.g;
                    float mask098 = smoothstep(0.04, 0.20, purple098) *
                        smoothstep(0.18, 0.40, saturation098);
                    fixed3 accent098 = _RemasterAccentColor098.rgb * peak098;
                    repairedRgb = lerp(repairedRgb, accent098,
                        mask098 * saturate(_RemasterAccentAmount098));
                }
                float feather = max(_AlphaFeather, 0.001);
                float cleanedAlpha = smoothstep(
                    _AlphaFloor,
                    min(1.0, _AlphaFloor + feather),
                    source.a);

                fixed4 color = fixed4(
                    repairedRgb * input.color.rgb,
                    cleanedAlpha * input.color.a);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
