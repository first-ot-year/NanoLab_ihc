using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD "Análisis microscópico" que aparece al acercar la cara al ocular.
// Se construye por código, pegado a la cabeza, a 0,6 m delante de los ojos.
public class HUDMicroscopio : MonoBehaviour
{
    public static HUDMicroscopio Instancia { get; private set; }

    static readonly Color Cian = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color CianSuave = new Color(0.35f, 0.85f, 1f, 0.35f);
    static readonly Color Panel = new Color(0.02f, 0.09f, 0.16f, 0.85f);
    static readonly Color TextoClaro = new Color(0.85f, 0.93f, 1f, 1f);
    static readonly Color TextoGris = new Color(0.55f, 0.66f, 0.78f, 1f);

    CanvasGroup grupo;
    RawImage vista, micro;
    TMP_Text subtitulo, zoomTxt, escalaTxt, tabTitulo, tabTexto, obsTxt, creditoTxt, riesgoTxt, microTitulo;
    readonly TMP_Text[] claves = new TMP_Text[6], valores = new TMP_Text[6];
    readonly Image[] tabs = new Image[4];
    readonly TMP_Text[] tabsTxt = new TMP_Text[4];
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

    public void Mostrar(MuestraData m)
    {
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

        Img(raiz, "Fondo", 0, 0, 1400, 640, new Color(0.01f, 0.025f, 0.05f, 0.97f));
        var vin = Rect(raiz, "Vineta", 0, 0, 1400, 640).gameObject.AddComponent<RawImage>();
        vin.texture = Vineta(); vin.raycastTarget = false;

        // Cabecera
        Txt(raiz, "Titulo", -420, 285, 560, 46, "ANÁLISIS MICROSCÓPICO", 34, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        subtitulo = Txt(raiz, "Subtitulo", -420, 250, 560, 30, "", 20, TextoClaro, TextAlignmentOptions.Left);
        Img(raiz, "LineaTitulo", -420, 230, 560, 2, CianSuave);

        // Zoom
        Img(raiz, "ZoomFondo", 330, 262, 190, 74, Panel);
        Marco(raiz, 330, 262, 190, 74, CianSuave);
        Txt(raiz, "ZoomEtq", 330, 285, 170, 20, "ZOOM", 15, Cian, TextAlignmentOptions.Left);
        zoomTxt = Txt(raiz, "Zoom", 330, 255, 170, 38, "", 30, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);

        // Vista circular
        var mascara = Img(raiz, "Mascara", 0, 25, 440, 440, Color.white, Circulo());
        var mask = mascara.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
        vista = Rect(mascara.transform, "Vista", 0, 0, 440, 440).gameObject.AddComponent<RawImage>();
        vista.raycastTarget = false;
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
        for (int i = 0; i < 6; i++)
        {
            float y = 155 - i * 28;
            claves[i] = Txt(raiz, "Clave" + i, -585, y, 190, 28, "", 17, TextoGris, TextAlignmentOptions.Left);
            valores[i] = Txt(raiz, "Valor" + i, -395, y, 230, 28, "", 17, TextoClaro, TextAlignmentOptions.Left);
        }
        Txt(raiz, "RiesgoEtq", -585, -18, 190, 30, "Riesgo:", 17, TextoGris, TextAlignmentOptions.Left);
        Img(raiz, "RiesgoFondo", -395, -18, 220, 28, new Color(0.45f, 0.05f, 0.08f, 0.85f));
        Marco(raiz, -395, -18, 220, 28, new Color(1f, 0.3f, 0.3f, 0.9f));
        riesgoTxt = Txt(raiz, "Riesgo", -395, -18, 210, 28, "", 16, new Color(1f, 0.6f, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold);

        // Panel izquierdo: pestaña actual
        Img(raiz, "TabFondo", -480, -140, 420, 170, Panel);
        Marco(raiz, -480, -140, 420, 170, CianSuave);
        tabTitulo = Txt(raiz, "TabTitulo", -480, -73, 390, 28, "", 20, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        tabTexto = Txt(raiz, "TabTexto", -480, -152, 390, 130, "", 16, TextoClaro, TextAlignmentOptions.TopLeft);

        // Panel derecho: micrografía real y observaciones
        microTitulo = Txt(raiz, "MicroTit", 480, 188, 400, 30, "", 19, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        Img(raiz, "MicroFondo", 480, 70, 420, 210, Panel);
        Marco(raiz, 480, 70, 420, 210, CianSuave);
        micro = Rect(raiz, "Micro", 480, 80, 380, 170).gameObject.AddComponent<RawImage>();
        micro.raycastTarget = false;
        creditoTxt = Txt(raiz, "Credito", 480, -22, 390, 18, "", 10, TextoGris, TextAlignmentOptions.Left);
        Img(raiz, "ObsFondo", 480, -140, 420, 170, Panel);
        Marco(raiz, 480, -140, 420, 170, CianSuave);
        Txt(raiz, "ObsTit", 480, -73, 390, 28, "OBSERVACIONES", 20, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        obsTxt = Txt(raiz, "Obs", 480, -152, 390, 130, "", 16, TextoClaro, TextAlignmentOptions.TopLeft);

        // Pestañas (debajo de los paneles, entre ellos)
        for (int i = 0; i < 4; i++)
        {
            float x = -255f + i * 170f;
            tabs[i] = Img(raiz, "Tab" + i, x, -262, 162, 40, Panel);
            Marco(raiz, x, -262, 162, 40, CianSuave);
            tabsTxt[i] = Txt(raiz, "TabTxt" + i, x, -262, 158, 38, MuestraData.NombresPestanas[i], 15, TextoGris, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        // Pie
        Txt(raiz, "Pie", -520, -305, 360, 20, "LAB. DE BIOCIENCIAS  |  NANOLAB", 13, TextoGris, TextAlignmentOptions.Left);
        Txt(raiz, "Ayuda", 0, -305, 760, 20, "A / X: más aumento     B / Y: menos aumento     Joystick izq./der.: pestañas     Aléjate del ocular para salir", 13, TextoGris, TextAlignmentOptions.Center);
        Img(raiz, "Led", 468, -305, 12, 12, new Color(0.25f, 1f, 0.5f), Circulo());
        Txt(raiz, "Captura", 575, -305, 200, 20, "IMAGEN CAPTURADA", 13, new Color(0.25f, 1f, 0.5f), TextAlignmentOptions.Left);

        gameObject.SetActive(false);
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

    static void Marco(Transform parent, float x, float y, float w, float h, Color c)
    {
        Img(parent, "MarcoS", x, y + h / 2, w, 2, c);
        Img(parent, "MarcoI", x, y - h / 2, w, 2, c);
        Img(parent, "MarcoIz", x - w / 2, y, 2, h, c);
        Img(parent, "MarcoD", x + w / 2, y, 2, h, c);
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

    static Sprite Anillo()
    {
        if (anillo == null) anillo = SpriteRadial(256, (r) => Mathf.Clamp01(1f - Mathf.Abs(r - 0.485f) * 256f / 3f));
        return anillo;
    }

    static Texture2D Vineta()
    {
        if (vineta != null) return vineta;
        vineta = new Texture2D(256, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 256; x++)
            {
                float dx = (x / 255f - 0.5f) * 2f, dy = (y / 127f - 0.5f) * 2f;
                float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                vineta.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0f, 0.85f, d - 0.55f)));
            }
        vineta.Apply();
        return vineta;
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
