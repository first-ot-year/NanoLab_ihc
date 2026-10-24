using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TextCore.LowLevel;
using MM = MontarMicrobiologia;
using ML = MontarLaboratorio;

// Introducción narrativa (RF-01), orientación por áreas y tarjeta de muestra seleccionada (E2).
// Paneles con el estilo de Quest: esquinas redondeadas (shader del Interaction SDK de Meta),
// fondo oscuro casi opaco, alto contraste y tipografía Atkinson Hyperlegible (OFL), diseñada
// para la máxima legibilidad. El color siempre va con texto (RNF-03).
public static class MontarIntroduccion
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string MatDir = "Assets/NanoLab/Materiales/Micro";
    const string FuenteDir = "Assets/NanoLab/Fuentes";
    const string BaymaxPrefab = "Assets/NanoLab/Modelos/Baymax/AvatarBaymax.prefab";

    // Caracteres que se rasterizan al crear la fuente (el resto se añade al vuelo).
    const string Caracteres = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                              "¡¿ÁÉÍÓÚÜÑáéíóúüñ·°×–—…“”‘’";

    // Opaco: con mezcla en espacio lineal, incluso un 3 % de transparencia deja ver el texto de detrás.
    static readonly Color Fondo = new Color(0.075f, 0.09f, 0.115f, 1f);
    static readonly Color Inserto = new Color(0.11f, 0.17f, 0.23f, 1f);
    static readonly Color Cian = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color TextoClaro = new Color(0.9f, 0.94f, 0.97f, 1f);
    static readonly Color TextoTenue = new Color(0.62f, 0.7f, 0.78f, 1f);

    static Material matPanel;
    // Orden de dibujo base de lo que se está montando: el diálogo y la tarjeta van por encima de las balizas.
    static int ordenBase;
    static TMP_FontAsset fuente, fuenteNegrita;

    // ================================================================== entrada

    [MenuItem("NanoLab/Actualizar introducción y jugador (sin hornear)", false, 4)]
    public static void Actualizar()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MM.CrearMateriales();
        MM.suelo = ML.AlturaSuelo(ML.BoundsEscena());
        foreach (var t in Object.FindObjectsOfType<Introduccion>(true).ToArray()) Object.DestroyImmediate(t.gameObject);
        var padre = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Bienvenida" && t.parent != null && t.parent.name == "Microbiologia");
        if (padre == null) padre = new GameObject("Bienvenida").transform;
        Crear(padre);
        ConfigurarMetaISDK.ActualizarPrefabPortaobjetos();
        ConfigurarMetaISDK.Configurar(scene);
        AgruparLab.Agrupar(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Intro] Introducción actualizada en Microbiologia.unity.");
        Capturas();
    }

    // Logs/Capturas/intro_*.png: pasos del diálogo, balizas y tarjeta, vistos desde la entrada.
    [MenuItem("NanoLab/Capturas de la introducción", false, 5)]
    public static void Capturas()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var intro = Object.FindObjectOfType<Introduccion>(true);
        if (intro == null) { Debug.LogWarning("[Intro] No hay Introduccion en la escena."); return; }
        string dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Logs", "Capturas");
        System.IO.Directory.CreateDirectory(dir);
        float y = ML.AlturaSuelo(ML.BoundsEscena());
        var ojo = new Vector3(-3.3f, y + 1.65f, 2.24f);
        var frente = Quaternion.Euler(0f, 24f, 0f) * Vector3.forward;
        void Delante(Transform t, float dist, float bajo)
        {
            t.gameObject.SetActive(true);
            t.position = ojo + frente * dist + Vector3.down * bajo;
            t.rotation = Quaternion.LookRotation(frente);
        }

        Delante(intro.dialogo, intro.distancia, intro.bajoOjos);
        if (intro.figura != null) intro.figura.localRotation = Quaternion.Euler(0f, 160f, 0f);
        foreach (int i in new[] { 0, 3, intro.pasos.Length - 1 })
        {
            intro.Previsualizar(i);
            foreach (var t in intro.dialogo.GetComponentsInChildren<TMP_Text>(true)) t.ForceMeshUpdate();
            ML.Capturar(dir, $"intro_paso{i + 1}", ojo, frente);
        }
        intro.dialogo.gameObject.SetActive(false);

        intro.areas.SetActive(true);
        foreach (var b in intro.areas.GetComponentsInChildren<BalizaArea>(true))
        {
            var d = b.rotulo.position - ojo; d.y = 0f;
            b.rotulo.rotation = Quaternion.LookRotation(d);
            b.rotulo.localScale = Vector3.one * Mathf.Clamp(d.magnitude / b.distanciaBase, 1f, b.escalaMax);
        }
        ML.Capturar(dir, "intro_balizas", ojo, Quaternion.Euler(0f, 10f, 0f) * Vector3.forward);
        foreach (var b in intro.areas.GetComponentsInChildren<BalizaArea>(true)) b.rotulo.localScale = Vector3.one;
        intro.areas.SetActive(false);

        var muestra = AssetDatabase.LoadAssetAtPath<MuestraData>("Assets/NanoLab/Muestras/Ecoli.asset");
        if (muestra != null)
        {
            Delante(intro.tarjeta, 1.1f, 0.25f);
            intro.RellenarTarjeta(muestra, null);
            foreach (var t in intro.tarjeta.GetComponentsInChildren<TMP_Text>(true)) t.ForceMeshUpdate();
            ML.Capturar(dir, "intro_tarjeta", ojo, frente + Vector3.down * 0.2f);
            intro.tarjeta.gameObject.SetActive(false);
        }
        // Las capturas no se guardan en la escena.
        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
        Debug.Log("[Intro] Capturas guardadas en Logs/Capturas (intro_*.png).");
    }

    public static Introduccion Crear(Transform padre)
    {
        Recursos();
        var intro = new GameObject("Introduccion").AddComponent<Introduccion>();
        intro.transform.SetParent(padre, false);
        Dialogo(intro);
        Areas(intro);
        Tarjeta(intro);
        return intro;
    }

    // ================================================================== diálogo

    static void Dialogo(Introduccion intro)
    {
        const float ancho = 1.2f, alto = 0.64f;
        ordenBase = 100;
        var d = Grupo(intro.transform, "Dialogo");
        intro.dialogo = d;
        Panel(d, "Fondo", 0f, 0f, 0f, ancho, alto, 0.05f, Fondo, new Color(Cian.r, Cian.g, Cian.b, 0.35f), 0.003f, 0);

        // Retrato: Baby Baymax (modelo estático) sobre un inserto.
        const float rx = -0.43f;
        Panel(d, "Retrato", rx, 0.0f, -0.002f, 0.28f, 0.54f, 0.035f, Inserto, new Color(Cian.r, Cian.g, Cian.b, 0.55f), 0.002f, 1);
        Panel(d, "Suelo", rx, -0.215f, -0.003f, 0.2f, 0.012f, 0.006f, new Color(Cian.r, Cian.g, Cian.b, 0.5f), Color.clear, 0f, 2);
        intro.figura = Figura(d, new Vector3(rx, -0.215f, -0.06f), 0.36f);
        var nombre = Texto(d, "BABY BAYMAX", rx, 0.235f, 0.2f, 0.22f, Cian, true, TextAlignmentOptions.Center);
        nombre.characterSpacing = 4f;

        // Columna de texto.
        const float cx = 0.16f, cw = 0.8f;
        Panel(d, "Hablante", cx - cw / 2f + 0.15f, 0.245f, -0.002f, 0.3f, 0.045f, 0.0225f, new Color(Cian.r, Cian.g, Cian.b, 0.14f), Cian, 0.002f, 1);
        var hablante = Texto(d, "SISTEMA DE BAYMAX", cx - cw / 2f + 0.15f, 0.245f, 0.28f, 0.21f, Cian, true, TextAlignmentOptions.Center);
        hablante.characterSpacing = 1.5f;
        hablante.enableWordWrapping = false;
        intro.titulo = Texto(d, "", cx, 0.175f, cw, 0.5f, Color.white, true, TextAlignmentOptions.Left);
        intro.texto = Texto(d, "", cx, -0.01f, cw, 0.32f, TextoClaro, false, TextAlignmentOptions.TopLeft);
        intro.texto.rectTransform.sizeDelta = new Vector2(cw, 0.29f);
        intro.texto.lineSpacing = 12f;
        Panel(d, "Separador", cx, -0.2f, -0.002f, cw, 0.0015f, 0f, new Color(1f, 1f, 1f, 0.12f), Color.clear, 0f, 1);

        // Pie: progreso y controles (icono + texto, no solo color).
        int n = Pasos().Length;
        intro.puntos = new PanelRedondeado[n];
        for (int i = 0; i < n; i++)
            intro.puntos[i] = Panel(d, "Punto" + i, cx - cw / 2f + 0.012f + i * 0.035f, -0.25f, -0.002f, 0.014f, 0.014f, 0.007f, new Color(1f, 1f, 1f, 0.18f), Color.clear, 0f, 2);
        intro.chipAtras = Pista(d, "Atras", 0.21f, -0.25f, "B / Y", "Atrás", false, out _).gameObject;
        Pista(d, "Siguiente", 0.44f, -0.25f, "A / X", "Continuar", true, out intro.accionSiguiente);

        intro.pasos = Pasos();
        d.gameObject.SetActive(false);
    }

    static Introduccion.Paso[] Pasos() => new[]
    {
        new Introduccion.Paso { titulo = "Reinicio completado", texto =
            "Hola. Mira tus manos y busca el <b>espejo</b> a tu derecha: eres <color=#8FD8FF><b>Baby Baymax</b></color>, " +
            "el asistente robótico de este laboratorio de microbiología." },
        new Introduccion.Paso { titulo = "Memoria dañada", texto =
            "Un fallo borró tu <color=#FFD25A><b>Archivo Científico</b></color>. Sabes que existen los microorganismos… " +
            "pero no recuerdas cómo son ni cómo se estudian." },
        new Introduccion.Paso { titulo = "Tu misión", texto =
            "Explora el laboratorio, observa muestras al microscopio y experimenta. " +
            "Cada caso que resuelvas te devolverá un <b>recuerdo científico</b>." },
        new Introduccion.Paso { titulo = "El laboratorio", mostrarAreas = true, texto =
            "Las <color=#8FD8FF><b>balizas</b></color> marcan las áreas: puestos de microscopía, cepario (bacterias), " +
            "sala de alto riesgo (virus), centro de experimentación y el portal a química. Gira para verlas." },
        new Introduccion.Paso { titulo = "Cómo moverte", texto =
            "<b>Joystick izquierdo:</b> caminar.   <b>Joystick derecho:</b> girar.\n<b>Gatillo o grip:</b> agarrar (también apuntando con el rayo).\n" +
            "Si se te cae un portaobjetos, vuelve solo a su bandeja. Equivocarte no tiene penalización." },
        new Introduccion.Paso { titulo = "Antes de empezar", texto =
            "NanoLab es un <b>complemento educativo</b>. Las muestras, intervenciones y resultados están simplificados " +
            "y no son protocolos clínicos ni de laboratorio reales." },
        new Introduccion.Paso { titulo = "Primera meta: elige una muestra", texto =
            "<color=#8FD8FF><b>1.</b></color> Coge un <b>portaobjetos</b> de la bandeja de un puesto (con la mano o apuntando con el rayo).\n" +
            "<color=#8FD8FF><b>2.</b></color> Apunta a un tubo de la gradilla <b>CULTIVOS</b> de tu puesto y pulsa el <b>gatillo</b>. " +
            "Los virus están en la <b>sala de alto riesgo</b>." },
    };

    // Indicación de control: botón (píldora) + acción.
    static Transform Pista(Transform padre, string nombre, float x, float y, string boton, string accion, bool principal, out TMP_Text etiqueta)
    {
        var g = Grupo(padre, nombre);
        g.localPosition = new Vector3(x, y, 0f);
        var colorBoton = principal ? Cian : new Color(1f, 1f, 1f, 0.16f);
        Panel(g, "Boton", -0.06f, 0f, -0.002f, 0.085f, 0.04f, 0.02f, colorBoton, Color.clear, 0f, 2);
        Texto(g, boton, -0.06f, 0f, 0.085f, 0.2f, principal ? new Color(0.03f, 0.08f, 0.12f) : Color.white, true, TextAlignmentOptions.Center);
        etiqueta = Texto(g, accion, 0.055f, 0f, 0.13f, 0.24f, principal ? Color.white : TextoTenue, principal, TextAlignmentOptions.Left);
        return g;
    }

    // Baby Baymax como imagen estática: el mismo modelo del avatar, sin scripts, a escala de figura.
    static Transform Figura(Transform padre, Vector3 pie, float altura)
    {
        var raiz = Grupo(padre, "Baymax");
        raiz.localPosition = pie;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaymaxPrefab);
        if (prefab == null) { Debug.LogWarning("[Intro] No está el prefab de Baymax: el retrato queda vacío."); return raiz; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;   // la cabeza del avatar va en una capa oculta
        foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        var b = ML.BoundsDe(go);
        go.transform.localScale = Vector3.one * (altura / Mathf.Max(0.01f, b.size.y));
        b = ML.BoundsDe(go);
        go.transform.position += raiz.position - new Vector3(b.center.x, b.min.y, b.center.z);
        // Pivote en los pies y centrado: así Introduccion lo puede girar sobre sí mismo.
        return raiz;
    }

    // ================================================================== orientación

    static readonly (string titulo, string sub, float x, float z, Color color)[] AreasLab =
    {
        ("PUESTOS DE MICROSCOPÍA", "Portaobjetos y microscopios", -1.85f, 9.9f, new Color(0.35f, 0.85f, 1f)),
        ("CEPARIO", "Muestras de bacterias", 5.2f, 10.3f, new Color(0.45f, 0.65f, 1f)),
        ("SALA DE ALTO RIESGO", "Muestras de virus · BSL-3", -9.2f, 14.7f, new Color(1f, 0.5f, 0.65f)),
        ("CENTRO DE EXPERIMENTACIÓN", "Caso 01", 5.9f, 8.1f, new Color(1f, 0.82f, 0.35f)),
        ("LABORATORIO DE QUÍMICA", "Portal", -5.9f, 1.9f, new Color(0.4f, 1f, 0.6f)),
    };

    static void Areas(Introduccion intro)
    {
        var g = Grupo(intro.transform, "Areas");
        intro.areas = g.gameObject;
        ordenBase = 0;
        for (int i = 0; i < AreasLab.Length; i++)
        {
            var a = AreasLab[i];
            var e = Grupo(g, "Area_" + (i + 1));
            e.position = new Vector3(a.x, MM.suelo, a.z);
            var emis = Emisivo(a.color);
            MM.Cilindro(e, "Anillo", new Vector3(0f, 0.004f, 0f), new Vector3(0.9f, 0.002f, 0.9f), Quaternion.identity, emis);
            MM.Cilindro(e, "Haz", new Vector3(0f, 1.15f, 0f), new Vector3(0.025f, 1.15f, 0.025f), Quaternion.identity, emis);

            var rot = Grupo(e, "Rotulo");
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
        g.gameObject.SetActive(false);
    }

    // ================================================================== tarjeta de muestra

    static void Tarjeta(Introduccion intro)
    {
        var t = Grupo(intro.transform, "TarjetaMuestra");
        intro.tarjeta = t;
        ordenBase = 200;
        Panel(t, "Fondo", 0f, 0f, 0f, 0.74f, 0.25f, 0.04f, Fondo, new Color(Cian.r, Cian.g, Cian.b, 0.35f), 0.003f, 0);
        intro.tarjetaFranja = Panel(t, "Franja", -0.345f, 0f, -0.002f, 0.014f, 0.19f, 0.007f, Cian, Color.clear, 0f, 1);
        Texto(t, "MUESTRA SELECCIONADA", -0.02f, 0.085f, 0.6f, 0.21f, Cian, true, TextAlignmentOptions.Left).characterSpacing = 3f;
        intro.tarjetaNombre = Texto(t, "", -0.02f, 0.035f, 0.6f, 0.42f, Color.white, true, TextAlignmentOptions.Left);
        intro.tarjetaDetalle = Texto(t, "", -0.02f, -0.06f, 0.6f, 0.25f, TextoClaro, false, TextAlignmentOptions.Left);
        intro.tarjetaDetalle.rectTransform.sizeDelta = new Vector2(0.6f, 0.1f);
        intro.tarjetaChip = Panel(t, "Tipo", 0.26f, 0.085f, -0.002f, 0.15f, 0.04f, 0.02f, new Color(1f, 1f, 1f, 0.06f), Cian, 0.002f, 1);
        intro.tarjetaTipo = Texto(t, "", 0.26f, 0.085f, 0.15f, 0.2f, Cian, true, TextAlignmentOptions.Center);
        t.gameObject.SetActive(false);
    }

    // ================================================================== piezas

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

    static Transform Grupo(Transform padre, string nombre)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        return t;
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
        r.sortingOrder = ordenBase + orden;
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
        t.sortingOrder = ordenBase + 10;
        return t;
    }
}
