Shader "VRSL/Basic Laser/AudioLink" {
	Properties {
		[Toggle] _EnableColorChord ("Enable Color Chord Tinting", Float) = 0
		[Toggle] _EnableColorTextureSample ("Enable Color Texture Sampling", Float) = 0
		_SamplingTexture ("Texture To Sample From for Color", 2D) = "white" {}
		_TextureColorSampleX ("X coordinate to sample the texture from", Range(0, 1)) = 0.5
		_TextureColorSampleY ("Y coordinate to sample the texture from", Range(0, 1)) = 0.5
		[Toggle] _EnableAudioLink ("Enable Audio Link", Float) = 0
		_Band ("AudioLink Band", Float) = 0
		_BandMultiplier ("AudioLink Multiplier", Float) = 1
		_Delay ("Audio Link Delay", Float) = 0
		_RenderTextureMultiplier ("Render Texture Multiplier", Range(1, 10)) = 1
		_UniversalIntensity ("Universal Intensity", Range(0, 1)) = 1
		_FinalIntensity ("Final Intensity", Range(0, 1)) = 1
		_GlobalIntensity ("Global Intensity", Range(0, 1)) = 1
		_GlobalIntensityBlend ("Global Intensity Blend", Range(0, 1)) = 1
		[HideInInspector] _MainTex ("Texture", 2D) = "white" {}
		[HDR] _Emission ("Emission Color", Vector) = (1,1,1,1)
		_Multiplier ("Emission Multiplier", Range(1, 10)) = 1
		_VertexConeWidth ("Cone Width", Range(-3.75, 20)) = 0
		_VertexConeLength ("Cone Length", Range(-0.5, 5)) = 0
		_ZConeFlatness ("Z Flatness", Range(0, 1.999)) = 0
		_ZConeFlatnessAlt ("Z Flatness Alt", Range(0, 1.999)) = 0
		_ZRotation ("Z Rotation", Range(-90, 90)) = 0
		_XRotation ("X Rotation", Range(-90, 90)) = 0
		_YRotation ("Y Rotation", Range(-180, 180)) = 0
		_AltZRotation ("Alt Z Rotation", Range(-90, 90)) = 0
		_AltXRotation ("Alt X Rotation", Range(-90, 90)) = 0
		_AltYRotation ("Alt Y Rotation", Range(-180, 180)) = 0
		[IntRange] _LaserCount ("Laser Beam Count", Range(4, 68)) = 1
		_LaserThickness ("Laser Beam Thickenss", Range(0.003, 0.25)) = 1
		_EndFade ("End Fade", Range(0, 3)) = 2.2
		_FadeStrength ("Cone Edge Fade", Range(1, 2)) = 0
		_LaserSoftening ("Laser Softness", Range(0.05, 5)) = 5
		_InternalShine ("Internal Shine Strength", Range(0, 5)) = 1
		_InternalShineLength ("Internal Shine Length", Range(0.001, 500)) = 12.1
		_BlackOut ("Global Blackout Slider", Range(0, 1)) = 1
		_Scroll ("Scroll", Range(-1, 1)) = 1
		[Toggle] _EnableThemeColorSampling ("Enable Theme Color Sampling", Float) = 0
		[Toggle] _UseTraditionalSampling ("Use Traditional Texture Sampling", Float) = 0
		_ThemeColorTarget ("Choose Theme Color", Float) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
}