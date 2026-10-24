using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MM = MontarMicrobiologia;

// Aplica en la escena las mejoras visuales del microscopio y de la estación de análisis 3D
// (leyenda de composición, materiales del holograma, resplandor, plataforma holográfica)
// sin regenerar la sala ni hornear la luz, y genera las capturas para revisarlas.
public static class MejorasMicroscopio
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";

    [MenuItem("NanoLab/Actualizar microscopio y estación 3D (sin hornear)", false, 10)]
    public static void Actualizar()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CrearMuestras.Todas();   // componentes de cada virus para la leyenda
        AssetDatabase.SaveAssets();
        MM.CrearMateriales();
        int n = 0;
        foreach (var est in Object.FindObjectsOfType<EstacionAnalisis3D>(true))
        {
            foreach (var vieja in est.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name == "Plataforma").ToArray())
                Object.DestroyImmediate(vieja.gameObject);
            est.plataforma = MM.PlataformaHolo(est.transform);
            EditorUtility.SetDirty(est);
            n++;
        }
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Microscopio] Mejoras aplicadas ({n} estación(es) 3D). Generando capturas...");
        MM.ProbarMicroscopio();   // Logs/Capturas/hud_*.png y holo_estacion.png
    }
}
