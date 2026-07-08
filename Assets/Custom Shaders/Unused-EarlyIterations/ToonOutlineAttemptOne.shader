Shader "Custom/ToonOutline"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Steps("Toon Steps", Range(1, 5)) = 3
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness("Outline Thickness", Range(0.001, 0.05)) = 0.01
        _EmissionColor("Emission Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // --- First pass: outline ---
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }

            Cull Front // Render back faces for outline
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            uniform float _OutlineThickness;
            uniform float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 norm = normalize(v.normal);
                float3 offset = norm * _OutlineThickness;
                float4 posOffset = v.vertex + float4(offset, 0);
                o.pos = UnityObjectToClipPos(posOffset);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // --- Second pass: main toon shading ---
        Pass
        {
            Name "TOON"
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma surface surf Lambert
            #pragma target 3.0

            sampler2D _MainTex;
            fixed4 _Color;
            fixed _Steps;
            fixed4 _EmissionColor;

            struct Input
            {
                float2 uv_MainTex;
            };

            void surf(Input IN, inout SurfaceOutput o)
            {
                fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;

                // Quantize lighting for toon effect
                half NdotL = dot(o.Normal, _WorldSpaceLightPos0.xyz);
                NdotL = saturate(NdotL);
                NdotL = floor(NdotL * _Steps) / _Steps;

                o.Albedo = tex.rgb * NdotL;
                o.Alpha = tex.a;

                // Add emission
                o.Emission = _EmissionColor.rgb;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
