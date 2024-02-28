Shader "bluebean/StableFluids/AdvectShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            sampler2D _velocity;
            sampler2D _source;
            float _dt;
            float4 _resolution;

            float4 frag(v2f i) : SV_Target
            {
                float2 texelSize = float2(1.0 / _resolution.x, 1.0 / _resolution.y);
                float2 uv = i.uv - _dt * tex2D(_velocity,i.uv).rg * texelSize;
                float4 col = tex2D(_source, uv);
                return col;
            }

            ENDCG
        }
    }
}
