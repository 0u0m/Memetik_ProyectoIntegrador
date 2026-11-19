using UnityEngine;

// Mece suavemente el objeto (como si lo moviera el viento).
// Va en el punto de donde cuelga la jaula ("Colgante").
public class Balanceo : MonoBehaviour
{
    [Tooltip("Que tanto se inclina, en grados")]
    public float amplitud = 4f;
    [Tooltip("Que tan rapido se mece")]
    public float velocidad = 0.8f;

    Quaternion inicial;
    float desfase;

    void Start()
    {
        inicial = transform.localRotation;
        desfase = Random.value * 10f; // para que no todas se muevan igual
    }

    void Update()
    {
        float t = Time.time * velocidad + desfase;
        float x = Mathf.Sin(t) * amplitud + Mathf.Sin(t * 2.3f + 1f) * amplitud * 0.3f;
        float z = Mathf.Sin(t * 0.7f + 2f) * amplitud * 0.6f;
        transform.localRotation = inicial * Quaternion.Euler(x, 0f, z);
    }
}
