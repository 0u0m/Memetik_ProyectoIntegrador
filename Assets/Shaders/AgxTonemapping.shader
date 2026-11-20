// Tonemapping AgX (Troy Sobotka) para URP, como pase de pantalla completa.
//
// Se usa con el Tonemapping de URP en "None" y Color Grading en HDR: URP hace
// bloom y gradeo en HDR lineal, y este pase convierte el resultado a imagen de
// pantalla. Va en PC_Renderer y Mobile_Renderer como "Full Screen Pass Renderer Feature"
// (inyeccion: After Rendering Post Processing).
//
// Curva y matrices basadas en la aproximacion publica de AgX de Benjamin Wrensch
// (iolite engine, MIT), derivada de la configuracion OCIO original de AgX.
Shader "Hidden/Memetik/AgX"
{
    Properties
    {
        _Exposure ("Exposicion (EV)", Range(-3, 3)) = 0
        _LookPower ("Look: contraste (power)", Range(0.5, 2)) = 1
        _LookSaturation ("Look: saturacion", Range(0, 2)) = 1
        _LookSlope ("Look: pendiente / tinte", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "AgX"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Exposure;
            float _LookPower;
            float _LookSaturation;
            float4 _LookSlope;

            // Sigmoide de AgX (aproximacion polinomica, error^2 medio ~3.7e-6)
            float3 AgxContrast(float3 x)
            {
                float3 x2 = x * x;
                float3 x4 = x2 * x2;
                return 15.5 * x4 * x2 - 40.14 * x4 * x + 31.96 * x4 - 6.868 * x2 * x + 0.4298 * x2 + 0.1191 * x - 0.00232;
            }

            float3 AgxEncode(float3 v)
            {
                // "Inset": comprime las primarias hacia el acromatico para que
                // los colores muy brillantes tiendan al blanco sin torcer el tono.
                // (Mismas 9 cifras que la referencia GLSL; mul(v, M) == M*v en GLSL)
                const float3x3 inset = float3x3(
                    0.842479062253094, 0.0423282422610123, 0.0423756549057051,
                    0.0784335999999992, 0.878468636469772, 0.0784336,
                    0.0792237451477643, 0.0791661274605434, 0.879142973793104);
                const float minEv = -12.47393;
                const float maxEv = 4.026069;
                v = mul(v, inset);
                v = clamp(log2(max(v, 1e-10)), minEv, maxEv);
                v = (v - minEv) / (maxEv - minEv);
                return AgxContrast(v);
            }

            float3 AgxLook(float3 v)
            {
                float luma = dot(v, float3(0.2126, 0.7152, 0.0722));
                v = pow(max(v * _LookSlope.rgb, 0.0), _LookPower);
                return luma + _LookSaturation * (v - luma);
            }

            float3 AgxDecode(float3 v)
            {
                // "Outset": inversa del inset
                const float3x3 outset = float3x3(
                    1.19687900512017, -0.0528968517574562, -0.0529716355144438,
                    -0.0980208811401368, 1.15190312990417, -0.0980434501171241,
                    -0.0990297440797205, -0.0989611768448433, 1.15107367264116);
                return mul(v, outset);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                float3 v = c.rgb * exp2(_Exposure);
                v = AgxEncode(v);
                v = AgxLook(v);
                v = saturate(AgxDecode(v));     // valor de pantalla (codificado para sRGB)
                v = SRGBToLinear(v);            // el buffer es lineal: URP lo codifica a sRGB al final
                return half4(v, c.a);
            }
            ENDHLSL
        }
    }
}
