using UnityEngine;
using UnityEngine.Rendering;

// Con cada nota el cielo avanza de la noche hacia la HORA DORADA (sin llegar al dia).
// Al empezar toma los valores de noche que tenga la escena; al final llega a los colores de abajo.
// Todo se cambia solo mientras se juega (al salir de Play la escena queda como estaba).
public class Amanecer : MonoBehaviour
{
    [Header("Avance")]
    [Tooltip("Segundos que tarda en avanzar despues de cada nota")]
    public float duracionCambio = 4f;
    [Tooltip("Hasta donde llega con las 5 notas (1 = hora dorada completa)")]
    [Range(0f, 1f)] public float limite = 1f;

    [Header("Cielo en hora dorada")]
    public Color cenitDorado = new Color(0.22f, 0.22f, 0.45f);
    public Color horizonteDorado = new Color(1.0f, 0.55f, 0.28f);
    public Color sueloDorado = new Color(0.30f, 0.18f, 0.16f);

    [Header("Sol bajito")]
    public Color luzDorada = new Color(1f, 0.62f, 0.32f);
    public float intensidadDorada = 1.1f;
    [Tooltip("Rotacion del sol al final (angulo bajo = luz rasante)")]
    public Vector3 rotacionDorada = new Vector3(9f, -40f, 0f);

    [Header("Ambiente y niebla")]
    public Color ambienteCieloDorado = new Color(0.55f, 0.40f, 0.38f);
    public Color ambienteMedioDorado = new Color(0.45f, 0.30f, 0.24f);
    public Color ambienteSueloDorado = new Color(0.16f, 0.12f, 0.12f);
    public Color nieblaDorada = new Color(0.60f, 0.40f, 0.32f);
    public Color neblinaVolumenDorada = new Color(0.70f, 0.50f, 0.42f);

    // valores de noche (se leen al empezar)
    Light sol; Color luzN; float intN; Quaternion rotN, rotD;
    Color ambCieloN, ambMedioN, ambSueloN, nieblaN, volN;
    Material cielo; Color cenitN, horizN, sueloN;
    Material volumen;
    float actual, objetivo;

    void Start()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null) { luzN = sol.color; intN = sol.intensity; rotN = sol.transform.rotation; }
        rotD = Quaternion.Euler(rotacionDorada);

        ambCieloN = RenderSettings.ambientSkyColor;
        ambMedioN = RenderSettings.ambientEquatorColor;
        ambSueloN = RenderSettings.ambientGroundColor;
        nieblaN = RenderSettings.fogColor;

        // copia del cielo para no tocar el material original
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_ColorCenit"))
        {
            cielo = new Material(RenderSettings.skybox);
            RenderSettings.skybox = cielo;
            cenitN = cielo.GetColor("_ColorCenit");
            horizN = cielo.GetColor("_ColorHorizonte");
            sueloN = cielo.GetColor("_ColorSuelo");
        }

        // copia del material de la neblina (para que el original no cambie)
        var vol = GameObject.Find("Volumen_Bosque");
        if (vol != null && vol.TryGetComponent(out Renderer r) && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
        {
            volumen = r.material;
            volN = volumen.GetColor("_Color");
        }
    }

    void Update()
    {
        var prog = ProgresoNotas.Instancia;
        if (prog != null && prog.totalNotas > 0)
            objetivo = limite * prog.Recogidas / (float)prog.totalNotas;

        if (Mathf.Approximately(actual, objetivo)) return;
        int pasos = (prog != null && prog.totalNotas > 0) ? prog.totalNotas : 5;
        float velocidad = limite / pasos / Mathf.Max(0.1f, duracionCambio); // un paso por nota en 'duracionCambio' segundos
        actual = Mathf.MoveTowards(actual, objetivo, velocidad * Time.deltaTime);
        Aplicar(Mathf.SmoothStep(0f, 1f, actual));
    }

    void Aplicar(float t)
    {
        if (sol != null)
        {
            sol.color = Color.Lerp(luzN, luzDorada, t);
            sol.intensity = Mathf.Lerp(intN, intensidadDorada, t);
            sol.transform.rotation = Quaternion.Slerp(rotN, rotD, t);
        }
        RenderSettings.ambientSkyColor = Color.Lerp(ambCieloN, ambienteCieloDorado, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(ambMedioN, ambienteMedioDorado, t);
        RenderSettings.ambientGroundColor = Color.Lerp(ambSueloN, ambienteSueloDorado, t);
        RenderSettings.fogColor = Color.Lerp(nieblaN, nieblaDorada, t);

        if (cielo != null)
        {
            cielo.SetColor("_ColorCenit", Color.Lerp(cenitN, cenitDorado, t));
            cielo.SetColor("_ColorHorizonte", Color.Lerp(horizN, horizonteDorado, t));
            cielo.SetColor("_ColorSuelo", Color.Lerp(sueloN, sueloDorado, t));
            cielo.SetFloat("_Estrellas", 1f - Mathf.Clamp01(t * 1.4f));   // las estrellas se apagan antes
            cielo.SetFloat("_LunaVisible", 1f - Mathf.Clamp01(t * 1.1f));
        }
        if (volumen != null) volumen.SetColor("_Color", Color.Lerp(volN, neblinaVolumenDorada, t));
        DynamicGI.UpdateEnvironment();
    }
}
