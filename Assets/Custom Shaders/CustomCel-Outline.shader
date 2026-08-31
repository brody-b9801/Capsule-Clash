Shader "FlexibleCelShader/Comic Book Style"
{
    Properties
    {
        _Color("Global Color Modifier", Color) = (1, 1, 1, 1)
        _MainTex("Texture", 2D) = "white" {}
        _EmmisTex("Emission", 2D) = "black" {}

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
        _OutlineSize("Outline Size", Range(0, 0.5)) = 0.01
        [Toggle] _OutlineScreenSpace("Constant Screen-Space Width", Float) = 1
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
    }

    SubShader
    {
        // ═══════════════════════════════════════════════════════════════════
        //  OUTLINE PASS
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "Always" }
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            float  _OutlineSize;
            float  _OutlineScreenSpace;
            float  _OutlineScaleStartDistance;
            float  _OutlineDistanceScale;
            float4 _OutlineColor;
            float  _DynamicOutline;
            float  _OutlineLightModulation;

            v2f vert(appdata v)
            {
                v2f o;

                // Extrude along the world-space normal (lean outline method).
                float3 positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 normalWS   = normalize(UnityObjectToWorldNormal(v.normal));

                float width = _OutlineSize;

                // Constant screen-space width: grow with camera distance.
                if (_OutlineScreenSpace > 0.5)
                    width *= distance(positionWS, _WorldSpaceCameraPos);

                // Distance-based shrink past a threshold (kept from original).
                float viewDist = length(UnityObjectToViewPos(v.vertex));
                if (viewDist > _OutlineScaleStartDistance)
                {
                    float excess = viewDist - _OutlineScaleStartDistance;
                    width /= max(excess * _OutlineDistanceScale + 1.0, 1.0);
                }

                // Light-driven thickness (kept from original).
                if (_DynamicOutline > 0.5)
                {
                    float3 lightDir   = normalize(_WorldSpaceLightPos0.xyz);
                    float  NdotL      = dot(normalWS, lightDir);
                    float  lightScale = lerp(1.8, 0.3, saturate(NdotL * 0.5 + 0.5));
                    width *= lerp(1.0, lightScale, _OutlineLightModulation);
                }

                positionWS += normalWS * width;

                o.pos = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _OutlineColor;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════════════
        //  MAIN CEL PASS  (ForwardBase — directional light + lightmaps)
        //
        //  Lean cel model:
        //      celRamp = ToonRamp( saturate(N·L * _LightScalar) * shadow )
        //      diffuse = lerp( unlit color , lit color , celRamp )
        //  plus SH/lightmap indirect and the original banded hard-edge rim.
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
                float2 texcoord  : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float2 uv          : TEXCOORD0;
                SHADOW_COORDS(1)
                float3 worldNormal : TEXCOORD2;
                float4 worldPos    : TEXCOORD3;
                float2 lightmapUV  : TEXCOORD4;
                float4 pos         : SV_POSITION;
            };

            float4    _Color;
            sampler2D _MainTex;
            float4    _MainTex_ST;
            sampler2D _EmmisTex;
            float4    _EmmisTex_ST;
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

            // Softened N-band staircase in [0,1].  When banding is disabled the
            // input passes through unchanged (smooth ramp).
            float ToonRamp(float x)
            {
                x = saturate(x);
                if (_UseShadowBands < 0.5)
                    return x;

                float steps  = max((float)_ShadowBands - 1.0, 1.0);
                float scaled = x * steps;
                float lower  = floor(scaled);
                float f      = scaled - lower;                       // position within step
                float w      = max(_ShadowBandSmoothness, 1e-5);
                float blend  = smoothstep(0.5 - w, 0.5 + w, f);
                return (lower + blend) / steps;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.uv          = v.texcoord;
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex);
                o.pos         = mul(UNITY_MATRIX_VP, o.worldPos);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.lightmapUV  = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 viewDir  = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                fixed4 albedo   = tex2D(_MainTex,  i.uv * _MainTex_ST.xy  + _MainTex_ST.zw);
                fixed4 emission = tex2D(_EmmisTex, i.uv * _EmmisTex_ST.xy + _EmmisTex_ST.zw);

                float3 N = normalize(i.worldNormal);

                // ── Lean cel ramp ─────────────────────────────────────────
                float shadow    = SHADOW_ATTENUATION(i);
                float lightTerm = saturate(dot(N, lightDir) * _LightScalar) * shadow;
                float celRamp   = ToonRamp(lightTerm);

                // ── Indirect / ambient ────────────────────────────────────
                #ifdef LIGHTMAP_ON
                    float3 indirectLight = DecodeLightmap(
                        UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUV)
                    );
                #else
                    float3 indirectLight = ShadeSH9(float4(N, 1.0));
                #endif

                // ── Shadow-color → lit-color blend ────────────────────────
                float3 litColor    = _HighColor.rgb * _HighIntensity * _LightColor0.rgb;
                float3 shadowColor = _LowColor.rgb  * _LowIntensity;
                float3 diffuse     = lerp(shadowColor, litColor, celRamp);

                float3 baseCol = albedo.rgb * _Color.rgb;
                fixed4 col     = fixed4(baseCol * diffuse, albedo.a * _Color.a);

                col.rgb += baseCol * indirectLight * _IndirectLightIntensity;

                // ── Banded hard edge (rim) light ──────────────────────────
                half  rimDot          = dot(viewDir, N);
                float currentRimAlpha = _RimAlpha * (1.0 - ((1.0 - celRamp) * (1.0 - _RimDropOff)));
                float rimRange        = max(_RimPower, 0.0001);
                float rimFactor       = saturate((rimRange - rimDot) / rimRange);
                int   rimBandLevels   = max(_RimBands - 1, 1);
                float rimBanded       = round(rimFactor * float(rimBandLevels)) / float(rimBandLevels);
                col.rgb = lerp(col.rgb, _RimColor.rgb, rimBanded * currentRimAlpha);

                // ── Emission ──────────────────────────────────────────────
                half eIntensity = max(max(emission.r, emission.g), emission.b);
                col.rgb = emission.rgb * eIntensity + col.rgb * (1.0 - eIntensity);

                return col;
            }
            ENDCG
        }

        // ═══════════════════════════════════════════════════════════════════
        //  FORWARD ADD PASS  (additional lights: point, spot, etc)
        //
        //  Same lean ramp, blended up from black so each extra light only
        //  adds its lit contribution (no unlit color, no ambient, no rim).
        // ═══════════════════════════════════════════════════════════════════
        Pass
        {
            Tags { "LightMode" = "ForwardAdd" }
            Blend One One
            Cull Back

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_fwdadd_fullshadows

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 worldPos    : TEXCOORD2;
                float4 pos         : SV_POSITION;
                LIGHTING_COORDS(3, 4)
            };

            float4    _Color;
            sampler2D _MainTex;
            float4    _MainTex_ST;
            float     _LightScalar;
            float     _HighIntensity;
            float4    _HighColor;
            float     _UseShadowBands;
            int       _ShadowBands;
            float     _ShadowBandSmoothness;

            float ToonRamp(float x)
            {
                x = saturate(x);
                if (_UseShadowBands < 0.5)
                    return x;

                float steps  = max((float)_ShadowBands - 1.0, 1.0);
                float scaled = x * steps;
                float lower  = floor(scaled);
                float f      = scaled - lower;
                float w      = max(_ShadowBandSmoothness, 1e-5);
                float blend  = smoothstep(0.5 - w, 0.5 + w, f);
                return (lower + blend) / steps;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.uv          = v.texcoord;
                o.worldPos    = mul(unity_ObjectToWorld, v.vertex);
                o.pos         = mul(UNITY_MATRIX_VP, o.worldPos);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 N = normalize(i.worldNormal);
                float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos.xyz));

                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos.xyz);

                fixed4 albedo = tex2D(_MainTex, i.uv * _MainTex_ST.xy + _MainTex_ST.zw);

                float lightTerm = saturate(dot(N, lightDir) * _LightScalar) * atten;
                float celRamp   = ToonRamp(lightTerm);

                float3 litColor = _HighColor.rgb * _HighIntensity * _LightColor0.rgb;
                float3 add      = albedo.rgb * _Color.rgb * litColor * celRamp;

                return fixed4(add, 1.0);
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
            float4    _HighColor;

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

                o.Albedo   = albedo.rgb * _HighColor.rgb;
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
