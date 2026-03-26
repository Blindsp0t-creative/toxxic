Shader "Silent/Filamented" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo", 2D) = "white" {}
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_GlossMapScale ("Smoothness Scale", Range(0, 1)) = 1
		[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0
		[Gamma] _Metallic ("Metallic", Range(0, 1)) = 0
		_MetallicGlossMap ("Metallic", 2D) = "white" {}
		[ToggleOff] _SpecularHighlights ("Specular Highlights", Float) = 1
		[ToggleOff] _GlossyReflections ("Glossy Reflections", Float) = 1
		_BumpScale ("Scale", Float) = 1
		[Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
		_Parallax ("Height Scale", Range(0.005, 0.25)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}
		_OcclusionStrength ("Strength", Range(0, 1)) = 1
		_OcclusionMap ("Occlusion", 2D) = "white" {}
		_EmissionColor ("Color", Vector) = (0,0,0,1)
		_EmissionMap ("Emission", 2D) = "white" {}
		_DetailMask ("Detail Mask", 2D) = "white" {}
		_DetailAlbedoMap ("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale ("Scale", Float) = 1
		[Normal] _DetailNormalMap ("Normal Map", 2D) = "bump" {}
		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
		_ExposureOcclusion ("Lightmap Occlusion Sensitivity", Range(0, 1)) = 0.2
		[Toggle(_LIGHTMAPSPECULAR)] _LightmapSpecular ("Lightmap Specular", Range(0, 1)) = 1
		_LightmapSpecularMaxSmoothness ("Lightmap Specular Max Smoothness", Range(0, 1)) = 1
		[Toggle(_NORMALMAP_SHADOW)] _NormalMapShadows ("Normal Map Shadows", Range(0, 1)) = 0
		_BumpShadowHeightScale ("Height Scale", Range(0, 1)) = 0.2
		_BumpShadowHardness ("Shadow Hardness", Range(0, 100)) = 50
		_specularAntiAliasingVariance ("Specular AA Variance", Range(0, 1)) = 0.15
		_specularAntiAliasingThreshold ("Specular AA Threshold", Range(0, 1)) = 0.25
		[KeywordEnum(None, SH, RNM, MonoSH)] _Bakery ("Bakery Mode", Float) = 0
		_RNM0 ("RNM0", 2D) = "black" {}
		_RNM1 ("RNM1", 2D) = "black" {}
		_RNM2 ("RNM2", 2D) = "black" {}
		[Toggle(_LTCGI)] _LTCGI ("LTCGI", Float) = 0
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2
		[ToggleUI] _AlphaToMaskMode ("Alpha to Coverage", Float) = 0
		[HideInInspector] _DFG ("DFG", 2D) = "white" {}
		[HideInInspector] _Mode ("__mode", Float) = 0
		[HideInInspector] _SrcBlend ("__src", Float) = 1
		[HideInInspector] _DstBlend ("__dst", Float) = 0
		[HideInInspector] _ZWrite ("__zw", Float) = 1
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
	Fallback "VertexLit"
	//CustomEditor "SilentTools.FilamentedStandardShaderGUI"
}