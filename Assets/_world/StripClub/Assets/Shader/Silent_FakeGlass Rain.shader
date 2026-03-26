Shader "Silent/FakeGlass Rain" {
	Properties {
		[Header(Glass Colour)] _Color ("Diffuse Color", Vector) = (1,1,1,0)
		_MainTex ("Tint Texture", 2D) = "white" {}
		[HDR] _Glow ("Glow Strength", Vector) = (0,0,0,0)
		[Header(Material Properties)] [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
		_NormalScale ("Normal Scale", Float) = 1
		_Smoothness ("Smoothness", Range(0, 1)) = 1
		_Metallic ("Metallic", Range(0, 1)) = 0
		[Toggle(BLOOM)] _UseColourShift ("Use Colour Shift", Float) = 0
		_IOR ("IOR", Range(0, 2)) = 1
		[Gamma] _Refraction ("Refraction Power", Range(0, 1)) = 0.1
		_InteriorDiffuseStrength ("Interior Diffuse Strength", Range(0, 1)) = 0.1
		[Header(Additional Properties)] _SurfaceMask ("Surface Mask", 2D) = "black" {}
		_SurfaceSmoothness ("Surface Smoothness ", Range(0, 1)) = 0
		_SurfaceLevelTweak ("Surface Level Tweak", Range(-1, 1)) = 0
		_SurfaceSmoothnessTweak ("Surface Smoothness Tweak", Range(-1, 1)) = 0
		_OcclusionMap ("Occlusion Map", 2D) = "white" {}
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
		_ShadowTransparency ("Shadow Transparency", Range(0, 1)) = 1
		[ToggleUI] _ZWrite ("Z Write (for solid glass)", Float) = 0
		[Header(Rain Properties)] _RainPattern ("Rain Pattern", 2D) = "gray" {}
		[NoScaleOffset] [Normal] _RippleNormals ("Ripple Normals", 2D) = "bump" {}
		[NoScaleOffset] [Normal] _DropletNormals ("Droplet Normals", 2D) = "bump" {}
		_RainSpeed ("Rain Speed", Float) = 1
		_StreakTiling ("Streak Tiling", Float) = 1
		_StreakLength ("Streak Length", Float) = 1
		_RainFade ("Rain Fade", Range(0, 1)) = 1
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
	Fallback "Diffuse"
	//CustomEditor "ASEMaterialInspector"
}