Shader "Custom/Terrain Cel With Outline"
{
    Properties
    {
        _Color("Global Color Modifier", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _NormalTex("Normal", 2D) = "bump" {}
        _EmmisTex("Emission", 2D) = "black" {}
        
        _RampLevels("Ramp Levels", Range(2,50)) = 2
        _LightScalar("Light Scalar", Range(0,10)) = 1

        _HighColor("High Light Color", Color) = (1,1,1,1)
        _HighIntensity("High Light Intensity", Range(0,10)) = 1.5

        _LowColor("Low Light Color", Color) = (1,1,1,1)
        _LowIntensity("Low Light Intensity", Range(0,10)) = 1

        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineSize("Outline Size", Range(0,0.1)) = 0.01
        _OutlineScaleStartDistance("Outline Scale Start Distance", Range(0,100)) = 10
        _OutlineDistanceScale("Outline Distance Scale", Range(0.1,2)) = 1

        _RimColor("Hard Edge Light Color", Color) = (1,1,1,1)
        _RimAlpha("Hard Edge Light Brightness", Range(0,1)) = 0
        _RimPower("Hard Edge Light Size", Range(0,1)) = 0
        _RimDropOff("Hard Edge Light Dropoff", Range(0,1)) = 0
        _RimBands("Hard Edge Light Bands", Range(2,50)) = 5

        _FresnelColor("Soft Edge Light Color", Color) = (1,1,1,1)
        _FresnelBrightness("Soft Edge Light Brightness", Range(0,1)) = 0
        _FresnelPower("Soft Edge Light Size", Range(0,1)) = 0
        _FresnelShadowDropoff("Soft Edge Light Dropoff", Range(0,1)) = 0
        _FresnelBands("Soft Edge Light Bands", Range(2,50)) = 5

        _NormalDistanceBoost("Normal Boost With Distance", Range(0,1)) = 0.2
        _NormalStrength("Normal Map Strength", Range(0,1)) = 0.5
        _NormalDarkColor("Normal Map Shadow Color", Color) = (0,0,0,1)
        _NormalBands("Normal Map Bands", Range(2,50)) = 5
        
        _NormalRimColor("Normal Hard Edge Light Color", Color) = (1,1,1,1)
        _NormalRimAlpha("Normal Hard Edge Light Brightness", Range(0,1)) = 0
        _NormalRimPower("Normal Hard Edge Light Size", Range(0,1)) = 0
        _NormalRimBands("Normal Hard Edge Light Bands", Range(2,50)) = 5
        
        _NormalFresnelColor("Normal Soft Edge Light Color", Color) = (1,1,1,1)
        _NormalFresnelBrightness("Normal Soft Edge Light Brightness", Range(0,1)) = 0
        _NormalFresnelPower("Normal Soft Edge Light Size", Range(0,1)) = 0
        _NormalFresnelBands("Normal Soft Edge Light Bands", Range(2,50)) = 5

        // --- NORMAL MAP DISTANCE FADE ---
        _NormalFadeStart("Normal Fade Start Distance", Float) = 15
        _NormalFadeEnd("Normal Fade End Distance", Float) = 60

        // --- DISTANCE COLOR FADE ---
        _DistanceFadeColor("Distance Fade Color", Color) = (0.5,0.5,0.5,1)
        _DistanceFadeStart("Distance Fade Start", Float) = 30
        _DistanceFadeEnd("Distance Fade End", Float) = 80
    }

    SubShader
    {
        // ---------- OUTLINE PASS ----------
        Pass
        {
            Tags { "LightMode" = "Always" }
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float _OutlineSize;
            float _OutlineScaleStartDistance;
            float _OutlineDistanceScale;
            float4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                float4 pos = UnityObjectToClipPos(v.vertex);

                float3 viewPos = UnityObjectToViewPos(v.vertex);
                float distance = length(viewPos);

                float distanceScale = 1.0;
                if (distance > _OutlineScaleStartDistance)
                {
                    float excess = distance - _OutlineScaleStartDistance;
                    distanceScale = 1.0 / max(excess * _OutlineDistanceScale + 1.0, 1.0);
                }

                float3 norm = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                float2 offset = TransformViewToProjection(norm.xy);

                pos.xy += offset * pos.w * _OutlineSize * distanceScale;
                o.pos = pos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // ---------- MAIN CEL PASS ----------
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back

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

            float4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _NormalTex;
            float4 _NormalTex_ST;
            sampler2D _EmmisTex;
            float4 _EmmisTex_ST;

            int _RampLevels;
            float _LightScalar;

            float _HighIntensity;
            float4 _HighColor;
            float _LowIntensity;
            float4 _LowColor;

            float _RimPower;
            float _RimAlpha;
            float4 _RimColor;
            float _RimDropOff;
            int _RimBands;

            float _FresnelBrightness;
            float _FresnelPower;
            float4 _FresnelColor;
            float _FresnelShadowDropoff;
            int _FresnelBands;

            float _NormalDistanceBoost;
            float _NormalStrength;
            float4 _NormalDarkColor;
            int _NormalBands;
            
            float4 _NormalRimColor;
            float _NormalRimAlpha;
            float _NormalRimPower;
            int _NormalRimBands;
            
            float4 _NormalFresnelColor;
            float _NormalFresnelBrightness;
            float _NormalFresnelPower;
            int _NormalFresnelBands;

            float _NormalFadeStart;
            float _NormalFadeEnd;

            float4 _DistanceFadeColor;
            float _DistanceFadeStart;
            float _DistanceFadeEnd;

            fixed4 frag(v2f i) : SV_Target
            {
                int levels = _RampLevels - 1;

                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                fixed4 col = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);
                fixed4 emission = tex2D(_EmmisTex, i.uv * _EmmisTex_ST.xy + _EmmisTex_ST.zw);

                // --- BASE LIGHT ---
                float baseIntensity = saturate(dot(i.worldNormal, lightDir) * _LightScalar);
                baseIntensity *= SHADOW_ATTENUATION(i);

                float rampLevel = round(baseIntensity * levels);
                float lightMultiplier =
                    _LowIntensity +
                    ((_HighIntensity - _LowIntensity) / max(levels, 1)) * rampLevel;

                float4 highColor = (rampLevel / max(levels, 1)) * _HighColor;
                float4 lowColor = ((levels - rampLevel) / max(levels, 1)) * _LowColor;
                float4 mixColor = (highColor + lowColor) * 0.5;

                col *= _Color * mixColor * lightMultiplier;

                // --- NORMAL MAP DETAIL (SHADOW-ONLY COLOR) ---
                float3 tangentNormal = tex2Dlod(
                    _NormalTex,
                    float4(i.uv * _NormalTex_ST.xy + _NormalTex_ST.zw, 0, 0)
                ).rgb * 2 - 1;

                float3 normalDetail = normalize(
                    i.worldTangent   * tangentNormal.r +
                    i.worldBitangent * tangentNormal.g +
                    i.worldNormal    * tangentNormal.b
                );

                float ndotlDetail = saturate(dot(normalDetail, lightDir));
                
                // Apply banding to normal details with its own band count
                int normalLevels = _NormalBands - 1;
                float rampDetail = round(ndotlDetail * normalLevels) / max(normalLevels, 1);

                float dist = distance(_WorldSpaceCameraPos, i.worldPos.xyz);
                float normalFade = saturate((_NormalFadeEnd - dist) / max(_NormalFadeEnd - _NormalFadeStart, 0.0001));
                float finalNormalStrength = _NormalStrength * normalFade;

                // --- DARKEN BASED ON NORMAL MAP ---
                float shadowAmount = 1.0 - rampDetail;
                col.rgb = lerp(col.rgb, col.rgb * _NormalDarkColor.rgb, shadowAmount * finalNormalStrength);

                // --- NORMAL MAP SOFT FRESNEL ---
                float normalFactor = dot(viewDir, normalDetail);
                float normalFresnelFactor = 1 - min(pow(max(1 - normalFactor, 0), (1 - _NormalFresnelPower) * 10), 1);
                
                // Apply banding to normal fresnel
                int normalFresnelLevels = _NormalFresnelBands - 1;
                normalFresnelFactor = round(normalFresnelFactor * normalFresnelLevels) / max(normalFresnelLevels, 1);
                
                col.rgb += _NormalFresnelColor.rgb * 
                    (_NormalFresnelBrightness * 10 - normalFresnelFactor * _NormalFresnelBrightness * 10) * 
                    normalFade;

                // --- NORMAL MAP HARD RIM ---
                // Apply banding to normal rim factor
                int normalRimLevels = _NormalRimBands - 1;
                float normalRimFactor = round(normalFactor * normalRimLevels) / max(normalRimLevels, 1);
                
                if (normalRimFactor <= _NormalRimPower)
                {
                    col.rgb = lerp(col.rgb, _NormalRimColor.rgb, _NormalRimAlpha * normalFade);
                }

                // --- SOFT FRESNEL ---
                float factor = dot(viewDir, i.worldNormal);
                float fresnelFactor =
                    1 - min(pow(max(1 - factor, 0), (1 - _FresnelPower) * 10), 1);

                // Apply banding to fresnel with its own band count
                int fresnelLevels = _FresnelBands - 1;
                fresnelFactor = round(fresnelFactor * fresnelLevels) / max(fresnelLevels, 1);

                float rampPercentSoft =
                    1 - ((1 - rampLevel / max(levels, 1)) * (1 - _FresnelShadowDropoff));

                col.rgb +=
                    _FresnelColor.rgb *
                    (_FresnelBrightness * 10 - fresnelFactor * _FresnelBrightness * 10) *
                    rampPercentSoft;

                // --- HARD RIM ---
                float rimAlpha =
                    _RimAlpha * (1 - ((1 - rampLevel / max(levels, 1)) * (1 - _RimDropOff)));

                // Apply banding to rim factor with its own band count
                int rimLevels = _RimBands - 1;
                float rimFactor = round(factor * rimLevels) / max(rimLevels, 1);

                if (rimFactor <= _RimPower)
                {
                    col.rgb = lerp(col.rgb, _RimColor.rgb, rimAlpha);
                }

                // --- EMISSION ---
                float e = max(emission.r, max(emission.g, emission.b));
                col.rgb = lerp(col.rgb, emission.rgb, e);

                // --- DISTANCE COLOR FADE (FINAL) ---
                float distFade = saturate(
                    (dist - _DistanceFadeStart) /
                    max(_DistanceFadeEnd - _DistanceFadeStart, 0.0001)
                );

                col.rgb = lerp(col.rgb, _DistanceFadeColor.rgb, distFade);

                return col;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}