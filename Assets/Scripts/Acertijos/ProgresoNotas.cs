using UnityEngine;

// Lleva la cuenta de las notas recogidas y, con cada una, disipa la neblina
// (las capas de neblina baja y la niebla general de la escena).
// Despues aqui mismo se puede conectar el amanecer y la puerta final.
public class ProgresoNotas : MonoBehaviour
{
    public static ProgresoNotas Instancia { get; private set; }

    public int totalNotas = 5;
    [Tooltip("Cuanto se disipa la neblina baja con TODAS las notas (1 = desaparece por completo)")]
    [Range(0f, 1f)] public float disipadaAlFinal = 0.85f;
    [Tooltip("Cuanto queda de la niebla general con todas las notas (0.4 = 40%)")]
    [Range(0f, 1f)] public float nieblaGeneralAlFinal = 0.4f;
    [Tooltip("Segundos que tarda en disiparse un poco despues de recoger una nota")]
    public float duracionCambio = 3f;

    public int Recogidas { get; private set; }

    [Tooltip("El porton final (se le avisa cada nota). Opcional.")]
    public PortonFinal porton;
    [Tooltip("La camara que viaja a la casa final en cada nota. Opcional.")]
    public CamaraNota camaraNota;

    float disipada, objetivo, densidadBase, ultimoAviso = -99f;

    void Awake()
    {
        Instancia = this;
        densidadBase = RenderSettings.fogDensity;
        Shader.SetGlobalFloat("_NeblinaDisipada", 0f);
    }

public void NotaRecogida()
    {
        if (Recogidas >= totalNotas) return;          // ya estan todas
        if (Time.time - ultimoAviso < 0.5f) return;   // evita contar dos veces el mismo E
        ultimoAviso = Time.time;

        Recogidas = Mathf.Min(Recogidas + 1, totalNotas);
        objetivo = disipadaAlFinal * Recogidas / (float)totalNotas;
        if (porton != null) porton.NotaRecogida(Recogidas);
        if (camaraNota != null) camaraNota.Mostrar();
    }

    void Update()
    {
        disipada = Mathf.MoveTowards(disipada, objetivo, Time.deltaTime / duracionCambio * disipadaAlFinal / totalNotas);
        Shader.SetGlobalFloat("_NeblinaDisipada", disipada);
        float t = disipadaAlFinal > 0 ? disipada / disipadaAlFinal : 0f;
        RenderSettings.fogDensity = densidadBase * Mathf.Lerp(1f, nieblaGeneralAlFinal, t);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat("_NeblinaDisipada", 0f);
        RenderSettings.fogDensity = densidadBase;
    }
}
