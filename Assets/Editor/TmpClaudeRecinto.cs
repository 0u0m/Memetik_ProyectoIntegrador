using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class TmpClaudeRecinto
{
    [MenuItem("Tools/TmpClaude/Recinto Final")]
    public static void Hacer()
    {
        var sb = new StringBuilder("TMPCLAUDE_RF|");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var terrain = Terrain.activeTerrain;
        var tPos = terrain.transform.position;
        var fuente = GameObject.Find("Fuente").transform.position;

        // 1) quitar la casa que puse mal en medio
        var casaMala = GameObject.Find("Casa_Final");
        if (casaMala != null) { Object.DestroyImmediate(casaMala); sb.Append("casaMalaBorrada|"); }

        // 2) mostrar de nuevo los muros grandes del porton
        var porton = GameObject.Find("Porton");
        var modelo = porton.transform.Find("Modelo");
        int mostrados = 0;
        foreach (Transform c in modelo) if (!c.gameObject.activeSelf) { c.gameObject.SetActive(true); mostrados++; }
        sb.Append("murosMostrados=" + mostrados + "|");

        // 3) ubicar el recinto al fondo del bosque (mas al norte, +z), centrado en x de la fuente
        // medir el recinto sin rotacion
        porton.transform.rotation = Quaternion.identity;
        var rs = porton.GetComponentsInChildren<Renderer>(true).Where(r => r.name != "Portal").ToArray();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        sb.Append("recintoSize=" + b.size.ToString("F0") + "|");

        // el recinto se extiende hacia +x (los muros estan en bcLocal.x=+19), asi que lo centramos
        // colocarlo pegado a la orilla norte de la meseta
        float zDestino = 700f;
        Vector3 pos = new Vector3(fuente.x, 0, zDestino);
        // ajustar para que el centro del recinto quede sobre el eje; el modelo tiene su reja en x~0 y muros hacia +x
        float centroX = b.center.x - porton.transform.position.x;
        pos.x = fuente.x - centroX;
        float suelo = terrain.SampleHeight(new Vector3(pos.x, 0, pos.z)) + tPos.y;
        // reapoyar
        porton.transform.position = new Vector3(pos.x, porton.transform.position.y, pos.z);
        var rs2 = porton.GetComponentsInChildren<Renderer>(true).Where(r => r.name != "Portal").ToArray();
        var b2 = rs2[0].bounds; foreach (var r in rs2) b2.Encapsulate(r.bounds);
        porton.transform.position += new Vector3(0, (suelo - 0.2f) - b2.min.y, 0);
        porton.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up); // reja mira al pueblo (-z)
        Vector3 posFinal = porton.transform.position;
        sb.Append("recinto=" + posFinal.ToString("F0") + " suelo=" + suelo.ToString("F0") + "|");

        // 4) casa dentro del recinto: tomar una casa del pueblo, hacerla alta y oscura
        var plaza = GameObject.Find("Pueblo_Plaza");
        Transform baseCasa = null; float mejor = 0;
        foreach (Transform c in plaza.transform)
        {
            var r2 = c.GetComponentsInChildren<Renderer>(false);
            if (r2.Length == 0) continue;
            var bb = r2[0].bounds; foreach (var r in r2) bb.Encapsulate(r.bounds);
            float area = bb.size.x * bb.size.z;
            if (area > mejor) { mejor = area; baseCasa = c; }
        }
        var vieja = GameObject.Find("Casa_Recinto");
        if (vieja != null) Object.DestroyImmediate(vieja);
        var casa = Object.Instantiate(baseCasa.gameObject);
        casa.name = "Casa_Recinto";
        casa.transform.localScale = Vector3.Scale(baseCasa.localScale, new Vector3(1.25f, 1.55f, 1.25f));

        var matFinal = Toon("Assets/Casas/Casa_Recinto_Pared.mat", new Color(0.28f, 0.24f, 0.30f));
        var paredMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Casas/Models/Materials/Pared/PAredes.mat");
        foreach (var r in casa.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials; bool ch = false;
            for (int i = 0; i < mats.Length; i++) if (mats[i] == paredMat) { mats[i] = matFinal; ch = true; }
            if (ch) r.sharedMaterials = mats;
        }
        // colocar la casa DENTRO del recinto (detras de la reja, hacia +z, centro de los muros)
        // los muros estan centrados en x local +19*escala del recinto respecto a la reja
        float escR = porton.transform.lossyScale.x;
        Vector3 dentro = posFinal + porton.transform.forward * (-20f) + porton.transform.right * (19f * escR * 0f);
        // forward del porton apunta a -z (hacia el pueblo), asi que "adentro" es -forward
        dentro = posFinal - porton.transform.forward * 22f;
        float yc = terrain.SampleHeight(dentro) + tPos.y;
        var bc = casa.GetComponentsInChildren<Renderer>()[0].bounds;
        foreach (var r in casa.GetComponentsInChildren<Renderer>()) bc.Encapsulate(r.bounds);
        casa.transform.position = new Vector3(dentro.x, yc - 0.3f, dentro.z);
        casa.transform.rotation = Quaternion.LookRotation((posFinal - casa.transform.position).normalized, Vector3.up); // puerta hacia la reja
        sb.Append("casa=" + casa.transform.position.ToString("F0") + "|");

        // 5) despejar pinos y pasto donde va el recinto
        var bosque = GameObject.Find("Bosque");
        var centro2 = new Vector2(posFinal.x + 20f, posFinal.z);
        int quitados = 0;
        if (bosque != null)
            foreach (Transform p in bosque.transform.Cast<Transform>().ToList())
            {
                if (Vector2.Distance(new Vector2(p.position.x, p.position.z), centro2) < 45f) { Object.DestroyImmediate(p.gameObject); quitados++; }
            }
        var td = terrain.terrainData; int res = td.detailResolution;
        var layer = td.GetDetailLayer(0, 0, res, res, 0);
        for (int zi = 0; zi < res; zi++) for (int xi = 0; xi < res; xi++)
        {
            float wx = tPos.x + (xi + 0.5f) / res * td.size.x, wz = tPos.z + (zi + 0.5f) / res * td.size.z;
            if (Vector2.Distance(new Vector2(wx, wz), centro2) < 40f) layer[zi, xi] = 0;
        }
        td.SetDetailLayer(0, 0, 0, layer);
        sb.Append("pinosQuitados=" + quitados + "|");

        // 6) camara que viaja a la casa en cada nota
        var prog = Object.FindFirstObjectByType<ProgresoNotas>();
        var camNotaGO = GameObject.Find("CamaraNota");
        if (camNotaGO == null) camNotaGO = new GameObject("CamaraNota");
        var camNota = camNotaGO.GetComponent<CamaraNota>(); if (camNota == null) camNota = camNotaGO.AddComponent<CamaraNota>();
        var mira = new GameObject("MiraFinal").transform;
        mira.SetParent(camNotaGO.transform, false);
        mira.position = posFinal + Vector3.up * 8f;
        camNota.puntoMira = mira;
        if (prog != null) { prog.camaraNota = camNota; EditorUtility.SetDirty(prog); }
        sb.Append("camaraNota=ok");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log(sb.ToString());
    }

    static Material Toon(string path, Color c)
    {
        var sh = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Casas/Toon_Casas.shader");
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
        m.shader = sh; m.SetColor("_BaseColor", c); m.SetColor("_EmissionColor", Color.black); m.SetFloat("_RimAmount", 0.2f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
