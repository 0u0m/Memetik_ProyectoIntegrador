Shader "Bosque/Toon Pasto"
{
    Properties
    {
        _RootColor ("Color raiz", Color) = (0.13, 0.32, 0.12, 1)
        _TipColor ("Color punta", Color) = (0.55, 0.82, 0.32, 1)
        _TipColor2 ("Color punta (variacion)", Color) = (0.72, 0.85, 0.35, 1)
        _VarScale ("Tamano manchas de color", Float) = 0.015
        _ShadowTint ("Tinte de sombra", Color) = (0.6, 0.65, 0.75, 1)
        _WindStrength ("Fuerza viento", Float) = 0.35
        _WindSpeed ("Velocidad viento", Float) = 1.6
        _WindScale ("Tamano rafagas", Float) = 0.05
        _WindDir ("Direccion viento", Vector) = (1, 0, 0.4, 0)
        _GIIntensidad ("Luz indirecta (GI)", Range(0, 8)) = 3
        [Header(Realismo)]
        _Realismo ("Realismo (0 = toon, 1 = PBR)", Range(0, 1)) = 1
        _Smoothness ("Lisura", Range(0, 1)) = 0.1
        _Translucidez ("Luz a traves del pasto", Range(0, 2)) = 0.9
        _GIReal ("Luz indirecta (realista)", Range(0, 4)) = 2.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _RootColor;
            half4 _TipColor;
            half4 _TipColor2;
            float _VarScale;
            half4 _ShadowTint;
            float _WindStrength;
            float _WindSpeed;
            float _WindScale;
            float4 _WindDir;
            half _GIIntensidad;
            half _Realismo;
            half _Smoothness;
            half _Translucidez;
            half _GIReal;
        CBUFFER_END

        float hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
        float vnoise(float2 p)
        {
            float2 i = floor(p), f = frac(p);
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                        lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
        }

        float3 Viento(float3 posWS, float alto)
        {
            float t = _Time.y * _WindSpeed;
            float2 dir = normalize(_WindDir.xz + 1e-4);
            float rafaga = vnoise(posWS.xz * _WindScale - dir * t * 0.5);
            float vaiven = sin(t * 2.0 + dot(posWS.xz, float2(0.7, 0.3))) * 0.3;
            float k = alto * alto * _WindStrength;
            float3 off = float3(dir.x, 0, dir.y) * (rafaga + vaiven) * k;
            off.y = -length(off.xz) * 0.3;
            return off;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Casas/ToonLuces.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float alto : TEXCOORD1;
                float3 baseWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 pws = TransformObjectToWorld(i.positionOS.xyz);
                float3 baseWS = TransformObjectToWorld(float3(0, 0, 0));
                float h = saturate(i.uv.y);
                pws += Viento(baseWS, h) * (length(TransformObjectToWorldDir(float3(0, 1, 0), false)));
                o.positionWS = pws;
                o.positionCS = TransformWorldToHClip(pws);
                o.alto = h;
                o.baseWS = baseWS;
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float v = vnoise(i.baseWS.xz * _VarScale);
                half3 tip = lerp(_TipColor.rgb, _TipColor2.rgb, smoothstep(0.35, 0.75, v));
                half3 col = lerp(_RootColor.rgb, tip, smoothstep(0.0, 1.0, i.alto));

                // pasto: normal hacia arriba para luz suave y uniforme
                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(half3(0, 1, 0), l.direction)) * l.shadowAttenuation;
                half b = step(0.35, ndl);
                col *= lerp(_ShadowTint.rgb, half3(1, 1, 1), b) * l.color;

                half3 albedoPasto = lerp(_RootColor.rgb, tip, smoothstep(0.0, 1.0, i.alto));
                col += albedoPasto * ToonGI(0, float3(0, 1, 0), i.positionCS) * _GIIntensidad;
                col += ToonLucesAdicionales(i.positionWS, float3(0, 1, 0), i.positionCS, albedoPasto);

                // --- modo realista (PBR): la punta se oscurece hacia la raiz y deja pasar luz ---
                if (_Realismo > 0.001)
                {
                    float3 vP = normalize(GetWorldSpaceViewDir(i.positionWS));
                    // normal inclinada hacia la camara: con la normal vertical, de lado el Fresnel
                    // reflejaba el cielo en las puntas y las hojas se veian como tocones oscuros
                    float3 nP = normalize(float3(0, 1, 0) + float3(vP.x, 0, vP.z) * 0.8);
                    half3 albedoR = albedoPasto * lerp(0.75, 1.0, i.alto);
                    // piso de luz ambiente del cielo: las sondas pegadas a muros y arboles quedan muy oscuras
                    // y el pasto se veia negro sobre un suelo (estatico, con lightmap) mas claro
                    half3 giP = max(ToonGI(0, nP, i.positionCS), unity_AmbientSky.rgb) * _GIReal;
                    half3 real = IluminacionReal(albedoR, 0, _Smoothness, i.positionWS, nP, vP, i.positionCS, giP, _Translucidez);
                    col = lerp(col, real, _Realismo);
                }
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
