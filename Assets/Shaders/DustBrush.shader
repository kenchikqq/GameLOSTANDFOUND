Shader "Hidden/Dust/Brush"
{
    Properties{}
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex; // текущая маска
            float4 _MainTex_TexelSize;
            float2 _BrushPos;      // uv 0..1
            float _BrushSize;      // нормализованный радиус (от 0 до 1 в UV)
            float _BrushHardness;  // 0..1
            float _BrushStrength;  // 0..1

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 src = tex2D(_MainTex, i.uv);
                float m = src.a > 0 ? src.r : src.g; // поддержка разных каналов
                float2 d = i.uv - _BrushPos;
                float dist = length(d) / max(_BrushSize, 1e-4);
                float falloff = saturate(1.0 - smoothstep(0.0, 1.0, dist));
                falloff = pow(falloff, saturate(_BrushHardness));
                // уменьшаем маску (вычищаем)
                m = saturate(m - falloff * _BrushStrength);
                return fixed4(m,m,m,1);
            }
            ENDCG
        }
    }
}

