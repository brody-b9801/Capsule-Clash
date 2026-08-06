Shader "Hidden/RetroDither"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorAmount ("Color Amount", Float) = 4
        _Bias ("Dither Bias", Range(-1, 1)) = -0.25
        _Curve ("Curve Distortion", Range(0, 0.5)) = 0
        _ScanlineSpeed ("Scanline Speed", Range(0, 10000)) = 50
        _ShakeIntensity ("Shake Intensity", Range(0, 10)) = 0.5
        _ShakeFrequency ("Shake Frequency", Range(0, 500)) = 50
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
        _MinHW ("Min Half-Width", Float) = 0.1
        _MaxHW ("Max Half-Width", Float) = 0.5

        // Set from RetroDither.cs each frame; declared here so the material
        // inspector reflects the full uniform set.
        _Resolution ("Source Resolution", Vector) = (1920, 1080, 0, 0)
        _DitherRes ("Dither Buffer Resolution", Vector) = (1920, 1080, 0, 0)
        _ContentRes ("Content Buffer Resolution", Vector) = (1920, 1080, 0, 0)
        _CellSize ("Dither Cell Size", Float) = 1
        _BlurDir ("Blur Direction", Vector) = (1, 0, 0, 0)
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
    float _MinHW;
    float _MaxHW;
    
    v2f vert(appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    // 8x8 Bayer threshold via bit interleaving. Equivalent to the classic
    // 64-entry lookup table but stays in registers instead of spilling the
    // array to scratch memory under a dynamic index.
    float bayer8(int x, int y)
    {
        int xc = x ^ y;

        // Interleave: bit pairs from (x^y) and y, most significant first.
        int v = ((y >> 2) & 1)
          | (((xc >> 2) & 1) << 1)
          | (((y  >> 1) & 1) << 2)
          | (((xc >> 1) & 1) << 3)
          | ((  y       & 1) << 4)
          | (( xc       & 1) << 5);

        return v / 64.0;
    }

    float bayerThreshold(float2 cellPx)
    {
        int x = (int)fmod(cellPx.x, 8.0);
        int y = (int)fmod(cellPx.y, 8.0);
        return bayer8(x, y);
    }

    // Barrel distortion shared by the image sample, the scanlines, the
    // subpixel mask and the edge vignette so they all warp together.
    float2 curveUV(float2 uv, float curveAmount)
    {
        float2 c = uv * 2.0 - 1.0;
        float2 offset = c.yx * curveAmount;
        c += c * offset * offset;
        return c * 0.5 + 0.5;
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

            float3 posterize3(float3 x, float3 w, float levels)
            {
                float3 v = saturate(x) * levels;
                float3 f = frac(v);
                float3 e = smoothstep(0.5 - w, 0.5 + w, f);
                return (floor(v) + e) / levels;
            }

            float3 quantize(float2 cellPx, float3 color)
            {
                float levels = max(1.0, floor(_ColorAmount + 0.5) - 1.0);

                // One posterization step is 1/levels wide. Scaling the bias by
                // levels keeps the dither strength constant in step-units, so
                // _Bias behaves the same at any _ColorAmount.
                float threshold = bayerThreshold(cellPx) - 0.5;
                color += threshold * _Bias / levels;

                // fwidth is meaningless here: this pass runs at dither
                // resolution where adjacent fragments are unrelated content,
                // so the 2x2 quad derivative is noise. Use a fixed transition
                // width in level-units instead.
                float3 w = (float3)clamp(_MinHW, 0.0, _MaxHW);

                return posterize3(color, w, levels);
            }

            fixed4 fragDither(v2f i) : SV_Target
            {
                float2 cellPx = floor(i.uv * _ContentRes);

                float4 col;
                col.rgb = tex2D(_MainTex, i.uv).rgb;
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
                float aspect = res.x / max(1.0, res.y);

                // Computed once and reused by the image sample, scanlines,
                // subpixel mask and vignette.
                float2 sampleUV = curveUV(uv, _Curve);

                // Advance the noise coordinate with time rather than scaling
                // it: multiplying by sin() drives the sample point through
                // zero twice per cycle, collapsing every row onto the same
                // noise value and killing the per-row jitter.
                float rowV = (floor(uv.y * res.y / cell) + 0.5) * cell / res.y;
                float shake = (noise(float2(rowV * _ShakeFrequency, _Time.y * 40.0)) - 0.5) * 0.0025;

                float2 imagePx = sampleUV * res;
                imagePx.x += shake * _ShakeIntensity * res.x;
                float2 shakenUV = imagePx / res;
                float2 dUV = (floor(imagePx / cell) + 0.5) / _DitherRes;

                // Chromatic aberration belongs here, on the full-res present
                // pass, not baked into the pixelated buffer before
                // quantization. Radial and aspect-corrected so it grows
                // toward the edges like a real lens.
                float2 fromCenter = shakenUV * 2.0 - 1.0;
                float2 caOffset = float2(fromCenter.x * aspect, fromCenter.y) * _ChromaticAberration;

                float4 col;
                col.r = tex2D(_MainTex, dUV + caOffset).r;
                col.g = tex2D(_MainTex, dUV).g;
                col.b = tex2D(_MainTex, dUV - caOffset).b;
                col.a = 1.0;

                // Scanlines ride the curved surface and scroll with
                // _ScanlineSpeed, so they stay attached to the tube instead
                // of floating flat over a warped image.
                // Normalizing by _RefHeight keeps the scanline pitch fixed in
                // reference-pixel terms, so the count tracks resolution
                // instead of aliasing against the pixel grid.
                float scanV = sampleUV.y * (res.y / max(1.0, _RefHeight));
                // _ScanlineSpeed is authored in the same units as the old
                // (unused) value; 0.001 turns it into a slow vertical roll.
                float banding = abs(sin(scanV * _ScanlineFrequency - _Time.y * _ScanlineSpeed * 0.001));
                float effect = lerp(1.0, banding, _ScanlineDarkness);

                col.rgb *= effect;

                // Sample bloom with the shaken UV so the glow stays locked to
                // the objects producing it during a shot shake.
                float3 bloom = tex2D(_BloomTex, shakenUV).rgb;
                col.rgb += bloom * (_BloomStrength + _GlowStrength);

                if (_SubpixelEnabled > 0.5)
                {
                    // Phosphor stripes follow the curved glass, matching the
                    // scanlines and vignette.
                    float subX = (sampleUV.x * res.x) / max(1.0, _SubpixelMaskSize) * 3.0;
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

                float2 edge = smoothstep(0., 0.02, sampleUV)*(1.-smoothstep(1.-0.02, 1., sampleUV));
                col.rgb *= edge.x * edge.y;

                // Bloom addition and up-to-4x subpixel brightness can push
                // past 1; clamp so an HDR camera target does not blow out.
                col.rgb = saturate(col.rgb);

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
                // _MainTex_TexelSize tracks whatever Blit bound as the source,
                // so the ping-pong stays correct even if the two bloom RTs
                // ever differ in size.
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
