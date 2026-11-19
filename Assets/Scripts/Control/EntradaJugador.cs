using UnityEngine;
using UnityEngine.InputSystem;

// Une el control Arduino y el teclado en un solo lugar.
// El resto del juego solo usa esta clase.
//
//  Accion          Arduino      Teclado
//  Avanzar/retro   Joystick Y   W / S  o  flechas arriba/abajo
//  Girar           Joystick X   A / D  o  flechas izq/der
//  Interactuar     Boton A      E  o  Espacio
//  Notas           Boton B      Q  o  Tab
//  Cancelar        Boton C      X  o  Retroceso
//  Pausa           Boton D      P
public static class EntradaJugador
{
    static ControlArduino A => ControlArduino.Instancia;

    static bool Tecla(Key k) => Keyboard.current != null && Keyboard.current[k].isPressed;
    static bool TeclaBajada(Key k) => Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;

    public static float Avanzar
    {
        get
        {
            float t = (Tecla(Key.W) || Tecla(Key.UpArrow) ? 1f : 0f) - (Tecla(Key.S) || Tecla(Key.DownArrow) ? 1f : 0f);
            float a = A != null ? A.EjeY : 0f;
            return Mathf.Clamp(t + a, -1f, 1f);
        }
    }

    public static float Girar
    {
        get
        {
            float t = (Tecla(Key.D) || Tecla(Key.RightArrow) ? 1f : 0f) - (Tecla(Key.A) || Tecla(Key.LeftArrow) ? 1f : 0f);
            float a = A != null ? A.EjeX : 0f;
            return Mathf.Clamp(t + a, -1f, 1f);
        }
    }

    public static bool Interactuar => (A != null && A.BotonBajado(0)) || TeclaBajada(Key.E) || TeclaBajada(Key.Space);
    public static bool Notas => (A != null && A.BotonBajado(1)) || TeclaBajada(Key.Q) || TeclaBajada(Key.Tab);
    public static bool Cancelar => (A != null && A.BotonBajado(2)) || TeclaBajada(Key.X) || TeclaBajada(Key.Backspace);
    public static bool Pausa => (A != null && A.BotonBajado(3)) || TeclaBajada(Key.P);

    // Para los acertijos: direccion "recien empujada" del joystick/teclas (mover la linea de punto en punto)
    static Vector2Int ultimaDir;
    static int ultimoFrame = -1;
    static Vector2Int dirFrame;
    public static Vector2Int DireccionBajada
    {
        get
        {
            if (ultimoFrame == Time.frameCount) return dirFrame;
            ultimoFrame = Time.frameCount;
            Vector2Int d = Vector2Int.zero;
            float x = Girar, y = Avanzar;
            if (Mathf.Abs(x) > 0.6f && Mathf.Abs(x) >= Mathf.Abs(y)) d = new Vector2Int((int)Mathf.Sign(x), 0);
            else if (Mathf.Abs(y) > 0.6f) d = new Vector2Int(0, (int)Mathf.Sign(y));
            dirFrame = d != ultimaDir ? d : Vector2Int.zero;
            ultimaDir = d;
            return dirFrame;
        }
    }
}
