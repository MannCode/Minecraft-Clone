Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _MinLOD ("Mip Min", Range(0, 12)) = 0
        _MaxLOD ("Mip Max", Range(0, 12)) = 12
        _BiasLOD ("Mip Bias", Float) = 0
        _SurfaceType ("Surface Type", Float) = 0 // 0: Opaque, 1: Transparent
        _BlendMode ("Blend Mode", Float) = 0 // 0: Alpha, 1: Additive, 2: Multiply
    }
    SubShader
    {
        // Handle Surface Type
        Tags { "RenderType" = "Opaque" }
        LOD 100

        // Opaque Surface
        ZWrite On
        Blend One Zero // No blending

        // Transparent Surface
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            fixed4 _Color;

            float _MinLOD, _MaxLOD, _BiasLOD;
            float _SurfaceType, _BlendMode;

            float CalcMipLevel(float2 texture_coord)
            {
                float2 dx = ddx(texture_coord);
                float2 dy = ddy(texture_coord);
                float delta_max_sqr = max(dot(dx, dx), dot(dy, dy));
               
                return max(0.0, 0.5 * log2(delta_max_sqr));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float mipLevel = CalcMipLevel(i.uv * _MainTex_TexelSize.zw);
                fixed4 texColor = tex2Dlod(_MainTex, float4(i.uv, 0.0, clamp(mipLevel + _BiasLOD, _MinLOD, _MaxLOD)));
                fixed4 col = texColor * _Color;

                // Apply blending mode
                if (_BlendMode == 1)
                {
                    col.rgb += texColor.rgb; // Additive
                }
                else if (_BlendMode == 2)
                {
                    col.rgb *= texColor.rgb; // Multiply
                }

                // Apply transparency if SurfaceType is Transparent
                if (_SurfaceType == 1)
                {
                    clip(col.a - 0.01); // Handles transparency
                }

                return texColor; // Return mip level
            }
            ENDCG
        }
    }
}
