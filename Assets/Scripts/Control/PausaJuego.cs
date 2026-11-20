using UnityEngine;

// Pausa con P (o boton D del control), como ya estaba documentado en EntradaJugador.
// En pausa: P / boton D = continuar, X / Retroceso / boton C = salir del juego.
// Sin esto no habia forma de cerrar el build en pantalla completa (solo Alt+F4).
public class PausaJuego : MonoBehaviour
{
    public static bool EnPausa { get; private set; }

    JugadorDoom jugador;
    bool bloqueoPrevio;
    int framePausa;

    void Start() { jugador = FindFirstObjectByType<JugadorDoom>(); }

    void Update()
    {
        if (!EnPausa)
        {
            // no pausar mientras otro sistema tiene el control (tablero, nota, panel)
            if (EntradaJugador.Pausa && UIJuego.ocupadoPor == null) Pausar();
            return;
        }
        if (Time.frameCount == framePausa) return;
        if (EntradaJugador.Pausa) Continuar();
        else if (EntradaJugador.Cancelar) Salir();
    }

    void Pausar()
    {
        EnPausa = true;
        framePausa = Time.frameCount;
        UIJuego.ocupadoPor = this;
        if (jugador != null) { bloqueoPrevio = jugador.bloqueado; jugador.bloqueado = true; }
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    void Continuar()
    {
        EnPausa = false;
        if (UIJuego.ocupadoPor == this) UIJuego.ocupadoPor = null;
        if (jugador != null) jugador.bloqueado = bloqueoPrevio;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Salir()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable()
    {
        if (EnPausa) Continuar();
    }

    void OnGUI()
    {
        if (!EnPausa) return;
        UIJuego.Panel("Pausa", "\nP (o boton D): continuar\n\nX (o boton C): salir del juego");
    }
}
