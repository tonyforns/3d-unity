Shader "ThreeDUnity/Wireframe Edges"
{
    Properties
    {
        [HDR] _EdgeColor ("Edge Color", Color) = (1, 0.92, 0.2, 1)
        _EdgeWidth ("Edge Width", Range(0.0001, 0.1)) = 0.015
        [Toggle] _ShowFill ("Show Fill", Float) = 0
        [HDR] _FillColor ("Fill Color", Color) = (0, 0, 0, 0.15)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "WireframeEdges"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma require geometry
            #pragma vertex Vert
            #pragma geometry Geom
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
                half4 _FillColor;
                half _EdgeWidth;
                half _ShowFill;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct V2G
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct G2F
            {
                float4 positionCS : SV_POSITION;
                float3 barycentric : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            V2G Vert(Attributes input)
            {
                V2G output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            [maxvertexcount(3)]
            void Geom(triangle V2G input[3], inout TriangleStream<G2F> triStream)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input[0]);

                for (uint i = 0; i < 3; i++)
                {
                    G2F output;
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                    output.positionCS = input[i].positionCS;
                    output.barycentric = float3(i == 0, i == 1, i == 2);
                    triStream.Append(output);
                }

                triStream.RestartStrip();
            }

            half4 Frag(G2F input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 bary = input.barycentric;
                float3 derivatives = fwidth(bary);
                float3 edgeFactor = smoothstep(derivatives * _EdgeWidth, derivatives * (_EdgeWidth + 0.002), bary);
                float edge = 1.0 - min(edgeFactor.x, min(edgeFactor.y, edgeFactor.z));

                half4 fill = _ShowFill > 0.5 ? _FillColor : half4(0, 0, 0, 0);
                half4 color = lerp(fill, _EdgeColor, edge);
                color.a *= max(edge, fill.a);

                clip(color.a - 0.001);
                return color;
            }
            ENDHLSL
        }
    }

    // Sin geometry shader (móvil / WebGL): relleno plano semitransparente como aviso.
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "WireframeFallback"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex VertFallback
            #pragma fragment FragFallback

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings VertFallback(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 FragFallback(Varyings input) : SV_Target
            {
                return half4(_EdgeColor.rgb, 0.25);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
