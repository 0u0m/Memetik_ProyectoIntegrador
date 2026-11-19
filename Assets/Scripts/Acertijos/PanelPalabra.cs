using UnityEngine;
using UnityEngine.InputSystem;

// Panel junto al porton: escribes la palabra secreta y, si es correcta, se abre la reja.
//  Teclado:  escribir letras, Retroceso = borrar, Enter = comprobar, Esc = salir
//  Control:  joystick = moverte por las letras, A = elegir, C = salir
public class PanelPalabra : MonoBehaviour
{
    [Tooltip("La palabra secreta (sin acentos)")]
    public string palabraSecreta = "KELTIK";
    public PortonFinal porton;
    [Tooltip("Pedir las 5 notas antes de poder usar el panel")]
    public bool requiereTodasLasNotas = true;
    public float distanciaUso = 18f;

    const string LETRAS = "ABCDEFGHIJKLMNÑOPQRSTUVWXYZ";
    const int COLUMNAS = 7;
    // celdas: 27 letras + BORRAR + LISTO
    int TotalCeldas => LETRAS.Length + 2;

    JugadorDoom jugador;
    bool activo, mostrarAviso;
    string escrito = "";
    int seleccion;
    string mensaje = ""; float tiempoMensaje;
    int frameEntrada;
    Vector2Int ultimaDir;

    void Start() { jugador = FindFirstObjectByType<JugadorDoom>(); }

    void Update()
    {
        if (jugador == null || (porton != null && porton.Abierto)) { mostrarAviso = false; return; }

        if (!activo)
        {
            Vector3 d = transform.position - jugador.transform.position; d.y = 0;
            mostrarAviso = UIJuego.ocupadoPor == null && d.magnitude < distanciaUso;
            if (mostrarAviso && EntradaJugador.Interactuar)
            {
                var prog = ProgresoNotas.Instancia;
                if (requiereTodasLasNotas && prog != null && prog.Recogidas < prog.totalNotas)
                {
                    Mensaje("Aún te faltan " + (prog.totalNotas - prog.Recogidas) + " notas");
                    return;
                }
                Entrar();
            }
            return;
        }

        if (Time.frameCount == frameEntrada) return; // que la E de entrar no se escriba

        var kb = Keyboard.current;
        var ard = ControlArduino.Instancia;

        // salir
        if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (ard != null && ard.BotonBajado(2))) { Salir(); return; }

