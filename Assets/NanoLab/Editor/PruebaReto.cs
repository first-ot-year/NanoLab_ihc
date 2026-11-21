using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ML = MontarLaboratorio;

// Prueba de punta a punta de las MISIONES 1 y 2 en play mode (también en batchmode):
// entra en play, ejecuta DebugReto.Simular (falla una vez y luego acierta), guarda capturas del HUD
// y de la cámara en Logs/Capturas y el informe en Logs/prueba_reto.txt. En batchmode sale con 0/1.
[InitializeOnLoad]
public static class PruebaReto
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Clave = "NanoLab.PruebaReto";
    const string ClaveT0 = "NanoLab.PruebaReto.t0";
    const float Limite = 240f;
    const string ClaveCorto = "NanoLab.PruebaReto.corto";   // recorrido corto (Misiones.ModoCorto)

    static PruebaReto()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update += Vigilar;
    }

    [MenuItem("NanoLab/Probar misiones 1 y 2 · modo corto (play mode)", false, 4)]
    public static void EjecutarCorto() => Lanzar(true);

    [MenuItem("NanoLab/Probar misiones 1 y 2 (play mode)", false, 4)]
    public static void Ejecutar() => Lanzar(false);

    static void Lanzar(bool corto)
    {
        SessionState.SetBool(ClaveCorto, corto);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(Clave, true);
        SessionState.SetFloat(ClaveT0, (float)EditorApplication.timeSinceStartup);
        EditorApplication.playModeStateChanged -= Cambio;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update -= Vigilar;
        EditorApplication.update += Vigilar;
        PartidaPruebas.Usar();   // partida aparte y vacía: la prueba no carga ni pisa la partida guardada
        EditorApplication.EnterPlaymode();
    }

    static void Cambio(PlayModeStateChange c)
    {
        if (c != PlayModeStateChange.EnteredPlayMode) return;
        var d = new GameObject("PruebaReto").AddComponent<DebugReto>();
        DebugReto.Momento -= Capturar;
        DebugReto.Momento += Capturar;
        d.StartCoroutine(SessionState.GetBool(ClaveCorto, false) ? d.SimularCorto() : d.Simular(true));
    }

    static void Vigilar()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        bool tiempo = EditorApplication.timeSinceStartup - SessionState.GetFloat(ClaveT0, 0f) > Limite;
        if (!EditorApplication.isPlaying && !tiempo) return;
        if (!DebugReto.Terminado && !tiempo) return;

        string informe = DebugReto.Terminado ? DebugReto.Informe : "TIEMPO AGOTADO: la simulación no terminó.\n" + DebugReto.Informe;
        bool ok = DebugReto.Terminado && DebugReto.Correcto;
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Logs", SessionState.GetBool(ClaveCorto, false) ? "prueba_reto_corto.txt" : "prueba_reto.txt"), (ok ? "RESULTADO: CORRECTO\n" : "RESULTADO: CON FALLOS\n") + informe);
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
            else if (nombre.StartsWith("terminal"))
            {
                // De pie frente a la terminal (5): consola con las dos filas y la pantalla.
                var v = RecorridoCentro.Vista("5_Terminal", new Vector3(20.5f, 0f, 16.55f), 180f, new Vector3(20.5f, 1.62f, 15.4f), new Vector3(0f, -0.27f, 1f));
                ML.Capturar(dir, nombre, v.desde, v.dir);
            }
            else if (nombre.StartsWith("prensa"))
            {
                // Frente a la prensa (6): pantalla, viales y el tablero de agentes detrás.
                var v = RecorridoCentro.Vista("6_Prensa", new Vector3(15.5f, 0f, 15.86f), 180f, new Vector3(15.25f, 1.6f, 14.75f), new Vector3(0f, -0.22f, 1f));
                ML.Capturar(dir, nombre, v.desde, v.dir);
                v = RecorridoCentro.Vista("6_Prensa", new Vector3(15.5f, 0f, 15.86f), 180f, new Vector3(14.95f, 1.42f, 15.6f), new Vector3(0f, -0.3f, 1f));
                ML.Capturar(dir, nombre + "_pantalla", v.desde, v.dir);
            }
            else if (nombre == "recuerdo_baymax")
            {
                // Indicador anclado a la vista: lo que ve el jugador (desde sus ojos, mirando al frente).
                var ojo = ManosRig.Cabeza;
                if (ojo == null) return;
                ML.Capturar(dir, nombre, ojo.position, ojo.forward);
            }
            else if (nombre == "tincion_lupa")
            {
                // Holograma junto a lo que lo produjo (lupa de la tinción o recuerdo de Baymax), visto de frente.
                string n = nombre == "tincion_lupa" ? "LupaTincion" : "RecuerdoBaymax";
                var l = Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == n && x.gameObject.activeInHierarchy);
                if (l == null) return;
                float d = nombre == "tincion_lupa" ? 0.45f : 1.6f;
                var desde = l.position - l.forward * d + Vector3.up * 0.04f;
                ML.Capturar(dir, nombre, desde, l.position - desde);
            }
            else if (nombre.StartsWith("revision_"))
            {
                // Revisión del playtest: insignia vista por detrás, espejos, tótem de bienvenida y manos.
                var todos = Object.FindObjectsOfType<Transform>();
                Transform T(string n) => todos.FirstOrDefault(x => x.name == n && x.gameObject.activeInHierarchy);
                if (nombre == "revision_insignia")
                {
                    var ins = T("Insignia");
                    if (ins == null) return;
                    var c = ins.position + Vector3.up * 1.2f;
                    var f = Vector3.ProjectOnPlane(ins.forward, Vector3.up).normalized;
                    ML.Capturar(dir, nombre + "_lado1", c + f * 1.3f, -f);
                    ML.Capturar(dir, nombre + "_lado2", c - f * 1.3f, f);
                }
                else if (nombre == "revision_entrada")
                {
                    var desde = new Vector3(11.2f, 1.7f, 6.0f);
                    ML.Capturar(dir, nombre, desde, new Vector3(7.2f, 1.4f, 7.0f) - desde);
                    var esp = T("EspejoCentro");
                    if (esp != null) { var d2 = new Vector3(10.0f, 1.7f, 8.4f); ML.Capturar(dir, "revision_espejo_centro", d2, esp.position + Vector3.up * 1.1f - d2); }
                }
                else if (nombre == "revision_recibidor")
                {
                    var esp = T("EspejoRecibidor");
                    var spawn = new Vector3(-3.3f, 1.6f, 2.24f);
                    if (esp != null) ML.Capturar(dir, nombre, spawn, esp.position + Vector3.up * 1.1f - spawn);
                }
                else if (nombre == "revision_manos")
                {
                    var m = Object.FindObjectsOfType<SkinnedMeshRenderer>().FirstOrDefault(r => r.name == "Manos");
                    if (m == null) return;
                    var c = m.bounds.center;
                    var cab = ManosRig.Cabeza;
                    var f = cab != null ? Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized : Vector3.forward;
                    var desde = c + f * 0.55f + Vector3.up * 0.15f;
                    ML.Capturar(dir, nombre, desde, c - desde);
                }
            }
            else if (nombre == "presencia_brazalete")
            {
                var b = Object.FindObjectsOfType<Transform>().FirstOrDefault(x => x.name == "BrazaleteBaymax");
                if (b == null) return;
                var desde = b.position - b.forward * 0.32f + b.up * 0.04f;
                ML.Capturar(dir, nombre, desde, b.position - desde);
            }
            else if (nombre == "presencia_aviso")
            {
                var a = Object.FindObjectsOfType<Aviso.Item>().Where(x => x.Activo).OrderByDescending(x => x.Inicio).FirstOrDefault();
                if (a == null) return;
                var f = a.transform.forward; f.y = 0f; f.Normalize();
                var desde = a.Ancla - f * 0.7f + Vector3.up * 0.25f;
                ML.Capturar(dir, nombre, desde, a.transform.position - desde);
            }
            else if (nombre.StartsWith("camara"))
            {
                float y = 1.75f;
                var v = RecorridoCentro.Vista("7_CamaraExperimental", new Vector3(15.5f, 0f, 8.5f), -90f, new Vector3(12.9f, y, 8.5f), new Vector3(1f, -0.12f, 0f));
                ML.Capturar(dir, nombre, v.desde, v.dir);
            }
            else
            {
                var v = RecorridoCentro.Vista("8_Archivo", new Vector3(10.2f, 0f, 16.6f), 180f, new Vector3(10.2f, 1.75f, 14.2f), new Vector3(0f, -0.02f, 1f));
                ML.Capturar(dir, nombre, v.desde, v.dir);
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
