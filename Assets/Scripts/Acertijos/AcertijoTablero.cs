using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// Acertijo estilo The Witness en la cara del tablero.
// La cuadricula se dibuja en una textura; el jugador mueve la linea de punto en punto
// (joystick o W/A/S/D) desde el INICIO hasta la SALIDA pasando por todos los PUNTOS.
public class AcertijoTablero : MonoBehaviour
{
    [Header("Cuadricula (en celdas)")]
    public int columnas = 4;
    public int filas = 4;
    [Tooltip("Punto de inicio (columna, fila). (0,0) = abajo a la izquierda")]
    public Vector2Int inicio = new Vector2Int(0, 0);
    public Vector2Int salida = new Vector2Int(4, 4);
    [Tooltip("Hacia donde sale el 'piquito' de la salida")]
    public Vector2Int direccionSalida = new Vector2Int(0, 1);
    [Tooltip("Puntos por los que DEBE pasar la linea")]
    public List<Vector2Int> puntos = new List<Vector2Int> { new Vector2Int(1, 1), new Vector2Int(3, 1), new Vector2Int(2, 2), new Vector2Int(1, 3), new Vector2Int(3, 3) };

    [System.Serializable] public class Tramo { public Vector2Int a, b; }
    [Header("Regla: caminos cortados")]
    [Tooltip("Tramos de la cuadricula rotos (entre dos puntos vecinos): la linea no los puede cruzar")]
    public List<Tramo> tramosRotos = new List<Tramo>();

    [Header("Regla: colores")]
    [Tooltip("Celdas (columna, fila) con cuadro oscuro. (0,0) = celda de abajo a la izquierda")]
    public List<Vector2Int> cuadrosOscuros = new List<Vector2Int>();
    [Tooltip("Celdas con cuadro claro. La linea debe separar los oscuros de los claros")]
    public List<Vector2Int> cuadrosClaros = new List<Vector2Int>();
    public Color colorOscuro = new Color(0.05f, 0.06f, 0.08f);
    public Color colorClaro = new Color(0.97f, 0.97f, 0.95f);

    public enum Regla { Puntos, Cortados, Colores }
    [Header("Al azar")]
    [Tooltip("Inventa un acertijo nuevo cada vez que empieza el juego")]
    public bool generarAlAzar = true;
    public Regla regla = Regla.Puntos;
    [Tooltip("Que tan largo es el camino secreto (0.4 = corto, 0.7 = enredado)")]
    [Range(0.3f, 0.8f)] public float largoCamino = 0.55f;
    [Tooltip("Escribe la solucion en la consola (para probar)")]
    public bool mostrarSolucionEnConsola = true;

    [Header("Colores")]
    public Color fondo = new Color(0.07f, 0.20f, 0.30f);
    public Color caminos = new Color(0.22f, 0.50f, 0.66f);
    public Color linea = new Color(1f, 0.97f, 0.85f);
    public Color lineaBien = new Color(1f, 0.85f, 0.35f);
    public Color lineaMal = new Color(1f, 0.3f, 0.25f);
    public Color colorPuntos = new Color(0.04f, 0.10f, 0.16f);

    [Header("Interaccion")]
    public float distanciaUso = 20f;
    public float distanciaCamara = 6.5f;

    [Header("Orientacion de la textura en la cara (si sale volteada)")]
    public bool espejoHorizontal = false;
    public bool espejoVertical = false;
    public bool rotar90 = false;

    [Header("Al resolverlo")]
    public UnityEvent alResolver = new UnityEvent();

    // estado
    public bool Resuelto { get; private set; }
    readonly List<Vector2Int> camino = new List<Vector2Int>();
    bool activo, bloqueadoAnim;
    Color colorLinea;

    Renderer cara;
    Texture2D tex;
    Color32[] px;
    const int RES = 512;
    Transform vista;
    JugadorDoom jugador;
    bool mostrarAviso;

