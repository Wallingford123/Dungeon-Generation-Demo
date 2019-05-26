Shader "Deform/FirstShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Multiplier("Multiplier", Range(0,25)) = 0
		_Intensity("Intensity", Range(0,5)) = 0
		_RotateSpeed("Rotate Speed", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal: NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _Multiplier;
			float _Intensity;
			float _RotateSpeed;
			
			float4 RotateAroundYInDegrees(float4 vertex, float degrees)
			{
				float alpha = degrees * UNITY_PI / 180.0;
				float sina, cosa;
				sincos(alpha, sina, cosa);
				float2x2 m = float2x2(cosa, -sina, sina, cosa);
				return float4(mul(m, vertex.xz), vertex.yw).xzyw;
			}

			v2f vert (appdata v)
			{
				float l = _Multiplier * sin(_Time.w) / 50;
				//v.vertex.x += v.normal.x * l;
				//v.vertex.y += v.normal.y * l;
				//v.vertex.z += v.normal.z * l;

				float4 n = (v.normal.x, v.normal.y, v.normal.z, 1);

				//v.vertex.x += v.normal.x * l; //(n * sin((_Time.w*2) * v.vertex.y * _Intensity)/300 * _Multiplier);
				//v.vertex.y += v.normal.y * l;
				//v.vertex.z += v.normal.z * l;

				float px = sin(((_Time.w) + v.normal.x))/ 2 + 1;
				float py = sin(((_Time.w) + v.normal.y))/ 2 + 1;
				float pz = sin(((_Time.w) + v.normal.z))/ 2 + 1;

				//v.vertex.x *= px;
				//v.vertex.y *= py;
				//v.vertex.z *= pz;

				v.vertex.x *= sin(_Time.w + (v.vertex.y*_Multiplier)) / (16 / _Intensity) + 1;
				v.vertex.y *= sin(_Time.w + (v.vertex.z*_Multiplier)) / (16 / _Intensity) + 1;
				v.vertex.z *= sin(_Time.w + (v.vertex.x*_Multiplier)) / (16/_Intensity) + 1;

				v.vertex = RotateAroundYInDegrees(v.vertex, _Time.w* _RotateSpeed);

				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
