Shader "Custom/LabelOverlayURP"
{
    Properties
    {
        _BaseMap ("Базовая текстура", 2D) = "white" {}
        _BaseColor ("Цвет базы", Color) = (1,1,1,1)

        _LabelTex ("PNG надпись (прозрачный фон)", 2D) = "white" {}
        _LabelColor ("Цвет надписи", Color) = (1,1,1,1)
        _LabelOpacity ("Непрозрачность", Range(0,1)) = 1
        _LabelRect ("UV прямоугольник [minU,minV,maxU,maxV]", Vector) = (0.1,0.1,0.3,0.18)
        _LabelRotation ("Поворот (градусы)", Range(-180,180)) = 0
        _LabelMipBias ("Mip Bias", Range(-2,2)) = -0.5

        [Header(Face Mask)]
        _EnableFaceMask ("Enable Face Mask (0/1)", Range(0,1)) = 0
        _UseObjectSpaceDir ("Direction In Object Space (0=World,1=Object)", Range(0,1)) = 1
        _FaceDir ("Face Direction (xyz)", Vector) = (0,0,1,0)
        _FaceHalfAngle ("Face Half Angle (deg)", Range(0,90)) = 25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float2 uv          : TEXCOORD0;
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _BaseMap_ST;
            float4 _BaseColor;

            TEXTURE2D(_LabelTex); SAMPLER(sampler_LabelTex);
            float4 _LabelTex_ST;
            float4 _LabelColor;
            float _LabelOpacity;
            float4 _LabelRect; // xy=min, zw=max
            float _LabelRotation;
            float _LabelMipBias;
            float _EnableFaceMask;
            float _UseObjectSpaceDir;
            float4 _FaceDir; // xyz
            float _FaceHalfAngle;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float2 rotate2D(float2 p, float angleRad)
            {
                float s = sin(angleRad);
                float c = cos(angleRad);
                float2x2 m = float2x2(c, -s, s, c);
                return mul(m, p);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // База
                float4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Проверяем валидность прямоугольника
                bool hasLabel = (_LabelRect.z > _LabelRect.x) && (_LabelRect.w > _LabelRect.y) && (_LabelOpacity > 0.001);
                if (!hasLabel)
                {
                    return baseCol;
                }

                // Преобразуем UV в локальные координаты [0..1] внутри _LabelRect
                float2 uvMin = _LabelRect.xy;
                float2 uvMax = _LabelRect.zw;
                float2 local = (IN.uv - uvMin) / max(uvMax - uvMin, float2(1e-5, 1e-5));

                // Центрируем и поворачиваем
                local = local * 2.0 - 1.0; // [-1..1]
                local = rotate2D(local, radians(_LabelRotation));
                local = (local * 0.5) + 0.5; // обратно в [0..1]

                // Внутри ли прямоугольника после поворота
                float inside = step(0.0, local.x) * step(0.0, local.y) * step(local.x, 1.0) * step(local.y, 1.0);

                // Семплируем PNG со смещением мип-уровня для резкости
                #if defined(SHADER_API_GLES)
                    float4 lbl = SAMPLE_TEXTURE2D(_LabelTex, sampler_LabelTex, local);
                #else
                    float4 lbl = SAMPLE_TEXTURE2D_BIAS(_LabelTex, sampler_LabelTex, local, _LabelMipBias);
                #endif

                // Маска по направлению нормали (ограничить одной гранью)
                float facingMask = 1.0;
                if (_EnableFaceMask > 0.5)
                {
                    float3 dirWS = _FaceDir.xyz;
                    if (_UseObjectSpaceDir > 0.5)
                    {
                        dirWS = TransformObjectToWorldDir(dirWS);
                    }
                    dirWS = normalize(dirWS);
                    float ndl = dot(normalize(IN.normalWS), dirWS);
                    float minDot = cos(radians(_FaceHalfAngle));
                    facingMask = step(minDot, ndl);
                }

                float a = lbl.a * _LabelOpacity * _LabelColor.a * inside * facingMask;
                float3 labelRGB = lbl.rgb * _LabelColor.rgb;

                // Наложение поверх базы (обычный альфа-композитинг)
                float3 outRGB = lerp(baseCol.rgb, labelRGB, a);
                float outA = baseCol.a; // непрозрачный предмет

                float4 col = float4(outRGB, outA);
                return col;
            }
            ENDHLSL
        }
    }

    FallBack Off
}


