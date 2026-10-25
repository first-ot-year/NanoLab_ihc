using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MM = MontarMicrobiologia;
using ML = MontarLaboratorio;

// Centro de Experimentación Microbiológica: sala anexa detrás de la pared este de Microbiologia.unity
// con el Caso 01 completo (banco de muestras > tinción de Gram > microscopía > biblioteca > hipótesis >
// cápsula > cámara experimental > comparación antes/después > archivo).
//
// Distribución (planta, x hacia la derecha, z hacia arriba; puerta en la pared oeste):
//   z 17 ┌──────── 8 ARCHIVO ─────── 6 PRENSA ──── 5 TERMINAL ───┐
//        │ recepción                                              │
//   puerta      (objetivo)          7 CÁMARA                4 BIBLIOTECA
//        │ recepción                                              │
//   z  0 └──────── 1 BANCO ───────── 2 TINCIÓN ── 3 MICROSCOPÍA ───┘
//       x 6.9                                                  x 24.1
public static class MontarCentroExperimentacion
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Pack = "Assets/3D Laboratory Environment with Appratus/Prefabs/";
    const string RetoDir = "Assets/NanoLab/Retos";
    const string RetoPath = RetoDir + "/Caso01.asset";
    const string ImgDir = "Assets/NanoLab/Muestras/Imagenes";
    const string MatDir = "Assets/NanoLab/Materiales/Micro";
    public const string NombreRaiz = "CentroExperimentacion";

    // Medidas de la sala de Microbiología (NanoLab/Diagnóstico): pared este en x 6,63–6,87, techo a 4,25 m.
    const float ParedX0 = 6.63f, ParedX1 = 6.87f, AltoSala = 4.25f, Grosor = 0.25f;
    // Interior del anexo (el suelo del pack mide 17,5 × 17,5 m).
    public const float X0 = 6.87f, X1 = 24.14f, Z0 = -0.25f, Z1 = 17.26f;
    const float PuertaZ0 = 7.3f, PuertaZ1 = 8.9f, AltoPuerta = 2.25f;
    public static readonly Vector3 CentroCamara = new Vector3(15.5f, 0f, 8.5f);

    static readonly Color Cian = new Color(0.35f, 0.85f, 1f);
    static readonly Color Gris = new Color(0.66f, 0.75f, 0.85f);
    static readonly Color Amarillo = new Color(1f, 0.82f, 0.35f);
    static readonly Color[] ColorZona =
    {
        new Color(0.3f, 0.85f, 1f), new Color(0.62f, 0.38f, 1f), new Color(0.35f, 0.55f, 1f), new Color(1f, 0.6f, 0.2f),
        new Color(1f, 0.35f, 0.75f), new Color(0.95f, 0.45f, 0.95f), new Color(0.3f, 1f, 0.6f), new Color(1f, 0.85f, 0.4f),
    };

    static Dictionary<string, Material> M => MM.mats;
    static float suelo;
    static RetoData reto;

    // ================================================================== entrada

    [MenuItem("NanoLab/Reconstruir solo el centro de experimentación", false, 2)]
    public static void Reconstruir()
    {
        ML.GenerarUVsDeLightmap();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CrearMuestras.Todas();
        AssetDatabase.SaveAssets();
        MM.CrearMateriales();
        var slidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NanoLab/Prefabs/Portaobjetos.prefab");
        foreach (var t in Object.FindObjectsOfType<Transform>(true).Where(t => t != null && t.name == NombreRaiz).ToArray())
            Object.DestroyImmediate(t.gameObject);
        var sondas = Object.FindObjectsOfType<LightProbeGroup>(true).FirstOrDefault(l => l.name == "SondasDeLuz");
        if (sondas != null) Object.DestroyImmediate(sondas.gameObject);

        var padre = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Microbiologia" && t.parent != null && t.parent.name.StartsWith("LAB_"));
        if (padre == null) padre = new GameObject("Microbiologia").transform;
        Construir(padre, slidePrefab);
        AcercarMicroscopios.Aplicar(scene);
        ConfigurarMetaISDK.ActualizarPrefabPortaobjetos();
        ConfigurarMetaISDK.Configurar(scene);
        ML.ConfigurarLuces("Microbiologia_Lighting");
        LucesTiempoReal();
        ML.CrearSondas(ML.BoundsEscena(), suelo);
        AgruparLab.Agrupar(scene);
        EditorSceneManager.SaveScene(scene);
        if (!Application.isBatchMode || !System.Environment.GetCommandLineArgs().Contains("-sinBake"))
        {
            Debug.Log("[Centro] Horneando iluminación...");
            Debug.Log(Lightmapping.Bake() ? "[Centro] Bake completado." : "[Centro] El bake falló.");
            EditorSceneManager.SaveScene(scene);
        }
        Capturas();
    }

    public static void Construir(Transform padre, GameObject slidePrefab)
    {
        suelo = ML.AlturaSuelo(ML.BoundsEscena());
        MM.suelo = suelo;
        MaterialesCentro();
        reto = CrearReto();

        var raiz = MM.Grupo(padre, NombreRaiz);
        AbrirPuerta(raiz);
        Sala(MM.Grupo(raiz, "Sala"));
        Physics.SyncTransforms();

        var estaciones = new EstacionGuia[8];
        var rm = new GameObject("RetoManager").AddComponent<RetoManager>();
        rm.transform.SetParent(raiz, false);
        rm.reto = reto;
        rm.zona = new Bounds(new Vector3((X0 + X1) / 2f, suelo + 2f, (Z0 + Z1) / 2f), new Vector3(X1 - X0, 4f, Z1 - Z0));
        rm.materialPanel = M["PanelHUD"];

        var est = MM.Grupo(raiz, "Estaciones");
        var placa = Banco(est, out estaciones[0]);
        Tincion(est, slidePrefab, out estaciones[1]);
        Microscopia(est, slidePrefab, out estaciones[2]);
        rm.fichas = Biblioteca(est, out estaciones[3]);
        rm.terminal = Terminal(est, out estaciones[4]);
        var prensa = Prensa(est, out estaciones[5]);
        rm.camara = Camara(est, slidePrefab, out estaciones[6]);
        rm.resumen = Archivo(est, out estaciones[7]);
        rm.placa = placa;
        rm.estaciones = estaciones;


        var paneles = new List<PanelObjetivo>();
        paneles.AddRange(TorreObjetivo(raiz));
        paneles.Add(Recepcion(raiz));
        rm.paneles = paneles.ToArray();
        rm.baliza = Baliza(raiz);

        var depurar = new GameObject("DebugReto");
        depurar.transform.SetParent(raiz, false);
        depurar.AddComponent<DebugReto>();

        Equipamiento(MM.Grupo(raiz, "Equipamiento"));
        SenalesSalaPrincipal(raiz);
        Debug.Log($"[Centro] Centro de experimentación montado: {estaciones.Length} estaciones, reto {reto.codigo}.");
    }

    // La luz de resultado de la cámara cambia de color en tiempo real (ConfigurarLuces las hornea todas).
    public static void LucesTiempoReal()
    {
        foreach (var c in Object.FindObjectsOfType<CamaraExperimental>(true))
            if (c.luzResultado != null) c.luzResultado.lightmapBakeType = LightmapBakeType.Realtime;
    }

    // ================================================================== sala

    // Quita los tramos de la pared este que tapan la puerta y los reconstruye con el hueco.
    static void AbrirPuerta(Transform raiz)
    {
        var hueco = new Bounds(new Vector3((ParedX0 + ParedX1) / 2f, suelo + 1f, (PuertaZ0 + PuertaZ1) / 2f), new Vector3(0.6f, 1.6f, PuertaZ1 - PuertaZ0));
        int quitados = 0;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>(true).ToArray())
        {
            if (r == null || r.transform.IsChildOf(raiz)) continue;
            var go = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject) ?? r.gameObject;
            string n = go.name.ToLowerInvariant();
            bool pared = n.StartsWith("wall01") && r.bounds.Intersects(hueco);
            // Estanterías colgadas justo encima de la puerta.
            bool rack = n.StartsWith("rack") && r.bounds.min.x > 5.8f && r.bounds.max.z > PuertaZ0 - 0.2f && r.bounds.min.z < PuertaZ1 + 0.2f;
            if (!pared && !rack) continue;
            Debug.Log($"[Centro] Quitado para abrir la puerta: {go.name} ({r.bounds.min:F2}..{r.bounds.max:F2})");
            Object.DestroyImmediate(go);
            quitados++;
        }
        Debug.Log($"[Centro] Objetos quitados para la puerta: {quitados}");

        var p = MM.Grupo(raiz, "Puerta");
        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "wall01.prefab");
        float y0 = 0f, y1 = AltoSala;
        Pieza(p, wall, new Bounds(V((ParedX0 + ParedX1) / 2f, (y0 + y1) / 2f, (3.89f + PuertaZ0) / 2f), V(Grosor - 0.01f, y1 - y0, PuertaZ0 - 3.89f)), 90f);
        Pieza(p, wall, new Bounds(V((ParedX0 + ParedX1) / 2f, (y0 + y1) / 2f, (PuertaZ1 + 12.39f) / 2f), V(Grosor - 0.01f, y1 - y0, 12.39f - PuertaZ1)), 90f);
        float yd = suelo + AltoPuerta;
        Pieza(p, wall, new Bounds(V((ParedX0 + ParedX1) / 2f, (yd + y1) / 2f, (PuertaZ0 + PuertaZ1) / 2f), V(Grosor - 0.01f, y1 - yd, PuertaZ1 - PuertaZ0)), 90f);

        // Marco luminoso a ambos lados y umbral metálico.
        foreach (float x in new[] { ParedX0 - 0.012f, ParedX1 + 0.012f })
        {
            MM.Caja(p, "MarcoI", V(x, suelo + AltoPuerta / 2f, PuertaZ0 + 0.03f), V(0.02f, AltoPuerta, 0.05f), M["EmisivoCian"], false);
            MM.Caja(p, "MarcoD", V(x, suelo + AltoPuerta / 2f, PuertaZ1 - 0.03f), V(0.02f, AltoPuerta, 0.05f), M["EmisivoCian"], false);
            MM.Caja(p, "MarcoS", V(x, suelo + AltoPuerta - 0.025f, (PuertaZ0 + PuertaZ1) / 2f), V(0.02f, 0.05f, PuertaZ1 - PuertaZ0), M["EmisivoCian"], false);
        }
        MM.Caja(p, "Umbral", V((ParedX0 + ParedX1) / 2f, suelo + 0.004f, (PuertaZ0 + PuertaZ1) / 2f), V(Grosor + 0.1f, 0.008f, PuertaZ1 - PuertaZ0), M["Cromo"], false);
    }

    static void Sala(Transform s)
    {
        // Suelo: una pieza del pack (17,5 × 17,5 m).
        var floor = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "floor.prefab");
        var fGo = Pieza(s, floor, new Bounds(V((ParedX0 + X1) / 2f, suelo - 0.07f, (Z0 + Z1) / 2f), V(X1 - ParedX0, 0.14f, Z1 - Z0)), 0f);

        // Paredes con el wall01 del pack.
        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "wall01.prefab");
        Muro(s, wall, V(ParedX0, 0, Z0 - Grosor), V(X1 + Grosor, 0, Z0), true);            // sur
        Muro(s, wall, V(ParedX0, 0, Z1), V(X1 + Grosor, 0, Z1 + Grosor), true);            // norte
        Muro(s, wall, V(X1, 0, Z0 - Grosor), V(X1 + Grosor, 0, Z1 + Grosor), false);       // este
        Muro(s, wall, V(ParedX0, 0, Z0 - Grosor), V(ParedX1, 0, 3.89f), false);            // oeste, tramo sur
        Muro(s, wall, V(ParedX0, 0, 16.65f), V(ParedX1, 0, Z1 + Grosor), false);           // oeste, tramo norte

        // Techo con las placas del pack (7 × 7).
        var roof = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "roof.prefab");
        var techo = MM.Grupo(s, "Techo");
        float tx0 = ParedX0, tx1 = X1 + Grosor, tz0 = Z0 - Grosor, tz1 = Z1 + Grosor;
        int n = 7;
        float w = (tx1 - tx0) / n, d = (tz1 - tz0) / n;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                Pieza(techo, roof, new Bounds(V(tx0 + (i + 0.5f) * w, AltoSala + 0.03f, tz0 + (j + 0.5f) * d), V(w, 0.06f, d)), 0f, colliders: false);

        // Columnas en las esquinas interiores.
        var pillar = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "pillar.prefab");
        foreach (var (x, z) in new[] { (X0 + 0.2f, Z0 + 0.18f), (X1 - 0.2f, Z0 + 0.18f), (X0 + 0.2f, Z1 - 0.18f), (X1 - 0.2f, Z1 - 0.18f) })
            Pieza(s, pillar, new Bounds(V(x, AltoSala / 2f, z), V(0.4f, AltoSala, 0.35f)), 0f);

        // Luces horneadas en rejilla (4 × 4) + acento sobre la cámara.
        var luces = MM.Grupo(s, "Luces");
        foreach (var x in new[] { 9.1f, 13.4f, 17.7f, 22.0f })
            foreach (var z in new[] { 2.0f, 6.4f, 10.8f, 15.2f })
            {
                var l = new GameObject("LuzTecho").AddComponent<Light>();
                l.transform.SetParent(luces, false);
                l.transform.position = V(x, AltoSala - 0.2f, z);
                l.type = LightType.Point; l.range = 7.5f; l.intensity = 1.25f; l.color = new Color(1f, 0.97f, 0.92f);
                l.lightmapBakeType = LightmapBakeType.Baked;
            }
        var acento = new GameObject("LuzCamara").AddComponent<Light>();
        acento.transform.SetParent(luces, false);
        acento.transform.position = CentroCamara + Vector3.up * 3f;
        acento.type = LightType.Point; acento.range = 4f; acento.intensity = 0.9f; acento.color = new Color(0.45f, 0.85f, 1f);
        acento.lightmapBakeType = LightmapBakeType.Baked;

        // Suelo de seguridad propio (el de la sala principal no llega hasta aquí).
        var seg = new GameObject("SueloSeguridadAnexo");
        seg.transform.SetParent(s, false);
        seg.transform.position = V((X0 + X1) / 2f, suelo - 0.1f, (Z0 + Z1) / 2f);
        seg.AddComponent<BoxCollider>().size = V(X1 - X0 + 4f, 0.2f, Z1 - Z0 + 4f);
        Debug.Log($"[Centro] Sala anexa: x {X0}..{X1}, z {Z0}..{Z1} ({(X1 - X0) * (Z1 - Z0):F0} m²), suelo {fGo.GetComponentInChildren<Renderer>().bounds.max.y:F2}");
    }

    // Tramo de pared entre dos esquinas (a, b) teselado con wall01.
    static void Muro(Transform padre, GameObject wall, Vector3 a, Vector3 b, bool alongX)
    {
        float largo = alongX ? b.x - a.x : b.z - a.z;
        int n = Mathf.Max(1, Mathf.CeilToInt(largo / 4.25f - 0.05f));
        for (int i = 0; i < n; i++)
        {
            float t0 = i * largo / n, t1 = (i + 1) * largo / n;
            var c = alongX ? V(a.x + (t0 + t1) / 2f, AltoSala / 2f, (a.z + b.z) / 2f) : V((a.x + b.x) / 2f, AltoSala / 2f, a.z + (t0 + t1) / 2f);
            var size = alongX ? V(t1 - t0, AltoSala, Mathf.Abs(b.z - a.z)) : V(Mathf.Abs(b.x - a.x), AltoSala, t1 - t0);
            Pieza(padre, wall, new Bounds(c, size), alongX ? 0f : 90f);
        }
    }

    // Instancia un prefab del pack y lo escala/coloca para que ocupe exactamente 'objetivo' (ejes del mundo).
    static GameObject Pieza(Transform padre, GameObject prefab, Bounds objetivo, float yaw, bool colliders = true)
    {
        var cont = new GameObject(prefab.name).transform;
        cont.SetParent(padre, false);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cont);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var b = ML.BoundsDe(inst);
        cont.localScale = new Vector3(objetivo.size.x / b.size.x, objetivo.size.y / b.size.y, objetivo.size.z / b.size.z);
        b = ML.BoundsDe(inst);
        cont.position += objetivo.center - b.center;
        Fijo(cont.gameObject, colliders);
        return cont.gameObject;
    }

    // Geometría estática: colliders (como ML.AnadirColliders) y flags para hornear la luz.
    static void Fijo(GameObject go, bool colliders)
    {
        var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                    StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
        {
            var s = r.bounds.size;
            r.receiveGI = Mathf.Max(s.x, s.y, s.z) < 0.5f ? ReceiveGI.LightProbes : ReceiveGI.Lightmaps;
            if (!colliders || r.GetComponent<Collider>() != null || !r.TryGetComponent(out MeshFilter mf) || mf.sharedMesh == null) continue;
            if (Mathf.Max(s.x, s.y, s.z) > 0.5f) r.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            else r.gameObject.AddComponent<BoxCollider>();
        }
    }

    // Mueble de primitivas que no se mueve: estático pero iluminado con sondas.
    static void FijoPrimitiva(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.receiveGI = ReceiveGI.LightProbes;
    }

    // ================================================================== estaciones

    // Raíz de una estación: 'frente' es el borde frontal (lado del usuario) a ras de suelo;
    // el usuario está en +z local y mira hacia -z.
    static Transform Estacion(Transform padre, string nombre, Vector3 frente, float yaw, int zona, string tituloSuelo, float anchoFranja)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.position = V(frente.x, suelo, frente.z);
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        // Franja de color y rótulo en el suelo (forma + texto, no solo color).
        MM.Caja(t, "FranjaSuelo", V(0f, 0.003f, 0.75f), V(anchoFranja, 0.004f, 0.06f), M["Zona" + zona], false);
        var r = MM.Texto3D(t, $"{zona}  ·  {tituloSuelo}", V(0f, 0.004f, 1.05f), Quaternion.Euler(90f, 180f, 0f), 0.75f, ColorZona[zona - 1], anchoFranja);
        r.alignment = TextAlignmentOptions.Center; r.fontStyle = FontStyles.Bold;
        return t;
    }

    static float Mesa(Transform estacion, float x, float profundidad = 1.4f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "table with drawers.prefab");
        var cont = new GameObject("Mesa").transform;
        cont.SetParent(estacion, false);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cont);
        inst.transform.localRotation = Quaternion.identity;
        var b = ML.BoundsDe(inst);
        bool largoEnX = Mathf.Abs(Vector3.Dot(estacion.right, Vector3.right)) > 0.5f ? b.size.x >= b.size.z : b.size.z >= b.size.x;
        if (!largoEnX) inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        b = ML.BoundsDe(inst);
        var objetivo = estacion.TransformPoint(V(x, 0f, -profundidad / 2f));
        cont.position += new Vector3(objetivo.x - b.center.x, suelo - b.min.y, objetivo.z - b.center.z);
        Fijo(cont.gameObject, true);
        return ML.BoundsDe(inst).max.y - suelo;
    }

    // Cartel de estación con su botón PISTA (sobre un tótem a la izquierda de la estación).
    static EstacionGuia Cartel(Transform estacion, Vector3 local, int numero, string titulo, string descripcion, Vector3 baliza)
    {
        var pos = estacion.TransformPoint(local);
        var mirar = estacion.TransformPoint(V(local.x * 0.5f, 0f, 1.6f));
        var panel = MM.Totem(estacion, "Cartel", pos, mirar, 1.05f, 0.82f);
        var g = panel.parent.gameObject.AddComponent<EstacionGuia>();
        g.numero = numero; g.titulo = titulo; g.descripcion = descripcion;
        g.tituloTxt = Txt(panel, "", 0f, 0.3f, 0.36f, Color.white, 0.95f, TextAlignmentOptions.Left);
        g.tituloTxt.fontStyle = FontStyles.Bold;
        MM.Caja(panel, "Linea", V(0f, 0.22f, -0.016f), V(0.95f, 0.006f, 0.004f), M["Zona" + numero], false);
        g.textoTxt = Txt(panel, "", 0f, 0.17f, 0.24f, Color.white, 0.95f, TextAlignmentOptions.TopLeft);
        g.textoTxt.rectTransform.sizeDelta = new Vector2(0.95f, 0.5f);
        g.textoTxt.rectTransform.pivot = new Vector2(0.5f, 1f);
        var marco = MM.Caja(panel, "MarcoActivo", V(0f, 0f, 0.016f), V(1.15f, 0.92f, 0.02f), M["Brillo"], false);
        g.marcoActivo = marco.GetComponent<Renderer>();
        g.marcoActivo.enabled = false;
        // Repisa con el botón PISTA, a la altura de la mano.
        MM.Caja(panel, "Repisa", V(0.36f, -0.43f, -0.07f), V(0.24f, 0.02f, 0.16f), M["Marco"], false);
        g.pista = MM.Boton(panel, "PISTA", V(0.36f, -0.42f, -0.08f));
        g.pista.tapa.GetComponent<Renderer>().sharedMaterial = M["EmisivoAmarillo"];
        var pistaTxt = Txt(panel, "<b>?</b> PISTA", -0.2f, -0.36f, 0.22f, Amarillo, 0.5f, TextAlignmentOptions.Right);
        pistaTxt.fontStyle = FontStyles.Bold;
        g.puntoBaliza = MM.Vacio(estacion, "PuntoBaliza", baliza);
        FijoPrimitiva(panel.parent.Find("Pie").gameObject);
        return g;
    }

    // Tablero de información apoyado al fondo de una estación, mirando al usuario.
    static Transform Tablero(Transform estacion, string nombre, Vector3 local, float ancho, float alto, string titulo, string cuerpo, float tamCuerpo = 0.26f)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(estacion, false);
        t.localPosition = local;
        t.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MM.Caja(t, "Fondo", V(0f, 0f, 0.01f), V(ancho, alto, 0.03f), M["PanelHUD"], true);
        MM.Caja(t, "Borde", V(0f, 0f, 0.012f), V(ancho + 0.03f, alto + 0.03f, 0.028f), M["EmisivoCian"], false);
        var tt = Txt(t, titulo, 0f, alto / 2f - 0.1f, 0.4f, Cian, ancho - 0.1f, TextAlignmentOptions.Center);
        tt.fontStyle = FontStyles.Bold;
        if (cuerpo != null)
        {
            var c = Txt(t, cuerpo, 0f, alto / 2f - 0.2f, tamCuerpo, Color.white, ancho - 0.12f, TextAlignmentOptions.TopLeft);
            c.rectTransform.pivot = new Vector2(0.5f, 1f);
            c.rectTransform.sizeDelta = new Vector2(ancho - 0.12f, alto - 0.25f);
        }
        return t;
    }

    // ---------------------------------------------------------------- 1 banco de muestras

    static PlacaCaso Banco(Transform padre, out EstacionGuia guia)
    {
        var e = Estacion(padre, "1_BancoMuestras", V(10.5f, 0f, 0.55f), 0f, 1, "BANCO DE MUESTRAS", 2.2f);
        float h = 0.95f;
        var mueble = MM.Grupo(e, "Mueble");
        MM.Caja(mueble, "Cuerpo", V(0f, h / 2f, -0.4f), V(2.1f, h, 0.8f), M["Blanco"], true);
        MM.Caja(mueble, "Encimera", V(0f, h + 0.01f, -0.4f), V(2.14f, 0.02f, 0.84f), M["Oscuro"], true);
        MM.Caja(mueble, "FranjaLuz", V(0f, h - 0.06f, 0.005f), V(2.1f, 0.02f, 0.01f), M["Zona1"], false);
        // Vitrina refrigerada al fondo.
        MM.Caja(mueble, "Vitrina", V(0f, h + 0.5f, -0.65f), V(2.1f, 1.0f, 0.3f), M["Blanco"], true);
        MM.Caja(mueble, "VitrinaVidrio", V(0f, h + 0.52f, -0.495f), V(1.95f, 0.85f, 0.01f), M["Vidrio"], false);
        MM.Caja(mueble, "VitrinaInterior", V(0f, h + 0.52f, -0.66f), V(1.95f, 0.85f, 0.25f), M["Oscuro"], false);
        foreach (var y in new[] { 0.3f, 0.62f }) MM.Caja(mueble, "Balda", V(0f, h + y, -0.6f), V(1.95f, 0.012f, 0.2f), M["Cromo"], false);
        var coleccion = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NanoLab/Modelos/PlacasPetri_Coleccion.prefab");
        if (coleccion != null)
            foreach (var (x, y) in new[] { (-0.55f, 0.31f), (0.55f, 0.31f), (0f, 0.63f) })
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(coleccion, mueble);
                c.transform.localPosition = V(x, h + y + 0.006f, -0.6f);
                foreach (var col in c.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            }
        var rot = MM.Texto3D(mueble, "BANCO DE MUESTRAS  ·  4 °C", V(0f, h + 0.93f, -0.49f), Quaternion.Euler(0f, 180f, 0f), 0.42f, Cian, 1.9f);
        rot.alignment = TextAlignmentOptions.Center; rot.fontStyle = FontStyles.Bold;
        MM.Senal(mueble, "Biohazard", e.TransformPoint(V(0.85f, h + 0.18f, -0.49f)), e.rotation * Quaternion.Euler(0f, 180f, 0f), "RIESGO\nBIOLÓGICO", 0.16f);
        FijoPrimitiva(mueble.gameObject);

        // Ranuras: caso 01 (activo) y dos casos bloqueados.
        var placa = CrearPlaca(e, V(-0.6f, h + 0.02f, -0.25f), out var zonaCasa);
        RotuloRanura(e, V(-0.6f, h + 0.02f, -0.06f), "CASO 01 · Herida de piel", "ACTIVO", ColorZona[0]);
        var cultivo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NanoLab/Modelos/PlacaPetri_Cultivo.prefab");
        for (int i = 0; i < 2; i++)
        {
            float x = 0.1f + i * 0.6f;
            MM.Cilindro(e, "Ranura", V(x, h + 0.025f, -0.25f), V(0.18f, 0.004f, 0.18f), Quaternion.identity, M["Oscuro"]);
            if (cultivo != null)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(cultivo, e);
                c.transform.localPosition = V(x, h + 0.03f, -0.25f);
                c.transform.localScale = Vector3.one * 1.3f;
                foreach (var col in c.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
            }
            MM.Cilindro(e, "Campana", V(x, h + 0.09f, -0.25f), V(0.2f, 0.06f, 0.2f), Quaternion.identity, M["Vidrio"]);
            RotuloRanura(e, V(x, h + 0.02f, -0.06f), $"CASO 0{i + 2} · Próximamente", "BLOQUEADO", Gris);
        }
        guia = Cartel(e, V(-1.65f, 0f, 0.2f), 1, "BANCO DE MUESTRAS",
            "Aquí se guardan los casos. Elige la placa del CASO 01 y llévala a la mesa de TINCIÓN (2) para hacer el frotis.", V(-0.6f, 2.1f, -0.25f));
        return placa;
    }

    static void RotuloRanura(Transform e, Vector3 pos, string titulo, string estado, Color color)
    {
        var hex = ColorUtility.ToHtmlStringRGB(color);
        var t = MM.Texto3D(e, $"<b>{titulo}</b>\n<color=#{hex}>{estado}</color>", pos, Quaternion.Euler(60f, 180f, 0f), 0.2f, Color.white, 0.55f);
        t.alignment = TextAlignmentOptions.Center;
    }

    // Placa de Petri del caso: agar sangre con colonias doradas (S. aureus) animables.
    static PlacaCaso CrearPlaca(Transform padre, Vector3 pos, out ZonaEncaje ranura)
    {
        ranura = Zona(padre, "placa", pos, V(0.2f, 0.08f, 0.2f), PrimitiveType.Cylinder, V(0.17f, 0.003f, 0.17f));
        MM.Cilindro(padre, "RanuraCaso", pos + V(0f, 0.003f, 0f), V(0.19f, 0.004f, 0.19f), Quaternion.identity, M["Zona1"]);

        var go = new GameObject("PlacaCaso01");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.transform;
        MM.Cilindro(t, "Base", V(0f, 0.008f, 0f), V(0.15f, 0.008f, 0.15f), Quaternion.identity, M["Vidrio"]);
        MM.Cilindro(t, "Agar", V(0f, 0.008f, 0f), V(0.14f, 0.004f, 0.14f), Quaternion.identity, M["AgarSangre"]);
        var halo = MM.Cilindro(t, "Halo", V(0f, 0.0123f, 0f), V(0.1f, 0.0004f, 0.1f), Quaternion.identity, M["Halo"]);
        MM.Cilindro(t, "Tapa", V(0f, 0.02f, 0f), V(0.155f, 0.002f, 0.155f), Quaternion.identity, M["Vidrio"]);
        var brillo = MM.Cilindro(t, "Brillo", V(0f, 0.012f, 0f), V(0.17f, 0.013f, 0.17f), Quaternion.identity, M["Brillo"]);

        var rnd = new System.Random(7);
        Transform Colonia(string n, float dMin, float dMax, float rMax)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rnd.NextDouble()) * rMax;
            float d = dMin + (float)rnd.NextDouble() * (dMax - dMin);
            var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(s.GetComponent<Collider>());
            s.name = n;
            s.transform.SetParent(t, false);
            s.transform.localPosition = V(Mathf.Cos(a) * r, 0.0125f, Mathf.Sin(a) * r);
            s.transform.localScale = V(d, d * 0.35f, d);
            var mr = s.GetComponent<MeshRenderer>();
            mr.sharedMaterial = M["Colonia"]; mr.shadowCastingMode = ShadowCastingMode.Off;
            return s.transform;
        }
        var colonias = Enumerable.Range(0, 18).Select(i => Colonia("Colonia" + i, 0.008f, 0.016f, 0.058f)).ToArray();
        var extra = Enumerable.Range(0, 14).Select(i => Colonia("ColoniaExtra" + i, 0.006f, 0.012f, 0.06f)).ToArray();

        var solido = go.AddComponent<BoxCollider>();
        solido.center = V(0f, 0.011f, 0f); solido.size = V(0.155f, 0.022f, 0.155f);
        var trig = go.AddComponent<BoxCollider>();
        trig.isTrigger = true; trig.center = V(0f, 0.035f, 0f); trig.size = V(0.17f, 0.07f, 0.17f);

        var etq = new GameObject("Etiqueta").transform;
        etq.SetParent(t, false);
        etq.localPosition = V(0f, 0.13f, 0f);
        etq.gameObject.AddComponent<Cartel>();
        var titulo = MM.Texto3D(etq, "<b>CASO 01</b>", V(0f, 0.025f, 0f), Quaternion.identity, 0.32f, ColorZona[0], 0.4f);
        titulo.alignment = TextAlignmentOptions.Center;
        titulo.outlineWidth = 0.2f; titulo.outlineColor = new Color32(0, 0, 0, 220);
        var estado = MM.Texto3D(etq, "SIN TRATAR", V(0f, -0.015f, 0f), Quaternion.identity, 0.18f, Color.white, 0.4f);
        estado.alignment = TextAlignmentOptions.Center;
        estado.outlineWidth = 0.2f; estado.outlineColor = new Color32(0, 0, 0, 220);

        var p = go.AddComponent<PlacaCaso>();
        p.colonias = colonias; p.coloniasExtra = extra; p.halo = halo.transform; p.radioHaloMax = 0.05f; p.estado = estado;
        var f = go.AddComponent<FuenteMuestra>();
        f.muestra = reto.muestra; f.etiqueta = reto.codigo;
        var enc = go.AddComponent<Encajable>();
        enc.tipo = "placa"; enc.casa = ranura; enc.brillo = brillo.GetComponent<Renderer>(); enc.radioEncaje = 0.16f;
        ConfigurarMetaISDK.HacerAgarrable(go, 0.12f);
        return p;
    }

    static ZonaEncaje Zona(Transform padre, string tipo, Vector3 pos, Vector3 trigger, PrimitiveType forma, Vector3 fantasma, bool aceptaSoltar = true)
    {
        var go = new GameObject("Zona_" + tipo);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.center = V(0f, trigger.y / 2f, 0f); bc.size = trigger;
        var z = go.AddComponent<ZonaEncaje>();
        z.tipo = tipo; z.aceptaSoltar = aceptaSoltar;
        z.anclaje = MM.Vacio(go.transform, "Anclaje", Vector3.zero);
        var f = forma == PrimitiveType.Cylinder
            ? MM.Cilindro(z.anclaje, "Fantasma", V(0f, fantasma.y, 0f), fantasma, Quaternion.identity, M["Fantasma"])
            : MM.Caja(z.anclaje, "Fantasma", V(0f, fantasma.y / 2f, 0f), fantasma, M["Fantasma"], false);
        z.fantasma = f.GetComponent<Renderer>();
        z.fantasma.enabled = false;
        return z;
    }

    // ---------------------------------------------------------------- 2 tinción de Gram

    static readonly (string nombre, string accion, Color color)[] Reactivos =
    {
        ("CRISTAL VIOLETA", "tiñe todas las células", new Color(0.45f, 0.15f, 0.8f)),
        ("LUGOL", "fija el violeta", new Color(0.55f, 0.32f, 0.08f)),
        ("ALCOHOL-ACETONA", "decolora las Gram -", new Color(0.85f, 0.92f, 1f)),
        ("SAFRANINA", "tiñe de rosa las Gram -", new Color(0.9f, 0.2f, 0.35f)),
    };

    static void Tincion(Transform padre, GameObject slidePrefab, out EstacionGuia guia)
    {
        var e = Estacion(padre, "2_Tincion", V(15.2f, 0f, 1.15f), 0f, 2, "TINCIÓN DE GRAM", 3.7f);
        float h = Mesa(e, -0.93f);
        Mesa(e, 0.93f);
        var puesto = new GameObject("Puesto").transform;
        puesto.SetParent(e, false);
        puesto.localPosition = V(0f, h, 0f);

        // Bandeja con portaobjetos y desinfectante (a la izquierda).
        MM.Caja(puesto, "Bandeja", V(-1.35f, 0.006f, -0.32f), V(0.13f, 0.012f, 0.17f), M["Oscuro"], true);
        for (int i = 0; i < 4; i++)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, puesto);
            s.transform.localPosition = V(-1.35f, 0.015f, -0.39f + i * 0.045f);
            s.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        var tb = MM.Texto3D(puesto, "PORTAOBJETOS", V(-1.35f, 0.002f, -0.17f), Quaternion.Euler(90f, 180f, 0f), 0.14f, Color.white, 0.3f);
        tb.alignment = TextAlignmentOptions.Center;
        MM.CrearDesinfectante(puesto, V(-0.95f, 0f, -0.32f));

        // Goteros numerados 1 > 4, de izquierda a derecha vistos por el usuario (su izquierda es +x local).
        for (int i = 0; i < 4; i++) Gotero(puesto, i + 1, V(0.62f - i * 0.32f, 0f, -0.32f));
        var orden = MM.Texto3D(puesto, "ORDEN:  1  >  2  >  3  >  4", V(0.14f, 0.002f, -0.12f), Quaternion.Euler(90f, 180f, 0f), 0.3f, Amarillo, 1.3f);
        orden.alignment = TextAlignmentOptions.Center; orden.fontStyle = FontStyles.Bold;

        // Procedimiento al fondo.
        Tablero(e, "Procedimiento", V(0f, h + 0.55f, -1.32f), 3.4f, 0.95f, "TINCIÓN DE GRAM  ·  PROCEDIMIENTO",
            "<b>Frotis:</b> agarra un portaobjetos y toca con él la placa del caso.   <b>Tinción:</b> toca con el portaobjetos cada gotero <b>en orden</b>.\n" +
            "<color=#B98CFF>1 CRISTAL VIOLETA</color> tiñe todas las células   <color=#C8955A>2 LUGOL</color> fija el violeta   <color=#DDEBFF>3 ALCOHOL-ACETONA</color> decolora las Gram -   <color=#FF6F8A>4 SAFRANINA</color> tiñe de rosa las Gram -\n" +
            "<b>Resultado:</b> las bacterias <color=#B98CFF><b>Gram +</b> quedan VIOLETAS</color> y las <color=#FF6F8A><b>Gram -</b> quedan ROSAS</color>. ¿Te equivocaste? Toca el <color=#8FD8FF>desinfectante</color> y empieza de nuevo.", 0.25f);
        guia = Cartel(e, V(-2.45f, 0f, 0.25f), 2, "TINCIÓN DE GRAM",
            "Sin teñir, las bacterias son casi invisibles. La tinción de Gram revela su forma y el tipo de pared celular.", V(0f, 2.15f, -0.35f));
    }

    static void Gotero(Transform puesto, int n, Vector3 pos)
    {
        var (nombre, accion, color) = Reactivos[n - 1];
        var g = new GameObject($"Gotero{n}").transform;
        g.SetParent(puesto, false);
        g.localPosition = pos;
        MM.Cilindro(g, "Base", V(0f, 0.003f, 0f), V(0.11f, 0.003f, 0.11f), Quaternion.identity, M["Oscuro"]);
        var mat = MM.MatEn(MatDir, "Reactivo" + n, Shader.Find("Standard"));
        mat.color = new Color(color.r, color.g, color.b, 0.85f);
        mat.SetFloat("_Glossiness", 0.85f);
        MM.Cilindro(g, "Frasco", V(0f, 0.05f, 0f), V(0.06f, 0.045f, 0.06f), Quaternion.identity, mat);
        MM.Cilindro(g, "Cuello", V(0f, 0.1f, 0f), V(0.025f, 0.008f, 0.025f), Quaternion.identity, M["Negro"]);
        MM.Cilindro(g, "Pipeta", V(0f, 0.125f, 0f), V(0.012f, 0.02f, 0.012f), Quaternion.identity, M["Vidrio"]);
        var bulbo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(bulbo.GetComponent<Collider>());
        bulbo.name = "Bulbo"; bulbo.transform.SetParent(g, false);
        bulbo.transform.localPosition = V(0f, 0.152f, 0f); bulbo.transform.localScale = V(0.028f, 0.035f, 0.028f);
        bulbo.GetComponent<Renderer>().sharedMaterial = M["Negro"];
        var brillo = MM.Cilindro(g, "Brillo", V(0f, 0.07f, 0f), V(0.075f, 0.075f, 0.075f), Quaternion.identity, M["Brillo"]);
        var num = MM.Texto3D(g, n.ToString(), V(0f, 0.004f, 0.075f), Quaternion.Euler(90f, 180f, 0f), 0.6f, Color.white, 0.08f);
        num.alignment = TextAlignmentOptions.Center; num.fontStyle = FontStyles.Bold;
        MM.Cilindro(g, "Disco", V(0f, 0.0035f, 0.075f), V(0.07f, 0.002f, 0.07f), Quaternion.identity, M["Zona2"]);
        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.center = V(0f, 0.08f, 0f); bc.size = V(0.1f, 0.16f, 0.1f);
        g.gameObject.AddComponent<FuenteMuestra>().reactivo = n;
        g.gameObject.AddComponent<Resaltado>().brillos = new[] { brillo.GetComponent<Renderer>() };
        var hex = ColorUtility.ToHtmlStringRGB(color == Reactivos[2].color ? Cian : color);
        var etq = MM.Texto3D(g, $"<b><color=#{hex}>{nombre}</color></b>\n<size=75%>{accion}</size>", V(0f, 0.004f, 0.16f), Quaternion.Euler(70f, 180f, 0f), 0.17f, Color.white, 0.3f);
        etq.alignment = TextAlignmentOptions.Center;
    }

    // ---------------------------------------------------------------- 3 microscopía

    static void Microscopia(Transform padre, GameObject slidePrefab, out EstacionGuia guia)
    {
        var e = Estacion(padre, "3_Microscopia", V(20.5f, 0f, 1.15f), 0f, 3, "MICROSCOPÍA", 3.7f);
        float h = Mesa(e, -0.93f);
        Mesa(e, 0.93f);
        foreach (var x in new[] { -0.95f, 0.95f })
        {
            var puesto = new GameObject("Puesto").transform;
            puesto.SetParent(e, false);
            puesto.localPosition = V(x, h, -0.3f);
            MM.CrearMicroscopio(puesto, V(0f, 0f, -0.05f));
        }
        foreach (var m in e.GetComponentsInChildren<MicroscopioOptico>()) m.modoReto = true;
        var bandeja = new GameObject("Bandeja").transform;
        bandeja.SetParent(e, false);
        bandeja.localPosition = V(0f, h, -0.3f);
        MM.Caja(bandeja, "Bandeja", V(0f, 0.006f, 0f), V(0.13f, 0.012f, 0.12f), M["Oscuro"], true);
        for (int i = 0; i < 2; i++)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, bandeja);
            s.transform.localPosition = V(0f, 0.015f, -0.03f + i * 0.05f);
        }
        Tablero(e, "Comparar", V(0f, h + 0.55f, -1.32f), 3.4f, 0.95f, "OBSERVAR  ·  COMPARAR",
            "Suelta el portaobjetos sobre la <color=#8FD8FF>silueta azul</color> y <b>acerca la cara al ocular</b>.   <color=#8FD8FF>A / X</color> más aumento · <color=#8FD8FF>B / Y</color> menos.\n" +
            "<b>ANTES:</b> registra 3 evidencias en tu cuaderno: <b>forma</b>, <b>agrupación</b> y <b>color Gram</b>.\n" +
            "<b>DESPUÉS del experimento:</b> trae el portaobjetos DESPUÉS y compara: ¿hay menos células? ¿están rotas? ¿por qué?", 0.26f);
        guia = Cartel(e, V(-2.45f, 0f, 0.25f), 3, "MICROSCOPÍA",
            "El microscopio es tu instrumento de evidencia: úsalo antes del experimento y después, para comparar.", V(0f, 2.15f, -0.35f));
    }

    // ---------------------------------------------------------------- 4 biblioteca

    static EstacionFicha[] Biblioteca(Transform padre, out EstacionGuia guia)
    {
        var e = Estacion(padre, "4_Biblioteca", V(23.45f, 0f, 8.6f), -90f, 4, "BIBLIOTECA", 6.5f);
        float fondo = -(X1 - 23.45f);
        var mueble = MM.Grupo(e, "Mostrador");
        MM.Caja(mueble, "Cuerpo", V(0f, 0.45f, fondo + 0.27f), V(7.2f, 0.9f, 0.5f), M["Blanco"], true);
        MM.Caja(mueble, "Encimera", V(0f, 0.915f, fondo + 0.27f), V(7.24f, 0.03f, 0.56f), M["Oscuro"], true);
        MM.Caja(mueble, "FranjaLuz", V(0f, 0.85f, fondo + 0.525f), V(7.2f, 0.02f, 0.01f), M["Zona4"], false);
        FijoPrimitiva(mueble.gameObject);

        var fichas = new List<EstacionFicha>();
        var orden = new[] { "Ecoli", "Saureus", "Bsubtilis" };
        for (int i = 0; i < 4; i++)
        {
            float x = -2.7f + i * 1.8f;
            var panel = new GameObject(i < 3 ? "Ficha_" + orden[i] : "Ficha_Pared").transform;
            panel.SetParent(e, false);
            panel.localPosition = V(x, 1.78f, fondo + 0.03f);
            panel.localRotation = Quaternion.Euler(0f, 180f, 0f);
            MM.Caja(panel, "Fondo", V(0f, 0f, 0.01f), V(1.65f, 1.3f, 0.03f), M["PanelHUD"], true);
            MM.Caja(panel, "Borde", V(0f, 0f, 0.012f), V(1.68f, 1.33f, 0.028f), M["Zona4"], false);
            var f = panel.gameObject.AddComponent<EstacionFicha>();
            f.cerrada = MM.Grupo(panel, "Cerrada").gameObject;
            f.abierta = MM.Grupo(panel, "Abierta").gameObject;
            if (i < 3)
            {
                var m = AssetDatabase.LoadAssetAtPath<MuestraData>($"Assets/NanoLab/Muestras/{orden[i]}.asset");
                f.id = m.nombre;
                FichaMuestra(f, m);
            }
            else
            {
                f.id = "Pared Gram + / Gram -";
                FichaPared(f);
            }
            // Botón en el mostrador, delante del panel.
            var b = MM.Boton(e, "ABRIR", V(x, 0.93f, fondo + 0.42f));
            b.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            f.boton = b;
            f.botonTxt = b.etiqueta;
            var hint = MM.Texto3D(e, i < 3 ? "FICHA" : "PANEL", V(x, 0.932f, fondo + 0.26f), Quaternion.Euler(90f, 180f, 0f), 0.2f, ColorZona[3], 0.4f);
            hint.alignment = TextAlignmentOptions.Center; hint.fontStyle = FontStyles.Bold;
            fichas.Add(f);
        }
        var titulo = MM.Texto3D(e, "BIBLIOTECA  ·  FICHAS DE REFERENCIA", V(0f, 2.75f, fondo + 0.02f), Quaternion.Euler(0f, 180f, 0f), 0.6f, ColorZona[3], 6f);
        titulo.alignment = TextAlignmentOptions.Center; titulo.fontStyle = FontStyles.Bold;
        guia = Cartel(e, V(-4.2f, 0f, 0.0f), 4, "BIBLIOTECA",
            "Compara lo que observaste con las fichas. La respuesta no se adivina: se deduce de la evidencia.", V(0f, 2.45f, fondo + 0.5f));
        return fichas.ToArray();
    }

    static void FichaMuestra(EstacionFicha f, MuestraData m)
    {
        var hex = ColorUtility.ToHtmlStringRGB(m.color);
        var c = Txt(f.cerrada.transform, $"<b>{m.nombre.ToUpper()}</b>\n<size=60%><i>{m.nombreCientifico}</i></size>\n\n<size=55%><color=#A9BED2>Pulsa ABRIR en el mostrador</color></size>", 0f, 0.15f, 0.55f, Color.white, 1.5f, TextAlignmentOptions.Center);
        var a = f.abierta.transform;
        var t = Txt(a, $"<b><color=#{hex}>{m.nombre.ToUpper()}</color></b>  <size=65%><i>{m.nombreCientifico}</i></size>", 0f, 0.55f, 0.42f, Color.white, 1.55f, TextAlignmentOptions.Center);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.name = "Micrografia";
        quad.transform.SetParent(a, false);
        quad.transform.localPosition = V(-0.4f, 0.12f, -0.005f);
        quad.transform.localScale = V(0.72f, 0.54f, 1f);
        var mat = MM.MatEn(MatDir, "Ficha_" + m.name, Shader.Find("Unlit/Texture"));
        mat.mainTexture = m.micrografia;
        quad.GetComponent<Renderer>().sharedMaterial = mat;
        var pie = Txt(a, "Tinción de Gram · 1000x", -0.4f, -0.18f, 0.18f, Gris, 0.72f, TextAlignmentOptions.Center);
        var datos = new System.Text.StringBuilder();
        foreach (var d in m.caracteristicas.Where(d => d.clave != "Tipo")) datos.AppendLine($"<color=#8FD8FF>{d.clave}:</color> {d.valor}");
        var dt = Txt(a, datos.ToString(), 0.42f, 0.36f, 0.22f, Color.white, 0.75f, TextAlignmentOptions.TopLeft);
        dt.rectTransform.pivot = new Vector2(0.5f, 1f);
        dt.rectTransform.sizeDelta = new Vector2(0.75f, 0.6f);
        var morf = Txt(a, $"<b>Al microscopio:</b> {m.morfologia}\n<b>Pared:</b> {m.estructura}", 0f, -0.27f, 0.21f, Color.white, 1.5f, TextAlignmentOptions.TopLeft);
        morf.rectTransform.pivot = new Vector2(0.5f, 1f);
        morf.rectTransform.sizeDelta = new Vector2(1.5f, 0.35f);
    }

    static void FichaPared(EstacionFicha f)
    {
        Txt(f.cerrada.transform, "<b>PARED CELULAR</b>\n<size=60%>Gram +  vs  Gram -</size>\n\n<size=55%><color=#A9BED2>Pulsa ABRIR en el mostrador</color></size>", 0f, 0.15f, 0.55f, Color.white, 1.5f, TextAlignmentOptions.Center);
        var a = f.abierta.transform;
        Txt(a, "<b>PARED CELULAR: GRAM +  vs  GRAM -</b>", 0f, 0.55f, 0.36f, Cian, 1.55f, TextAlignmentOptions.Center);
        // Esquema de capas (forma + texto, no solo color).
        Capas(a, -0.4f, "GRAM +", "quedan <b>VIOLETAS</b>", new[] { ("Peptidoglucano GRUESO", new Color(0.55f, 0.3f, 0.95f), 0.11f), ("Membrana", new Color(0.9f, 0.8f, 0.5f), 0.03f) });
        Capas(a, 0.4f, "GRAM -", "quedan <b>ROSAS</b>", new[] { ("MEMBRANA EXTERNA", new Color(1f, 0.45f, 0.6f), 0.04f), ("Peptidoglucano fino", new Color(0.55f, 0.3f, 0.95f), 0.025f), ("Membrana", new Color(0.9f, 0.8f, 0.5f), 0.03f) });
        var c = Txt(a, "<color=#B98CFF><b>Gram +</b></color>: pared gruesa de peptidoglucano, <b>sin</b> membrana externa (S. aureus, B. subtilis).\n" +
                       "<color=#FF6F8A><b>Gram -</b></color>: pared fina y una <b>membrana externa</b> extra (E. coli).\n" +
                       "<color=#8FD8FF><b>Virus</b></color>: no son células (~100 nm); no se ven con este microscopio.", 0f, -0.2f, 0.2f, Color.white, 1.5f, TextAlignmentOptions.TopLeft);
        c.rectTransform.pivot = new Vector2(0.5f, 1f);
        c.rectTransform.sizeDelta = new Vector2(1.5f, 0.42f);
    }

    static void Capas(Transform a, float x, string titulo, string sub, (string nombre, Color color, float grosor)[] capas)
    {
        Txt(a, $"<b>{titulo}</b>  <size=75%>{sub}</size>", x, 0.42f, 0.26f, Color.white, 0.75f, TextAlignmentOptions.Center);
        float y = 0.3f;
        foreach (var (nombre, color, grosor) in capas)
        {
            float alto = Mathf.Max(0.03f, grosor);
            var mat = MM.MatEn(MatDir, "Capa_" + nombre.Replace(" ", ""), Shader.Find("Unlit/Color"));
            mat.color = color;
            MM.Caja(a, "Capa", V(x - 0.18f, y - alto / 2f, -0.005f), V(0.3f, alto, 0.005f), mat, false);
            Txt(a, nombre, x + 0.17f, y - alto / 2f + 0.01f, 0.15f, Color.white, 0.4f, TextAlignmentOptions.Left);
            y -= alto + 0.02f;
        }
    }

    // ---------------------------------------------------------------- 5 terminal de hipótesis

    static TerminalHipotesis Terminal(Transform padre, out EstacionGuia guia)
    {
        var e = Estacion(padre, "5_Terminal", V(20.5f, 0f, 16.55f), 180f, 5, "HIPÓTESIS", 2.2f);
        var c = MM.Grupo(e, "Consola");
        MM.Caja(c, "Cuerpo", V(0f, 0.45f, -0.3f), V(1.4f, 0.9f, 0.45f), M["Blanco"], true);
        MM.Caja(c, "Tapa", V(0f, 0.91f, -0.3f), V(1.44f, 0.02f, 0.5f), M["Oscuro"], true);
        MM.Caja(c, "FranjaLuz", V(0f, 0.85f, -0.07f), V(1.4f, 0.02f, 0.01f), M["Zona5"], false);
        FijoPrimitiva(c.gameObject);
        var pantalla = Tablero(e, "Pantalla", V(0f, 1.55f, -0.62f), 1.6f, 0.75f, "TERMINAL DE HIPÓTESIS", null);
        var t = e.gameObject.AddComponent<TerminalHipotesis>();
        t.pantalla = Txt(pantalla, "", 0f, 0.05f, 0.3f, Color.white, 1.45f, TextAlignmentOptions.Center);
        t.opciones = reto.opciones;
        t.botones = new BotonFisico[reto.opciones.Length];
        t.marcos = new Renderer[reto.opciones.Length];
        for (int i = 0; i < reto.opciones.Length; i++)
        {
            float x = 0.45f - i * 0.45f;   // A a la izquierda del usuario
            var b = MM.Boton(e, ((char)('A' + i)).ToString(), V(x, 0.92f, -0.2f));
            b.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            b.tapa.GetComponent<Renderer>().sharedMaterial = M["Zona5"];
            t.botones[i] = b;
            var marco = MM.Caja(e, "Elegida", V(x, 0.925f, -0.2f), V(0.11f, 0.03f, 0.11f), M["Brillo"], false);
            t.marcos[i] = marco.GetComponent<Renderer>();
            t.marcos[i].enabled = false;
            var n = MM.Texto3D(e, $"<b>{reto.opciones[i].nombre}</b>", V(x, 0.923f, -0.38f), Quaternion.Euler(90f, 180f, 0f), 0.24f, Color.white, 0.42f);
            n.alignment = TextAlignmentOptions.Center;
        }
        guia = Cartel(e, V(-1.25f, 0f, 0.15f), 5, "HIPÓTESIS",
            "Decide qué microorganismo es, según tus evidencias. Podrás cambiar de idea si el experimento te da nueva información.", V(0f, 2.3f, -0.3f));
        return t;
    }

    // ---------------------------------------------------------------- 6 prensa de cápsulas

    static PrensaCapsulas Prensa(Transform padre, out EstacionGuia guia)
    {
        var e = Estacion(padre, "6_Prensa", V(15.5f, 0f, 15.86f), 180f, 6, "PREPARACIÓN: CÁPSULA", 2.2f);
        float h = Mesa(e, 0f);
        var top = new GameObject("Puesto").transform;
        top.SetParent(e, false);
        top.localPosition = V(0f, h, 0f);

        // Rack de viales (orden mezclado: el correcto no es el primero).
        MM.Caja(top, "Rack", V(-0.42f, 0.012f, -0.32f), V(0.95f, 0.024f, 0.16f), M["Marco"], true);
        int[] ordenRack = { 1, 2, 0, 3 };
        for (int k = 0; k < ordenRack.Length; k++)
        {
            int ag = ordenRack[k];
            var a = reto.agentes[ag];
            var pos = V(-0.78f + k * 0.24f, 0.024f, -0.32f);
            var z = Zona(top, "vial", pos, V(0.09f, 0.14f, 0.09f), PrimitiveType.Cylinder, V(0.05f, 0.05f, 0.05f));
            CrearVial(top, ag, a, pos, z);
            var r = MM.Texto3D(top, $"<b>{a.sigla}</b>", V(pos.x, 0.026f, -0.2f), Quaternion.Euler(90f, 180f, 0f), 0.3f, a.color, 0.2f);
            r.alignment = TextAlignmentOptions.Center;
        }

        // Prensa: ranura del vial, pistón, pantalla, botón FORMULAR y salida de la cápsula.
        var p = new GameObject("Prensa").transform;
        p.SetParent(top, false);
        MM.Caja(p, "Cuerpo", V(0.55f, 0.25f, -0.6f), V(0.62f, 0.5f, 0.28f), M["Blanco"], true);
        MM.Caja(p, "Brazo", V(0.36f, 0.47f, -0.42f), V(0.1f, 0.06f, 0.28f), M["Blanco"], false);
        var piston = MM.Cilindro(p, "Piston", V(0.36f, 0.4f, -0.3f), V(0.05f, 0.05f, 0.05f), Quaternion.identity, M["Cromo"]).transform;
        var ranura = Zona(p, "vial", V(0.36f, 0.004f, -0.3f), V(0.1f, 0.14f, 0.1f), PrimitiveType.Cylinder, V(0.05f, 0.05f, 0.05f));
        MM.Cilindro(p, "Plataforma", V(0.36f, 0.002f, -0.3f), V(0.1f, 0.002f, 0.1f), Quaternion.identity, M["Zona6"]);
        var salida = Zona(p, "capsula", V(0.74f, 0.004f, -0.33f), V(0.1f, 0.12f, 0.1f), PrimitiveType.Cylinder, V(0.04f, 0.03f, 0.04f));
        MM.Cilindro(p, "Cuna", V(0.74f, 0.003f, -0.33f), V(0.07f, 0.003f, 0.07f), Quaternion.identity, M["Zona6"]);
        var luz = MM.Cilindro(p, "Luz", V(0.75f, 0.45f, -0.455f), V(0.03f, 0.005f, 0.03f), Quaternion.Euler(90f, 0f, 0f), M["EmisivoVerde"]);
        var pantallaT = new GameObject("Pantalla").transform;
        pantallaT.SetParent(p, false);
        pantallaT.localPosition = V(0.55f, 0.28f, -0.455f);
        pantallaT.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MM.Caja(pantallaT, "Fondo", V(0f, 0f, 0.004f), V(0.56f, 0.24f, 0.006f), M["PanelHUD"], false);
        var pantalla = Txt(pantallaT, "", 0f, 0f, 0.17f, Color.white, 0.52f, TextAlignmentOptions.Center);
        var b = MM.Boton(p, "FORMULAR", V(0.62f, 0f, -0.08f));
        b.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        b.tapa.GetComponent<Renderer>().sharedMaterial = M["Zona6"];
        var tr = MM.Texto3D(p, "VIAL AQUÍ", V(0.36f, 0.003f, -0.2f), Quaternion.Euler(90f, 180f, 0f), 0.13f, Color.white, 0.2f);
        tr.alignment = TextAlignmentOptions.Center;
        var ts = MM.Texto3D(p, "CÁPSULA", V(0.74f, 0.003f, -0.24f), Quaternion.Euler(90f, 180f, 0f), 0.13f, Color.white, 0.2f);
        ts.alignment = TextAlignmentOptions.Center;

        var prensa = e.gameObject.AddComponent<PrensaCapsulas>();
        prensa.ranura = ranura; prensa.salida = salida; prensa.formular = b; prensa.piston = piston;
        prensa.pantalla = pantalla; prensa.luz = luz.GetComponent<Renderer>();
        prensa.capsula = CrearCapsula(p, salida);

        var sb = new System.Text.StringBuilder();
        foreach (var a in reto.agentes) sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(a.color)}><b>{a.sigla}</b></color>  ·  <b>{a.nombre}</b>: {a.accion}");
        sb.Append("<size=80%><color=#A9BED2>Modelo educativo simplificado: no son tratamientos reales.</color></size>");
        Tablero(e, "Agentes", V(0f, h + 0.75f, -1.33f), 1.9f, 1.0f, "AGENTES DISPONIBLES", sb.ToString(), 0.25f);
        guia = Cartel(e, V(-1.55f, 0f, 0.2f), 6, "PREPARACIÓN",
            "Cada agente ataca una estructura distinta. Elige el que creas que afecta a tu microorganismo y formula una cápsula.", V(0f, 2.15f, -0.35f));
        return prensa;
    }

    static void CrearVial(Transform padre, int agente, RetoData.Agente a, Vector3 pos, ZonaEncaje casa)
    {
        var go = new GameObject("Vial_" + a.sigla);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.transform;
        var liquido = MM.MatEn(MatDir, "Agente_" + a.sigla, Shader.Find("Standard"));
        liquido.color = a.color;
        liquido.EnableKeyword("_EMISSION");
        liquido.SetColor("_EmissionColor", a.color * 0.35f);
        MM.Cilindro(t, "Vidrio", V(0f, 0.05f, 0f), V(0.045f, 0.05f, 0.045f), Quaternion.identity, M["Vidrio"]);
        MM.Cilindro(t, "Liquido", V(0f, 0.036f, 0f), V(0.039f, 0.034f, 0.039f), Quaternion.identity, liquido);
        MM.Cilindro(t, "Tapon", V(0f, 0.106f, 0f), V(0.048f, 0.009f, 0.048f), Quaternion.identity, liquido);
        var brillo = MM.Cilindro(t, "Brillo", V(0f, 0.055f, 0f), V(0.06f, 0.06f, 0.06f), Quaternion.identity, M["Brillo"]);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = V(0f, 0.058f, 0f); bc.size = V(0.05f, 0.116f, 0.05f);
        var etq = MM.Texto3D(t, $"<b>{a.sigla}</b>", V(0f, 0.16f, 0f), Quaternion.identity, 0.3f, a.color, 0.15f);
        etq.alignment = TextAlignmentOptions.Center;
        etq.outlineWidth = 0.25f; etq.outlineColor = new Color32(0, 0, 0, 230);
        etq.gameObject.AddComponent<Cartel>();
        var enc = go.AddComponent<Encajable>();
        enc.tipo = "vial"; enc.casa = casa; enc.brillo = brillo.GetComponent<Renderer>(); enc.radioEncaje = 0.12f;
        go.AddComponent<VialAgente>().agente = agente;
        ConfigurarMetaISDK.HacerAgarrable(go, 0.05f);
    }

    static Capsula CrearCapsula(Transform padre, ZonaEncaje salida)
    {
        var go = new GameObject("Capsula");
        go.transform.SetParent(padre, false);
        go.transform.localPosition = salida.transform.localPosition;
        var t = go.transform;
        var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        cuerpo.name = "Cuerpo"; cuerpo.transform.SetParent(t, false);
        cuerpo.transform.localPosition = V(0f, 0.035f, 0f); cuerpo.transform.localScale = V(0.04f, 0.035f, 0.04f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = M["CapsulaCuerpo"];
        var nucleo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(nucleo.GetComponent<Collider>());
        nucleo.name = "Nucleo"; nucleo.transform.SetParent(t, false);
        nucleo.transform.localPosition = V(0f, 0.035f, 0f); nucleo.transform.localScale = Vector3.one * 0.022f;
        nucleo.GetComponent<Renderer>().sharedMaterial = M["CapsulaNucleo"];
        var brillo = MM.Cilindro(t, "Brillo", V(0f, 0.035f, 0f), V(0.055f, 0.045f, 0.055f), Quaternion.identity, M["Brillo"]);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = V(0f, 0.035f, 0f); bc.size = V(0.045f, 0.07f, 0.045f);
        var etq = MM.Texto3D(t, "", V(0f, 0.11f, 0f), Quaternion.identity, 0.28f, Color.white, 0.15f);
        etq.alignment = TextAlignmentOptions.Center;
        etq.outlineWidth = 0.25f; etq.outlineColor = new Color32(0, 0, 0, 230);
        etq.gameObject.AddComponent<Cartel>();
        var enc = go.AddComponent<Encajable>();
        enc.tipo = "capsula"; enc.casa = salida; enc.brillo = brillo.GetComponent<Renderer>(); enc.radioEncaje = 0.12f;
        var c = go.AddComponent<Capsula>();
        c.cuerpo = cuerpo.GetComponent<Renderer>(); c.nucleo = nucleo.GetComponent<Renderer>(); c.etiqueta = etq;
        ConfigurarMetaISDK.HacerAgarrable(go, 0.03f);
        go.SetActive(false);   // aparece al formularla en la prensa
        return c;
    }

    // ---------------------------------------------------------------- 7 cámara experimental

    static CamaraExperimental Camara(Transform padre, GameObject slidePrefab, out EstacionGuia guia)
    {
        // Frente hacia la puerta (-x): el usuario la ve nada más entrar.
        var e = Estacion(padre, "7_CamaraExperimental", CentroCamara, -90f, 7, "CÁMARA EXPERIMENTAL", 1.8f);
        e.Find("FranjaSuelo").localPosition = V(0f, 0.003f, 1.75f);
        e.Find("Texto").localPosition = V(0f, 0.004f, 2.05f);
        var cuerpo = MM.Grupo(e, "Cuerpo");
        var ped = MM.Cilindro(cuerpo, "Pedestal", V(0f, 0.45f, 0f), V(1.5f, 0.45f, 1.5f), Quaternion.identity, M["Negro"]);
        var cc = ped.AddComponent<CapsuleCollider>(); cc.radius = 0.5f; cc.height = 2f / 0.45f * 1.1f; cc.center = V(0f, 0.8f, 0f);
        MM.Cilindro(cuerpo, "Plataforma", V(0f, 0.91f, 0f), V(1.0f, 0.01f, 1.0f), Quaternion.identity, M["Oscuro"]);
        MM.Cilindro(cuerpo, "Vidrio", V(0f, 1.53f, 0f), V(1.24f, 0.62f, 1.24f), Quaternion.identity, M["Vidrio"]);
        MM.Cilindro(cuerpo, "Techo", V(0f, 2.19f, 0f), V(1.4f, 0.04f, 1.4f), Quaternion.identity, M["Blanco"]);
        var anilloBajo = MM.Cilindro(cuerpo, "AnilloBajo", V(0f, 0.93f, 0f), V(1.28f, 0.01f, 1.28f), Quaternion.identity, M["EmisivoCian"]);
        var anilloAlto = MM.Cilindro(cuerpo, "AnilloAlto", V(0f, 2.14f, 0f), V(1.28f, 0.012f, 1.28f), Quaternion.identity, M["EmisivoCian"]);
        MM.Senal(cuerpo, "Biohazard", e.TransformPoint(V(-0.38f, 0.45f, 1.258f)), e.rotation * Quaternion.Euler(0f, 180f, 0f), "CÁMARA\nCONTENIDA", 0.26f);
        // Consola delante (lado del usuario).
        MM.Caja(cuerpo, "Consola", V(0f, 0.47f, 1.0f), V(1.3f, 0.94f, 0.5f), M["Blanco"], true);
        MM.Caja(cuerpo, "ConsolaTapa", V(0f, 0.945f, 1.0f), V(1.34f, 0.02f, 0.54f), M["Oscuro"], true);
        var franja = MM.Caja(cuerpo, "ConsolaLuz", V(0f, 0.88f, 1.255f), V(1.3f, 0.025f, 0.01f), M["EmisivoCian"], false);
        MM.Caja(cuerpo, "Rail", V(0f, 0.957f, 0.55f), V(0.08f, 0.006f, 0.9f), M["Cromo"], false);

        var cam = e.gameObject.AddComponent<CamaraExperimental>();
        cam.lucesEstado = new[] { anilloBajo.GetComponent<Renderer>(), anilloAlto.GetComponent<Renderer>(), franja.GetComponent<Renderer>() };

        // Bandeja móvil de la placa.
        var bandeja = new GameObject("Bandeja").transform;
        bandeja.SetParent(e, false);
        bandeja.localPosition = V(0f, 0.957f, 1.02f);
        MM.Caja(bandeja, "Placa", V(0f, 0.005f, 0f), V(0.24f, 0.01f, 0.24f), M["Oscuro"], false);
        MM.Caja(bandeja, "Borde", V(0f, 0.002f, 0f), V(0.26f, 0.006f, 0.26f), M["Zona7"], false);
        cam.zonaPlaca = Zona(bandeja, "placa", V(0f, 0.011f, 0f), V(0.22f, 0.08f, 0.22f), PrimitiveType.Cylinder, V(0.17f, 0.003f, 0.17f));
        cam.bandeja = bandeja;
        cam.bandejaDentro = V(0f, 0.925f, 0f);
        cam.puntoLiberacion = MM.Vacio(e, "PuntoLiberacion", V(0f, 1.15f, 0f));
        var tp = MM.Texto3D(e, "PLACA", V(0f, 0.957f, 1.2f), Quaternion.Euler(90f, 180f, 0f), 0.16f, Color.white, 0.25f);
        tp.alignment = TextAlignmentOptions.Center;

        // Ranura de la cápsula y botón ACTIVAR.
        MM.Cilindro(e, "CunaCapsula", V(-0.42f, 0.958f, 1.08f), V(0.08f, 0.004f, 0.08f), Quaternion.identity, M["Zona6"]);
        cam.zonaCapsula = Zona(e, "capsula", V(-0.42f, 0.96f, 1.08f), V(0.1f, 0.12f, 0.1f), PrimitiveType.Cylinder, V(0.04f, 0.03f, 0.04f));
        var tc = MM.Texto3D(e, "CÁPSULA", V(-0.42f, 0.957f, 1.2f), Quaternion.Euler(90f, 180f, 0f), 0.14f, Color.white, 0.25f);
        tc.alignment = TextAlignmentOptions.Center;
        var act = MM.Boton(e, "ACTIVAR", V(0.42f, 0.955f, 1.1f));
        act.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        act.transform.localScale = Vector3.one * 1.5f;
        act.radio = 0.08f;
        act.tapa.GetComponent<Renderer>().sharedMaterial = M["EmisivoVerde"];
        cam.activar = act;

        // Bandeja de salida del portaobjetos DESPUÉS.
        MM.Caja(e, "BandejaDespues", V(0.42f, 0.96f, 0.86f), V(0.13f, 0.012f, 0.08f), M["Oscuro"], false);
        cam.anclajeDespues = MM.Vacio(e, "AnclajeDespues", V(0.42f, 0.975f, 0.86f));
        var td = MM.Texto3D(e, "DESPUÉS", V(0.42f, 0.967f, 0.79f), Quaternion.Euler(90f, 180f, 0f), 0.12f, Color.white, 0.2f);
        td.alignment = TextAlignmentOptions.Center;
        var porta = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, e);
        porta.name = "PortaobjetosDespues";
        porta.transform.localPosition = V(0.42f, 0.975f, 0.86f);
        porta.SetActive(false);
        cam.portaDespues = porta.GetComponent<Portaobjetos>();

        // Pantalla de estado sobre la consola (alta, para no tapar la placa dentro de la cámara).
        MM.Caja(cuerpo, "Poste", V(-0.62f, 1.3f, 0.78f), V(0.04f, 0.75f, 0.04f), M["Marco"], false);
        var pant = new GameObject("Pantalla").transform;
        pant.SetParent(e, false);
        pant.localPosition = V(-0.2f, 1.78f, 0.78f);
        pant.localRotation = Quaternion.Euler(-12f, 180f, 0f);
        MM.Caja(pant, "Fondo", V(0f, 0f, 0.01f), V(0.95f, 0.36f, 0.02f), M["PanelHUD"], false);
        MM.Caja(pant, "Borde", V(0f, 0f, 0.012f), V(0.97f, 0.38f, 0.018f), M["Zona7"], false);
        cam.pantalla = Txt(pant, "", 0f, 0f, 0.26f, Color.white, 0.9f, TextAlignmentOptions.Center);

        // Resultado sobre la cámara: icono con forma (V / X) + texto.
        var res = new GameObject("Resultado").transform;
        res.SetParent(e, false);
        res.localPosition = V(0f, 2.6f, 0.3f);
        res.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MM.Caja(res, "Fondo", V(0f, 0f, 0.01f), V(1.9f, 0.5f, 0.02f), M["PanelHUD"], false);
        cam.resultado = Txt(res, "", 0.22f, 0f, 0.46f, Color.white, 1.36f, TextAlignmentOptions.Center);
        cam.resultado.fontStyle = FontStyles.Bold;
        var ok = MM.Grupo(res, "IconoExito");
        ok.localPosition = V(-0.68f, 0f, -0.02f);
        MM.Caja(ok, "Corto", V(-0.06f, -0.03f, 0f), V(0.05f, 0.16f, 0.02f), M["EmisivoVerde"], false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        MM.Caja(ok, "Largo", V(0.05f, 0.03f, 0f), V(0.05f, 0.3f, 0.02f), M["EmisivoVerde"], false).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        var ko = MM.Grupo(res, "IconoFallo");
        ko.localPosition = V(-0.68f, 0f, -0.02f);
        MM.Caja(ko, "A", Vector3.zero, V(0.05f, 0.32f, 0.02f), M["EmisivoRojo"], false).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        MM.Caja(ko, "B", Vector3.zero, V(0.05f, 0.32f, 0.02f), M["EmisivoRojo"], false).transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        cam.iconoExito = ok.gameObject; cam.iconoFallo = ko.gameObject;
        ok.gameObject.SetActive(false); ko.gameObject.SetActive(false);

        var luz = new GameObject("LuzResultado").AddComponent<Light>();
        luz.transform.SetParent(e, false);
        luz.transform.localPosition = V(0f, 1.5f, 0f);
        luz.type = LightType.Point; luz.range = 3.5f; luz.intensity = 2.2f;
        luz.lightmapBakeType = LightmapBakeType.Realtime; luz.shadows = LightShadows.None;
        luz.enabled = false;
        cam.luzResultado = luz;

        cam.niebla = Niebla(e);
        FijoPrimitiva(cuerpo.gameObject);
        guia = Cartel(e, V(-1.2f, 0f, 1.45f), 7, "CÁMARA EXPERIMENTAL",
            "Aquí se prueba tu intervención de forma segura: la cápsula actúa sobre la placa dentro de la cámara, no al aire libre.", V(0f, 3.0f, 0f));
        return cam;
    }

    static ParticleSystem Niebla(Transform e)
    {
        var go = new GameObject("Niebla");
        go.transform.SetParent(e, false);
        go.transform.localPosition = V(0f, 0.95f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = true; main.duration = 2f;
        main.startLifetime = 2.6f; main.startSpeed = 0.18f; main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.maxParticles = 70; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startColor = new Color(0.6f, 0.9f, 1f, 0.35f);
        var em = ps.emission; em.rateOverTime = 22f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.42f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        return ps;
    }

    // ---------------------------------------------------------------- 8 archivo de recuerdos de Baymax

    static PanelResumen Archivo(Transform padre, out EstacionGuia guia)
    {
        var e = Estacion(padre, "8_Archivo", V(10.2f, 0f, 16.6f), 180f, 8, "ARCHIVO DE RECUERDOS", 3.2f);
        float fondo = -(Z1 - 16.6f);
        var p = new GameObject("Panel").transform;
        p.SetParent(e, false);
        p.localPosition = V(0f, 1.8f, fondo + 0.03f);
        p.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MM.Caja(p, "Fondo", V(0f, 0f, 0.01f), V(3.4f, 1.75f, 0.03f), M["PanelHUD"], true);
        MM.Caja(p, "Borde", V(0f, 0f, 0.012f), V(3.43f, 1.78f, 0.028f), M["Zona8"], false);
        var r = p.gameObject.AddComponent<PanelResumen>();
        r.titulo = Txt(p, "", 0f, 0.74f, 0.55f, ColorZona[7], 3.2f, TextAlignmentOptions.Center);
        r.titulo.fontStyle = FontStyles.Bold;
        r.izquierda = Txt(p, "", -0.83f, 0.6f, 0.3f, Color.white, 1.55f, TextAlignmentOptions.TopLeft);
        r.izquierda.rectTransform.pivot = new Vector2(0.5f, 1f);
        r.izquierda.rectTransform.sizeDelta = new Vector2(1.55f, 1.2f);
        r.derecha = Txt(p, "", 0.83f, 0.6f, 0.28f, Color.white, 1.55f, TextAlignmentOptions.TopLeft);
        r.derecha.rectTransform.pivot = new Vector2(0.5f, 1f);
        r.derecha.rectTransform.sizeDelta = new Vector2(1.55f, 1.2f);
        MM.Caja(p, "Separador", V(0f, 0f, -0.005f), V(0.006f, 1.2f, 0.004f), M["Zona8"], false);
        // Recuerdos de Baymax (3 ranuras con número).
        r.fragmentos = new Renderer[3];
        for (int i = 0; i < 3; i++)
        {
            var f = MM.Caja(p, "Fragmento" + i, V(-1.45f + i * 0.2f, -0.72f, -0.01f), V(0.16f, 0.16f, 0.02f), M["BlancoMate"], false);
            r.fragmentos[i] = f.GetComponent<Renderer>();
            var n = Txt(p, $"0{i + 1}", -1.45f + i * 0.2f, -0.72f, 0.2f, Color.black, 0.15f, TextAlignmentOptions.Center);
            n.transform.localPosition += V(0f, 0f, -0.012f);
            n.fontStyle = FontStyles.Bold;
        }
        r.indiceFragmento = 0;
        r.fragmentoTxt = Txt(p, "", 0.35f, -0.72f, 0.28f, ColorZona[7], 2.4f, TextAlignmentOptions.Left);

        // Consola con REINICIAR.
        var c = MM.Grupo(e, "Consola");
        MM.Caja(c, "Pie", V(1.3f, 0.45f, -0.25f), V(0.4f, 0.9f, 0.35f), M["Blanco"], true);
        MM.Caja(c, "Tapa", V(1.3f, 0.91f, -0.25f), V(0.44f, 0.02f, 0.39f), M["Oscuro"], true);
        FijoPrimitiva(c.gameObject);
        var b = MM.Boton(e, "REINICIAR", V(1.3f, 0.92f, -0.22f));
        b.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        r.reiniciar = b;
        var tb = MM.Texto3D(e, "REPETIR EL CASO", V(1.3f, 0.922f, -0.38f), Quaternion.Euler(90f, 180f, 0f), 0.16f, Color.white, 0.4f);
        tb.alignment = TextAlignmentOptions.Center;
        guia = Cartel(e, V(-2.15f, 0f, 0.15f), 8, "ARCHIVO DE RECUERDOS",
            "Cada caso resuelto le devuelve a Baymax un recuerdo científico. Aquí verás tu informe.", V(0f, 2.95f, fondo + 0.4f));
        return r;
    }

    // ---------------------------------------------------------------- guía global

    static PanelObjetivo[] TorreObjetivo(Transform raiz)
    {
        var t = MM.Grupo(raiz, "TorreObjetivo");
        t.position = CentroCamara + Vector3.up * (suelo + 3.6f);
        MM.Caja(t, "Nucleo", Vector3.zero, V(2.3f, 0.95f, 2.3f), M["Oscuro"], false);
        foreach (var x in new[] { -0.9f, 0.9f }) MM.Caja(t, "Tirante", V(x, 0.55f, 0f), V(0.03f, 0.2f, 0.03f), M["Cromo"], false);
        var paneles = new List<PanelObjetivo>();
        for (int i = 0; i < 4; i++)
        {
            var rot = Quaternion.Euler(0f, i * 90f, 0f);
            // La cara -z de cada panel mira hacia fuera.
            var p = CrearPanelObjetivo(t, "Panel" + i, rot * V(0f, 0f, -1.17f), rot, 2.3f, 0.95f, true);
            paneles.Add(p);
        }
        return paneles.ToArray();
    }

    static PanelObjetivo CrearPanelObjetivo(Transform padre, string nombre, Vector3 pos, Quaternion rot, float ancho, float alto, bool conFondo)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = pos;
        t.localRotation = rot;
        if (conFondo)
        {
            MM.Caja(t, "Fondo", V(0f, 0f, 0.01f), V(ancho, alto, 0.03f), M["PanelHUD"], false);
            MM.Caja(t, "Borde", V(0f, 0f, 0.012f), V(ancho + 0.03f, alto + 0.03f, 0.028f), M["EmisivoCian"], false);
        }
        var p = t.gameObject.AddComponent<PanelObjetivo>();
        p.titulo = Txt(t, "", 0f, alto * 0.39f, 0.36f, Cian, ancho - 0.1f, TextAlignmentOptions.Center);
        p.titulo.fontStyle = FontStyles.Bold;
        int n = RetoManager.Ciclo.Length;
        float w = (ancho - 0.16f) / n;
        p.segmentos = new Renderer[n];
        p.etiquetas = new TMP_Text[n];
        for (int i = 0; i < n; i++)
        {
            float x = -ancho / 2f + 0.08f + (i + 0.5f) * w;
            p.segmentos[i] = MM.Caja(t, "Seg" + i, V(x, alto * 0.2f, -0.004f), V(w - 0.02f, 0.06f, 0.008f), M["BlancoMate"], false).GetComponent<Renderer>();
            p.etiquetas[i] = Txt(t, RetoManager.Ciclo[i], x, alto * 0.1f, 0.13f, Color.white, w, TextAlignmentOptions.Center);
        }
        p.objetivo = Txt(t, "", 0f, -alto * 0.12f, 0.36f, Color.white, ancho - 0.14f, TextAlignmentOptions.Center);
        p.estado = Txt(t, "", 0f, -alto * 0.38f, 0.22f, Gris, ancho - 0.14f, TextAlignmentOptions.Center);
        return p;
    }

    static Baliza Baliza(Transform raiz)
    {
        var go = new GameObject("Baliza");
        go.transform.SetParent(raiz, false);
        var b = go.AddComponent<Baliza>();
        var rombo = MM.Grupo(go.transform, "Rombo");
        MM.Caja(rombo, "Gema", Vector3.zero, V(0.13f, 0.13f, 0.13f), M["EmisivoAmarillo"], false).transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        MM.Cilindro(go.transform, "Haz", V(0f, -0.55f, 0f), V(0.05f, 0.45f, 0.05f), Quaternion.identity, M["Brillo"]);
        b.rombo = rombo;
        return b;
    }

    static PanelObjetivo Recepcion(Transform raiz)
    {
        var r = MM.Grupo(raiz, "Recepcion");
        var puerta = V(ParedX1 - 0.4f, 0f, (PuertaZ0 + PuertaZ1) / 2f);
        var bien = MM.Totem(r, "Bienvenida", V(8.75f, 0f, 10.7f), puerta, 1.6f, 1.45f);
        var t1 = Txt(bien, "CENTRO DE EXPERIMENTACIÓN\nMICROBIOLÓGICA", 0f, 0.6f, 0.4f, Cian, 1.5f, TextAlignmentOptions.Center);
        t1.fontStyle = FontStyles.Bold;
        var cuerpo = Txt(bien,
            "<color=#FFD25A><b>BAYMAX:</b></color> perdiste tus <b>recuerdos científicos</b>. En este centro se investigan casos: cada caso que resuelvas te devuelve un recuerdo.\n\n" +
            $"<b>{reto.codigo} · {reto.titulo}</b>\n{reto.historia}\n\n" +
            "<color=#8FD8FF>Joystick izq.: caminar · joystick der.: girar · gatillo/grip: agarrar · acerca la mano a un botón para pulsarlo · cada estación tiene un botón PISTA.</color>",
            0f, 0.42f, 0.235f, Color.white, 1.48f, TextAlignmentOptions.TopLeft);
        cuerpo.rectTransform.pivot = new Vector2(0.5f, 1f);
        cuerpo.rectTransform.sizeDelta = new Vector2(1.48f, 1.0f);
        var aviso = Txt(bien, "Experiencia educativa simplificada: las muestras, agentes y resultados no son protocolos clínicos ni tratamientos reales.", 0f, -0.64f, 0.17f, Gris, 1.48f, TextAlignmentOptions.Center);
        aviso.fontStyle = FontStyles.Italic;
        FijoPrimitiva(bien.parent.Find("Pie").gameObject);

        var obj = MM.Totem(r, "TuObjetivo", V(8.2f, 0f, 5.0f), puerta, 1.6f, 0.95f);
        var p = CrearPanelObjetivo(obj, "Objetivo", Vector3.zero, Quaternion.identity, 1.6f, 0.95f, false);
        return p;
    }

    // Equipos de un laboratorio de microbiología (ambientación, no interactivos).
    static void Equipamiento(Transform g)
    {
        Equipo(g, "Incubadora", V(X0 + 0.38f, 0f, 1.8f), 90f, V(0.8f, 1.6f, 0.7f), "INCUBADORA", "37.0 °C", false);
        Equipo(g, "Autoclave", V(X0 + 0.38f, 0f, 3.35f), 90f, V(0.8f, 1.05f, 0.7f), "AUTOCLAVE", "121 °C · LISTO", true);
        Equipo(g, "Ultracongelador", V(7.75f, 0f, Z1 - 0.38f), 180f, V(0.8f, 1.9f, 0.7f), "ULTRACONGELADOR", "-80 °C", false);
        Equipo(g, "IncubadoraCO2", V(X1 - 0.38f, 0f, 1.8f), -90f, V(0.8f, 1.6f, 0.7f), "INCUBADORA CO2", "37 °C · 5 %", false);

        // Cabina de bioseguridad clase II (abierta por delante, luz azul).
        var c = new GameObject("CabinaBioseguridad").transform;
        c.SetParent(g, false);
        c.position = V(X1 - 0.4f, suelo, 14.6f);
        c.rotation = Quaternion.Euler(0f, -90f, 0f);
        MM.Caja(c, "Base", V(0f, 0.45f, 0f), V(1.6f, 0.9f, 0.75f), M["Blanco"], true);
        MM.Caja(c, "Fondo", V(0f, 1.4f, -0.33f), V(1.6f, 1.0f, 0.08f), M["Blanco"], true);
        MM.Caja(c, "LadoI", V(-0.77f, 1.4f, 0f), V(0.06f, 1.0f, 0.75f), M["Blanco"], true);
        MM.Caja(c, "LadoD", V(0.77f, 1.4f, 0f), V(0.06f, 1.0f, 0.75f), M["Blanco"], true);
        MM.Caja(c, "Techo", V(0f, 2.05f, 0f), V(1.6f, 0.3f, 0.75f), M["Blanco"], true);
        MM.Caja(c, "Mesa", V(0f, 0.91f, 0f), V(1.48f, 0.02f, 0.66f), M["Cromo"], false);
        MM.Caja(c, "LuzUV", V(0f, 1.88f, -0.1f), V(1.4f, 0.02f, 0.04f), M["EmisivoAzul"], false);
        var vidrio = MM.Caja(c, "Ventana", V(0f, 1.55f, 0.33f), V(1.48f, 0.6f, 0.01f), M["Vidrio"], false);
        vidrio.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        var t = MM.Texto3D(c, "CABINA DE BIOSEGURIDAD · CLASE II", V(0f, 2.05f, 0.38f), Quaternion.Euler(0f, 180f, 0f), 0.3f, Color.black, 1.5f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        MM.Senal(c, "Biohazard", c.TransformPoint(V(0.55f, 0.55f, 0.38f)), c.rotation * Quaternion.Euler(0f, 180f, 0f), "RIESGO\nBIOLÓGICO", 0.2f);
        FijoPrimitiva(c.gameObject);
    }

    static void Equipo(Transform g, string nombre, Vector3 pos, float yaw, Vector3 tam, string rotulo, string pantalla, bool redondo)
    {
        var e = new GameObject(nombre).transform;
        e.SetParent(g, false);
        e.position = V(pos.x, suelo, pos.z);
        e.rotation = Quaternion.Euler(0f, yaw, 0f);
        float frente = tam.z / 2f;
        MM.Caja(e, "Cuerpo", V(0f, tam.y / 2f, 0f), tam, M["Blanco"], true);
        if (redondo)
        {
            MM.Cilindro(e, "Puerta", V(0f, tam.y * 0.5f, frente + 0.01f), V(0.55f, 0.02f, 0.55f), Quaternion.Euler(90f, 0f, 0f), M["Cromo"]);
            MM.Cilindro(e, "Volante", V(0f, tam.y * 0.5f, frente + 0.04f), V(0.18f, 0.02f, 0.18f), Quaternion.Euler(90f, 0f, 0f), M["Negro"]);
        }
        else
        {
            MM.Caja(e, "Puerta", V(0f, tam.y * 0.45f, frente + 0.005f), V(tam.x - 0.1f, tam.y * 0.75f, 0.01f), M["Vidrio"], false);
            MM.Caja(e, "Interior", V(0f, tam.y * 0.45f, frente - 0.02f), V(tam.x - 0.14f, tam.y * 0.72f, 0.02f), M["Oscuro"], false);
            for (int i = 1; i <= 3; i++) MM.Caja(e, "Balda", V(0f, tam.y * (0.1f + i * 0.18f), frente - 0.05f), V(tam.x - 0.16f, 0.01f, 0.08f), M["Cromo"], false);
            MM.Caja(e, "Tirador", V(tam.x / 2f - 0.08f, tam.y * 0.45f, frente + 0.03f), V(0.03f, 0.3f, 0.03f), M["Cromo"], false);
        }
        MM.Caja(e, "Display", V(0f, tam.y - 0.12f, frente + 0.005f), V(0.36f, 0.12f, 0.01f), M["PanelHUD"], false);
        var d = MM.Texto3D(e, pantalla, V(0f, tam.y - 0.12f, frente + 0.012f), Quaternion.Euler(0f, 180f, 0f), 0.3f, new Color(0.4f, 1f, 0.6f), 0.34f);
        d.alignment = TextAlignmentOptions.Center; d.fontStyle = FontStyles.Bold;
        var r = MM.Texto3D(e, rotulo, V(0f, tam.y + 0.08f, frente - 0.1f), Quaternion.Euler(0f, 180f, 0f), 0.3f, Cian, tam.x + 0.4f);
        r.alignment = TextAlignmentOptions.Center; r.fontStyle = FontStyles.Bold;
        FijoPrimitiva(e.gameObject);
    }

    // Carteles en la sala principal para encontrar la puerta.
    static void SenalesSalaPrincipal(Transform raiz)
    {
        var s = MM.Grupo(raiz, "SenalesSalaPrincipal");
        Rotulo(s, V(ParedX0 - 0.03f, suelo + AltoPuerta + 0.35f, (PuertaZ0 + PuertaZ1) / 2f), 90f, "CENTRO DE EXPERIMENTACIÓN", $"{reto.codigo} · {reto.titulo}");
        Rotulo(s, V(ParedX1 + 0.03f, suelo + AltoPuerta + 0.35f, (PuertaZ0 + PuertaZ1) / 2f), -90f, "SALIDA", "Laboratorio de microbiología");
        // Tótem-flecha en el pasillo de la sala principal.
        var tot = MM.Totem(s, "HaciaElCentro", V(3.2f, 0f, 6.85f), V(0f, 0f, 8.1f), 0.9f, 0.45f);
        var t = Txt(tot, "CENTRO DE EXPERIMENTACIÓN  >\n<size=70%><color=#FFD25A>¡Nuevo! Resuelve el " + reto.codigo + "</color></size>", 0f, 0f, 0.3f, Cian, 0.85f, TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
    }

    static void Rotulo(Transform padre, Vector3 pos, float yaw, string titulo, string sub)
    {
        var t = new GameObject("Rotulo").transform;
        t.SetParent(padre, false);
        t.position = pos;
        t.rotation = Quaternion.Euler(0f, yaw, 0f);
        MM.Caja(t, "Fondo", V(0f, 0f, 0.01f), V(1.7f, 0.42f, 0.02f), M["PanelHUD"], false);
        MM.Caja(t, "Borde", V(0f, 0f, 0.012f), V(1.73f, 0.45f, 0.018f), M["EmisivoCian"], false);
        var tt = Txt(t, $"<b>{titulo}</b>\n<size=65%>{sub}</size>", 0f, 0f, 0.36f, Color.white, 1.6f, TextAlignmentOptions.Center);
    }

    // ================================================================== datos

    static void MaterialesCentro()
    {
        var std = Shader.Find("Standard");
        MM.Mat("AgarSangre", std, new Color(0.52f, 0.05f, 0.07f), 0f, 0.8f);
        MM.Mat("Colonia", std, new Color(0.96f, 0.8f, 0.4f), 0f, 0.85f);
        MM.Transparente("Halo", new Color(0.95f, 0.6f, 0.52f, 0.8f), 0.75f);
        MM.Transparente("CapsulaCuerpo", new Color(1f, 1f, 1f, 0.5f), 0.95f);
        MM.Emisivo("CapsulaNucleo", Color.white, 1.5f);
        MM.Emisivo("EmisivoVerde", new Color(0.25f, 1f, 0.45f), 1.8f);
        MM.Emisivo("EmisivoRojo", new Color(1f, 0.25f, 0.2f), 1.8f);
        MM.Emisivo("EmisivoAmarillo", new Color(1f, 0.8f, 0.25f), 1.8f);
        for (int i = 0; i < ColorZona.Length; i++) MM.Emisivo("Zona" + (i + 1), ColorZona[i], 1.4f);
        AssetDatabase.SaveAssets();
    }

    static RetoData CrearReto()
    {
        Directory.CreateDirectory(RetoDir);
        var r = AssetDatabase.LoadAssetAtPath<RetoData>(RetoPath);
        bool nuevo = r == null;
        if (nuevo) r = ScriptableObject.CreateInstance<RetoData>();
        MuestraData Muestra(string n) => AssetDatabase.LoadAssetAtPath<MuestraData>($"Assets/NanoLab/Muestras/{n}.asset");

        r.codigo = "CASO 01";
        r.titulo = "La herida que no sana";
        r.historia = "Llegó al centro una muestra de una herida de piel que no termina de sanar. Nadie sabe qué microorganismo hay en ella. Obsérvala, investiga, propón una hipótesis y prueba qué intervención la detiene.";
        r.origen = "Herida de piel";
        r.muestra = Muestra("Saureus");
        r.opciones = new[] { Muestra("Ecoli"), Muestra("Saureus"), Muestra("Bsubtilis") };
        r.evidencias = new[]
        {
            new RetoData.Evidencia { clave = "Forma", valor = "cocos (esféricas)", aumentoMinimo = 1, comoObtenerla = "tíñela y mira a 400x" },
            new RetoData.Evidencia { clave = "Agrupación", valor = "en racimos, como uvas", aumentoMinimo = 2, comoObtenerla = "sube a 1000x" },
            new RetoData.Evidencia { clave = "Gram", valor = "positiva (VIOLETA)", aumentoMinimo = 0, requiereGram = true, comoObtenerla = "completa la tinción 1-2-3-4" },
        };
        r.agentes = new[]
        {
            new RetoData.Agente { sigla = "PG", nombre = "Agente PG", accion = "Impide construir la pared de peptidoglucano.", color = new Color(0.25f, 0.85f, 1f), diana = DianaAgente.ParedPeptidoglucano,
                pistaSiFalla = "El agente PG debilita la pared de peptidoglucano: funciona mejor en bacterias con pared gruesa." },
            new RetoData.Agente { sigla = "ME", nombre = "Agente ME", accion = "Rompe la membrana externa de las bacterias Gram -.", color = new Color(1f, 0.6f, 0.2f), diana = DianaAgente.MembranaExterna,
                pistaSiFalla = "El agente ME ataca la MEMBRANA EXTERNA, que solo tienen las Gram - (las que quedan ROSAS). Tu muestra quedó VIOLETA: ¿tiene esa membrana? Revisa el panel de pared celular de la BIBLIOTECA (4)." },
            new RetoData.Agente { sigla = "AV", nombre = "Antiviral", accion = "Bloquea la copia de los virus dentro de las células.", color = new Color(0.78f, 0.45f, 1f), diana = DianaAgente.Virus,
                pistaSiFalla = "Los antivirales actúan sobre virus, que miden ~100 nm y no se ven con este microscopio. Tú observaste células de ~1 µm: bacterias. ¿Qué agente ataca la pared de una bacteria?" },
            new RetoData.Agente { sigla = "SAL", nombre = "Solución salina", accion = "Control: no ataca ninguna estructura.", color = new Color(0.82f, 0.86f, 0.9f), diana = DianaAgente.Ninguna,
                pistaSiFalla = "La solución salina es un CONTROL: sirve para comparar, pero no detiene el crecimiento (por eso hay más bacterias). Elige un agente que ataque una estructura que tu muestra sí tiene." },
        };
        r.umbralExito = 0.7f;
        r.pistasExploracion = new[]
        {
            "Busca el BANCO DE MUESTRAS (zona 1, luz cian): la placa del CASO 01 es la única activa.",
            "Agarra un portaobjetos de la bandeja de la mesa de TINCIÓN (2) y toca con él la placa: así se hace el frotis.",
        };
        r.pistasTincion = new[]
        {
            "La tinción de Gram tiene 4 pasos y el orden importa: 1 cristal violeta, 2 lugol, 3 alcohol-acetona, 4 safranina.",
            "Lleva el portaobjetos en la mano y tócalo con cada gotero, de izquierda a derecha. Si te equivocas, toca el desinfectante y empieza de nuevo.",
        };
        r.pistasObservacion = new[]
        {
            "Suelta el portaobjetos sobre la silueta azul de la platina y acerca la cara al ocular.",
            "Con A o X subes el aumento. Cómo se agrupan las células solo se distingue bien a 1000x.",
        };
        r.pistasInvestigacion = new[]
        {
            "Abre las fichas de la BIBLIOTECA (4) y busca cuál coincide en forma, agrupación y color Gram.",
            "Abre también el panel 'Pared celular': explica qué significa que tu muestra quedara VIOLETA.",
        };
        r.pistasHipotesis = new[]
        {
            "Tu muestra: cocos, en racimos y Gram positiva (violeta). ¿Qué ficha describe exactamente eso?",
            "Pulsa en la TERMINAL (5) el botón del microorganismo que elijas. Puedes cambiarlo antes de experimentar.",
        };
        r.pistasPreparacion = new[]
        {
            "Cada vial ataca una estructura distinta. Lee el tablero AGENTES DISPONIBLES detrás de la prensa.",
            "Las Gram + tienen una pared GRUESA de peptidoglucano y NO tienen membrana externa. ¿Qué agente ataca lo que sí tienen?",
        };
        r.pistasExperimento = new[]
        {
            "La placa va en la bandeja de la cámara y la cápsula en su ranura (siluetas azules).",
            "Cuando las dos estén encajadas, acerca la mano al botón verde ACTIVAR.",
        };
        r.pistasAnalisis = new[]
        {
            "Toma el portaobjetos DESPUÉS de la bandeja de la cámara y ponlo en un MICROSCOPIO (3).",
            "Compara la vista central (DESPUÉS) con la imagen ANTES de la derecha: ¿hay menos células? ¿están rotas?",
        };
        r.explicacion = "S. aureus es una bacteria Gram positiva: su pared de peptidoglucano es gruesa y retiene el cristal violeta (por eso la viste VIOLETA). Forma cocos agrupados en racimos. " +
                        "Un agente que impide construir esa pared (como los antibióticos betalactámicos) la debilita hasta que la célula se rompe. " +
                        "El agente ME no sirve porque ataca una membrana externa que las Gram + no tienen, y un antiviral no actúa sobre bacterias.";
        r.fragmento = "Recuerdo científico 1/3";
        r.textoFragmento = "Baymax recuerda: \"La estructura decide el efecto: primero observa, después interviene.\"";
        GenerarTexturas(r);

        if (nuevo) AssetDatabase.CreateAsset(r, RetoPath);
        EditorUtility.SetDirty(r);
        AssetDatabase.SaveAssets();
        return r;
    }

    // Vistas del microscopio derivadas de la micrografía real de S. aureus.
    static void GenerarTexturas(RetoData r)
    {
        string fuente = Path.Combine(ImgDir, "saureus.jpg");
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(fuente));
        int w = tex.width, h = tex.height;
        float esc = Mathf.Min(1f, 1024f / Mathf.Max(w, h));
        int W = Mathf.RoundToInt(w * esc), H = Mathf.RoundToInt(h * esc);
        var src = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                src[y * W + x] = tex.GetPixelBilinear((x + 0.5f) / W, (y + 0.5f) / H);
        Object.DestroyImmediate(tex);

        // Color de fondo: media de los píxeles claros.
        Color fondo = Color.black; int nf = 0;
        foreach (var c in src) if (Lum(c) > 0.72f) { fondo += c; nf++; }
        fondo = nf > 0 ? fondo / nf : new Color(0.9f, 0.88f, 0.9f);

        r.vistaSinTenir = Guardar("saureus_sin_tenir", W, H, src, (c, x, y) =>
        {
            float v = Mathf.Lerp(Lum(c), 0.86f, 0.78f);
            return new Color(v, v, v * 0.98f);
        });
        r.vistaTincionFallida = Guardar("saureus_tincion_fallida", W, H, src, (c, x, y) =>
        {
            Color.RGBToHSV(c, out var hh, out var s, out var v);
            if (s < 0.16f) return c;
            return Color.HSVToRGB(0.94f, s * 0.75f, Mathf.Min(1f, v * 1.12f));
        });
        r.vistaDespuesEficaz = Guardar("saureus_despues", W, H, src, (c, x, y) =>
        {
            Color.RGBToHSV(c, out _, out var s, out _);
            float celula = Mathf.Clamp01((s - 0.14f) / 0.22f);
            bool sobrevive = Mathf.PerlinNoise(x * 0.012f + 3.1f, y * 0.012f + 7.7f) > 0.74f;
            if (sobrevive || celula <= 0f) return c;
            // Célula lisada: queda un "fantasma" pálido.
            var fantasma = Color.Lerp(fondo, new Color(0.8f, 0.7f, 0.82f), 0.35f);
            return Color.Lerp(c, fantasma, celula * 0.9f);
        });
        r.vistaDespuesCrecimiento = Guardar("saureus_crecimiento", W, H, src, (c, x, y) =>
        {
            int x2 = (x + W * 13 / 100) % W, y2 = (y + H * 9 / 100) % H;
            var c2 = src[y2 * W + x2];
            return Lum(c2) < Lum(c) ? c2 : c;
        });
    }

    static float Lum(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;

    static Texture2D Guardar(string nombre, int W, int H, Color[] src, System.Func<Color, int, int, Color> f)
    {
        var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        var px = new Color[src.Length];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                px[y * W + x] = f(src[y * W + x], x, y);
        t.SetPixels(px);
        t.Apply();
        string path = $"{ImgDir}/{nombre}.png";
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter ti)
        {
            ti.maxTextureSize = 1024; ti.mipmapEnabled = true; ti.textureCompression = TextureImporterCompression.Compressed;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ================================================================== utilidades

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    static TextMeshPro Txt(Transform padre, string texto, float x, float y, float tam, Color color, float ancho, TextAlignmentOptions al)
    {
        var t = MM.Texto3D(padre, texto, V(x, y, -0.02f), Quaternion.identity, tam, color, ancho);
        t.alignment = al;
        return t;
    }

    // ================================================================== verificación

    [MenuItem("NanoLab/Capturas del centro de experimentación", false, 3)]
    public static void Capturas()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        float y = ML.AlturaSuelo(ML.BoundsEscena());
        ML.Capturar(dir, "centro_1_puerta_desde_sala", V(1.5f, y + 1.65f, 8.1f), V(1f, -0.05f, 0f));
        ML.Capturar(dir, "centro_2_entrada", V(7.6f, y + 1.65f, 8.1f), V(1f, -0.12f, 0f));
        ML.Capturar(dir, "centro_3_banco", V(10.5f, y + 1.6f, 2.4f), V(0f, -0.55f, -1f));
        ML.Capturar(dir, "centro_4_tincion", V(15.2f, y + 1.55f, 2.7f), V(0f, -0.65f, -1f));
        ML.Capturar(dir, "centro_5_microscopia", V(20.5f, y + 1.6f, 2.8f), V(0f, -0.55f, -1f));
        ML.Capturar(dir, "centro_6_biblioteca", V(21.0f, y + 1.65f, 8.6f), V(1f, -0.05f, 0f));
        ML.Capturar(dir, "centro_7_terminal_prensa", V(18.0f, y + 1.65f, 12.8f), V(0.05f, -0.3f, 1f));
        ML.Capturar(dir, "centro_8_camara", V(12.6f, y + 1.7f, 8.5f), V(1f, -0.15f, 0f));
        ML.Capturar(dir, "centro_9_archivo", V(10.2f, y + 1.65f, 13.4f), V(0f, -0.05f, 1f));
        ML.Capturar(dir, "centro_10_general", V(8.0f, y + 3.6f, 1.0f), V(1f, -0.45f, 0.8f));
        VistaCenital.Renderizar(ScenePath);
        Debug.Log("[Centro] Capturas guardadas.");
    }
}
