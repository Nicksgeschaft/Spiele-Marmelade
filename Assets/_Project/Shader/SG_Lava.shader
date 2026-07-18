Shader "Custom/LavaHazard_StaticMesh_URP"
{
    Properties
    {
        [HDR] _ColorA ("Color A (Yellow)", Color) = (1.0, 0.8, 0.0, 1.0)
        [HDR] _ColorB ("Color B (Orange)", Color) = (1.0, 0.3, 0.0, 1.0)
        _SloshSpeed ("Slosh Speed (Time based)", Float) = 2.0
        _SloshScale ("Slosh Scale (Coordinate based)", Float) = 5.0
        _SloshStrength ("Slosh Strength (Intensity)", Float) = 0.5
        _NoiseScale ("Noise Size", Float) = 15.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                float _SloshSpeed;
                float _SloshScale;
                float _SloshStrength;
                float _NoiseScale;
            CBUFFER_END

            float hash(float2 p) 
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * (p.x + p.y));
            }

            float noise(float2 x) 
            {
                float2 i = floor(x);
                float2 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i + float2(0.0, 0.0)), hash(i + float2(1.0, 0.0)), f.x),
                            lerp(hash(i + float2(0.0, 1.0)), hash(i + float2(1.0, 1.0)), f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 posOS = input.positionOS;
                
                float n = noise(posOS.xz * _NoiseScale);
                
                float animatedZ = posOS.z + (n * _SloshStrength);
                
                float sloshValue = sin(_Time.y * _SloshSpeed + (animatedZ * _SloshScale)); 
                sloshValue = sloshValue * 0.5 + 0.5;
                
                half4 finalColor = lerp(_ColorA, _ColorB, sloshValue);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}