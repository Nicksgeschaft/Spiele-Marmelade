Shader "Custom/LavaHazard_DeepSeamless_URP"
{
    Properties
    {
        [HDR] _ColorA ("Deep Color A (Matches Surface Bottom)", Color) = (1.0, 0.8, 0.0, 1.0)
        [HDR] _ColorB ("Deep Color B (Darker Pulse)", Color) = (1.0, 0.5, 0.0, 1.0)
        _WaveFrequency ("Wave Frequency", Float) = 8.0
        _WaveSpeed ("Wave Speed", Float) = 3.0
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
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                float _WaveFrequency;
                float _WaveSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float wave = sin(input.positionWS.x * _WaveFrequency + _Time.y * _WaveSpeed) * 0.5 
                           + cos(input.positionWS.z * _WaveFrequency * 0.8 + _Time.y * _WaveSpeed * 1.1) * 0.5;
                
                wave = wave * 0.5 + 0.5;
                
                half4 finalColor = lerp(_ColorA, _ColorB, wave);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}