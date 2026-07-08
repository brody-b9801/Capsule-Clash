Shader "Hidden/CartoonOutlineDepthAware"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineThickness ("Outline Thickness", Float) = 1.0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _DepthSensitivity ("Depth Sensitivity", Float) = 1.0
        _NormalsSensitivity ("Normals Sensitivity", Float) = 1.0
        _DepthThreshold ("Depth Threshold", Float) = 0.1
        _CelShadingEnabled ("Cel Shading Enabled", Float) = 1.0
        _CelBands ("Cel Bands", Int) = 3
        _CelShadingStrength ("Cel Shading Strength", Float) = 1.0
        _DebugDepth ("Debug Depth View", Float) = 0.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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
            
            sampler2D _MainTex;
            sampler2D _CameraDepthNormalsTexture;
            sampler2D _CameraDepthTexture;
            float4 _MainTex_TexelSize;
            
            float _OutlineThickness;
            float4 _OutlineColor;
            float _DepthSensitivity;
            float _NormalsSensitivity;
            float _DepthThreshold;
            float _CelShadingEnabled;
            int _CelBands;
            float _CelShadingStrength;
            float _DebugDepth;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float GetDepth(float2 uv)
            {
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                return Linear01Depth(rawDepth);
            }
            
            bool IsSkybox(float depth)
            {
                return depth > 0.9999;
            }
            
            float CompareNormal(float3 normal1, float3 normal2)
            {
                float normalDiff = 1.0 - dot(normal1, normal2);
                return pow(normalDiff, 2.0) * _NormalsSensitivity;
            }
            
            // NEW APPROACH: Only detect edges on the CLOSER object
            // This prevents outlines from bleeding through from behind
            float DetectEdges(float2 uv, out bool isSkybox)
            {
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineThickness;
                
                float centerDepth = GetDepth(uv);
                
                isSkybox = IsSkybox(centerDepth);
                if (isSkybox)
                    return 0.0;
                
                float4 centerDepthNormal = tex2D(_CameraDepthNormalsTexture, uv);
                float centerDepthDN;
                float3 centerNormal;
                DecodeDepthNormal(centerDepthNormal, centerDepthDN, centerNormal);
                
                float2 offsets[4] = {
                    float2(0, texelSize.y),
                    float2(0, -texelSize.y),
                    float2(-texelSize.x, 0),
                    float2(texelSize.x, 0)
                };
                
                float totalEdge = 0.0;
                
                for (int i = 0; i < 4; i++)
                {
                    float2 sampleUV = uv + offsets[i];
                    float sampleDepth = GetDepth(sampleUV);
                    
                    if (IsSkybox(sampleDepth))
                        continue;
                    
                    // KEY CHANGE: Calculate depth difference
                    float depthDiff = sampleDepth - centerDepth;
                    
                    // ONLY draw outline if:
                    // 1. Center is CLOSER than sample (we're in front)
                    // 2. OR depths are very similar (on same surface)
                    
                    if (depthDiff > 0.02)
                    {
                        // Sample is MUCH further than center
                        // This is a depth discontinuity where CENTER is in front
                        // Draw the outline on the FOREGROUND (center) object
                        float depthEdge = saturate(abs(depthDiff) * _DepthSensitivity * 50.0);
                        totalEdge += depthEdge;
                    }
                    else if (abs(depthDiff) < 0.01)
                    {
                        // Depths are similar - check normals for surface edges
                        float4 sampleDepthNormal = tex2D(_CameraDepthNormalsTexture, sampleUV);
                        float sampleDepthDN;
                        float3 sampleNormal;
                        DecodeDepthNormal(sampleDepthNormal, sampleDepthDN, sampleNormal);
                        
                        float normalEdge = CompareNormal(centerNormal, sampleNormal);
                        totalEdge += normalEdge * 0.5;
                    }
                    // If depthDiff < -0.02 (center is behind), don't add to edge
                    // This prevents outlines from showing through
                }
                
                totalEdge *= 0.25; // Average
                
                // Apply threshold
                totalEdge = totalEdge > _DepthThreshold ? totalEdge : 0.0;
                
                return saturate(totalEdge);
            }
            
            float4 ApplyCelShading(float4 color)
            {
                float cmax = max(color.r, max(color.g, color.b));
                float cmin = min(color.r, min(color.g, color.b));
                float delta = cmax - cmin;
                
                float3 hsv;
                hsv.z = cmax;
                hsv.y = (cmax > 0.0) ? (delta / cmax) : 0.0;
                
                if (delta == 0.0)
                {
                    hsv.x = 0.0;
                }
                else if (cmax == color.r)
                {
                    hsv.x = 60.0 * fmod((color.g - color.b) / delta, 6.0);
                }
                else if (cmax == color.g)
                {
                    hsv.x = 60.0 * ((color.b - color.r) / delta + 2.0);
                }
                else
                {
                    hsv.x = 60.0 * ((color.r - color.g) / delta + 4.0);
                }
                
                if (hsv.x < 0.0)
                    hsv.x += 360.0;
                
                float bands = (float)_CelBands;
                float steppedValue = floor(hsv.z * bands) / bands;
                
                float bandTransition = frac(hsv.z * bands);
                steppedValue += smoothstep(0.85, 1.0, bandTransition) / bands;
                
                float h = hsv.x / 60.0;
                float s = hsv.y;
                float v = steppedValue;
                
                float c = v * s;
                float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
                float m = v - c;
                
                float3 celColor;
                if (h < 1.0)
                    celColor = float3(c, x, 0.0);
                else if (h < 2.0)
                    celColor = float3(x, c, 0.0);
                else if (h < 3.0)
                    celColor = float3(0.0, c, x);
                else if (h < 4.0)
                    celColor = float3(0.0, x, c);
                else if (h < 5.0)
                    celColor = float3(x, 0.0, c);
                else
                    celColor = float3(c, 0.0, x);
                
                celColor += m;
                
                return float4(lerp(color.rgb, celColor, _CelShadingStrength), color.a);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                if (_DebugDepth > 0.5)
                {
                    float depth = GetDepth(i.uv);
                    if (IsSkybox(depth))
                        return fixed4(1, 0, 0, 1);
                    
                    return fixed4(1.0 - depth, 1.0 - depth, 1.0 - depth, 1);
                }
                
                fixed4 col = tex2D(_MainTex, i.uv);
                
                bool isSkybox;
                float edge = DetectEdges(i.uv, isSkybox);
                
                if (isSkybox)
                {
                    return col;
                }
                
                if (_CelShadingEnabled > 0.5)
                {
                    col = ApplyCelShading(col);
                }
                
                col = lerp(col, _OutlineColor, edge);
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback Off
}
