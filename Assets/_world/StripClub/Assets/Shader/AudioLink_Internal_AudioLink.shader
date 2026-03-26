Shader "AudioLink/Internal/AudioLink" {
	Properties {
		_Gain ("Gain", Range(0, 2)) = 1
		_FadeLength ("Fade Length", Range(0, 1)) = 0.8
		_FadeExpFalloff ("Fade Exp Falloff", Range(0, 1)) = 0.3
		_Bass ("Bass", Range(0, 4)) = 1
		_Treble ("Treble", Range(0, 4)) = 1
		_X0 ("X0", Range(0, 0.168)) = 0.25
		_X1 ("X1", Range(0.242, 0.387)) = 0.25
		_X2 ("X2", Range(0.461, 0.628)) = 0.5
		_X3 ("X3", Range(0.704, 0.953)) = 0.75
		_Threshold0 ("Threshold 0", Range(0, 1)) = 0.45
		_Threshold1 ("Threshold 1", Range(0, 1)) = 0.45
		_Threshold2 ("Threshold 2", Range(0, 1)) = 0.45
		_Threshold3 ("Threshold 3", Range(0, 1)) = 0.45
		[ToggleUI] _Autogain ("Enable Autogain", Float) = 1
		_AutogainDerate ("Autogain Derate", Range(0.001, 0.5)) = 0.1
		_SourceVolume ("Audio Source Volume", Float) = 1
		_SourceDistance ("Distance to Source", Float) = 1
		_SourceSpatialBlend ("Spatial Blend", Float) = 0
		_SourcePosition ("Source Position", Vector) = (0,0,0,0)
		_ThemeColorMode ("Theme Color Mode", Float) = 0
		_CustomThemeColor0 ("Theme Color 0", Vector) = (1,1,0,1)
		_CustomThemeColor1 ("Theme Color 1", Vector) = (0,0,1,1)
		_CustomThemeColor2 ("Theme Color 2", Vector) = (1,0,0,1)
		_CustomThemeColor3 ("Theme Color 3", Vector) = (0,1,0,1)
		[Enum(None,0,Playing,1,Paused,2,Stopped,3,Loading,4,Streaming,5,Error,6)] _MediaPlaying ("Media Playing", Float) = 0
		[Enum(None,0,Loop,1,Loop One,2,Random,3,Random Loop,4)] _MediaLoop ("Media Loop", Float) = 0
		_MediaVolume ("Media Volume", Range(0, 1)) = 0
		_MediaTime ("Media Time (Progress %)", Range(0, 1)) = 0
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
}