#ifndef POI_FUR
    #define POI_FUR
    
    UNITY_DECLARE_TEX2D_NOSAMPLER(_FurTexture); float4 _FurTexture_ST;
    UNITY_DECLARE_TEX2D_NOSAMPLER(_FurHeightMap); float4 _FurHeightMap_ST;
    UNITY_DECLARE_TEX2D_NOSAMPLER(_FurMask); float4 _FurMask_ST;
    float _FurAO;
    float4 _FurColor;
    float _FurLayers;
    float _FurMinDistance;
    float _FurMaxDistance;
    float _FurHeightMapMin;
    float _FurHeightMapMax;
    float _FurDebug;
    float _AoRampMin;
    float _AoRampMax;
    float _FurTipAlpha;
    float _FurFadeStart;
    
    //globals
    half furHeightMap;
    half furMask;
    
    float4 calculateFur(float4 albedo, float4 mainTexture)
    {
        
        furHeightMap = UNITY_SAMPLE_TEX2D_SAMPLER(_FurHeightMap, _MainTex, TRANSFORM_TEX(poiMesh.uv[0], _FurHeightMap)).x;
        furHeightMap = remap(furHeightMap, 0, float(1), float(0), 1);
        furMask = poiMax(UNITY_SAMPLE_TEX2D_SAMPLER(_FurMask, _MainTex, TRANSFORM_TEX(poiMesh.uv[0], _FurMask)).xyz);
        half3 furTexture = UNITY_SAMPLE_TEX2D_SAMPLER(_FurTexture, _MainTex, TRANSFORM_TEX(poiMesh.uv[0], _FurTexture));
        clip(furHeightMap - poiMesh.furAlpha);
        clip(furMask - poiMesh.furAlpha);
        
        albedo.rgb = lerp(mainTexture.rgb, furTexture.rgb * float4(1,1,1,1).rgb, ceil(poiMesh.furAlpha));
        albedo.rgb *= lerp(1, smoothstep(float(0), float(1), furHeightMap), float(1) * furMask);
        
        albedo.a *= smoothstep(1.01, float(1), remapClamped(poiMesh.furAlpha, float(0), 1, 0, 1)) + float(0);
		return albedo;
    }
    
#endif
