Shader "Ambiente/Neblina Bosque (volumen)"
{
    // Neblina DE SUELO en toda la escena: muy espesa pegada al piso y se desvanece
    // rapido hacia arriba (a la altura de los ojos casi no hay). Mas espesa en el bosque.
    // Se calcula a lo largo de la vista (raymarching). Va en un cubo que sigue a la camara.
    Properties
    {
        _Color ("Color", Color) = (0.24, 0.29, 0.45, 1)
        _Densidad ("Densidad", Float) = 0.08
        _DensidadPueblo ("Densidad dentro del pueblo (0-1)", Range(0, 1)) = 0.6
        _Altura ("Altura de la neblina (unidades)", Float) = 4.5
        _Suelo ("Altura del suelo", Float) = 10
        _Escala ("Tamano de las nubes", Float) = 0.02
        _Velocidad ("Velocidad (x, z)", Vector) = (0.5, 0.2, 0, 0)
        _CentroPueblo ("Centro del pueblo (x, z)", Vector) = (594, 0, 539, 0)
        _RadiosPueblo ("Radios del pueblo (x, z)", Vector) = (195, 0, 170, 0)
        _Orilla ("Ancho de la orilla (0-1)", Range(0.05, 1)) = 0.5
        _DistanciaMax ("Distancia maxima", Float) = 400
        _Bandas ("Bandas (toon)", Range(1, 8)) = 5
        _MezclaBandas ("Mezcla de bandas (0 = continua)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+50" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "NeblinaBosque"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Densidad, _DensidadPueblo, _Altura, _Suelo, _Escala;
                float4 _Velocidad, _CentroPueblo, _RadiosPueblo;
                float _Orilla, _DistanciaMax;
                half _Bandas;
                half _MezclaBandas;
            CBUFFER_END
            float _NeblinaDisipada; // global

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screenPos : TEXCOORD1; };

            float hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
            }

            Varyings vert (Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.screenPos = ComputeScreenPos(p.positionCS);
                return o;
            }

            float Densidad(float3 p, float t)
            {
                float e = length((p.xz - _CentroPueblo.xz) / _RadiosPueblo.xz);
                float zona = lerp(_DensidadPueblo, 1.0, smoothstep(0.8, 0.8 + _Orilla, e));
                // nubes que se arrastran: tambien cambian un poco la altura de la neblina
                float2 uv = p.xz * _Escala + t * _Velocidad.xy * 0.02;
                float n = vnoise(uv) * 0.6 + vnoise(uv * 2.3 + 13.7) * 0.4;
                float altoLocal = _Altura * (0.7 + 0.6 * n);
                // espesa en el piso, se desvanece rapido hacia arriba
                float h = max(p.y - _Suelo, 0.0) / altoLocal;
                float alto = exp(-h * h * 3.0);
                return _Densidad * zona * alto * (0.6 + 0.4 * n);
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 ro = _WorldSpaceCameraPos;
                float3 rd = normalize(i.positionWS - ro);

                float2 suv = i.screenPos.xy / i.screenPos.w;
                float eye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float3 fwd = -UNITY_MATRIX_V[2].xyz;
                float dist = min(eye / max(dot(rd, fwd), 1e-3), _DistanciaMax);

                // solo la franja baja donde hay neblina
                float yTope = _Suelo + _Altura * 2.6;
                float t0 = 0.0, t1 = dist;
                if (ro.y > yTope)
                {
                    if (rd.y >= -1e-4) return 0;
                    t0 = (yTope - ro.y) / rd.y;
                }
                else if (rd.y > 1e-4) t1 = min(t1, (yTope - ro.y) / rd.y);
                if (t1 <= t0) return 0;

                const int PASOS = 20;
                float dt = (t1 - t0) / PASOS;
                float jitter = hash21(suv * _ScreenParams.xy + frac(_Time.y));
                float optico = 0.0;
                for (int k = 0; k < PASOS; k++)
                {
                    float3 p = ro + rd * (t0 + (k + jitter) * dt);
                    optico += Densidad(p, _Time.y) * dt;
                }
                float a = 1.0 - exp(-optico);
                // Las bandas toon dependen de la distancia: cada salto se veia como un anillo
                // duro alrededor del jugador. Por defecto la neblina es continua.
                a = lerp(a, floor(a * _Bandas + 0.5) / _Bandas, _MezclaBandas);
                a *= 1.0 - saturate(_NeblinaDisipada);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
