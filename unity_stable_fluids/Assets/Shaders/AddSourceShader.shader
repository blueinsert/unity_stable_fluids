Shader "bluebean/StableFluids/AddSourceShader"
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
                float2 vL : TEXCOORD1;
                float2 vR : TEXCOORD2;
                float2 vT : TEXCOORD3;
                float2 vB : TEXCOORD4;
            };

            float2 _texelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.vL = v.uv - float2(_texelSize.x, 0);
                o.vR = v.uv + float2(_texelSize.x, 0);
                o.vT = v.uv + float2(0, _texelSize.y);
                o.vB = v.uv - float2(0, _texelSize.y);
                return o;
            }

            sampler2D _MainTex;
            
            float3 _color;
            float2 _point;
            float _radius;

            float4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv - _point.xy;
                //p.x *= aspectRatio;
                float3 splat = exp(-dot(p, p) / _radius) * _color;
                float3 base = tex2D(_MainTex, i.uv).xyz;
                float4 color = float4(base + splat, 1.0);
                return color;
            }
            ENDCG
        }
    }
}
