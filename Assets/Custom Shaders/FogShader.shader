Shader "Hidden/FogShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)
        _FogStart ("Fog Start", Float) = 0
        _FogEnd ("Fog End", Float) = 100
        _FogDensity ("Fog Density", Range(0, 1)) = 0
        _AffectSkybox ("Affect Skybox", int) = 0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float _FogStart;
            float _FogEnd;
            float _FogDensity;
            float4 _FogColor;
            int _AffectSkybox;

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                
                #if UNITY_REVERSED_Z
                    if (raw <= 0.0 && !_AffectSkybox)
                        return col;
                #else
                    if (raw >= 1.0 && !_AffectSkybox)
                        return col;
                #endif
                return lerp(col, _FogColor, min((max(LinearEyeDepth(raw) - _FogStart, 0) / max(_FogEnd - _FogStart, 1e-4)) * _FogDensity, _FogDensity));
            }
            ENDCG
        }
    }
}
