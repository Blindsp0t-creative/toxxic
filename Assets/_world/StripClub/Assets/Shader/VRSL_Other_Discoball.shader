Shader "VRSL/Other/Discoball" {
	Properties {
		[HideInInspector] _DMXChannel ("DMX Fixture Number/Sector (Per 13 Channels)", Float) = 0
		[Toggle] [HideInInspector] _NineUniverseMode ("Extended Universe Mode", Float) = 0
		[Toggle] _EnableCompatibilityMode ("Enable Compatibility Mode", Float) = 0
		[Toggle] _EnableVerticalMode ("Enable Vertical Mode", Float) = 0
		[Toggle] _EnableDMX ("Enable Stream DMX/DMX Control", Float) = 0
		_GlobalIntensity ("Global Intensity", Range(0, 1)) = 1
		_FinalIntensity ("Final Intensity", Range(0, 1)) = 1
		_UniversalIntensity ("Universal Intensity", Range(0, 1)) = 1
		[HDR] _Emission ("Color", Vector) = (1,1,1,0.2)
		_Cube ("Projection Map", Cube) = "" {}
		[Toggle] _UseWorldNorm ("Use World Normal vs View Normal", Float) = 0
		_RotationSpeed ("Rotation Speed", Range(-180, 180)) = 8.2
		_Multiplier ("Brightness Multiplier", Range(0, 10)) = 1
		[Enum(Transparent,1,AlphaToCoverage,2)] _RenderMode ("Render Mode", Float) = 1
		[Enum(Off,0,On,1)] _ZWrite ("Z Write", Float) = 0
		[Enum(Off,0,On,1)] _AlphaToCoverage ("Alpha To Coverage", Float) = 0
		[Enum(Off,0,One,1)] _BlendDst ("Destination Blend mode", Float) = 1
		[Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Operation", Float) = 0
		_ClippingThreshold ("Clipping Threshold", Range(0, 1)) = 0.5
		_GlobalIntensityBlend ("Global Intensity Blend", Range(0, 1)) = 1
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