    void Start()
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r.name == "Cara") cara = r;
        tex = new Texture2D(RES, RES, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        px = new Color32[RES * RES];
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        if (cara != null)
        {
            // ajustar la textura a la zona UV que realmente usa la cara (por si no va de 0 a 1)
            var m = cara.GetComponent<MeshFilter>().sharedMesh;
            if (m.isReadable && m.uv.Length > 0)
            {
                Vector2 mn = m.uv[0], mx = m.uv[0];
                foreach (var u in m.uv) { mn = Vector2.Min(mn, u); mx = Vector2.Max(mx, u); }
                Vector2 tam = mx - mn;
                if (tam.x > 0.0001f && tam.y > 0.0001f)
                {
                    var esc = new Vector2(1f / tam.x, 1f / tam.y);
                    mat.SetTextureScale("_BaseMap", esc);
                    mat.SetTextureOffset("_BaseMap", -Vector2.Scale(mn, esc));
                }
            }
            cara.material = mat;
        }

        // punto de vista frente a la cara
        vista = new GameObject("VistaTablero").transform;
        vista.SetParent(transform, false);
        if (cara != null)
        {
            var mf = cara.GetComponent<MeshFilter>();
            Vector3 n = cara.transform.TransformDirection(mf.sharedMesh.normals[0]).normalized;
            Vector3 c = cara.bounds.center;
            vista.position = c + n * distanciaCamara;
            vista.rotation = Quaternion.LookRotation(-n, Vector3.up);
        }
        jugador = FindFirstObjectByType<JugadorDoom>();
        if (generarAlAzar) Generar();
        colorLinea = linea;
        Dibujar();
    }

    void Update()
    {
        if (jugador == null) return;
        if (!activo)
        {
            Vector3 d = transform.position - jugador.transform.position; d.y = 0;
            mostrarAviso = !Resuelto && UIJuego.ocupadoPor == null && d.magnitude < distanciaUso && Vector3.Angle(jugador.transform.forward, d) < 70f;
            if (mostrarAviso && EntradaJugador.Interactuar) Entrar();
            return;
        }
        if (bloqueadoAnim) return;

        if (EntradaJugador.Cancelar) { Salir(); return; }
        var dir = EntradaJugador.DireccionBajada;
        if (dir != Vector2Int.zero)
        {
            var ultimo = camino[camino.Count - 1];
            if (ultimo == salida && dir == direccionSalida) { Validar(); return; }
            Mover(dir);
        }
        else if (EntradaJugador.Interactuar && camino[camino.Count - 1] == salida) Validar();
    }

    void Entrar()
    {
        activo = true; mostrarAviso = false;
        UIJuego.ocupadoPor = this;
        jugador.bloqueado = true;
        jugador.vistaForzada = vista;
        camino.Clear(); camino.Add(inicio);
        colorLinea = linea;
        Dibujar();
    }

    void Salir()
    {
        activo = false;
        if (UIJuego.ocupadoPor == this) UIJuego.ocupadoPor = null;
        jugador.bloqueado = false;
        jugador.vistaForzada = null;
        if (!Resuelto) { camino.Clear(); Dibujar(); }
    }

    void Mover(Vector2Int dir)
    {
        var ultimo = camino[camino.Count - 1];
        var sig = ultimo + dir;
        if (sig.x < 0 || sig.y < 0 || sig.x > columnas || sig.y > filas) return;
        if (Roto(ultimo, sig)) return; // tramo cortado
        if (camino.Count >= 2 && sig == camino[camino.Count - 2]) camino.RemoveAt(camino.Count - 1); // regresar
        else if (!camino.Contains(sig)) camino.Add(sig);
        Dibujar();
    }

    void Validar()
    {
        bool ok = camino[camino.Count - 1] == salida;
        foreach (var p in puntos) if (!camino.Contains(p)) ok = false;
        if (ok && !ColoresOk()) ok = false;
        StartCoroutine(ok ? Exito() : Fallo());
    }

    IEnumerator Exito()
    {
        bloqueadoAnim = true;
        Resuelto = true;
        for (int i = 0; i < 6; i++) { colorLinea = i % 2 == 0 ? lineaBien : linea; Dibujar(); yield return new WaitForSeconds(0.12f); }
        colorLinea = lineaBien; Dibujar();
        yield return new WaitForSeconds(0.5f);
        bloqueadoAnim = false;
        Salir();
        alResolver?.Invoke();
    }

    IEnumerator Fallo()
    {
        bloqueadoAnim = true;
        colorLinea = lineaMal; Dibujar();
        yield return new WaitForSeconds(0.7f);
        camino.Clear(); camino.Add(inicio);
        colorLinea = linea; Dibujar();
        bloqueadoAnim = false;
    }

    void OnGUI()
    {
        if (mostrarAviso) UIJuego.Aviso("Presiona  E  para usar el tablero");
        if (activo && !bloqueadoAnim)
            UIJuego.Ayuda("W A S D: mover la linea    E o empujar hacia la salida: comprobar    X: salir");
    }

    // ===================== dibujo =====================
    float celda, margen, grosor;

    Vector2 Px(Vector2Int n)
    {
        float ancho = Mathf.Max(columnas, filas) * celda;
        float ox = (RES - columnas * celda) * 0.5f, oy = (RES - filas * celda) * 0.5f;
        return new Vector2(ox + n.x * celda, oy + n.y * celda);
    }

    void Dibujar()
    {
        if (tex == null) return;
        margen = RES * 0.16f;
        celda = (RES - 2 * margen) / Mathf.Max(columnas, filas);
        grosor = celda * 0.22f;
        Color32 cf = fondo, cc = caminos, cl = colorLinea, cp = colorPuntos;
        for (int i = 0; i < px.Length; i++) px[i] = cf;

        // caminos (ranuras)
        for (int x = 0; x <= columnas; x++)
            for (int y = 0; y <= filas; y++)
            {
                var n = new Vector2Int(x, y);
                if (x < columnas) DibujarTramo(n, n + Vector2Int.right, cc);
                if (y < filas) DibujarTramo(n, n + Vector2Int.up, cc);
            }
        Circulo(Px(inicio), grosor * 1.25f, cc);
        Vector2 ps = Px(salida), pf = ps + (Vector2)direccionSalida * celda * 0.38f;
        Segmento(ps, pf, grosor, cc);

        // linea del jugador
        if (camino.Count > 0)
        {
            Circulo(Px(inicio), grosor * 1.25f, cl);
            for (int i = 1; i < camino.Count; i++) Segmento(Px(camino[i - 1]), Px(camino[i]), grosor * 0.92f, cl);
            if (Resuelto && camino[camino.Count - 1] == salida) Segmento(ps, pf, grosor * 0.92f, cl);
        }

        // puntos obligatorios (hexagonos -> circulos chicos)
        foreach (var p in puntos) Circulo(Px(p), grosor * 0.36f, cp);

        // cuadros de colores (en el centro de cada celda)
        foreach (var c in cuadrosOscuros) Cuadro(CentroCelda(c), celda * 0.34f, colorOscuro);
        foreach (var c in cuadrosClaros) Cuadro(CentroCelda(c), celda * 0.34f, colorClaro);

        // aplicar orientacion
        var salidaPx = new Color32[px.Length];
        for (int y = 0; y < RES; y++)
            for (int x = 0; x < RES; x++)
            {
                int sx = x, sy = y;
                if (rotar90) { int t = sx; sx = sy; sy = RES - 1 - t; }
                if (espejoHorizontal) sx = RES - 1 - sx;
                if (espejoVertical) sy = RES - 1 - sy;
                salidaPx[y * RES + x] = px[sy * RES + sx];
            }
        tex.SetPixels32(salidaPx);
        tex.Apply();
    }

    void Circulo(Vector2 c, float r, Color32 col)
    {
        int x0 = Mathf.Max(0, (int)(c.x - r)), x1 = Mathf.Min(RES - 1, (int)(c.x + r));
        int y0 = Mathf.Max(0, (int)(c.y - r)), y1 = Mathf.Min(RES - 1, (int)(c.y + r));
        float r2 = r * r;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - c.x, dy = y + 0.5f - c.y;
                if (dx * dx + dy * dy <= r2) px[y * RES + x] = col;
            }
    }

    void Segmento(Vector2 a, Vector2 b, float g, Color32 col)
    {
        float h = g * 0.5f;
        int x0 = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - h)), x1 = Mathf.Min(RES - 1, (int)(Mathf.Max(a.x, b.x) + h));
        int y0 = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - h)), y1 = Mathf.Min(RES - 1, (int)(Mathf.Max(a.y, b.y) + h));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++) px[y * RES + x] = col;
        Circulo(a, h, col); Circulo(b, h, col);
    }


