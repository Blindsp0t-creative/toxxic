Shader "VRSL/AudioLink/Standard Mover/Projection" {
	Properties {
		[Toggle] [HideInInspector] _PanInvert ("Invert Mover Pan", Float) = 0
		[Toggle] [HideInInspector] _TiltInvert ("Invert Mover Tilt", Float) = 0
		[HideInInspector] _FixtureBaseRotationY ("Mover Pan Offset (Blue + Green)", Range(-540, 540)) = 0
		[HideInInspector] _FixtureRotationX ("Mover Tilt Offset (Blue)", Range(-180, 180)) = 0
		[HideInInspector] _ProjectionSelection ("GOBO Selection", Range(1, 6)) = 1
		[Toggle] [HideInInspector] _EnableSpin ("Enable Auto Spinning", Float) = 0
		[Header(Audio Section)] [Toggle] _EnableAudioLink ("Enable Audio Link", Float) = 0
		[Toggle] _EnableColorChord ("Enable Color Chord Tinting", Float) = 0
		[Enum(Bass,0,Low Mids,1,High Mids,2,Treble,3)] _Band ("Band", Float) = 0
		_BandMultiplier ("Band Multiplier", Range(1, 15)) = 1
		_Delay ("Delay", Float) = 0
		_NumBands ("Num Bands", Float) = 4
		_AudioSpectrum ("AudioSpectrum", 2D) = "black" {}
		_FinalIntensity ("Final Intensity", Range(0, 1)) = 1
		_GlobalIntensity ("Global Intensity", Range(0, 1)) = 1
		_GlobalIntensityBlend ("Global Intensity Blend", Range(0, 1)) = 1
		_UniversalIntensity ("Universal Intensity", Range(0, 1)) = 1
		[HDR] _Emission ("Light Color Tint", Vector) = (1,1,1,1)
		[HDR] _StaticEmission ("Static Light Color Tint", Vector) = (1,1,1,1)
		[Toggle] _EnableStaticEmissionColor ("Enable Static Emission Color", Float) = 0
		_Saturation ("Final Saturation", Range(0, 1)) = 1
		_SaturationLength ("Final Saturation Length", Range(0, 0.2)) = 0.1
		_LensMaxBrightness ("Lens Max Brightness", Range(0.01, 10)) = 5
		_ConeWidth ("Cone Width", Range(0, 5.5)) = 0
		_ConeLength ("Cone Length", Range(1, 10)) = 1
		_ConeSync ("Cone Scale Sync", Range(1, 2)) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _BlendSrc ("Source Blend mode", Float) = 2
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
		_RenderTextureMultiplier ("Render Texture Multiplier", Range(1, 10)) = 1
		[Toggle] _EnableColorTextureSample ("Enable Color Texture Sampling", Float) = 0
		_SamplingTexture ("Texture To Sample From for Color", 2D) = "white" {}
		_TextureColorSampleX ("X coordinate to sample the texture from", Range(0, 1)) = 0.5
		_TextureColorSampleY ("Y coordinate to sample the texture from", Range(0, 1)) = 0.5
		_FixtureRotationOrigin ("Fixture Pivot Origin", Vector) = (0,0.014709,-1.02868,0)
		_MaxMinPanAngle ("Max/Min Pan Angle (-x, x)", Float) = 180
		_MaxMinTiltAngle ("Max/Min Tilt Angle (-y, y)", Float) = 180
		_FixtureMaxIntensity ("Maximum Cone Intensity", Range(0, 0.5)) = 0.5
		[Toggle] _UseTraditionalSampling ("Use Traditional Texture Sampling", Float) = 0
		_ProjectionRotation ("Static Projection UV Rotation", Range(-180, 180)) = 0
		_SpinSpeed ("Auto Spin Speed", Range(0, 10)) = 0
		_ProjectionIntensity ("Projection Intensity", Range(0, 20)) = 0
		_ProjectionFade ("Projection Edge Fade", Range(0, 10)) = 0
		_ProjectionFadeCurve ("Projection Edge Fade Harshness", Range(0, 10)) = 1
		_ProjectionDistanceFallOff ("Projection Distance Fallof Strength", Range(0.001, 0.5)) = 0.05
		_ProjectionRange ("Projection Drawing Range", Range(0, 10)) = 0
		_ProjectionRangeOrigin ("Projection Drawing Range Scale Origin", Vector) = (0,-0.07535,0.12387,0)
		_ProjectionShadowHarshness ("Projection Shadow Harshness", Range(0, 1)) = 0
		[NoScaleOffset] _ProjectionMainTex ("Projection Texture GOBO 1", 2D) = "white" {}
		_ProjectionUVMod ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod2 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod3 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod4 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod5 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod6 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod7 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_ProjectionUVMod8 ("Projection UV Scale Modifier ", Range(0.01, 10)) = 0
		_RedMultiplier ("Red Channel Multiplier", Range(0, 5)) = 1
		_GreenMultiplier ("Green Channel Multiplier", Range(0, 5)) = 1
		_BlueMultiplier ("Blue Channel Multiplier", Range(0, 5)) = 1
		_ProjectionCutoff ("Projection Cutoff Point", Range(0, 1)) = 0.25
		_ProjectionOriginCutoff ("Projection Origin Cutoff Point", Range(0, 3)) = 0.25
		[Toggle] _EnableThemeColorSampling ("Enable Theme Color Sampling", Float) = 0
		_ThemeColorTarget ("Choose Theme Color", Float) = 0
		[Enum(Transparent,1,AlphaToCoverage,2)] _RenderMode ("Render Mode", Float) = 1
		[Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 0
		[Enum(Off,0,On,1)] _AlphaToCoverage ("Alpha To Coverage", Float) = 0
		[Enum(Off,0,One,1)] _BlendDst ("Destination Blend mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
		_ClippingThreshold ("Clipping Threshold", Range(0, 1)) = 0.5
		_MinimumBeamRadius ("Minimum Beam Radius", Range(0.001, 1)) = 1
		[Enum(Off,0,On,1)] _MultiSampleDepth ("Multi Sample Depth", Float) = 1
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
		}
		ENDCG
	}
	//CustomEditor "VRSLInspector"
}