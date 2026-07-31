Shader "Hidden/DYSTOPIAN/IsolatedObjectPixelComposite"
{
    Properties
    {
        _SourceTex ("Isolated Object Buffer", 2D) = "black" {}
        _ColorSteps ("Brightness Steps", Range(2, 16)) = 6
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.85
        _OutlinePixels ("Outline Pixels", Range(0, 6)) = 1
        _OutlineColor ("Outline Color", Color) = (0.025, 0.02, 0.025, 1)
        _SilhouetteThreshold ("Silhouette Threshold", Range(0.01, 0.99)) = 0.45
        _NeutralChromaThreshold ("Neutral Chroma Threshold", Range(0, 0.2)) = 0.075
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "Process Isolated Object Pixels"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);
            float4 _SourceTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _ColorSteps;
                float _DitherStrength;
                float _OutlinePixels;
                float _SilhouetteThreshold;
                float _NeutralChromaThreshold;
                half4 _OutlineColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float Bayer4(float2 pixel)
            {
                float2 cell = floor(fmod(pixel, 4.0));

                if (cell.y < 1.0)
                {
                    if (cell.x < 1.0) return 0.5 / 16.0;
                    if (cell.x < 2.0) return 8.5 / 16.0;
                    if (cell.x < 3.0) return 2.5 / 16.0;
                    return 10.5 / 16.0;
                }

                if (cell.y < 2.0)
                {
                    if (cell.x < 1.0) return 12.5 / 16.0;
                    if (cell.x < 2.0) return 4.5 / 16.0;
                    if (cell.x < 3.0) return 14.5 / 16.0;
                    return 6.5 / 16.0;
                }

                if (cell.y < 3.0)
                {
                    if (cell.x < 1.0) return 3.5 / 16.0;
                    if (cell.x < 2.0) return 11.5 / 16.0;
                    if (cell.x < 3.0) return 1.5 / 16.0;
                    return 9.5 / 16.0;
                }

                if (cell.x < 1.0) return 15.5 / 16.0;
                if (cell.x < 2.0) return 7.5 / 16.0;
                if (cell.x < 3.0) return 13.5 / 16.0;
                return 5.5 / 16.0;
            }

            half SampleAlpha(float2 uv, int2 offset)
            {
                return SAMPLE_TEXTURE2D(
                    _SourceTex,
                    sampler_SourceTex,
                    uv + (float2)offset * _SourceTex_TexelSize.xy).a;
            }

            half FindNearbyAlpha(float2 uv, int radius)
            {
                half nearby = 0.0h;
                int cornerCut = radius > 1 ? (radius + 1) / 2 : 0;
                int maxManhattanDistance = radius * 2 - cornerCut;

                // Use a filled pixel-art octagon instead of a rasterized
                // circle. A discrete circle ends in one-pixel-wide tips at
                // its cardinal points; when many silhouette pixels overlap,
                // those tips become the small teeth visible on the outline.
                // The octagon keeps broad, even sides while trimming corners.
                [unroll]
                for (int y = -6; y <= 6; y++)
                {
                    [unroll]
                    for (int x = -6; x <= 6; x++)
                    {
                        int absoluteX = abs(x);
                        int absoluteY = abs(y);
                        int chebyshevDistance = max(absoluteX, absoluteY);
                        int manhattanDistance = absoluteX + absoluteY;

                        if ((x == 0 && y == 0) ||
                            chebyshevDistance > radius ||
                            manhattanDistance > maxManhattanDistance)
                        {
                            continue;
                        }

                        nearby = max(
                            nearby,
                            SampleAlpha(uv, int2(x, y)));
                    }
                }

                return nearby;
            }

            float3 GroupColorByBrightness(float3 sourceColor, float2 pixelCoord)
            {
                const float3 luminanceWeights = float3(0.2126, 0.7152, 0.0722);
                float sourceLuminance = dot(sourceColor, luminanceWeights);
                float levels = max(_ColorSteps - 1.0, 1.0);
                float threshold = lerp(
                    0.5,
                    Bayer4(pixelCoord),
                    saturate(_DitherStrength));
                float groupedLuminance =
                    floor(sourceLuminance * levels + threshold) / levels;

                float brightest = max(sourceColor.r, max(sourceColor.g, sourceColor.b));
                float darkest = min(sourceColor.r, min(sourceColor.g, sourceColor.b));
                float chroma = brightest - darkest;

                float luminanceScale = groupedLuminance /
                    max(sourceLuminance, 0.0001);
                float clippingSafeScale = 1.0 /
                    max(brightest, 0.0001);
                luminanceScale = min(luminanceScale, clippingSafeScale);

                float3 huePreserved = saturate(sourceColor * luminanceScale);

                float darknessBoost = lerp(
                    1.65,
                    1.0,
                    saturate(brightest * 2.0));
                float neutralThreshold =
                    _NeutralChromaThreshold * darknessBoost;
                float neutralBlend = 1.0 - smoothstep(
                    neutralThreshold,
                    neutralThreshold * 2.0,
                    chroma);
                float3 neutralColor = groupedLuminance.xxx;

                return lerp(huePreserved, neutralColor, neutralBlend);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 bufferSize = _SourceTex_TexelSize.zw;
                float2 pixelCoord = floor(input.uv * bufferSize);
                float2 snappedUv = (pixelCoord + 0.5) / bufferSize;
                half4 source = SAMPLE_TEXTURE2D(
                    _SourceTex,
                    sampler_SourceTex,
                    snappedUv);

                bool inside = source.a >= _SilhouetteThreshold;
                int radius = (int)round(_OutlinePixels);

                if (!inside && radius > 0)
                {
                    half nearbyAlpha = FindNearbyAlpha(snappedUv, radius);
                    if (nearbyAlpha >= _SilhouetteThreshold)
                        return _OutlineColor;
                }

                if (!inside)
                    return half4(0, 0, 0, 0);

                float3 groupedColor = GroupColorByBrightness(
                    saturate(source.rgb),
                    pixelCoord);

                return half4(groupedColor, source.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
