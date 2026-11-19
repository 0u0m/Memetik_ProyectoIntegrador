Shader "Hada/Brillo"
{
    // Esfera de luz aditiva: brilla mas al centro y se desvanece en la orilla. Late suavemente.
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.7, 0.25, 1)
        _Intensidad ("Intensidad", Float) = 3
        _Potencia ("Suavidad de la orilla", Float) = 2
        _Pulso ("Velocidad del latido", Float) = 4
        _CantidadPulso ("Fuerza del latido", Range(0, 1)) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; half _Intensidad, _Potencia, _Pulso, _CantidadPulso;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 n : TEXCOORD0; float3 pws : TEXCOORD1; };
            V vert (A i)
            {
                V o;
                o.pws = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.pws);
                o.n = TransformObjectToWorldNormal(i.normalOS);
                return o;
            }
            half4 frag (V i) : SV_Target
            {
                float3 v = normalize(GetWorldSpaceViewDir(i.pws));
                float f = pow(saturate(dot(normalize(i.n), v)), _Potencia);
                float pulso = 1.0 + sin(_Time.y * _Pulso) * _CantidadPulso;
                return half4(_Color.rgb * _Intensidad * f * pulso, 1);
            }
            ENDHLSL
        }
    }
}
