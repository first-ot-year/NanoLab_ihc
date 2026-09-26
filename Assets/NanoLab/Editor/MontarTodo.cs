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

        Paso(1, "Convirtiendo modelos de Sketchfab");
        ConvertirSketchfab.Convertir();

        Paso(2, "Convirtiendo virus del NIH");
        ConvertirSketchfab.ConvertirVirus();

        Paso(3, "Creando fichas de muestras");
        CrearMuestras.Crear();

        Paso(4, "Montando la sala de Química");
        MontarLaboratorio.Montar();

        Paso(5, "Montando la sala de Microbiología");
        MontarMicrobiologia.Montar();

        EditorUtility.ClearProgressBar();
        Debug.Log("[Todo] Proyecto montado: Laboratorio.unity y Microbiologia.unity listos.");
        if (!Application.isBatchMode)
            EditorUtility.DisplayDialog("Montar todo", "Listo. Abre Assets/NanoLab/Scenes/Microbiologia.unity o Laboratorio.unity.", "OK");
    }

    static void Paso(int n, string texto)
    {
        Debug.Log($"[Todo] Paso {n}/5: {texto}...");
        if (!Application.isBatchMode) EditorUtility.DisplayProgressBar("Montar todo", texto, n / 5f);
    }
}
