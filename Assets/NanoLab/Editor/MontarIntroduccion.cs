using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using MM = MontarMicrobiologia;
using ML = MontarLaboratorio;

// Introducción narrativa (RF-01): tótem estático frente al punto de aparición. El estudiante es Baymax,
// ha perdido sus recuerdos científicos y debe recuperarlos investigando. Lo invita a mirarse en el espejo
// del recibidor, lo orienta hacia las áreas (hasta el banco de muestras del Centro) y muestra el
// indicador de recuerdos recuperados. Incluye el aviso de contenido simplificado (RNF-06).
// Además coloca las balizas numeradas sobre cada área del laboratorio (diseño de Luis).
public static class MontarIntroduccion
{
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Quimica = "Assets/NanoLab/Scenes/Laboratorio.unity";
    static readonly Vector3 Spawn = new Vector3(-3.3f, 0f, 2.24f);
    static readonly Vector3 PosIntro = new Vector3(-2.2f, 0f, 3.35f);
    public static readonly Vector3 PosTutorial = new Vector3(-0.6f, 0f, 3.6f);

    [MenuItem("NanoLab/Montar introducción Baymax", false, 26)]
    public static void AplicarEscenaMicrobiologia()
    {
        var s = EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        MM.CrearMateriales();
        Aplicar(s);
        EditorSceneManager.SaveScene(s);
        OrdenarEscenas();
    }

    public static void Aplicar(Scene s)
    {
        if (MM.mats == null) MM.CrearMateriales();
        MM.suelo = ML.AlturaSuelo(ML.BoundsEscena());

        var tutorial = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Tutorial");
        var padre = tutorial != null ? tutorial.parent : new GameObject("Bienvenida").transform;
        // Quita la introducción y las balizas anteriores (también el diálogo de la versión previa).
        foreach (Transform hijo in padre.Cast<Transform>().ToArray())
            if (hijo.name == "Introduccion" || hijo.name == "AreasLaboratorio") Object.DestroyImmediate(hijo.gameObject);

        // El tutorial de portaobjetos se aparta a la derecha para dejar la introducción delante.
        if (tutorial != null)
        {
            tutorial.position = new Vector3(PosTutorial.x, MM.suelo, PosTutorial.z);
            var d = Spawn - PosTutorial; d.y = 0f;
            tutorial.rotation = Quaternion.LookRotation(-d.normalized);
        }

        Construir(padre);
        Areas(padre);
        Debug.Log($"[Intro] {s.name}: introducción de Baymax montada en {PosIntro} y {AreasLab.Length} balizas de áreas.");
    }

