Shader "Casas/Toon Degradado"
{
    // Replica el Shader Graph "Casa 2" (canal R -> x2.4 -> Saturation -> Sample Gradient)
    // con iluminacion toon en bandas, igual que "Casas/Toon Texturizado".
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _Multiplicador ("Multiplicador canal R", Float) = 2.4
        _SatIn ("Entrada Saturation (In)", Vector) = (0.06, 1.33, -2.95, 0)
        _ColorA ("Degradado color A", Color) = (0.2736, 0.1048, 0.0994, 1)
        _PosA ("Degradado posicion A", Range(0, 1)) = 0.4971
        _ColorB ("Degradado color B", Color) = (1, 1, 1, 1)
        _PosB ("Degradado posicion B", Range(0, 1)) = 0.8647
        _ShadowTint ("Tinte de sombra", Color) = (0.55, 0.5, 0.68, 1)
        _Steps ("Bandas", Range(1, 5)) = 2
        _Suavidad ("Suavidad del corte", Range(0.001, 0.2)) = 0.03
        _RimColor ("Color borde", Color) = (1, 0.95, 0.85, 1)
        _RimAmount ("Cantidad borde", Range(0, 1)) = 0.15
        _RimPower ("Fuerza borde", Range(0.5, 8)) = 4
        _GIIntensidad ("Luz indirecta (GI)", Range(0, 8)) = 3
        [Header(Realismo)]
        _Realismo ("Realismo (0 = toon, 1 = PBR)", Range(0, 1)) = 1
        [Normal] _BumpMap ("Normal map", 2D) = "bump" {}
        _BumpScale ("Fuerza del normal map", Range(0, 2)) = 1
        _RoughMap ("Mapa de rugosidad", 2D) = "white" {}
        _UsarRugosidad ("Usar mapa de rugosidad (0/1)", Range(0, 1)) = 0
        _Smoothness ("Lisura", Range(0, 1)) = 0.3
        _Metallic ("Metalico", Range(0, 1)) = 0
        _GIReal ("Luz indirecta (realista)", Range(0, 4)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float _Multiplicador;
            float4 _SatIn;
            half4 _ColorA;
            float _PosA;
            half4 _ColorB;
            float _PosB;
            half4 _ShadowTint;
            half _Steps;
            half _Suavidad;
            half4 _RimColor;
            half _RimAmount;
            half _RimPower;
            half _GIIntensidad;
            half _Realismo;
            half _BumpScale;
            half _UsarRugosidad;
            half _Smoothness;
            half _Metallic;
            half _GIReal;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
        TEXTURE2D(_RoughMap); SAMPLER(sampler_RoughMap);

        // Mismo calculo de color que el Shader Graph "Casa 2" (lo usan ForwardLit y Meta)
        half3 AlbedoDegradado(float2 uv)
        {
            float r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).r;
            float s = r * _Multiplicador;
            float3 inV = _SatIn.xyz;
            float luma = dot(inV, float3(0.2126729, 0.7151522, 0.0721750));
            float t = luma + s * (inV.x - luma);
            float k = saturate((t - _PosA) / max(_PosB - _PosA, 1e-4));
            return lerp(_ColorA.rgb, _ColorB.rgb, k);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Casas/ToonLuces.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float2 uv : TEXCOORD0; float2 uv2 : TEXCOORD2; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float2 dynLmUV : TEXCOORD4;
                float4 tangentWS : TEXCOORD5;
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                VertexNormalInputs ni = GetVertexNormalInputs(i.normalOS, i.tangentOS);
                o.normalWS = ni.normalWS;
                o.tangentWS = float4(ni.tangentWS, i.tangentOS.w * GetOddNegativeScale());
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                o.dynLmUV = i.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
                return o;
            }

            // normal con normal map (si la malla no trae tangentes, se queda la normal geometrica)
            float3 NormalReal(float3 n, float4 tangentWS, float2 uv)
            {
                float3 t = tangentWS.xyz;
                if (dot(t, t) < 1e-4) return n;
                t = normalize(t - n * dot(n, t));
                float3 b = cross(n, t) * (tangentWS.w < 0 ? -1.0 : 1.0);
                half3 nt = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
                return normalize(nt.x * t + nt.y * b + nt.z * n);
            }
            half Lisura(float2 uv)
            {
                half r = SAMPLE_TEXTURE2D(_RoughMap, sampler_RoughMap, uv).g;
                return lerp(_Smoothness, saturate((1 - r) * _Smoothness * 2), _UsarRugosidad);
            }

            half4 frag (Varyings i) : SV_Target
            {
                // --- igual que el Shader Graph ---
                half3 albedo = AlbedoDegradado(i.uv);

                // --- iluminacion toon ---
                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 n = normalize(i.normalWS);
                half ndl = saturate(dot(n, l.direction)) * l.shadowAttenuation;
                half x = ndl * _Steps;
                half b = (floor(x) + smoothstep(0.5 - _Suavidad, 0.5 + _Suavidad, frac(x))) / _Steps;
                half3 col = albedo * lerp(_ShadowTint.rgb, half3(1, 1, 1), b) * l.color;

                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1.0 - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * step(0.5, rim) * _RimAmount * b;

                col += albedo * ToonGI(i.dynLmUV, n, i.positionCS) * _GIIntensidad;
                col += ToonLucesAdicionales(i.positionWS, n, i.positionCS, albedo);

                // --- modo realista (PBR) ---
                if (_Realismo > 0.001)
                {
                    float3 nR = NormalReal(n, i.tangentWS, i.uv);
                    half3 real = IluminacionReal(albedo, _Metallic, Lisura(i.uv), i.positionWS, nR, v, i.positionCS,
                                                 ToonGI(i.dynLmUV, nR, i.positionCS) * _GIReal, 0);
                    col = lerp(col, real, _Realismo);
                }
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            // Le da a Enlighten el color de la superficie para calcular el rebote de luz
            Name "Meta"
            Tags { "LightMode"="Meta" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex UniversalVertexMeta
            #pragma fragment fragMeta
            #pragma shader_feature EDITOR_VISUALIZATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"
            half4 fragMeta (Varyings i) : SV_Target
            {
                MetaInput m = (MetaInput)0;
                m.Albedo = AlbedoDegradado(i.uv);
                return UniversalFragmentMeta(i, m);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertS
            #pragma fragment fragS
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vertS (Attributes i)
            {
                Varyings o;
                float3 pws = TransformObjectToWorld(i.positionOS.xyz);
                float3 nws = TransformObjectToWorldNormal(i.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 ld = normalize(_LightPosition - pws);
                #else
                    float3 ld = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(pws, nws, ld));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 fragS (Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            struct AttributesDN { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct VaryingsDN { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            VaryingsDN vertDN (AttributesDN i)
            {
                VaryingsDN o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }
            half4 fragDN (VaryingsDN i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct * 0.5 + 0.5)), 0.0);
                #else
                    return half4(n, 0.0);
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vertD
            #pragma fragment fragD
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vertD (Attributes i) { Varyings o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half fragD (Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
