#ifndef MATCAP
    #define MATCAP
    #if defined(PROP_MATCAP) || !defined(OPTIMIZER_ENABLED)
        UNITY_DECLARE_TEX2D_NOSAMPLER(_Matcap); float4 _Matcap_ST;
    #endif
    #if defined(PROP_MATCAPMASK) || !defined(OPTIMIZER_ENABLED)
        POI_TEXTURE_NOSAMPLER(_MatcapMask);
    #endif
    float _MatcapMaskInvert;
    float _MatcapBorder;
    float4 _MatcapColor;
    float _MatcapIntensity;
    float _MatcapReplace;
    float _MatcapMultiply;
    float _MatcapAdd;
    float _MatcapEnable;
    float _MatcapLightMask;
    float _MatcapEmissionStrength;
    float _MatcapNormal;
    float _MatcapHueShiftEnabled;
    float _MatcapHueShiftSpeed;
    float _MatcapHueShift;
    #ifdef COLOR_GRADING_HDR_3D
        #if defined(PROP_MATCAP2) || !defined(OPTIMIZER_ENABLED)
            UNITY_DECLARE_TEX2D_NOSAMPLER(_Matcap2);float4 _Matcap2_ST;
        #endif
        #if defined(PROP_MATCAP2MASK) || !defined(OPTIMIZER_ENABLED)
            POI_TEXTURE_NOSAMPLER(_Matcap2Mask);
        #endif
        float _Matcap2MaskInvert;
        float _Matcap2Border;
        float4 _Matcap2Color;
        float _Matcap2Intensity;
        float _Matcap2Replace;
        float _Matcap2Multiply;
        float _Matcap2Add;
        float _Matcap2Enable;
        float _Matcap2LightMask;
        float _Matcap2EmissionStrength;
        float _Matcap2Normal;
        float _Matcap2HueShiftEnabled;
        float _Matcap2HueShiftSpeed;
        float _Matcap2HueShift;
    #endif
    void blendMatcap(inout float4 finalColor, float add, float multiply, float replace, float4 matcapColor, float matcapMask, inout float3 matcapEmission, float emissionStrength
    #ifdef POI_LIGHTING
    , float matcapLightMask
    #endif
    #ifdef POI_BLACKLIGHT
    , uint blackLightMaskIndex
    #endif
    )
    {
        #ifdef POI_LIGHTING
            if (matcapLightMask)
            {
                matcapMask *= lerp(1, poiLight.rampedLightMap, matcapLightMask);
            }
        #endif
        #ifdef POI_BLACKLIGHT
            if(blackLightMaskIndex != 4)
            {
                matcapMask *= blackLightMask[blackLightMaskIndex];
            }
        #endif
        finalColor.rgb = lerp(finalColor.rgb, matcapColor.rgb, replace * matcapMask * matcapColor.a * .999999);
        finalColor.rgb *= lerp(1, matcapColor.rgb, multiply * matcapMask * matcapColor.a);
        finalColor.rgb += matcapColor.rgb * add * matcapMask * matcapColor.a;
        matcapEmission += matcapColor.rgb * emissionStrength * matcapMask * matcapColor.a;
    }
    void applyMatcap(inout float4 finalColor, inout float3 matcapEmission)
    {
        float4 matcap = 0;
        float matcapMask = 0;
        float4 matcap2 = 0;
        float matcap2Mask = 0;
        half3 worldViewUp = normalize(half3(0, 1, 0) - poiCam.viewDir * dot(poiCam.viewDir, half3(0, 1, 0)));
        half3 worldViewRight = normalize(cross(poiCam.viewDir, worldViewUp));
        half2 matcapUV = half2(dot(worldViewRight, poiMesh.normals[(1.0 /*_MatcapNormal*/)]), dot(worldViewUp, poiMesh.normals[(1.0 /*_MatcapNormal*/)])) * (0.43 /*_MatcapBorder*/) + 0.5;
        #if defined(PROP_MATCAP) || !defined(OPTIMIZER_ENABLED)
            matcap = UNITY_SAMPLE_TEX2D_SAMPLER(_Matcap, _MainTex, TRANSFORM_TEX(matcapUV, _Matcap)) * float4(1,1,1,1);
        #else
            matcap = float4(1,1,1,1);
        #endif
        matcap.rgb *= (1.0 /*_MatcapIntensity*/);
        #if defined(PROP_MATCAPMASK) || !defined(OPTIMIZER_ENABLED)
            matcapMask = POI2D_SAMPLER_PAN(_MatcapMask, _MainTex, poiMesh.uv[(0.0 /*_MatcapMaskUV*/)], float4(0,0,0,0));
        #else
            matcapMask = 1;
        #endif
        if ((0.0 /*_MatcapMaskInvert*/))
        {
            matcapMask = 1 - matcapMask;
        }
        
        if((0.0 /*_MatcapHueShiftEnabled*/))
        {
            matcap.rgb = hueShift(matcap.rgb, (0.0 /*_MatcapHueShift*/) + _Time.x * (0.0 /*_MatcapHueShiftSpeed*/));
        }
        blendMatcap(finalColor, (1.0 /*_MatcapAdd*/), (0.265 /*_MatcapMultiply*/), (0.427 /*_MatcapReplace*/), matcap, matcapMask, matcapEmission, (0.0 /*_MatcapEmissionStrength*/)
        #ifdef POI_LIGHTING
        , (0.0 /*_MatcapLightMask*/)
        #endif
        #ifdef POI_BLACKLIGHT
        , _BlackLightMaskMatcap
        #endif
        );
        #ifdef COLOR_GRADING_HDR_3D
            half2 matcapUV2 = half2(dot(worldViewRight, poiMesh.normals[(1.0 /*_Matcap2Normal*/)]), dot(worldViewUp, poiMesh.normals[(1.0 /*_Matcap2Normal*/)])) * (0.5 /*_Matcap2Border*/) + 0.5;
            #if defined(PROP_MATCAP2) || !defined(OPTIMIZER_ENABLED)
                matcap2 = UNITY_SAMPLE_TEX2D_SAMPLER(_Matcap2, _MainTex, TRANSFORM_TEX(matcapUV2, _Matcap2)) * float4(0.1554452,1,0,1);
            #else
                matcap2 = float4(0.1554452,1,0,1);
            #endif
            matcap2.rgb *= (3.25 /*_Matcap2Intensity*/);
            #if defined(PROP_MATCAP2MASK) || !defined(OPTIMIZER_ENABLED)
                matcap2Mask = POI2D_SAMPLER_PAN(_Matcap2Mask, _MainTex, poiMesh.uv[(0.0 /*_Matcap2MaskUV*/)], float4(0,0,0,0));
            #else
                matcap2Mask = 1;
            #endif
            if ((0.0 /*_Matcap2MaskInvert*/))
            {
                matcap2Mask = 1 - matcap2Mask;
            }
            
            if((1.0 /*_Matcap2HueShiftEnabled*/))
            {
                matcap2.rgb = hueShift(matcap2.rgb, _Matcap2HueShift + _Time.x * (0.0 /*_Matcap2HueShiftSpeed*/));
            }
            blendMatcap(finalColor, (1.0 /*_Matcap2Add*/), (1.0 /*_Matcap2Multiply*/), (1.0 /*_Matcap2Replace*/), matcap2, matcap2Mask, matcapEmission, (0.0 /*_Matcap2EmissionStrength*/)
            #ifdef POI_LIGHTING
            , (0.0 /*_Matcap2LightMask*/)
            #endif
            #ifdef POI_BLACKLIGHT
            , _BlackLightMaskMatcap2
            #endif
            );
        #endif
    }
#endif
