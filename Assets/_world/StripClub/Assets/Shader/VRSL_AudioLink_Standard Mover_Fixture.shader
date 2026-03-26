Shader "VRSL/AudioLink/Standard Mover/Fixture" {
	Properties {
		[Enum(Legacy, 0, GGX, 1)] _LightingModel ("Lighting Model", Float) = 0
		[Toggle] [HideInInspector] _PanInvert ("Invert Mover Pan", Float) = 0
		[Toggle] [HideInInspector] _TiltInvert ("Invert Mover Tilt", Float) = 0
		[HideInInspector] _FixtureBaseRotationY ("Mover Pan Offset (Blue + Green)", Range(-540, 540)) = 0
		[HideInInspector] _FixtureRotationX ("Mover Tilt Offset (Blue)", Range(-180, 180)) = 0
		[HideInInspector] _ProjectionSelection ("GOBO Selection", Range(0, 6)) = 0
		[HideInInspector] _StrobeFreq ("Strobe Frequency", Range(0, 25)) = 1
		[Toggle] [HideInInspector] _EnableSpin ("Enable Auto Spinning", Float) = 0
		[Header(Audio Section)] [Toggle] _EnableAudioLink ("Enable Audio Link", Float) = 0
		[Toggle] _EnableColorChord ("Enable Color Chord Tinting", Float) = 0
		_Band ("Band", Float) = 0
		_BandMultiplier ("Band Multiplier", Range(1, 15)) = 1
		_Delay ("Delay", Float) = 0
		_NumBands ("Num Bands", Float) = 4
		_AudioSpectrum ("AudioSpectrum", 2D) = "black" {}
		[Toggle] _UseTraditionalSampling ("Use Traditional Texture Sampling", Float) = 0
		_RenderTextureMultiplier ("Render Texture Multiplier", Range(1, 10)) = 1
		_FinalIntensity ("Final Intensity", Range(0, 1)) = 1
		_GlobalIntensity ("Global Intensity", Range(0, 1)) = 1
		_GlobalIntensityBlend ("Global Intensity Blend", Range(0, 1)) = 1
		_UniversalIntensity ("Universal Intensity", Range(0, 1)) = 1
		[HDR] _Emission ("Light Color Tint", Vector) = (1,1,1,1)
		_Saturation ("Final Saturation", Range(0, 1)) = 1
		_SaturationLength ("Final Saturation Length", Range(0, 0.2)) = 0.1
		_LensMaxBrightness ("Lens Max Brightness", Range(0, 20)) = 5
		_ConeWidth ("Cone Width", Range(0, 5.5)) = 0
		_ConeLength ("Cone Length", Range(1, 10)) = 1
		_ConeSync ("Cone Scale Sync", Range(0, 1)) = 0.2
		_FixutreIntensityMultiplier ("Intensity Multipler (For Bloom Scaling)", Range(1, 10)) = 1
		[Toggle] _EnableColorTextureSample ("Enable Color Texture Sampling", Float) = 0
		_SamplingTexture ("Texture To Sample From for Color", 2D) = "white" {}
		_TextureColorSampleX ("X coordinate to sample the texture from", Range(0, 1)) = 0.5
		_TextureColorSampleY ("Y coordinate to sample the texture from", Range(0, 1)) = 0.5
		_FixtureRotationOrigin ("Fixture Pivot Origin", Vector) = (0,0.014709,-1.02868,0)
		_MaxMinPanAngle ("Max/Min Pan Angle (-x, x)", Float) = 180
		_MaxMinTiltAngle ("Max/Min Tilt Angle (-y, y)", Float) = 180
		_FixtureMaxIntensity ("Maximum Cone Intensity", Range(0, 0.5)) = 0.5
		[Toggle] _EnableThemeColorSampling ("Enable Theme Color Sampling", Float) = 0
		_ThemeColorTarget ("Choose Theme Color", Float) = 0
		[Enum(Unity Default, 0, Non Linear, 1)] _LightProbeMethod ("Light Probe Sampling", Float) = 0
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Scale", Range(-1, 1)) = 1
		_MetallicGlossMap ("Metallic Map", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0
		_OcclusionMap ("Occlusion Map", 2D) = "white" {}
		_OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 0
		_DecorativeEmissiveMap ("Decorative Emissive Map", 2D) = "black" {}
		_DecorativeEmissiveMapStrength ("Decorative Emissive Map Strength", Range(0, 1)) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	//CustomEditor "VRSLInspector"
}