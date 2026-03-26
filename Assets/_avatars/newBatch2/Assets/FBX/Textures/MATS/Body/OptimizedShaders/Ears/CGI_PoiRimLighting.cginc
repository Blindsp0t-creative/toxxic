#ifndef POI_RIM
#define POI_RIM
float4 _RimLightColor;
float _RimLightingInvert;
float _RimWidth;
float _RimStrength;
float _RimSharpness;
float _RimLightColorBias;
float _ShadowMix;
float _ShadowMixThreshold;
float _ShadowMixWidthMod;
float _EnableRimLighting;
float _RimBrighten;
float _RimLightNormal;
float _RimHueShiftEnabled;
float _RimHueShiftSpeed;
float _RimHueShift;
#ifdef POI_AUDIOLINK
    half _AudioLinkRimWidthBand;
    float2 _AudioLinkRimWidthAdd;
    half _AudioLinkRimEmissionBand;
    float2 _AudioLinkRimEmissionAdd;
    half _AudioLinkRimBrightnessBand;
    float2 _AudioLinkRimBrightnessAdd;
#endif
#if defined(PROP_RIMTEX) || !defined(OPTIMIZER_ENABLED)
    POI_TEXTURE_NOSAMPLER(_RimTex);
#endif
#if defined(PROP_RIMMASK) || !defined(OPTIMIZER_ENABLED)
    POI_TEXTURE_NOSAMPLER(_RimMask);
#endif
#if defined(PROP_RIMWIDTHNOISETEXTURE) || !defined(OPTIMIZER_ENABLED)
    POI_TEXTURE_NOSAMPLER(_RimWidthNoiseTexture);
#endif
float _RimWidthNoiseStrength;
float4 rimColor = float4(0, 0, 0, 0);
float rim = 0;
void applyRimLighting(inout float4 albedo, inout float3 rimLightEmission)
{
    #if defined(PROP_RIMWIDTHNOISETEXTURE) || !defined(OPTIMIZER_ENABLED)
        float rimNoise = POI2D_SAMPLER_PAN(_RimWidthNoiseTexture, _MainTex, poiMesh.uv[(0.0 /*_RimWidthNoiseTextureUV*/)], float4(0,0,0,0));
    #else
        float rimNoise = 0;
    #endif
    rimNoise = (rimNoise - .5) * (0.1 /*_RimWidthNoiseStrength*/);
    float viewDotNormal = saturate(abs(dot(poiCam.viewDir, poiMesh.normals[(1.0 /*_RimLightNormal*/)])));
    
    if ((0.0 /*_RimLightingInvert*/))
    {
        viewDotNormal = 1 - viewDotNormal;
    }
    float rimStrength = (0.0 /*_RimStrength*/);
    float rimBrighten = (0.0 /*_RimBrighten*/);
    float rimWidth = lerp( - .05, 1, (1.0 /*_RimWidth*/));
    #ifdef POI_AUDIOLINK
        
        if (poiMods.audioLinkTextureExists)
        {
            rimWidth = clamp(rimWidth + lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[(0.0 /*_AudioLinkRimWidthBand*/)]), - .05, 1);
            rimStrength += lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[(0.0 /*_AudioLinkRimEmissionBand*/)]);
            rimBrighten += lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[(0.0 /*_AudioLinkRimBrightnessBand*/)]);
        }
    #endif
    rimWidth -= rimNoise;
    #if defined(PROP_RIMMASK) || !defined(OPTIMIZER_ENABLED)
        float rimMask = POI2D_SAMPLER_PAN(_RimMask, _MainTex, poiMesh.uv[(0.0 /*_RimMaskUV*/)], float4(0,0,0,0));
    #else
        float rimMask = 1;
    #endif
    #if defined(PROP_RIMTEX) || !defined(OPTIMIZER_ENABLED)
        rimColor = POI2D_SAMPLER_PAN(_RimTex, _MainTex, poiMesh.uv[(0.0 /*_RimTexUV*/)], float4(0,0,0,0)) * float4(0,0,0,1);
    #else
        rimColor = float4(0,0,0,1);
    #endif
    
    if ((0.0 /*_RimHueShiftEnabled*/))
    {
        rimColor.rgb = hueShift(rimColor.rgb, (0.0 /*_RimHueShift*/) + _Time.x * (0.0 /*_RimHueShiftSpeed*/));
    }
    rimWidth = max(lerp(rimWidth, rimWidth * lerp(0, 1, poiLight.lightMap - (0.5 /*_ShadowMixThreshold*/)) * (0.5 /*_ShadowMixWidthMod*/), (0.0 /*_ShadowMix*/)), 0);
    rim = 1 - smoothstep(min((0.128 /*_RimSharpness*/), rimWidth), rimWidth, viewDotNormal);
    rim *= float4(0,0,0,1).a * rimColor.a * rimMask;
    rimLightEmission = rim * lerp(albedo, rimColor, (1.0 /*_RimLightColorBias*/)) * rimStrength;
    albedo.rgb = lerp(albedo.rgb, lerp(albedo.rgb, rimColor, (1.0 /*_RimLightColorBias*/)) + lerp(albedo.rgb, rimColor, (1.0 /*_RimLightColorBias*/)) * rimBrighten, rim);
}
#endif