    static void Construir(Transform padre)
    {
        var panel = MM.Totem(padre, "Introduccion", PosIntro, Spawn, 1.4f, 1.62f);
        var raiz = panel.parent;
        var cian = new Color(0.4f, 0.9f, 1f);
        var amarillo = new Color(1f, 0.82f, 0.35f);

        // Cara de Baymax (dos ojos unidos por una línea) junto al título.
        var cara = MM.Grupo(panel, "CaraBaymax");
        cara.localPosition = new Vector3(-0.52f, 0.64f, -0.02f);
        MM.Cilindro(cara, "Cara", Vector3.zero, new Vector3(0.24f, 0.004f, 0.2f), Quaternion.Euler(90f, 0f, 0f), MM.mats["BlancoMate"]);
        foreach (var x in new[] { -0.055f, 0.055f })
            MM.Cilindro(cara, "Ojo", new Vector3(x, 0f, -0.005f), new Vector3(0.04f, 0.002f, 0.04f), Quaternion.Euler(90f, 0f, 0f), MM.mats["NegroMate"]);
        MM.Caja(cara, "Linea", new Vector3(0f, 0f, -0.005f), new Vector3(0.11f, 0.006f, 0.002f), MM.mats["NegroMate"], false);

        var titulo = MM.Texto3D(panel, "ERES BAYMAX", new Vector3(0.12f, 0.64f, -0.02f), Quaternion.identity, 0.85f, cian, 1.1f);
        titulo.alignment = TextAlignmentOptions.Center; titulo.fontStyle = FontStyles.Bold;

        var historia = MM.Texto3D(panel,
            "Has perdido tus <b>recuerdos científicos</b>. Para recuperarlos tendrás que investigar muestras como un microbiólogo: observar, consultar, decidir y comprobar.",
            new Vector3(0f, 0.41f, -0.02f), Quaternion.identity, 0.4f, Color.white, 1.3f);
        historia.alignment = TextAlignmentOptions.Center;

        var espejo = MM.Texto3D(panel, "<b>Mírate en el ESPEJO de tu derecha  >></b>", new Vector3(0f, 0.2f, -0.02f), Quaternion.identity, 0.45f, amarillo, 1.3f);
        espejo.alignment = TextAlignmentOptions.Center;

        var dondeT = MM.Texto3D(panel, "DÓNDE IR", new Vector3(0f, 0.07f, -0.02f), Quaternion.identity, 0.42f, cian, 1.3f);
        dondeT.alignment = TextAlignmentOptions.Center; dondeT.fontStyle = FontStyles.Bold;
        var donde = MM.Texto3D(panel,
            "Sigue las <b>balizas numeradas</b>:\n" +
            "<b>1</b> Puestos de microscopio  ·  <b>2</b> Cepario (bacterias)  ·  <b>3</b> Sala de alto riesgo (virus)\n" +
            "<b>4</b> <color=#FFD25A><b>CENTRO DE EXPERIMENTACIÓN</b></color>: en el <b>BANCO DE MUESTRAS</b> elige el CASO 01 y recupera tu primer recuerdo  ·  <b>5</b> Química",
            new Vector3(0f, -0.16f, -0.02f), Quaternion.identity, 0.33f, Color.white, 1.3f);
        donde.alignment = TextAlignmentOptions.Left;

        // Indicador de recuerdos recuperados (se actualiza en tiempo de ejecución).
        var indicador = panel.gameObject.AddComponent<IndicadorRecuerdos>();
        indicador.texto = MM.Texto3D(panel, "", new Vector3(-0.12f, -0.46f, -0.02f), Quaternion.identity, 0.4f, Color.white, 1.0f);
        indicador.texto.alignment = TextAlignmentOptions.Left; indicador.texto.fontStyle = FontStyles.Bold;
        indicador.ranuras = new Renderer[RecuerdosBaymax.Total];
        for (int i = 0; i < indicador.ranuras.Length; i++)
        {
            var r = MM.Caja(panel, "Recuerdo" + i, new Vector3(0.38f + i * 0.11f, -0.46f, -0.015f), new Vector3(0.08f, 0.08f, 0.01f), MM.mats["BlancoMate"], false);
            indicador.ranuras[i] = r.GetComponent<Renderer>();
        }
        indicador.texto.text = $"RECUERDOS CIENTÍFICOS: <color=#FFD25A>0/{RecuerdosBaymax.Total}</color>";

        var controles = MM.Texto3D(panel, "Joystick izq.: caminar  ·  joystick der.: girar  ·  gatillo o grip: agarrar (también con el rayo)",
            new Vector3(0f, -0.6f, -0.02f), Quaternion.identity, 0.28f, new Color(0.56f, 0.85f, 1f), 1.3f);
        controles.alignment = TextAlignmentOptions.Center;
        var aviso = MM.Texto3D(panel, "Complemento educativo: las muestras, intervenciones y resultados están simplificados; no son protocolos clínicos ni de laboratorio reales.",
            new Vector3(0f, -0.72f, -0.02f), Quaternion.identity, 0.24f, new Color(0.66f, 0.75f, 0.82f), 1.3f);
        aviso.alignment = TextAlignmentOptions.Center; aviso.fontStyle = FontStyles.Italic;

        GameObjectUtility.SetStaticEditorFlags(raiz.Find("Pie").gameObject, StaticEditorFlags.BatchingStatic);
    }

    // ------------------------------------------------------------------ balizas de las áreas (orientación, RF-01)
    // Siempre visibles: anillo y haz de luz en el suelo y un rótulo numerado que mira al jugador y crece
    // con la distancia (BalizaArea). Paneles con el estilo de Quest (esquinas redondeadas del Interaction
    // SDK) y tipografía Atkinson Hyperlegible. El color siempre va con número y texto (RNF-03).

