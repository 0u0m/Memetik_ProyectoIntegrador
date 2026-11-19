using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Menu: Tools > Ambiente > Noche (vista previa) / Dia (regresar)
// Al pasar a Noche guarda los ajustes de Dia en Assets/Ambiente/Estado_Dia.json
// y "Dia" los restaura tal cual. No borra nada.
public static class CambiarAmbiente
{
    const string JSON = "Assets/Ambiente/Estado_Dia.json";
    const string CIELO_MAT = "Assets/Ambiente/Cielo_Noche.mat";
    const string VOLUMEN_NOCHE = "Assets/Ambiente/Noche_Volume.asset";

    [Serializable] class MatColor { public string path; public string prop; public Color color; }
    [Serializable] class Estado
    {
        public Color luzColor; public float luzIntensidad; public Vector3 luzRot;
        public int ambientMode; public Color ambSky, ambEq, ambGround, ambLight; public float ambIntensity;
        public bool fog; public Color fogColor; public int fogMode; public float fogDensity, fogStart, fogEnd;
        public string skyboxPath, skyboxName;
        public string volumenPath;
        public List<MatColor> mats = new List<MatColor>();
    }

    // Colores de noche para materiales que no dependen de la luz (agua, ventanas)
    static readonly (string path, string prop, Color color)[] MATS_NOCHE =
    {
        ("Assets/Agua/Agua_Lago.mat", "_ShallowColor", new Color(0.12f, 0.32f, 0.42f, 0.85f)),
        ("Assets/Agua/Agua_Lago.mat", "_DeepColor", new Color(0.03f, 0.08f, 0.20f, 0.95f)),
        ("Assets/Agua/Agua_Lago.mat", "_FoamColor", new Color(0.55f, 0.65f, 0.80f, 1f)),
        ("Assets/Agua/Agua_Lago.mat", "_SpecColor2", new Color(0.85f, 0.9f, 1f, 1f)),
        ("Assets/Fuente/Fuente_Agua.mat", "_ShallowColor", new Color(0.20f, 0.42f, 0.55f, 0.85f)),
        ("Assets/Fuente/Fuente_Agua.mat", "_DeepColor", new Color(0.06f, 0.16f, 0.30f, 0.95f)),
        ("Assets/Fuente/Fuente_Agua.mat", "_FoamColor", new Color(0.60f, 0.72f, 0.85f, 1f)),
        ("Assets/Fuente/Fuente_Cascada.mat", "_ColorAgua", new Color(0.25f, 0.45f, 0.62f, 0.55f)),
        ("Assets/Fuente/Fuente_Cascada.mat", "_ColorFranja", new Color(0.65f, 0.80f, 0.95f, 0.85f)),
        ("Assets/Casas/Models/Materials/Ventanas/Cristal.mat", "_EmissionColor", new Color(1f, 0.6f, 0.25f) * 1.8f),
        // (Puerta1_Extraidos/Cristal.mat NO va aqui: en realidad es la base de la manija, no vidrio)
        ("Assets/Casas/Models/Materials/Puerta1_Extraidos/Cristal.001.mat", "_EmissionColor", new Color(1f, 0.6f, 0.25f) * 1.8f),
    };

