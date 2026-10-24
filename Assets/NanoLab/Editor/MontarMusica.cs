using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Añade la música ambiental a las escenas y configura su importación para Quest
// (Vorbis comprimido y en streaming: apenas ocupa memoria).
public static class MontarMusica
{
    const string Ruta = "Assets/NanoLab/Audio/AmbienteLaboratorio.wav";
    static readonly string[] Escenas = { "Assets/NanoLab/Scenes/Microbiologia.unity", "Assets/NanoLab/Scenes/Laboratorio.unity" };

    [MenuItem("NanoLab/Añadir música ambiental", false, 9)]
    public static void Anadir()
    {
        foreach (var e in Escenas)
        {
            if (!System.IO.File.Exists(e)) continue;
            var scene = EditorSceneManager.OpenScene(e, OpenSceneMode.Single);
            Agregar();
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[Musica] Música ambiental añadida a las escenas.");
    }

    // En la escena abierta (la llama también Montar microbiología).
    public static void Agregar()
    {
        var clip = Importar();
        foreach (var vieja in Object.FindObjectsOfType<MusicaAmbiente>(true).ToArray()) Object.DestroyImmediate(vieja.gameObject);
        var go = new GameObject("MusicaAmbiente");
        go.AddComponent<AudioSource>().playOnAwake = false;
        go.AddComponent<MusicaAmbiente>().musica = clip;
    }

    static AudioClip Importar()
    {
        if (AssetImporter.GetAtPath(Ruta) is AudioImporter ai)
        {
            var s = ai.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.5f;
            s.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            ai.defaultSampleSettings = s;
            ai.loadInBackground = true;
            ai.SaveAndReimport();
        }
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Ruta);
        if (clip == null) Debug.LogWarning($"[Musica] No se encontró {Ruta}.");
        return clip;
    }
}
