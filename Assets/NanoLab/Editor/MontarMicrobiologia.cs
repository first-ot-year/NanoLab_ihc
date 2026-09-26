using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ML = MontarLaboratorio;

// Monta Assets/NanoLab/Scenes/Microbiologia.unity a partir de la sala de Animatics:
// microscopios en las islas, cepario, zona de alto riesgo con la estación 3D,
// cabina de desinfección, tutorial, progreso y portal a Química.
public static class MontarMicrobiologia
{
    const string SourceScene = "Assets/3D Laboratory Environment with Appratus/Scenes/Laboratory Scene.unity";
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string QuimicaPath = "Assets/NanoLab/Scenes/Laboratorio.unity";
    const string MatDir = "Assets/NanoLab/Materiales/Micro";
    const string ResDir = "Assets/NanoLab/Resources/NanoLab";
    const string PrefabDir = "Assets/NanoLab/Prefabs";
    const string MicroscopioReal = "Assets/NanoLab/Modelos/Microscopio.prefab";

    // Distribución (medida en la vista cenital de la sala).
    static readonly (float x, float z, bool haciaMasZ)[] Puestos =
    {
        (-2.55f, 12.62f, true), (-1.15f, 12.62f, true), (-2.55f, 12.12f, false), (-1.15f, 12.12f, false),
        (-2.55f, 7.37f, true),  (-1.15f, 7.37f, true),  (-2.55f, 6.87f, false),  (-1.15f, 6.87f, false),
        (4.55f, 11.75f, true),  (5.85f, 11.75f, true),
    };
    static readonly Vector3 CeparioCentro = new Vector3(5.2f, 0f, 11.05f);   // fila inferior de la isla derecha
    const float BslMinX = -10.95f, BslMaxX = -5.0f, BslFrenteZ = 12.3f, BslFondoZ = 15.25f;
    const float PuertaX0 = -9.2f, PuertaX1 = -8.0f;
    static readonly Vector3 Pedestal = new Vector3(-8.2f, 0f, 14.4f);
    static readonly Vector3 Consola = new Vector3(-8.2f, 0f, 13.35f);
    static readonly Vector2 MesaCriotubos = new Vector2(-7.21f, 15.73f);

    static readonly string[] Aparatos =
    {
        "erlenmeyer", "beaker", "florence", "test_tube", "spirit_lamp", "crucible", "funnel", "boiling_liquid",
        "graduated_cylinder", "glass support", "stand glass", "china dish", "forceps", "dropper", "analytical", "liquids", "lab glass",
    };

    static Dictionary<string, Material> mats;
    static float suelo;

