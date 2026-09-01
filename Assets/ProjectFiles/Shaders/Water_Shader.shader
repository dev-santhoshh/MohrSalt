Shader "Custom/Water_Gravity_Container_Unity6"
{
    Properties
    {
        _ShallowColor ("Shallow Tint", Color) = (0.7, 0.9, 1.0, 1)
        _DeepColor ("Deep Tint", Color) = (0.1, 0.4, 0.8, 1)
        _SurfaceRimColor ("Top Edge Color (Supports Alpha)", Color) = (1, 1, 1, 1)

        _Transparency ("Transparency", Range(0.1, 1)) = 0.75

        [Header(Container Relative Fill Level)]
        _FillHeight ("Water Level (Height above pivot)", Float) = 0.05

        _FresnelPower ("Edge/Rim Power", Range(0.5, 10)) = 3.0
        _DepthStrength ("Depth Darkening", Range(0,10)) = 1.5

        _WaveStrength ("Tiny Ripples", Range(0,0.02)) = 0.002
        _WaveSpeed ("Ripple Speed", Range(0,5)) = 0.6
        _WaveScale ("Ripple Scale", Range(0,10)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+10"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct v2f
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _SurfaceRimColor;
                float _FillHeight;
                float _FresnelPower;
                float _DepthStrength;
                float _WaveStrength;
                float _WaveSpeed;
                float _WaveScale;
                half _Transparency;
            CBUFFER_END

            v2f vert (appdata input)
            {
                v2f output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInputs.normalWS;

                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

                return output;
            }

            half4 frag (v2f input, FRONT_FACE_TYPE facing : SV_IsFrontFace) : SV_Target
            {
                // World Gravity Direction (Always points straight up)
                float3 gravityUp = float3(0, 1, 0);

                // Animated surface ripple
                float ripple = sin((input.positionWS.x + input.positionWS.z) * _WaveScale + _Time.y * _WaveSpeed) * _WaveStrength;

                // 1. Get the current Object's World Origin (Pivot) dynamically from matrix
                float3 containerWorldPivot = UNITY_MATRIX_M._m03_m13_m23;

                // 2. Measure fragment height relative to the container pivot, BUT along global World Gravity Y (0,1,0)
                float heightRelativeToContainerPivot = dot(input.positionWS - containerWorldPivot, gravityUp);

                // 3. Clip surface
                float surfaceLevel = _FillHeight + ripple;
                float distToSurface = surfaceLevel - heightRelativeToContainerPivot;

                // Clip pixels above water line
                clip(distToSurface);

                // Water depth color blend along gravity vector
                float depth = saturate(distToSurface * _DepthStrength);
                half3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, depth);

                // Handle double-sided rendering (inside container wall lighting)
                float3 N = normalize(input.normalWS);
                N = facing ? N : -N;

                float3 V = normalize(input.viewDirWS);
                float NdotV = saturate(dot(N, V));

                // Rim light / fresnel outline
                half fresnel = pow(1.0 - NdotV, _FresnelPower);

                // Surface line rim
                float surfaceEdge = smoothstep(0.015, 0.00, distToSurface);
                half3 finalCol = lerp(waterColor + (fresnel * 0.3), _SurfaceRimColor.rgb, surfaceEdge);

                // Dynamic Alpha calculation taking top edge color alpha (_SurfaceRimColor.a) into account
                half baseAlpha = max(_Transparency, fresnel * 0.5);
                half targetEdgeAlpha = _SurfaceRimColor.a;
                half alpha = lerp(baseAlpha, targetEdgeAlpha, surfaceEdge);

                return half4(finalCol, alpha);
            }

            ENDHLSL
        }
    }
}