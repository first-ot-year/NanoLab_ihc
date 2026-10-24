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
// microscopios en las islas, cepario, sala de vidrio de alto riesgo con la estación 3D,
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
    internal static readonly (float x, float z, bool haciaMasZ)[] Puestos =
    {
        (-2.55f, 12.62f, true), (-1.15f, 12.62f, true), (-2.55f, 12.12f, false), (-1.15f, 12.12f, false),
        (-2.55f, 7.37f, true),  (-1.15f, 7.37f, true),  (-2.55f, 6.87f, false),  (-1.15f, 6.87f, false),
        (4.55f, 11.75f, true),  (5.85f, 11.75f, true),
    };
    internal static readonly Vector3 CeparioCentro = new Vector3(5.2f, 0f, 11.05f);   // fila inferior de la isla derecha
    // La zona de alto riesgo es una sala de vidrio aparte (MontarSalaAltoRiesgo), detrás de la pared norte.
    const float RadioJugador = 0.25f;

    internal static readonly string[] Aparatos =
    {
        "erlenmeyer", "beaker", "florence", "test_tube", "spirit_lamp", "crucible", "funnel", "boiling_liquid",
        "graduated_cylinder", "glass support", "stand glass", "china dish", "forceps", "dropper", "analytical", "liquids", "lab glass",
    };

    internal static Dictionary<string, Material> mats;
    internal static float suelo;

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
        MontarSalaAltoRiesgo.Construir(raiz, virus, slidePrefab);
        CrearTutorialYProgreso(Grupo(raiz, "Bienvenida"), muestras);
        CrearPortal(Grupo(raiz, "Portal"), "Laboratorio", "LABORATORIO\nDE QUÍMICA");
        // Sala anexa con el Caso 01 (detrás de la pared este).
        MontarCentroExperimentacion.Construir(raiz, slidePrefab);
        Physics.SyncTransforms();
        AcercarMicroscopios.Aplicar(scene);   // platinas a 15 cm del borde de la mesa
        Utileria.ReubicarPuestos(bacterias);   // bandeja, desinfectante y cultivos junto al microscopio

        // Jugador: rig de Meta (OVRCameraRigInteraction) con agarre, teletransporte y objetos agarrables.
        ConfigurarMetaISDK.Configurar(scene);
        MontarMusica.Agregar();

        ML.ConfigurarLuces("Microbiologia_Lighting");
        MontarCentroExperimentacion.LucesTiempoReal();
        ML.CrearSondas(ML.BoundsEscena(), suelo);   // incluye el anexo
        AgruparLab.Agrupar(scene);
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
        MontarCentroExperimentacion.Capturas();
    }

    // ------------------------------------------------------------------ sala

    internal static int QuitarAparatos()
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

    internal static float AlturaEn(float x, float z, float desde = 1.6f)
    {
        if (Physics.Raycast(new Vector3(x, desde, z), Vector3.down, out var hit, 3f)) return hit.point.y;
        return suelo;
    }

    internal static float Techo(float x, float z)
    {
        if (Physics.Raycast(new Vector3(x, suelo + 1.5f, z), Vector3.up, out var hit, 5f)) return hit.point.y;
        return suelo + 3f;
    }

    // ------------------------------------------------------------------ puestos

    internal static void CrearPuesto(Transform padre, int numero, Vector3 punto, float yaw, GameObject slidePrefab)
    {
        float top = AlturaEn(punto.x, punto.z);
        var puesto = new GameObject($"Puesto_{numero:00}").transform;
        puesto.SetParent(padre, false);
        puesto.position = new Vector3(punto.x, top, punto.z);
        puesto.rotation = Quaternion.Euler(0f, yaw, 0f);

        CrearMicroscopio(puesto, new Vector3(0f, 0f, -0.05f));

        // Bandeja con 3 portaobjetos limpios (a la derecha del microscopio visto por el usuario).
        Utileria.Bandeja(puesto, new Vector3(-0.3f, 0f, 0.02f), new[] { -0.04f, -0.005f, 0.03f });
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

    internal static void CrearMicroscopio(Transform padre, Vector3 pos)
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

    internal static GameObject Ayuda(Transform padre, string nombre, string texto, Vector3 pos, Color color)
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

    internal static void CrearDesinfectante(Transform padre, Vector3 pos)
    {
        var v = new GameObject("Desinfectante").transform;
        v.SetParent(padre, false);
        v.localPosition = pos;
        var brillo = Utileria.FrascoLavador(v);   // frasco lavador de etanol al 70 %
        v.gameObject.AddComponent<BoxCollider>();
        Utileria.AjustarTriggerFrasco(v.gameObject);
        v.gameObject.AddComponent<FuenteMuestra>().esDesinfectante = true;
        v.gameObject.AddComponent<Resaltado>().brillos = new[] { brillo };
        var t = Texto3D(v, "DESINFECTANTE", new Vector3(0f, 0.25f, 0f), Quaternion.identity, 0.16f, new Color(0.5f, 0.85f, 1f), 0.3f);
        t.alignment = TextAlignmentOptions.Center;
        t.gameObject.AddComponent<Cartel>();
    }

    // ------------------------------------------------------------------ cepario

    internal static void CrearCepario(Transform padre, MuestraData[] bacterias, GameObject slidePrefab)
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
        Utileria.Bandeja(c, new Vector3(0.62f, 0f, 0.05f), new[] { -0.04f, -0.005f, 0.03f });
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

    internal static void Etiqueta(Transform padre, string titulo, string sub, Vector3 pos, Color color)
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

    // ------------------------------------------------------------------ zona de alto riesgo (piezas compartidas)

    internal static void Pared(Transform padre, Vector3 a, Vector3 b, float alto)
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

    internal static void Senal(Transform padre, string nombre, Vector3 pos, Quaternion rot, string texto, float tam)
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

    internal static void CartelEPP(Transform padre, Vector3 pos)
    {
        var s = new GameObject("CartelEPP").transform;
        s.SetParent(padre, false);
        s.position = pos;   // sin rotar: el texto mira hacia -z (hacia la sala)
        Caja(s, "Fondo", Vector3.zero, new Vector3(0.9f, 0.6f, 0.01f), mats["Rojo"], false);
        var t = Texto3D(s, "USO OBLIGATORIO DE EPP\n<size=70%>Bata · Guantes · Gafas · Mascarilla N95</size>", new Vector3(0f, 0f, -0.007f), Quaternion.identity, 0.55f, Color.white, 0.85f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
    }

    // 'pedestal' y 'consolaPos' en planta (se ignora la y); el usuario se coloca en -z de la consola.
    internal static void CrearEstacion3D(Transform padre, Vector3 pedestal, Vector3 consolaPos)
    {
        var est = new GameObject("EstacionAnalisis3D").transform;
        est.SetParent(padre, false);
        est.position = new Vector3(pedestal.x, suelo, pedestal.z);

        // Pedestal y proyector.
        Cilindro(est, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.45f, 0.55f), Quaternion.identity, mats["Negro"]).AddComponent<CapsuleCollider>();
        Cilindro(est, "AnilloLuz", new Vector3(0f, 0.905f, 0f), new Vector3(0.5f, 0.006f, 0.5f), Quaternion.identity, mats["EmisivoCian"]);
        var haz = Cilindro(est, "Haz", new Vector3(0f, 1.25f, 0f), new Vector3(0.42f, 0.34f, 0.42f), Quaternion.identity, mats["Haz"]);
        var holo = Vacio(est, "Holograma", new Vector3(0f, 1.6f, 0f));

        // Consola con ranura y botones.
        var consola = new GameObject("Consola").transform;
        consola.SetParent(est, false);
        consola.position = new Vector3(consolaPos.x, suelo, consolaPos.z);
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
        e.plataforma = PlataformaHolo(est);
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
        var infoPos = new Vector3(pedestal.x + 1.65f, 0f, pedestal.z - 0.25f);
        info.position = infoPos + Vector3.up * (suelo + 1.55f);
        var haciaUsuario = new Vector3(pedestal.x - 0.2f, 0f, pedestal.z - 1.85f) - infoPos;
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

    // Plataforma holográfica sobre el pedestal: anillos con flechas de giro y rejilla luminosa.
    internal static Transform PlataformaHolo(Transform est)
    {
        var p = new GameObject("Plataforma").transform;
        p.SetParent(est, false);
        p.localPosition = new Vector3(0f, 0.915f, 0f);
        Transform Disco(string nombre, Material mat, float tam, float y)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = nombre;
            q.transform.SetParent(p, false);
            q.transform.localPosition = new Vector3(0f, y, 0f);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * tam;
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return q.transform;
        }
        Disco("Rejilla", mats["RejillaHolo"], 0.95f, 0f);
        Disco("Anillos", mats["PlataformaHolo"], 1.25f, 0.003f);
        return p;
    }

    internal static BotonFisico Boton(Transform padre, string texto, Vector3 pos)
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

    // ------------------------------------------------------------------ bienvenida

    internal static void CrearTutorialYProgreso(Transform padre, MuestraData[] muestras)
    {
        var spawn = new Vector3(-3.3f, 0f, 2.24f);

        var tut = Totem(padre, "Tutorial", new Vector3(-1.9f, 0f, 3.7f), spawn, 1.4f, 1.5f);
        var tt = Texto3D(tut, "LABORATORIO DE MICROBIOLOGÍA", new Vector3(0f, 0.64f, -0.02f), Quaternion.identity, 0.75f, new Color(0.4f, 0.9f, 1f), 1.3f);
        tt.alignment = TextAlignmentOptions.Center; tt.fontStyle = FontStyles.Bold;
        string[] pasos =
        {
            "<b>Coge un portaobjetos</b>\nAcerca el mando y aprieta el gatillo o el grip.",
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
        var extra = Texto3D(tut, "Los virus están en la <color=#FF8CA0>sala de vidrio de alto riesgo</color> (al fondo, a la izquierda). Toca el <color=#8FD8FF>desinfectante</color> para limpiar un portaobjetos.\n<color=#FFD25A>¡Nuevo! CENTRO DE EXPERIMENTACIÓN</color> (puerta de la pared derecha): resuelve el CASO 01.",
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

        MontarIntroduccion.Crear(padre);   // RF-01: introducción, orientación y selección de muestra
    }

    internal static Transform Totem(Transform padre, string nombre, Vector3 pos, Vector3 mirarA, float ancho, float altoPanel)
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

    internal static void CrearPortal(Transform padre, string escena, string texto)
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
        AgruparLab.Agrupar(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Micro] Portal a Microbiología añadido en Laboratorio.unity");
    }

    // ------------------------------------------------------------------ portaobjetos

    internal static GameObject CrearPrefabPortaobjetos()
    {
        Directory.CreateDirectory(PrefabDir);
        var go = new GameObject("Portaobjetos");
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.05f; rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(0.075f, 0.003f, 0.026f);

        // Vidrio de 75 × 25 × 1 mm con canto, zona esmerilada y frotis (Utileria).
        var (frotis, brillo, etiqueta) = Utileria.Portaobjetos(go.transform);
        var p = go.AddComponent<Portaobjetos>();
        p.frotis = frotis;
        p.etiqueta = etiqueta;
        p.brillo = brillo;
        ConfigurarMetaISDK.HacerAgarrable(go, 0.05f);   // Grabbable + interactables del Interaction SDK

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Portaobjetos.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------ materiales

    internal static void CrearMateriales()
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

        Utileria.Materiales();

        var bio = Mat("Biohazard", Shader.Find("Unlit/Transparent"), Color.white);
        bio.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NanoLab/Muestras/Imagenes/biohazard.png");
        Mat("MicroEstacion", Shader.Find("Unlit/Texture"), Color.white);

        var holo = Shader.Find("NanoLab/Holograma");
        var azul = MatEn(ResDir, "Holograma_Azul", holo);
        azul.SetColor("_Tint", new Color(0.3f, 0.7f, 1f)); azul.SetFloat("_TintAmount", 0.85f);
        azul.SetColor("_RimColor", new Color(0.5f, 0.9f, 1f)); azul.SetFloat("_RimIntensity", 1.8f);
        var color = MatEn(ResDir, "Holograma_Color", holo);
        // Colores reales con un borde holográfico sutil (como una visualización científica 3D).
        color.SetFloat("_TintAmount", 0.04f);
        color.SetColor("_RimColor", new Color(0.45f, 0.85f, 1f)); color.SetFloat("_RimIntensity", 0.55f);
        color.SetFloat("_Especular", 0.4f);
        azul.SetFloat("_Especular", 0.25f);

        // Luz aditiva: resplandor del visor del microscopio (Resources) y plataforma de la estación 3D.
        var aditivo = Shader.Find("NanoLab/Aditivo");
        Material Aditivo(string dir, string nombre, string textura, Color c, float intensidad)
        {
            var m = MatEn(dir, nombre, aditivo);
            m.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/NanoLab/Texturas/{textura}.png");
            m.color = c;
            m.SetFloat("_Intensidad", intensidad);
            mats[nombre] = m;
            return m;
        }
        Aditivo(ResDir, "Resplandor", "Resplandor", new Color(0.2f, 0.5f, 1f), 0.55f);
        MatEn(ResDir, "LineaPuntero", Shader.Find("Unlit/Color")).color = new Color(0.35f, 0.85f, 1f);
        MatEn(ResDir, "JaulaConfort", Shader.Find("NanoLab/JaulaConfort"));   // marco de reposo al caminar
        MatEn(ResDir, "LineaPunteroOk", Shader.Find("Unlit/Color")).color = new Color(0.35f, 1f, 0.55f);
        Aditivo(MatDir, "PlataformaHolo", "PlataformaHolo", new Color(0.3f, 0.8f, 1f), 1.1f);
        Aditivo(MatDir, "RejillaHolo", "RejillaHolo", new Color(0.25f, 0.6f, 0.95f), 0.7f);
        AssetDatabase.SaveAssets();
    }

    internal static Material MatEn(string dir, string nombre, Shader shader)
    {
        string path = $"{dir}/{nombre}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.shader = shader;
        EditorUtility.SetDirty(m);
        return m;
    }

    internal static Material Mat(string nombre, Shader shader, Color c, float metal = 0f, float liso = 0.5f)
    {
        var m = MatEn(MatDir, nombre, shader);
        m.color = c;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", liso);
        mats[nombre] = m;
        return m;
    }

    internal static void Emisivo(string nombre, Color c, float intensidad)
    {
        var m = Mat(nombre, Shader.Find("Standard"), c * 0.5f, 0f, 0.5f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensidad);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
    }

    internal static void Transparente(string nombre, Color c, float liso)
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

    internal static Material MaterialTapa(MuestraData v)
    {
        var m = MatEn(MatDir, "Tapa_" + v.name, Shader.Find("Standard"));
        m.color = v.color;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", v.color * 0.6f);
        return m;
    }

    // ------------------------------------------------------------------ helpers

    internal static Transform Grupo(Transform padre, string nombre)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
    }

    internal static Transform Vacio(Transform padre, string nombre, Vector3 pos)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = pos;
        return t;
    }

    internal static GameObject Caja(Transform padre, string nombre, Vector3 pos, Vector3 tam, Material mat, bool collider)
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

    internal static GameObject Cilindro(Transform padre, string nombre, Vector3 pos, Vector3 tam, Quaternion rot, Material mat)
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
    internal static TextMeshPro Texto3D(Transform padre, string texto, Vector3 pos, Quaternion rot, float tam, Color color, float ancho)
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
        ML.Capturar(dir, "micro_4_bsl_fuera", new Vector3(-5.6f, y + 1.7f, 11.0f), new Vector3(-0.3f, -0.08f, 1f));
        ML.Capturar(dir, "micro_5_bsl_dentro", new Vector3(-9.2f, y + 1.65f, 17.3f), new Vector3(0.35f, -0.2f, 1f));
        ML.Capturar(dir, "micro_9_bsl_fondo", new Vector3(-3.4f, y + 1.8f, 17.4f), new Vector3(-0.8f, -0.25f, 1f));
        ML.Capturar(dir, "micro_6_cepario", new Vector3(5.2f, y + 1.5f, 10.0f), new Vector3(0f, -0.6f, 1f));
        ML.Capturar(dir, "micro_7_tutorial", new Vector3(-3.3f, y + 1.65f, 2.24f), Quaternion.Euler(10f, 50f, 0f) * Vector3.forward);
        ML.Capturar(dir, "micro_8_progreso", new Vector3(-3.3f, y + 1.65f, 2.24f), Quaternion.Euler(10f, -10f, 0f) * Vector3.forward);
        VistaCenital.Renderizar(ScenePath);
        Debug.Log("[Micro] Capturas guardadas.");
    }

    // Recorre a pie (cápsula del jugador) rutas clave y avisa de lo que bloquee el paso.
    [MenuItem("NanoLab/Probar recorrido del jugador")]
    public static void ProbarRecorrido()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Physics.SyncTransforms();
        float y = ML.AlturaSuelo(ML.BoundsEscena());
        var rutas = new Dictionary<string, Vector2[]>
        {
            { "Entrada -> sala de alto riesgo -> criotubos", new[] { new Vector2(-3.3f, 2.24f), new Vector2(-3.3f, 4.8f), new Vector2(-5.4f, 5.6f), new Vector2(-5.4f, 9.8f), new Vector2(-8.65f, 10.6f), new Vector2(-9.2f, 12.0f), new Vector2(-9.2f, 17.6f), new Vector2(-8.0f, 20.0f), new Vector2(-8.6f, 23.6f) } },
            { "Sala de alto riesgo -> frente a la consola", new[] { new Vector2(-9.2f, 17.6f), new Vector2(-6.6f, 19.95f) } },
            { "Entrada -> puesto 1", new[] { new Vector2(-3.3f, 2.24f), new Vector2(-3.3f, 4.8f), new Vector2(0.8f, 5.0f), new Vector2(0.8f, 14.5f), new Vector2(-2.55f, 14.5f), new Vector2(-2.55f, 13.65f) } },
            { "Entrada -> cepario", new[] { new Vector2(-3.3f, 2.24f), new Vector2(-3.3f, 4.8f), new Vector2(0.8f, 5.0f), new Vector2(0.8f, 9.4f), new Vector2(5.2f, 9.4f), new Vector2(5.2f, 10.1f) } },
            { "Entrada -> puerta del centro de experimentación", new[] { new Vector2(-3.3f, 2.24f), new Vector2(-3.3f, 4.8f), new Vector2(0.8f, 5.0f), new Vector2(0.8f, 8.1f), new Vector2(8.0f, 8.1f) } },
            { "Centro: puerta -> banco -> tinción -> microscopía", new[] { new Vector2(8.0f, 8.1f), new Vector2(10.5f, 3.5f), new Vector2(10.5f, 1.3f), new Vector2(10.5f, 2.4f), new Vector2(20.5f, 2.4f) } },
            { "Centro: microscopía -> biblioteca -> terminal", new[] { new Vector2(20.5f, 2.4f), new Vector2(22.6f, 5.0f), new Vector2(22.6f, 12.0f), new Vector2(20.5f, 15.4f) } },
            { "Centro: terminal -> prensa -> cámara -> archivo", new[] { new Vector2(20.5f, 15.4f), new Vector2(20.5f, 14.4f), new Vector2(15.5f, 14.4f), new Vector2(15.5f, 11.5f), new Vector2(13.4f, 10.0f), new Vector2(13.4f, 8.5f), new Vector2(10.2f, 12.0f), new Vector2(10.2f, 15.5f) } },
        };
        int fallos = 0;
        foreach (var r in rutas)
        {
            string bloqueo = null;
            for (int i = 0; i + 1 < r.Value.Length && bloqueo == null; i++)
            {
                var a = r.Value[i]; var b = r.Value[i + 1];
                int pasos = Mathf.CeilToInt(Vector2.Distance(a, b) / 0.1f);
                for (int k = 0; k <= pasos; k++)
                {
                    var p = Vector2.Lerp(a, b, k / (float)pasos);
                    var pies = new Vector3(p.x, y, p.y);
                    var hits = Physics.OverlapCapsule(pies + Vector3.up * (RadioJugador + 0.35f), pies + Vector3.up * 1.75f, RadioJugador, ~0, QueryTriggerInteraction.Ignore)
                        .Where(h => h.GetComponentInParent<OVRPlayerController>() == null && h.transform.root.GetComponentInChildren<OVRCameraRig>(true) == null).ToArray();   // ni el propio jugador
                    if (hits.Length > 0) { bloqueo = $"bloqueado en ({p.x:F2}, {p.y:F2}) por {string.Join(", ", hits.Select(h => h.name).Distinct())}"; break; }
                }
            }
            if (bloqueo != null) fallos++;
            Debug.Log($"[Recorrido] {r.Key}: {(bloqueo ?? "OK, se puede pasar")}");
        }
        Debug.Log($"[Recorrido] {(fallos == 0 ? "Todas las rutas son transitables." : fallos + " ruta(s) bloqueada(s).")}");
    }

    // Pone la cabeza donde la pondría el usuario al mirar por el ocular de un microscopio real de
    // la escena (sala y anexo) y captura lo que vería: el HUD no debe quedar tapado por el
    // microscopio, la mesa ni las manos (Logs/Capturas/ocular_*.png).
    [MenuItem("NanoLab/Probar visor desde el ocular (capturas)")]
    public static void ProbarOcular()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        var holoAzul = Resources.Load<Material>("NanoLab/Holograma_Azul");
        var muestra = AssetDatabase.LoadAssetAtPath<MuestraData>("Assets/NanoLab/Muestras/Ecoli.asset");
        var micros = Object.FindObjectsOfType<MicroscopioOptico>(true);
        foreach (var micro in new[] { micros.Where(m => !m.modoReto).OrderBy(m => m.name).FirstOrDefault(), micros.FirstOrDefault(m => m.modoReto) })
        {
            if (micro == null) continue;
            // Ojo un poco por encima y por detrás del ocular (dentro del radio de 0,22 m que activa el HUD),
            // mirando hacia la platina.
            var haciaUsuario = Vector3.ProjectOnPlane(micro.transform.forward, Vector3.up).normalized;
            var ojo = new GameObject("OjoPrueba").transform;
            ojo.position = micro.ocular.position + Vector3.up * 0.06f + haciaUsuario * 0.1f;
            ojo.rotation = Quaternion.LookRotation(micro.platina.anclaje.position - ojo.position);
            var hud = HUDMicroscopio.Obtener(ojo, holoAzul);
            hud.MostrarInmediato(muestra);
            Canvas.ForceUpdateCanvases();
            foreach (var g in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Where(g => g.name == "Vista" || g.name == "Fondo"))
            {
                var m = g.materialForRendering;
                Debug.Log($"[Micro] {g.name}: material {m.name} shader {m.shader.name} ZTest {m.GetInt("unity_GUIZTestMode")} cola {m.renderQueue}");
            }
            var camGo = new GameObject("CamPrueba");
            var cam = camGo.AddComponent<Camera>();
            cam.transform.SetParent(ojo, false);
            cam.fieldOfView = 90f; cam.nearClipPlane = 0.01f;
            cam.cullingMask &= ~(1 << VisorVirus.Capa);
            cam.cullingMask &= ~(1 << AvatarBaymax.CapaCabeza);
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt; cam.aspect = 16f / 9f;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            File.WriteAllBytes(Path.Combine(dir, $"ocular_{(micro.modoReto ? "centro" : "sala")}.png"), tex.EncodeToPNG());
            RenderTexture.active = null;
            Object.DestroyImmediate(camGo);
            hud.transform.SetParent(null, false);
            Object.DestroyImmediate(ojo.gameObject);
        }
        Debug.Log("[Micro] Capturas desde el ocular guardadas.");
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
            void Guardar(string archivo)
            {
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 800, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1600, 800), 0, 0);
                File.WriteAllBytes(Path.Combine(dir, archivo), tex.EncodeToPNG());
                RenderTexture.active = null;
            }
            Guardar($"hud_{nombre}.png");
            if (m.tipo == TipoMuestra.Virus)
            {
                hud.CambiarPestana(2);   // COMPOSICIÓN: leyenda de colores
                VisorVirus.Obtener().RenderAhora();
                Guardar($"hud_{nombre}_composicion.png");
            }
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
