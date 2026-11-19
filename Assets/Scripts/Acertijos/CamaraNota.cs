using System.Collections;
using UnityEngine;

// Cuando recoges una nota, la camara viaja a la casa final, muestra como se
// enciende el circulo y regresa al jugador. Lo dispara ProgresoNotas.
public class CamaraNota : MonoBehaviour
{
    public Transform puntoMira;     // hacia donde mira la camara al llegar (la casa final / arco)
    public float duracionViaje = 2.6f;      // mas lento
    public float duracionMirando = 2.5f;    // se queda mas tiempo viendo
    public float alturaVuelo = 6f;
    [Tooltip("A que distancia se queda la camara del punto que mira")]
    public float distancia = 34f;

    JugadorDoom jugador;
    Transform cam;
    bool ocupada;

    /// <summary>True mientras la camara esta en el viaje a los circulos (el hada se queda quieta).</summary>
    public static bool EnAnimacion { get; private set; }

    void Start()
    {
        jugador = FindFirstObjectByType<JugadorDoom>();
        if (jugador != null) cam = jugador.camara;
    }

    public void Mostrar()
    {
        if (!ocupada && isActiveAndEnabled) StartCoroutine(Viaje());
    }

    IEnumerator Viaje()
    {
        if (cam == null || puntoMira == null) yield break;
        ocupada = true;
        EnAnimacion = true;
        jugador.bloqueado = true;

        Vector3 desdePos = cam.position;
        Quaternion desdeRot = cam.rotation;

        // punto de observacion: un poco arriba y frente a la casa final
        Vector3 dir = (cam.position - puntoMira.position); dir.y = 0; dir.Normalize();
        Vector3 miraPos = puntoMira.position + dir * distancia + Vector3.up * alturaVuelo;
        Quaternion miraRot = Quaternion.LookRotation((puntoMira.position - miraPos).normalized, Vector3.up);

        yield return Mover(desdePos, desdeRot, miraPos, miraRot, duracionViaje);
        yield return new WaitForSeconds(duracionMirando);
        // regresar exactamente a los ojos del jugador (un solo movimiento)
        Vector3 ojos = jugador.transform.position + Vector3.up * jugador.alturaOjos;
        Quaternion rotOjos = jugador.transform.rotation;
        yield return Mover(miraPos, miraRot, ojos, rotOjos, duracionViaje);
        cam.localPosition = new Vector3(0f, jugador.alturaOjos, 0f);
        cam.localRotation = Quaternion.identity;

        jugador.bloqueado = false;
        ocupada = false;
        EnAnimacion = false;
    }

    IEnumerator Mover(Vector3 p0, Quaternion r0, Vector3 p1, Quaternion r1, float dur)
    {
        float t = 0f;
        jugador.vistaForzada = null; jugador.mirarA = null;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float e = Mathf.SmoothStep(0, 1, t);
            cam.position = Vector3.Lerp(p0, p1, e);
            cam.rotation = Quaternion.Slerp(r0, r1, e);
            yield return null;
        }
    }
}
