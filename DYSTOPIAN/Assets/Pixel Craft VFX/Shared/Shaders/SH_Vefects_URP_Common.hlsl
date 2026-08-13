#ifndef VEFECTS_PIXEL_CRAFT_URP_COMMON_INCLUDED
#define VEFECTS_PIXEL_CRAFT_URP_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTexture);
SAMPLER(sampler_MainTexture);

#if VEFECTS_ADVANCED
TEXTURE2D(_disolveMap);
SAMPLER(sampler_disolveMap);
TEXTURE2D(_DistortionTexture);
SAMPLER(sampler_DistortionTexture);
#endif

CBUFFER_START(UnityPerMaterial)
    float4 _R;
    float4 _G;
    float4 _B;
    float4 _Outline;
    float4 _OverallTint;
    float4 _UVS;
    float4 _UVP;
    float4 _UVDS;
    float4 _UVDP;
    float _FlatColor;
    float _Emissive;
    float _HueShift;
    float _SaturationMultiply;
    float _FlipbookX;
    float _FlipbookY;
    float _DissolveMapScale;
    float _DistortionLerp;
    float _PixelsMultiplier;
    float _PixelsX;
    float _PixelsY;
CBUFFER_END

struct VefectsAttributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VefectsVaryings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float4 uv0 : TEXCOORD0;
    float4 uv1 : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

VefectsVaryings VefectsVert(VefectsAttributes input)
{
    VefectsVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.color = input.color;
    output.uv0 = input.uv0;
    output.uv1 = input.uv1;
    return output;
}

float3 VefectsRGBToHSV(float3 color)
{
    float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(color.bg, k.wz), float4(color.gb, k.xy), step(color.b, color.g));
    float4 q = lerp(float4(p.xyw, color.r), float4(color.r, p.yzx), step(p.x, color.r));
    float d = q.x - min(q.w, q.y);
    const float epsilon = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + epsilon)), d / (q.x + epsilon), q.x);
}

float3 VefectsHSVToRGB(float3 color)
{
    float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(color.xxx + k.xyz) * 6.0 - k.www);
    return color.z * lerp(k.xxx, saturate(p - k.xxx), color.y);
}

float2 VefectsGetMainUV(VefectsVaryings input)
{
#if VEFECTS_ADVANCED
    float2 mainUV = _Time.y * _UVP.xy + input.uv0.xy * _UVS.xy;
    float2 distortionUV = _Time.y * _UVDP.xy + input.uv0.xy * _UVDS.xy;
    float2 distortion = (SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, distortionUV).rg - 0.5) * 2.0;
    mainUV += distortion * _DistortionLerp;

    #if defined(_PIXELATE_ON)
        float2 pixelCount = max(float2(_PixelsX, _PixelsY) * _PixelsMultiplier, float2(1.0, 1.0));
        mainUV = trunc(mainUV * pixelCount) / pixelCount;
    #endif

    return mainUV;
#else
    return input.uv0.xy;
#endif
}

half4 VefectsFrag(VefectsVaryings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 mainUV = VefectsGetMainUV(input);
    half4 mainSample = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, mainUV);

#if VEFECTS_COLOR_TEXTURE
    float3 hsv = VefectsRGBToHSV(mainSample.rgb);
    #if VEFECTS_ADVANCED
        half3 rgb = input.color.rgb * VefectsHSVToRGB(float3(hsv.x + _HueShift, hsv.y, hsv.z));
    #else
        half3 shiftedColor = VefectsHSVToRGB(float3(hsv.x + _HueShift, hsv.y * _SaturationMultiply, hsv.z));
        half3 rgb = input.color.rgb * _OverallTint.rgb * shiftedColor;
    #endif
#else
    half4 paletteColor = lerp(_Outline, _B, mainSample.b);
    paletteColor = lerp(paletteColor, _G, mainSample.g);
    paletteColor = lerp(paletteColor, _R, mainSample.r);
    half3 rgb = lerp(input.color.rgb * paletteColor.rgb, input.color.rgb, _FlatColor);
#endif

    half alpha = mainSample.a * input.color.a;

#if VEFECTS_ADVANCED
    float opacityWidth = input.uv0.w;
    float threshold = (opacityWidth - 1.0) + input.uv0.z * (2.0 - opacityWidth);
    float2 dissolveScale = float2(_FlipbookX, _FlipbookY) * _DissolveMapScale;
    #if VEFECTS_COLOR_TEXTURE
        float dissolveOffset = input.uv1.x;
    #else
        float dissolveOffset = _FlipbookX;
    #endif
    float dissolve = SAMPLE_TEXTURE2D(
        _disolveMap,
        sampler_disolveMap,
        input.uv0.xy * dissolveScale + dissolveOffset).g;
    alpha *= smoothstep(threshold, threshold + opacityWidth, dissolve);
#endif

    return half4(rgb * _Emissive, alpha);
}

#endif
