Shader "Custom/MineBomb_URP"
{
    Properties
    {
        [HDR] _TopColor ("Top Color (Blinking)", Color) = (1.0, 0.0, 0.0, 1.0)
        [HDR] _BottomColor ("Bottom Color (Solid)", Color) = (0.0, 0.8, 0.0, 1.0)
        _BlinkSpeed ("Blink Speed", Float) = 8.0
        _SplitHeight ("Split Height (Y-Axis)", Float) = 0.2
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
                half4 _TopColor;
                half4 _BottomColor;
                float _BlinkSpeed;
                float _SplitHeight;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float blink = (sin(_Time.y * _BlinkSpeed) * 0.5) + 0.5;
                half4 activeTopColor = _TopColor * blink;
                
                float isTop = step(_SplitHeight, input.positionOS.y);
                
                half4 finalColor = lerp(_BottomColor, activeTopColor, isTop);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}