// Colocar en: Assets/Editor/GeneradorPueblo.cs
// Menu: Tools > Generar pueblo
// Arma casas a la medida de TUS techos (rectangulares y en L) y las acomoda
// en dos filas frente a una calle central.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class GeneradorPueblo : EditorWindow
{
    // --- Piezas (arrastra PREFABS o FBX desde la ventana Project) ---
    public GameObject[] paredes;     // SOLO paredes rectas (las L no)
    public GameObject[] puertas;
    public GameObject[] ventanas;
    public GameObject[] techos;      // rectangulares y en L, todos juntos

    // --- Medidas del kit ---
    float modulo = 2f;               // ancho de una pared
    float alturaPared = 2.2f;
    float grosor = 0.2f;
    float aleroTecho = 0.3f;         // cuanto sobresale TU techo de la pared, por lado
    float hundirTecho = 0.05f;       // baja el techo para que no quede hueco
    float alturaVentana = 0.4f;      // de la base de la pared a la base de la ventana

    // --- Pueblo ---
    int cantidad = 10;
    int semilla = 1;
    float calle = 6f;
    float separacionCasas = 1.5f;
    bool crearTerreno = true;
    bool colisiones = true;
    bool girarDecoracion = false;    // activalo si puertas/ventanas se ven volteadas
    float probDosPisos = 0.4f;       // probabilidad de que una casa tenga dos pisos
    bool usarTerreno = true;         // asentar el pueblo sobre el Terrain de la escena
    Vector3 posicionPueblo = Vector3.zero;  // centro del pueblo
    float margenNivelado = 4f;       // metros extra que se nivelan alrededor
    float suavizado = 8f;            // metros de transicion hacia el terreno original

    SerializedObject so;
    Vector2 scroll;

    class InfoTecho
    {
        public GameObject prefab;
        public Vector3 tam;
        public int sx, sz;           // 0 = techo rectangular; +-1 = esquina donde falta la L
        public float nx, nz;         // tamano de la muesca de la L
    }

    struct Seg { public Vector3 pos; public float rotY; }

    [MenuItem("Tools/Generar pueblo")]
    static void Abrir() { GetWindow<GeneradorPueblo>("Generar pueblo"); }

    void OnEnable() { so = new SerializedObject(this); }

    void OnGUI()
    {
        so.Update();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Piezas", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("paredes"), true);
        EditorGUILayout.PropertyField(so.FindProperty("puertas"), true);
        EditorGUILayout.PropertyField(so.FindProperty("ventanas"), true);
        EditorGUILayout.PropertyField(so.FindProperty("techos"), true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Medidas del kit", EditorStyles.boldLabel);
        modulo = EditorGUILayout.FloatField("Ancho de pared (m)", modulo);
        alturaPared = EditorGUILayout.FloatField("Alto de pared (m)", alturaPared);
        grosor = EditorGUILayout.FloatField("Grosor de pared (m)", grosor);
        aleroTecho = EditorGUILayout.FloatField("Alero de tus techos (m por lado)", aleroTecho);
        hundirTecho = EditorGUILayout.FloatField("Bajar techo (m)", hundirTecho);
        alturaVentana = EditorGUILayout.FloatField("Altura de ventana (m)", alturaVentana);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pueblo", EditorStyles.boldLabel);
        cantidad = EditorGUILayout.IntSlider("Cantidad de casas", cantidad, 1, 40);
        probDosPisos = EditorGUILayout.Slider("Probabilidad de dos pisos", probDosPisos, 0f, 1f);
        semilla = EditorGUILayout.IntField("Semilla", semilla);
        calle = EditorGUILayout.FloatField("Ancho de la calle (m)", calle);
        separacionCasas = EditorGUILayout.FloatField("Separacion entre casas (m)", separacionCasas);
        crearTerreno = EditorGUILayout.Toggle("Crear terreno y calle", crearTerreno);
        colisiones = EditorGUILayout.Toggle("Agregar colisiones", colisiones);
        girarDecoracion = EditorGUILayout.Toggle("Girar 180 puerta y ventana", girarDecoracion);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Terreno", EditorStyles.boldLabel);
        usarTerreno = EditorGUILayout.Toggle("Colocar sobre el Terrain de la escena", usarTerreno);
        posicionPueblo = EditorGUILayout.Vector3Field("Centro del pueblo", posicionPueblo);
        if (GUILayout.Button("Tomar centro de la vista de escena"))
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) posicionPueblo = sv.pivot;
        }
        margenNivelado = EditorGUILayout.FloatField("Margen nivelado (m)", margenNivelado);
        suavizado = EditorGUILayout.FloatField("Suavizado de orillas (m)", suavizado);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        if (GUILayout.Button("Medir mis piezas (diagnostico)", GUILayout.Height(24)))
            MedirPiezas();
        if (GUILayout.Button("Generar pueblo", GUILayout.Height(32)))
            Generar();
    }

    // ------------------------------------------------------------------
    void Generar()
    {
        paredes = Limpiar(paredes);
        puertas = Limpiar(puertas);
        ventanas = Limpiar(ventanas);
        techos = Limpiar(techos);

        if (paredes.Length == 0 || techos.Length == 0)
        {
            EditorUtility.DisplayDialog("Falta algo",
                "Arrastra al menos una pared recta y un techo.", "OK");
            return;
        }

        AsegurarCarpeta("Assets/Casas");
        AsegurarCarpeta("Assets/Casas/Materiales");

        // Borra el pueblo anterior para que no se encimen
        var viejo = GameObject.Find("Pueblo");
        if (viejo != null) Undo.DestroyObjectImmediate(viejo);

        var infos = new List<InfoTecho>();
        foreach (var tp in techos) infos.Add(Analizar(tp));

        var rnd = new System.Random(semilla);
        var pueblo = new GameObject("Pueblo");
        Undo.RegisterCreatedObjectUndo(pueblo, "Generar pueblo");

        float cursorNorte = 0f, cursorSur = 0f, maxFondo = 0f;

        for (int i = 0; i < cantidad; i++)
        {
            InfoTecho it = infos[rnd.Next(infos.Count)];
            float ancho, fondo;
            GameObject casa = ConstruirCasa(i, rnd, it, out ancho, out fondo);

            string ruta = "Assets/Casas/Casa_" + (i + 1) + ".prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(casa, ruta, InteractionMode.AutomatedAction);

            casa.transform.SetParent(pueblo.transform, false);
            float paso = ancho + separacionCasas;
            if (i % 2 == 0)   // fila norte: la puerta mira al sur
            {
                casa.transform.localPosition = new Vector3(cursorNorte + paso / 2f, 0, calle / 2f + fondo / 2f);
                casa.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                cursorNorte += paso;
            }
            else              // fila sur: la puerta mira al norte
            {
                casa.transform.localPosition = new Vector3(cursorSur + paso / 2f, 0, -calle / 2f - fondo / 2f);
                casa.transform.localRotation = Quaternion.identity;
                cursorSur += paso;
            }
            maxFondo = Mathf.Max(maxFondo, fondo);
        }

        float largo = Mathf.Max(cursorNorte, cursorSur);
        Vector3 origen = new Vector3(posicionPueblo.x - largo / 2f, posicionPueblo.y, posicionPueblo.z);
        Terrain terr = usarTerreno ? Terrain.activeTerrain : null;
        if (terr != null) origen.y = NivelarTerreno(terr, origen, largo, maxFondo);
        pueblo.transform.position = origen;

        if (crearTerreno)
            CrearSuelo(pueblo.transform, largo, maxFondo, terr != null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = pueblo;
        Debug.Log("Pueblo generado: " + cantidad + " casas.");
    }

    // Escribe en la Console el tamano real de cada pieza, tal como llega a Unity
    void MedirPiezas()
    {
        var sb = new System.Text.StringBuilder("MEDIDAS DE PIEZAS (X, Y, Z en metros)\n");
        AgregarMedidas(sb, "Pared", paredes);
        AgregarMedidas(sb, "Puerta", puertas);
        AgregarMedidas(sb, "Ventana", ventanas);
        AgregarMedidas(sb, "Techo", techos);
        Debug.Log(sb.ToString());
    }

    void AgregarMedidas(System.Text.StringBuilder sb, string tipo, GameObject[] lista)
    {
        foreach (var g in Limpiar(lista))
        {
            GameObject inst = PrefabUtility.IsPartOfPrefabAsset(g)
                ? (GameObject)PrefabUtility.InstantiatePrefab(g)
                : Instantiate(g);
            var rs = inst.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) sb.AppendLine(tipo + " " + g.name + ": SIN MALLAS");
            else
            {
                Bounds b = Medir(rs);
                sb.AppendLine(tipo + " " + g.name + ": X=" + b.size.x.ToString("F3")
                    + " Y=" + b.size.y.ToString("F3") + " Z=" + b.size.z.ToString("F3"));
            }
            DestroyImmediate(inst);
        }
    }

    // ------------------------------------------------------------------
    GameObject ConstruirCasa(int idx, System.Random r, InfoTecho it, out float ancho, out float fondo)
    {
        float t = grosor;

        // Cuantas paredes mide la casa, segun el tamano de TU techo
        int w = MejorN(it.tam.x, 2f * t + 2f * aleroTecho);
        int d = MejorN(it.tam.z, 2f * aleroTecho);
        bool esL = it.sx != 0;
        if (esL) { w = Mathf.Max(2, w); d = Mathf.Max(2, d); }

        // Casillas ocupadas (la L tiene una esquina vacia)
        var occ = new bool[w, d];
        int nI = 0, nJ = 0;
        if (esL)
        {
            nI = Mathf.Clamp(Mathf.RoundToInt(it.nx / modulo), 1, w - 1);
            nJ = Mathf.Clamp(Mathf.RoundToInt(it.nz / modulo), 1, d - 1);
        }
        for (int i = 0; i < w; i++)
            for (int j = 0; j < d; j++)
            {
                bool vacia = false;
                if (esL)
                {
                    bool enI = it.sx > 0 ? i >= w - nI : i < nI;
                    bool enJ = it.sz > 0 ? j >= d - nJ : j < nJ;
                    vacia = enI && enJ;
                }
                occ[i, j] = !vacia;
            }

        float W = w * modulo, D = d * modulo;
        var root = new GameObject("Casa_" + (idx + 1));
        GameObject paredPrefab = paredes[r.Next(paredes.Length)];
        int pisos = r.NextDouble() < probDosPisos ? 2 : 1;

        // Paredes en el borde de la casa. Las de lado X van hacia adentro y las de
        // lado Z hacia afuera, asi las esquinas cierran sin huecos ni encimes.
        var segs = new List<Seg>();
        for (int i = 0; i < w; i++)
            for (int j = 0; j < d; j++)
            {
                if (!occ[i, j]) continue;
                float cx = -W / 2f + modulo * (i + 0.5f);
                float cz = -D / 2f + modulo * (j + 0.5f);
                if (!Ocupada(occ, w, d, i, j + 1))
                    segs.Add(new Seg { pos = new Vector3(cx, 0, cz + modulo / 2f - t / 2f), rotY = 0f });
                if (!Ocupada(occ, w, d, i, j - 1))
                    segs.Add(new Seg { pos = new Vector3(cx, 0, cz - modulo / 2f + t / 2f), rotY = 180f });
                if (!Ocupada(occ, w, d, i + 1, j))
                    segs.Add(new Seg { pos = new Vector3(cx + modulo / 2f + t / 2f, 0, cz), rotY = 90f });
                if (!Ocupada(occ, w, d, i - 1, j))
                    segs.Add(new Seg { pos = new Vector3(cx - modulo / 2f - t / 2f, 0, cz), rotY = -90f });
            }

        for (int piso = 0; piso < pisos; piso++)
        foreach (var s in segs)
        {
            Vector3 tam;
            GameObject p = Normalizar(paredPrefab, "Pared", out tam);
            p.transform.SetParent(root.transform, false);
            p.transform.localPosition = s.pos + Vector3.up * (piso * alturaPared);
            p.transform.localRotation = Quaternion.Euler(0, s.rotY, 0);
            if (colisiones)
            {
                var bc = p.AddComponent<BoxCollider>();
                bc.center = new Vector3(0, alturaPared / 2f, 0);
                bc.size = new Vector3(modulo, alturaPared, t);
            }
        }

        // Puerta en una pared que mire al frente (+Z)
        int idxPuerta = -1;
        if (puertas.Length > 0)
        {
            var frentes = new List<int>();
            for (int i = 0; i < segs.Count; i++)
                if (Mathf.Approximately(segs[i].rotY, 0f)) frentes.Add(i);
            if (frentes.Count > 0)
            {
                idxPuerta = frentes[r.Next(frentes.Count)];
                Decorar(root, puertas[r.Next(puertas.Length)], "Puerta", segs[idxPuerta], t, 0f);
            }
        }

        // Ventanas (60% de las demas paredes)
        if (ventanas.Length > 0)
        {
            for (int piso = 0; piso < pisos; piso++)
                for (int i = 0; i < segs.Count; i++)
                {
                    if (i == idxPuerta && piso == 0) continue;
                    if (r.NextDouble() < 0.6)
                        Decorar(root, ventanas[r.Next(ventanas.Length)], "Ventana", segs[i], t,
                            alturaVentana + piso * alturaPared);
                }
        }

        // Tu techo, ajustado a la casa
        Vector3 tamR;
        GameObject techo = Normalizar(it.prefab, "Techo", out tamR);
        techo.transform.SetParent(root.transform, false);
        float objX = W + 2f * t + 2f * aleroTecho;
        float objZ = D + 2f * aleroTecho;
        float ex = tamR.x > 0.01f ? Mathf.Clamp(objX / tamR.x, 0.6f, 1.6f) : 1f;
        float ez = tamR.z > 0.01f ? Mathf.Clamp(objZ / tamR.z, 0.6f, 1.6f) : 1f;
        techo.transform.localScale = new Vector3(ex, 1f, ez);
        techo.transform.localPosition = new Vector3(0, alturaPared * pisos - hundirTecho, 0);
        if (colisiones)
            foreach (var mf in techo.GetComponentsInChildren<MeshFilter>())
                if (mf.GetComponent<Collider>() == null && mf.sharedMesh != null)
                    mf.gameObject.AddComponent<MeshCollider>();

        ancho = W + 2f * t + 2f * aleroTecho;
        fondo = D;
        return root;
    }

    // Numero de paredes que deja el techo lo mas cerca posible de su tamano real
    int MejorN(float tamTecho, float extra)
    {
        if (tamTecho < 0.01f) return 1;
        int f = Mathf.Max(1, Mathf.FloorToInt((tamTecho - extra) / modulo));
        int c = f + 1;
        float rf = (f * modulo + extra) / tamTecho;
        float rc = (c * modulo + extra) / tamTecho;
        return Mathf.Abs(Mathf.Log(rf)) <= Mathf.Abs(Mathf.Log(rc)) ? f : c;
    }

    static bool Ocupada(bool[,] occ, int w, int d, int i, int j)
    {
        if (i < 0 || j < 0 || i >= w || j >= d) return false;
        return occ[i, j];
    }

    void Decorar(GameObject root, GameObject prefab, string nombre, Seg s, float t, float alturaBase)
    {
        Vector3 tam;
        GameObject g = Normalizar(prefab, nombre, out tam);
        g.transform.SetParent(root.transform, false);
        Quaternion rot = Quaternion.Euler(0, s.rotY, 0);
        Vector3 fuera = rot * Vector3.forward;
        float prof = tam.z > 0.001f ? tam.z : 0.1f;
        g.transform.localPosition = s.pos + fuera * (t / 2f + prof / 2f) + Vector3.up * alturaBase;
        g.transform.localRotation = rot * Quaternion.Euler(0, girarDecoracion ? 180f : 0f, 0);
    }

    // ------------------------------------------------------------------
    // Contenedor con el origen en el CENTRO de la base de la pieza. Si su lado
    // largo quedo en Z, se gira 90 grados para dejarlo en X.
    GameObject Normalizar(GameObject prefab, string nombre, out Vector3 tam)
    {
        var cont = new GameObject(nombre);
        GameObject inst = PrefabUtility.IsPartOfPrefabAsset(prefab)
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : Instantiate(prefab);
        inst.transform.SetParent(cont.transform, false);
        inst.transform.localPosition = Vector3.zero;

        var rs = inst.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { tam = Vector3.zero; return cont; }

        Bounds b = Medir(rs);
        if (b.size.z > b.size.x * 1.01f)
        {
            inst.transform.Rotate(0, 90f, 0, Space.World);
            b = Medir(rs);
        }

        inst.transform.position -= new Vector3(b.center.x, b.min.y, b.center.z);
        tam = b.size;
        return cont;
    }

    static Bounds Medir(Renderer[] rs)
    {
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ------------------------------------------------------------------
    InfoTecho Analizar(GameObject prefab)
    {
        Vector3 tam;
        GameObject tmp = Normalizar(prefab, "tmp", out tam);
        var it = new InfoTecho { prefab = prefab, tam = tam };
        DetectarMuesca(tmp, tam, out it.sx, out it.sz, out it.nx, out it.nz);
        DestroyImmediate(tmp);
        return it;
    }

    // Una L tiene una esquina de su caja sin geometria cerca: ahi esta la muesca.
    static void DetectarMuesca(GameObject cont, Vector3 tam, out int sx, out int sz, out float nx, out float nz)
    {
        sx = 0; sz = 0; nx = 0; nz = 0;

        var pts = new List<Vector3>();
        foreach (var mf in cont.GetComponentsInChildren<MeshFilter>())
        {
            Mesh m = mf.sharedMesh;
            if (m == null) continue;
            Matrix4x4 mat = mf.transform.localToWorldMatrix;
            foreach (var v in m.vertices) pts.Add(mat.MultiplyPoint3x4(v));
        }
        if (pts.Count == 0 || tam.x < 0.01f || tam.z < 0.01f) return;

        float hx = tam.x / 2f, hz = tam.z / 2f;
        float lim = 0.25f * Mathf.Min(tam.x, tam.z);
        float mejor = 0f;

        for (int ix = -1; ix <= 1; ix += 2)
            for (int iz = -1; iz <= 1; iz += 2)
            {
                var c = new Vector2(ix * hx, iz * hz);
                float dmin = float.MaxValue;
                foreach (var p in pts)
                {
                    float dd = (new Vector2(p.x, p.z) - c).magnitude;
                    if (dd < dmin) dmin = dd;
                }
                if (dmin > lim && dmin > mejor) { mejor = dmin; sx = ix; sz = iz; }
            }

        if (sx == 0) return;

        float tol = 0.1f * Mathf.Min(tam.x, tam.z);
        float k1 = -hx, k2 = -hz;
        foreach (var p in pts)
        {
            if (Mathf.Abs(p.z - sz * hz) < tol) k1 = Mathf.Max(k1, sx * p.x);
            if (Mathf.Abs(p.x - sx * hx) < tol) k2 = Mathf.Max(k2, sz * p.z);
        }
        nx = Mathf.Max(0f, hx - k1);
        nz = Mathf.Max(0f, hz - k2);
    }

    // Aplana el Terrain bajo el pueblo (con orillas suaves) y devuelve la altura del suelo
    float NivelarTerreno(Terrain t, Vector3 origen, float largo, float maxFondo)
    {
        TerrainData td = t.terrainData;
        Vector3 tp = t.transform.position;
        float h = t.SampleHeight(new Vector3(origen.x + largo / 2f, 0f, origen.z)) + tp.y;

        float x0 = origen.x - margenNivelado, x1 = origen.x + largo + margenNivelado;
        float zr = calle / 2f + maxFondo + margenNivelado;
        float z0 = origen.z - zr, z1 = origen.z + zr;
        float blend = Mathf.Max(0.1f, suavizado);

        int res = td.heightmapResolution;
        int ix0 = Mathf.Clamp(Mathf.FloorToInt((x0 - blend - tp.x) / td.size.x * (res - 1)), 0, res - 1);
        int ix1 = Mathf.Clamp(Mathf.CeilToInt((x1 + blend - tp.x) / td.size.x * (res - 1)), 0, res - 1);
        int iz0 = Mathf.Clamp(Mathf.FloorToInt((z0 - blend - tp.z) / td.size.z * (res - 1)), 0, res - 1);
        int iz1 = Mathf.Clamp(Mathf.CeilToInt((z1 + blend - tp.z) / td.size.z * (res - 1)), 0, res - 1);
        int nx = ix1 - ix0 + 1, nz = iz1 - iz0 + 1;
        if (nx <= 1 || nz <= 1)
        {
            Debug.LogWarning("El centro del pueblo queda fuera del Terrain. Muevelo dentro del terreno.");
            return h;
        }

        Undo.RegisterCompleteObjectUndo(td, "Nivelar terreno");
        float[,] alt = td.GetHeights(ix0, iz0, nx, nz);
        float hn = Mathf.Clamp01((h - tp.y) / td.size.y);

        for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float wx = tp.x + (ix0 + i) / (float)(res - 1) * td.size.x;
                float wz = tp.z + (iz0 + j) / (float)(res - 1) * td.size.z;
                float dx = Mathf.Max(Mathf.Max(x0 - wx, 0f), wx - x1);
                float dz = Mathf.Max(Mathf.Max(z0 - wz, 0f), wz - z1);
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float k = 1f - Mathf.SmoothStep(0f, 1f, dist / blend);
                alt[j, i] = Mathf.Lerp(alt[j, i], hn, k);
            }

        td.SetHeights(ix0, iz0, alt);
        return h;
    }

    // ------------------------------------------------------------------
    void CrearSuelo(Transform padre, float largo, float maxFondo, bool soloCalle)
    {
        float ancho = Mathf.Max(20f, largo + 40f);
        float prof = calle + 2f * (maxFondo + 10f);

        if (!soloCalle)
        {
            var terreno = GameObject.CreatePrimitive(PrimitiveType.Plane);
            terreno.name = "Terreno";
            terreno.transform.SetParent(padre, false);
            terreno.transform.localPosition = new Vector3(largo / 2f, -0.03f, 0);
            terreno.transform.localScale = new Vector3(ancho / 10f, 1f, prof / 10f);
            terreno.GetComponent<Renderer>().sharedMaterial =
                MaterialLiso("Terreno", new Color(0.45f, 0.58f, 0.36f));
        }

        var via = GameObject.CreatePrimitive(PrimitiveType.Cube);
        via.name = "Calle";
        via.transform.SetParent(padre, false);
        via.transform.localPosition = new Vector3(largo / 2f, 0f, 0);
        via.transform.localScale = new Vector3(largo + 10f, 0.04f, calle);
        via.GetComponent<Renderer>().sharedMaterial =
            MaterialLiso("Calle", new Color(0.62f, 0.56f, 0.48f));
    }

    Material MaterialLiso(string nombre, Color c)
    {
        string ruta = "Assets/Casas/Materiales/" + nombre + ".mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;

        Shader sh;
        if (GraphicsSettings.currentRenderPipeline == null)
            sh = Shader.Find("Standard");
        else
        {
            sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("HDRP/Lit");
        }
        if (sh == null) sh = Shader.Find("Standard");

        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.05f);
        AssetDatabase.CreateAsset(m, ruta);
        return m;
    }

    static GameObject[] Limpiar(GameObject[] a)
    {
        return System.Array.FindAll(a ?? new GameObject[0], g => g != null);
    }

    static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        string padre = System.IO.Path.GetDirectoryName(ruta).Replace('\\', '/');
        string nombre = System.IO.Path.GetFileName(ruta);
        AssetDatabase.CreateFolder(padre, nombre);
    }
}
