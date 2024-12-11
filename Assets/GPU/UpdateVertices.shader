
Shader "UpdateVertices"
{
    Properties
    {
        importTexture ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        //Blend SrcAlpha OneMinusSrcAlpha
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

            sampler2D importTexture;

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float displacement = tex2Dlod(importTexture, float4(v.uv, 0, 0)).r;
                worldPos.y += displacement*1.0f;
                o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float value = tex2D(importTexture, i.uv).r*10.0f;
                float r = value * (sin(_Time.w + 0.0) * 0.05 + 0.5);
                float g = value * (sin(_Time.w + 2.0) * 0.05 + 0.5);
                float b = value * (sin(_Time.w + 4.0) * 0.05 + 0.5);
                float alpha = value;
                fixed4 col = fixed4(1.0f-r, 1.0f-g, 1.0f-b, alpha);
                return col;
            }
            ENDCG
        }
    }
}