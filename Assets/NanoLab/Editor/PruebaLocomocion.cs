using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prueba de la locomoción de Meta en play mode (también en batchmode): ejecuta DebugLocomocion
// y guarda el informe en Logs/prueba_locomocion.txt. En batchmode sale con 0/1.
[InitializeOnLoad]
public static class PruebaLocomocion
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Clave = "NanoLab.PruebaLocomocion";
    const string ClaveT0 = "NanoLab.PruebaLocomocion.t0";

    static PruebaLocomocion()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update += Vigilar;
    }

    [MenuItem("NanoLab/Probar locomoción (play mode)", false, 5)]
    public static void Ejecutar()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(Clave, true);
        SessionState.SetFloat(ClaveT0, (float)EditorApplication.timeSinceStartup);
        EditorApplication.playModeStateChanged -= Cambio;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update -= Vigilar;
        EditorApplication.update += Vigilar;
        EditorApplication.EnterPlaymode();
    }

    static void Cambio(PlayModeStateChange c)
    {
        if (c != PlayModeStateChange.EnteredPlayMode) return;
        var d = new GameObject("PruebaLocomocion").AddComponent<DebugLocomocion>();
        d.StartCoroutine(d.Simular());
    }

    static void Vigilar()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        bool tiempo = EditorApplication.timeSinceStartup - SessionState.GetFloat(ClaveT0, 0f) > 180f;
        if (!EditorApplication.isPlaying && !tiempo) return;
        if (!DebugLocomocion.Terminado && !tiempo) return;
        bool ok = DebugLocomocion.Terminado && DebugLocomocion.Correcto;
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "prueba_locomocion.txt"),
            (ok ? "RESULTADO: CORRECTO\n" : "RESULTADO: CON FALLOS\n") + (DebugLocomocion.Informe ?? "TIEMPO AGOTADO"));
        SessionState.SetBool(Clave, false);
        EditorApplication.update -= Vigilar;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
