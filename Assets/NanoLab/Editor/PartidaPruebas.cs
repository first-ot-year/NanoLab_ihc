using System.IO;
using UnityEditor;
using UnityEngine;

// Partida guardada desde el Editor: en Play se usa la misma carpeta que una build de PC
// (Application.persistentDataPath), así que una partida guardada al probar se carga en la siguiente.
//  - Menús para borrarla o abrir su carpeta.
//  - Las pruebas automáticas usan un archivo aparte (Logs/partida_prueba.json), vacío al empezar:
//    no dependen de lo que haya guardado quien prueba ni lo pisan.
[InitializeOnLoad]
public static class PartidaPruebas
{
    static string RutaPrueba => Path.Combine(Directory.GetCurrentDirectory(), "Logs", "partida_prueba.json");
    static string RutaReal => Path.Combine(Application.persistentDataPath, "partida.json");

    static PartidaPruebas()
    {
        // Al volver al modo edición se deja de usar el archivo de prueba (en batchmode el proceso acaba antes).
        EditorApplication.playModeStateChanged += c =>
        {
            if (c == PlayModeStateChange.EnteredEditMode) SessionState.EraseString(Partida.ClaveRutaPrueba);
        };
    }

    // Lo llaman PruebaReto y PruebaLocomocion antes de entrar en Play.
    public static void Usar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaPrueba));
        foreach (var r in new[] { RutaPrueba, RutaPrueba + ".bak", RutaPrueba + ".tmp", Path.ChangeExtension(RutaPrueba, ".corrupta.json") })
            if (File.Exists(r)) File.Delete(r);
        SessionState.SetString(Partida.ClaveRutaPrueba, RutaPrueba);
        Partida.Olvidar();
    }

    [MenuItem("NanoLab/Partida/Borrar partida guardada (Editor)", false, 40)]
    public static void Borrar()
    {
        int n = 0;
        foreach (var r in new[] { RutaReal, RutaReal + ".bak", RutaReal + ".tmp", Path.ChangeExtension(RutaReal, ".corrupta.json") })
            if (File.Exists(r)) { File.Delete(r); n++; }
        Partida.Olvidar();
        Debug.Log($"[Partida] {(n > 0 ? $"Borrados {n} archivos de partida" : "No había partida guardada")} en {Application.persistentDataPath}.");
    }

    [MenuItem("NanoLab/Partida/Abrir carpeta de la partida", false, 41)]
    public static void AbrirCarpeta()
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        EditorUtility.RevealInFinder(File.Exists(RutaReal) ? RutaReal : Application.persistentDataPath);
    }
}
