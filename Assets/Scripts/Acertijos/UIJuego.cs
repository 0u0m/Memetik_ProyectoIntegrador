using UnityEngine;

// Textos simples en pantalla (avisos y lectura de notas).
// Es provisional: despues se puede cambiar por una UI con el estilo del juego.
public static class UIJuego
{
    static GUIStyle aviso, ayuda, panelTitulo, panelTexto;
    static Texture2D fondo;

    // Solo un objeto a la vez puede "tomar" el control (tablero, nota...)
    public static Object ocupadoPor;

    static void Estilos()
    {
        if (aviso != null) return;
        fondo = new Texture2D(1, 1); fondo.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.12f, 0.82f)); fondo.Apply();
        aviso = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
        aviso.normal.textColor = new Color(1f, 0.92f, 0.7f);
        ayuda = new GUIStyle(aviso) { fontSize = 16, fontStyle = FontStyle.Normal };
        ayuda.normal.textColor = new Color(0.85f, 0.9f, 1f);
        panelTitulo = new GUIStyle(aviso) { fontSize = 26, alignment = TextAnchor.UpperCenter };
        panelTexto = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, alignment = TextAnchor.UpperLeft };
        panelTexto.normal.textColor = new Color(0.95f, 0.92f, 0.85f);
    }

    public static void Aviso(string texto)
    {
        Estilos();
        var r = new Rect(Screen.width * 0.5f - 300, Screen.height - 120, 600, 44);
        GUI.DrawTexture(r, fondo);
        GUI.Label(r, texto, aviso);
    }

    public static void Ayuda(string texto)
    {
        Estilos();
        var r = new Rect(Screen.width * 0.5f - 360, Screen.height - 56, 720, 34);
        GUI.DrawTexture(r, fondo);
        GUI.Label(r, texto, ayuda);
    }

    public static void Panel(string titulo, string texto)
    {
        Estilos();
        float w = Mathf.Min(720, Screen.width - 80), h = Mathf.Min(460, Screen.height - 160);
        var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f - 30, w, h);
        GUI.DrawTexture(r, fondo);
        GUI.Label(new Rect(r.x, r.y + 24, r.width, 40), titulo, panelTitulo);
        GUI.Label(new Rect(r.x + 40, r.y + 84, r.width - 80, r.height - 110), texto, panelTexto);
    }
}
