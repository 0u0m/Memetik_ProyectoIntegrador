using UnityEngine;

// Movimiento estilo DOOM clasico:
//  - Joystick adelante/atras = caminar
//  - Joystick izquierda/derecha = girar
//  - La camara no mira arriba/abajo con el control, pero se inclina sola
//    hacia los PuntoDeInteres cercanos (las jaulas).
// Escala del pueblo: 1 metro real = 3.4 unidades.
[RequireComponent(typeof(CharacterController))]
public class JugadorDoom : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 14f;          // ~4 m/s
    public float velocidadGiro = 110f;     // grados por segundo
    public float aceleracion = 10f;
    public float gravedad = -33.4f;       // 9.81 m/s2 x 3.4 u/m

    [Header("Camara")]
    public Transform camara;
    public float alturaOjos = 5.4f;        // 1.6 m
    [Tooltip("Que tan rapido se inclina hacia los puntos de interes")]
    public float suavidadMirada = 2.5f;
    public float inclinacionMaxima = 35f;

    [Header("Balanceo al caminar")]
    public float balanceo = 0.12f;
    public float frecuenciaBalanceo = 7f;

    [HideInInspector] public bool bloqueado; // true mientras usa un tablero, lee una nota, etc.
    // Si tiene valor, la camara se acomoda en este punto (por ejemplo frente al tablero)
    [HideInInspector] public Transform vistaForzada;
    // Si tiene valor, la camara se queda en los ojos pero voltea a ver este objeto (por ejemplo la nota cayendo)
    [HideInInspector] public Transform mirarA;

    CharacterController cc;
    float velActual, velY, pitch, fase;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
    }

    void Update()
    {
        float avanzar = bloqueado ? 0f : EntradaJugador.Avanzar;
        float girar = bloqueado ? 0f : EntradaJugador.Girar;

        // girar
        transform.Rotate(0f, girar * velocidadGiro * Time.deltaTime, 0f);

        // caminar (con aceleracion suave)
        velActual = Mathf.MoveTowards(velActual, avanzar * velocidad, velocidad * aceleracion * Time.deltaTime);
        if (cc.isGrounded && velY < 0f) velY = -2f;
        velY += gravedad * Time.deltaTime;
        Vector3 mov = transform.forward * velActual + Vector3.up * velY;
        cc.Move(mov * Time.deltaTime);

        ActualizarCamara();
    }

void ActualizarCamara()
    {
        if (camara == null) return;
        float k = 1f - Mathf.Exp(-6f * Time.deltaTime);

        // vista forzada (tablero, nota): la camara viaja suave hasta ese punto
        if (vistaForzada != null)
        {
            camara.position = Vector3.Lerp(camara.position, vistaForzada.position, k);
            camara.rotation = Quaternion.Slerp(camara.rotation, vistaForzada.rotation, k);
            return;
        }

        // seguir con la mirada un objeto (sin mover al jugador)
        if (mirarA != null)
        {
            camara.localPosition = Vector3.Lerp(camara.localPosition, new Vector3(0f, alturaOjos, 0f), k * 2f);
            Vector3 dirM = mirarA.position - camara.position;
            if (dirM.sqrMagnitude > 0.01f)
                camara.rotation = Quaternion.Slerp(camara.rotation, Quaternion.LookRotation(dirM, Vector3.up), k * 1.5f);
            pitch = Mathf.DeltaAngle(0f, camara.localEulerAngles.x); // para regresar suave despues
            return;
        }

        // inclinacion automatica hacia el punto de interes mas cercano que este enfrente
        float objetivo = 0f, mejor = float.MaxValue;
        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        foreach (var p in PuntoDeInteres.Todos)
        {
            Vector3 d = p.Posicion - ojos;
            Vector3 plano = new Vector3(d.x, 0f, d.z);
            float dist = plano.magnitude;
            if (dist > p.radio || dist < 0.5f) continue;
            if (Vector3.Angle(transform.forward, plano) > 50f) continue;
            if (dist < mejor)
            {
                mejor = dist;
                float ang = -Mathf.Atan2(d.y, dist) * Mathf.Rad2Deg;
                float peso = 1f - Mathf.InverseLerp(p.radio * 0.6f, p.radio, dist);
                objetivo = Mathf.Clamp(ang, -inclinacionMaxima, inclinacionMaxima) * peso;
            }
        }
        pitch = Mathf.Lerp(pitch, objetivo, 1f - Mathf.Exp(-suavidadMirada * Time.deltaTime));

        // balanceo al caminar
        float rapidez = Mathf.Abs(velActual) / velocidad;
        fase += Time.deltaTime * frecuenciaBalanceo * rapidez;
        float bob = Mathf.Sin(fase * 2f) * balanceo * rapidez;

        // regresa suave a los ojos (por si venia de una vista forzada)
        camara.localPosition = Vector3.Lerp(camara.localPosition, new Vector3(0f, alturaOjos + bob, 0f), k * 2f);
        camara.localRotation = Quaternion.Slerp(camara.localRotation, Quaternion.Euler(pitch, 0f, 0f), k * 2f);
    }
}
