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

// Introducción narrativa (RF-01): ventana pequeña paso a paso (VentanaIntro) que aparece delante de la
// cara al entrar. Explica quién es Baymax (el estudiante ES Baymax y ha perdido sus recuerdos
// científicos), el objetivo de NanoLab VR, el ciclo de investigación, los controles, el espejo del
// recibidor y dónde empezar (banco de muestras del Centro). Incluye el aviso de contenido simplificado
// (RNF-06). Al cerrarla queda una insignia con los recuerdos recuperados y un botón para volver a verla.
// Además coloca las balizas numeradas sobre cada área del laboratorio (diseño de Luis).
public static class MontarIntroduccion
{
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";
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
        Debug.Log($"[Intro] {s.name}: ventana de introducción de Baymax ({VentanaIntro.PasosPorDefecto().Length} pasos), insignia en {PosIntro} y {AreasLab.Length} balizas de áreas.");
    }

    // Ventana pequeña paso a paso (VentanaIntro) en lugar del tótem: en tiempo de ejecución se coloca
    // delante de la cara; aquí queda delante del punto de aparición para verla en el Editor. Al cerrarla
    // queda la insignia con los recuerdos recuperados (en el sitio del antiguo tótem) para volver a abrirla.
    static readonly Color FondoVentana = new Color(0.075f, 0.09f, 0.115f, 1f);
    static readonly Color Cian = new Color(0.37f, 0.83f, 1f);
    static readonly Color Amarillo = new Color(1f, 0.82f, 0.35f);

    static void Construir(Transform padre)
    {
        Recursos();
        var raiz = new GameObject("Introduccion").transform;
        raiz.SetParent(padre, false);
        var intro = raiz.gameObject.AddComponent<VentanaIntro>();
        var haciaIntro = PosIntro - Spawn; haciaIntro.y = 0f; haciaIntro.Normalize();

        // ---------------- ventana
        var v = new GameObject("Ventana").transform;
        v.SetParent(raiz, false);
        v.position = new Vector3(Spawn.x, MM.suelo + 1.45f, Spawn.z) + haciaIntro * intro.distancia;
        v.rotation = Quaternion.LookRotation(haciaIntro, Vector3.up);
        const float W = 0.64f, H = 0.48f;
        Panel(v, "Fondo", 0f, 0f, 0f, W, H, 0.03f, intro.colorCristal, Cian, 0.003f, 0);   // cristal transparente

        CaraBaymax(v, -0.275f, 0.195f);
        intro.titulo = Texto(v, "¡HOLA! ERES BAYMAX", -0.235f + 0.21f, 0.195f, 0.42f, 0.34f, Cian, true, TextAlignmentOptions.Left);
        intro.contador = Texto(v, "PASO 1 DE 7", 0.245f, 0.195f, 0.13f, 0.17f, TextoTenue, false, TextAlignmentOptions.Right);
        Panel(v, "Separador", 0f, 0.155f, -0.002f, W - 0.06f, 0.002f, 0f, new Color(0.25f, 0.33f, 0.42f, 1f), Color.clear, 0f, 1);

        intro.texto = Texto(v, "", 0f, 0.01f, W - 0.06f, 0.25f, Color.white, false, TextAlignmentOptions.TopLeft);
        intro.texto.rectTransform.sizeDelta = new Vector2(W - 0.06f, 0.25f);
        intro.texto.enableAutoSizing = true;
        intro.texto.fontSizeMin = 0.15f; intro.texto.fontSizeMax = 0.25f;
        intro.texto.overflowMode = TextOverflowModes.Truncate;

        intro.pasos = VentanaIntro.PasosPorDefecto();
        int n = intro.pasos.Length;
        intro.puntos = new PanelRedondeado[n];
        for (int i = 0; i < n; i++)
            intro.puntos[i] = Panel(v, "Punto" + i, (i - (n - 1) / 2f) * 0.026f, -0.145f, -0.002f, 0.014f, 0.014f, 0.007f, i == 0 ? Cian : new Color(0.2f, 0.26f, 0.33f), Color.clear, 0f, 1);

        intro.atras = MontarAjustes.Boton(v, "atras", "< ATRÁS", "volver al paso anterior", -0.215f, -0.195f, 0.15f);
        intro.saltar = MontarAjustes.Boton(v, "saltar", "SALTAR", "saltar la introducción", 0f, -0.195f, 0.13f);
        intro.siguiente = MontarAjustes.Boton(v, "siguiente", "SIGUIENTE >", "ver el siguiente paso", 0.2f, -0.195f, 0.18f);
        intro.etiquetaSiguiente = intro.siguiente.etiqueta;
        intro.titulo.text = intro.pasos[0].titulo;
        intro.texto.text = intro.pasos[0].texto;
        intro.contador.text = $"PASO 1 DE {n}";
        intro.atras.gameObject.SetActive(false);
        // Como el panel ☰: por encima de rótulos y balizas transparentes que queden detrás.
        foreach (var r in v.GetComponentsInChildren<Renderer>(true))
            r.sortingOrder += r.GetComponent<TMP_Text>() != null ? 40 : 30;
        intro.ventana = v.gameObject;

        // ---------------- insignia (ventana cerrada): recuerdos y botón para volver a verla
        var ins = new GameObject("Insignia").transform;
        ins.SetParent(raiz, false);
        ins.position = new Vector3(PosIntro.x, MM.suelo, PosIntro.z);
        ins.rotation = Quaternion.LookRotation(haciaIntro, Vector3.up);
        MM.Caja(ins, "Pie", new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.04f, 0.26f), MM.mats["Marco"], true);
        MM.Caja(ins, "Poste", new Vector3(0f, 0.6f, 0.02f), new Vector3(0.05f, 1.2f, 0.04f), MM.mats["Marco"], true);
        var pi = new GameObject("Panel").transform;
        pi.SetParent(ins, false);
        pi.localPosition = new Vector3(0f, 1.32f, 0f);
        Panel(pi, "Fondo", 0f, 0f, 0f, 0.5f, 0.24f, 0.025f, FondoVentana, Cian, 0.003f, 0);
        CaraBaymax(pi, -0.205f, 0.075f);
        Texto(pi, "BAYMAX", -0.17f + 0.1f, 0.075f, 0.2f, 0.26f, Cian, true, TextAlignmentOptions.Left);
        var indicador = pi.gameObject.AddComponent<IndicadorRecuerdos>();
        indicador.texto = Texto(pi, $"RECUERDOS CIENTÍFICOS: <color=#FFD25A>0/{RecuerdosBaymax.Total}</color>", 0f, 0.015f, 0.46f, 0.2f, Color.white, true, TextAlignmentOptions.Center);
        indicador.ranuras = new Renderer[RecuerdosBaymax.Total];
        for (int i = 0; i < indicador.ranuras.Length; i++)
        {
            var r = MM.Caja(pi, "Recuerdo" + i, new Vector3(0.12f + i * 0.045f, 0.075f, -0.004f), new Vector3(0.032f, 0.032f, 0.004f), MM.mats["BlancoMate"], false);
            indicador.ranuras[i] = r.GetComponent<Renderer>();
        }
        intro.reabrir = MontarAjustes.Boton(pi, "reabrir", "VER INTRODUCCIÓN", "volver a ver la introducción", 0f, -0.065f, 0.26f);
        intro.insignia = ins.gameObject;
        ins.gameObject.SetActive(false);   // al empezar se ve la ventana

        GameObjectUtility.SetStaticEditorFlags(ins.Find("Pie").gameObject, StaticEditorFlags.BatchingStatic);
        EditorUtility.SetDirty(intro);
    }

    // Cara de Baymax (dos ojos unidos por una línea) sobre un óvalo blanco.
    static void CaraBaymax(Transform p, float x, float y)
    {
        Panel(p, "Cara", x, y, -0.002f, 0.07f, 0.05f, 0.025f, Color.white, Color.clear, 0f, 1);
        foreach (var dx in new[] { -0.016f, 0.016f })
            Panel(p, "Ojo", x + dx, y, -0.003f, 0.012f, 0.012f, 0.006f, new Color(0.05f, 0.05f, 0.06f), Color.clear, 0f, 2);
        Panel(p, "Linea", x, y, -0.003f, 0.032f, 0.002f, 0f, new Color(0.05f, 0.05f, 0.06f), Color.clear, 0f, 2);
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

    internal static void Recursos()
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
    internal static PanelRedondeado Panel(Transform padre, string nombre, float x, float y, float z, float ancho, float alto, float radio,
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

    internal static TextMeshPro Texto(Transform padre, string s, float x, float y, float ancho, float tam, Color color, bool negrita, TextAlignmentOptions alin)
    {
        var t = MM.Texto3D(padre, s, new Vector3(x, y, -0.004f), Quaternion.identity, tam, color, ancho);
        var f = negrita ? fuenteNegrita : fuente;
        if (f != null) t.font = f;
        else if (negrita) t.fontStyle = FontStyles.Bold;
        t.alignment = alin;
        t.sortingOrder = 10;
        return t;
    }

    // La experiencia es solo la escena de Microbiología (la sala de Química y su portal se quitaron).
    public static void OrdenarEscenas()
    {
        EditorBuildSettingsScene Escena(string ruta) => new EditorBuildSettingsScene(new GUID(AssetDatabase.AssetPathToGUID(ruta)), true);
        var lista = new System.Collections.Generic.List<EditorBuildSettingsScene> { Escena(Microbiologia) };
        // Se vacía antes para que Unity reescriba los GUID aunque las rutas no hayan cambiado.
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[0];
        EditorBuildSettings.scenes = lista.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[Intro] Escenas de build: " + string.Join(", ", lista.Select(e => $"{e.path} ({e.guid})")));
    }
}
