Shader "Custom/LavaHazard_SeamlessGrid_URP"
{
    Properties
    {
        [HDR] _BottomColor ("Bottom Color (Bright)", Color) = (1.0, 0.8, 0.0, 1.0)
        [HDR] _TopColor ("Top Color (Dark)", Color) = (0.8, 0.2, 0.0, 1.0)
        _LavaLevel ("Base Lava Level (Height)", Float) = 0.0
        _WaveHeight ("Wave Amplitude", Float) = 0.05
        _WaveFrequency ("Wave Frequency", Float) = 8.0
        _WaveSpeed ("Wave Speed", Float) = 3.0
        _GradientSpread ("Gradient Spread", Float) = 0.05
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
                float3 positionWS : TEXCOORD1; // NEU: World Space Position
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BottomColor;
                half4 _TopColor;
                float _LavaLevel;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _GradientSpread;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                
                // NEU: Wir wandeln die lokale Position in die globale Szenen-Position um
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // WICHTIG: Die Welle nutzt jetzt input.positionWS (Weltkoordinaten) für X und Z
                float wave = sin(input.positionWS.x * _WaveFrequency + _Time.y * _WaveSpeed) * 0.5 
                           + cos(input.positionWS.z * _WaveFrequency * 0.8 + _Time.y * _WaveSpeed * 1.1) * 0.5;
                
                float currentLavaLevel = _LavaLevel + (wave * _WaveHeight);
                
                // WICHTIG: Die Höhe nutzt weiterhin input.positionOS (Objektkoordinaten)
                float blend = smoothstep(currentLavaLevel - _GradientSpread, currentLavaLevel + _GradientSpread, input.positionOS.y);
                
                half4 finalColor = lerp(_BottomColor, _TopColor, blend);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}