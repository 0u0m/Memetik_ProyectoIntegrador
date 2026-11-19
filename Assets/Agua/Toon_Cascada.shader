Shader "Agua/Toon Cascada"
{
    // Cortina de agua que cae: franjas que bajan, estilo toon.
    Properties
    {
        _ColorAgua ("Color agua", Color) = (0.45, 0.85, 0.95, 0.65)
        _ColorFranja ("Color franjas", Color) = (0.9, 1.0, 1.0, 0.9)
        _Velocidad ("Velocidad de caida", Float) = 1.2
        _FrecVertical ("Largo de franjas", Float) = 0.6
        _NumFranjas ("Numero de franjas alrededor", Float) = 18
        _Corte ("Cantidad de franjas", Range(0, 1)) = 0.55
        _CentroOS ("Centro de la malla (auto)", Vector) = (0, 0, 0, 0)
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
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorAgua;
                half4 _ColorFranja;
                float _Velocidad;
                float _FrecVertical;
                float _NumFranjas;
                half _Corte;
                float4 _CentroOS;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

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
                o.positionOS = i.positionOS.xyz - _CentroOS.xyz;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float ang = atan2(i.positionOS.z, i.positionOS.x) / (2.0 * PI) + 0.5;
                float y = i.positionOS.y;
                float t = _Time.y * _Velocidad;
                float col = floor(ang * _NumFranjas);
                float offset = hash21(float2(col, 3.1)) * 10.0;
                float n = vnoise(float2(ang * _NumFranjas, (y * _FrecVertical) + t + offset));
                float franja = step(1.0 - _Corte * 0.6, n);
                half4 c = lerp(_ColorAgua, _ColorFranja, franja);
                c.rgb = MixFog(c.rgb, i.fogFactor);
                return c;
            }
            ENDHLSL
        }
    }
}
