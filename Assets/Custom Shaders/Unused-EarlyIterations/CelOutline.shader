Shader "FlexibleCelShader/Comic Book Style W/ Normal Map"
{
    Properties
    {
        _Color("Global Color Modifier", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _NormalTex("Normal Map", 2D) = "bump" {}
        _EmmisTex("Emission", 2D) = "black" {}

        [Header(Normal Map Detail Overlay)]
        [Space(10)]
        _NormalDetailLitColor("Detail Lit Color", Color) = (1, 1, 1, 1)
        _NormalDetailShadowColor("Detail Shadow Color", Color) = (0.6, 0.6, 0.8, 1)
        _NormalDetailIntensity("Detail Overlay Intensity", Range(0, 2)) = 0.5
        _NormalDetailBands("Detail Overlay Bands", Range(2, 10)) = 3
        _NormalDetailMipBias("Detail Mip Bias", Range(-4, 0)) = -1

        _RampLevels("Ramp Levels", Range(2, 50)) = 3
        _LightScalar("Light Scalar", Range(0, 10)) = 1

        [Header(Indirect Lighting)]
        [Space(10)]
        _IndirectLightIntensity("Indirect Light Intensity", Range(0, 2)) = 1



        [Header(Shadow Transition Bands)]
        [Space(10)]
        [Toggle] _UseShadowBands("Enable Banded Shadow Transition", Float) = 1
        _ShadowBands("Number of Shadow Bands", Range(2, 10)) = 3
        _ShadowBandSmoothness("Shadow Band Smoothness", Range(0, 0.5)) = 0.05

        [Header(Lit and Unlit Colors)]
        [Space(10)]
        _HighColor("Lit Color", Color) = (1, 1, 1, 1)
        _HighIntensity("Lit Intensity", Range(0, 10)) = 1.5

        _LowColor("Unlit Color", Color) = (1, 1, 1, 1)
        _LowIntensity("Unlit Intensity", Range(0, 10)) = 1

        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineSize("Outline Size", Range(0, 0.1)) = 0.01
        _OutlineScaleStartDistance("Outline Scale Start Distance", Range(0, 100)) = 10.0
        _OutlineDistanceScale("Outline Distance Scale", Range(0.1, 2)) = 1.0
        [Toggle] _DynamicOutline("Dynamic Outline Thickness", Float) = 1
        _OutlineLightModulation("Outline Light Modulation", Range(0, 2)) = 0.8

        [Header(Hard Edge Lighting)]
        [Space(10)]
        _RimColor("Hard Edge Light Color", Color) = (1, 1, 1, 1)
        _RimAlpha("Hard Edge Light Brightness", Range(0, 1)) = 0
        _RimPower("Hard Edge Light Size", Range(0, 1)) = 0.5
        _RimDropOff("Hard Edge Light Dropoff", Range(0, 1)) = 0.3
        _RimBands("Hard Edge Light Bands", Range(2, 10)) = 3

        [Header(Specular Highlights)]
        [Space(10)]
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 1
        _SpecularSize("Specular Size", Range(0, 1)) = 0.2
        _SpecularSmoothness("Specular Smoothness", Range(1, 100)) = 30
        _SpecularBands("Specular Bands", Range(1, 10)) = 2
        _SpecularShadowDropoff("Specular Shadow Dropoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        // ═══════════════════════════════════════════════════════════════════
        //  OUTLINE PASS
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Tags { "LightMode" = "Always" }
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos         : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
            };

            float  _OutlineSize;
            float  _OutlineScaleStartDistance;
            float  _OutlineDistanceScale;
            float4 _OutlineColor;
            float  _DynamicOutline;
            float  _OutlineLightModulation;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex).xyz;

                float4 pos      = UnityObjectToClipPos(v.vertex);
                float3 viewPos  = UnityObjectToViewPos(v.vertex);
                float  distance = length(viewPos);

                float distanceScale = 1.0;
                if (distance > _OutlineScaleStartDistance)
                {
                    float excessDistance = distance - _OutlineScaleStartDistance;
                    distanceScale = 1.0 / max(excessDistance * _OutlineDistanceScale + 1.0, 1.0);
                }

                float outlineScale = 1.0;
                if (_DynamicOutline > 0.5)
                {
                    float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                    float  NdotL    = dot(o.worldNormal, lightDir);
                    outlineScale    = lerp(1.8, 0.3, saturate(NdotL * 0.5 + 0.5));
                    outlineScale    = lerp(1.0, outlineScale, _OutlineLightModulation);
                }

                float3 norm   = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                float2 offset = TransformViewToProjection(norm.xy);
                pos.xy += offset * pos.w * _OutlineSize * distanceScale * outlineScale;

                o.pos = pos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MAIN CEL PASS  (ForwardBase — handles directional light + lightmaps)
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            Cull Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_fwdbase
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex    : POSITION;
                float3 normal    : NORMAL;
                float4 tangent   : TANGENT;
                float2 texcoord  : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
            };

            struct v2f
            {
                float2 uv             : TEXCOORD0;
                SHADOW_COORDS(1)
                float3 worldNormal    : TEXCOORD2;
                float3 worldTangent   : TEXCOORD3;
                float3 worldBitangent : TEXCOORD4;
                float4 worldPos       : TEXCOORD5;
                float2 lightmapUV     : TEXCOORD6;
                float4 pos            : SV_POSITION;
            };

            float4    _Color;
            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _NormalTex;
            float4    _NormalTex_ST;
            sampler2D _EmmisTex;
            float4    _EmmisTex_ST;
            int       _RampLevels;
            float     _LightScalar;
            float     _IndirectLightIntensity;
            float     _HighIntensity;
            float4    _HighColor;
            float     _LowIntensity;
            float4    _LowColor;
            float     _RimPower;
            float     _RimAlpha;
            float4    _RimColor;
            float     _RimDropOff;
            int       _RimBands;
            float     _UseShadowBands;
            int       _ShadowBands;
            float     _ShadowBandSmoothness;
            float4    _SpecularColor;
            float     _SpecularIntensity;
            float     _SpecularSize;
            float     _SpecularSmoothness;
            int       _SpecularBands;
            float     _SpecularShadowDropoff;
            float4    _NormalDetailLitColor;
            float4    _NormalDetailShadowColor;
            float     _NormalDetailIntensity;
            int       _NormalDetailBands;
            float     _NormalDetailMipBias;

            v2f vert(appdata v)
            {
                v2f o;
                o.uv             = v.texcoord;
                o.worldPos       = mul(unity_ObjectToWorld, v.vertex);
                o.pos            = mul(UNITY_MATRIX_VP, o.worldPos);
                o.worldNormal    = UnityObjectToWorldNormal(v.normal);
                o.worldTangent   = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangent.w;
                o.lightmapUV     = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int levels = max(_RampLevels - 1, 1);

                float3 viewDirection  = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);

                // ── Sample textures ────────────────────────────────────────
                fixed4 albedo        = tex2D(_MainTex,   i.uv * _MainTex_ST.xy  + _MainTex_ST.zw);
                fixed3 tangentNormal = UnpackNormal(tex2Dbias(_NormalTex, float4(i.uv * _NormalTex_ST.xy + _NormalTex_ST.zw, 0, _NormalDetailMipBias)));
                fixed4 emission      = tex2D(_EmmisTex,  i.uv * _EmmisTex_ST.xy + _EmmisTex_ST.zw);

                // ── TBN matrix ─────────────────────────────────────────────
                float3 T = normalize(i.worldTangent);
                float3 B = normalize(i.worldBitangent);
                float3 N = normalize(i.worldNormal);

                // ── Normal map (overlay only) ──────────────────────────────
                // Build the bumped normal purely so the detail overlay below
                // can compare it against the geometric normal.  It is NOT fed
                // into the primary cel lighting — N drives all main shading.
                float3 bumpedNormal = normalize(T * tangentNormal.x + B * tangentNormal.y + N * tangentNormal.z);

                // ── Indirect / ambient lighting ────────────────────────────
                // Evaluated against the smooth geometric normal (N) rather
                // than the bumped one.  This keeps ambient stable at all
                // distances and prevents lightmap seams from fighting the
                // normal map.
                #ifdef LIGHTMAP_ON
                    float3 indirectLight = DecodeLightmap(
                        UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUV)
                    );
                #else
                    float3 indirectLight = ShadeSH9(float4(N, 1.0));
                #endif

                // ── Direct lighting — primary cel shading uses the geometric
                //    normal N.  Normal map detail is layered on separately via
                //    the overlay so it survives quantization.  We keep the raw
                //    (unshadowed) NdotL separate so we can blend baked +
                //    realtime shadows properly below. ─────────────────────────
                fixed shadow    = SHADOW_ATTENUATION(i);
                float NdotL     = dot(N, lightDirection);

                // rawIntensity: purely geometry-driven, no shadow yet.
                // Saturate NdotL first, then apply light scalar for overall
                // intensity. This prevents detail loss when light scalar is high.
                float rawIntensity = saturate(NdotL) * _LightScalar;

                // Blend realtime shadow with baked lightmap contribution.
                float shadowedIntensity = rawIntensity * shadow;

                // ── Banded shadow transition ───────────────────────────────
                float bandedIntensity = shadowedIntensity;
                if (_UseShadowBands > 0.5)
                {
                    int   shadowBandLevels = max(_ShadowBands - 1, 1);
                    float bandedValue      = round(shadowedIntensity * float(shadowBandLevels)) / float(shadowBandLevels);

                    if (_ShadowBandSmoothness > 0.0)
                    {
                        float bandWidth    = 1.0 / float(shadowBandLevels);
                        float bandPosition = frac(shadowedIntensity * float(shadowBandLevels));
                        float smoothFactor = smoothstep(
                            0.5 - _ShadowBandSmoothness,
                            0.5 + _ShadowBandSmoothness,
                            bandPosition
                        );
                        float nextBand  = min(bandedValue + bandWidth, 1.0);
                        bandedIntensity = lerp(bandedValue, nextBand, smoothFactor);
                    }
                    else
                    {
                        bandedIntensity = bandedValue;
                    }
                }

                // ── Quantize lighting intensity ────────────────────────────
                float rampLevel      = round(bandedIntensity * float(levels));
                float lightIntensity = rampLevel / float(levels);
                float lightMultiplier = _LowIntensity + ((_HighIntensity - _LowIntensity) / float(levels)) * rampLevel;

                float4 highColor = (rampLevel / float(levels)) * _HighColor;
                float4 lowColor  = ((float(levels) - rampLevel) / float(levels)) * _LowColor;
                float4 mixColor  = (highColor + lowColor) / 2.0;

                // Direct light contribution
                fixed4 col = albedo * _Color * mixColor * lightMultiplier * float4(_LightColor0.rgb, 1);

                // Indirect light contribution.
                col.rgb += albedo.rgb * _Color.rgb * indirectLight * _IndirectLightIntensity;

                // ── Normal map detail overlay ──────────────────────────────
                // Compare bumped vs geometric NdotL to extract per-pixel
                // micro-detail that survives cel-shading quantization.
                if (_NormalDetailIntensity > 0.0)
                {
                    float geomNdotL   = saturate(dot(N, lightDirection));
                    float bumpedNdotL = saturate(dot(bumpedNormal, lightDirection));
                    float detail      = bumpedNdotL - geomNdotL; // [-1, 1] range

                    // Cel-shade the overlay: quantize the detail delta into
                    // discrete bands (round handles the sign, so it steps
                    // symmetrically around zero — positive and negative alike).
                    int detailBands = max(_NormalDetailBands - 1, 1);
                    detail = round(detail * float(detailBands)) / float(detailBands);

                    // Pick detail color based on whether this pixel is lit or shadowed
                    float3 detailColor = lerp(_NormalDetailShadowColor.rgb, _NormalDetailLitColor.rgb, lightIntensity);

                    // Apply: positive detail brightens, negative darkens
                    col.rgb += detailColor * detail * _NormalDetailIntensity;
                }

                // ── Rim light ──────────────────────────────────────────────
                half  factor          = dot(viewDirection, N);
                float currentRimAlpha = _RimAlpha * (1.0 - ((1.0 - lightIntensity) * (1.0 - _RimDropOff)));
                float rimRange        = max(_RimPower, 0.0001);
                float rimFactor       = saturate((rimRange - factor) / rimRange);
                int rimBandLevels     = max(_RimBands - 1, 1);
                float rimBanded       = round(rimFactor * float(rimBandLevels)) / float(rimBandLevels);
                col.rgb = lerp(col.rgb, _RimColor.rgb, rimBanded * currentRimAlpha);

                // ── Specular highlights ────────────────────────────────────
                float3 halfVector      = normalize(lightDirection + viewDirection);
                float  specularDot     = max(dot(N, halfVector), 0.0);
                float  specularRaw     = pow(specularDot, max(_SpecularSmoothness, 1.0));
                specularRaw            = saturate((specularRaw - (1.0 - _SpecularSize)) / max(_SpecularSize, 0.0001));

                int   specularBandLvls = max(_SpecularBands, 1);
                float specularBanded   = round(specularRaw * float(specularBandLvls - 1)) / float(max(specularBandLvls - 1, 1));

                float rampPercentSpecular = 1.0 - ((1.0 - lightIntensity) * (1.0 - _SpecularShadowDropoff));
                col.rgb += _SpecularColor.rgb * specularBanded * _SpecularIntensity * rampPercentSpecular;

                // ── Emission ───────────────────────────────────────────────
                half eIntensity = max(max(emission.r, emission.g), emission.b);
                col.rgb = emission.rgb * eIntensity + col.rgb * (1.0 - eIntensity);

                return col;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════════════
        //  FORWARD ADD PASS  (additional lights: point, spot, etc)
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            Cull Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_fwdadd
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex    : POSITION;
                float3 normal    : NORMAL;
                float4 tangent   : TANGENT;
                float2 texcoord  : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv             : TEXCOORD0;
                SHADOW_COORDS(1)
                float3 worldNormal    : TEXCOORD2;
                float3 worldTangent   : TEXCOORD3;
                float3 worldBitangent : TEXCOORD4;
                float4 worldPos       : TEXCOORD5;
                float4 pos            : SV_POSITION;
            };

            float4    _Color;
            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _NormalTex;
            float4    _NormalTex_ST;
            int       _RampLevels;
            float     _LightScalar;
            float     _HighIntensity;
            float4    _HighColor;
            float     _LowIntensity;
            float4    _LowColor;
            float     _UseShadowBands;
            int       _ShadowBands;
            float     _ShadowBandSmoothness;
            float4    _NormalDetailLitColor;
            float4    _NormalDetailShadowColor;
            float     _NormalDetailIntensity;
            int       _NormalDetailBands;
            float     _NormalDetailMipBias;

            v2f vert(appdata v)
            {
                v2f o;
                o.uv             = v.texcoord;
                o.worldPos       = mul(unity_ObjectToWorld, v.vertex);
                o.pos            = mul(UNITY_MATRIX_VP, o.worldPos);
                o.worldNormal    = UnityObjectToWorldNormal(v.normal);
                o.worldTangent   = UnityObjectToWorldDir(v.tangent.xyz);
                o.worldBitangent = cross(o.worldNormal, o.worldTangent) * v.tangent.w;

                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                int levels = max(_RampLevels - 1, 1);

                float3 viewDirection  = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 lightDirection = normalize(UnityWorldSpaceLightDir(i.worldPos));

                // ── Sample textures ────────────────────────────────────────
                fixed4 albedo        = tex2D(_MainTex,   i.uv * _MainTex_ST.xy  + _MainTex_ST.zw);
                fixed3 tangentNormal = UnpackNormal(tex2Dbias(_NormalTex, float4(i.uv * _NormalTex_ST.xy + _NormalTex_ST.zw, 0, _NormalDetailMipBias)));

                // ── TBN matrix ────────────────────────────────────────────
                float3 T = normalize(i.worldTangent);
                float3 B = normalize(i.worldBitangent);
                float3 N = normalize(i.worldNormal);

                // ── Normal map (overlay only) — bumped normal used only for
                //    the detail overlay; main lighting uses the geometric N. ──
                float3 bumpedNormal = normalize(T * tangentNormal.x + B * tangentNormal.y + N * tangentNormal.z);

                // ── Direct lighting from additional lights (geometric normals) ──
                fixed shadow    = SHADOW_ATTENUATION(i);
                float NdotL     = dot(N, lightDirection);
                float rawIntensity = saturate(NdotL) * _LightScalar;
                float shadowedIntensity = rawIntensity * shadow;

                // ── Banded shadow transition ───────────────────────────────
                float bandedIntensity = shadowedIntensity;
                if (_UseShadowBands > 0.5)
                {
                    int   shadowBandLevels = max(_ShadowBands - 1, 1);
                    float bandedValue      = round(shadowedIntensity * float(shadowBandLevels)) / float(shadowBandLevels);

                    if (_ShadowBandSmoothness > 0.0)
                    {
                        float bandWidth    = 1.0 / float(shadowBandLevels);
                        float bandPosition = frac(shadowedIntensity * float(shadowBandLevels));
                        float smoothFactor = smoothstep(
                            0.5 - _ShadowBandSmoothness,
                            0.5 + _ShadowBandSmoothness,
                            bandPosition
                        );
                        float nextBand  = min(bandedValue + bandWidth, 1.0);
                        bandedIntensity = lerp(bandedValue, nextBand, smoothFactor);
                    }
                    else
                    {
                        bandedIntensity = bandedValue;
                    }
                }

                // ── Quantize lighting intensity ────────────────────────────
                float rampLevel      = round(bandedIntensity * float(levels));
                float lightIntensity = rampLevel / float(levels);
                float lightMultiplier = _LowIntensity + ((_HighIntensity - _LowIntensity) / float(levels)) * rampLevel;

                float4 highColor = (rampLevel / float(levels)) * _HighColor;
                float4 lowColor  = ((float(levels) - rampLevel) / float(levels)) * _LowColor;
                float4 mixColor  = (highColor + lowColor) / 2.0;

                // ── Accumulate this light's contribution
                fixed4 col = albedo * _Color * mixColor * lightMultiplier * float4(_LightColor0.rgb, 1);

                // ── Normal map detail overlay ──────────────────────────────
                if (_NormalDetailIntensity > 0.0)
                {
                    float bumpedNdotL = saturate(dot(bumpedNormal, lightDirection));
                    float detail      = bumpedNdotL - saturate(dot(N, lightDirection));

                    // Cel-shade the overlay: quantize the detail delta into bands.
                    int detailBands = max(_NormalDetailBands - 1, 1);
                    detail = round(detail * float(detailBands)) / float(detailBands);

                    float3 detailColor = lerp(_NormalDetailShadowColor.rgb, _NormalDetailLitColor.rgb, lightIntensity);
                    col.rgb += detailColor * detail * _NormalDetailIntensity;
                }

                return col;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════════════
        //  META PASS  (lightmap baking — albedo & emission contribution)
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Name "META"
            Tags { "LightMode" = "Meta" }
            Cull Off

            CGPROGRAM
            #pragma vertex   vert_meta
            #pragma fragment frag_meta
            #include "UnityCG.cginc"
            #include "UnityMetaPass.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _EmmisTex;
            float4    _EmmisTex_ST;
            float4    _Color;

            struct appdata_meta
            {
                float4 vertex    : POSITION;
                float2 texcoord  : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
            };

            struct v2f_meta
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f_meta vert_meta(appdata_meta v)
            {
                v2f_meta o;
                o.pos = UnityMetaVertexPosition(
                    v.vertex,
                    v.texcoord1.xy, v.texcoord2.xy,
                    unity_LightmapST, unity_DynamicLightmapST
                );
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag_meta(v2f_meta i) : SV_Target
            {
                UnityMetaInput o;
                UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

                fixed4 albedo   = tex2D(_MainTex,  i.uv) * _Color;
                fixed4 emission = tex2D(_EmmisTex, i.uv);

                o.Albedo   = albedo.rgb;
                o.Emission = emission.rgb;

                return UnityMetaFragment(o);
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════════════
        //  SHADOW CASTER PASS
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vert_shadow
            #pragma fragment frag_shadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
            };

            v2f vert_shadow(appdata v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag_shadow(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