// ===================== reglas =====================
    bool MismoTramo(Vector2Int a, Vector2Int b, Tramo t) => (t.a == a && t.b == b) || (t.a == b && t.b == a);

    bool Roto(Vector2Int a, Vector2Int b)
    {
        foreach (var t in tramosRotos) if (MismoTramo(a, b, t)) return true;
        return false;
    }

    bool EnCamino(Vector2Int a, Vector2Int b)
    {
        for (int i = 1; i < camino.Count; i++)
            if ((camino[i - 1] == a && camino[i] == b) || (camino[i - 1] == b && camino[i] == a)) return true;
        return false;
    }

    // Cada zona que deja la linea no puede tener cuadros oscuros y claros juntos
    bool ColoresOk()
    {
        if (cuadrosOscuros.Count == 0 || cuadrosClaros.Count == 0) return true;
        var zona = new int[columnas, filas];
        for (int x = 0; x < columnas; x++) for (int y = 0; y < filas; y++) zona[x, y] = -1;
        int nZona = 0;
        var pila = new Stack<Vector2Int>();
        for (int x = 0; x < columnas; x++)
            for (int y = 0; y < filas; y++)
            {
                if (zona[x, y] >= 0) continue;
                zona[x, y] = nZona; pila.Push(new Vector2Int(x, y));
                while (pila.Count > 0)
                {
                    var c = pila.Pop();
                    // derecha: la separa el tramo vertical (c.x+1, c.y)-(c.x+1, c.y+1)
                    if (c.x + 1 < columnas && zona[c.x + 1, c.y] < 0 && !EnCamino(new Vector2Int(c.x + 1, c.y), new Vector2Int(c.x + 1, c.y + 1))) { zona[c.x + 1, c.y] = nZona; pila.Push(new Vector2Int(c.x + 1, c.y)); }
                    // izquierda: tramo vertical (c.x, c.y)-(c.x, c.y+1)
                    if (c.x - 1 >= 0 && zona[c.x - 1, c.y] < 0 && !EnCamino(new Vector2Int(c.x, c.y), new Vector2Int(c.x, c.y + 1))) { zona[c.x - 1, c.y] = nZona; pila.Push(new Vector2Int(c.x - 1, c.y)); }
                    // arriba: tramo horizontal (c.x, c.y+1)-(c.x+1, c.y+1)
                    if (c.y + 1 < filas && zona[c.x, c.y + 1] < 0 && !EnCamino(new Vector2Int(c.x, c.y + 1), new Vector2Int(c.x + 1, c.y + 1))) { zona[c.x, c.y + 1] = nZona; pila.Push(new Vector2Int(c.x, c.y + 1)); }
                    // abajo: tramo horizontal (c.x, c.y)-(c.x+1, c.y)
                    if (c.y - 1 >= 0 && zona[c.x, c.y - 1] < 0 && !EnCamino(new Vector2Int(c.x, c.y), new Vector2Int(c.x + 1, c.y))) { zona[c.x, c.y - 1] = nZona; pila.Push(new Vector2Int(c.x, c.y - 1)); }
                }
                nZona++;
            }
        var zonasOscuras = new HashSet<int>();
        foreach (var c in cuadrosOscuros) zonasOscuras.Add(zona[c.x, c.y]);
        foreach (var c in cuadrosClaros) if (zonasOscuras.Contains(zona[c.x, c.y])) return false;
        return true;
    }

    // ===================== dibujo extra =====================
    void DibujarTramo(Vector2Int a, Vector2Int b, Color32 col)
    {
        if (!Roto(a, b)) { Segmento(Px(a), Px(b), grosor, col); return; }
        // tramo roto: dos pedazos con un hueco en medio
        Vector2 pa = Px(a), pb = Px(b);
        Segmento(pa, Vector2.Lerp(pa, pb, 0.32f), grosor, col);
        Segmento(Vector2.Lerp(pa, pb, 0.68f), pb, grosor, col);
    }

    Vector2 CentroCelda(Vector2Int c) => (Px(c) + Px(c + Vector2Int.one)) * 0.5f;

    void Cuadro(Vector2 c, float lado, Color32 col)
    {
        float h = lado * 0.5f, r = lado * 0.18f;
        int x0 = Mathf.Max(0, (int)(c.x - h)), x1 = Mathf.Min(RES - 1, (int)(c.x + h));
        int y0 = Mathf.Max(0, (int)(c.y - h)), y1 = Mathf.Min(RES - 1, (int)(c.y + h));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                // esquinas redondeadas
                float dx = Mathf.Max(Mathf.Abs(x + 0.5f - c.x) - (h - r), 0f), dy = Mathf.Max(Mathf.Abs(y + 0.5f - c.y) - (h - r), 0f);
                if (dx * dx + dy * dy <= r * r) px[y * RES + x] = col;
            }
    }


