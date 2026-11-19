using UnityEngine;

// Mantiene este objeto siempre centrado en la camara.
// Lo usa el volumen de neblina: un cubo chico alrededor de la camara basta,
// porque la neblina se calcula con posiciones del mundo.
[ExecuteAlways]
public class SeguirCamara : MonoBehaviour
{
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam != null) transform.position = cam.transform.position;
    }
}
