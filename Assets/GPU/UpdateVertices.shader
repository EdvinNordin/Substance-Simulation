
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
                float intensifier = 1.0f;
                float value = tex2D(importTexture, i.uv).r*intensifier;
                
                float r = value;//min(0,value * (sin(_Time.w + 0.0) * 0.05 + 0.5));
                float g = value;//max(1,value * (sin(_Time.w + 2.0) * 0.05 + 0.5));
                float b = value;//0;//value * (sin(_Time.w + 4.0) * 0.05 + 0.5);
                float alpha = value;
                //fixed4 col = fixed4(1.0f-r, 1.0f-g, 1.0f-b, alpha); //old
                //fixed4 col = fixed4(0.5f+r,0.5f+g,0.5f+b,alpha); //for WE
                fixed4 col = fixed4(0.0f+r,0.0f+g,0.0f+b,alpha); //for NS
                return col;
            }
            ENDCG
        }
    }
}