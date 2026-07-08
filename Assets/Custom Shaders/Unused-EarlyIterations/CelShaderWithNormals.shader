Shader "Custom/ToonCel_FullControl"
{
    Properties
    {
        [Header(Textures)]
        _Albedo ("Albedo (Optional)", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _HeightMap ("Height Map", 2D) = "black" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}

        [Header(Height)]
        _ParallaxStrength ("Height Strength", Range(0,0.1)) = 0.02

        [Header(Cel Shading)]
        _Bands ("Light Bands", Range(1,5)) = 3
        _BandSoftness ("Band Softness", Range(0,0.2)) = 0.02

        [Header(Light Level Colors)]
        _DarkColor ("Shadow Color", Color) = (0.05,0.05,0.05,1)
        _MidColor ("Mid Light Color", Color) = (0.6,0.6,0.6,1)
        _LightColor ("Light Color", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1.3,1.3,1.3,1)

        [Header(Control)]
        _GlobalTint ("Global Tint", Color) = (1,1,1,1)
        _AlbedoInfluence ("Albedo Influence", Range(0,1)) = 0
        _NormalStrength ("Normal Strength", Range(0,2)) = 1
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="ForwardBase" }
        LOD 300

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _Albedo;
            sampler2D _NormalMap;
            sampler2D _HeightMap;
            sampler2D _OcclusionMap;

            float _ParallaxStrength;
            float _Bands;
            float _BandSoftness;

            float4 _DarkColor;
            float4 _MidColor;
            float4 _LightColor;
            float4 _HighlightColor;
            float4 _GlobalTint;

            float _AlbedoInfluence;
            float _NormalStrength;
            float _OcclusionStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 lightDir : TEXCOORD2;
                float3x3 tbn : TEXCOORD3;
                SHADOW_COORDS(6)
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;

                o.tbn = float3x3(worldTangent, worldBinormal, worldNormal);

                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.lightDir = normalize(_WorldSpaceLightPos0.xyz);

                TRANSFER_SHADOW(o);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Parallax Mapping
                float height = tex2D(_HeightMap, i.uv).r;
                float2 parallaxOffset = normalize(i.viewDir.xy) * (height - 0.5) * _ParallaxStrength;
                float2 uv = i.uv + parallaxOffset;

                // Normal Map
                float3 normalTS = UnpackNormal(tex2D(_NormalMap, uv));
                normalTS.xy *= _NormalStrength;
                normalTS = normalize(normalTS);
                float3 normalWS = normalize(mul(i.tbn, normalTS));

                // Lighting
                float NdotL = saturate(dot(normalWS, i.lightDir));
                float shadow = SHADOW_ATTENUATION(i);

                float lightValue = NdotL * shadow;

                // Cel Bands
                float bandStep = 1.0 / max(1,_Bands);
                float band = floor(lightValue / bandStep) * bandStep;
                band = smoothstep(band, band + _BandSoftness, lightValue);

                // Color Selection
                float4 lightColor;
                if (band < 0.25)
                    lightColor = _DarkColor;
                else if (band < 0.5)
                    lightColor = _MidColor;
                else if (band < 0.75)
                    lightColor = _LightColor;
                else
                    lightColor = _HighlightColor;

                // Occlusion
                float occlusion = tex2D(_OcclusionMap, uv).r;
                occlusion = lerp(1, occlusion, _OcclusionStrength);

                // Optional Albedo
                float4 albedo = tex2D(_Albedo, uv);
                float3 finalColor =
                    lightColor.rgb * _GlobalTint.rgb * occlusion;

                finalColor = lerp(finalColor, finalColor * albedo.rgb, _AlbedoInfluence);

                return float4(finalColor, 1);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
