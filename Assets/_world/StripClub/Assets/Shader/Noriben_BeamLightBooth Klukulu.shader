Shader "Noriben/BeamLightBooth Klukulu" {
	Properties {
		[Header(Texture)] _NoiseTex ("NoiseTex", 2D) = "white" {}
		[Header(Color)] _Color ("Color", Vector) = (1,1,1,1)
		_Emission ("Emissive", 2D) = "" {}
		_EmissiveScroll ("Emissive Scroll", Vector) = (0.1,0.05,0,0)
		_EmissiveOffset ("Emissive Offset (XY)", Vector) = (0,0,0,0)
		_Intensity ("Light Intensity", Range(0, 10)) = 1
		[Space] [Header(Size)] _ConeWidth ("Width", Range(-0.07, 0.5)) = 1
		_ConeLength ("Length", Range(0.01, 3)) = 1
		[Space] [Header(Noise)] _TexPower ("Noise power", Range(0, 1)) = 1
		_LightIndex ("Noise seed", Float) = 0
		[Space] [Header(Soft)] _RimPower ("Edge soft", Range(0.01, 10)) = 3
		[Space] [Header(Gradation Height)] [Toggle] _GradOn ("Gradation Height ON", Float) = 0
		_GradHeight ("Gradation Height", Float) = 1
		_GradPower ("Gradation Power", Range(1, 0)) = 0.3
		[Space] [Header(Divide)] _Divide ("Divide", Range(0, 30)) = 0
		_DivideScroll ("Divide Scroll", Range(-10, 10)) = 0
		_DividePower ("Divide Power", Range(0, 1)) = 0
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = _Color.rgb;
			o.Alpha = _Color.a;
		}
		ENDCG
	}
}