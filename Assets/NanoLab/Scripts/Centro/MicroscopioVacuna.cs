using TMPro;
using UnityEngine;
using UnityEngine.UI;
using A = AgarreRuntime;
using C = ContenidoVacuna;
using K = KitVacuna;

// Microscopio invertido con cámara del CASO 01 (paso 7 · TÍTULO). En un laboratorio de cultivo celular las placas
// se miran desde abajo con un microscopio invertido y su imagen sale en un monitor: así el estudiante no tiene que
// acercar la cara al ocular y puede comparar dos pocillos lado a lado.
//   ANTES   = pocillo CONTROL (células sin virus): capa continua de células sanas teñidas de violeta.
//   DESPUÉS = pocillo PLACEBO (virus sin anticuerpos): células redondas, sueltas y huecos (efecto citopático).
// Conecta la microscopía (RF-03 observación, RF-08 comparación antes/después) con la vacuna: el virus (~100 nm) no se
// ve con luz visible, lo que se ve es lo que les hace a las células. Las micrografías se generan por código
// (no hay imágenes con licencia que citar) y son ilustrativas (RNF-06).
public class MicroscopioVacuna : MonoBehaviour
{
    public ZonaEncaje platina;
    public RawImage antes, despues;
    public TextMeshProUGUI titulo, rotuloAntes, rotuloDespues, pieAntes, pieDespues, pie;
    public Image fondoAntes, fondoDespues;
    public Renderer luz;

    public bool Encendido { get; private set; }

    static readonly Color Apagado = new Color(0.05f, 0.07f, 0.1f);

    // ------------------------------------------------------------------ estados del monitor

    public void Apagar()
    {
        Encendido = false;
        antes.texture = despues.texture = null;
        antes.color = despues.color = Apagado;
        titulo.text = "MICROSCOPIO INVERTIDO · CÁMARA";
        rotuloAntes.text = rotuloDespues.text = "";
        pieAntes.text = pieDespues.text = "";
        pie.text = "<color=#FFD25A>Pon la PLACA sobre la platina (la bandeja del microscopio).</color>";
        if (luz != null) K.Pintar(luz, new Color(0.25f, 0.3f, 0.35f));
    }

    // CONTROL (antes) frente a PLACEBO (después).
    public void MostrarComparacion()
    {
        Encendido = true;
        Poner(antes, rotuloAntes, pieAntes, C.MicroAntes, true);
        Poner(despues, rotuloDespues, pieDespues, C.MicroDespues, false);
        titulo.text = "MICROSCOPIO INVERTIDO · 200x";
        pie.text = C.MicroPie;
        if (luz != null) K.Pintar(luz, new Color(1f, 0.95f, 0.8f));
    }

    // CONTROL frente al pocillo VACUNADO de una dilución (al elegir el título).
    public void MostrarColumna(int c)
    {
        if (c < 0 || c >= PlacaNeutralizacion.Columnas) return;
        Encendido = true;
        Poner(antes, rotuloAntes, pieAntes, C.MicroAntes, true);
        bool vive = PlacaNeutralizacion.Vive(0, c);
        Poner(despues, rotuloDespues, pieDespues, $"VACUNADO · {PlacaNeutralizacion.Diluciones[c]}", vive);
        pie.text = vive ? "A esta dilución aún hay anticuerpos suficientes: las células siguen sanas."
                        : "A esta dilución ya no quedan anticuerpos suficientes: el virus destruyó las células.";
    }

    static void Poner(RawImage img, TMP_Text rotulo, TMP_Text pie, string titulo, bool sanas)
    {
        img.texture = sanas ? MicrografiasCultivo.Sanas : MicrografiasCultivo.Destruidas;
        img.color = Color.white;
        rotulo.text = titulo;
        pie.text = sanas ? C.MicroSanas : C.MicroDestruidas;
    }

    // ------------------------------------------------------------------ montaje (lo llama MontajeVacuna)

