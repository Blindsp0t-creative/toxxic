Shader "VRCBCE/Surface Color Mask" {
	Properties {
		_Turnoff ("Turn off", Range(0, 1)) = 0
		_MainTex ("Main Tex", 2D) = "white" {}
		_BallColour ("Ball Colour", Vector) = (1,0,0,0)
		_CustomColor ("Custom Color", Range(-1, 0)) = -1
		_ColourMask ("Colour Mask", 2D) = "black" {}
		_ConstMask ("Const Mask", 2D) = "black" {}
		_emissionmap ("emission map", 2D) = "white" {}
		_Emission ("Emission", Vector) = (0,0,0,0)
		_normalmap ("normal map", 2D) = "bump" {}
		_metalic ("metalic", 2D) = "black" {}
		_Smoothness ("Smoothness", Range(0, 1)) = 0
		[HideInInspector] _texcoord ("", 2D) = "white" {}
		[HideInInspector] __dirty ("", Float) = 1
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
	Fallback "Diffuse"
}