#ifndef POI_FLIPBOOK
#define POI_FLIPBOOK
#if defined(PROP_FLIPBOOKTEXARRAY) || !defined(OPTIMIZER_ENABLED)
    UNITY_DECLARE_TEX2DARRAY(_FlipbookTexArray); float4 _FlipbookTexArray_ST;
#endif
#if defined(PROP_FLIPBOOKMASK) || !defined(OPTIMIZER_ENABLED)
    POI_TEXTURE_NOSAMPLER(_FlipbookMask);
#endif
float4 _FlipbookColor;
float _FlipbookFPS;
float _FlipbookTotalFrames;
float4 _FlipbookScaleOffset;
float _FlipbookTiled;
float _FlipbookCurrentFrame;
float _FlipbookEmissionStrength;
float _FlipbookRotation;
float _EnableFlipbook;
float _FlipbookTexArrayUV;
float _FlipbookAlphaControlsFinalAlpha;
float _FlipbookRotationSpeed;
float _FlipbookIntensityControlsAlpha;
float _FlipbookColorReplaces;
float2 _FlipbookTexArrayPan;
float _FlipbookReplace;
float _FlipbookMultiply;
float _FlipbookAdd;
float _FlipbookMovementType;
float4 _FlipbookStartEndOffset;
float _FlipbookMovementSpeed;
float _FlipbookCrossfadeEnabled;
float2 _FlipbookCrossfadeRange;
float _FlipbookHueShiftEnabled;
float _FlipbookHueShiftSpeed;
float _FlipbookHueShift;
float4 flipBookPixel;
float4 flipBookPixelMultiply;
float flipBookMask;
half _AudioLinkFlipbookScaleBand;
half4 _AudioLinkFlipbookScale;
half _AudioLinkFlipbookAlphaBand;
half2 _AudioLinkFlipbookAlpha;
half _AudioLinkFlipbookEmissionBand;
half2 _AudioLinkFlipbookEmission;
half _AudioLinkFlipbookFrameBand;
half2 _AudioLinkFlipbookFrame;
#ifndef POI_SHADOW
    void applyFlipbook(inout float4 finalColor, inout float3 flipbookEmission)
    {
        #if defined(PROP_FLIPBOOKMASK) || !defined(OPTIMIZER_ENABLED)
            flipBookMask = POI2D_SAMPLER_PAN(_FlipbookMask, _MainTex, poiMesh.uv[(0.0 /*_FlipbookMaskUV*/)], float4(0,0,0,0)).r;
        #else
            flipBookMask = 1;
        #endif
        float4 flipbookScaleOffset = float4(1.09,1,0,0);
        #ifdef POI_AUDIOLINK
            flipbookScaleOffset.xy += lerp(float4(0,0,0,0).xy, float4(0,0,0,0).zw, poiMods.audioLink[(0.0 /*_AudioLinkFlipbookScaleBand*/)]);
        #endif
        flipbookScaleOffset.xy = 1 - flipbookScaleOffset.xy;
        float2 uv = frac(poiMesh.uv[(0.0 /*_FlipbookTexArrayUV*/)]);
        float theta = radians((0.0 /*_FlipbookRotation*/) + _Time.z * (0.0 /*_FlipbookRotationSpeed*/));
        float cs = cos(theta);
        float sn = sin(theta);
        float2 spriteCenter = flipbookScaleOffset.zw + .5;
        uv = float2((uv.x - spriteCenter.x) * cs - (uv.y - spriteCenter.y) * sn + spriteCenter.x, (uv.x - spriteCenter.x) * sn + (uv.y - spriteCenter.y) * cs + spriteCenter.y);
        float2 newUV = remap(uv, float2(0, 0) + flipbookScaleOffset.xy / 2 + flipbookScaleOffset.zw, float2(1, 1) - flipbookScaleOffset.xy / 2 + flipbookScaleOffset.zw, float2(0, 0), float2(1, 1));
        
        if ((0.0 /*_FlipbookTiled*/) == 0)
        {
            if (max(newUV.x, newUV.y) > 1 || min(newUV.x, newUV.y) < 0)
            {
                flipBookPixel = 0;
                return;
            }
        }
        #if defined(PROP_FLIPBOOKTEXARRAY) || !defined(OPTIMIZER_ENABLED)
            float currentFrame = fmod((-1.0 /*_FlipbookCurrentFrame*/), (50.0 /*_FlipbookTotalFrames*/));
            if ((-1.0 /*_FlipbookCurrentFrame*/) < 0)
            {
                currentFrame = (_Time.y / (1 / (17.0 /*_FlipbookFPS*/))) % (50.0 /*_FlipbookTotalFrames*/);
            }
            #ifdef POI_AUDIOLINK
                currentFrame += lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[(0.0 /*_AudioLinkFlipbookFrameBand*/)]);
            #endif
            flipBookPixel = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(TRANSFORM_TEX(newUV, _FlipbookTexArray) + _Time.x * float4(0,0,0,0), floor(currentFrame)));
            
            if ((0.0 /*_FlipbookCrossfadeEnabled*/))
            {
                float4 flipbookNextPixel = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(TRANSFORM_TEX(newUV, _FlipbookTexArray) + _Time.x * float4(0,0,0,0), floor((currentFrame + 1) % (50.0 /*_FlipbookTotalFrames*/))));
                flipBookPixel = lerp(flipBookPixel, flipbookNextPixel, smoothstep(float4(0.75,1,0,1).x, float4(0.75,1,0,1).y, frac(currentFrame)));
            }
        #else
            flipBookPixel = 1;
        #endif
        
        if ((0.0 /*_FlipbookIntensityControlsAlpha*/))
        {
            flipBookPixel.a = poiMax(flipBookPixel.rgb);
        }
        
        if ((0.0 /*_FlipbookColorReplaces*/))
        {
            flipBookPixel.rgb = float4(1,1,1,1).rgb;
        }
        else
        {
            flipBookPixel.rgb *= float4(1,1,1,1).rgb;
        }
        #ifdef POI_BLACKLIGHT
            
            if (_BlackLightMaskFlipbook != 4)
            {
                flipBookMask *= blackLightMask[_BlackLightMaskFlipbook];
            }
        #endif
        
        if ((0.0 /*_FlipbookHueShiftEnabled*/))
        {
            flipBookPixel.rgb = hueShift(flipBookPixel.rgb, (0.0 /*_FlipbookHueShift*/) + _Time.x * (0.0 /*_FlipbookHueShiftSpeed*/));
        }
        half flipbookAlpha = 1;
        #ifdef POI_AUDIOLINK
            flipbookAlpha = saturate(lerp(float4(1,1,0,0).x, float4(1,1,0,0).y, poiMods.audioLink[(0.0 /*_AudioLinkFlipbookAlphaBand*/)]));
        #endif
        finalColor.rgb = lerp(finalColor.rgb, flipBookPixel.rgb, flipBookPixel.a * float4(1,1,1,1).a * (1.0 /*_FlipbookReplace*/) * flipBookMask * flipbookAlpha);
        finalColor.rgb = finalColor + flipBookPixel.rgb * (0.0 /*_FlipbookAdd*/) * flipBookMask * flipbookAlpha;
        finalColor.rgb = finalColor * lerp(1, flipBookPixel.rgb, flipBookPixel.a * float4(1,1,1,1).a * flipBookMask * (0.0 /*_FlipbookMultiply*/) * flipbookAlpha);
        
        if ((0.0 /*_FlipbookAlphaControlsFinalAlpha*/))
        {
            finalColor.a = lerp(finalColor.a, flipBookPixel.a * float4(1,1,1,1).a, flipBookMask);
        }
        float flipbookEmissionStrength = (0.48 /*_FlipbookEmissionStrength*/);
        #ifdef POI_AUDIOLINK
            flipbookEmissionStrength += max(lerp(float4(0,0,0,0).x, float4(0,0,0,0).y, poiMods.audioLink[(0.0 /*_AudioLinkFlipbookEmissionBand*/)]), 0);
        #endif
        flipbookEmission = lerp(0, flipBookPixel.rgb * flipbookEmissionStrength, flipBookPixel.a * float4(1,1,1,1).a * flipBookMask * flipbookAlpha);
    }