        // teclado: escribir directo
        if (kb != null)
        {
            for (Key k = Key.A; k <= Key.Z; k++)
                if (kb[k].wasPressedThisFrame) Escribir(k.ToString());
            if (kb.semicolonKey.wasPressedThisFrame) Escribir("Ñ"); // tecla Ñ en teclado latino
            if (kb.backspaceKey.wasPressedThisFrame) Borrar();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Comprobar(); return; }
        }

        // flechas / joystick: moverse por el tablero de letras
        Vector2Int dir = Vector2Int.zero;
        if (kb != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame) dir.x = -1;
            if (kb.rightArrowKey.wasPressedThisFrame) dir.x = 1;
            if (kb.upArrowKey.wasPressedThisFrame) dir.y = 1;
            if (kb.downArrowKey.wasPressedThisFrame) dir.y = -1;
        }
        if (ard != null && ard.Conectado)
        {
            Vector2Int d = Vector2Int.zero;
            if (Mathf.Abs(ard.EjeX) > 0.6f) d.x = (int)Mathf.Sign(ard.EjeX);
            else if (Mathf.Abs(ard.EjeY) > 0.6f) d.y = (int)Mathf.Sign(ard.EjeY);
            if (d != ultimaDir && d != Vector2Int.zero) dir = d;
            ultimaDir = d;
        }
        if (dir != Vector2Int.zero) Mover(dir);

        // elegir con Espacio o boton A del control
        bool elegir = (kb != null && kb.spaceKey.wasPressedThisFrame) || (ard != null && ard.BotonBajado(0));
        if (elegir)
        {
            if (seleccion < LETRAS.Length) Escribir(LETRAS[seleccion].ToString());
            else if (seleccion == LETRAS.Length) Borrar();
            else Comprobar();
        }
    }

    void Mover(Vector2Int dir)
    {
        int fila = seleccion / COLUMNAS, col = seleccion % COLUMNAS;
        col += dir.x; fila -= dir.y; // arriba = fila anterior
        int filas = Mathf.CeilToInt(TotalCeldas / (float)COLUMNAS);
        col = (col + COLUMNAS) % COLUMNAS;
        fila = (fila + filas) % filas;
        seleccion = Mathf.Clamp(fila * COLUMNAS + col, 0, TotalCeldas - 1);
    }

    void Escribir(string letra)
    {
        if (escrito.Length < Mathf.Max(palabraSecreta.Length, 12)) escrito += letra;
    }

    void Borrar() { if (escrito.Length > 0) escrito = escrito.Substring(0, escrito.Length - 1); }

    void Comprobar()
    {
        if (escrito.Trim().ToUpperInvariant() == palabraSecreta.Trim().ToUpperInvariant())
        {
            Salir();
            Mensaje("¡La reja se abre!");
            if (porton != null) porton.AbrirPorton();
        }
        else
        {
            Mensaje("No es la palabra correcta");
            escrito = "";
        }
    }

    void Entrar()
    {
        activo = true; mostrarAviso = false;
        frameEntrada = Time.frameCount;
        escrito = ""; seleccion = 0;
        UIJuego.ocupadoPor = this;
        jugador.bloqueado = true;
    }

    void Salir()
    {
        activo = false;
        if (UIJuego.ocupadoPor == this) UIJuego.ocupadoPor = null;
        jugador.bloqueado = false;
    }

    void Mensaje(string m) { mensaje = m; tiempoMensaje = Time.time; }

    // ===================== interfaz =====================
    GUIStyle sLetra, sLetraSel, sEscrito, sTitulo, sAyuda;
    Texture2D fondo, fondoSel;

    void Estilos()
    {
        if (sLetra != null) return;
        fondo = new Texture2D(1, 1); fondo.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.12f, 0.88f)); fondo.Apply();
        fondoSel = new Texture2D(1, 1); fondoSel.SetPixel(0, 0, new Color(1f, 0.85f, 0.4f, 0.9f)); fondoSel.Apply();
        sLetra = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 24, fontStyle = FontStyle.Bold };
        sLetra.normal.textColor = new Color(0.9f, 0.92f, 1f);
        sLetraSel = new GUIStyle(sLetra); sLetraSel.normal.textColor = new Color(0.1f, 0.08f, 0.05f);
        sEscrito = new GUIStyle(sLetra) { fontSize = 40 }; sEscrito.normal.textColor = new Color(1f, 0.95f, 0.8f);
        sTitulo = new GUIStyle(sLetra) { fontSize = 24 }; sTitulo.normal.textColor = new Color(1f, 0.9f, 0.7f);
        sAyuda = new GUIStyle(sLetra) { fontSize = 14, fontStyle = FontStyle.Normal };
    }

    void OnGUI()
    {
        if (mostrarAviso) UIJuego.Aviso("Presiona  E  para usar el panel");
        if (Time.time - tiempoMensaje < 2.5f && !string.IsNullOrEmpty(mensaje))
            UIJuego.Aviso(mensaje);
        if (!activo) return;

        Estilos();
        float w = 560, h = 430;
        var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUI.DrawTexture(r, fondo);
        GUI.Label(new Rect(r.x, r.y + 16, w, 34), "Escribe la palabra secreta", sTitulo);

        // casillas de lo escrito
        string mostrado = "";
        int largo = Mathf.Max(palabraSecreta.Length, escrito.Length);
        for (int i = 0; i < largo; i++) mostrado += (i < escrito.Length ? escrito[i].ToString() : "_") + " ";
        GUI.Label(new Rect(r.x, r.y + 60, w, 56), mostrado.Trim(), sEscrito);

        // tablero de letras
        float cs = 62, ox = r.x + (w - COLUMNAS * cs) * 0.5f, oy = r.y + 130;
        for (int i = 0; i < TotalCeldas; i++)
        {
            int f = i / COLUMNAS, c = i % COLUMNAS;
            var celda = new Rect(ox + c * cs + 3, oy + f * cs * 0.8f + 3, cs - 6, cs * 0.8f - 6);
            string t = i < LETRAS.Length ? LETRAS[i].ToString() : (i == LETRAS.Length ? "←" : "✓");
            bool sel = i == seleccion;
            if (sel) GUI.DrawTexture(celda, fondoSel);
            GUI.Label(celda, t, sel ? sLetraSel : sLetra);
        }
        GUI.Label(new Rect(r.x, r.y + h - 34, w, 24),
            "Escribe con el teclado · Retroceso borra · Enter comprueba · Esc sale   |   Control: joystick + A", sAyuda);
    }
}