    [MenuItem("NanoLab/Montar microbiología")]
    public static void Montar()
    {
        ML.GenerarUVsDeLightmap();
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath)) throw new System.Exception("No se pudo copiar la escena base");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Después de abrir la escena: OpenScene descarga los assets cargados antes.
        var muestras = CrearMuestras.Todas();
        AssetDatabase.SaveAssets();
        CrearMateriales();
        var slidePrefab = CrearPrefabPortaobjetos();

        var cam = Object.FindObjectsOfType<Camera>().FirstOrDefault();
        if (cam) Object.DestroyImmediate(cam.gameObject);

        int quitados = QuitarAparatos();
        int colliders = ML.AnadirColliders();
        ML.MarcarEstatico();
        var sala = ML.BoundsEscena();
        suelo = ML.AlturaSuelo(sala);
        ML.CrearSueloSeguridad(sala, suelo);
        Physics.SyncTransforms();
        Debug.Log($"[Micro] Aparatos de química quitados: {quitados}. Colliders añadidos a la sala: {colliders}.");

        var raiz = new GameObject("Microbiologia").transform;
        var virus = muestras.Where(m => m.tipo == TipoMuestra.Virus).ToArray();
        var bacterias = muestras.Where(m => m.tipo == TipoMuestra.Bacteria).ToArray();

        var puestos = Grupo(raiz, "Puestos");
        for (int i = 0; i < Puestos.Length; i++)
        {
            var p = Puestos[i];
            CrearPuesto(puestos, i + 1, new Vector3(p.x, 0f, p.z), p.haciaMasZ ? 0f : 180f, slidePrefab);
        }
        CrearCepario(Grupo(raiz, "Cepario"), bacterias, slidePrefab);
        CrearZonaBSL(Grupo(raiz, "ZonaAltoRiesgo"), virus, slidePrefab);
        CrearCabinaDesinfeccion(Grupo(raiz, "CabinaDesinfeccion"));
        CrearTutorialYProgreso(Grupo(raiz, "Bienvenida"), muestras);
        CrearPortal(Grupo(raiz, "Portal"), "Laboratorio", "LABORATORIO\nDE QUÍMICA");

        var player = ML.ColocarJugador(new Vector3(-3.3f, 0f, 2.24f), 24f, suelo, sala);
        ConfigurarAgarre(player);

        ML.ConfigurarLuces("Microbiologia_Lighting");
        ML.CrearSondas(sala, suelo);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Micro] Horneando iluminación...");
        bool ok = Lightmapping.Bake();
        Debug.Log(ok ? "[Micro] Bake completado." : "[Micro] El bake falló.");
        EditorSceneManager.SaveScene(scene);

        var escenas = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        escenas.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = escenas.ToArray();

        AgregarPortalEnQuimica();
        Capturas();
    }

    // ------------------------------------------------------------------ sala

    static int QuitarAparatos()
    {
        int n = 0;
        foreach (var t in Object.FindObjectsOfType<Transform>())
        {
            if (t == null) continue;
            var go = t.gameObject;
            string fuente = go.name;
            var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
            if (src != null) fuente = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(src)) + " " + go.name;
            fuente = fuente.ToLowerInvariant().Replace('_', ' ');
            if (!Aparatos.Any(a => fuente.Contains(a.Replace('_', ' ')))) continue;
            var raizInst = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
            if (raizInst == null) continue;
            Object.DestroyImmediate(raizInst);
            n++;
        }
        return n;
    }

    static float AlturaEn(float x, float z, float desde = 1.6f)
    {
        if (Physics.Raycast(new Vector3(x, desde, z), Vector3.down, out var hit, 3f)) return hit.point.y;
        return suelo;
    }

    static float Techo(float x, float z)
    {
        if (Physics.Raycast(new Vector3(x, suelo + 1.5f, z), Vector3.up, out var hit, 5f)) return hit.point.y;
        return suelo + 3f;
    }

    // ------------------------------------------------------------------ puestos

    static void CrearPuesto(Transform padre, int numero, Vector3 punto, float yaw, GameObject slidePrefab)
    {
        float top = AlturaEn(punto.x, punto.z);
        var puesto = new GameObject($"Puesto_{numero:00}").transform;
        puesto.SetParent(padre, false);
        puesto.position = new Vector3(punto.x, top, punto.z);
        puesto.rotation = Quaternion.Euler(0f, yaw, 0f);

        CrearMicroscopio(puesto, new Vector3(0f, 0f, -0.05f));

        // Bandeja con 3 portaobjetos limpios (a la derecha del microscopio visto por el usuario).
        var bandeja = Caja(puesto, "Bandeja", new Vector3(-0.3f, 0.006f, 0.02f), new Vector3(0.1f, 0.012f, 0.11f), mats["Oscuro"], true);
        for (int i = 0; i < 3; i++)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, puesto);
            s.transform.localPosition = new Vector3(-0.3f, 0.015f, -0.02f + i * 0.035f);
            s.transform.localRotation = Quaternion.identity;
        }
        CrearDesinfectante(puesto, new Vector3(0.28f, 0f, 0.02f));

        var num = Texto3D(puesto, $"PUESTO {numero}", new Vector3(0f, 0.012f, 0.2f), Quaternion.Euler(90f, 180f, 0f), 0.35f, new Color(0.35f, 0.85f, 1f), 0.4f);
        num.alignment = TextAlignmentOptions.Center;
    }

    static void CrearMicroscopio(Transform padre, Vector3 pos)
    {
        var m = new GameObject("Microscopio").transform;
        m.SetParent(padre, false);
        m.localPosition = pos;

        Transform ocular, anclajeParent;
        Vector3 platinaPos;
        var real = AssetDatabase.LoadAssetAtPath<GameObject>(MicroscopioReal);
        if (real != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(real, m);
            inst.transform.localPosition = Vector3.zero;
            // Anclajes relativos a los bounds del modelo (ajustados con las capturas).
            var b = ML.BoundsDe(inst);
            var lo = m.InverseTransformPoint(b.min); var hi = m.InverseTransformPoint(b.max);
            platinaPos = new Vector3(0f, Mathf.Lerp(lo.y, hi.y, 0.36f), Mathf.Lerp(lo.z, hi.z, 0.62f));
            ocular = Vacio(m, "Ocular", new Vector3(0f, hi.y + 0.03f, Mathf.Lerp(lo.z, hi.z, 0.85f)));
            foreach (var r in inst.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponent<Collider>() == null) r.gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            // Microscopio provisional (se sustituye por el SWIFT SW380B al convertirlo).
            Caja(m, "Base", new Vector3(0f, 0.015f, -0.01f), new Vector3(0.18f, 0.03f, 0.22f), mats["Blanco"], true);
            Caja(m, "Columna", new Vector3(0f, 0.17f, -0.09f), new Vector3(0.05f, 0.28f, 0.05f), mats["Blanco"], true);
            Caja(m, "Platina", new Vector3(0f, 0.125f, 0.02f), new Vector3(0.15f, 0.012f, 0.14f), mats["Negro"], true);
            Cilindro(m, "Condensador", new Vector3(0f, 0.1f, 0.02f), new Vector3(0.05f, 0.012f, 0.05f), Quaternion.identity, mats["Negro"]);
            Cilindro(m, "Lampara", new Vector3(0f, 0.035f, 0.02f), new Vector3(0.035f, 0.004f, 0.035f), Quaternion.identity, mats["LuzBlanca"]);
            Caja(m, "Brazo", new Vector3(0f, 0.29f, -0.04f), new Vector3(0.07f, 0.07f, 0.14f), mats["Blanco"], true);
            Cilindro(m, "Revolver", new Vector3(0f, 0.245f, 0.02f), new Vector3(0.09f, 0.012f, 0.09f), Quaternion.identity, mats["Cromo"]);
            for (int i = 0; i < 3; i++)
            {
                float a = (i - 1) * 35f * Mathf.Deg2Rad;
                Cilindro(m, "Objetivo" + i, new Vector3(Mathf.Sin(a) * 0.03f, 0.215f, 0.02f + Mathf.Cos(a) * 0.012f), new Vector3(0.02f, 0.025f, 0.02f), Quaternion.identity, mats["Cromo"]);
            }
            Caja(m, "Cabezal", new Vector3(0f, 0.34f, 0.02f), new Vector3(0.1f, 0.045f, 0.09f), mats["Blanco"], true);
            for (int s = -1; s <= 1; s += 2)
            {
                Cilindro(m, "Ocular" + s, new Vector3(0.03f * s, 0.375f, 0.06f), new Vector3(0.028f, 0.04f, 0.028f), Quaternion.Euler(45f, 0f, 0f), mats["Negro"]);
                Cilindro(m, "Goma" + s, new Vector3(0.03f * s, 0.405f, 0.09f), new Vector3(0.034f, 0.008f, 0.034f), Quaternion.Euler(45f, 0f, 0f), mats["Negro"]);
                Cilindro(m, "Mando" + s, new Vector3(0.075f * s, 0.14f, -0.07f), new Vector3(0.05f, 0.012f, 0.05f), Quaternion.Euler(0f, 0f, 90f), mats["Negro"]);
            }
            platinaPos = new Vector3(0f, 0.1335f, 0.02f);
            ocular = Vacio(m, "Ocular", new Vector3(0f, 0.43f, 0.125f));
        }

        // Platina: trigger + anclaje + silueta fantasma.
        var platinaGo = new GameObject("PlatinaZona");
        platinaGo.transform.SetParent(m, false);
        platinaGo.transform.localPosition = platinaPos + Vector3.up * 0.02f;
        var bc = platinaGo.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(0.16f, 0.07f, 0.15f);
        var platina = platinaGo.AddComponent<Platina>();
        platina.anclaje = Vacio(m, "Anclaje", platinaPos + Vector3.up * 0.001f);
        var fantasma = Caja(platina.anclaje, "Fantasma", Vector3.zero, new Vector3(0.075f, 0.002f, 0.025f), mats["Fantasma"], false);
        platina.fantasma = fantasma.GetComponent<Renderer>();
        platina.fantasma.enabled = false;

        var micro = m.gameObject.AddComponent<MicroscopioOptico>();
        micro.platina = platina;
        micro.ocular = ocular;
        micro.ayuda = Ayuda(m, "AyudaColocar", "Coloca aquí un portaobjetos\ncon muestra", new Vector3(0f, 0.55f, 0.05f), new Color(1f, 0.85f, 0.4f));
        micro.ayudaMirar = Ayuda(m, "AyudaMirar", "Acerca la vista al ocular", new Vector3(0f, 0.55f, 0.05f), new Color(0.4f, 1f, 0.6f));
    }

    static GameObject Ayuda(Transform padre, string nombre, string texto, Vector3 pos, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.AddComponent<Cartel>();
        var t = Texto3D(go.transform, texto, Vector3.zero, Quaternion.identity, 0.28f, color, 0.7f);
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 200);
        go.SetActive(false);
        return go;
    }

    static void CrearDesinfectante(Transform padre, Vector3 pos)
    {
        var v = new GameObject("Desinfectante").transform;
        v.SetParent(padre, false);
        v.localPosition = pos;
        Cilindro(v, "Vaso", new Vector3(0f, 0.045f, 0f), new Vector3(0.07f, 0.045f, 0.07f), Quaternion.identity, mats["Vidrio"]);
        Cilindro(v, "Liquido", new Vector3(0f, 0.03f, 0f), new Vector3(0.064f, 0.028f, 0.064f), Quaternion.identity, mats["LiquidoAzul"]);
        var brillo = Cilindro(v, "Brillo", new Vector3(0f, 0.045f, 0f), new Vector3(0.074f, 0.047f, 0.074f), Quaternion.identity, mats["Brillo"]);
        var bc = v.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.center = new Vector3(0f, 0.06f, 0f); bc.size = new Vector3(0.09f, 0.1f, 0.09f);
        v.gameObject.AddComponent<FuenteMuestra>().esDesinfectante = true;
        v.gameObject.AddComponent<Resaltado>().brillos = new[] { brillo.GetComponent<Renderer>() };
        var t = Texto3D(v, "DESINFECTANTE", new Vector3(0f, 0.13f, 0f), Quaternion.identity, 0.16f, new Color(0.5f, 0.85f, 1f), 0.3f);
        t.alignment = TextAlignmentOptions.Center;
        t.gameObject.AddComponent<Cartel>();
    }

    // ------------------------------------------------------------------ cepario

    static void CrearCepario(Transform padre, MuestraData[] bacterias, GameObject slidePrefab)
    {
        float top = AlturaEn(CeparioCentro.x, CeparioCentro.z);
        var petri = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NanoLab/Modelos/PlacaPetri_Cultivo.prefab");
        var c = new GameObject("CeparioBacterias").transform;
        c.SetParent(padre, false);
        c.position = new Vector3(CeparioCentro.x, top, CeparioCentro.z);
        c.rotation = Quaternion.Euler(0f, 180f, 0f);   // el usuario está en z menor

        for (int i = 0; i < bacterias.Length; i++)
        {
            var b = bacterias[i];
            var f = new GameObject("Placa_" + b.name).transform;
            f.SetParent(c, false);
            f.localPosition = new Vector3(-0.35f + i * 0.35f, 0f, 0.05f);
            if (petri != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(petri, f);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localScale = Vector3.one * 1.3f;
                foreach (var col in inst.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            }
            else Cilindro(f, "Placa", new Vector3(0f, 0.01f, 0f), new Vector3(0.1f, 0.01f, 0.1f), Quaternion.identity, mats["Vidrio"]);
            var brillo = Cilindro(f, "Brillo", new Vector3(0f, 0.012f, 0f), new Vector3(0.14f, 0.014f, 0.14f), Quaternion.identity, mats["Brillo"]);
            var bc = f.gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = new Vector3(0f, 0.03f, 0f); bc.size = new Vector3(0.15f, 0.07f, 0.15f);
            f.gameObject.AddComponent<FuenteMuestra>().muestra = b;
            f.gameObject.AddComponent<Resaltado>().brillos = new[] { brillo.GetComponent<Renderer>() };
            Etiqueta(f, b.nombre, b.nombreCientifico, new Vector3(0f, 0.2f, 0f), b.color);
        }

        // Portaobjetos de reserva y desinfectante en el cepario.
        Caja(c, "Bandeja", new Vector3(0.62f, 0.006f, 0.05f), new Vector3(0.1f, 0.012f, 0.11f), mats["Oscuro"], true);
        for (int i = 0; i < 3; i++)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, c);
            s.transform.localPosition = new Vector3(0.62f, 0.015f, 0.01f + i * 0.035f);
        }
        CrearDesinfectante(c, new Vector3(-0.65f, 0f, 0.05f));

        var titulo = new GameObject("TituloCepario").transform;
        titulo.SetParent(c, false);
        titulo.localPosition = new Vector3(0f, 0.55f, 0.15f);
        titulo.gameObject.AddComponent<Cartel>();
        var t = Texto3D(titulo, "CEPARIO · BACTERIAS", Vector3.zero, Quaternion.identity, 0.5f, new Color(0.35f, 0.85f, 1f), 1.5f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 200);
    }

    static void Etiqueta(Transform padre, string titulo, string sub, Vector3 pos, Color color)
    {
        var e = new GameObject("Etiqueta").transform;
        e.SetParent(padre, false);
        e.localPosition = pos;
        e.gameObject.AddComponent<Cartel>();
        var hex = ColorUtility.ToHtmlStringRGB(color);
        var t = Texto3D(e, $"<b><color=#{hex}>{titulo}</color></b>\n<size=60%><i>{sub}</i></size>", Vector3.zero, Quaternion.identity, 0.3f, Color.white, 0.6f);
        t.alignment = TextAlignmentOptions.Center;
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 220);
    }

    // ------------------------------------------------------------------ zona BSL

    static void CrearZonaBSL(Transform padre, MuestraData[] virus, GameObject slidePrefab)
    {
        float techo = Techo(-8f, 14f);
        float alto = Mathf.Min(techo - suelo, 3.2f) - 0.02f;

        // Paredes de vidrio con postes metálicos.
        Pared(padre, new Vector3(BslMinX, 0, BslFrenteZ), new Vector3(PuertaX0, 0, BslFrenteZ), alto);
        Pared(padre, new Vector3(PuertaX1, 0, BslFrenteZ), new Vector3(BslMaxX, 0, BslFrenteZ), alto);
        Pared(padre, new Vector3(BslMaxX, 0, BslFrenteZ), new Vector3(BslMaxX, 0, BslFondoZ), alto);
        Caja(padre, "Dintel", new Vector3((PuertaX0 + PuertaX1) / 2f, suelo + 2.25f + (alto - 2.25f) / 2f, BslFrenteZ), new Vector3(PuertaX1 - PuertaX0, alto - 2.25f, 0.06f), mats["Marco"], true);

        // Luz rosada y franja emisiva.
        var luz = new GameObject("LuzRosada").AddComponent<Light>();
        luz.transform.SetParent(padre, false);
        luz.transform.position = new Vector3(-8f, suelo + alto - 0.3f, 14.2f);
        luz.type = LightType.Point; luz.color = new Color(1f, 0.5f, 0.62f); luz.range = 7.5f; luz.intensity = 1.6f;
        luz.lightmapBakeType = LightmapBakeType.Baked;
        Caja(padre, "FranjaRosa", new Vector3((BslMinX + BslMaxX) / 2f, suelo + alto - 0.05f, BslFrenteZ + 0.05f), new Vector3(BslMaxX - BslMinX, 0.03f, 0.03f), mats["EmisivoRosa"], false);

        // Carteles exteriores.
        Senal(padre, "SenalRiesgo", new Vector3(-10.0f, suelo + 1.6f, BslFrenteZ - 0.035f), Quaternion.identity,
              "ÁREA DE ALTO RIESGO\nAGENTES BIOLÓGICOS", 0.75f);
        Senal(padre, "SenalAcceso", new Vector3(-7.25f, suelo + 1.55f, BslFrenteZ - 0.035f), Quaternion.identity,
              "ACCESO\nRESTRINGIDO", 0.42f);
        CartelEPP(padre, new Vector3(-9.6f, suelo + 1.75f, 16.45f));

        // Estación de análisis 3D.
        CrearEstacion3D(padre);

        // Criotubos de virus sobre la mesa del fondo.
        float top = AlturaEn(MesaCriotubos.x, MesaCriotubos.y);
        var rack = new GameObject("RackCriotubos").transform;
        rack.SetParent(padre, false);
        rack.position = new Vector3(MesaCriotubos.x, top, MesaCriotubos.y - 0.15f);
        rack.rotation = Quaternion.Euler(0f, 180f, 0f);
        Caja(rack, "Rack", new Vector3(0f, 0.02f, 0f), new Vector3(0.62f, 0.04f, 0.09f), mats["Marco"], true);
        for (int i = 0; i < virus.Length; i++)
        {
            var v = virus[i];
            var tubo = new GameObject("Criotubo_" + v.name).transform;
            tubo.SetParent(rack, false);
            tubo.localPosition = new Vector3(-0.24f + i * 0.12f, 0.04f, 0f);
            Cilindro(tubo, "Tubo", new Vector3(0f, 0.035f, 0f), new Vector3(0.026f, 0.035f, 0.026f), Quaternion.identity, mats["TuboBlanco"]);
            Cilindro(tubo, "Tapa", new Vector3(0f, 0.078f, 0f), new Vector3(0.03f, 0.009f, 0.03f), Quaternion.identity, MaterialTapa(v));
            var brillo = Cilindro(tubo, "Brillo", new Vector3(0f, 0.045f, 0f), new Vector3(0.036f, 0.047f, 0.036f), Quaternion.identity, mats["Brillo"]);
            var bc = tubo.gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = new Vector3(0f, 0.06f, 0f); bc.size = new Vector3(0.07f, 0.12f, 0.07f);
            tubo.gameObject.AddComponent<FuenteMuestra>().muestra = v;
            tubo.gameObject.AddComponent<Resaltado>().brillos = new[] { brillo.GetComponent<Renderer>() };
            Etiqueta(tubo, v.nombre, v.riesgo, new Vector3(0f, 0.22f, 0f), v.color);
        }
        Caja(rack, "Bandeja", new Vector3(0.45f, 0.006f, 0f), new Vector3(0.1f, 0.012f, 0.11f), mats["Oscuro"], true);
        for (int i = 0; i < 3; i++)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, rack);
            s.transform.localPosition = new Vector3(0.45f, 0.015f, -0.035f + i * 0.035f);
        }
        var titulo = new GameObject("TituloVirus").transform;
        titulo.SetParent(rack, false);
        titulo.localPosition = new Vector3(0f, 0.5f, 0f);
        titulo.gameObject.AddComponent<Cartel>();
        var t = Texto3D(titulo, "MUESTRAS VIRALES · BSL-3", Vector3.zero, Quaternion.identity, 0.45f, new Color(1f, 0.55f, 0.65f), 1.6f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 200);
    }

    static void Pared(Transform padre, Vector3 a, Vector3 b, float alto)
    {
        var dir = b - a;
        float largo = dir.magnitude;
        int paneles = Mathf.Max(1, Mathf.CeilToInt(largo / 1.3f));
        var rot = Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir.normalized));
        for (int i = 0; i < paneles; i++)
        {
            var c = Vector3.Lerp(a, b, (i + 0.5f) / paneles);
            var g = Caja(padre, "Vidrio", new Vector3(c.x, suelo + alto / 2f, c.z), new Vector3(largo / paneles, alto, 0.02f), mats["VidrioPared"], true);
            g.transform.rotation = rot;
        }
        for (int i = 0; i <= paneles; i++)
        {
            var c = Vector3.Lerp(a, b, i / (float)paneles);
            Caja(padre, "Poste", new Vector3(c.x, suelo + alto / 2f, c.z), new Vector3(0.05f, alto, 0.05f), mats["Marco"], true);
        }
        var mid = (a + b) / 2f;
        var zocalo = Caja(padre, "Zocalo", new Vector3(mid.x, suelo + 0.05f, mid.z), new Vector3(largo, 0.1f, 0.05f), mats["Marco"], false);
        zocalo.transform.rotation = rot;
        var riel = Caja(padre, "Riel", new Vector3(mid.x, suelo + alto - 0.03f, mid.z), new Vector3(largo, 0.06f, 0.05f), mats["Marco"], false);
        riel.transform.rotation = rot;
    }

    static void Senal(Transform padre, string nombre, Vector3 pos, Quaternion rot, string texto, float tam)
    {
        var s = new GameObject(nombre).transform;
        s.SetParent(padre, false);
        s.SetPositionAndRotation(pos, rot);
        Caja(s, "FondoAmarillo", new Vector3(0f, tam * 0.35f, 0f), new Vector3(tam, tam, 0.005f), mats["Amarillo"], false);
        Caja(s, "Borde", new Vector3(0f, tam * 0.35f, 0.002f), new Vector3(tam * 1.06f, tam * 1.06f, 0.004f), mats["NegroMate"], false);
        var sim = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(sim.GetComponent<Collider>());
        sim.name = "Simbolo";
        sim.transform.SetParent(s, false);
        sim.transform.localPosition = new Vector3(0f, tam * 0.35f, -0.004f);
        sim.transform.localScale = Vector3.one * tam * 0.8f;
        sim.GetComponent<Renderer>().sharedMaterial = mats["Biohazard"];
        Caja(s, "FondoTexto", new Vector3(0f, -tam * 0.38f, 0f), new Vector3(tam * 1.5f, tam * 0.45f, 0.005f), mats["BlancoMate"], false);
        var t = Texto3D(s, texto, new Vector3(0f, -tam * 0.38f, -0.004f), Quaternion.identity, tam * 1.1f, Color.black, tam * 1.45f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
    }

    static void CartelEPP(Transform padre, Vector3 pos)
    {
        var s = new GameObject("CartelEPP").transform;
        s.SetParent(padre, false);
        s.position = pos;   // sin rotar: el texto mira hacia -z (hacia la sala)
        Caja(s, "Fondo", Vector3.zero, new Vector3(0.9f, 0.6f, 0.01f), mats["Rojo"], false);
        var t = Texto3D(s, "USO OBLIGATORIO DE EPP\n<size=70%>Bata · Guantes · Gafas · Mascarilla N95</size>", new Vector3(0f, 0f, -0.007f), Quaternion.identity, 0.55f, Color.white, 0.85f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
    }

    static void CrearEstacion3D(Transform padre)
    {
        var est = new GameObject("EstacionAnalisis3D").transform;
        est.SetParent(padre, false);
        est.position = new Vector3(Pedestal.x, suelo, Pedestal.z);

        // Pedestal y proyector.
        Cilindro(est, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.45f, 0.55f), Quaternion.identity, mats["Negro"]).AddComponent<CapsuleCollider>();
        Cilindro(est, "AnilloLuz", new Vector3(0f, 0.905f, 0f), new Vector3(0.5f, 0.006f, 0.5f), Quaternion.identity, mats["EmisivoCian"]);
        var haz = Cilindro(est, "Haz", new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 0.34f, 0.42f), Quaternion.identity, mats["Haz"]);
        var holo = Vacio(est, "Holograma", new Vector3(0f, 1.6f, 0f));

        // Consola con ranura y botones.
        var consola = new GameObject("Consola").transform;
        consola.SetParent(est, false);
        consola.position = new Vector3(Consola.x, suelo, Consola.z);
        Caja(consola, "Cuerpo", new Vector3(0f, 0.42f, 0f), new Vector3(0.8f, 0.84f, 0.36f), mats["Blanco"], true);
        var panel = Caja(consola, "Panel", new Vector3(0f, 0.87f, -0.03f), new Vector3(0.8f, 0.03f, 0.42f), mats["Oscuro"], true).transform;
        panel.localRotation = Quaternion.Euler(-25f, 0f, 0f);

        // Ranura (platina) a la izquierda.
        var ranura = new GameObject("Ranura").transform;
        ranura.SetParent(panel, false);
        ranura.localPosition = new Vector3(-0.25f, 0.5f, 0f);   // en unidades locales del panel (escalado)
        ranura.localRotation = Quaternion.identity;
        ranura.localScale = new Vector3(1f / 0.8f, 1f / 0.03f, 1f / 0.42f);   // deshace la escala del panel
        var marcoRanura = Caja(ranura, "Marco", new Vector3(0f, 0.002f, 0f), new Vector3(0.1f, 0.003f, 0.05f), mats["EmisivoCian"], false);
        var zona = new GameObject("PlatinaZona");
        zona.transform.SetParent(ranura, false);
        zona.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        var bc = zona.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.14f, 0.07f, 0.1f);
        var platina = zona.AddComponent<Platina>();
        platina.anclaje = Vacio(ranura, "Anclaje", new Vector3(0f, 0.005f, 0f));
        var fantasma = Caja(platina.anclaje, "Fantasma", Vector3.zero, new Vector3(0.075f, 0.002f, 0.025f), mats["Fantasma"], false);
        platina.fantasma = fantasma.GetComponent<Renderer>();
        platina.fantasma.enabled = false;
        var tr = Texto3D(ranura, "RANURA DE MUESTRA", new Vector3(0f, 0.004f, -0.05f), Quaternion.Euler(90f, 0f, 0f), 0.13f, new Color(0.4f, 0.9f, 1f), 0.2f);
        tr.alignment = TextAlignmentOptions.Center;

        var e = est.gameObject.AddComponent<EstacionAnalisis3D>();
        e.platina = platina;
        e.holograma = holo;
        e.haz = haz;
        haz.SetActive(false);

        string[] nombres = { "ROTAR <", "ROTAR >", "ZOOM +", "ZOOM -", "SUPERIOR", "LATERAL", "CORTE" };
        var botones = new BotonFisico[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            int fila = i / 4, col = i % 4;
            botones[i] = Boton(ranura, nombres[i], new Vector3(0.12f + col * 0.085f, 0f, 0.05f - fila * 0.09f));
        }
        e.rotarIzq = botones[0]; e.rotarDer = botones[1]; e.zoomMas = botones[2]; e.zoomMenos = botones[3];
        e.vistaSuperior = botones[4]; e.vistaLateral = botones[5]; e.corte = botones[6];

        // Panel de información (mira hacia la puerta).
        var info = new GameObject("PanelInfo").transform;
        info.SetParent(est, false);
        info.position = new Vector3(-6.75f, suelo + 1.55f, 14.2f);
        var haciaUsuario = new Vector3(-8.4f, 0f, 12.8f) - new Vector3(-6.75f, 0f, 14.2f);
        info.rotation = Quaternion.LookRotation(-haciaUsuario.normalized);
        Caja(info, "Fondo", new Vector3(0f, 0f, 0.01f), new Vector3(1.0f, 0.95f, 0.02f), mats["PanelHUD"], false);
        Caja(info, "Borde", new Vector3(0f, 0f, 0.012f), new Vector3(1.03f, 0.98f, 0.018f), mats["EmisivoCian"], false);
        e.titulo = Texto3D(info, "ESTACIÓN DE ANÁLISIS 3D", new Vector3(0f, 0.4f, -0.005f), Quaternion.identity, 0.5f, new Color(0.4f, 0.9f, 1f), 0.9f);
        e.titulo.fontStyle = FontStyles.Bold; e.titulo.alignment = TextAlignmentOptions.Left;
        e.ficha = Texto3D(info, "", new Vector3(-0.2f, 0.05f, -0.005f), Quaternion.identity, 0.27f, Color.white, 0.5f);
        e.ficha.alignment = TextAlignmentOptions.TopLeft;
        e.ficha.rectTransform.sizeDelta = new Vector2(0.5f, 0.55f);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.name = "Micrografia";
        quad.transform.SetParent(info, false);
        quad.transform.localPosition = new Vector3(0.24f, 0.05f, -0.004f);
        quad.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
        quad.GetComponent<Renderer>().sharedMaterial = mats["MicroEstacion"];
        e.micrografia = quad.GetComponent<Renderer>();
        e.micrografia.enabled = false;
        e.estado = Texto3D(info, "", new Vector3(0f, -0.36f, -0.005f), Quaternion.identity, 0.26f, new Color(1f, 0.85f, 0.45f), 0.9f);
        e.estado.alignment = TextAlignmentOptions.Center;
    }

    static BotonFisico Boton(Transform padre, string texto, Vector3 pos)
    {
        var b = new GameObject("Boton_" + texto).transform;
        b.SetParent(padre, false);
        b.localPosition = pos;
        Caja(b, "Base", new Vector3(0f, 0.004f, 0f), new Vector3(0.07f, 0.008f, 0.07f), mats["Marco"], false);
        var tapa = Caja(b, "Tapa", new Vector3(0f, 0.014f, 0f), new Vector3(0.055f, 0.012f, 0.055f), mats["EmisivoCian"], false).transform;
        var bc = b.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.center = new Vector3(0f, 0.03f, 0f); bc.size = new Vector3(0.07f, 0.06f, 0.07f);
        var bf = b.gameObject.AddComponent<BotonFisico>();
        bf.tapa = tapa;
        bf.etiqueta = Texto3D(b, texto, new Vector3(0f, 0.001f, -0.05f), Quaternion.Euler(90f, 0f, 0f), 0.11f, Color.white, 0.09f);
        bf.etiqueta.alignment = TextAlignmentOptions.Center;
        bf.etiqueta.fontStyle = FontStyles.Bold;
        return bf;
    }

    // ------------------------------------------------------------------ cabina

    static void CrearCabinaDesinfeccion(Transform padre)
    {
        float cx = (PuertaX0 + PuertaX1) / 2f;
        float z0 = BslFrenteZ - 1.15f, z1 = BslFrenteZ - 0.04f, zc = (z0 + z1) / 2f;
        float ancho = PuertaX1 - PuertaX0, alto = 2.3f;
        foreach (var x in new[] { PuertaX0 - 0.03f, PuertaX1 + 0.03f })
        {
            Caja(padre, "Lateral", new Vector3(x, suelo + alto / 2f, zc), new Vector3(0.06f, alto, z1 - z0), mats["Blanco"], true);
            float dentro = x < cx ? 0.035f : -0.035f;
            for (int i = 0; i < 3; i++)
                Caja(padre, "Boquilla", new Vector3(x + dentro, suelo + 0.6f + i * 0.55f, zc), new Vector3(0.012f, 0.04f, z1 - z0 - 0.2f), mats["EmisivoAzul"], false);
        }
        Caja(padre, "Techo", new Vector3(cx, suelo + alto + 0.08f, zc), new Vector3(ancho + 0.14f, 0.16f, z1 - z0), mats["Blanco"], true);
        Caja(padre, "Rejilla", new Vector3(cx, suelo + 0.006f, zc), new Vector3(ancho - 0.05f, 0.012f, z1 - z0 - 0.05f), mats["Oscuro"], false);
        Caja(padre, "BordeRejilla", new Vector3(cx, suelo + 0.004f, zc), new Vector3(ancho, 0.006f, z1 - z0), mats["EmisivoAzul"], false);
        var luz = new GameObject("LuzAzul").AddComponent<Light>();
        luz.transform.SetParent(padre, false);
        luz.transform.position = new Vector3(cx, suelo + 2.1f, zc);
        luz.type = LightType.Point; luz.color = new Color(0.35f, 0.65f, 1f); luz.range = 2.6f; luz.intensity = 1.8f;
        luz.lightmapBakeType = LightmapBakeType.Baked;

        foreach (var (z, rotY, texto, fondo) in new[] { (z1 + 0.02f, 180f, "DESINFECCIÓN\nANTES DE SALIR", "BlancoMate"), (z0 - 0.02f, 0f, "ENTRADA\nÁREA BSL-3", "Amarillo") })
        {
            var cartel = new GameObject("Cartel").transform;
            cartel.SetParent(padre, false);
            cartel.position = new Vector3(cx, suelo + alto + 0.36f, z);
            cartel.rotation = Quaternion.Euler(0f, rotY, 0f);
            Caja(cartel, "Placa", new Vector3(0f, 0f, 0.012f), new Vector3(ancho + 0.2f, 0.42f, 0.02f), mats[fondo], false);
            var t = Texto3D(cartel, texto, new Vector3(0f, 0f, -0.002f), Quaternion.identity, 0.85f, Color.black, ancho + 0.15f);
            t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        }
    }

    // ------------------------------------------------------------------ bienvenida

    static void CrearTutorialYProgreso(Transform padre, MuestraData[] muestras)
    {
        var spawn = new Vector3(-3.3f, 0f, 2.24f);

        var tut = Totem(padre, "Tutorial", new Vector3(-1.9f, 0f, 3.7f), spawn, 1.4f, 1.5f);
        var tt = Texto3D(tut, "LABORATORIO DE MICROBIOLOGÍA", new Vector3(0f, 0.64f, -0.02f), Quaternion.identity, 0.75f, new Color(0.4f, 0.9f, 1f), 1.3f);
        tt.alignment = TextAlignmentOptions.Center; tt.fontStyle = FontStyles.Bold;
        string[] pasos =
        {
            "<b>Coge un portaobjetos</b>\nAcerca el mando y aprieta el gatillo lateral (grip).",
            "<b>Toma una muestra</b>\nToca un criotubo (virus) o una placa (bacterias).",
            "<b>Colócalo en la platina</b>\nSuéltalo sobre la silueta azul del microscopio.",
            "<b>Mira por el ocular</b>\nAcerca la cara. A/B: aumento · joystick: pestañas.",
        };
        for (int i = 0; i < pasos.Length; i++)
        {
            float y = 0.36f - i * 0.25f;
            Cilindro(tut, "Circulo" + i, new Vector3(-0.56f, y, -0.012f), new Vector3(0.13f, 0.004f, 0.13f), Quaternion.Euler(90f, 0f, 0f), mats["EmisivoCian"]);
            var n = Texto3D(tut, (i + 1).ToString(), new Vector3(-0.56f, y, -0.02f), Quaternion.identity, 0.7f, Color.black, 0.12f);
            n.alignment = TextAlignmentOptions.Center; n.fontStyle = FontStyles.Bold;
            var p = Texto3D(tut, pasos[i], new Vector3(0.1f, y, -0.02f), Quaternion.identity, 0.42f, Color.white, 1.1f);
            p.alignment = TextAlignmentOptions.Left;
        }
        var extra = Texto3D(tut, "Los virus están en la <color=#FF8CA0>zona de alto riesgo</color> (al fondo, a la izquierda).\nToca el <color=#8FD8FF>desinfectante</color> para limpiar un portaobjetos.",
                            new Vector3(0f, -0.64f, -0.02f), Quaternion.identity, 0.32f, new Color(0.8f, 0.88f, 1f), 1.3f);
        extra.alignment = TextAlignmentOptions.Center;

        var prog = Totem(padre, "Progreso", new Vector3(-4.7f, 0f, 3.7f), spawn, 1.0f, 1.3f);
        var pt = Texto3D(prog, "MUESTRAS ANALIZADAS", new Vector3(0f, 0.54f, -0.02f), Quaternion.identity, 0.55f, new Color(0.4f, 0.9f, 1f), 0.95f);
        pt.alignment = TextAlignmentOptions.Center; pt.fontStyle = FontStyles.Bold;
        var contador = Texto3D(prog, $"0/{muestras.Length}", new Vector3(0f, 0.39f, -0.02f), Quaternion.identity, 0.7f, Color.white, 0.9f);
        contador.alignment = TextAlignmentOptions.Center; contador.fontStyle = FontStyles.Bold;
        // Texto inicial (el script lo actualiza en tiempo de ejecución).
        var sb = new System.Text.StringBuilder();
        foreach (var m in muestras) sb.AppendLine($"<color=#4A6278>[  ]</color>  <color=#8FA6BC>{m.nombre}</color>");
        var lista = Texto3D(prog, sb.ToString(), new Vector3(0f, -0.12f, -0.02f), Quaternion.identity, 0.4f, Color.white, 0.8f);
        lista.alignment = TextAlignmentOptions.Left;
        lista.rectTransform.sizeDelta = new Vector2(0.8f, 0.85f);
        var p2 = prog.gameObject.AddComponent<Progreso>();
        p2.muestras = muestras; p2.lista = lista; p2.contador = contador;
    }

    static Transform Totem(Transform padre, string nombre, Vector3 pos, Vector3 mirarA, float ancho, float altoPanel)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.position = new Vector3(pos.x, suelo, pos.z);
        var d = mirarA - pos; d.y = 0f;
        t.rotation = Quaternion.LookRotation(-d.normalized);
        Caja(t, "Pie", new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.04f, 0.35f), mats["Marco"], true);
        Caja(t, "Poste", new Vector3(0f, 0.45f, 0.02f), new Vector3(0.08f, 0.9f, 0.05f), mats["Marco"], true);
        var panel = new GameObject("Panel").transform;
        panel.SetParent(t, false);
        panel.localPosition = new Vector3(0f, 0.9f + altoPanel / 2f, 0f);
        Caja(panel, "Fondo", new Vector3(0f, 0f, 0.01f), new Vector3(ancho, altoPanel, 0.03f), mats["PanelHUD"], true);
        Caja(panel, "Borde", new Vector3(0f, 0f, 0.012f), new Vector3(ancho + 0.03f, altoPanel + 0.03f, 0.028f), mats["EmisivoCian"], false);
        return panel;
    }

    // ------------------------------------------------------------------ portal

    static void CrearPortal(Transform padre, string escena, string texto)
    {
        // Pared izquierda del recibidor (x ≈ -6.5), mirando hacia +x.
        var p = new GameObject("Portal_" + escena).transform;
        p.SetParent(padre, false);
        p.position = new Vector3(-6.42f, suelo, 1.9f);
        p.rotation = Quaternion.Euler(0f, -90f, 0f);    // local -z apunta a +x (hacia el recibidor)
        Caja(p, "Hoja", new Vector3(0f, 1.1f, 0.02f), new Vector3(1.1f, 2.2f, 0.03f), mats["PanelHUD"], false);
        Caja(p, "MarcoI", new Vector3(-0.6f, 1.12f, 0f), new Vector3(0.06f, 2.24f, 0.06f), mats["EmisivoCian"], false);
        Caja(p, "MarcoD", new Vector3(0.6f, 1.12f, 0f), new Vector3(0.06f, 2.24f, 0.06f), mats["EmisivoCian"], false);
        Caja(p, "MarcoS", new Vector3(0f, 2.24f, 0f), new Vector3(1.26f, 0.06f, 0.06f), mats["EmisivoCian"], false);
        var t = Texto3D(p, texto + "\n<size=60%>Camina hacia la puerta</size>", new Vector3(0f, 1.35f, -0.01f), Quaternion.identity, 0.5f, Color.white, 1.0f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        var trig = new GameObject("Trigger");
        trig.transform.SetParent(p, false);
        trig.transform.localPosition = new Vector3(0f, 1f, -0.35f);
        var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1.1f, 2f, 0.5f);
        trig.AddComponent<PuertaEscena>().escena = escena;
    }

    [MenuItem("NanoLab/Añadir portal en Química")]
    public static void AgregarPortalEnQuimica()
    {
        if (!File.Exists(QuimicaPath)) return;
        CrearMateriales();
        var scene = EditorSceneManager.OpenScene(QuimicaPath, OpenSceneMode.Single);
        var viejo = GameObject.Find("Portal");
        if (viejo != null) Object.DestroyImmediate(viejo);
        var rs = Object.FindObjectsOfType<MeshRenderer>();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        suelo = ML.AlturaSuelo(b);
        CrearPortal(new GameObject("Portal").transform, "Microbiologia", "LABORATORIO DE\nMICROBIOLOGÍA");
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Micro] Portal a Microbiología añadido en Laboratorio.unity");
    }

    // ------------------------------------------------------------------ agarre

    static void ConfigurarAgarre(GameObject player)
    {
        if (player == null) return;
        var controllerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRControllerPrefab.prefab");
        foreach (var (anchorName, ctrl) in new[] { ("LeftControllerAnchor", OVRInput.Controller.LTouch), ("RightControllerAnchor", OVRInput.Controller.RTouch) })
        {
            var anchor = player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == anchorName);
            if (anchor == null) { Debug.LogError($"[Micro] No se encontró {anchorName}"); continue; }

            if (controllerPrefab != null)
            {
                var vis = (GameObject)PrefabUtility.InstantiatePrefab(controllerPrefab, anchor);
                var so = new SerializedObject(vis.GetComponent<OVRControllerHelper>());
                so.FindProperty("m_controller").intValue = (int)ctrl;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var mano = new GameObject(ctrl == OVRInput.Controller.LTouch ? "ManoIzquierda" : "ManoDerecha");
            mano.transform.SetParent(anchor, false);
            mano.transform.localPosition = new Vector3(0f, -0.01f, 0.03f);
            var rb = mano.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var sc = mano.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = 0.055f;
            var grabber = mano.AddComponent<OVRGrabber>();
            var s = new SerializedObject(grabber);
            s.FindProperty("m_controller").intValue = (int)ctrl;
            s.FindProperty("m_gripTransform").objectReferenceValue = mano.transform;
            s.FindProperty("m_parentTransform").objectReferenceValue = anchor;
            s.FindProperty("m_player").objectReferenceValue = player;
            s.FindProperty("m_moveHandPosition").boolValue = false;
            s.FindProperty("m_parentHeldObject").boolValue = false;
            var vols = s.FindProperty("m_grabVolumes");
            vols.arraySize = 1;
            vols.GetArrayElementAtIndex(0).objectReferenceValue = sc;
            s.ApplyModifiedPropertiesWithoutUndo();
            mano.AddComponent<ManoNanoLab>().controlador = ctrl;
        }
        Debug.Log("[Micro] Agarre configurado en los dos mandos.");
    }

    // ------------------------------------------------------------------ portaobjetos

    static GameObject CrearPrefabPortaobjetos()
    {
        Directory.CreateDirectory(PrefabDir);
        var go = new GameObject("Portaobjetos");
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.05f; rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(0.075f, 0.004f, 0.026f);
        var g = go.AddComponent<OVRGrabbable>();
        var so = new SerializedObject(g);
        var gp = so.FindProperty("m_grabPoints"); gp.arraySize = 1; gp.GetArrayElementAtIndex(0).objectReferenceValue = bc;
        so.ApplyModifiedPropertiesWithoutUndo();

        Caja(go.transform, "Vidrio", Vector3.zero, new Vector3(0.075f, 0.0015f, 0.025f), mats["VidrioPorta"], false);
        Caja(go.transform, "Esmerilado", new Vector3(0.0275f, 0.0001f, 0f), new Vector3(0.02f, 0.0018f, 0.025f), mats["BlancoMate"], false);
        var frotis = Caja(go.transform, "Frotis", new Vector3(-0.008f, 0.0009f, 0f), new Vector3(0.024f, 0.0004f, 0.017f), mats["Frotis"], false);
        var brillo = Caja(go.transform, "Brillo", Vector3.zero, new Vector3(0.078f, 0.004f, 0.028f), mats["Brillo"], false);

        var p = go.AddComponent<Portaobjetos>();
        p.frotis = frotis.GetComponent<Renderer>();
        p.etiqueta = Texto3D(go.transform, "LIMPIO", new Vector3(0.0275f, 0.0012f, 0f), Quaternion.Euler(90f, 90f, 0f), 0.028f, Color.black, 0.024f);
        p.etiqueta.alignment = TextAlignmentOptions.Center;
        p.etiqueta.fontStyle = FontStyles.Bold;
        p.etiqueta.enableAutoSizing = true; p.etiqueta.fontSizeMin = 0.01f; p.etiqueta.fontSizeMax = 0.028f;
        go.AddComponent<Resaltado>().brillos = new[] { brillo.GetComponent<Renderer>() };

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Portaobjetos.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------ materiales

    static void CrearMateriales()
    {
        Directory.CreateDirectory(MatDir);
        Directory.CreateDirectory(ResDir);
        AssetDatabase.Refresh();
        mats = new Dictionary<string, Material>();
        var std = Shader.Find("Standard");

        Mat("Blanco", std, new Color(0.93f, 0.93f, 0.92f), 0.1f, 0.6f);
        Mat("Negro", std, new Color(0.08f, 0.08f, 0.09f), 0.3f, 0.55f);
        Mat("Oscuro", std, new Color(0.12f, 0.14f, 0.17f), 0.2f, 0.4f);
        Mat("Cromo", std, new Color(0.8f, 0.82f, 0.85f), 0.95f, 0.85f);
        Mat("Marco", std, new Color(0.55f, 0.58f, 0.62f), 0.8f, 0.6f);
        Mat("TuboBlanco", std, new Color(0.95f, 0.96f, 0.98f), 0f, 0.8f);
        Mat("BlancoMate", Shader.Find("Unlit/Color"), Color.white);
        Mat("NegroMate", Shader.Find("Unlit/Color"), Color.black);
        Mat("Amarillo", Shader.Find("Unlit/Color"), new Color(1f, 0.82f, 0.05f));
        Mat("Rojo", Shader.Find("Unlit/Color"), new Color(0.78f, 0.08f, 0.1f));
        Mat("PanelHUD", Shader.Find("Unlit/Color"), new Color(0.02f, 0.07f, 0.13f));

        Emisivo("EmisivoCian", new Color(0.3f, 0.85f, 1f), 1.6f);
        Emisivo("EmisivoAzul", new Color(0.3f, 0.6f, 1f), 1.8f);
        Emisivo("EmisivoRosa", new Color(1f, 0.45f, 0.6f), 1.8f);
        Emisivo("LuzBlanca", new Color(1f, 0.97f, 0.9f), 1.5f);

        Transparente("Vidrio", new Color(0.85f, 0.95f, 1f, 0.22f), 0.9f);
        Transparente("VidrioPared", new Color(0.8f, 0.92f, 1f, 0.12f), 0.95f);
        Transparente("VidrioPorta", new Color(0.85f, 0.97f, 1f, 0.35f), 0.95f);
        Transparente("LiquidoAzul", new Color(0.2f, 0.55f, 1f, 0.7f), 0.8f);
        Transparente("Frotis", new Color(0.9f, 0.4f, 0.6f, 0.85f), 0.3f);

        var brillo = Shader.Find("NanoLab/Brillo");
        Mat("Brillo", brillo, new Color(0.25f, 0.8f, 1f));
        Mat("Fantasma", brillo, new Color(0.3f, 0.9f, 1f)).SetFloat("_Base", 0.6f);
        var haz = Mat("Haz", brillo, new Color(0.15f, 0.5f, 0.8f));
        haz.SetFloat("_Base", 0.15f); haz.SetFloat("_Pulso", 2f); haz.SetFloat("_Grosor", 0f);

        var bio = Mat("Biohazard", Shader.Find("Unlit/Transparent"), Color.white);
        bio.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NanoLab/Muestras/Imagenes/biohazard.png");
        Mat("MicroEstacion", Shader.Find("Unlit/Texture"), Color.white);

        var holo = Shader.Find("NanoLab/Holograma");
        var azul = MatEn(ResDir, "Holograma_Azul", holo);
        azul.SetColor("_Tint", new Color(0.3f, 0.7f, 1f)); azul.SetFloat("_TintAmount", 0.85f);
        azul.SetColor("_RimColor", new Color(0.5f, 0.9f, 1f)); azul.SetFloat("_RimIntensity", 1.8f);
        var color = MatEn(ResDir, "Holograma_Color", holo);
        color.SetFloat("_TintAmount", 0.15f);
        color.SetColor("_RimColor", new Color(0.45f, 0.85f, 1f)); color.SetFloat("_RimIntensity", 1.3f);
        AssetDatabase.SaveAssets();
    }

    static Material MatEn(string dir, string nombre, Shader shader)
    {
        string path = $"{dir}/{nombre}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Mat(string nombre, Shader shader, Color c, float metal = 0f, float liso = 0.5f)
    {
        var m = MatEn(MatDir, nombre, shader);
        m.color = c;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", liso);
        mats[nombre] = m;
        return m;
    }

    static void Emisivo(string nombre, Color c, float intensidad)
    {
        var m = Mat(nombre, Shader.Find("Standard"), c * 0.5f, 0f, 0.5f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensidad);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
    }

    static void Transparente(string nombre, Color c, float liso)
    {
        var m = Mat(nombre, Shader.Find("Standard"), c, 0f, liso);
        m.SetFloat("_Mode", 3);   // Transparent
        m.SetInt("_SrcBlend", (int)BlendMode.One);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.DisableKeyword("_ALPHABLEND_ON");
        m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;
    }

    static Material MaterialTapa(MuestraData v)
    {
        var m = MatEn(MatDir, "Tapa_" + v.name, Shader.Find("Standard"));
        m.color = v.color;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", v.color * 0.6f);
        return m;
    }

    // ------------------------------------------------------------------ helpers

    static Transform Grupo(Transform padre, string nombre)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    static Transform Vacio(Transform padre, string nombre, Vector3 pos)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = pos;
        return t;
    }

    static GameObject Caja(Transform padre, string nombre, Vector3 pos, Vector3 tam, Material mat, bool collider)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nombre;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = tam;
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return g;
    }

    static GameObject Cilindro(Transform padre, string nombre, Vector3 pos, Vector3 tam, Quaternion rot, Material mat)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = nombre;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.transform.localScale = tam;
        var r = g.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return g;
    }

    // Texto 3D: 'tam' es la altura aproximada de la letra en décimas de metro (fontSize de TMP).
    static TextMeshPro Texto3D(Transform padre, string texto, Vector3 pos, Quaternion rot, float tam, Color color, float ancho)
    {
        var go = new GameObject("Texto");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        var t = go.AddComponent<TextMeshPro>();
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.enableWordWrapping = true;
        t.rectTransform.sizeDelta = new Vector2(ancho, tam * 0.6f);
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    // ------------------------------------------------------------------ capturas

    [MenuItem("NanoLab/Capturas de microbiología")]
    public static void Capturas()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        float y = ML.AlturaSuelo(ML.BoundsEscena());
        ML.Capturar(dir, "micro_1_entrada", new Vector3(-3.3f, y + 1.65f, 2.24f), Quaternion.Euler(8f, 24f, 0f) * Vector3.forward);
        ML.Capturar(dir, "micro_2_isla", new Vector3(-1.8f, y + 1.6f, 14.1f), new Vector3(0f, -0.55f, -1f));
        ML.Capturar(dir, "micro_3_puesto", new Vector3(-2.55f, y + 1.35f, 13.25f), new Vector3(0f, -0.75f, -1f));
        ML.Capturar(dir, "micro_4_bsl_fuera", new Vector3(-6.2f, y + 1.7f, 9.3f), new Vector3(-0.55f, -0.12f, 1f));
        ML.Capturar(dir, "micro_5_bsl_dentro", new Vector3(-8.3f, y + 1.65f, 12.7f), new Vector3(0.25f, -0.2f, 1f));
        ML.Capturar(dir, "micro_6_cepario", new Vector3(5.2f, y + 1.5f, 10.0f), new Vector3(0f, -0.6f, 1f));
        ML.Capturar(dir, "micro_7_tutorial", new Vector3(-3.3f, y + 1.65f, 2.24f), Quaternion.Euler(10f, 50f, 0f) * Vector3.forward);
        ML.Capturar(dir, "micro_8_progreso", new Vector3(-3.3f, y + 1.65f, 2.24f), Quaternion.Euler(10f, -10f, 0f) * Vector3.forward);
        VistaCenital.Renderizar(ScenePath);
        Debug.Log("[Micro] Capturas guardadas.");
    }

    // Simula lo que se ve por el ocular (HUD) y el holograma, sin visor.
    [MenuItem("NanoLab/Probar microscopio (capturas)")]
    public static void ProbarMicroscopio()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        var holoAzul = Resources.Load<Material>("NanoLab/Holograma_Azul");
        foreach (var nombre in new[] { "SARSCoV2", "Ecoli" })
        {
            var m = AssetDatabase.LoadAssetAtPath<MuestraData>($"Assets/NanoLab/Muestras/{nombre}.asset");
            var ojo = new GameObject("OjoPrueba").transform;
            ojo.position = new Vector3(0f, 100f, 0f);
            var hud = HUDMicroscopio.Obtener(ojo, holoAzul);
            hud.MostrarInmediato(m);
            if (m.tipo == TipoMuestra.Virus) VisorVirus.Obtener().RenderAhora();
            Canvas.ForceUpdateCanvases();
            var camGo = new GameObject("CamPrueba");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.SetParent(ojo, false);
            cam.fieldOfView = 68f; cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.cullingMask &= ~(1 << VisorVirus.Capa);
            var rt = new RenderTexture(1600, 800, 24);
            cam.targetTexture = rt; cam.aspect = 2f;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 800, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 800), 0, 0);
            File.WriteAllBytes(Path.Combine(dir, $"hud_{nombre}.png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(camGo);
        }

        // Holograma de la estación con un virus.
        var est = Object.FindObjectOfType<EstacionAnalisis3D>();
        if (est != null)
        {
            var m = AssetDatabase.LoadAssetAtPath<MuestraData>("Assets/NanoLab/Muestras/Influenza.asset");
            var holoColor = Resources.Load<Material>("NanoLab/Holograma_Color");
            ModeloVirus.Instanciar(m, holoColor, est.holograma, est.diametro);
            est.haz.SetActive(true);
            var pos = est.holograma.position + new Vector3(0f, -0.1f, -1.6f);
            ML.Capturar(dir, "holo_estacion", pos, est.holograma.position - pos);
        }
        Debug.Log("[Micro] Capturas del HUD y del holograma guardadas.");
    }
}
