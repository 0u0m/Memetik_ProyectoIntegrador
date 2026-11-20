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

// Luz indirecta (GI dinamico).
//  - Objetos estaticos (Contribute GI): lightmap de Enlighten en tiempo real (DYNAMICLIGHTMAP_ON),
//    direccional si el Lighting Settings lo pide (DIRLIGHTMAP_COMBINED: varia con la normal).
//  - Objetos dinamicos (arboles, pasto, hada...): light probes / ambiente (SH), que Enlighten
//    tambien actualiza en runtime cuando Amanecer cambia el sol y el cielo.
//  - Si el renderer tiene SSAO, oscurece la luz indirecta en esquinas y contactos.
half3 ToonGI(float2 dynamicLightmapUV, float3 n, float4 posCS)
{
#if defined(DYNAMICLIGHTMAP_ON)
    half3 gi = SampleLightmap(0, dynamicLightmapUV, n);
#else
    half3 gi = SampleSH(n);
#endif
#if defined(_SCREEN_SPACE_OCCLUSION)
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(posCS));
    gi *= ao.indirectAmbientOcclusion;
#endif
    return gi;
}

// ======================= Modo realista =======================
// Iluminacion fisica de URP (la misma BRDF que URP/Lit): difusa + especular GGX de
// la luz principal y de las adicionales (con sus sombras), mas GI con reflejos del
// entorno y Fresnel. Los shaders mezclan esto con su version toon segun _Realismo
// (0 = toon original del equipo, 1 = realista).

// Luz que atraviesa hojas/pasto cuando la fuente esta detras (subsurface barato)
half3 Translucidez(half3 albedo, Light l, float3 n, float3 v)
{
    half3 h = normalize(-l.direction + n * 0.35);
    half t = pow(saturate(dot(v, h)), 4.0);
    return albedo * l.color * (t * l.distanceAttenuation * l.shadowAttenuation);
}

half3 IluminacionReal(half3 albedo, half metallic, half smoothness, float3 posWS, float3 n, float3 v,
                      float4 posCS, half3 gi, half translucidez)
{
    BRDFData brdf; half alpha = 1;
    InitializeBRDFData(albedo, metallic, half3(0, 0, 0), smoothness, alpha, brdf);
    half3 c = GlobalIllumination(brdf, gi, 1, posWS, n, v);

    Light ml = GetMainLight(TransformWorldToShadowCoord(posWS));
    c += LightingPhysicallyBased(brdf, ml, n, v);
    c += Translucidez(albedo, ml, n, v) * translucidez;

#if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS) || defined(_CLUSTER_LIGHT_LOOP)
    InputData inputData = (InputData)0;
    inputData.positionWS = posWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(posCS);
    uint count = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
        Light l = GetAdditionalLight(lightIndex, posWS, half4(1, 1, 1, 1));
        c += LightingPhysicallyBased(brdf, l, n, v);
        c += Translucidez(albedo, l, n, v) * translucidez;
    LIGHT_LOOP_END
#endif
    return c;
}

// Ruido de valor para romper colores planos (arboles, pasto)
float RuidoReal(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    #define H(q) frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453)
    float r = lerp(lerp(H(i), H(i + float2(1, 0)), u.x), lerp(H(i + float2(0, 1)), H(i + float2(1, 1)), u.x), u.y);
    #undef H
    return r;
}
#endif
