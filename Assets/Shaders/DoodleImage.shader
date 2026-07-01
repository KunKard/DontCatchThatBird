Shader "UI/DoodleImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineWidth ("Outline Width", Range(0, 10)) = 5
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineDensity ("Outline Density", Range(0, 1)) = 0.75
        _NoiseScale ("Noise Scale", Range(1, 50)) = 12
        _OutlineJitter ("Outline Jitter", Range(0, 2)) = 0.5
        _OutlineSpeed  ("Outline Speed",  Range(0, 10)) = 2
        _BodyJitter ("Body Jitter", Range(0, 2)) = 0.25
        _BodySpeed  ("Body Speed",  Range(0, 10)) = 1
        _BodyOffset ("Body Offset", Vector) = (0, 0, 0, 0)

        _StencilComp  ("Stencil Comparison", Float) = 8
        _Stencil      ("Stencil ID", Float) = 0
        _StencilOp    ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask", Float) = 255
        _ColorMask    ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp]
            ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _ClipRect;
            float _OutlineWidth;
            fixed4 _OutlineColor;
            float _OutlineDensity;
            float _NoiseScale;
            float _OutlineJitter;
            float _OutlineSpeed;
            float _BodyJitter;
            float _BodySpeed;
            float4 _BodyOffset;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }

            // ========== Procedural Noise ==========

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // ========== Alpha Outline ==========

            float SampleMaxAlpha(float2 uv, float width)
            {
                float2 texel = _MainTex_TexelSize.xy * width;
                float a = 0;
                a = max(a, tex2D(_MainTex, uv + float2( texel.x, 0)).a);
                a = max(a, tex2D(_MainTex, uv + float2(-texel.x, 0)).a);
                a = max(a, tex2D(_MainTex, uv + float2(0,  texel.y)).a);
                a = max(a, tex2D(_MainTex, uv + float2(0, -texel.y)).a);
                a = max(a, tex2D(_MainTex, uv + float2( texel.x,  texel.y)).a);
                a = max(a, tex2D(_MainTex, uv + float2(-texel.x,  texel.y)).a);
                a = max(a, tex2D(_MainTex, uv + float2( texel.x, -texel.y)).a);
                a = max(a, tex2D(_MainTex, uv + float2(-texel.x, -texel.y)).a);
                return a;
            }

            // ========== Fragment ==========

            fixed4 frag(v2f i) : SV_Target
            {
                // 主体抖动：0.2~0.5px 的 UV 偏移
                float2 bodyUV = i.texcoord;
                if (_BodyJitter > 0.001)
                {
                    float bx = noise(float2(i.texcoord.x * 0.3 + _Time.y * _BodySpeed * 0.07, i.texcoord.y * 0.3 + 7.7)) - 0.5;
                    float by = noise(float2(i.texcoord.x * 0.3 + 3.3, i.texcoord.y * 0.3 + _Time.y * _BodySpeed * 0.07)) - 0.5;
                    bodyUV += float2(bx, by) * _BodyJitter * _MainTex_TexelSize.xy * 8;
                }
                bodyUV += _BodyOffset.xy;

                half4 tex = tex2D(_MainTex, bodyUV);
                float centerAlpha = tex.a;

                // Outline 抖动：仅影响描边采样
                float2 outlineUV = i.texcoord;
                if (_OutlineJitter > 0.001)
                {
                    float jx = noise(float2(i.texcoord.x * 0.5 + _Time.y * _OutlineSpeed * 0.1, i.texcoord.y * 0.5)) - 0.5;
                    float jy = noise(float2(i.texcoord.x * 0.5, i.texcoord.y * 0.5 + _Time.y * _OutlineSpeed * 0.1)) - 0.5;
                    outlineUV += float2(jx, jy) * _OutlineJitter * _MainTex_TexelSize.xy * 10;
                }
                float neighborMax = SampleMaxAlpha(outlineUV, _OutlineWidth);

                half4 body = tex * i.color;
                half4 outline = _OutlineColor * i.color;
                outline.a = 1;

                // Noise mask: 控制描边断续
                float noiseVal = noise(i.texcoord * _NoiseScale);

                bool isBody = centerAlpha > 0.01;
                bool isOutline = !isBody && neighborMax > 0.01 && noiseVal < _OutlineDensity;

                half4 result;
                if (isBody)
                    result = body;
                else if (isOutline)
                    result = outline;
                else
                    result = half4(0, 0, 0, 0);

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                result.rgb *= result.a;
                return result;
            }
            ENDCG
        }
    }
}
