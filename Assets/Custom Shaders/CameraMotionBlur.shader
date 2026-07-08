Shader "Hidden/CameraMotionBlur"
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

    // Reconstruction matrices (set per-frame in C#)
    float4x4 _InvViewProj;       // inverse of the CURRENT frame view-projection
    float4x4 _PrevViewProj;      // the PREVIOUS frame view-projection

    float _BlurScale;            // overall strength multiplier
    float _MaxBlurRadius;        // clamp on per-pixel velocity, in UV units
    int   _SampleCount;          // taps along the velocity vector
    float _DepthThreshold;       // sky / far-plane cutoff

    float _VignetteStart;        // radius (0..1) where edge blur begins
    float _VignetteEnd;          // radius where edge blur is full
    float _VignettePower;        // falloff curve exponent
    float _CenterStrength;       // residual blur at screen center

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
        return rawDepth < _DepthThreshold;
        #else
        return rawDepth > 1.0 - _DepthThreshold;
        #endif
    }

    float2 VelocityUV(float2 uv, out bool valid)
    {
        float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        valid = !IsSky(rawDepth);

        float3 worldPos = WorldPosFromDepth(uv, rawDepth);

        float4 prevClip = mul(_PrevViewProj, float4(worldPos, 1.0));
        if (prevClip.w <= 0.0) { valid = false; return float2(0, 0); }
        float2 prevNdc = prevClip.xy / prevClip.w;
        float2 prevUv  = prevNdc * 0.5 + 0.5;
        #if UNITY_UV_STARTS_AT_TOP
        if (_MainTex_TexelSize.y < 0) prevUv.y = 1.0 - prevUv.y;
        #endif

        float2 vel = (uv - prevUv) * _BlurScale;

        float len = length(vel);
        if (len > _MaxBlurRadius) vel *= _MaxBlurRadius / len;
        return vel;
    }

    float VignetteMask(float2 uv)
    {
        float2 d = uv - 0.5;
        d.x *= _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1.0); // aspect-correct (circular)
        float r = length(d) / 0.5; // normalize so the top/bottom edge is ~1
        float v = saturate((r - _VignetteStart) / max(_VignetteEnd - _VignetteStart, 1e-4));
        v = pow(v, _VignettePower);
        return lerp(_CenterStrength, 1.0, v);
    }

    fixed4 frag(v2f i) : SV_Target
    {
        bool valid;
        float2 vel = VelocityUV(i.uv, valid);

        fixed4 col = tex2D(_MainTex, i.uv);

        float mask = VignetteMask(i.uv);
        vel *= mask;

        if (!valid || length(vel) < _MainTex_TexelSize.x)
            return col;

        float4 accum = col;
        float  weight = 1.0;

        [loop]
        for (int s = 1; s < _SampleCount; s++)
        {
            float t = (float)s / (float)(_SampleCount - 1) - 0.5;
            float2 offset = vel * t;

            float w = 1.0 - abs(t) * 2.0;
            accum  += tex2D(_MainTex, i.uv + offset) * w;
            weight += w;
        }

        return accum / weight;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            ENDCG
        }
    }
    Fallback Off
}
