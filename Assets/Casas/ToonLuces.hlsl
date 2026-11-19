#ifndef TOON_LUCES_INCLUDED
#define TOON_LUCES_INCLUDED
// Luces puntuales/foco con corte en bandas (estilo toon). Lo usan los shaders del pueblo.
half3 ToonLucesAdicionales(float3 posWS, float3 n, float4 posCS, half3 albedo)
{
    half3 suma = 0;
#if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS) || defined(_CLUSTER_LIGHT_LOOP)
    InputData inputData = (InputData)0;
    inputData.positionWS = posWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(posCS);
    uint count = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
        Light l = GetAdditionalLight(lightIndex, posWS);
        half lum = max(l.color.r, max(l.color.g, l.color.b));
        half v = l.distanceAttenuation * lum * saturate(dot(n, l.direction) * 0.8 + 0.2);
        half b = smoothstep(0.08, 0.11, v) * 0.5 + smoothstep(0.35, 0.40, v) * 0.5;
        suma += albedo * (l.color / max(lum, 1e-4)) * b * 1.2;
    LIGHT_LOOP_END
#endif
    return suma;
}
#endif