    static Light Sol()
    {
        if (RenderSettings.sun != null) return RenderSettings.sun;
        return UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
    }
    static Volume VolumenGlobal()
    {
        return UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).FirstOrDefault(v => v.isGlobal);
    }

    [MenuItem("Tools/Ambiente/Noche (vista previa)")]
    public static void Noche()
    {
        var sol = Sol();
        var vol = VolumenGlobal();

        // 1) guardar el estado de dia (solo si no esta guardado ya)
        if (!File.Exists(JSON))
        {
            var e = new Estado();
            if (sol != null) { e.luzColor = sol.color; e.luzIntensidad = sol.intensity; e.luzRot = sol.transform.eulerAngles; }
            e.ambientMode = (int)RenderSettings.ambientMode; e.ambSky = RenderSettings.ambientSkyColor; e.ambEq = RenderSettings.ambientEquatorColor;
            e.ambGround = RenderSettings.ambientGroundColor; e.ambLight = RenderSettings.ambientLight; e.ambIntensity = RenderSettings.ambientIntensity;
            e.fog = RenderSettings.fog; e.fogColor = RenderSettings.fogColor; e.fogMode = (int)RenderSettings.fogMode;
            e.fogDensity = RenderSettings.fogDensity; e.fogStart = RenderSettings.fogStartDistance; e.fogEnd = RenderSettings.fogEndDistance;
            if (RenderSettings.skybox != null) { e.skyboxPath = AssetDatabase.GetAssetPath(RenderSettings.skybox); e.skyboxName = RenderSettings.skybox.name; }
            if (vol != null && vol.sharedProfile != null) e.volumenPath = AssetDatabase.GetAssetPath(vol.sharedProfile);
            foreach (var m in MATS_NOCHE)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(m.path);
                if (mat != null && mat.HasProperty(m.prop)) e.mats.Add(new MatColor { path = m.path, prop = m.prop, color = mat.GetColor(m.prop) });
            }
            if (!AssetDatabase.IsValidFolder("Assets/Ambiente")) AssetDatabase.CreateFolder("Assets", "Ambiente");
            File.WriteAllText(JSON, JsonUtility.ToJson(e, true));
            AssetDatabase.ImportAsset(JSON);
        }

        // 2) luna
        if (sol != null)
        {
            Undo.RecordObject(sol, "Noche"); Undo.RecordObject(sol.transform, "Noche");
            sol.color = new Color(0.6f, 0.7f, 1f);
            sol.intensity = 0.45f;
            sol.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        }

        // 3) luz ambiental y niebla
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.10f, 0.13f, 0.25f);
        RenderSettings.ambientEquatorColor = new Color(0.07f, 0.09f, 0.17f);
        RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.17f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.0045f;

        // 4) cielo
        var cielo = AssetDatabase.LoadAssetAtPath<Material>(CIELO_MAT);
        if (cielo == null)
        {
            cielo = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Ambiente/Cielo_Noche.shader"));
            AssetDatabase.CreateAsset(cielo, CIELO_MAT);
        }
        if (sol != null) cielo.SetVector("_LunaDir", -sol.transform.forward);
        EditorUtility.SetDirty(cielo);
        RenderSettings.skybox = cielo;

        // 5) materiales de agua y ventanas
        foreach (var m in MATS_NOCHE)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(m.path);
            if (mat == null || !mat.HasProperty(m.prop)) continue;
            mat.SetColor(m.prop, m.color);
            EditorUtility.SetDirty(mat);
        }

        // 6) postproceso: bloom (resplandor) + vineta
        if (vol != null)
        {
            var noche = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VOLUMEN_NOCHE);
            if (noche == null)
            {
                noche = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(noche, VOLUMEN_NOCHE);
                if (vol.sharedProfile != null)
                    foreach (var c in vol.sharedProfile.components)
                    {
                        var copia = UnityEngine.Object.Instantiate(c);
                        copia.name = c.name;
                        AssetDatabase.AddObjectToAsset(copia, noche);
                        noche.components.Add(copia);
                    }
                if (!noche.TryGet(out Bloom bloom)) { bloom = noche.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom, noche); }
                bloom.active = true;
                bloom.threshold.Override(0.9f); bloom.intensity.Override(1.3f); bloom.scatter.Override(0.65f);
                if (!noche.TryGet(out Vignette vig)) { vig = noche.Add<Vignette>(); AssetDatabase.AddObjectToAsset(vig, noche); }
                vig.active = true;
                vig.intensity.Override(0.33f); vig.smoothness.Override(0.5f);
                EditorUtility.SetDirty(noche);
            }
            Undo.RecordObject(vol, "Noche");
            vol.sharedProfile = noche;
        }
        ActivarNeblina(true);

        AssetDatabase.SaveAssets();
        DynamicGI.UpdateEnvironment();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AMBIENTE: Noche aplicada (Tools > Ambiente > Dia para regresar)");
    }

    [MenuItem("Tools/Ambiente/Dia (regresar)")]
    public static void Dia()
    {
        if (!File.Exists(JSON)) { Debug.Log("AMBIENTE: no hay estado de dia guardado; ya esta en dia."); return; }
        var e = JsonUtility.FromJson<Estado>(File.ReadAllText(JSON));
        var sol = Sol();
        if (sol != null)
        {
            Undo.RecordObject(sol, "Dia"); Undo.RecordObject(sol.transform, "Dia");
            sol.color = e.luzColor; sol.intensity = e.luzIntensidad; sol.transform.eulerAngles = e.luzRot;
        }
        RenderSettings.ambientMode = (AmbientMode)e.ambientMode;
        RenderSettings.ambientSkyColor = e.ambSky; RenderSettings.ambientEquatorColor = e.ambEq;
        RenderSettings.ambientGroundColor = e.ambGround; RenderSettings.ambientLight = e.ambLight; RenderSettings.ambientIntensity = e.ambIntensity;
        RenderSettings.fog = e.fog; RenderSettings.fogColor = e.fogColor; RenderSettings.fogMode = (FogMode)e.fogMode;
        RenderSettings.fogDensity = e.fogDensity; RenderSettings.fogStartDistance = e.fogStart; RenderSettings.fogEndDistance = e.fogEnd;

        Material sky = null;
        if (!string.IsNullOrEmpty(e.skyboxPath))
        {
            sky = AssetDatabase.LoadAssetAtPath<Material>(e.skyboxPath);
            if (sky == null) sky = AssetDatabase.LoadAllAssetsAtPath(e.skyboxPath).OfType<Material>().FirstOrDefault(m => m.name == e.skyboxName);
            if (sky == null) sky = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        }
        RenderSettings.skybox = sky;

        foreach (var m in e.mats)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(m.path);
            if (mat == null || !mat.HasProperty(m.prop)) continue;
            mat.SetColor(m.prop, m.color);
            EditorUtility.SetDirty(mat);
        }
        var vol = VolumenGlobal();
        if (vol != null && !string.IsNullOrEmpty(e.volumenPath))
        {
            Undo.RecordObject(vol, "Dia");
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(e.volumenPath);
        }
        ActivarNeblina(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.DeleteAsset(JSON);
        DynamicGI.UpdateEnvironment();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("AMBIENTE: Dia restaurado");
    }


// La neblina baja solo se ve de noche
    static void ActivarNeblina(bool activa)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "Neblina") { Undo.RecordObject(root, "Neblina"); root.SetActive(activa); }
    }
}
