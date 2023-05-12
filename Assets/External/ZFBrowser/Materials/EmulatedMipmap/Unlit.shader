// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Unity's base unlit shader adapted for emulated mipmaps

// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Emulated Mipmap/Unlit Transparent" {
	Properties{
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}

		SubShader{
			Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
			LOD 100

			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass {
				CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma multi_compile_fog
					#pragma target 3.0

					#include "UnityCG.cginc"
					#include "./MipSample.cginc"

					struct appdata_t {
						float4 vertex : POSITION;
						float2 texcoord : TEXCOORD0;

						UNITY_VERTEX_INPUT_INSTANCE_ID //Insert
					};

					struct v2f {
						float4 vertex : SV_POSITION;
						half2 texcoord : TEXCOORD0;
						UNITY_FOG_COORDS(1)

						UNITY_VERTEX_OUTPUT_STEREO //Insert
					};

					sampler2D _MainTex;
					float4 _MainTex_ST;
					float4 _MainTex_TexelSize;

					v2f vert(appdata_t v)
					{
						v2f o;

							UNITY_SETUP_INSTANCE_ID(v); //Insert
							UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
							UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert

						o.vertex = UnityObjectToClipPos(v.vertex);
						o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
						UNITY_TRANSFER_FOG(o,o.vertex);
						return o;
					}

					fixed4 frag(v2f i) : SV_Target
					{
						fixed4 col = mipSample(_MainTex, i.texcoord, _MainTex_TexelSize.zw, 10, 1);
						UNITY_APPLY_FOG(i.fogCoord, col);
						return col;
					}
				ENDCG
			}
	}

}