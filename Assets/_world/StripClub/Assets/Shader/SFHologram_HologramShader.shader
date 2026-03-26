Shader "SFHologram/HologramShader" {
	Properties {
		_Brightness ("Brightness", Range(0.1, 6)) = 3
		_Alpha ("Alpha", Range(0, 1)) = 1
		_Direction ("Direction", Vector) = (0,1,0,0)
		_MainTex ("MainTexture", 2D) = "white" {}
		_MainColor ("MainColor", Vector) = (1,1,1,1)
		_RimColor ("Rim Color", Vector) = (1,1,1,1)
		_RimPower ("Rim Power", Range(0.1, 10)) = 5
		_ScanTiling ("Scan Tiling", Range(0.01, 10)) = 0.05
		_ScanSpeed ("Scan Speed", Range(-2, 2)) = 1
		_GlowTiling ("Glow Tiling", Range(0.01, 1)) = 0.05
		_GlowSpeed ("Glow Speed", Range(-10, 10)) = 1
		_GlitchSpeed ("Glitch Speed", Range(0, 50)) = 1
		_GlitchIntensity ("Glitch Intensity", Float) = 0
		_FlickerTex ("Flicker Control Texture", 2D) = "white" {}
		_FlickerSpeed ("Flicker Speed", Range(0.01, 100)) = 1
		[HideInInspector] _Fold ("__fld", Float) = 1
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
	//CustomEditor "HologramShaderGUI"
}