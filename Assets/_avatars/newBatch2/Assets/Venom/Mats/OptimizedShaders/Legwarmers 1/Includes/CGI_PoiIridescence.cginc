#ifndef POI_IRIDESCENCE
    #define POI_IRIDESCENCE
    #if defined(PROP_IRIDESCENCERAMP) || !defined(OPTIMIZER_ENABLED)
        UNITY_DECLARE_TEX2D_NOSAMPLER(_IridescenceRamp); float4 _IridescenceRamp_ST;
    #endif
    #if defined(PROP_IRIDESCENCEMASK) || !defined(OPTIMIZER_ENABLED)
        UNITY_DECLARE_TEX2D_NOSAMPLER(_IridescenceMask); float4 _IridescenceMask_ST;
    #endif
    #if defined(PROP_IRIDESCENCENORMALMAP) || !defined(OPTIMIZER_ENABLED)
        UNITY_DECLARE_TEX2D_NOSAMPLER(_IridescenceNormalMap); float4 _IridescenceNormalMap_ST;
    #endif
    float _IridescenceNormalUV;
    float _IridescenceMaskUV;
    float _IridescenceNormalSelection;
    float _IridescenceNormalIntensity;
    float _IridescenceNormalToggle;
    float _IridescenceIntensity;
    fixed _IridescenceAddBlend;
    fixed _IridescenceReplaceBlend;
    fixed _IridescenceMultiplyBlend;
    float _IridescenceEmissionStrength;
    
    //global
    #if defined(PROP_IRIDESCENCENORMALMAP) || !defined(OPTIMIZER_ENABLED)
        float3 calculateNormal(float3 baseNormal)
        {
            
            float3 normal = UnpackScaleNormal(UNITY_SAMPLE_TEX2D_SAMPLER(_IridescenceNormalMap, _MainTex, TRANSFORM_TEX(poiMesh.uv[float(0)], _IridescenceNormalMap)), float(1));
            return normalize(
                normal.x * poiMesh.tangent +
                normal.y * poiMesh.binormal +
                normal.z * baseNormal
            );
        }
    #endif
    void applyIridescence(inout float4 albedo, inout float3 IridescenceEmission)
    {
        float3 normal = poiMesh.normals[float(1)];
        
        #if defined(PROP_IRIDESCENCENORMALMAP) || !defined(OPTIMIZER_ENABLED)
            // Use custom normal map
            
            if (float(0))
            {
                normal = calculateNormal(normal);
            }
        #endif
        
        float ndotv = dot(normal, poiCam.viewDir);
        
        #if defined(PROP_IRIDESCENCERAMP) || !defined(OPTIMIZER_ENABLED)
            float4 iridescenceColor = UNITY_SAMPLE_TEX2D_SAMPLER(_IridescenceRamp, _MainTex, 1 - abs(ndotv));
        #else
            float4 iridescenceColor = 1;
        #endif
        
        #if defined(PROP_IRIDESCENCEMASK) || !defined(OPTIMIZER_ENABLED)
            float4 iridescenceMask = UNITY_SAMPLE_TEX2D_SAMPLER(_IridescenceMask, _MainTex, TRANSFORM_TEX(poiMesh.uv[float(0)], _IridescenceMask));
        #else
            float4 iridescenceMask = 1;
        #endif
        #ifdef POI_BLACKLIGHT
            if(float(4) != 4)
            {
                iridescenceMask *= blackLightMask[float(4)];
            }
        #endif
        
        
        albedo.rgb = lerp(albedo.rgb, saturate(iridescenceColor.rgb * float(1)), iridescenceColor.a * float(0) * iridescenceMask);
        albedo.rgb += saturate(iridescenceColor.rgb * float(1) * iridescenceColor.a * float(0) * iridescenceMask);
        albedo.rgb *= saturate(lerp(1, iridescenceColor.rgb * float(1), iridescenceColor.a * float(0) * iridescenceMask));
        
        IridescenceEmission = saturate(iridescenceColor.rgb * float(1)) * iridescenceColor.a * iridescenceMask * float(0);
    }
    
#endif
