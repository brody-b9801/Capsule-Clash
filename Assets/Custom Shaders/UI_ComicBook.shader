Shader "UI/ComicBook"
{
    // Drop this on any UI Image / RawImage material.
    // Effects are screen-space so dots stay the same physical size
    // as the PPE halftone on the scene camera — they look continuous.

    Properties
    {
        // ── Required by Unity UI ────────────────────────────────────────────
        _MainTex        ("Sprite Texture",  2D)     = "white" {}
        _Color          ("Tint",            Color)  = (1,1,1,1)
        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID",      Float)  = 0
        _StencilOp      ("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
        _ColorMask      ("Color Mask",      Float)  = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        // ── Halftone ────────────────────────────────────────────────────────
        [Header(Halftone)]
        _HalftoneScale    ("Scale (px)",       Range(4,  120)) = 30
        _HalftoneStrength ("Strength",         Range(0,  1))   = 0.6
        _HalftoneAngle    ("Angle (deg)",      Range(0,  90))  = 45
        _HalftoneContrast ("Dot Crispness",    Range(1,  12))  = 6
        _HalftoneGamma    ("Gamma (<1=hi, >1=shadow)", Range(0.1, 2)) = 1.0
        [Toggle] _HalftoneOn ("Enable",        Float)          = 1

        // ── Paper Grain ─────────────────────────────────────────────────────
        [Header(Paper Grain)]
        _PaperStrength  ("Strength",           Range(0, 0.4))  = 0.05
        _PaperScale     ("Scale",              Range(100, 3000)) = 1400
        _PaperTint      ("Tint",               Color)          = (1,1,1,1)
        [Toggle] _PaperOn ("Enable",           Float)          = 1

        // ── Ink Noise ───────────────────────────────────────────────────────
        [Header(Ink Noise)]
        _InkStrength    ("Strength",           Range(0, 0.5))  = 0.18
        _InkScale       ("Scale",              Range(10, 500)) = 120
        [Toggle] _InkOn  ("Enable",            Float)          = 1

        // ── Color Grading ───────────────────────────────────────────────────
        [Header(Color Grading)]
        _Contrast       ("Contrast",           Range(0, 4))    = 1.0
        _Saturation     ("Saturation",         Range(0, 4))    = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
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
            Name "UI_COMICBOOK"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            // ── Uniforms ───────────────────────────────────────────────────
            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;

            float _HalftoneScale;
            float _HalftoneStrength;
            float _HalftoneAngle;
            float _HalftoneContrast;
            float _HalftoneGamma;
            float _HalftoneOn;

            float  _PaperStrength;
            float  _PaperScale;
            float4 _PaperTint;
            float  _PaperOn;

            float _InkStrength;
            float _InkScale;
            float _InkOn;

            float _Contrast;
            float _Saturation;

            // Screen resolution — set from C# each frame so the UI grid
            // matches the PPE grid exactly.
            float2 _ScreenSize;

            // ── Vertex ─────────────────────────────────────────────────────
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex      : SV_POSITION;
                fixed4 color       : COLOR;
                float2 texcoord    : TEXCOORD0;
                float4 worldPos    : TEXCOORD1;    // for clip rect
                float2 screenPos   : TEXCOORD2;    // actual screen pixels
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos  = v.vertex;
                o.vertex    = UnityObjectToClipPos(v.vertex);
                o.texcoord  = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color     = v.color * _Color;

                // Screen pixel position (0,0 = bottom-left)
                float4 sp   = ComputeScreenPos(o.vertex);
                o.screenPos = sp.xy / sp.w * _ScreenSize;

                return o;
            }

            // ═══════════════════════════════════════════════════════════════
            //  SHARED UTILITIES  (identical to PPE shader)
            // ═══════════════════════════════════════════════════════════════

            float hash21(float2 p)
            {
                p = frac(p * float2(443.8975, 397.2973));
                p += dot(p, p.yx + 19.19);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float2 rot2(float2 p, float rad)
            {
                float s, c;
                sincos(rad, s, c);
                return float2(c*p.x - s*p.y, s*p.x + c*p.y);
            }

            float luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            // ═══════════════════════════════════════════════════════════════
            //  HALFTONE  — same AM formula as PPE, uses screen pixels
            // ═══════════════════════════════════════════════════════════════
            float halftone(float2 screenPx, float brightness)
            {
                float  rad  = _HalftoneAngle * (3.14159265 / 180.0);
                float2 rpx  = rot2(screenPx, rad);

                float2 grid = frac(rpx / _HalftoneScale) - 0.5;
                float  dist = length(grid);

                float gamma    = max(0.01, _HalftoneGamma);
                float coverage = pow(1.0 - brightness, gamma);
                float dotRadius = sqrt(coverage) * 0.47;

                float aaWidth = 0.5 / (_HalftoneContrast * _HalftoneScale * 0.5);
                float dotMask = 1.0 - smoothstep(dotRadius - aaWidth,
                                                 dotRadius + aaWidth, dist);

                return saturate(dotMask + step(dotRadius, aaWidth));
            }

            // ═══════════════════════════════════════════════════════════════
            //  PAPER GRAIN
            // ═══════════════════════════════════════════════════════════════
            float paperGrain(float2 uv)
            {
                float n  = vnoise(uv * _PaperScale) * 0.6;
                      n += vnoise(uv * _PaperScale * 2.1) * 0.3;
                      n += vnoise(uv * _PaperScale * 4.3) * 0.1;
                return n;
            }

            // ═══════════════════════════════════════════════════════════════
            //  INK NOISE
            // ═══════════════════════════════════════════════════════════════
            float inkNoise(float2 uv)
            {
                float n  = vnoise(uv * _InkScale) * 0.55;
                      n += vnoise(uv * _InkScale * 1.7 + float2(5.1, 3.3)) * 0.30;
                      n += vnoise(uv * _InkScale * 3.1 + float2(1.7, 8.9)) * 0.15;
                return 0.5 + (n - 0.5) * _InkStrength;
            }

            // ═══════════════════════════════════════════════════════════════
            //  COLOR GRADING
            // ═══════════════════════════════════════════════════════════════
            float3 applyContrast(float3 col, float contrast)
            {
                // Pivot around 0.5 in linear space
                return saturate((col - 0.5) * contrast + 0.5);
            }

            float3 applySaturation(float3 col, float saturation)
            {
                float grey = luma(col);
                return saturate(lerp(float3(grey, grey, grey), col, saturation));
            }

            // ═══════════════════════════════════════════════════════════════
            //  FRAGMENT
            // ═══════════════════════════════════════════════════════════════
            fixed4 frag(v2f i) : SV_Target
            {
                // Standard Unity UI sample
                half4 col = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // Don't process fully transparent pixels
                clip(col.a - 0.001);

                float3 rgb = col.rgb;
                float  brightness = luma(rgb);

                // ── Halftone ───────────────────────────────────────────────
                if (_HalftoneOn > 0.5 && _HalftoneStrength > 0.001)
                {
                    float ht    = halftone(i.screenPos, brightness);
                    rgb = lerp(rgb, rgb * ht, _HalftoneStrength);
                }

                // ── Ink noise ──────────────────────────────────────────────
                if (_InkOn > 0.5 && _InkStrength > 0.001)
                {
                    // Use normalised UV so noise tiles with texture, not screen
                    float2 uvNorm = i.texcoord;
                    float ink = inkNoise(uvNorm);
                    rgb *= ink;
                }

                // ── Paper grain ────────────────────────────────────────────
                if (_PaperOn > 0.5 && _PaperStrength > 0.001)
                {
                    float2 uvNorm = i.texcoord;
                    float  paper  = paperGrain(uvNorm);
                    rgb = rgb * lerp(1.0, paper, _PaperStrength);
                    rgb = lerp(rgb, rgb * _PaperTint.rgb, _PaperStrength * 0.35);
                }

                // ── Color Grading ──────────────────────────────────────────
                rgb = applyContrast(rgb, _Contrast);
                rgb = applySaturation(rgb, _Saturation);

                col.rgb = saturate(rgb);

                // ── Unity UI clipping ──────────────────────────────────────
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
