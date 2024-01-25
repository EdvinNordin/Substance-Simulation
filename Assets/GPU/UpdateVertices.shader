
Shader "UpdateVertices"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
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

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float displacement = tex2Dlod(_MainTex, float4(v.uv, 0, 0)).r * 1.0f;
                worldPos.y += displacement;
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float r = tex2D(_MainTex, i.uv).r * (sin(_Time.w + 0.0) * 0.05 + 0.5);
                float g = tex2D(_MainTex, i.uv).r * (sin(_Time.w + 2.0) * 0.05 + 0.5);
                float b = tex2D(_MainTex, i.uv).r * (sin(_Time.w + 4.0) * 0.05 + 0.5);
                fixed4 col = fixed4(r, g, b, 1);
                return col;
            }
            ENDCG
        }
    }
}