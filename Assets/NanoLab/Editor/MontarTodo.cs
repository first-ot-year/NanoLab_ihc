using UnityEditor;
using UnityEngine;

// Monta el proyecto completo en el orden correcto:
// modelos convertidos -> fichas de muestras -> sala de Química -> sala de Microbiología.
// (Microbiología va al final porque también añade el portal dentro de Química.)
public static class MontarTodo
{
    [MenuItem("NanoLab/Montar TODO (Química + Microbiología)", false, 0)]
    public static void Montar()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Montar todo",
                "Se van a regenerar desde cero las escenas Laboratorio (Química) y Microbiología, " +
                "incluido el horneado de la luz. Se perderán los cambios hechos a mano en ellas y puede tardar bastante.\n\n¿Continuar?",
                "Montar todo", "Cancelar"))
            return;

        int paso = 0;
        try
        {
            Paso(paso = 1, "Convirtiendo modelos de Sketchfab");
            ConvertirSketchfab.Convertir();

            Paso(paso = 2, "Convirtiendo virus del NIH");
            ConvertirSketchfab.ConvertirVirus();

            Paso(paso = 3, "Creando fichas de muestras");
            CrearMuestras.Crear();

            Paso(paso = 4, "Montando la sala de Química");
            MontarLaboratorio.Montar();

            Paso(paso = 5, "Montando la sala de Microbiología");
            MontarMicrobiologia.Montar();
        }
        catch (System.Exception e)
        {
            // Sin esto la barra de progreso se quedaba abierta y parecía que seguía montando.
            EditorUtility.ClearProgressBar();
            Debug.LogException(e);
            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Montar todo", $"Falló el paso {paso}/5:\n{e.Message}\n\nLa escena de Microbiología quedó sin montar (se reemplaza al empezar). Vuelve a ejecutar Montar TODO o recupérala con: git restore Assets/NanoLab/Scenes/Microbiologia.unity\n\nEl detalle está en la consola (Console).", "OK");
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/NanoLab/Scenes/Microbiologia.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            return;
        }

        EditorUtility.ClearProgressBar();
        // Las capturas dejan la escena abierta con cámaras de prueba: se reabre tal como quedó guardada.
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/NanoLab/Scenes/Microbiologia.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        Debug.Log("[Todo] Proyecto montado: Laboratorio.unity y Microbiologia.unity listos.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Montar todo", "Listo. Abre Assets/NanoLab/Scenes/Microbiologia.unity o Laboratorio.unity.", "OK");
    }

    // Descarta los cambios sin guardar de la escena abierta (p. ej. el techo apagado por una captura)
    // y abre Microbiologia.unity tal como está guardada.
    [MenuItem("NanoLab/Recargar Microbiología (sin guardar)", false, 1)]
    public static void RecargarMicrobiologia()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Todo] Sal del modo Play antes de recargar la escena."); return; }
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/NanoLab/Scenes/Microbiologia.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        Debug.Log("[Todo] Microbiologia.unity recargada desde el disco.");
    }

    static void Paso(int n, string texto)
    {
        Debug.Log($"[Todo] Paso {n}/5: {texto}...");
        if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Montar todo", texto, n / 5f);
    }
}
