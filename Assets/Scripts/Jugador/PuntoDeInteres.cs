using System.Collections.Generic;
using UnityEngine;

// Marca algo que la camara debe "voltear a ver" cuando el jugador esta cerca
// (por ejemplo las jaulas, que estan altas). Ver JugadorDoom.
public class PuntoDeInteres : MonoBehaviour
{
    public static readonly List<PuntoDeInteres> Todos = new List<PuntoDeInteres>();

    [Tooltip("Distancia a la que la camara empieza a inclinarse hacia aqui")]
    public float radio = 45f;

    Renderer rend;
    /// <summary>Centro real del modelo (no su pivote, que puede estar en otro lado).</summary>
    public Vector3 Posicion
    {
        get
        {
            if (rend == null) rend = GetComponent<Renderer>();
            return rend != null ? rend.bounds.center : transform.position;
        }
    }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(Posicion, radio);
    }
}
