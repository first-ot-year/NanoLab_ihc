using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ML = MontarLaboratorio;

// Prueba de punta a punta del CASO 01 · Crea la vacuna (LABORATORIO DE VACUNAS, único reto del centro) en play mode, sin visor:
// entra en play, espera a que se monte la estación y ejecuta RetoVacuna.Simular (en cada paso comete un error típico
// con las mismas acciones que haría el estudiante —encajar piezas, pipetear, pulsar— y luego lo resuelve). Guarda
// capturas en Logs/Capturas/vacuna_*.png (vista del estudiante y vista de la mesa) y el informe en
// Logs/prueba_vacuna.txt. En batchmode sale con 0/1.
[InitializeOnLoad]
public static class PruebaVacuna
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Clave = "NanoLab.PruebaVacuna";
    const string ClaveT0 = "NanoLab.PruebaVacuna.t0";
    const float Limite = 240f;
    static bool lanzada;

    static PruebaVacuna()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        EditorApplication.update += Vigilar;
    }

    [MenuItem("NanoLab/Probar reto de vacuna (play mode)", false, 5)]
    public static void Ejecutar()
    {
        // Si la escena ya está abierta no se recarga (así no se pierden cambios sin guardar).
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        PartidaPruebas.Usar();   // partida de prueba en Logs/: no pisa la partida real del Editor
        SessionState.SetBool(Clave, true);
        SessionState.SetFloat(ClaveT0, (float)EditorApplication.timeSinceStartup);
        lanzada = false;
        EditorApplication.update -= Vigilar;
        EditorApplication.update += Vigilar;
        EditorApplication.EnterPlaymode();
    }

    static void Vigilar()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        bool tiempo = EditorApplication.timeSinceStartup - SessionState.GetFloat(ClaveT0, 0f) > Limite;

        // La estación se monta en el Start de AdaptadorEscena: se espera a que exista.
        if (EditorApplication.isPlaying && !lanzada && RetoVacuna.Instancia != null && Time.frameCount > 5)
        {
            lanzada = true;
            RetoVacuna.Momento -= Capturar;
            RetoVacuna.Momento += Capturar;
            RetoVacuna.Instancia.StartCoroutine(RetoVacuna.Instancia.Simular());
        }
        if (!tiempo && (!EditorApplication.isPlaying || !RetoVacuna.PruebaTerminada)) return;

        bool ok = RetoVacuna.PruebaTerminada && RetoVacuna.PruebaCorrecta;
        string informe = RetoVacuna.PruebaTerminada ? RetoVacuna.Informe
            : RetoVacuna.Instancia == null ? "TIEMPO AGOTADO: no se montó la estación de la vacuna (¿falta el RetoManager del centro?)."
            : "TIEMPO AGOTADO: la simulación no terminó.\n" + RetoVacuna.Informe;
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Logs", "prueba_vacuna.txt"), (ok ? "RESULTADO: CORRECTO\n" : "RESULTADO: CON FALLOS\n") + informe);
        Debug.Log($"[PruebaVacuna] Terminado. Correcto: {ok}. Informe en Logs/prueba_vacuna.txt");
        SessionState.SetBool(Clave, false);
        EditorApplication.update -= Vigilar;
        RetoVacuna.Momento -= Capturar;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    // Vista del estudiante delante del mostrador (a 1,5 m, a la altura de los ojos) y vista de la mesa desde arriba.
    static void Capturar(string nombre)
    {
        var r = RetoVacuna.Instancia;
        if (r == null) return;
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        try
        {
            var t = r.transform;
            ML.Capturar(dir, nombre, t.TransformPoint(new Vector3(0f, 1.6f, 1.55f)), t.TransformDirection(new Vector3(0f, -0.08f, -1f)));
            ML.Capturar(dir, nombre + "_mesa", t.TransformPoint(new Vector3(0f, 1.75f, 0.7f)), t.TransformDirection(new Vector3(0f, -0.85f, -1f)));
            // Al empezar, vista general del centro desde la entrada (comprueba que el reto anterior está oculto).
            if (nombre == "vacuna_0_intro")
                ML.Capturar(dir, "vacuna_0_centro", t.TransformPoint(new Vector3(0f, 3.4f, 6.5f)), t.TransformDirection(new Vector3(0f, -0.45f, -1f)));
        }
        catch (System.Exception e) { Debug.LogWarning($"[PruebaVacuna] No se pudo capturar {nombre}: {e.Message}"); }
    }
}
