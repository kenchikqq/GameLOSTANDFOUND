Shader "Dust/Overlay"
{
    Properties
    {
        _DustTex ("Dust Texture", 2D) = "white" {}
        _DustColor ("Dust Color", Color) = (1,1,1,1)
        _DustStrength ("Dust Strength", Float) = 1
        _DustMask ("Dust Mask", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _DustTex;
            sampler2D _DustMask;
            fixed4 _DustColor;
            float _DustStrength;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Маска: поддержим r/alpha
                fixed4 m = tex2D(_DustMask, i.uv);
                float mask = max(m.r, m.a);
                float dust = tex2D(_DustTex, i.uv).r;
                fixed3 col = _DustColor.rgb * dust;
                float a = saturate(mask * _DustStrength) * _DustColor.a;
                return fixed4(col, a);
            }
            ENDCG
        }
    }
}

