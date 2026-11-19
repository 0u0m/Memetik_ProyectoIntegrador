Shader "Porton/Circulo Nota"
{
    // Circulo del arco: apagado oscuro; encendido brilla con su color (resalta con el Bloom de noche).
    Properties
    {
        [HDR] _Color ("Color encendido", Color) = (1, 0.8, 0.2, 1)
        _Apagado ("Color apagado", Color) = (0.04, 0.04, 0.05, 1)
        _Encendido ("Encendido (0-1)", Range(0, 1)) = 0
        _Intensidad ("Intensidad del brillo", Float) = 6
        _Pulso ("Velocidad del latido", Float) = 3
        _CantidadPulso ("Fuerza del latido", Range(0, 1)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _Apagado;
                half _Encendido, _Intensidad, _Pulso, _CantidadPulso;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert (A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half4 frag (V i) : SV_Target
            {
                float pulso = 1.0 + sin(_Time.y * _Pulso) * _CantidadPulso;
                half3 enc = _Color.rgb * _Intensidad * pulso;
                half3 col = lerp(_Apagado.rgb, enc, _Encendido);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
