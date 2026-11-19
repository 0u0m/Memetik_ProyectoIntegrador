using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// El porton de regreso: 5 circulos que se encienden (uno por nota) y, con las 5,
// las rejas se abren y aparece la luz del portal. ProgresoNotas lo avisa.
public class PortonFinal : MonoBehaviour
{
    [Header("Circulos (en orden)")]
    public List<Renderer> circulos = new List<Renderer>();
    public Color[] colores = {
        Color.white, Color.white, Color.white, Color.white, Color.white
    };

    [Header("Rejas")]
    public Transform hojaIzq;
    public Transform hojaDer;
    [Tooltip("Cuanto giran al abrirse")]
    public float anguloApertura = 100f;
    public float duracionApertura = 2.5f;
    [Tooltip("Sentido de giro de cada hoja (1 o -1). Si una abre hacia afuera, cambia su signo")]
    public float signoIzq = -1f;
    public float signoDer = 1f;

    [Header("Portal")]
    public Renderer portal;     // plano detras de la reja (opcional)
    public Light luzPortal;     // luz blanca (opcional)

    int encendidos = 0;
    readonly List<Material> mats = new List<Material>();
    Quaternion izq0, der0;

void Awake()
    {
        // instancia de material por circulo, para animar cada uno por separado
        var sh = Shader.Find("Porton/Circulo Nota");
        for (int i = 0; i < circulos.Count; i++)
        {
            if (circulos[i] == null) { mats.Add(null); continue; }
            var m = new Material(sh);
            m.SetColor("_Color", colores[i % colores.Length]);
            m.SetColor("_Apagado", new Color(0.04f, 0.04f, 0.05f));
            m.SetFloat("_Intensidad", 6f);
            m.SetFloat("_Encendido", 0f);
            circulos[i].material = m;
            mats.Add(m);
        }
        if (hojaIzq != null) izq0 = hojaIzq.localRotation;
        if (hojaDer != null) der0 = hojaDer.localRotation;
        if (luzPortal != null) luzPortal.intensity = 0f;
        if (portal != null) portal.enabled = false;
    }

    // Lo llama ProgresoNotas cada vez que recoges una nota
// Lo llama ProgresoNotas cada vez que recoges una nota: solo enciende circulos.
    // La reja se abre con el panel de la palabra secreta (AbrirPorton).
    public void NotaRecogida(int totalRecogidas)
    {
        int meta = Mathf.Min(totalRecogidas, circulos.Count);
        while (encendidos < meta) StartCoroutine(Encender(encendidos++));
    }

    public bool Abierto { get; private set; }

    /// <summary>Abre la reja y enciende el portal (lo llama el panel de la palabra secreta).</summary>
    public void AbrirPorton()
    {
        if (Abierto) return;
        Abierto = true;
        StartCoroutine(Abrir());
    }

    IEnumerator Encender(int i)
    {
        if (i >= mats.Count) yield break;
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime / 0.8f; mats[i].SetFloat("_Encendido", Mathf.SmoothStep(0, 1, t)); yield return null; }
        mats[i].SetFloat("_Encendido", 1f);
    }

    IEnumerator Abrir()
    {
        yield return new WaitForSeconds(0.5f);
        if (portal != null) portal.enabled = true;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionApertura;
            float e = Mathf.SmoothStep(0, 1, t);
            if (hojaIzq != null) hojaIzq.localRotation = izq0 * Quaternion.Euler(0, signoIzq * anguloApertura * e, 0);
            if (hojaDer != null) hojaDer.localRotation = der0 * Quaternion.Euler(0, signoDer * anguloApertura * e, 0);
            if (luzPortal != null) luzPortal.intensity = Mathf.Lerp(0f, 12f, e);
            yield return null;
        }
    }
}
