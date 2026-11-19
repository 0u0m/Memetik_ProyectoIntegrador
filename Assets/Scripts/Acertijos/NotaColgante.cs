using System.Collections;
using UnityEngine;

// Nota dentro de la jaula. Al llamar Liberar() (por ejemplo desde AcertijoTablero.alResolver)
// cae despacio meciendose como hoja hasta la altura de la mano. Luego el jugador la recoge con E y la lee.
public class NotaColgante : MonoBehaviour
{
    [Header("Contenido")]
    public string titulo = "Nota 1";
    [TextArea(4, 10)]
    public string texto = "Si estas leyendo esto, ya entendiste como funcionan los tableros.\n\nTodavia faltan cuatro notas. El hada dijo que la puerta solo se abre con las cinco... y que el sol no espera.";

    [Header("Caida")]
    public float duracionCaida = 3.5f;
    [Tooltip("Altura final sobre el suelo (unidades). 4 = ~1.2 m, a la altura de la mano")]
    public float alturaFinal = 4.5f;
    public float distanciaRecoger = 12f;

    [Header("Nota escondida (sin jaula)")]
    [Tooltip("Activalo para notas que no estan en jaula: flotan desde el inicio, listas para recogerse")]
    public bool sueltaDesdeElInicio = false;

    public bool Liberada { get; private set; }
    public bool Recogida { get; private set; }
    bool leyendo, enSuelo, mostrarAviso;
    JugadorDoom jugador;
    Transform vista;
    Vector3 posFinal;
    Transform pivote;

    void Start()
    {
        jugador = FindFirstObjectByType<JugadorDoom>();
        if (sueltaDesdeElInicio) Soltar();
    }

    // Nota escondida: se queda flotando donde esta, sin caer
    void Soltar()
    {
        Liberada = true;
        var rend = GetComponent<Renderer>();
        pivote = new GameObject(name + "_Pivote").transform;
        pivote.position = rend != null ? rend.bounds.center : transform.position;
        pivote.rotation = Quaternion.identity;
        transform.SetParent(pivote, true);
        posFinal = pivote.position;
        enSuelo = true;
        var pi = pivote.gameObject.AddComponent<PuntoDeInteres>();
        pi.radio = 25f;
    }

    public void Liberar()
    {
        if (Liberada) return;
        Liberada = true;
        StartCoroutine(Caer());
    }

    IEnumerator Caer()
    {
        // La nota puede traer el pivote lejos (en el FBX quedo en la base del poste).
        // Se crea un pivote nuevo en el centro real de la nota y se anima ese.
        // la jaula vacia ya no es punto de interes
        if (transform.parent != null)
            foreach (var pi in transform.parent.GetComponentsInChildren<PuntoDeInteres>(true)) pi.enabled = false;

        var rend = GetComponent<Renderer>();
        pivote = new GameObject("Nota_Pivote").transform;
        pivote.position = rend != null ? rend.bounds.center : transform.position;
        pivote.rotation = Quaternion.identity;
        transform.SetParent(pivote, true); // ya no se mece con la jaula
        Vector3 desde = pivote.position;
        float suelo = Terrain.activeTerrain != null ? Terrain.activeTerrain.SampleHeight(desde) + Terrain.activeTerrain.transform.position.y : desde.y - 10f;
        posFinal = new Vector3(desde.x, suelo + alturaFinal, desde.z);
        Quaternion rot0 = pivote.rotation;

        // la camara sigue la nota mientras cae (si el jugador esta cerca)
        bool seguir = jugador != null && Vector3.Distance(jugador.transform.position, desde) < 80f;
        if (seguir)
        {
            UIJuego.ocupadoPor = this;
            jugador.bloqueado = true;
            jugador.mirarA = pivote;
        }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duracionCaida;
            float e = 1f - (1f - t) * (1f - t); // frena al final
            Vector3 p = Vector3.Lerp(desde, posFinal, e);
            float vaiven = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 1.2f;   // se mece como hoja
            p += new Vector3(vaiven, 0f, vaiven * 0.5f);
            pivote.position = p;
            pivote.rotation = rot0 * Quaternion.Euler(Mathf.Sin(t * 12f) * 25f * (1f - t), t * 200f, Mathf.Cos(t * 9f) * 20f * (1f - t));
            yield return null;
        }
        enSuelo = true;

        // devolver el control y hacer que la nota llame la atencion de cerca
        if (seguir)
        {
            yield return new WaitForSeconds(0.4f);
            jugador.mirarA = null;
            jugador.bloqueado = false;
            if (UIJuego.ocupadoPor == this) UIJuego.ocupadoPor = null;
        }
        var piNota = pivote.gameObject.AddComponent<PuntoDeInteres>();
        piNota.radio = 25f;
    }

    void Update()
    {
        if (!enSuelo || Recogida || jugador == null) return;

        // flota suavemente esperando a que la recojan
        if (!leyendo)
        {
            pivote.position = posFinal + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.25f;
            pivote.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            Vector3 d = pivote.position - jugador.transform.position; d.y = 0;
            mostrarAviso = UIJuego.ocupadoPor == null && d.magnitude < distanciaRecoger;
            if (mostrarAviso && EntradaJugador.Interactuar)
            {
                leyendo = true; mostrarAviso = false;
                UIJuego.ocupadoPor = this;
                jugador.bloqueado = true;
            }
        }
        else if (EntradaJugador.Interactuar || EntradaJugador.Cancelar)
        {
            leyendo = false;
            Recogida = true;
            if (ProgresoNotas.Instancia != null) ProgresoNotas.Instancia.NotaRecogida();
            UIJuego.ocupadoPor = null;
            jugador.bloqueado = false;
            pivote.gameObject.SetActive(false); // la nota ya esta "en el inventario"
        }
    }

    void OnGUI()
    {
        if (mostrarAviso) UIJuego.Aviso("Presiona  E  para recoger la nota");
        if (leyendo)
        {
            UIJuego.Panel(titulo, texto);
            UIJuego.Ayuda("E o X: guardar la nota");
        }
    }
}