    const string MatDir = "Assets/NanoLab/Materiales/Micro";
    const string FuenteDir = "Assets/NanoLab/Fuentes";
    // Caracteres que se rasterizan al crear la fuente (el resto se añade al vuelo).
    const string Caracteres = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                              "¡¿ÁÉÍÓÚÜÑáéíóúüñ·°×–—…“”‘’";
    // Opaco: con mezcla en espacio lineal, incluso un 3 % de transparencia deja ver el texto de detrás.
    static readonly Color Fondo = new Color(0.075f, 0.09f, 0.115f, 1f);
    static readonly Color TextoTenue = new Color(0.62f, 0.7f, 0.78f, 1f);
    static Material matPanel;
    static TMP_FontAsset fuente, fuenteNegrita;

    // Mismo orden que el "DÓNDE IR" del tótem.
    static readonly (string titulo, string sub, float x, float z, Color color)[] AreasLab =
    {
        ("PUESTOS DE MICROSCOPÍA", "Portaobjetos y microscopios", -1.85f, 9.9f, new Color(0.35f, 0.85f, 1f)),
        ("CEPARIO", "Muestras de bacterias", 5.2f, 10.3f, new Color(0.45f, 0.65f, 1f)),
        ("SALA DE ALTO RIESGO", "Muestras de virus · BSL-3", -9.2f, 14.7f, new Color(1f, 0.5f, 0.65f)),
        ("CENTRO DE EXPERIMENTACIÓN", "Caso 01", 5.9f, 8.1f, new Color(1f, 0.82f, 0.35f)),
        ("LABORATORIO DE QUÍMICA", "Portal", -5.9f, 1.9f, new Color(0.4f, 1f, 0.6f)),
    };

    static void Areas(Transform padre)
    {
        Recursos();
        var g = new GameObject("AreasLaboratorio").transform;
        g.SetParent(padre, false);
        for (int i = 0; i < AreasLab.Length; i++)
        {
            var a = AreasLab[i];
            var e = new GameObject("Area_" + (i + 1)).transform;
            e.SetParent(g, false);
            e.position = new Vector3(a.x, MM.suelo, a.z);
            var emis = Emisivo(a.color);
            MM.Cilindro(e, "Anillo", new Vector3(0f, 0.004f, 0f), new Vector3(0.9f, 0.002f, 0.9f), Quaternion.identity, emis);
            MM.Cilindro(e, "Haz", new Vector3(0f, 1.15f, 0f), new Vector3(0.025f, 1.15f, 0.025f), Quaternion.identity, emis);

            var rot = new GameObject("Rotulo").transform;
            rot.SetParent(e, false);
            rot.localPosition = new Vector3(0f, 2.55f, 0f);
            Panel(rot, "Fondo", 0f, 0f, 0f, 0.86f, 0.2f, 0.05f, Fondo, a.color, 0.004f, 0);
            Panel(rot, "Numero", -0.36f, 0f, -0.002f, 0.1f, 0.1f, 0.05f, a.color, Color.clear, 0f, 1);
            Texto(rot, (i + 1).ToString(), -0.36f, 0f, 0.1f, 0.5f, new Color(0.03f, 0.08f, 0.12f), true, TextAlignmentOptions.Center);
            Texto(rot, a.titulo, 0.06f, 0.035f, 0.68f, 0.34f, Color.white, true, TextAlignmentOptions.Left);
            Texto(rot, a.sub, 0.06f, -0.04f, 0.68f, 0.26f, TextoTenue, false, TextAlignmentOptions.Left);
            var baliza = e.gameObject.AddComponent<BalizaArea>();
            baliza.rotulo = rot;
            baliza.distanciaBase = 3f; baliza.escalaMax = 3.5f;
        }
    }

