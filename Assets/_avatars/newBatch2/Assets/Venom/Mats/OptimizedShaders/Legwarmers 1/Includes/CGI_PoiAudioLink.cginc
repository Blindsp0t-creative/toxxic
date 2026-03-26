#ifndef POI_AUDIOLINK
#define POI_AUDIOLINK

UNITY_DECLARE_TEX2D(_AudioTexture);
float4 _AudioTexture_ST;
fixed _AudioLinkDelay;
fixed _AudioLinkAveraging;
fixed _AudioLinkAverageRange;
void AudioTextureExists()
{
    half testw = 0;
    half testh = 0;
    _AudioTexture.GetDimensions(testw, testh);
    poiMods.audioLinkTextureExists = testw > 16;
}

float getBandAtTime(float band, fixed time)
{
    return UNITY_SAMPLE_TEX2D(_AudioTexture, float2(time, band * .25 + .125)).r;
}

void initAudioBands()
{
    AudioTextureExists();

    if (poiMods.audioLinkTextureExists)
    {
        poiMods.audioLink.x = UNITY_SAMPLE_TEX2D(_AudioTexture, float2(float(0), .125));
        poiMods.audioLink.y = UNITY_SAMPLE_TEX2D(_AudioTexture, float2(float(0), .375));
        poiMods.audioLink.z = UNITY_SAMPLE_TEX2D(_AudioTexture, float2(float(0), .625));
        poiMods.audioLink.w = UNITY_SAMPLE_TEX2D(_AudioTexture, float2(float(0), .875));
    }

    
    if (float(0))
    {
        float uv = saturate(float(0) + float(0.5) * .25);
        poiMods.audioLink.x += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .125));
        poiMods.audioLink.y += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .375));
        poiMods.audioLink.z += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .625));
        poiMods.audioLink.w += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .875));

        uv = saturate(float(0) + float(0.5) * .5);
        poiMods.audioLink.x += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .125));
        poiMods.audioLink.y += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .375));
        poiMods.audioLink.z += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .625));
        poiMods.audioLink.w += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .875));

        uv = saturate(float(0) + float(0.5) * .75);
        poiMods.audioLink.x += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .125));
        poiMods.audioLink.y += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .375));
        poiMods.audioLink.z += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .625));
        poiMods.audioLink.w += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .875));

        uv = saturate(float(0) + float(0.5));
        poiMods.audioLink.x += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .125));
        poiMods.audioLink.y += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .375));
        poiMods.audioLink.z += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .625));
        poiMods.audioLink.w += UNITY_SAMPLE_TEX2D(_AudioTexture, float2(uv, .875));

        poiMods.audioLink /= 5;
    }
}

#endif
