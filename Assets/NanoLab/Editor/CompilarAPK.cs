using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Compila el APK para Meta Quest (Builds/NanoLab.apk) con las escenas en el orden correcto:
// Microbiología primero (la introducción de E2 empieza ahí) y Química después (portal).
// No cambia la lista de escenas del proyecto.
public static class CompilarAPK
{
    static readonly string[] Escenas =
    {
        "Assets/NanoLab/Scenes/Microbiologia.unity",
        "Assets/NanoLab/Scenes/Laboratorio.unity",
    };
    const string Salida = "Builds/NanoLab.apk";

    [MenuItem("NanoLab/Compilar APK para Quest", false, 50)]
    public static void Compilar()
    {
        var faltan = Escenas.Where(e => !File.Exists(e)).ToArray();
        if (faltan.Length > 0) { Debug.LogError("[APK] Faltan escenas: " + string.Join(", ", faltan)); return; }
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[APK] No se pudo cambiar la plataforma a Android (¿está instalado Android Build Support?).");
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Salida));
        var opciones = new BuildPlayerOptions
        {
            scenes = Escenas,
            locationPathName = Salida,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };
        EditorUserBuildSettings.buildAppBundle = false;   // .apk, no .aab
        Debug.Log("[APK] Compilando para Quest... (la primera vez tarda varios minutos)");
        var r = BuildPipeline.BuildPlayer(opciones);
        var s = r.summary;
        if (s.result == BuildResult.Succeeded)
        {
            Debug.Log($"[APK] Listo: {Path.GetFullPath(Salida)} ({s.totalSize / (1024f * 1024f):F0} MB, {s.totalTime.TotalMinutes:F1} min).");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(Salida);
        }
        else Debug.LogError($"[APK] La compilación terminó con {s.result}: {s.totalErrors} error(es). Revisa la consola.");
    }
}
