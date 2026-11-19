Shader "Cielo/Noche Toon"
{
    Properties
    {
        _ColorCenit ("Color arriba", Color) = (0.03, 0.05, 0.14, 1)
        _ColorHorizonte ("Color horizonte", Color) = (0.12, 0.17, 0.32, 1)
        _ColorSuelo ("Color abajo", Color) = (0.04, 0.05, 0.1, 1)
        _DensidadEstrellas ("Densidad estrellas", Range(0.9, 1)) = 0.996
        _TamEstrellas ("Tamano estrellas", Float) = 140
        _Estrellas ("Brillo estrellas (amanecer)", Range(0, 1)) = 1
        _LunaDir ("Direccion luna (auto)", Vector) = (0.3, 0.5, 0.8, 0)
        _LunaTam ("Tamano luna", Range(0.005, 0.1)) = 0.035
        _LunaColor ("Color luna", Color) = (1, 0.97, 0.85, 1)
        _HaloColor ("Color halo", Color) = (0.35, 0.45, 0.7, 1)
        _LunaVisible ("Luna visible (amanecer)", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCenit, _ColorHorizonte, _ColorSuelo;
                float _DensidadEstrellas, _TamEstrellas, _Estrellas;
                float4 _LunaDir;
                float _LunaTam, _LunaVisible;
                half4 _LunaColor, _HaloColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            float hash31(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.dir = i.positionOS.xyz;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half3 col;
                if (d.y >= 0)
                {
                    float h = pow(saturate(d.y), 0.55);
                    // degradado en 3 bandas suaves (estilo toon)
                    float hb = floor(h * 5.0) / 5.0;
                    h = lerp(h, hb, 0.5);
                    col = lerp(_ColorHorizonte.rgb, _ColorCenit.rgb, h);
                }
                else col = lerp(_ColorHorizonte.rgb, _ColorSuelo.rgb, saturate(-d.y * 4));

                // estrellas (se apagan con el amanecer)
                float3 celda = floor(d * _TamEstrellas);
                float r = hash31(celda);
                if (r > _DensidadEstrellas && d.y > 0.04)
                {
                    float3 centro = (celda + 0.5) / _TamEstrellas;
                    float dist = length(d - normalize(centro)) * _TamEstrellas;
                    float brillo = step(dist, 0.35 + frac(r * 91.3) * 0.25);
                    float tw = 0.7 + 0.3 * sin(_Time.y * (1.5 + frac(r * 13.7) * 3) + r * 50);
                    col += brillo * tw * saturate(d.y * 3) * _Estrellas;
                }

                // luna (disco con borde duro) + halo en bandas; se desvanece con el amanecer
                float3 L = normalize(_LunaDir.xyz);
                float c = dot(d, L);
                float ang = acos(clamp(c, -1, 1));
                float disco = step(ang, _LunaTam);
                float halo = saturate(1 - ang / (_LunaTam * 6));
                halo = floor(halo * 3) / 3;
                col = lerp(col, col + _HaloColor.rgb * 0.5, halo * _LunaVisible);
                col = lerp(col, _LunaColor.rgb * 1.6, disco * _LunaVisible);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
