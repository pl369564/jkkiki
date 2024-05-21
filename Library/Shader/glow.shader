// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/glow"
{
	Properties{
		//_GlowColor("Glow Color", Color) = (1,1,0,1)
		_Strength("Glow Strength", Range(5.0, 1.0)) = 2.0
		_GlowRange("Glow Range", Range(0.1,1)) = 0.6
	}

	SubShader{

			//Pass {
			//	Tags { "LightMode" = "ForwardBase" }

			//	CGPROGRAM

			//	#pragma vertex vert
			//	#pragma fragment frag

			//	float4 _Color;

			//	float4 vert(float4 vertexPos : POSITION) : SV_POSITION {
			//		return UnityObjectToClipPos(vertexPos);
			//	}

			//	float4 frag(void) : COLOR {
			//		return _Color;
			//	}

			//	ENDCG
			//}

			Pass {
				Tags { "LightMode" = "ForwardBase" "Queue" = "Transparent" "RenderType" = "Transparent" }
				// Cull Front
				LOD 100
				ZWrite Off
				Blend SrcAlpha OneMinusSrcAlpha

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_instancing

				#include "UnityCG.cginc"

				float _Strength;
				float _GlowRange;

				struct a2v {
					float4 vertex : POSITION;
					float4 normal : NORMAL;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 position : SV_POSITION;
					float4 col : COLOR;
				};

				UNITY_INSTANCING_BUFFER_START(Props)
				    // put more per-instance properties here
				    UNITY_DEFINE_INSTANCED_PROP(float, _testRed)
				    #define propTestRed Props

				    UNITY_DEFINE_INSTANCED_PROP(float, _testGreen)
				    #define propTestGreen Props

				    UNITY_DEFINE_INSTANCED_PROP(float, _testBlue)
				    #define propTestBlue Props

				    UNITY_DEFINE_INSTANCED_PROP(float, _testAlpha)
				    #define propTestAlpha Props

				UNITY_INSTANCING_BUFFER_END(Props)

				v2f vert(a2v a) {
					v2f o;
					UNITY_SETUP_INSTANCE_ID(a);
					float4x4 modelMatrix = unity_ObjectToWorld;
					float4x4 modelMatrixInverse = unity_WorldToObject;
					float3 normalDirection = normalize(mul(a.normal, modelMatrixInverse)).xyz;
					float3 viewDirection = normalize(_WorldSpaceCameraPos - mul(modelMatrix, a.vertex).xyz);
					float4 pos = a.vertex + (a.normal * _GlowRange);
					o.position = UnityObjectToClipPos(pos);
					float3 normalDirectionT = normalize(normalDirection);
					float3 viewDirectionT = normalize(viewDirection);
					float strength = abs(dot(viewDirectionT, normalDirectionT));

					float4 color;
					color.r = UNITY_ACCESS_INSTANCED_PROP(propTestRed, _testRed);
					color.g = UNITY_ACCESS_INSTANCED_PROP(propTestGreen, _testGreen);
					color.b = UNITY_ACCESS_INSTANCED_PROP(propTestBlue, _testBlue);
					color.a = UNITY_ACCESS_INSTANCED_PROP(propTestAlpha, _testAlpha);
					//if (color.a < 0.2)color.a = 0.2;
					float opacity = pow(strength, _Strength)* 0.75;
					float4 col = float4(color.xyz, opacity);
					o.col = col;
					return o;
				}

				float4 frag(v2f i) : COLOR {
					return i.col;
				}

				ENDCG
			}

	}
	FallBack "Diffuse"
}
