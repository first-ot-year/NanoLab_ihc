using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ML = MontarLaboratorio;

// Prueba de punta a punta del Caso 01 en play mode (también en batchmode):
// entra en play, ejecuta DebugReto.Simular (falla una vez y luego acierta), guarda capturas del HUD
// y de la cámara en Logs/Capturas y el informe en Logs/prueba_reto.txt. En batchmode sale con 0/1.
[InitializeOnLoad]
public static class PruebaReto
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Clave = "NanoLab.PruebaReto";
    const string ClaveT0 = "NanoLab.PruebaReto.t0";
    const float Limite = 240f;

    static PruebaReto()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update += Vigilar;
    }

    [MenuItem("NanoLab/Probar reto completo (play mode)", false, 4)]
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
        var d = new GameObject("PruebaReto").AddComponent<DebugReto>();
        DebugReto.Momento -= Capturar;
        DebugReto.Momento += Capturar;
        d.StartCoroutine(d.Simular(true));
    }

    static void Vigilar()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        bool tiempo = EditorApplication.timeSinceStartup - SessionState.GetFloat(ClaveT0, 0f) > Limite;
        if (!EditorApplication.isPlaying && !tiempo) return;
        if (!DebugReto.Terminado && !tiempo) return;

        string informe = DebugReto.Terminado ? DebugReto.Informe : "TIEMPO AGOTADO: la simulación no terminó.\n" + DebugReto.Informe;
        bool ok = DebugReto.Terminado && DebugReto.Correcto;
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "prueba_reto.txt"), (ok ? "RESULTADO: CORRECTO\n" : "RESULTADO: CON FALLOS\n") + informe);
        Debug.Log($"[PruebaReto] Terminado. Correcto: {ok}");
        SessionState.SetBool(Clave, false);
        EditorApplication.update -= Vigilar;
        DebugReto.Momento -= Capturar;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    static void Capturar(string nombre)
    {
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        try
        {
            if (nombre.StartsWith("hud"))
            {
                var hud = HUDMicroscopio.Instancia;
                var ojo = ManosRig.Cabeza;
                if (hud == null || ojo == null) return;
                var g = hud.GetComponent<CanvasGroup>();
                if (g != null) g.alpha = 1f;
                Canvas.ForceUpdateCanvases();
                var camGo = new GameObject("CamPrueba");
                var cam = camGo.AddComponent<Camera>();
                cam.transform.SetParent(ojo, false);
                cam.fieldOfView = 68f; cam.nearClipPlane = 0.05f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                cam.cullingMask &= ~(1 << VisorVirus.Capa);
                Render(cam, Path.Combine(dir, nombre + ".png"), 1600, 800);
                Object.DestroyImmediate(camGo);
            }
            else if (nombre.StartsWith("camara"))
            {
                float y = 1.75f;
                ML.Capturar(dir, nombre, new Vector3(12.9f, y, 8.5f), new Vector3(1f, -0.12f, 0f));
            }
            else
            {
                ML.Capturar(dir, nombre, new Vector3(10.2f, 1.75f, 14.2f), new Vector3(0f, -0.02f, 1f));
            }
        }
        catch (System.Exception e) { Debug.LogWarning($"[PruebaReto] No se pudo capturar {nombre}: {e.Message}"); }
    }

    static void Render(Camera cam, string path, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt; cam.aspect = (float)w / h;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
