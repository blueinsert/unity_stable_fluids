Shader "bluebean/StableFluids/PostEffectShader"
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
			sampler2D _sunray;

			//float3 linearToGamma(float3 color) {
			//	color = max(color, float3(0.0,0.0,0.0));
			//	return max(1.055 * pow(color, float3(0.416666667)) - 0.055, float3(0));
			//}

			float4 frag(v2f i) : SV_Target
			{
				float3 c = tex2D(_Source,i.uv).rgb;

				float sunray = tex2D(_sunray, i.uv).r;
				c *= sunray;

				float a = max(c.r, max(c.g, c.b));

				float4 result = float4(c, a);
				return result;
		    }
		ENDCG
	}
	}
}
