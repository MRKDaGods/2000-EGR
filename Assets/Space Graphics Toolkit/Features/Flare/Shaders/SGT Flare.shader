Shader "Space Graphics Toolkit/Flare"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
		_ZTest("ZTest", Float) = 2 // 2 = LEqual
		_DstBlend("DstBlend", Float) = 1 // 1 = One
	}
	SubShader
	{
		Tags
		{
			"Queue"       = "Transparent"
			"RenderType"  = "Transparent"
			"PreviewType" = "Plane"
		}

		Pass
		{
			Blend One [_DstBlend]
			Cull Off
			ZWrite Off
			ZTest [_ZTest]

			CGPROGRAM
			#pragma vertex   Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4    _Color;

			struct a2v
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD0;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct f2g
			{
				float4 color : SV_TARGET;
			};

			void Vert(a2v i, out v2f o)
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				o.vertex   = UnityObjectToClipPos(i.vertex);
				o.texcoord = i.texcoord;
			}

			void Frag(v2f i, out f2g o)
			{
				float4 color = _Color;

				color.rgb *= color.a;

				o.color = tex2D(_MainTex, i.texcoord) * color;
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader