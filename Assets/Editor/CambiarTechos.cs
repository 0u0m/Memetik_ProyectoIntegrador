using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Menu: Tools > Techos > ...
// Cambia entre los techos originales (objetos "Techo") y los nuevos de dos aguas ("Techo_Nuevo").
// No borra nada: solo activa/desactiva.
public static class CambiarTechos
{
    [MenuItem("Tools/Techos/Usar techos originales")]
    public static void Originales() { Cambiar(false); }

    [MenuItem("Tools/Techos/Usar techos nuevos")]
    public static void Nuevos() { Cambiar(true); }

    static void Cambiar(bool usarNuevos)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name != "Pueblo" && root.name != "Pueblo_B") continue;
            foreach (Transform casa in root.transform)
            {
                var nuevo = casa.Find("Techo_Nuevo");
                if (nuevo == null) continue; // casas sin techo nuevo se quedan como estan
                Undo.RecordObject(nuevo.gameObject, "Cambiar techos");
                nuevo.gameObject.SetActive(usarNuevos);
                foreach (Transform c in casa)
                {
                    if (c.name == "Techo_Nuevo" || !c.name.StartsWith("Techo")) continue;
                    Undo.RecordObject(c.gameObject, "Cambiar techos");
                    c.gameObject.SetActive(!usarNuevos);
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
