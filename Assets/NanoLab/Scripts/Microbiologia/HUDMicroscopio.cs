using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD "Análisis microscópico" que aparece al acercar la cara al ocular.
// Se construye por código, pegado a la cabeza, a 0,6 m delante de los ojos.
public class HUDMicroscopio : MonoBehaviour
{
    public static HUDMicroscopio Instancia { get; private set; }
    public static event System.Action<int> AumentoCambiado;

    // Contenido del HUD cuando la muestra pertenece a un reto del Centro de Experimentación:
    // identidad oculta, imagen según la tinción, cuaderno de evidencias y comparación ANTES | DESPUÉS.
    public struct DatosReto
    {
        public string subtitulo;
        public Texture vista, antes;
        public string antesTitulo;
        public string[] claves, valores;
        public string riesgo;
        public string riesgoClave;      // etiqueta de la fila (por defecto "Riesgo")
        public Color? colorRiesgo;      // color de la caja (por defecto rojo)
        public string cuadernoTitulo, cuaderno;
        public string obsTitulo, obs;
        public string guia;
        public string aviso;
        public Color colorAviso;
    }

    static readonly Color Cian = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color CianSuave = new Color(0.35f, 0.85f, 1f, 0.35f);
    static readonly Color Panel = new Color(0.02f, 0.09f, 0.16f, 0.85f);
    static readonly Color TextoClaro = new Color(0.85f, 0.93f, 1f, 1f);
    static readonly Color TextoGris = new Color(0.55f, 0.66f, 0.78f, 1f);

    CanvasGroup grupo;
    RawImage vista, micro;
    TMP_Text subtitulo, zoomTxt, escalaTxt, tabTitulo, tabTexto, obsTxt, creditoTxt, riesgoTxt, microTitulo, obsTit, avisoTxt, guiaTxt;
    GameObject pestanas;
    TMP_Text riesgoEtq;
    Image riesgoFondo;
    Image[] riesgoMarco;
    static readonly Color RiesgoFondoColor = new Color(0.45f, 0.05f, 0.08f, 0.85f);
    static readonly Color RiesgoMarcoColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    System.Func<int, DatosReto> proveedorReto;
    readonly TMP_Text[] claves = new TMP_Text[6], valores = new TMP_Text[6];
    readonly Image[] tabs = new Image[4];
    readonly TMP_Text[] tabsTxt = new TMP_Text[4];
    readonly Image[] leyendaPunto = new Image[5];
    readonly TMP_Text[] leyendaTxt = new TMP_Text[5];
    MuestraData actual;
    int aumento, pestana;
    float alphaObjetivo;
    Material holo;

    public static HUDMicroscopio Obtener(Transform ojo, Material holoAzul)
    {
        if (Instancia == null)
        {
            var go = new GameObject("HUDMicroscopio", typeof(RectTransform));
            Instancia = go.AddComponent<HUDMicroscopio>();
            Instancia.holo = holoAzul;
            Instancia.Construir();
        }
        var t = Instancia.transform;
        t.SetParent(ojo, false);
        t.localPosition = new Vector3(0f, 0f, 0.6f);
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one * 0.0008f;
        return Instancia;
    }

    public int Aumento => aumento;

    public void MostrarReto(MuestraData m, System.Func<int, DatosReto> proveedor)
    {
        Mostrar(m);
        proveedorReto = proveedor;
        Refrescar();
    }

    public void RefrescarReto() { if (proveedorReto != null) Refrescar(); }

    public void Mostrar(MuestraData m)
    {
        proveedorReto = null;
        actual = m;
        aumento = m.AumentoInicial;
        pestana = 0;
        gameObject.SetActive(true);
        var visor = VisorVirus.Obtener();
        if (m.tipo == TipoMuestra.Virus)
        {
            visor.Mostrar(m, holo);
            visor.Activo(true);
            vista.texture = visor.RT;
        }
        else
        {
            visor.Activo(false);
            vista.texture = m.micrografia;
        }
        Refrescar();
        alphaObjetivo = 1f;
    }

