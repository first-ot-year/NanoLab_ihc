using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ML = MontarLaboratorio;

// Capturas del recorrido completo, paso a paso (Logs/Capturas/PasoAPaso/NN_nombre.png + indice.txt):
// entra en play, recorre las MISIONES 1 y 2 (DebugReto.Simular, con un error a propósito para ver las explicaciones)
// y después la MISIÓN 3 (RetoVacuna.Simular). En cada momento coloca una cámara frente a la estación que toca,
// buscando un punto libre de muebles con vista directa. No toca la partida guardada.
[InitializeOnLoad]
public static class CapturasPasoAPaso
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Clave = "NanoLab.CapturasPasoAPaso";
    const string ClaveT0 = "NanoLab.CapturasPasoAPaso.t0";
    const float Limite = 420f;

    static int n;
    static string dir;
    static readonly StringBuilder indice = new StringBuilder();
    static bool terminado;

    static CapturasPasoAPaso()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update += Vigilar;
    }

    [MenuItem("NanoLab/Capturas paso a paso (3 misiones)", false, 6)]
    public static void Ejecutar()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        SessionState.SetBool(Clave, true);
        SessionState.SetFloat(ClaveT0, (float)EditorApplication.timeSinceStartup);
        EditorApplication.playModeStateChanged -= Cambio;
        EditorApplication.playModeStateChanged += Cambio;
        EditorApplication.update -= Vigilar;
        EditorApplication.update += Vigilar;
        PartidaPruebas.Usar();   // partida aparte y vacía
        EditorApplication.EnterPlaymode();
    }

    static void Cambio(PlayModeStateChange c)
    {
        if (c != PlayModeStateChange.EnteredPlayMode) return;
        dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas", "PasoAPaso");
        Directory.CreateDirectory(dir);
        foreach (var f in Directory.GetFiles(dir, "*.png")) { try { File.Delete(f); } catch { } }
        n = 0; indice.Clear(); terminado = false;
        var d = new GameObject("CapturasPasoAPaso").AddComponent<DebugReto>();
        d.StartCoroutine(Recorrido(d));
    }

    static IEnumerator Recorrido(DebugReto d)
    {
        DebugReto.SinPruebaPartida = true;
        DebugReto.Momento -= Mision;
        DebugReto.Momento += Mision;
        yield return null;
        yield return new WaitForSeconds(1f);
        Vista("00_centro_general");
        yield return d.Simular(true);
        DebugReto.Momento -= Mision;
        DebugReto.SinPruebaPartida = false;
        if (!DebugReto.Correcto) Debug.LogWarning("[Capturas] La simulación de las misiones 1 y 2 tuvo fallos: revisa Logs/Capturas/PasoAPaso/indice.txt");
        indice.AppendLine("--- MISIONES 1 y 2: " + (DebugReto.Correcto ? "CORRECTO" : "CON FALLOS"));
        try { File.WriteAllText(Path.Combine(dir, "informe_misiones.txt"), DebugReto.Informe ?? ""); } catch { }

        for (float t = 0f; t < 5f && RetoVacuna.Instancia == null; t += Time.deltaTime) yield return null;
        var rv = RetoVacuna.Instancia;
        if (rv != null)
        {
            RetoVacuna.Momento -= Vacuna;
            RetoVacuna.Momento += Vacuna;
            yield return rv.StartCoroutine(rv.Simular());
            RetoVacuna.Momento -= Vacuna;
            indice.AppendLine("--- MISIÓN 3: " + (RetoVacuna.PruebaCorrecta ? "CORRECTO" : "CON FALLOS"));
            try { File.WriteAllText(Path.Combine(dir, "informe_vacuna.txt"), RetoVacuna.Informe ?? ""); } catch { }
            yield return new WaitForSeconds(0.5f);
            Mision("indicador_final");
        }
        terminado = true;
    }

    static void Vigilar()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        bool tiempo = EditorApplication.timeSinceStartup - SessionState.GetFloat(ClaveT0, 0f) > Limite;
        if (!tiempo && (!EditorApplication.isPlaying || !terminado)) return;
        if (tiempo) indice.AppendLine("--- TIEMPO AGOTADO");
        try { File.WriteAllText(Path.Combine(dir ?? ".", "indice.txt"), indice.ToString()); } catch { }
        Debug.Log($"[Capturas] {n} capturas en Logs/Capturas/PasoAPaso");
        SessionState.SetBool(Clave, false);
        EditorApplication.update -= Vigilar;
        DebugReto.Momento -= Mision;
        RetoVacuna.Momento -= Vacuna;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    // ---------- Encuadre ----------

    static string Nombre(string nombre)
    {
        n++;
        string s = $"{n:00}_{nombre}";
        indice.AppendLine(s);
        return s;
    }

    // Los rótulos de área (BalizaArea) miran a la cabeza del jugador: para la foto, miran a la cámara.
    static void Capturar(string nombre, Vector3 pos, Vector3 forward)
    {
        foreach (var b in Object.FindObjectsOfType<BalizaArea>())
        {
            if (b.rotulo == null) continue;
            var d = b.rotulo.position - pos; d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) b.rotulo.rotation = Quaternion.LookRotation(d);
        }
        PrepararTextos();
        ML.Capturar(dir, Nombre(nombre), pos, forward);
    }

    static void PrepararTextos()
    {
        Canvas.ForceUpdateCanvases();
        foreach (var t in Object.FindObjectsOfType<TMP_Text>()) { try { t.ForceMeshUpdate(); } catch { } }
    }

    static Vector3 CentroSala()
    {
        if (RetoVacuna.Instancia != null) return RetoVacuna.Instancia.transform.position;
        var rm = RetoManager.Instancia;
        return rm != null ? rm.zona.center : Vector3.zero;
    }

    // Cámara a 'dist' del objetivo y 'alto' por encima, en la primera dirección libre (empieza mirando desde el centro de la sala).
    static void VistaLibre(string nombre, Vector3 objetivo, float dist = 1.5f, float alto = 0.45f, Vector3? preferida = null)
    {
        var pref = preferida ?? (CentroSala() - objetivo);
        pref.y = 0f;
        if (pref.sqrMagnitude < 1e-4f) pref = Vector3.forward;
        pref.Normalize();
        Vector3 cam = objetivo + pref * dist + Vector3.up * alto;
        float total = Mathf.Sqrt(dist * dist + alto * alto);
        for (int k = 0; k < 16; k++)
        {
            float ang = (k % 2 == 0 ? 1f : -1f) * ((k + 1) / 2) * 22.5f;
            var p = objetivo + Quaternion.Euler(0f, ang, 0f) * pref * dist + Vector3.up * alto;
            if (Physics.CheckSphere(p, 0.15f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (Physics.Linecast(p, objetivo, out var hit, ~0, QueryTriggerInteraction.Ignore) && hit.distance < total * 0.8f) continue;
            cam = p;
            break;
        }
        Capturar(nombre, cam, objetivo - cam);
    }

    static void Vista(string nombre)
    {
        var rv = RetoVacuna.Instancia;
        if (rv == null) return;
        var t = rv.transform;
        Capturar(nombre, t.TransformPoint(new Vector3(0f, 3.4f, 6.5f)), t.TransformDirection(new Vector3(0f, -0.45f, -1f)));
    }

    // Cara legible de un panel: la de ±forward que da hacia el centro de la sala.
    static Vector3 Frente(Transform t)
    {
        var f = t.forward; f.y = 0f;
        var c = CentroSala() - t.position; c.y = 0f;
        return Vector3.Dot(f, c) >= 0f ? f : -f;
    }

    static Vector3 Medio(params Component[] cs)
    {
        var v = cs.Where(c => c != null).Select(c => c.transform.position).ToArray();
        return v.Length == 0 ? CentroSala() : v.Aggregate(Vector3.zero, (a, b) => a + b) / v.Length;
    }

    // Lo que ve el estudiante por el microscopio (HUD delante de los ojos).
    static void Hud(string nombre)
    {
        var hud = HUDMicroscopio.Instancia;
        if (hud == null || !hud.gameObject.activeInHierarchy)
        {
            var micro = Object.FindObjectsOfType<MicroscopioOptico>().FirstOrDefault(m => m.modoReto);
            if (micro != null) micro.ObservarAhora();
            hud = HUDMicroscopio.Instancia;
        }
        var ojo = ManosRig.Cabeza;
        if (hud == null || ojo == null) return;
        var g = hud.GetComponent<CanvasGroup>();
        if (g != null) g.alpha = 1f;
        PrepararTextos();
        var camGo = new GameObject("CamCaptura");
        var cam = camGo.AddComponent<Camera>();
        cam.transform.SetParent(ojo, false);
        cam.fieldOfView = 68f; cam.nearClipPlane = 0.05f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        cam.cullingMask &= ~(1 << VisorVirus.Capa);
        const int w = 1600, h = 800;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt; cam.aspect = (float)w / h;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, Nombre(nombre) + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null; cam.targetTexture = null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(camGo);
    }

    static void Mision(string nombre)
    {
        if (dir == null) return;
        try
        {
            var rm = RetoManager.Instancia;
            if (nombre.StartsWith("hud")) { Hud(nombre); return; }
            if (rm == null) return;
            var fuentes = Object.FindObjectsOfType<FuenteMuestra>();
            if (nombre.StartsWith("inicio_banco") || nombre.StartsWith("banco"))
            {
                var b = fuentes.FirstOrDefault(f => f.muestra == rm.muestraHongo);
                var c = fuentes.FirstOrDefault(f => f.muestra == rm.muestraVirus);
                VistaLibre(nombre, Medio(rm.placa, b, c) + Vector3.up * 0.05f, 1.3f, 0.55f);
            }
            else if (nombre.StartsWith("tincion"))
            {
                var goteros = fuentes.Where(f => f.reactivo >= 1 && f.reactivo <= 4).Cast<Component>().ToArray();
                VistaLibre(nombre, Medio(goteros), 1.2f, 0.55f);
            }
            else if (nombre.StartsWith("conclusiones"))
            {
                var p = rm.conclusiones.transform;
                var cam = p.TransformPoint(new Vector3(0f, 1.6f, 1.3f));
                Capturar(nombre, cam, p.TransformPoint(new Vector3(0f, 1.15f, 0f)) - cam);
            }
            else if (nombre.StartsWith("biblioteca"))
            {
                var ficha = rm.fichas.FirstOrDefault(f => f.Abierta) ?? rm.fichas.FirstOrDefault();
                if (ficha == null) return;
                var hacia = ficha.boton != null ? ficha.boton.transform.position - ficha.transform.position : -ficha.transform.forward;
                hacia.y = 0f;
                var cam = ficha.transform.position + hacia.normalized * 1.25f - Vector3.up * 0.1f;
                Capturar(nombre, cam, ficha.transform.position - cam);
            }
            else if (nombre.StartsWith("terminal"))
                Terminal(nombre);   // de pie frente a la terminal (5): consola con las dos filas y la pantalla
            else if (nombre.StartsWith("prensa"))
                Capturar(nombre, new Vector3(15.25f, 1.6f, 14.75f), new Vector3(0f, -0.22f, 1f));
            else if (nombre.StartsWith("camara"))
            {
                var c = rm.camara;
                var obj = c.zonaPlaca != null ? c.zonaPlaca.transform.position : c.transform.position + Vector3.up * 0.3f;
                var fuera = obj - c.transform.position; fuera.y = 0f;
                VistaLibre(nombre, obj, 1.5f, 0.55f, fuera.sqrMagnitude > 1e-4f ? fuera : (Vector3?)null);
            }
            else if (nombre.StartsWith("archivo"))
                VistaLibre(nombre, rm.resumen.transform.position, 1.9f, 0.1f, Frente(rm.resumen.transform));
            else if (nombre.StartsWith("mision") || nombre.StartsWith("indicador"))
            {
                if (nombre.StartsWith("mision")) Terminal(nombre + "_aviso");
                var ind = Object.FindObjectOfType<IndicadorRecuerdos>();
                if (ind != null)
                {
                    var t = ind.texto != null ? ind.texto.transform : ind.transform;
                    var cam = t.position - t.forward * 0.35f + Vector3.up * 0.04f;
                    Capturar(nombre + "_recuerdos", cam, t.position - cam);
                }
            }
            else VistaLibre(nombre, CentroSala(), 4f, 2f);
        }
        catch (System.Exception e) { Debug.LogWarning($"[Capturas] No se pudo capturar {nombre}: {e.Message}"); }
    }

    static void Terminal(string nombre) => Capturar(nombre, new Vector3(20.5f, 1.62f, 15.4f), new Vector3(0f, -0.27f, 1f));

    // MISIÓN 3: vista del estudiante delante del mostrador y, en los pasos de manipulación, la mesa desde arriba.
    static void Vacuna(string nombre)
    {
        var r = RetoVacuna.Instancia;
        if (r == null || dir == null) return;
        try
        {
            var t = r.transform;
            Capturar(nombre, t.TransformPoint(new Vector3(0f, 1.6f, 1.55f)), t.TransformDirection(new Vector3(0f, -0.08f, -1f)));
            if (nombre.EndsWith("_b") && !nombre.Contains("antigeno") && !nombre.Contains("fases") && !nombre.Contains("eficacia"))
                Capturar(nombre + "_mesa", t.TransformPoint(new Vector3(0f, 1.75f, 0.7f)), t.TransformDirection(new Vector3(0f, -0.85f, -1f)));
        }
        catch (System.Exception e) { Debug.LogWarning($"[Capturas] No se pudo capturar {nombre}: {e.Message}"); }
    }
}