// ===================== generacion al azar =====================
    System.Random azar;
    List<Vector2Int> sol;
    int pasosDfs;

    void Generar()
    {
        azar = new System.Random(System.Environment.TickCount ^ GetInstanceID());
        int nodos = (columnas + 1) * (filas + 1);
        int minLen = Mathf.Max(4, Mathf.RoundToInt(nodos * largoCamino));

        for (int intento = 0; intento < 60; intento++)
        {
            tramosRotos = new List<Tramo>();
            sol = CaminoAlAzar(minLen);
            if (sol == null) { minLen = Mathf.Max(4, minLen - 1); continue; }

            puntos = new List<Vector2Int>();
            cuadrosOscuros = new List<Vector2Int>();
            cuadrosClaros = new List<Vector2Int>();
            bool ok = true;
            if (regla == Regla.Puntos) PonerPuntos(Mathf.Clamp(columnas + 1, 3, 6));
            else if (regla == Regla.Cortados) { PonerPuntos(3); CortarTramos(); }
            else ok = PonerColores();
            if (!ok) continue;

            if (mostrarSolucionEnConsola) Debug.Log("[" + name + "] Solucion: " + DescribirCamino(sol));
            return;
        }
        Debug.LogWarning("[" + name + "] No se pudo generar un acertijo; se usa el diseno fijo.");
    }

    static readonly Vector2Int[] DIRS = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    List<Vector2Int> CaminoAlAzar(int minLen)
    {
        var camino0 = new List<Vector2Int> { inicio };
        var visit = new HashSet<Vector2Int> { inicio };
        pasosDfs = 0;
        return Dfs(inicio, camino0, visit, minLen) ? camino0 : null;
    }

    bool Dfs(Vector2Int n, List<Vector2Int> cam, HashSet<Vector2Int> visit, int minLen)
    {
        if (++pasosDfs > 40000) return false;
        if (n == salida) return cam.Count >= minLen;
        var dirs = DIRS.OrderBy(_ => azar.Next()).ToArray();
        foreach (var d in dirs)
        {
            var s = n + d;
            if (s.x < 0 || s.y < 0 || s.x > columnas || s.y > filas || visit.Contains(s)) continue;
            visit.Add(s); cam.Add(s);
            if (Dfs(s, cam, visit, minLen)) return true;
            visit.Remove(s); cam.RemoveAt(cam.Count - 1);
        }
        return false;
    }

    void PonerPuntos(int k)
    {
        // puntos sobre el camino secreto, repartidos (no en el inicio ni la salida)
        var opciones = Enumerable.Range(1, sol.Count - 2).OrderBy(_ => azar.Next()).ToList();
        var elegidos = new List<int>();
        foreach (int i in opciones)
        {
            if (elegidos.Count >= k) break;
            if (elegidos.Any(e => Mathf.Abs(e - i) < 2)) continue; // que no queden pegados
            elegidos.Add(i);
        }
        foreach (int i in elegidos) puntos.Add(sol[i]);
    }

    void CortarTramos()
    {
        // cortar tramos que NO son parte de la solucion
        var candidatos = new List<Tramo>();
        for (int x = 0; x <= columnas; x++)
            for (int y = 0; y <= filas; y++)
            {
                var n = new Vector2Int(x, y);
                if (x < columnas && !EnCaminoDe(sol, n, n + Vector2Int.right)) candidatos.Add(new Tramo { a = n, b = n + Vector2Int.right });
                if (y < filas && !EnCaminoDe(sol, n, n + Vector2Int.up)) candidatos.Add(new Tramo { a = n, b = n + Vector2Int.up });
            }
        int m = Mathf.Max(3, Mathf.RoundToInt(candidatos.Count * 0.3f));
        tramosRotos = candidatos.OrderBy(_ => azar.Next()).Take(m).ToList();
    }

    bool PonerColores()
    {
        var zona = ZonasDe(sol, out int nZonas);
        if (nZonas < 2) return false;
        // cada zona recibe un color (al menos una de cada)
        var colorZona = new bool[nZonas];
        for (int z = 0; z < nZonas; z++) colorZona[z] = azar.Next(2) == 0;
        colorZona[0] = true; colorZona[1] = false;
        var celdas = new List<Vector2Int>();
        for (int x = 0; x < columnas; x++) for (int y = 0; y < filas; y++) celdas.Add(new Vector2Int(x, y));
        int cuantos = Mathf.Max(4, Mathf.RoundToInt(celdas.Count * 0.6f));
        foreach (var c in celdas.OrderBy(_ => azar.Next()).Take(cuantos))
            (colorZona[zona[c.x, c.y]] ? cuadrosOscuros : cuadrosClaros).Add(c);
        return cuadrosOscuros.Count > 0 && cuadrosClaros.Count > 0;
    }

    static bool EnCaminoDe(List<Vector2Int> cam, Vector2Int a, Vector2Int b)
    {
        for (int i = 1; i < cam.Count; i++)
            if ((cam[i - 1] == a && cam[i] == b) || (cam[i - 1] == b && cam[i] == a)) return true;
        return false;
    }

    int[,] ZonasDe(List<Vector2Int> cam, out int nZona)
    {
        var zona = new int[columnas, filas];
        for (int x = 0; x < columnas; x++) for (int y = 0; y < filas; y++) zona[x, y] = -1;
        nZona = 0;
        var pila = new Stack<Vector2Int>();
        for (int x = 0; x < columnas; x++)
            for (int y = 0; y < filas; y++)
            {
                if (zona[x, y] >= 0) continue;
                zona[x, y] = nZona; pila.Push(new Vector2Int(x, y));
                while (pila.Count > 0)
                {
                    var c = pila.Pop();
                    if (c.x + 1 < columnas && zona[c.x + 1, c.y] < 0 && !EnCaminoDe(cam, new Vector2Int(c.x + 1, c.y), new Vector2Int(c.x + 1, c.y + 1))) { zona[c.x + 1, c.y] = nZona; pila.Push(new Vector2Int(c.x + 1, c.y)); }
                    if (c.x - 1 >= 0 && zona[c.x - 1, c.y] < 0 && !EnCaminoDe(cam, new Vector2Int(c.x, c.y), new Vector2Int(c.x, c.y + 1))) { zona[c.x - 1, c.y] = nZona; pila.Push(new Vector2Int(c.x - 1, c.y)); }
                    if (c.y + 1 < filas && zona[c.x, c.y + 1] < 0 && !EnCaminoDe(cam, new Vector2Int(c.x, c.y + 1), new Vector2Int(c.x + 1, c.y + 1))) { zona[c.x, c.y + 1] = nZona; pila.Push(new Vector2Int(c.x, c.y + 1)); }
                    if (c.y - 1 >= 0 && zona[c.x, c.y - 1] < 0 && !EnCaminoDe(cam, new Vector2Int(c.x, c.y), new Vector2Int(c.x + 1, c.y))) { zona[c.x, c.y - 1] = nZona; pila.Push(new Vector2Int(c.x, c.y - 1)); }
                }
                nZona++;
            }
        return zona;
    }

    static string DescribirCamino(List<Vector2Int> cam)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 1; i < cam.Count; i++)
        {
            var d = cam[i] - cam[i - 1];
            sb.Append(d == Vector2Int.up ? "W " : d == Vector2Int.down ? "S " : d == Vector2Int.left ? "A " : "D ");
        }
        return sb.ToString().Trim() + " + salida";
    }
}
