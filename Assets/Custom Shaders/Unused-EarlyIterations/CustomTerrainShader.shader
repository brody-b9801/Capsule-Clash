Shader "Custom/Terrain Shader"
{
    Properties
    {
        _Color("Global Color Modifier", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _NormalTex("Normal", 2D) = "bump" {}
        _EmmisTex("Emission", 2D) = "black" {}
        
        _RampLevels("Ramp Levels", Range(2, 50)) = 2
        _LightScalar("Light Scalar", Range(0, 10)) = 1

        _HighColor("High Light Color", Color) = (1, 1, 1, 1)
        _HighIntensity("High Light Intensity", Range(0, 10)) = 1.5

        _LowColor("Low Light Color", Color) = (1, 1, 1, 1)
        _LowIntensity("Low Light Intensity", Range(0, 10)) = 1

        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineSize("Outline Size", Range(0, 0.1)) = 0.01
        _OutlineScaleStartDistance("Outline Scale Start Distance", Range(0, 100)) = 10.0
        _OutlineDistanceScale("Outline Distance Scale", Range(0.1, 2)) = 1.0

        _RimColor("Hard Edge Light Color", Color) = (1, 1, 1, 1)
        _RimAlpha("Hard Edge Light Brightness", Range(0, 1)) = 0
        _RimPower("Hard Edge Light Size", Range(0,1)) = 0
        _RimDropOff("Hard Edge Light Dropoff", range(0, 1)) = 0

        _FresnelColor("Soft Edge Light Color", Color) = (1,1,1,1)
        _FresnelBrightness("Soft Edge Light Brightness", Range(0, 1)) = 0
        _FresnelPower("Soft Edge Light Size", Range(0, 1)) = 0
        _FresnelShadowDropoff("Soft Edge Light Dropoff", range(0, 1)) = 0
    }
    
    SubShader
    {
        // --- OUTLINE PASS ---
        // This pass renders the inverted hull
        Pass
        {
            Tags { "LightMode" = "Always" }
            Cull Front // Render only the back faces

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            float _OutlineSize;
            float _OutlineScaleStartDistance;
            float _OutlineDistanceScale;
            float4 _OutlineColor;

            v2f vert (appdata v) {
                v2f o;
                // Convert to clip space
                float4 pos = UnityObjectToClipPos(v.vertex);
                
                // Calculate distance from camera in view space
                float3 viewPos = UnityObjectToViewPos(v.vertex);
                float distance = length(viewPos);
                
                // Scale outline based on distance, but only after the start distance
                // Keep outline at base size within the start distance
                float distanceScale = 1.0;
                if (distance > _OutlineScaleStartDistance) {
                    float excessDistance = distance - _OutlineScaleStartDistance;
                    distanceScale = 1.0 / max(excessDistance * _OutlineDistanceScale + 1.0, 1.0);
                }
                
                // Extrude the normals in view space to keep outline consistent
                float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                float2 offset = TransformViewToProjection(norm.xy);

                pos.xy += offset * pos.w * _OutlineSize * distanceScale;
                o.pos = pos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return _OutlineColor;
            }
            ENDCG
        }

        // --- MAIN CEL PASS ---
        Cull back
        Pass
        {
            Tags{ "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "AutoLight.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1)
                float3 worldNormal : TEXCOORD2;
                float3 worldTangent : TEXCOORD3;
                float3 worldBitangent : TEXCOORD4;
                float4 worldPos : TEXCOORD5;
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_tan v)
            {
                v2f o;
                o.uv = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = mul(UNITY_MATRIX_VP, o.worldPos);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldTangent = UnityObjectToWorldNormal(v.tangent);
                o.worldBitangent = cross(o.worldTangent, o.worldNormal);
                TRANSFER_SHADOW(o);
                return o;
            }

            float4    _Color;
            sampler2D _MainTex;
            uniform float4 _MainTex_ST;
            sampler2D _NormalTex;
            uniform float4 _NormalTex_ST;
            sampler2D _EmmisTex;
            uniform float4 _EmmisTex_ST;
            int       _RampLevels;
            float     _LightScalar;
            float     _HighIntensity;
            float4    _HighColor;
            float     _LowIntensity;
            float4    _LowColor;
            float     _RimPower;
            float     _RimAlpha;
            float4    _RimColor;
            float     _RimDropOff;
            float     _FresnelBrightness;
            float     _FresnelPower;
            float4    _FresnelColor;
            float     _FresnelShadowDropoff;

            fixed4 frag(v2f i) : SV_Target
            {
                int levels = _RampLevels - 1;

                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);

                fixed4 col = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
                fixed3 tangentNormal = tex2D(_NormalTex, i.uv * _NormalTex_ST.xy + _NormalTex_ST.zw).rgb * 2 - 1;
                fixed4 emmision = tex2D(_EmmisTex, i.uv * _EmmisTex_ST.xy + _EmmisTex_ST.zw);

                float3 worldNormal = normalize(i.worldTangent * tangentNormal.r + i.worldBitangent * tangentNormal.g + i.worldNormal * tangentNormal.b);

                half factor = dot(viewDirection, worldNormal);
                half fresnelFactor = 1 - min(pow(max(1 - factor, 0), (1 - _FresnelPower) * 10), 1);

                fixed shadow = SHADOW_ATTENUATION(i);

                float intensity = dot(worldNormal, lightDirection);
                intensity = clamp(intensity * _LightScalar, 0, 1);
                intensity *= shadow;
                
                float rampLevel = round(intensity * levels);
                float lightMultiplier = _LowIntensity + ((_HighIntensity - _LowIntensity) / max(levels, 1)) * rampLevel;

                float4 highColor = (rampLevel / max(levels, 1)) * _HighColor;
                float4 lowColor = ((levels - rampLevel) / max(levels, 1)) * _LowColor;
                float4 mixColor = (highColor + lowColor) / 2;
                
                col *= lightMultiplier;
                col *= _Color * mixColor;

                float rampPercentSoftFresnel = 1 - ((1 - rampLevel / max(levels, 1)) * (1 - _FresnelShadowDropoff));
                col.rgb = col.rgb + _FresnelColor.rgb * (_FresnelBrightness * 10 - fresnelFactor * _FresnelBrightness * 10) * rampPercentSoftFresnel;

                float currentRimAlpha = _RimAlpha * (1 - ((1 - rampLevel / max(levels, 1)) * (1 - _RimDropOff)));
                if (factor <= _RimPower) {
                    col.rgb = _RimColor.rgb * currentRimAlpha + col.rgb * (1 - currentRimAlpha);
                }

                half eIntensity = max(max(emmision.r, emmision.g), emmision.b);
                col.rgb = emmision.rgb * eIntensity + col.rgb * (1 - eIntensity);

                return col;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}