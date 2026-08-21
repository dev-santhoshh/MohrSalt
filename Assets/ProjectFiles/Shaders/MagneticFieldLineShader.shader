Shader "Custom/MagneticFieldLineShader"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor ("Base Color", Color) = (0.05, 0.1, 0.2, 1)
        [HDR] _GlowColor ("Glow Color (HDR)", Color) = (0.2, 3.0, 4.0, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 0)

        [Header(Glow)]
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.5
        _Opacity ("Opacity", Range(0, 1)) = 1.0
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.35

        [Header(Flow Animation)]
        _FlowSpeed ("Flow Speed", Range(-10, 10)) = 1.5
        _Tiling ("Tiling", Vector) = (2, 2, 0, 0)

        [Header(Line Shape)]
        _LineDensity ("Line Density", Range(1, 50)) = 10
        _LineWidth ("Line Width", Range(0.001, 1)) = 0.08

        [Header(Noise Distortion)]
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 0.3
        _NoiseScale ("Noise Scale", Range(0.1, 20)) = 3
        _DistortionStrength ("Distortion Strength", Range(0, 2)) = 0.25
        _DistortionSpeed ("Distortion Speed", Range(-5, 5)) = 0.75

        [Header(Pulse)]
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 2
        _PulseStrength ("Pulse Strength", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float4 _BackgroundColor;
                float  _GlowIntensity;
                float  _FlowSpeed;
                float  _LineDensity;
                float  _LineWidth;
                float  _NoiseStrength;
                float  _NoiseScale;
                float  _EdgeFade;
                float  _PulseSpeed;
                float  _PulseStrength;
                float  _Opacity;
                float4 _Tiling;
                float  _DistortionStrength;
                float  _DistortionSpeed;
            CBUFFER_END

            // Lightweight hash-based value noise, cheap enough for mobile/Quest.
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float valueNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Single sine-based field line ribbon with soft glow falloff.
            float fieldLine(float2 uv, float density, float width)
            {
                float wave = sin(uv.x * density * TWO_PI) * 0.5 + 0.5;
                float d = abs(frac(uv.y * density) - wave);
                d = min(d, 1.0 - d);
                float line = smoothstep(width, 0.0, d);
                return line;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = positionInputs.positionCS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float time = _Time.y;
                float2 tiling = _Tiling.xy;
                float2 uv = IN.uv * tiling;

                // Procedural distortion so lines wobble like a real magnetic field.
                float2 distortUV = uv * _NoiseScale + time * _DistortionSpeed;
                float2 distortion = float2(
                    valueNoise(distortUV),
                    valueNoise(distortUV + float2(19.7, 7.3))
                ) - 0.5;
                uv += distortion * _DistortionStrength;

                // Continuous seamless scroll (horizontal + vertical flow).
                float2 flowUV = uv + float2(time * _FlowSpeed, time * _FlowSpeed * 0.35);

                // Extra fine noise layered on top for organic shimmer.
                float fineNoise = valueNoise(flowUV * _NoiseScale * 2.0 + time * 0.2) - 0.5;
                flowUV += fineNoise * _NoiseStrength * 0.1;

                float line = fieldLine(flowUV, _LineDensity, _LineWidth);

                // Soft bloom-style falloff around the line core.
                float glowMask = exp(-abs(line - 1.0) * 6.0);
                glowMask = pow(saturate(line + glowMask * 0.3), 1.5);

                // Pulse animation modulating overall glow intensity.
                float pulse = 1.0 + sin(time * _PulseSpeed) * _PulseStrength * 0.5;

                // Radial-ish edge fade using UV distance from tile center, wrapped seamlessly.
                float2 centered = frac(IN.uv) - 0.5;
                float edgeDist = 1.0 - saturate(length(centered) * 2.0);
                float edgeFade = smoothstep(0.0, _EdgeFade, edgeDist);

                float3 emission = _GlowColor.rgb * _GlowIntensity * pulse * glowMask;
                float3 color = _BaseColor.rgb * _BackgroundColor.a + emission;

                float alpha = saturate(glowMask * pulse * edgeFade * _Opacity);

                half4 result = half4(color, alpha);
                return result;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
