Shader "UI/RetroDither"
{
    Properties
    {
        _MainTex          ("Sprite Texture",     2D)    = "white" {}
        _Color            ("Tint",               Color) = (1,1,1,1)
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        _Curve ("Curve Distortion", Range(0, 0.5)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UI_RETRODITHER"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _TextureSampleAdd;
            float4    _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float2 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float2 _ScreenSize;
            float  _Curve;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos  = v.vertex;
                o.vertex    = UnityObjectToClipPos(v.vertex);
                o.texcoord  = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color     = v.color * _Color;
                float4 sp   = ComputeScreenPos(o.vertex);
                o.screenPos = sp.xy / sp.w * _ScreenSize;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.screenPos / _ScreenSize;

                float2 curveUV = uv * 2.0 - 1.0;
                float2 coffset = curveUV.yx * _Curve;
                curveUV += curveUV * coffset * coffset;
                curveUV = curveUV * 0.5 + 0.5;

                float2 edge = smoothstep(0.0, 0.1, curveUV) * (1.0 - smoothstep(0.9, 1.0, curveUV));

                half4 col = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
                col.rgb *= edge.x * edge.y;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
