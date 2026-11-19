using UnityEngine;

// El hada: una bola de fuego magica que flota como si estuviera viva
// y cuya luz parpadea como llama.
public class HadaFuego : MonoBehaviour
{
    [Header("Flotar")]
    [Tooltip("Cuanto sube y baja")]
    public float alturaVaiven = 0.5f;
    public float velocidadVaiven = 1.6f;
    [Tooltip("Cuanto se mueve hacia los lados")]
    public float paseo = 0.6f;
    public float velocidadPaseo = 0.7f;

    [Header("Seguir al jugador")]
    public bool seguirJugador = true;
    [Tooltip("Que tan adelante del jugador flota")]
    public float adelante = 8f;
    [Tooltip("Que tan a la derecha (negativo = izquierda)")]
    public float lado = 3.5f;
    [Tooltip("Altura sobre el suelo del jugador (los ojos estan a 5.4)")]
    public float altura = 5f;
    [Tooltip("Segundos que tarda en alcanzarte (mas = mas retraso)")]
    public float retraso = 0.6f;

    [Header("Luz")]
    public Light luz;
    [Tooltip("Que tanto parpadea la luz (0 = nada)")]
    [Range(0f, 1f)] public float parpadeo = 0.25f;
    public float velocidadParpadeo = 9f;

    Vector3 posBase, velSeguir;
    float intensidadBase, semilla;
    Transform jugador;
    JugadorDoom jugadorDoom;

    void Start()
    {
        posBase = transform.position;
        semilla = Random.value * 100f;
        if (luz == null) luz = GetComponentInChildren<Light>();
        if (luz != null) intensidadBase = luz.intensity;
        var j = FindFirstObjectByType<JugadorDoom>();
        if (j != null) { jugador = j.transform; jugadorDoom = j; }
    }

    /// <summary>Mueve el punto alrededor del cual flota (para cuando siga al jugador).</summary>
    public void IrA(Vector3 nuevaBase) { posBase = nuevaBase; }

    void Update()
    {
        if (seguirJugador && jugador != null)
        {
            // Se acomoda respecto a hacia donde mira la camara, para quedar siempre a la vista.
            // Mientras usas un tablero se queda donde esta (para no tapar el acertijo).
            // Mientras usas un tablero o mientras la camara viaja a los circulos, se queda donde esta (junto a ti)
            bool enTablero = (jugadorDoom != null && jugadorDoom.vistaForzada != null) || CamaraNota.EnAnimacion;
            Transform cam = jugadorDoom != null ? jugadorDoom.camara : null;
            if (!enTablero)
            {
                Vector3 objetivo;
                if (cam != null) objetivo = cam.position + cam.forward * adelante + cam.right * lado + cam.up * (altura - jugadorDoom.alturaOjos);
                else objetivo = jugador.position + jugador.forward * adelante + jugador.right * lado + Vector3.up * altura;
                // que no se meta en el suelo
                var terreno = Terrain.activeTerrain;
                if (terreno != null)
                {
                    float suelo = terreno.SampleHeight(objetivo) + terreno.transform.position.y;
                    objetivo.y = Mathf.Max(objetivo.y, suelo + 2f);
                }
                posBase = Vector3.SmoothDamp(posBase, objetivo, ref velSeguir, retraso);
            }
        }

        float t = Time.time + semilla;
        Vector3 o = new Vector3(
            (Mathf.PerlinNoise(t * velocidadPaseo, 0.3f) - 0.5f) * 2f * paseo,
            Mathf.Sin(t * velocidadVaiven) * alturaVaiven,
            (Mathf.PerlinNoise(0.7f, t * velocidadPaseo) - 0.5f) * 2f * paseo);
        transform.position = posBase + o;

        if (luz != null)
        {
            float n = Mathf.PerlinNoise(t * velocidadParpadeo, 5.1f);
            luz.intensity = intensidadBase * (1f + (n - 0.5f) * 2f * parpadeo);
        }
    }
}
