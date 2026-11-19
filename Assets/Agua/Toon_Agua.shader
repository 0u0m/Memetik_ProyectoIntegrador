Shader "Agua/Toon Lago"
{
    Properties
    {
        _ShallowColor ("Color orilla", Color) = (0.35, 0.85, 0.85, 0.75)
        _DeepColor ("Color profundo", Color) = (0.08, 0.38, 0.62, 0.95)
        _DepthDist ("Distancia de profundidad", Float) = 6
        _FoamColor ("Color espuma", Color) = (1, 1, 1, 1)
        _FoamDist ("Ancho espuma orilla", Float) = 1.2
        _WaveScale ("Tamano de olas", Float) = 0.06
        _WaveSpeed ("Velocidad", Float) = 0.4
        _LineAmount ("Lineas de brillo", Range(0, 1)) = 0.35
        _SpecColor2 ("Color brillo sol", Color) = (1, 1, 0.95, 1)
        _Gloss ("Tamano brillo sol", Range(8, 256)) = 80
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                float _DepthDist;
                half4 _FoamColor;
                float _FoamDist;
                float _WaveScale;
                float _WaveSpeed;
                half _LineAmount;
                half4 _SpecColor2;
                half _Gloss;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), u.x),
                            lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), u.x), u.y);
            }
            float olas(float2 p, float t)
            {
                return noise(p + float2(t, t * 0.6)) * 0.6 + noise(p * 2.1 - float2(t * 0.8, -t * 0.5)) * 0.4;
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
                // profundidad del agua (con el depth texture de la camara)
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float surfEye = i.screenPos.w;
                float prof = max(0, sceneEye - surfEye);

                float t = _Time.y * _WaveSpeed;
                float2 p = i.positionWS.xz * _WaveScale;

                // normal procedural
                float e = 0.15;
                float h0 = olas(p, t);
                float hx = olas(p + float2(e, 0), t);
                float hz = olas(p + float2(0, e), t);
                float3 n = normalize(float3((h0 - hx) * 2.5, 1, (h0 - hz) * 2.5));

                // color por profundidad, en 3 bandas (estilo toon)
                float d = saturate(prof / _DepthDist);
                d = floor(d * 3.0 + 0.5) / 3.0;
                half4 col = lerp(_ShallowColor, _DeepColor, d);

                // lineas de brillo animadas
                float lineas = step(0.72, olas(p * 1.7, t * 1.3));
                col.rgb += lineas * _LineAmount;

                // brillo del sol escalonado
                Light l = GetMainLight();
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 hdir = normalize(l.direction + v);
                half spec = pow(saturate(dot(n, hdir)), _Gloss);
                col.rgb += step(0.5, spec) * _SpecColor2.rgb;

                // espuma en la orilla (con borde ondulado)
                float borde = _FoamDist * (0.6 + 0.8 * noise(i.positionWS.xz * 0.15 + t));
                float espuma = step(prof, borde);
                col = lerp(col, _FoamColor, espuma);

                col.rgb *= lerp(0.85, 1.0, saturate(dot(n, l.direction)));
                col.rgb = MixFog(col.rgb, i.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
}