#else
    float applyFlipbookAlphaToShadow(float2 uv)
    {
        
        if ((0.0 /*_FlipbookAlphaControlsFinalAlpha*/))
        {
            float flipbookShadowAlpha = 0;
            float4 flipbookScaleOffset = float4(1.09,1,0,0);
            flipbookScaleOffset.xy = 1 - flipbookScaleOffset.xy;
            float theta = radians((0.0 /*_FlipbookRotation*/));
            float cs = cos(theta);
            float sn = sin(theta);
            float2 spriteCenter = flipbookScaleOffset.zw + .5;
            uv = float2((uv.x - spriteCenter.x) * cs - (uv.y - spriteCenter.y) * sn + spriteCenter.x, (uv.x - spriteCenter.x) * sn + (uv.y - spriteCenter.y) * cs + spriteCenter.y);
            float2 newUV = remap(uv, float2(0, 0) + flipbookScaleOffset.xy / 2 + flipbookScaleOffset.zw, float2(1, 1) - flipbookScaleOffset.xy / 2 + flipbookScaleOffset.zw, float2(0, 0), float2(1, 1));
            #if defined(PROP_FLIPBOOKTEXARRAY) || !defined(OPTIMIZER_ENABLED)
                float currentFrame = fmod((-1.0 /*_FlipbookCurrentFrame*/), (50.0 /*_FlipbookTotalFrames*/));
                if ((-1.0 /*_FlipbookCurrentFrame*/) < 0)
                {
                    currentFrame = (_Time.y / (1 / (17.0 /*_FlipbookFPS*/))) % (50.0 /*_FlipbookTotalFrames*/);
                }
                half4 flipbookColor = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(TRANSFORM_TEX(newUV, _FlipbookTexArray) + _Time.x * float4(0,0,0,0), floor(currentFrame)));
                
                if ((0.0 /*_FlipbookCrossfadeEnabled*/))
                {
                    float4 flipbookNextPixel = UNITY_SAMPLE_TEX2DARRAY(_FlipbookTexArray, float3(TRANSFORM_TEX(newUV, _FlipbookTexArray) + _Time.x * float4(0,0,0,0), floor((currentFrame + 1) % (50.0 /*_FlipbookTotalFrames*/))));
                    flipbookColor = lerp(flipbookColor, flipbookNextPixel, smoothstep(float4(0.75,1,0,1).x, float4(0.75,1,0,1).y, frac(currentFrame)));
                }
            #else
                half4 flipbookColor = 1;
            #endif
            if ((0.0 /*_FlipbookIntensityControlsAlpha*/))
            {
                flipbookColor.a = poiMax(flipbookColor.rgb);
            }
            
            if ((0.0 /*_FlipbookTiled*/) == 0)
            {
                if (max(newUV.x, newUV.y) > 1 || min(newUV.x, newUV.y) < 0)
                {
                    flipbookColor.a = 0;
                }
            }
            return flipbookColor.a * float4(1,1,1,1).a;
        }
        return 1;
    }
#endif
#endif
