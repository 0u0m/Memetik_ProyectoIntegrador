Shader "Casas/Toon Texturizado"
{
    Properties
    {
        _BaseMap ("Textura", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ShadowTint ("Tinte de sombra", Color) = (0.55, 0.5, 0.68, 1)
        _Steps ("Bandas", Range(1, 5)) = 2
        _Suavidad ("Suavidad del corte", Range(0.001, 0.2)) = 0.03
        _Saturacion ("Saturacion", Range(0, 2)) = 1.2
        _RimColor ("Color borde", Color) = (1, 0.95, 0.85, 1)
        _RimAmount ("Cantidad borde", Range(0, 1)) = 0.15
        _RimPower ("Fuerza borde", Range(0.5, 8)) = 4
        [HDR] _EmissionColor ("Emision (brillo propio)", Color) = (0, 0, 0, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Caras visibles", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowTint;
            half _Steps;
            half _Suavidad;
            half _Saturacion;
            half4 _RimColor;
            half _RimAmount;
            half _RimPower;
            half4 _EmissionColor;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Casas/ToonLuces.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert (Attributes i)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(i.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i, bool esFrente : SV_IsFrontFace) : SV_Target
            {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                half gris = dot(albedo, half3(0.299, 0.587, 0.114));
                albedo = lerp(gris.xxx, albedo, _Saturacion);

                Light l = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 n = normalize(i.normalWS);
                n = esFrente ? n : -n; // cara de atras: voltear la normal
                half ndl = saturate(dot(n, l.direction)) * l.shadowAttenuation;

                half x = ndl * _Steps;
                half f = frac(x);
                half t = (floor(x) + smoothstep(0.5 - _Suavidad, 0.5 + _Suavidad, f)) / _Steps;

                half3 luz = lerp(_ShadowTint.rgb, half3(1, 1, 1), t) * l.color;
                half3 col = albedo * luz;

                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half rim = pow(1.0 - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * step(0.5, rim) * _RimAmount * t;

                col += ToonLucesAdicionales(i.positionWS, n, i.positionCS, albedo);
                col += _EmissionColor.rgb;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull [_Cull]
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
            Cull [_Cull]
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
            Cull [_Cull]
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
