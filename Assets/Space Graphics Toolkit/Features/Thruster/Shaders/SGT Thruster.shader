Shader "Space Graphics Toolkit/Thruster"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags
		{
			"Queue"             = "Transparent"
			"RenderType"        = "Transparent"
			"PreviewType"       = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Pass
		{
			Blend One One
			Cull Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex   Vert
			#pragma fragment Frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4    _Color;

			struct a2v
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				float4 color    : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float4 color    : COLOR;

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
				o.color    = i.color * _Color;
			}

			void Frag(v2f i, out f2g o)
			{
				o.color = tex2D(_MainTex, i.texcoord) * i.color;
				o.color.a = saturate(o.color.a);
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader