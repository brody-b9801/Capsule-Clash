Shader "Custom/TrailShape" {
    Properties {
        _MainTex ("Texture", 2D) = "white" { }
        _DistortionStrength ("Distortion Strength", Range(0.1, 1)) = 0.5
    }

    SubShader {
        Tags { "Queue" = "Overlay" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma exclude_renderers gles xbox360 ps3
            ENDCG

            SetTexture[_MainTex] {
                combine primary
            }
        }
    }

    SubShader {
        Tags { "Queue" = "Overlay" }
        LOD 100

        ZWrite On
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Name "DST_ALPHA"
            Blend DstAlpha SrcAlpha
        }
    }
}

CGPROGRAM
#pragma surface surf Lambert

sampler2D _MainTex;
float _DistortionStrength;

struct Input {
    float2 uv_MainTex;
};

void vert(inout appdata_full v) {
    // Distort the trail vertices based on a sine wave
    v.vertex.y += _DistortionStrength * sin(v.vertex.x * 10);
}
ENDCG
