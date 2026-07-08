Shader "Hidden/ComicBookPost"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _CameraDepthTexture;
    float4    _MainTex_TexelSize;

    float4x4 _InvViewProj;
    float3   _CameraForward;
    float    _DebugMode;

    // Posterization
    float  _Posterize;
    float  _ColorSaturation;
    float  _ColorContrast;
    float  _PosterizeAffectSky;
    float4 _ColorTint;
    float  _TintStrength;

    // Pixelation
    float _Pixelate;
    float _PixelSize;

    // Dithering
    float     _DitherStrength;
    float     _DitherSpread;
    float     _DitherPattern;        // 0=Bayer4 1=Bayer8 2=hash 3=grain 4=texture
    float3    _DitherChannelWeights;
    float     _DitherShadowBias;
    float     _DitherHighlightBias;
    float     _DitherAnimSpeed;
    float     _DitherWorldSpace;
    float     _DitherWorldScale;
    sampler2D _DitherTex;
    float2    _DitherTexelSize;

    // Color quantization / palette
    float     _QuantColorNum;        // number of quantization steps (2..32)
    float     _QuantMode;            // 0=off 1=luminance 2=RGB 3=hue-lightness 4=palette-texture
    sampler2D _PaletteTex;           // 1-D strip: sampled by quantized luminance (mode 4)
    float     _PaletteTexWidth;      // texel count in palette strip
    // Hue-lightness palette — up to 8 colours stored as float4 pairs (rgb + padding)
    float4    _HLPalette[8];
    float     _HLPaletteCount;       // how many entries are used

    // Crosshatch
    float  _CrosshatchStrength;
    float  _CrosshatchScale;
    float  _CrosshatchThresh;
    float  _CrosshatchThickness;
    float4 _CrosshatchColor;
    float  _CrosshatchAffectSky;
    float  _CrosshatchFamilies;
    float  _CrosshatchDash;
    float  _CrosshatchDashGrazing;

    // RGB Cell / CRT shadow mask
    float  _CRTStrength;             // 0=off, 1=full
    float  _CRTPixelSize;            // size of one CRT "cell" in screen pixels
    float  _CRTMaskBorder;           // sharpness of subcell border (default ~3)
    float  _CRTMaskIntensity;        // blend strength of mask onto image
    float  _CRTStagger;              // 1=staggered (aperture grill) 0=stripe mask

    // Scanlines
    float  _ScanlineStrength;        // 0=off
    float  _ScanlineFrequency;       // lines per screen height
    float  _ScanlineSpeed;           // scroll speed (0=static)

    // Screen curvature
    float  _CurveStrength;           // 0=flat, ~0.3=realistic CRT

    // Chromatic aberration
    float  _ChromaStrength;          // UV spread per channel (pixels)

    // ASCII
    sampler2D _AsciiAtlas;
    float4    _AsciiAtlas_TexelSize;
    float _AsciiStrength;
    float _AsciiCellSize;
    float4 _AsciiInkColor;
    float4 _AsciiBgColor;
    float  _AsciiColorMode;
    float  _AsciiAtlasCols;
    float  _AsciiAtlasRows;

    // ─────────────────────────────────────────────────────────────────────────
    //  Structs & vertex
    // ─────────────────────────────────────────────────────────────────────────

    struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
    struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv  = v.uv;
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0) o.uv.y = 1.0 - o.uv.y;
        #endif
        return o;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Utilities
    // ─────────────────────────────────────────────────────────────────────────

    float Lum(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

    float3 WorldPosFromDepth(float2 uv, float depth)
    {
        float4 ndc = float4(uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
        #if defined(UNITY_REVERSED_Z)
        ndc.z = 1.0 - depth;
        #endif
        float4 worldH = mul(_InvViewProj, ndc);
        return worldH.xyz / worldH.w;
    }

    bool IsSky(float rawDepth)
    {
        #if defined(UNITY_REVERSED_Z)
        return rawDepth < 0.0001;
        #else
        return rawDepth > 0.9999;
        #endif
    }

    float3 RGBtoHSL(float3 c)
    {
        float maxC = max(c.r, max(c.g, c.b));
        float minC = min(c.r, min(c.g, c.b));
        float d = maxC - minC;
        float l = (maxC + minC) * 0.5;
        float s = (d < 0.0001) ? 0.0 : d / (1.0 - abs(2.0 * l - 1.0));
        float h = 0.0;
        if (d > 0.0001)
        {
            if      (maxC == c.r) h = frac((c.g - c.b) / d / 6.0 + 1.0);
            else if (maxC == c.g) h = ((c.b - c.r) / d + 2.0) / 6.0;
            else                  h = ((c.r - c.g) / d + 4.0) / 6.0;
        }
        return float3(h, s, l);
    }

    float3 HSLtoRGB(float3 hsl)
    {
        float h = hsl.x, s = hsl.y, l = hsl.z;
        float c = (1.0 - abs(2.0 * l - 1.0)) * s;
        float x = c * (1.0 - abs(frac(h * 6.0) * 2.0 - 1.0 - 1.0 + 1.0));
        // simpler: use standard HUE helper
        float3 p = abs(frac(float3(h, h + 2.0/3.0, h + 1.0/3.0)) * 6.0 - 3.0) - 1.0;
        return (l + s * (saturate(p) - 0.5) * (1.0 - abs(2.0 * l - 1.0)));
    }

    float3 RGBtoHSV(float3 c)
    {
        float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
        float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
        float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
        float d = q.x - min(q.w, q.y), e = 1e-10;
        return float3(abs(q.z + (q.w - q.y) / (6.0*d + e)), d / (q.x + e), q.x);
    }

    float3 HSVtoRGB(float3 c)
    {
        float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
        float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
        return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Screen curvature — call first, remaps uv for everything downstream
    // ─────────────────────────────────────────────────────────────────────────

    float2 CurveUV(float2 uv)
    {
        float2 c = uv * 2.0 - 1.0;
        float2 offset = c.yx * _CurveStrength;
        c += c * offset * offset;
        return c * 0.5 + 0.5;
    }

    // Returns 0 at curved edges, 1 inside — use to black out border.
    float CurveMask(float2 curvedUV)
    {
        float2 e = smoothstep(0.0, 0.02, curvedUV) * (1.0 - smoothstep(0.98, 1.0, curvedUV));
        return e.x * e.y;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Chromatic aberration
    // ─────────────────────────────────────────────────────────────────────────

    float3 ChromaticAberration(float2 uv, float strength)
    {
        float2 spread = (strength / _ScreenParams.xy);
        float r = tex2D(_MainTex, uv + float2( spread.x,  0)).r;
        float g = tex2D(_MainTex, uv).g;
        float b = tex2D(_MainTex, uv + float2(-spread.x,  0)).b;
        return float3(r, g, b);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Pixelation
    // ─────────────────────────────────────────────────────────────────────────

    float2 PixelateUV(float2 uv)
    {
        if (_Pixelate < 0.5 || _PixelSize <= 1.0) return uv;
        float2 ps = _PixelSize / _ScreenParams.xy;
        return floor(uv / ps) * ps + ps * 0.5;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Posterization (4x4 Bayer for quantization noise only)
    // ─────────────────────────────────────────────────────────────────────────

    float Bayer4(float2 screenPx)
    {
        int x = (int)screenPx.x % 4;
        int y = (int)screenPx.y % 4;
        float b;
        int row = y * 4 + x;
        switch (row)
        {
            case  0: b =  0; break; case  1: b =  8; break;
            case  2: b =  2; break; case  3: b = 10; break;
            case  4: b = 12; break; case  5: b =  4; break;
            case  6: b = 14; break; case  7: b =  6; break;
            case  8: b =  3; break; case  9: b = 11; break;
            case 10: b =  1; break; case 11: b =  9; break;
            case 12: b = 15; break; case 13: b =  7; break;
            case 14: b = 13; break; default: b =  5; break;
        }
        return b / 16.0 - 0.5;
    }

    float3 Posterize(float3 col, float2 screenPx)
    {
        float lum = Lum(col);
        col = lerp(float3(lum, lum, lum), col, _ColorSaturation);
        col = saturate((col - 0.5) * _ColorContrast + 0.5);
        col += Bayer4(screenPx) / _Posterize;
        float vB = max(col.r, max(col.g, col.b));
        float vA = floor(vB * _Posterize + 0.5) / _Posterize;
        col = (vB > 0.001) ? col * (vA / vB) : float3(vA, vA, vA);
        col = lerp(col, col * _ColorTint.rgb, _TintStrength);
        return saturate(col);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Color quantization
    // ─────────────────────────────────────────────────────────────────────────

    float QuantStep(float v, float n)
    {
        return floor(v * (n - 1.0) + 0.5) / (n - 1.0);
    }

    // Hue-lightness quantization: find 2 nearest palette entries by hue,
    // dither between them based on lightness distance vs Bayer threshold.
    float3 HueLightnessQuant(float3 col, float2 screenPx)
    {
        int n = clamp((int)(_HLPaletteCount + 0.5), 2, 8);
        float3 hsl = RGBtoHSL(col);

        // Find two closest palette hues
        float bestDist0 = 1e9, bestDist1 = 1e9;
        int   bestIdx0  = 0,   bestIdx1  = 1;
        for (int i = 0; i < n; i++)
        {
            float ph = _HLPalette[i].x; // hue stored in .x
            float d  = abs(hsl.x - ph);
            d = min(d, 1.0 - d); // wrap hue distance
            if (d < bestDist0) { bestDist1 = bestDist0; bestIdx1 = bestIdx0; bestDist0 = d; bestIdx0 = i; }
            else if (d < bestDist1) { bestDist1 = d; bestIdx1 = i; }
        }

        float3 pal0 = _HLPalette[bestIdx0].rgb;
        float3 pal1 = _HLPalette[bestIdx1].rgb;

        // Distance between the two hue-neighbours normalised to [0,1]
        float totalHueDist = bestDist0 + bestDist1;
        float t = (totalHueDist > 0.0001) ? bestDist0 / totalHueDist : 0.0;

        // Use Bayer threshold to dither between the two colours
        float threshold = Bayer4(screenPx) + 0.5; // remap to [0,1]
        float3 chosen = (t < threshold) ? pal0 : pal1;

        // Now dither the lightness within the chosen palette colour
        float3 chosenHSL = RGBtoHSL(chosen);
        float lDist = hsl.z - chosenHSL.z;
        float lThresh = Bayer4(screenPx + float2(4, 0)) + 0.5;
        chosenHSL.z = chosenHSL.z + (lDist > lThresh * 0.15 ? 0.15 : 0.0);

        return HSLtoRGB(chosenHSL);
    }

    float3 ColorQuantize(float3 col, float2 screenPx)
    {
        int mode = (int)(_QuantMode + 0.5);
        if (mode == 0) return col;

        float n = max(_QuantColorNum, 2.0);
        float threshold = Bayer4(screenPx) * _DitherSpread * 0.5;

        if (mode == 1)
        {
            // Luminance-only quantization → grayscale
            float lum = saturate(Lum(col) + threshold);
            float q = QuantStep(lum, n);
            return float3(q, q, q);
        }
        if (mode == 2)
        {
            // Per-channel RGB quantization
            float3 c = saturate(col + threshold);
            return float3(QuantStep(c.r, n), QuantStep(c.g, n), QuantStep(c.b, n));
        }
        if (mode == 3)
        {
            return HueLightnessQuant(col, screenPx);
        }
        if (mode == 4)
        {
            // Palette-texture: quantize luminance then sample palette strip
            float lum = saturate(Lum(col) + threshold);
            float q = QuantStep(lum, n);
            float2 puv = float2(q, 0.5);
            return tex2D(_PaletteTex, puv).rgb;
        }
        return col;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dither pattern samplers
    // ─────────────────────────────────────────────────────────────────────────

    float Bayer4Sample(float2 px)
    {
        int x = (int)px.x % 4;
        int y = (int)px.y % 4;
        float b;
        int row = y * 4 + x;
        switch (row)
        {
            case  0: b =  0; break; case  1: b =  8; break;
            case  2: b =  2; break; case  3: b = 10; break;
            case  4: b = 12; break; case  5: b =  4; break;
            case  6: b = 14; break; case  7: b =  6; break;
            case  8: b =  3; break; case  9: b = 11; break;
            case 10: b =  1; break; case 11: b =  9; break;
            case 12: b = 15; break; case 13: b =  7; break;
            case 14: b = 13; break; default: b =  5; break;
        }
        return b / 16.0 - 0.5;
    }

    float Bayer8Sample(float2 px)
    {
        int x = (int)px.x % 8;
        int y = (int)px.y % 8;
        int idx = y * 8 + x;
        float b;
        switch (idx)
        {
            case  0: b =  0; break; case  1: b = 32; break; case  2: b =  8; break; case  3: b = 40; break;
            case  4: b =  2; break; case  5: b = 34; break; case  6: b = 10; break; case  7: b = 42; break;
            case  8: b = 48; break; case  9: b = 16; break; case 10: b = 56; break; case 11: b = 24; break;
            case 12: b = 50; break; case 13: b = 18; break; case 14: b = 58; break; case 15: b = 26; break;
            case 16: b = 12; break; case 17: b = 44; break; case 18: b =  4; break; case 19: b = 36; break;
            case 20: b = 14; break; case 21: b = 46; break; case 22: b =  6; break; case 23: b = 38; break;
            case 24: b = 60; break; case 25: b = 28; break; case 26: b = 52; break; case 27: b = 20; break;
            case 28: b = 62; break; case 29: b = 30; break; case 30: b = 54; break; case 31: b = 22; break;
            case 32: b =  3; break; case 33: b = 35; break; case 34: b = 11; break; case 35: b = 43; break;
            case 36: b =  1; break; case 37: b = 33; break; case 38: b =  9; break; case 39: b = 41; break;
            case 40: b = 51; break; case 41: b = 19; break; case 42: b = 59; break; case 43: b = 27; break;
            case 44: b = 49; break; case 45: b = 17; break; case 46: b = 57; break; case 47: b = 25; break;
            case 48: b = 15; break; case 49: b = 47; break; case 50: b =  7; break; case 51: b = 39; break;
            case 52: b = 13; break; case 53: b = 45; break; case 54: b =  5; break; case 55: b = 37; break;
            case 56: b = 63; break; case 57: b = 31; break; case 58: b = 55; break; case 59: b = 23; break;
            case 60: b = 61; break; case 61: b = 29; break; case 62: b = 53; break; default: b = 21; break;
        }
        return b / 64.0 - 0.5;
    }

    float HashSample(float2 px)
    {
        float2 p = floor(px);
        return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453) - 0.5;
    }

    float GrainSample(float2 px, float speed)
    {
        float t = floor(_Time.y * speed * 60.0);
        float2 p = floor(px) + float2(t * 13.7, t * 7.3);
        return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453) - 0.5;
    }

    float TexDitherSample(float2 px)
    {
        float2 ts = max(_DitherTexelSize, float2(1, 1));
        float2 uv = floor(px / ts) * ts / _ScreenParams.xy;
        return tex2Dlod(_DitherTex, float4(uv, 0, 0)).r - 0.5;
    }

    float DitherSample(float2 px, float speed)
    {
        int pat = (int)(_DitherPattern + 0.5);
        if (pat == 0) return Bayer4Sample(px);
        if (pat == 2) return HashSample(px);
        if (pat == 3) return GrainSample(px, max(speed, 0.001));
        if (pat == 4) return TexDitherSample(px);
        return Bayer8Sample(px);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Artistic dithering
    // ─────────────────────────────────────────────────────────────────────────

    float3 ArtisticDither(float3 col, float2 screenPx, float3 worldPos, bool sky)
    {
        if (_DitherStrength <= 0.0) return col;

        float2 ditherPx = screenPx;
        if (_DitherWorldSpace > 0.5 && !sky)
            ditherPx = floor(worldPos.xz * _DitherWorldScale);

        float raw = DitherSample(ditherPx, _DitherAnimSpeed) * _DitherSpread;

        float lum = Lum(col);
        float shadowMask    = pow(saturate(1.0 - lum), max(_DitherShadowBias    * 8.0, 0.001));
        float highlightMask = pow(saturate(lum),        max(_DitherHighlightBias * 8.0, 0.001));
        float tonalWeight   = saturate(shadowMask + highlightMask);
        float biasSum = _DitherShadowBias + _DitherHighlightBias;
        tonalWeight = lerp(1.0, tonalWeight, saturate(biasSum * 4.0));

        float t = raw * tonalWeight * _DitherStrength;
        float3 weights = max(_DitherChannelWeights, 0.0);
        return saturate(col + float3(t, t, t) * weights);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RGB Cell / CRT shadow mask
    // ─────────────────────────────────────────────────────────────────────────

    float3 CRTMask(float2 uv, float3 col)
    {
        if (_CRTStrength <= 0.0) return col;

        float2 pixel   = uv * _ScreenParams.xy;
        float2 coord   = pixel / max(_CRTPixelSize, 1.0);
        float2 subcoord = coord * float2(3.0, 1.0);

        // Stagger every other column by half a cell height (aperture grill)
        float staggerOn = step(0.5, _CRTStagger);
        float2 cellOffset = float2(0.0, staggerOn * (frac(floor(coord.x) * 0.5) > 0.25 ? 0.5 : 0.0));

        // Which subcell (R=0, G=1, B=2) does this pixel belong to?
        float ind = fmod(floor(subcoord.x), 3.0);
        float3 maskColor = float3(ind < 0.5, ind > 0.5 && ind < 1.5, ind > 1.5) * 2.0;

        // Border softening within each subcell
        float2 cellUV  = frac(subcoord + cellOffset) * 2.0 - 1.0;
        float2 border  = 1.0 - cellUV * cellUV * _CRTMaskBorder;
        maskColor *= saturate(border.x) * saturate(border.y);

        // Sample the scene using per-subcell UV (locks colour to cell centre)
        float2 rgbCellUV = floor(coord + cellOffset) * _CRTPixelSize / _ScreenParams.xy;
        rgbCellUV = clamp(rgbCellUV, _MainTex_TexelSize.xy, 1.0 - _MainTex_TexelSize.xy);
        float3 cellCol = tex2D(_MainTex, rgbCellUV).rgb;

        float3 masked = cellCol * (1.0 + (maskColor - 1.0) * _CRTMaskIntensity);
        return lerp(col, masked, _CRTStrength);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Scanlines
    // ─────────────────────────────────────────────────────────────────────────

    float3 Scanlines(float2 uv, float3 col)
    {
        if (_ScanlineStrength <= 0.0) return col;
        float scanline = sin((uv.y * _ScanlineFrequency + _Time.y * _ScanlineSpeed) * 3.14159265 * 2.0);
        col *= 1.0 - _ScanlineStrength * (1.0 - saturate(scanline + 1.0)) * 0.5;
        return col;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Crosshatch
    // ─────────────────────────────────────────────────────────────────────────

    float3 DepthNormal(float2 uv)
    {
        float2 d = _MainTex_TexelSize.xy * 1.5;
        float dr = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2( d.x, 0));
        float dl = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(-d.x, 0));
        float dt = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(0,  d.y));
        float db = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv + float2(0, -d.y));
        float3 wr = WorldPosFromDepth(uv + float2( d.x, 0), dr);
        float3 wl = WorldPosFromDepth(uv + float2(-d.x, 0), dl);
        float3 wt = WorldPosFromDepth(uv + float2(0,  d.y), dt);
        float3 wb = WorldPosFromDepth(uv + float2(0, -d.y), db);
        return normalize(cross(wr - wl, wt - wb));
    }

    float HatchLine(float2 px, float freq, float dir, float dashFreq, float dashPhase)
    {
        float a = frac((px.x * dir + px.y) * freq);
        a = min(a, 1.0 - a);
        float lw = _CrosshatchThickness * 0.02 / max(freq, 0.0001);
        float sw = max(lw * 0.25, 0.5);
        float hatch = smoothstep(lw + sw, lw - sw, a);

        float dashCoord = (px.x * (-dir) + px.y * dir + dashPhase) * dashFreq;
        float dash      = frac(dashCoord);
        float gapWidth  = 0.08;
        float dashMask  = smoothstep(gapWidth, gapWidth * 2.0, dash) *
                          smoothstep(1.0 - gapWidth, 1.0 - gapWidth * 2.0, dash);
        hatch *= lerp(1.0, dashMask, step(0.0001, dashFreq));
        return hatch;
    }

    float CrosshatchLines(float2 screenUV, float lum, float grazingFactor)
    {
        float inShadow = step(lum, _CrosshatchThresh);
        float freq     = _CrosshatchScale / _ScreenParams.x;
        float2 px      = screenUV * _ScreenParams.xy;
        float dashFreq = _CrosshatchDash * (1.0 + grazingFactor * _CrosshatchDashGrazing * 4.0);
        float h1 = HatchLine(px, freq,  1.0, dashFreq, 0.0);
        float h2 = HatchLine(px, freq, -1.0, dashFreq, 0.3);
        float twoFam = step(1.5, _CrosshatchFamilies);
        return max(h1, h2 * twoFam) * inShadow;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ASCII
    // ─────────────────────────────────────────────────────────────────────────

    float SampleAtlasChar(int charCode, float2 sub)
    {
        int idx  = clamp(charCode - 32, 0, 94);
        int col  = idx % 16;
        int row  = idx / 16;
        float totalRows = _AsciiAtlasRows;
        float2 cellSize = float2(1.0 / _AsciiAtlasCols, 1.0 / totalRows);
        float2 atlasUV  = float2(col + (1.0 - sub.x), (totalRows - 1.0 - row) + sub.y) * cellSize;
        return tex2D(_AsciiAtlas, atlasUV).r;
    }

    int DensityChar(float cellLum)
    {
        if (cellLum > 0.97) return 32;
        int idx = clamp((int)((1.0 - cellLum) * 19.99), 0, 19);
        switch (idx)
        {
            case  0: return 77;  case  1: return 87;  case  2: return 56;
            case  3: return 66;  case  4: return 64;  case  5: return 35;
            case  6: return 37;  case  7: return 38;  case  8: return 120;
            case  9: return 111; case 10: return 116; case 11: return 105;
            case 12: return 43;  case 13: return 45;  case 14: return 58;
            case 15: return 44;  case 16: return 39;  case 17: return 46;
            case 18: return 96;  default: return 32;
        }
    }

    int EdgeChar(float2 cellCenterUV, float cellSize)
    {
        float2 s = _MainTex_TexelSize.xy * cellSize;
        float tl = Lum(tex2D(_MainTex, cellCenterUV + float2(-s.x,  s.y)).rgb);
        float tc = Lum(tex2D(_MainTex, cellCenterUV + float2(    0,  s.y)).rgb);
        float tr = Lum(tex2D(_MainTex, cellCenterUV + float2( s.x,  s.y)).rgb);
        float ml = Lum(tex2D(_MainTex, cellCenterUV + float2(-s.x,     0)).rgb);
        float mr = Lum(tex2D(_MainTex, cellCenterUV + float2( s.x,     0)).rgb);
        float bl = Lum(tex2D(_MainTex, cellCenterUV + float2(-s.x, -s.y)).rgb);
        float bc = Lum(tex2D(_MainTex, cellCenterUV + float2(    0, -s.y)).rgb);
        float br = Lum(tex2D(_MainTex, cellCenterUV + float2( s.x, -s.y)).rgb);
        float gx = (tr + 2.0*mr + br) - (tl + 2.0*ml + bl);
        float gy = (tl + 2.0*tc + tr) - (bl + 2.0*bc + br);
        float mag = sqrt(gx*gx + gy*gy);
        if (mag < 1.5) return -1;
        float angle = atan2(gy, gx);
        float norm  = frac(angle / (2.0 * 3.14159265) + 1.0);
        int sector  = (int)(norm * 4.0 + 0.5) % 4;
        switch (sector)
        {
            case 0: return 45; case 1: return 92; case 2: return 124; default: return 47;
        }
    }

    float3 AsciiInk(float3 cellColor, float cellLum, int colorMode)
    {
        if (colorMode == 1) return cellColor;
        if (colorMode == 2)
        {
            float3 dark = float3(1.0, 0.1, 0.6);
            float3 mid  = float3(0.1, 0.9, 1.0);
            float3 lite = float3(1.0, 1.0, 1.0);
            return cellLum < 0.5 ? lerp(dark, mid, cellLum * 2.0)
                                 : lerp(mid, lite, (cellLum - 0.5) * 2.0);
        }
        if (colorMode == 3) return float3(0.0, cellLum * 0.85 + 0.15, 0.0);
        return _AsciiInkColor.rgb;
    }

    float3 AsciiFilter(float2 uv, float3 col, float lum, float2 screenPx)
    {
        float cell = max(_AsciiCellSize, 1.0);
        float2 px           = uv * _ScreenParams.xy;
        float2 cellOriginPx = floor(px / cell) * cell;
        float2 cellCenterPx = cellOriginPx + cell * 0.5;
        float2 halfTexel    = _MainTex_TexelSize.xy * 0.5;
        float2 cellCenterUV = clamp(cellCenterPx * _MainTex_TexelSize.xy, halfTexel, 1.0 - halfTexel);
        float2 sub          = (px - cellOriginPx) / cell;

        float2 ts = _MainTex_TexelSize.xy * max(cell * 0.33, 1.0);
        float3 cellColor =
            tex2D(_MainTex, cellCenterUV).rgb * 4.0 +
            tex2D(_MainTex, cellCenterUV + float2( ts.x,  0   )).rgb +
            tex2D(_MainTex, cellCenterUV + float2(-ts.x,  0   )).rgb +
            tex2D(_MainTex, cellCenterUV + float2( 0,     ts.y)).rgb +
            tex2D(_MainTex, cellCenterUV + float2( 0,    -ts.y)).rgb +
            tex2D(_MainTex, cellCenterUV + float2( ts.x,  ts.y)).rgb * 0.5 +
            tex2D(_MainTex, cellCenterUV + float2(-ts.x,  ts.y)).rgb * 0.5 +
            tex2D(_MainTex, cellCenterUV + float2( ts.x, -ts.y)).rgb * 0.5 +
            tex2D(_MainTex, cellCenterUV + float2(-ts.x, -ts.y)).rgb * 0.5;
        cellColor /= 10.0;
        float cellLum = floor(Lum(cellColor) * 20.0 + 0.502) / 20.0;

        int edgeC    = EdgeChar(cellCenterUV, cell);
        int charCode = (edgeC >= 0) ? edgeC : DensityChar(cellLum);
        float ink    = SampleAtlasChar(charCode, sub);

        int cmode = clamp((int)(_AsciiColorMode + 0.5), 0, 3);
        float3 inkCol = AsciiInk(cellColor, cellLum, cmode);
        float3 bg     = lerp(col, _AsciiBgColor.rgb, _AsciiBgColor.a);
        return lerp(col, lerp(bg, inkCol, ink), _AsciiStrength);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Main fragment
    // ─────────────────────────────────────────────────────────────────────────

    fixed4 frag(v2f i) : SV_Target
    {
        // ── Curvature ────────────────────────────────────────────────────────
        float2 curvedUV = (_CurveStrength > 0.0) ? CurveUV(i.uv) : i.uv;
        float  curveMask = (_CurveStrength > 0.0) ? CurveMask(curvedUV) : 1.0;

        float2 uv = PixelateUV(curvedUV);

        // ── Chromatic aberration (samples MainTex with offset channels) ──────
        float3 col;
        if (_ChromaStrength > 0.0)
            col = ChromaticAberration(uv, _ChromaStrength);
        else
            col = tex2D(_MainTex, uv).rgb;

        float4 src = tex2D(_MainTex, uv); // for alpha

        float rawDepth    = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        float linearDepth = LinearEyeDepth(rawDepth);
        bool  sky         = IsSky(rawDepth);

        float3 worldPos = float3(0, 0, 0);
        if (!sky) worldPos = WorldPosFromDepth(uv, rawDepth);

        int dbg = (int)(_DebugMode + 0.5);
        if (dbg == 1) return float4(frac(worldPos.xz * 0.1), 0, 1);
        if (dbg == 4) { float d = sky ? 1 : saturate(linearDepth * 0.02); return float4(d,d,d,1); }

        // ── Posterization ────────────────────────────────────────────────────
        if (!sky || _PosterizeAffectSky > 0.5)
            col = Posterize(col, i.pos.xy);

        float lum = Lum(col);
        if (dbg == 3) return float4(lum, lum, lum, 1);

        // ── Color quantization ───────────────────────────────────────────────
        col = ColorQuantize(col, i.pos.xy);
        lum = Lum(col);

        // ── Artistic dithering ───────────────────────────────────────────────
        col = ArtisticDither(col, i.pos.xy, worldPos, sky);
        lum = Lum(col);

        // ── CRT shadow mask ───────────────────────────────────────────────────
        col = CRTMask(uv, col);

        // ── Scanlines ────────────────────────────────────────────────────────
        col = Scanlines(uv, col);

        // ── Crosshatch ───────────────────────────────────────────────────────
        if (_CrosshatchStrength > 0.0 && (!sky || _CrosshatchAffectSky > 0.5))
        {
            float grazingFactor = 0.0;
            if (_CrosshatchDash > 0.0 && _CrosshatchDashGrazing > 0.0 && !sky)
            {
                float3 n = DepthNormal(uv);
                grazingFactor = 1.0 - abs(dot(n, _CameraForward));
            }
            float hatch = CrosshatchLines(uv, lum, grazingFactor);
            col = lerp(col, _CrosshatchColor.rgb, hatch * _CrosshatchStrength);
        }

        // ── ASCII ─────────────────────────────────────────────────────────────
        if (_AsciiStrength > 0.0)
            col = AsciiFilter(uv, col, lum, i.pos.xy);

        // ── Curve edge mask ───────────────────────────────────────────────────
        col *= curveMask;

        return float4(col, src.a);
    }

    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5
            ENDCG
        }
    }
}
