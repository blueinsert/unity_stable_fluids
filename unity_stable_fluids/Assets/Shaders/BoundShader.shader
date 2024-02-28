Shader "bluebean/StableFluids/BoundShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

	struct v2f
	{
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

			sampler2D _MainTex;

			sampler2D _Source;

			float _b;

			float4 _resolution;

			float4 frag(v2f i) : SV_Target
			{
				float2 vL = i.uv - float2(1.0 / _resolution.x,0);
				float2 vR = i.uv + float2(1.0 / _resolution.x, 0);
				float2 vT = i.uv + float2(0, 1.0 / _resolution.y);
				float2 vB = i.uv - float2(0, 1.0 / _resolution.y);

				float4 left = tex2D(_Source, vL);
				float4 right = tex2D(_Source, vR);
				float4 top = tex2D(_Source, vT);
				float4 bottom = tex2D(_Source, vB);

				float4 center = tex2D(_Source, i.uv);
				if (vL.x < 0 &&(i.uv.y>0 && i.uv.y<1.0)) {
					center = _b == 0 ? right : -right;
				}
				if (vR.x > 1.0 && (i.uv.y > 0 && i.uv.y < 1.0)) {
					center = _b == 0 ? left : -left;
				}
				if (vB.y < 0 && (i.uv.x > 0 && i.uv.x < 1.0)) {
					center = _b == 0 ? top : -top;
				}
				if (vT.y > 1.0 && (i.uv.x > 0 && i.uv.x < 1.0)) {
					center = _b == 0 ? bottom : -bottom;
				}
				if (vL.x < 0 && vB.y < 0) {
					center = (right + top) / 2.0;
				}
				if (vR.x > 1.0 && vB.y < 0) {
					center = (left + top) / 2.0;
				}
				if (vR.x > 1.0 && vT.y > 1.0) {
					center = (left + bottom) / 2.0;
				}
				if (vL.x < 0 && vT.y > 1.0) {
					center = (right + bottom) / 2.0;
				}
				return center;
		    }
		ENDCG
	}
	}
}
