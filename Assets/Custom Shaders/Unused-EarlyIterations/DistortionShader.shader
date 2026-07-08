Shader "Custom/DistortionShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" { }
        _DistortionAmount ("Distortion Amount", Range (0, 1)) = 0.1
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    CGINCLUDE

    struct appdata
    {
        float4 baseColor : COLOR;
    };

    struct v2f
    {
        float4 pos : POSITION;
        float4 color : COLOR;
    };

    float _DistortionAmount;

    v2f vert(appdata v)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.color = v.baseColor;
        return o;
    }

    fixed4 frag(v2f i) : COLOR
    {
        // Add distortion effect to the fragment color
        fixed4 col = i.color * tex2D(_MainTex, i.color.xy);
        col.rgb += (_DistortionAmount * 2.0 - 1.0) * 0.1;
        return col;
    }
}
