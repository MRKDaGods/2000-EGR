Shader "Hidden/SgtSpacetime"
{
	Properties
	{
		_MainTex("Main Tex", 2D) = "white" {}
		_Color("Color", Color) = (1, 1, 1, 1)
		_AmbientColor("Ambient Color", Color) = (1, 1, 1, 1)
		_DisplacementColor("Displacement Color", Color) = (1, 1, 1, 1)
		_HighlightColor("Highlight Color", Color) = (1, 1, 1, 1)
		_HighlightPower("Highlight Power", Float) = 1
		_HighlightScale("Highlight Scale", Float) = 1
		_Tile("Tile", Float) = 1
		_Power("Power", Float) = 1
		_Offset("Offset", Vector) = (0, 0, 0, 1)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}
		Pass
		{
			Blend One One
			Cull Off
			Lighting Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma multi_compile __ SGT_A // Pinch, Offset
			#pragma multi_compile __ SGT_B // Accumulate
			#define GAUSSIAN_COUNT_MAX 12
			#define RIPPLE_COUNT_MAX 1
			#define TWIST_COUNT_MAX 1
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4    _Color;
			float4    _AmbientColor;
			float4    _DisplacementColor;
			float     _Tile;
			float     _Power;
			float3    _Offset;
			float4    _HighlightColor;
			float     _HighlightPower;
			float     _HighlightScale;

			int       _Gau;
			float4    _GauPos[GAUSSIAN_COUNT_MAX]; // xyz = postion, w = radius
			float4    _GauDat[GAUSSIAN_COUNT_MAX]; // x = strength

			int       _Rip;
			float4    _RipPos[RIPPLE_COUNT_MAX]; // xyz = postion, w = radius
			float4    _RipDat[RIPPLE_COUNT_MAX]; // x = strength, y = frequency, z = offset

			int       _Twi;
			float4    _TwiPos[TWIST_COUNT_MAX]; // xyz = postion, w = radius
			float4    _TwiDat[TWIST_COUNT_MAX]; // x = strength, y = frequency, z = offset
			float4x4  _TwiMat[TWIST_COUNT_MAX]; // world to local space matrix of the well

			struct a2v
			{
				float4 vertex    : POSITION;
				float4 color     : COLOR;
				float2 texcoord0 : TEXCOORD0;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex    : SV_POSITION;
				float4 color     : COLOR;
				float2 texcoord0 : TEXCOORD0;
				float  texcoord1 : TEXCOORD1;

				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct f2g
			{
				float4 color : SV_TARGET;
			};

			void UpdateOutput(inout float4 modifiedWPos, float3 wellVector, float3 wellDistance01, float wellStrength)
			{
#if SGT_A
				wellDistance01 = smoothstep(1.0f, 0.0f, wellDistance01);

				modifiedWPos.xyz += _Offset * wellDistance01 * wellStrength;
#else 
				wellDistance01 = smoothstep(0.0f, 1.0f, wellDistance01);

				float invPow = 1.0f - pow(wellDistance01, _Power);

				modifiedWPos.xyz += wellVector * invPow * wellStrength;
#endif
			}

			void UpdateGaussian(inout float4 modifiedWPos, float4 originalWPos, float4 wellPos, float4 wellDat)
			{
#if SGT_B
				float3 vec = wellPos.xyz - originalWPos.xyz;
#else
				float3 vec = wellPos.xyz - modifiedWPos.xyz;
#endif
				float len = length(vec);
				float distance01 = saturate(len / wellPos.w);

				UpdateOutput(modifiedWPos, vec, distance01, wellDat.x);
			}

			void UpdateRipple(inout float4 modifiedWPos, float4 originalWPos, float4 wellPos, float4 wellDat)
			{
#if SGT_B
				float3 vec = wellPos.xyz - originalWPos.xyz;
#else
				float3 vec = wellPos.xyz - modifiedWPos.xyz;
#endif
				float len = length(vec);
				float distance01 = saturate(len / wellPos.w);
				float amplitude = sin(distance01 * wellDat.y + wellDat.z) * 0.5f + 0.5f;

				UpdateOutput(modifiedWPos, vec, distance01, amplitude * wellDat.x);
			}

			void UpdateTwist(inout float4 modifiedWPos, float4 originalWPos, float4 wellPos, float4 wellDat, float4 wellLPos)
			{
#if SGT_B
				float3 vec = wellPos.xyz - originalWPos.xyz;
#else
				float3 vec = wellPos.xyz - modifiedWPos.xyz;
#endif
				float len = length(vec);
				float distance01 = saturate(len / wellPos.w);
				float offset = atan2(wellLPos.x, wellLPos.z) * 2.0f;
				float amplitude = sin(distance01 * wellDat.y + offset) * 0.5f + 0.5f;

				float skew = saturate(distance01 - wellDat.z / (1.0f - wellDat.z));
				float hole = 1.0f - pow(1.0f - skew, wellDat.w);

				UpdateOutput(modifiedWPos, vec, distance01, amplitude * hole * wellDat.x);
			}

			inline float3 Unity_SafeDistance(float3 inVec)
			{
				float dp3 = max(0.00001f, dot(inVec, inVec));

				return sqrt(dp3);
			}

			void Vert(a2v i, out v2f o)
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 originalWPos = mul(unity_ObjectToWorld, i.vertex);
				float4 modifiedWPos = originalWPos;

				for (int g = 0; g < _Gau; g++)
				{
					UpdateGaussian(modifiedWPos, originalWPos, _GauPos[g], _GauDat[g]);
				}

				for (int r = 0; r < _Rip; r++)
				{
					UpdateRipple(modifiedWPos, originalWPos, _RipPos[r], _RipDat[r]);
				}

				for (int t = 0; t < _Twi; t++)
				{
					UpdateTwist(modifiedWPos, originalWPos, _TwiPos[t], _TwiDat[t], mul(_TwiMat[t], originalWPos));
				}

				o.vertex = mul(UNITY_MATRIX_VP, modifiedWPos);
				o.color = i.color * _Color;
				o.texcoord0 = i.texcoord0 * _Tile;
				//o.texcoord1 = distance(modifiedWPos.xyz, originalWPos.xyz);
				o.texcoord1 = Unity_SafeDistance(modifiedWPos.xyz - originalWPos.xyz);
			}

			void Frag(v2f i, out f2g o)
			{
				float4 mainTex = tex2D(_MainTex, i.texcoord0);
				float  strength = i.texcoord1;

				// Create ambient and displacement color
				o.color.rgb = mainTex.rgb * (_AmbientColor.rgb + _DisplacementColor.rgb * strength);

				// Add highlight color
				o.color.rgb += _HighlightColor * pow(strength * _HighlightScale, _HighlightPower);

				o.color.a = 1.0f;
				o.color *= i.color;
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader