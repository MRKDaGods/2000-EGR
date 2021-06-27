Shader "EGR/Glow" {
	Properties {
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Main Albedo (RGB) Alpha (A)", 2D) = "white" { }

		_Emission("Emission", float) = 0
	}
	SubShader {
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard alpha

		// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		fixed4 _Color;
		half _Emission;

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Emission = c.rgb * tex2D(_MainTex, IN.uv_MainTex).a * c.a * _Emission;
			o.Alpha = c.a;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