    static void Recursos()
    {
        var sh = Shader.Find("Interaction/RoundedBoxUnlit");
        if (sh == null) Debug.LogWarning("[Intro] No se encontró el shader Interaction/RoundedBoxUnlit (Meta Interaction SDK).");
        matPanel = MM.MatEn(MatDir, "PanelRedondeado", sh != null ? sh : Shader.Find("Unlit/Color"));
        matPanel.enableInstancing = true;
        EditorUtility.SetDirty(matPanel);
        fuente = Fuente("AtkinsonHyperlegible-Regular");
        fuenteNegrita = Fuente("AtkinsonHyperlegible-Bold");
        AssetDatabase.SaveAssets();
    }

    // Fuente TMP dinámica a partir del TTF (con los caracteres del español ya rasterizados).
    static TMP_FontAsset Fuente(string nombre)
    {
        string ruta = $"{FuenteDir}/{nombre} SDF.asset";
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (fa != null) return fa;
        var ttf = AssetDatabase.LoadAssetAtPath<Font>($"{FuenteDir}/{nombre}.ttf");
        if (ttf == null) { Debug.LogWarning($"[Intro] Falta {FuenteDir}/{nombre}.ttf: se usa la fuente por defecto de TMP."); return null; }
        fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fa.name = nombre + " SDF";
        fa.TryAddCharacters(Caracteres, out string faltan);
        if (!string.IsNullOrEmpty(faltan)) Debug.LogWarning($"[Intro] {nombre}: sin glifo para '{faltan}'.");
        AssetDatabase.CreateAsset(fa, ruta);
        foreach (var tex in fa.atlasTextures) { tex.name = nombre + " Atlas"; AssetDatabase.AddObjectToAsset(tex, fa); }
        fa.material.name = nombre + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        EditorUtility.SetDirty(fa);
        return fa;
    }

    static Material Emisivo(Color c)
    {
        string n = "Baliza_" + ColorUtility.ToHtmlStringRGB(c);
        var m = MM.MatEn(MatDir, n, Shader.Find("Unlit/Color"));
        m.color = c;
        EditorUtility.SetDirty(m);
        return m;
    }

    // Quad con PanelRedondeado. 'orden' fija qué capa se dibuja encima (todos son transparentes).
    static PanelRedondeado Panel(Transform padre, string nombre, float x, float y, float z, float ancho, float alto, float radio,
                                 Color color, Color borde, float grosorBorde, int orden)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(q.GetComponent<Collider>());
        q.name = nombre;
        q.transform.SetParent(padre, false);
        q.transform.localPosition = new Vector3(x, y, z);
        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = matPanel;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
        r.sortingOrder = orden;
        var p = q.AddComponent<PanelRedondeado>();
        p.ancho = ancho; p.alto = alto; p.radio = radio;
        p.color = color; p.colorBorde = borde; p.borde = grosorBorde;
        p.Aplicar();
        return p;
    }

    static TextMeshPro Texto(Transform padre, string s, float x, float y, float ancho, float tam, Color color, bool negrita, TextAlignmentOptions alin)
    {
        var t = MM.Texto3D(padre, s, new Vector3(x, y, -0.004f), Quaternion.identity, tam, color, ancho);
        var f = negrita ? fuenteNegrita : fuente;
        if (f != null) t.font = f;
        else if (negrita) t.fontStyle = FontStyles.Bold;
        t.alignment = alin;
        t.sortingOrder = 10;
        return t;
    }

    // La experiencia empieza en Microbiología; Química queda como sala secundaria y sin escenas de ejemplo.
    public static void OrdenarEscenas()
    {
        EditorBuildSettingsScene Escena(string ruta) => new EditorBuildSettingsScene(new GUID(AssetDatabase.AssetPathToGUID(ruta)), true);
        var lista = new System.Collections.Generic.List<EditorBuildSettingsScene> { Escena(Microbiologia) };
        if (System.IO.File.Exists(Quimica)) lista.Add(Escena(Quimica));
        // Se vacía antes para que Unity reescriba los GUID aunque las rutas no hayan cambiado.
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
        EditorBuildSettings.scenes = lista.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[Intro] Escenas de build: " + string.Join(", ", lista.Select(e => $"{e.path} ({e.guid})")));
    }
}
