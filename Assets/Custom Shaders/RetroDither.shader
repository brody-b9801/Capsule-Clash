Shader "Hidden/NewImageEffectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorAmount ("Color Amount", Float) = 4
        _Bias ("Dither Bias", Range(-1, 1)) = -0.25
        _Curve ("Curve Distortion", Range(0, 0.5)) = 0
        _ScanlineSpeed ("Scanline Speed", Range(0, 500)) = 100
        _ShakeIntensity ("Shake Intensity", Range(0, 1)) = 0.5
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.02)) = 0.002
        _ScanlineFrequency ("Scanline Frequency", Range(0, 10000)) = 2150
        _ScanlineDarkness ("Scanline Darkness", Range(0, 1)) = 1
        _RefHeight ("Reference Height", Float) = 1080
        _BloomTex ("Bloom", 2D) = "black" {}
        _BloomThreshold ("Bloom Threshold", Range(0, 1)) = 0.7
        _BloomKnee ("Bloom Knee", Range(0.01, 1)) = 0.3
        _BloomStrength ("Bloom Strength", Range(0, 3)) = 0.3
        _GlowStrength ("Phosphor Glow Strength", Range(0, 3)) = 0.2
        [Toggle] _SubpixelEnabled ("Enable RGB Subpixel", Float) = 1
        _SubpixelStrength ("Subpixel Strength", Range(0, 1)) = 0.4
        _SubpixelMaskSize ("Subpixel Mask Size", Range(1, 16)) = 3
        _SubpixelBorder ("Subpixel Border", Range(0, 1)) = 0.5
        _SubpixelBrightness ("Subpixel Brightness", Range(1, 4)) = 3
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;
    float _ColorAmount;
    float _Bias;
    float _Curve;
    float _RefHeight;
    float2 _Resolution;
    float2 _DitherRes;
    float2 _ContentRes;
    float _CellSize;
    float _ScanlineSpeed;
    float _ShakeIntensity;
    float _ChromaticAberration;
    float _ShakeFrequency;
    float _ScanlineFrequency;
    float _ScanlineDarkness;
    sampler2D _BloomTex;
    float _BloomThreshold;
    float _BloomKnee;
    float _BloomStrength;
    float _GlowStrength;
    float2 _BlurDir;
    float _SubpixelEnabled;
    float _SubpixelStrength;
    float _SubpixelMaskSize;
    float _SubpixelBorder;
    float _SubpixelBrightness;

    v2f vert(appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    float bayer8(int x, int y)
    {
        float m[64] = {
             0,32, 8,40, 2,34,10,42,
            48,16,56,24,50,18,58,26,
            12,44, 4,36,14,46, 6,38,
            60,28,52,20,62,30,54,22,
             3,35,11,43, 1,33, 9,41,
            51,19,59,27,49,17,57,25,
            15,47, 7,39,13,45, 5,37,
            63,31,55,23,61,29,53,21
        };
        return m[y * 8 + x] / 64.0;
    }

    float bayerThreshold(float2 cellPx)
    {
        int x = (int)fmod(cellPx.x, 8.0);
        int y = (int)fmod(cellPx.y, 8.0);
        return bayer8(x, y);
    }

    float random(float2 c)
    {
        return frac(sin(dot(c.xy, float2(12.9898, 78.233))) * 43758.5453);
    }

    float noise(in float2 st)
    {
        float2 i = floor(st);
        float2 f = frac(st);

        float a = random(i);
        float b = random(i + float2(1.0, 0.0));
        float c = random(i + float2(0.0, 1.0));
        float d = random(i + float2(1.0, 1.0));

        float2 u = f*f*(3.0-2.0*f);

        return lerp(a, b, u.x) +
        (c - a)* u.y * (1.0 - u.x) +
        (d - b) * u.x * u.y;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDither

            float3 quantize(float2 cellPx, float3 color)
            {
                float threshold = bayerThreshold(cellPx);
                color += threshold * _Bias;

                float levels = max(1.0, floor(_ColorAmount + 0.5) - 1.0);
                color.r = floor(saturate(color.r) * levels + 0.5) / levels;
                color.g = floor(saturate(color.g) * levels + 0.5) / levels;
                color.b = floor(saturate(color.b) * levels + 0.5) / levels;
                return color;
            }

            fixed4 fragDither(v2f i) : SV_Target
            {
                float2 cellPx = floor(i.uv * _ContentRes);
                float2 caOffset = float2(_ChromaticAberration, 0.0);

                float4 col;
                col.r = tex2D(_MainTex, i.uv + caOffset).r;
                col.g = tex2D(_MainTex, i.uv).g;
                col.b = tex2D(_MainTex, i.uv - caOffset).b;
                col.a = 1.0;

                col.rgb = quantize(cellPx, col.rgb);
                return col;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragPresent

            fixed4 fragPresent(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 res = _Resolution;
                float cell = max(1.0, _CellSize);

                float2 curved = uv * 2.0 - 1.0;
                float2 curveOffset = curved.yx * _Curve;
                curved += curved * curveOffset * curveOffset;
                float2 sampleUV = curved * 0.5 + 0.5;

                float rowV = (floor(uv.y * res.y / cell) + 0.5) * cell / res.y;
                float shake = (noise(float2(rowV, rowV) * sin(_Time.y * 400.0) * _ShakeFrequency) - 0.5) * 0.0025;

                float2 imagePx = sampleUV * res;
                imagePx.x += shake * _ShakeIntensity * res.x;
                float2 dUV = (floor(imagePx / cell) + 0.5) / _DitherRes;

                float4 col = tex2D(_MainTex, dUV);
                col.a = 1.0;

                float rowIdx = floor(uv.y * res.y);
                float periodRows = max(2.0, floor(6.28318 * res.y / max(_ScanlineFrequency, 1.0) + 0.5));
                float lines = sin(rowIdx / periodRows * 6.28318 + _Time.y * _ScanlineSpeed);
                col *= lines * _ScanlineDarkness + (1.0 - _ScanlineDarkness);

                float3 bloom = tex2D(_BloomTex, sampleUV).rgb;
                col.rgb += bloom * (_BloomStrength + _GlowStrength);

                if (_SubpixelEnabled > 0.5)
                {
                    float subX = (uv.x * res.x) / max(1.0, _SubpixelMaskSize) * 3.0;
                    float idx = fmod(subX, 3.0);
                    float3 mask = float3(
                        step(idx, 1.0),
                        step(1.0, idx) * step(idx, 2.0),
                        step(2.0, idx)) * _SubpixelBrightness;
                    float cellUV = frac(subX) * 2.0 - 1.0;
                    float border = 1.0 - cellUV * cellUV * _SubpixelBorder;
                    mask *= border;
                    mask = lerp(float3(1.0, 1.0, 1.0), mask, _SubpixelStrength);
                    col.rgb *= mask;
                }

                float2 edgeCurved = uv * 2.0 - 1.0;
                float2 edgeOffset = edgeCurved.yx * _Curve;
                edgeCurved += edgeCurved * edgeOffset * edgeOffset;
                float2 edgeUV = edgeCurved * 0.5 + 0.5;
                float2 edge = smoothstep(0., 0.02, edgeUV)*(1.-smoothstep(1.-0.02, 1., edgeUV));
                col.rgb *= edge.x * edge.y;

                return col;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBright

            fixed4 fragBright(v2f i) : SV_Target
            {
                float3 col = tex2D(_MainTex, i.uv).rgb;
                float l = dot(col, float3(0.299, 0.587, 0.114));
                col *= saturate((l - _BloomThreshold) / max(1e-4, _BloomKnee));
                return float4(col, 1.0);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlur

            fixed4 fragBlur(v2f i) : SV_Target
            {
                float2 step = _BlurDir * _MainTex_TexelSize.xy;
                float3 col = tex2D(_MainTex, i.uv).rgb * 0.227027;
                col += tex2D(_MainTex, i.uv + step * 1.384615).rgb * 0.316216;
                col += tex2D(_MainTex, i.uv - step * 1.384615).rgb * 0.316216;
                col += tex2D(_MainTex, i.uv + step * 3.230769).rgb * 0.070270;
                col += tex2D(_MainTex, i.uv - step * 3.230769).rgb * 0.070270;
                return float4(col, 1.0);
            }
            ENDCG
        }
    }
}