    // 'pos' en el espacio de 'raiz' (el mostrador). El usuario mira hacia -z.
    public static MicroscopioVacuna Crear(Transform raiz, Vector3 pos)
    {
        var g = K.Grupo(raiz, "MicroscopioInvertido", pos);
        var m = g.gameObject.AddComponent<MicroscopioVacuna>();
        var blanco = K.Escena("Blanco", new Color(0.93f, 0.93f, 0.92f));
        var gris = K.Plano(new Color(0.32f, 0.36f, 0.42f));
        var oscuro = K.Plano(new Color(0.12f, 0.14f, 0.18f));
        var negro = K.Plano(new Color(0.05f, 0.05f, 0.06f));

        // Cuerpo: base, columna trasera y brazo con la lámpara arriba (la luz baja sobre la placa); los objetivos
        // quedan debajo de la platina y los oculares delante, inclinados hacia el usuario.
        K.Caja(g, "Base", new Vector3(0f, 0.02f, -0.02f), new Vector3(0.24f, 0.04f, 0.28f), blanco, true);
        K.Caja(g, "Columna", new Vector3(0f, 0.17f, -0.14f), new Vector3(0.07f, 0.3f, 0.06f), blanco, true);
        K.Caja(g, "Brazo", new Vector3(0f, 0.3f, -0.06f), new Vector3(0.06f, 0.035f, 0.17f), blanco, true);
        K.Pieza(g, "Lampara", PrimitiveType.Cylinder, new Vector3(0f, 0.27f, 0.01f), new Vector3(0.06f, 0.02f, 0.06f), gris);
        m.luz = K.Pieza(g, "Luz", PrimitiveType.Cylinder, new Vector3(0f, 0.248f, 0.01f), new Vector3(0.045f, 0.002f, 0.045f), K.Plano(Color.white));
        K.Pieza(g, "Revolver", PrimitiveType.Cylinder, new Vector3(0f, 0.07f, 0.01f), new Vector3(0.07f, 0.012f, 0.07f), gris);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            K.Pieza(g, "Objetivo", PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.02f, 0.088f, 0.01f + Mathf.Sin(a) * 0.02f), new Vector3(0.014f, 0.012f, 0.014f), negro);
        }
        // Oculares (decorativos: la imagen sale en el monitor).
        foreach (float x in new[] { -0.022f, 0.022f })
            K.Pieza(g, "Ocular", PrimitiveType.Cylinder, new Vector3(x, 0.18f, 0.155f), new Vector3(0.022f, 0.04f, 0.022f), oscuro, Quaternion.Euler(-35f, 0f, 0f));
        K.Caja(g, "PortaOculares", new Vector3(0f, 0.15f, 0.13f), new Vector3(0.07f, 0.04f, 0.04f), blanco);
        foreach (float x in new[] { -0.125f, 0.125f })
            K.Pieza(g, "Enfoque", PrimitiveType.Cylinder, new Vector3(x, 0.07f, -0.1f), new Vector3(0.035f, 0.008f, 0.035f), gris, Quaternion.Euler(0f, 0f, 90f));

        // Platina (bandeja donde se apoya la placa) con su silueta.
        var platina = K.Grupo(g, "Platina", new Vector3(0f, 0.11f, 0.01f));
        K.Caja(platina, "Bandeja", new Vector3(0f, -0.006f, 0f), new Vector3(0.33f, 0.012f, 0.2f), gris, true);
        K.Caja(platina, "Ventana", new Vector3(0f, 0.0005f, 0f), new Vector3(0.06f, 0.001f, 0.06f), K.Plano(new Color(0.6f, 0.8f, 1f)));
        m.platina = A.Zona(platina, "Hueco", "placaCultivo", Vector3.zero, Quaternion.identity,
            K.Pieza(null, "Silueta", PrimitiveType.Cube, new Vector3(0f, 0.0125f, 0f), new Vector3(0.3f, 0.025f, 0.17f), K.Vidrio(new Color(0.4f, 0.9f, 1f, 0.35f))));
        K.Texto(g, "MICROSCOPIO INVERTIDO", new Vector3(0f, 0.041f, 0.128f), 0.1f, K.Cian, 0.24f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));

        // Monitor de la cámara sobre un pie, hacia el borde del mostrador (así no tapa la pantalla del caso),
        // girado hacia donde está el usuario e inclinado hacia arriba.
        K.Caja(g, "PieMonitor", new Vector3(0.16f, 0.005f, -0.1f), new Vector3(0.12f, 0.01f, 0.1f), gris);
        K.Caja(g, "Soporte", new Vector3(0.16f, 0.13f, -0.11f), new Vector3(0.02f, 0.25f, 0.02f), gris);
        var mon = K.Grupo(g, "Monitor", new Vector3(0.16f, 0.36f, -0.1f), Quaternion.Euler(0f, 150f, 0f) * Quaternion.Euler(18f, 0f, 0f));
        K.Caja(mon, "Marco", new Vector3(0f, 0f, 0.012f), new Vector3(0.375f, 0.222f, 0.02f), negro);
        m.ConstruirPantalla(mon);
        m.Apagar();
        return m;
    }

    // Lienzo de 450 x 260 px a 0,8 mm/px (36 x 21 cm), delante del marco.
    void ConstruirPantalla(Transform mon)
    {
        var go = new GameObject("Pantalla", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(mon, false);
        rt.localPosition = new Vector3(0f, 0f, -0.0005f);
        rt.localScale = Vector3.one * 0.0008f;
        rt.sizeDelta = new Vector2(450, 260);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = AdaptadorEscena.OrdenTexto;
        Img(rt, "Fondo", 0, 0, 450, 260, new Color(0.02f, 0.05f, 0.1f, 1f));
        titulo = Txt(rt, "Titulo", 0, 115, 430, 26, 19, new Color(0.45f, 0.9f, 1f), FontStyles.Bold);
        fondoAntes = Img(rt, "MarcoAntes", -110, 4, 142, 142, new Color(0.3f, 0.75f, 0.45f));
        fondoDespues = Img(rt, "MarcoDespues", 110, 4, 142, 142, new Color(1f, 0.6f, 0.4f));
        antes = Raw(rt, "Antes", -110, 4, 136, 136);
        despues = Raw(rt, "Despues", 110, 4, 136, 136);
        rotuloAntes = Txt(rt, "RotuloAntes", -110, 85, 214, 22, 15, new Color(0.55f, 1f, 0.65f), FontStyles.Bold);
        rotuloDespues = Txt(rt, "RotuloDespues", 110, 85, 214, 22, 15, new Color(1f, 0.75f, 0.55f), FontStyles.Bold);
        pieAntes = Txt(rt, "PieAntes", -110, -88, 214, 32, 13, new Color(0.85f, 0.9f, 0.95f), FontStyles.Normal);
        pieDespues = Txt(rt, "PieDespues", 110, -88, 214, 32, 13, new Color(0.85f, 0.9f, 0.95f), FontStyles.Normal);
        pie = Txt(rt, "Pie", 0, -117, 440, 24, 14, Color.white, FontStyles.Normal);
    }

    static RectTransform Rect(Transform p, string n, float x, float y, float w, float h)
    {
        var go = new GameObject(n, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(p, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Image Img(Transform p, string n, float x, float y, float w, float h, Color c)
    {
        var i = Rect(p, n, x, y, w, h).gameObject.AddComponent<Image>();
        i.color = c;
        i.raycastTarget = false;
        return i;
    }

    static RawImage Raw(Transform p, string n, float x, float y, float w, float h)
    {
        var i = Rect(p, n, x, y, w, h).gameObject.AddComponent<RawImage>();
        i.raycastTarget = false;
        return i;
    }

    static TextMeshProUGUI Txt(Transform p, string n, float x, float y, float w, float h, float tam, Color c, FontStyles estilo)
    {
        var t = Rect(p, n, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        if (EstiloNanoLab.Fuente != null) t.font = EstiloNanoLab.Fuente;
        t.fontSize = tam;
        t.enableAutoSizing = true;
        t.fontSizeMax = tam;
        t.fontSizeMin = tam * 0.6f;
        t.color = c;
        t.fontStyle = estilo;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }
}

// Micrografías ilustrativas de un cultivo celular teñido con cristal violeta, generadas al vuelo (una vez).
// Células: diagrama de Voronoi (cada célula es la región más cercana a su núcleo), con membrana más oscura y núcleo.
public static class MicrografiasCultivo
{
    const int Lado = 256;
    static Texture2D sanas, destruidas;

    public static Texture2D Sanas { get { if (sanas == null) Completo(Generar(true, 7, t => sanas = t, int.MaxValue)); return sanas; } }
    public static Texture2D Destruidas { get { if (destruidas == null) Completo(Generar(false, 11, t => destruidas = t, int.MaxValue)); return destruidas; } }

    // Prepara las dos imágenes repartiendo el cálculo en varios fotogramas (sin tirones en el visor). RetoVacuna la
    // lanza en el paso 6, antes de que se necesiten; si aún no estuvieran listas, Sanas/Destruidas las terminan al momento.
    public static System.Collections.IEnumerator Preparar(int filasPorFotograma = 12)
    {
        if (sanas == null) yield return Generar(true, 7, t => { if (sanas == null) sanas = t; }, filasPorFotograma);
        if (destruidas == null) yield return Generar(false, 11, t => { if (destruidas == null) destruidas = t; }, filasPorFotograma);
    }

    static void Completo(System.Collections.IEnumerator e) { while (e.MoveNext()) { } }

    static System.Collections.IEnumerator Generar(bool sanasCelulas, int semilla, System.Action<Texture2D> listo, int filasPorFotograma)
    {
        var rnd = new System.Random(semilla);
        const int n = 64;
        var px = new float[n]; var py = new float[n]; var vivas = new bool[n]; var radio = new float[n];
        for (int i = 0; i < n; i++)
        {
            px[i] = (float)rnd.NextDouble() * Lado;
            py[i] = (float)rnd.NextDouble() * Lado;
            vivas[i] = sanasCelulas || rnd.NextDouble() < 0.2;     // en el placebo sobreviven pocas
            radio[i] = 7f + (float)rnd.NextDouble() * 4f;          // células muertas: redondas y pequeñas
        }
        var fondo = new Color(0.93f, 0.92f, 0.96f);                // vidrio sin células (casi blanco)
        var citoplasma = new Color(0.62f, 0.45f, 0.82f);
        var membrana = new Color(0.42f, 0.26f, 0.62f);
        var nucleo = new Color(0.3f, 0.14f, 0.5f);
        var muerta = new Color(0.55f, 0.42f, 0.66f);
        var c = new Color[Lado * Lado];
        float centro = Lado / 2f, rCampo = Lado / 2f - 2f;
        for (int y = 0; y < Lado; y++)
            for (int x = 0; x < Lado; x++)
            {
                // Las dos semillas más cercanas: la región (célula) y la distancia al borde con la vecina.
                float d1 = float.MaxValue, d2 = float.MaxValue; int k1 = 0;
                for (int i = 0; i < n; i++)
                {
                    float dx = x - px[i], dy = y - py[i], d = dx * dx + dy * dy;
                    if (d < d1) { d2 = d1; d1 = d; k1 = i; } else if (d < d2) d2 = d;
                }
                float a = Mathf.Sqrt(d1), b = Mathf.Sqrt(d2), borde = (b - a) * 0.5f;
                Color col;
                if (vivas[k1] && sanasCelulas)
                {
                    col = borde < 1.2f ? membrana : Color.Lerp(citoplasma, citoplasma * 0.92f, a / 20f);
                    if (a < 4.5f) col = nucleo;
                }
                else if (vivas[k1])
                {
                    // Supervivientes del placebo: algo encogidas, separadas de las vecinas.
                    col = borde < 3.5f ? fondo : borde < 4.6f ? membrana : citoplasma;
                    if (a < 4f) col = nucleo;
                }
                else
                {
                    // Muertas: bolitas redondas sueltas sobre el vidrio (o nada).
                    col = a < radio[k1] * 0.55f ? (a > radio[k1] * 0.42f ? membrana : muerta) : fondo;
                }
                // Grano suave y viñeta del campo circular del microscopio.
                float ruido = ((x * 73856093 ^ y * 19349663 ^ semilla) & 255) / 255f;
                col = Color.Lerp(col, col * 0.9f, ruido * 0.35f);
                float r = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                if (r > rCampo) col = new Color(0.01f, 0.02f, 0.04f);
                else if (r > rCampo - 10f) col = Color.Lerp(col, new Color(0.01f, 0.02f, 0.04f), (r - (rCampo - 10f)) / 10f);
                col.a = 1f;
                c[y * Lado + x] = col;
                if (x == Lado - 1 && (y + 1) % filasPorFotograma == 0) yield return null;
            }
        var t = new Texture2D(Lado, Lado, TextureFormat.RGB24, false) { name = sanasCelulas ? "Cultivo_Sanas" : "Cultivo_Destruidas", wrapMode = TextureWrapMode.Clamp };
        t.SetPixels(c);
        t.Apply(false, true);
        listo(t);
    }
}
