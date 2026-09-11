Shader "Hidden/DZDMapEditor/OverlayComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "JFA Init"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 Frag(Varyings input) : SV_Target
            {
                int2 pixel = int2(input.positionCS.xy);
                float4 sampleColor = LOAD_TEXTURE2D_X(_BlitTexture, pixel);
                float occupancy = max(sampleColor.r, max(sampleColor.g, max(sampleColor.b, sampleColor.a)));
                if (occupancy < 0.01)
                    return float2(-1.0, -1.0);

                return float2(pixel);
            }
            ENDHLSL
        }

        Pass
        {
            Name "JFA Jump"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float2 _AxisWidth;

            float2 Frag(Varyings input) : SV_Target
            {
                int2 pixel = int2(input.positionCS.xy);
                int2 maxPixel = (int2)_BlitTexture_TexelSize.zw - 1;
                float bestDistance = 3.402823466e+38;
                float2 bestSeed = float2(-1.0, -1.0);

                UNITY_UNROLL
                for (int step = -1; step <= 1; step++)
                {
                    int2 samplePixel = clamp(pixel + int2(_AxisWidth) * step, int2(0, 0), maxPixel);
                    float2 seed = LOAD_TEXTURE2D_X(_BlitTexture, samplePixel).rg;
                    if (seed.x < 0.0)
                        continue;

                    float2 delta = float2(pixel) - seed;
                    float distanceSq = dot(delta, delta);
                    if (distanceSq < bestDistance)
                    {
                        bestDistance = distanceSq;
                        bestSeed = seed;
                    }
                }

                return bestSeed.x < 0.0 ? float2(-1.0, -1.0) : bestSeed;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local _ SCALE_WITH_RESOLUTION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_DZDOverlaySilhouette);

            float _OutlineWidth;
            float _ReferenceResolution;

            half4 Frag(Varyings input) : SV_Target
            {
                int2 pixel = int2(input.positionCS.xy);
                float2 seed = LOAD_TEXTURE2D_X(_BlitTexture, pixel).rg;
                if (seed.x < 0.0)
                    return 0;

                half4 inside = LOAD_TEXTURE2D(_DZDOverlaySilhouette, pixel);
                if (inside.a > 0.01)
                    return 0;

                float width = _OutlineWidth;
                #if defined(SCALE_WITH_RESOLUTION)
                width *= _ScreenParams.y / max(_ReferenceResolution, 1.0);
                #endif

                float distanceToSeed = length(float2(pixel) - seed);
                half coverage = saturate(width - distanceToSeed + 1.0);
                half4 color = LOAD_TEXTURE2D(_DZDOverlaySilhouette, int2(seed));
                return color * coverage;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Fill"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _FillColor;

            half4 Frag(Varyings input) : SV_Target
            {
                int2 pixel = int2(input.positionCS.xy);
                float mask = LOAD_TEXTURE2D_X(_BlitTexture, pixel).r;
                if (mask < 0.01)
                    return 0;

                return _FillColor;
            }
            ENDHLSL
        }
    }
}