    public void MostrarInmediato(MuestraData m)
    {
        Mostrar(m);
        grupo.alpha = 1f;
    }

    public void Ocultar() => alphaObjetivo = 0f;

    public void CambiarAumento(int d)
    {
        if (actual == null) return;
        aumento = Mathf.Clamp(aumento + d, 0, actual.Aumentos.Length - 1);
        Refrescar();
        AumentoCambiado?.Invoke(aumento);
    }

    public void CambiarPestana(int d)
    {
        pestana = (pestana + d + 4) % 4;
        Refrescar();
    }

    void Update()
    {
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, Time.deltaTime * 4f);
        if (alphaObjetivo == 0f && grupo.alpha == 0f)
        {
            VisorVirus.Obtener().Activo(false);
            gameObject.SetActive(false);
        }
    }

    void Refrescar()
    {
        var m = actual;
        if (m == null) return;
        subtitulo.text = $"MUESTRA {m.codigo}  ·  {m.nombre.ToUpper()}";
        zoomTxt.text = m.Aumentos[aumento];
        escalaTxt.text = m.Escalas[aumento];
        float f = m.FactoresZoom[aumento];
        if (m.tipo == TipoMuestra.Virus)
        {
            VisorVirus.Obtener().Zoom(f);
            vista.uvRect = new Rect(0, 0, 1, 1);
        }
        else
        {
            // Recorte centrado: más aumento = trozo más pequeño de la micrografía.
            float s = 1f / f;
            vista.uvRect = new Rect(0.5f - s / 2f, 0.5f - s / 2f, s, s);
        }

        for (int i = 0; i < claves.Length; i++)
        {
            bool hay = m.caracteristicas != null && i < m.caracteristicas.Length;
            claves[i].text = hay ? m.caracteristicas[i].clave + ":" : "";
            valores[i].text = hay ? m.caracteristicas[i].valor : "";
        }
        riesgoTxt.text = m.riesgo;

        tabTitulo.text = MuestraData.NombresPestanas[pestana];
        tabTexto.text = m.Pestana(pestana);
        // COMPOSICIÓN de un virus: leyenda con el mismo color que cada parte del modelo 3D.
        bool leyenda = proveedorReto == null && pestana == 2 && m.tipo == TipoMuestra.Virus && m.componentes != null && m.componentes.Length > 0;
        tabTexto.gameObject.SetActive(!leyenda);
        var pal = ModeloVirus.Paleta(m);
        for (int i = 0; i < leyendaTxt.Length; i++)
        {
            bool hay = leyenda && i < m.componentes.Length;
            leyendaPunto[i].gameObject.SetActive(hay);
            leyendaTxt[i].gameObject.SetActive(hay);
            if (!hay) continue;
            leyendaPunto[i].color = pal[i];
            leyendaTxt[i].text = m.componentes[i];
        }
        for (int i = 0; i < 4; i++)
        {
            bool sel = i == pestana;
            tabs[i].color = sel ? new Color(0.2f, 0.6f, 0.85f, 0.55f) : new Color(0.03f, 0.1f, 0.18f, 0.85f);
            tabsTxt[i].color = sel ? Color.white : TextoGris;
        }

        obsTxt.text = m.observaciones;
        micro.texture = m.micrografia;
        micro.uvRect = RecorteAspecto(m.micrografia, 380f / 190f);
        microTitulo.text = m.tipo == TipoMuestra.Virus ? "MICROGRAFÍA ELECTRÓNICA REAL" : "MICROGRAFÍA ÓPTICA REAL";
        creditoTxt.text = m.creditoImagen;

        bool reto = proveedorReto != null;
        pestanas.SetActive(!reto);
        PonerRiesgo("Riesgo", null);
        micro.enabled = true;
        obsTit.text = "OBSERVACIONES";
        avisoTxt.text = "";
        guiaTxt.text = "";
        if (reto) RefrescarReto(proveedorReto(aumento));
    }

    void RefrescarReto(DatosReto d)
    {
        tabTexto.gameObject.SetActive(true);
        foreach (var i in leyendaPunto) i.gameObject.SetActive(false);
        foreach (var t in leyendaTxt) t.gameObject.SetActive(false);
        subtitulo.text = d.subtitulo;
        if (d.vista != null) vista.texture = d.vista;
        for (int i = 0; i < claves.Length; i++)
        {
            bool hay = d.claves != null && i < d.claves.Length;
            claves[i].text = hay ? d.claves[i] + ":" : "";
            valores[i].text = hay ? d.valores[i] : "";
        }
        riesgoTxt.text = d.riesgo;
        PonerRiesgo(d.riesgoClave ?? "Riesgo", d.colorRiesgo);
        tabTitulo.text = d.cuadernoTitulo;
        tabTexto.text = d.cuaderno;
        obsTit.text = d.obsTitulo;
        obsTxt.text = d.obs;
        avisoTxt.text = d.aviso;
        avisoTxt.color = d.colorAviso;
        creditoTxt.text = "";
        if (d.antes != null)
        {
            // Mismo recorte que la vista principal para comparar a igual aumento.
            micro.texture = d.antes;
            var r = vista.uvRect;
            float h = r.width * 170f / 380f;
            micro.uvRect = new Rect(r.x, r.y + (r.height - h) / 2f, r.width, h);
            microTitulo.text = d.antesTitulo;
        }
        else
        {
            micro.enabled = false;
            microTitulo.text = "GUÍA DE OBSERVACIÓN";
            guiaTxt.text = d.guia;
        }
    }

    void PonerRiesgo(string clave, Color? color)
    {
        riesgoEtq.text = clave + ":";
        var c = color ?? new Color(1f, 0.3f, 0.3f);
        riesgoFondo.color = color.HasValue ? new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 0.85f) : RiesgoFondoColor;
        foreach (var i in riesgoMarco) i.color = color.HasValue ? new Color(c.r, c.g, c.b, 0.9f) : RiesgoMarcoColor;
        riesgoTxt.color = color.HasValue ? Color.Lerp(c, Color.white, 0.4f) : new Color(1f, 0.6f, 0.6f);
    }

    static Rect RecorteAspecto(Texture t, float aspecto)
    {
        if (t == null) return new Rect(0, 0, 1, 1);
        float a = (float)t.width / t.height;
        if (a > aspecto) { float w = aspecto / a; return new Rect((1 - w) / 2, 0, w, 1); }
        float h = a / aspecto; return new Rect(0, (1 - h) / 2, 1, h);
    }

    // ---------------- Construcción ----------------

    void Construir()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(1400, 640);
        var raiz = transform;

        Img(raiz, "Fondo", 0, 0, 1400, 640, new Color(0.01f, 0.025f, 0.05f, 1f));   // opaco: no se transparenta el microscopio
        var vin = Rect(raiz, "Vineta", 0, 0, 1400, 640).gameObject.AddComponent<RawImage>();
        vin.texture = Vineta(); vin.raycastTarget = false;
        // Borde de la lente del ocular (anillo tenue sobre la viñeta).
        var lente = Rect(raiz, "Lente", 0, 0, 1400, 640).gameObject.AddComponent<RawImage>();
        lente.texture = Lente(); lente.color = new Color(0.45f, 0.85f, 1f, 0.35f); lente.raycastTarget = false;

        // Cabecera
        Txt(raiz, "Titulo", -420, 285, 560, 46, "ANÁLISIS MICROSCÓPICO", 34, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        subtitulo = Txt(raiz, "Subtitulo", -420, 250, 560, 30, "", 20, TextoClaro, TextAlignmentOptions.Left);
        Img(raiz, "LineaTitulo", -420, 230, 560, 2, CianSuave);

        // Zoom
        Img(raiz, "ZoomFondo", 330, 262, 190, 74, Panel);
        Marco(raiz, 330, 262, 190, 74, CianSuave);
        Esquinas(raiz, 330, 262, 190, 74);
        Txt(raiz, "ZoomEtq", 330, 285, 170, 20, "ZOOM", 15, Cian, TextAlignmentOptions.Left);
        zoomTxt = Txt(raiz, "Zoom", 330, 255, 170, 38, "", 30, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

        // Vista circular
        vista = Rect(raiz, "Vista", 0, 25, 440, 440).gameObject.AddComponent<RawImage>();
        vista.raycastTarget = false;
        // Recorte circular con una imagen opaca fuera del círculo (no con Mask: el stencil
        // de la máscara pierde el "dibujar siempre encima" y el microscopio tapaba la vista).
        Img(raiz, "RecorteCircular", 0, 25, 446, 446, new Color(0.01f, 0.025f, 0.05f, 1f), CirculoInverso());
        Img(raiz, "Anillo", 0, 25, 474, 474, new Color(0.35f, 0.85f, 1f, 0.7f), Anillo());
        Img(raiz, "Cruz1", 0, 25 + 250, 2, 34, Cian); Img(raiz, "Cruz2", 0, 25 - 250, 2, 34, Cian);
        Img(raiz, "Cruz3", 250, 25, 34, 2, Cian); Img(raiz, "Cruz4", -250, 25, 34, 2, Cian);
        for (int i = 0; i < 9; i++) Img(raiz, "Regla" + i, -290, -125 + i * 37.5f, i % 4 == 0 ? 22 : 12, 2, CianSuave);
        Img(raiz, "Escala", 0, -168, 150, 3, Color.white);
        Img(raiz, "EscalaI", -75, -168, 3, 14, Color.white); Img(raiz, "EscalaD", 75, -168, 3, 14, Color.white);
        escalaTxt = Txt(raiz, "EscalaTxt", 0, -148, 200, 26, "", 20, Color.white, TextAlignmentOptions.Center);

        // Panel izquierdo: características
        Txt(raiz, "CaractTit", -480, 188, 400, 30, "CARACTERÍSTICAS PRINCIPALES", 21, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        Img(raiz, "CaractFondo", -480, 70, 420, 210, Panel);
        Marco(raiz, -480, 70, 420, 210, CianSuave);
        Esquinas(raiz, -480, 70, 420, 210);
        for (int i = 0; i < 6; i++)
        {
            float y = 155 - i * 28;
            claves[i] = Txt(raiz, "Clave" + i, -585, y, 190, 28, "", 17, TextoGris, TextAlignmentOptions.Left);
            valores[i] = Txt(raiz, "Valor" + i, -395, y, 230, 28, "", 17, TextoClaro, TextAlignmentOptions.Left);
        }
        riesgoEtq = Txt(raiz, "RiesgoEtq", -585, -18, 190, 30, "Riesgo:", 17, TextoGris, TextAlignmentOptions.Left);
        riesgoFondo = Img(raiz, "RiesgoFondo", -395, -18, 220, 28, RiesgoFondoColor);
        riesgoMarco = Marco(raiz, -395, -18, 220, 28, RiesgoMarcoColor);
        riesgoTxt = Txt(raiz, "Riesgo", -395, -18, 210, 28, "", 16, new Color(1f, 0.6f, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold);

        // Panel izquierdo: pestaña actual
        Img(raiz, "TabFondo", -480, -140, 420, 170, Panel);
        Marco(raiz, -480, -140, 420, 170, CianSuave);
        Esquinas(raiz, -480, -140, 420, 170);
        tabTitulo = Txt(raiz, "TabTitulo", -480, -73, 390, 28, "", 20, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        tabTexto = Txt(raiz, "TabTexto", -480, -152, 390, 130, "", 16, TextoClaro, TextAlignmentOptions.TopLeft);
        for (int i = 0; i < leyendaTxt.Length; i++)
        {
            float y = -103 - i * 25;
            leyendaPunto[i] = Img(raiz, "LeyendaPunto" + i, -672, y, 14, 14, Color.white, Circulo());
            leyendaTxt[i] = Txt(raiz, "Leyenda" + i, -462, y, 390, 24, "", 17, TextoClaro, TextAlignmentOptions.Left);
            leyendaPunto[i].gameObject.SetActive(false);
            leyendaTxt[i].gameObject.SetActive(false);
        }

        // Panel derecho: micrografía real y observaciones
        microTitulo = Txt(raiz, "MicroTit", 480, 188, 400, 30, "", 19, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        Img(raiz, "MicroFondo", 480, 70, 420, 210, Panel);
        Marco(raiz, 480, 70, 420, 210, CianSuave);
        Esquinas(raiz, 480, 70, 420, 210);
        micro = Rect(raiz, "Micro", 480, 80, 380, 170).gameObject.AddComponent<RawImage>();
        micro.raycastTarget = false;
        creditoTxt = Txt(raiz, "Credito", 480, -22, 390, 18, "", 10, TextoGris, TextAlignmentOptions.Left);
        guiaTxt = Txt(raiz, "Guia", 480, 70, 390, 190, "", 17, TextoClaro, TextAlignmentOptions.TopLeft);
        Img(raiz, "ObsFondo", 480, -140, 420, 170, Panel);
        Marco(raiz, 480, -140, 420, 170, CianSuave);
        Esquinas(raiz, 480, -140, 420, 170);
        obsTit = Txt(raiz, "ObsTit", 480, -73, 390, 28, "OBSERVACIONES", 20, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        obsTxt = Txt(raiz, "Obs", 480, -152, 390, 130, "", 16, TextoClaro, TextAlignmentOptions.TopLeft);

        // Pestañas (debajo de los paneles, entre ellos)
        var pest = Rect(raiz, "Pestanas", 0, 0, 1400, 640);
        pestanas = pest.gameObject;
        for (int i = 0; i < 4; i++)
        {
            float x = -255f + i * 170f;
            tabs[i] = Img(pest, "Tab" + i, x, -262, 162, 40, Panel);
            Marco(pest, x, -262, 162, 40, CianSuave);
            tabsTxt[i] = Txt(pest, "TabTxt" + i, x, -262, 158, 38, MuestraData.NombresPestanas[i], 15, TextoGris, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        // Aviso del reto: ocupa el hueco de las pestañas, que en modo reto se ocultan.
        avisoTxt = Txt(raiz, "Aviso", 0, -258, 720, 56, "", 19, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        // Pie
        Txt(raiz, "Pie", -520, -305, 360, 20, "LAB. DE BIOCIENCIAS  |  NANOLAB", 13, TextoGris, TextAlignmentOptions.Left);
        Txt(raiz, "Ayuda", 0, -305, 760, 20, "A / X: más aumento     B / Y: menos aumento     Joystick izq./der.: pestañas     Aléjate del ocular para salir", 13, TextoGris, TextAlignmentOptions.Center);
        Img(raiz, "Led", 468, -305, 12, 12, new Color(0.25f, 1f, 0.5f), Circulo());
        Txt(raiz, "Captura", 575, -305, 200, 20, "IMAGEN CAPTURADA", 13, new Color(0.25f, 1f, 0.5f), TextAlignmentOptions.Left);

        DibujarSiempreEncima();
        gameObject.SetActive(false);
    }

    // El HUD está a 0,6 m de los ojos, pero el microscopio, la mesa o las manos quedan más cerca
    // al acercar la cara al ocular y lo tapaban. Se dibuja sin prueba de profundidad y al final
    // (cola Overlay), como los visores pegados a la cara.
    void DibujarSiempreEncima()
    {
        const int Always = (int)UnityEngine.Rendering.CompareFunction.Always;
        var ui = new Material(Canvas.GetDefaultCanvasMaterial()) { name = "HUD_SiempreEncima" };
        ui.SetInt("unity_GUIZTestMode", Always);
        ui.renderQueue = 4000;
        var textos = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            if (g is TMP_Text t)
            {
                var src = t.fontSharedMaterial;
                if (src == null) continue;
                if (!textos.TryGetValue(src, out var m))
                {
                    m = new Material(src) { name = src.name + "_SiempreEncima" };
                    m.SetInt("unity_GUIZTestMode", Always);
                    m.renderQueue = 4000;
                    textos[src] = m;
                }
                t.fontSharedMaterial = m;
            }
            else g.material = ui;
        }
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Image Img(Transform parent, string name, float x, float y, float w, float h, Color c, Sprite s = null)
    {
        var img = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
        img.color = c; img.sprite = s; img.raycastTarget = false;
        return img;
    }

    static Image[] Marco(Transform parent, float x, float y, float w, float h, Color c)
    {
        return new[]
        {
            Img(parent, "MarcoS", x, y + h / 2, w, 2, c),
            Img(parent, "MarcoI", x, y - h / 2, w, 2, c),
            Img(parent, "MarcoIz", x - w / 2, y, 2, h, c),
            Img(parent, "MarcoD", x + w / 2, y, 2, h, c),
        };
    }

    // Esquinas brillantes (escuadras) sobre el marco de un panel, estilo visor científico.
    static void Esquinas(Transform parent, float x, float y, float w, float h, float largo = 18f)
    {
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sy in new[] { -1f, 1f })
            {
                float cx = x + sx * w / 2f, cy = y + sy * h / 2f;
                Img(parent, "EsquinaH", cx - sx * largo / 2f, cy, largo, 3, Cian);
                Img(parent, "EsquinaV", cx, cy - sy * largo / 2f, 3, largo, Cian);
            }
    }

    static TMP_Text Txt(Transform parent, string name, float x, float y, float w, float h, string texto, float tam, Color c,
                        TextAlignmentOptions al, FontStyles estilo = FontStyles.Normal)
    {
        var t = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = tam; t.color = c; t.alignment = al; t.fontStyle = estilo;
        t.enableWordWrapping = true; t.raycastTarget = false;
        return t;
    }

    // ---------------- Texturas procedurales ----------------

    static Sprite circulo, anillo;
    static Texture2D vineta;

    public static Sprite Circulo()
    {
        if (circulo == null) circulo = SpriteRadial(256, (r) => Mathf.Clamp01((0.5f - r) * 256f / 2f));
        return circulo;
    }

    static Sprite circuloInverso;

    static Sprite CirculoInverso()
    {
        if (circuloInverso == null) circuloInverso = SpriteRadial(256, (r) => Mathf.Clamp01((r - 0.5f) * 256f / 2f));
        return circuloInverso;
    }

    static Sprite Anillo()
    {
        if (anillo == null) anillo = SpriteRadial(256, (r) => Mathf.Clamp01(1f - Mathf.Abs(r - 0.485f) * 256f / 3f));
        return anillo;
    }

    // Distancia elíptica normalizada del ocular (1 = borde de la lente); los paneles quedan dentro.
    static float DistOcular(int x, int y, int w, int h)
    {
        float dx = ((x + 0.5f) / w - 0.5f) * 2f, dy = ((y + 0.5f) / h - 0.5f) * 2f;
        return Mathf.Sqrt(dx * dx * 0.55f + dy * dy * 0.8f);
    }

    // Viñeta del ocular: oscurece suavemente hacia el borde y cierra en negro fuera de la lente.
    static Texture2D Vineta()
    {
        if (vineta != null) return vineta;
        const int w = 512, h = 256;
        vineta = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = DistOcular(x, y, w, h);
                float a = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, (d - 0.98f) / 0.18f) * 0.95f + Mathf.SmoothStep(0f, 1f, (d - 0.6f) / 0.4f) * 0.15f);
                px[y * w + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
        vineta.SetPixels32(px);
        vineta.Apply();
        return vineta;
    }

    static Texture2D lente;

    // Anillo fino en el borde de la lente (lo tiñe el RawImage).
    static Texture2D Lente()
    {
        if (lente != null) return lente;
        const int w = 1024, h = 512;
        lente = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = DistOcular(x, y, w, h) - 1.0f;
                float a = Mathf.Exp(-(d * d) / (0.006f * 0.006f)) + Mathf.Exp(-((d + 0.02f) * (d + 0.02f)) / (0.012f * 0.012f)) * 0.25f;
                px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
        lente.SetPixels32(px);
        lente.Apply();
        return lente;
    }

    static Sprite SpriteRadial(int n, System.Func<float, float> alpha)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                t.SetPixel(x, y, new Color(1, 1, 1, alpha(Mathf.Sqrt(dx * dx + dy * dy))));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
