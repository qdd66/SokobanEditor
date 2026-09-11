Shader "Hidden/DZDMapEditor/OverlaySilhouette"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _FillMask ("Fill Mask", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Cull Off
        ZWrite Off
        ZTest [_ZTest]
        Blend Off

        Pass
        {
            Name "Silhouette"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _FillMask;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            struct FragOutput
            {
                half4 color : SV_Target0;
                half4 fill : SV_Target1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            FragOutput Frag(Varyings input)
            {
                FragOutput output;
                output.color = _Color;
                output.fill = half4(_FillMask, 0, 0, 0);
                return output;
            }
            ENDHLSL
        }
    }
}
