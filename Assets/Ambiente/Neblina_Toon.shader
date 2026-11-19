Shader "Ambiente/Neblina Toon"
{
    // Neblina baja estilo caricatura: nubes que se arrastran despacio, en bandas,
    // mas densa lejos de la plaza (viene del bosque) y que se desvanece suave
    // al tocar paredes/pasto y cerca de la camara.
    // El script ProgresoNotas la disipa con la variable global _NeblinaDisipada (0 = toda, 1 = nada).
    Properties
    {
        _Color ("Color", Color) = (0.55, 0.62, 0.78, 1)
        _Opacidad ("Opacidad", Range(0, 1)) = 0.35
        _Escala ("Tamano de las nubes (menor = mas grandes)", Float) = 0.015
        _Velocidad ("Velocidad (x, z)", Vector) = (0.6, 0.25, 0, 0)
        _Umbral ("Cantidad de huecos", Range(0, 1)) = 0.4
        _Bandas ("Bandas (estilo toon)", Range(1, 6)) = 3
        _SuavidadBorde ("Suavidad al tocar objetos", Float) = 2.5
        _CentroPlaza ("Centro de la plaza (x, z)", Vector) = (594, 0, 539, 0)
        _RadioClaro ("Radio mas despejado (plaza)", Float) = 60
        _RadioDenso ("Radio donde ya es densa (bosque)", Float) = 200
        _DensidadCentro ("Densidad en la plaza", Range(0, 1)) = 0.35
        _FadeCamara ("Desvanecer cerca de la camara", Float) = 6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Neblina"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Opacidad;
                float _Escala;
                float4 _Velocidad;
                half _Umbral;
                half _Bandas;
                float _SuavidadBorde;
                float4 _CentroPlaza;
                float _RadioClaro, _RadioDenso;
                half _DensidadCentro;
                float _FadeCamara;
            CBUFFER_END
            float _NeblinaDisipada; // global

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            float hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
            }
            float fbm(float2 p)
            {
                return vnoise(p) * 0.55 + vnoise(p * 2.03 + 17.1) * 0.3 + vnoise(p * 4.1 + 41.7) * 0.15;
            }

            Varyings vert (Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.screenPos = ComputeScreenPos(p.positionCS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // nubes que se arrastran
                float2 uv = i.positionWS.xz * _Escala + _Time.y * _Velocidad.xy * 0.02;
                float n = fbm(uv);
                float v = smoothstep(_Umbral, _Umbral + 0.3, n);
                v = floor(v * _Bandas + 0.5) / _Bandas;            // bandas (toon)

                // mas densa hacia el bosque
                float d = distance(i.positionWS.xz, _CentroPlaza.xz);
                float dens = lerp(_DensidadCentro, 1.0, smoothstep(_RadioClaro, _RadioDenso, d));

                // desvanecer donde toca otros objetos (sin cortes rectos)
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float escena = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float borde = saturate((escena - i.screenPos.w) / _SuavidadBorde);

                // desvanecer muy cerca de la camara
                float cam = saturate((distance(_WorldSpaceCameraPos, i.positionWS) - 1.0) / _FadeCamara);

                float a = _Opacidad * v * dens * borde * cam * (1.0 - saturate(_NeblinaDisipada));
                half3 col = MixFog(_Color.rgb, i.fogFactor);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
