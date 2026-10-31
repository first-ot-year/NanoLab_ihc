using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Campo vivo del microscopio: las bacterias de la muestra se mueven en la vista circular del HUD.
// La micrografía real queda debajo, atenuada (como el plano desenfocado de la preparación), y encima
// se simulan los microorganismos con un algoritmo de movimiento según el organismo:
//  - Bacilos con flagelos (E. coli, B. subtilis): "correr y girar" (run-and-tumble). Nadan en línea
//    recta con un leve serpenteo, se detienen, giran al azar y vuelven a nadar, como en la realidad.
//  - Cocos sin flagelos (S. aureus): racimos que tiemblan por movimiento browniano, giran despacio y
//    los arrastra la corriente del líquido.
//  - Todos: movimiento browniano y una corriente lenta del líquido (ruido de Perlin).
//  - Células muertas (tratamiento eficaz): solo movimiento browniano, sin nadar.
// Color, fondo y densidad salen de la propia imagen (sin teñir, Gram + violeta, Gram - rosa,
// crecimiento o lisis). El zoom y el desplazamiento de la platina (uvRect de la vista) se respetan:
// más aumento = organismos más grandes y menos; mover la platina desplaza el campo. Sin coste de CPU
// apreciable: imágenes de uGUI en un mismo lote, sin RenderTexture.
public class CampoMicroscopio : MonoBehaviour
{
    public enum Forma { Bacilo, Coco }

    [Tooltip("Tamaño de la vista (px del canvas del HUD).")]
    public float tamVista = 440f;
    [Tooltip("Opacidad del fondo simulado sobre la micrografía real (0 = solo la foto).")]
    [Range(0f, 1f)] public float opacidadFondo = 0.8f;
    public int maxImagenes = 160;

    class Organismo
    {
        public Vector2 pos;           // en px de la vista, centrada (con envoltura toroidal)
        public float angulo;          // grados
        public float giro;            // velocidad de giro (racimos)
        public bool corriendo;
        public float tEstado, fase;
        public float velocidad;       // multiplicador individual
        public Color color;
        public readonly List<Image> partes = new List<Image>();
        public readonly List<Vector2> offsets = new List<Vector2>();   // en unidades de tamaño (se escalan con el zoom)
        public readonly List<Vector2> tamanos = new List<Vector2>();
    }

    struct Perfil { public Color fondo, organismo; public float cobertura; }

    static readonly Dictionary<Texture, Perfil> perfiles = new Dictionary<Texture, Perfil>();
    static Sprite spriteBacilo, spriteCoco;

    RawImage vista;
    Image fondo;
    RectTransform capa;
    readonly List<Image> pool = new List<Image>();
    readonly List<Organismo> organismos = new List<Organismo>();
    Forma forma;
    bool movil, inmoviles, cadenas;
    Perfil perfil;
    MuestraData muestraActual;
    Texture imagenActual;
    float zoom = 1f;              // factor de zoom actual (1 / ancho del uvRect)
    Rect uvAnterior;
    int semilla;

    // ------------------------------------------------------------------ API

    public static CampoMicroscopio Crear(RectTransform rect, RawImage vista)
    {
        var c = rect.gameObject.AddComponent<CampoMicroscopio>();
        c.vista = vista;
        c.tamVista = rect.sizeDelta.x;
        c.fondo = new GameObject("FondoVivo", typeof(RectTransform)).AddComponent<Image>();
        Ajustar(c.fondo.rectTransform, rect, Vector2.zero, rect.sizeDelta);
        c.fondo.raycastTarget = false;
        c.capa = new GameObject("Organismos", typeof(RectTransform)).GetComponent<RectTransform>();
        Ajustar(c.capa, rect, Vector2.zero, rect.sizeDelta);
        c.gameObject.SetActive(false);
        return c;
    }

    // Muestra en movimiento la muestra 'm' con la imagen 'imagen' (la que se ve en la vista).
    public void Mostrar(MuestraData m, Texture imagen, bool celulasMuertas = false)
    {
        if (m == null || m.tipo != TipoMuestra.Bacteria || imagen == null) { Ocultar(); return; }
        // La misma vista (p. ej. al cambiar el aumento): la simulación sigue, no se reinicia.
        if (gameObject.activeSelf && m == muestraActual && imagen == imagenActual && celulasMuertas == inmoviles && organismos.Count > 0) return;
        muestraActual = m;
        imagenActual = imagen;
        gameObject.SetActive(true);
        Sprites();
        Clasificar(m);
        inmoviles = celulasMuertas;
        perfil = Analizar(imagen);
        var f = perfil.fondo; f.a = opacidadFondo;
        fondo.color = f;
        semilla = (m.name + imagen.name).GetHashCode();
        uvAnterior = vista != null ? vista.uvRect : new Rect(0, 0, 1, 1);
        zoom = 1f / Mathf.Max(0.05f, uvAnterior.width);
        Repoblar(true);
    }

    public void Ocultar()
    {
        muestraActual = null;
        imagenActual = null;
        organismos.Clear();
        foreach (var i in pool) i.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ organismos

    // Morfología y motilidad según la especie (sin tocar las fichas de muestras).
    void Clasificar(MuestraData m)
    {
        string n = ((m.nombreCientifico ?? "") + " " + (m.nombre ?? "")).ToLowerInvariant();
        forma = n.Contains("coccus") || n.Contains("coco") || n.Contains("aureus") ? Forma.Coco : Forma.Bacilo;
        movil = forma == Forma.Bacilo;                    // E. coli y B. subtilis tienen flagelos
        cadenas = n.Contains("bacillus") && !n.Contains("coccus");   // B. subtilis forma cadenas cortas
    }

    // Tamaño base (px) de un organismo con zoom 1 (100x).
    Vector2 TamBase => forma == Forma.Coco ? new Vector2(7f, 7f) : cadenas ? new Vector2(15f, 5.5f) : new Vector2(12f, 5f);

    int Objetivo()
    {
        float dens = Mathf.Clamp(perfil.cobertura / 0.12f, 0.25f, 1.6f);
        float porRacimo = forma == Forma.Coco ? 5f : cadenas ? 1.6f : 1f;
        float n = 70f * dens / Mathf.Pow(zoom, 1.3f) / Mathf.Sqrt(porRacimo);
        return Mathf.Clamp(Mathf.RoundToInt(n), 5, 60);
    }

    void Repoblar(bool todo)
    {
        if (todo)
        {
            organismos.Clear();
            foreach (var i in pool) i.gameObject.SetActive(false);
            Random.InitState(semilla);
        }
        int objetivo = Objetivo();
        while (organismos.Count > objetivo) Quitar(organismos[organismos.Count - 1]);
        while (organismos.Count < objetivo && Usadas() < maxImagenes - 8) organismos.Add(Nuevo());
    }

    int Usadas() { int n = 0; foreach (var o in organismos) n += o.partes.Count; return n; }

    void Quitar(Organismo o)
    {
        foreach (var p in o.partes) p.gameObject.SetActive(false);
        organismos.Remove(o);
    }

    Organismo Nuevo()
    {
        float r = tamVista / 2f;
        var o = new Organismo
        {
            pos = new Vector2(Random.Range(-r, r), Random.Range(-r, r)),
            angulo = Random.Range(0f, 360f),
            giro = Random.Range(-12f, 12f),
            corriendo = Random.value < 0.6f,
            tEstado = Random.Range(0.2f, 2f),
            fase = Random.Range(0f, 10f),
            velocidad = Random.Range(0.7f, 1.3f),
        };
        // Variación de color: unas células más claras y otras más oscuras (tinción desigual).
        float v = Random.Range(-0.12f, 0.08f);
        o.color = new Color(Mathf.Clamp01(perfil.organismo.r + v), Mathf.Clamp01(perfil.organismo.g + v), Mathf.Clamp01(perfil.organismo.b + v), Random.Range(0.75f, 0.95f));

        if (forma == Forma.Coco)
        {
            // Racimo "en uvas" de 1 a 8 cocos, compacto.
            int k = Random.value < 0.25f ? Random.Range(1, 3) : Random.Range(3, 9);
            var puntos = new List<Vector2> { Vector2.zero };
            while (puntos.Count < k)
            {
                var b = puntos[Random.Range(0, puntos.Count)];
                var c = b + Random.insideUnitCircle.normalized * 0.92f;
                bool choca = false;
                foreach (var q in puntos) if ((q - c).sqrMagnitude < 0.7f) { choca = true; break; }
                if (!choca) puntos.Add(c);
            }
            foreach (var p in puntos) Parte(o, p, Vector2.one * Random.Range(0.9f, 1.08f), spriteCoco);
        }
        else
        {
            int k = cadenas && Random.value < 0.45f ? Random.Range(2, 4) : 1;
            for (int i = 0; i < k; i++)
                Parte(o, new Vector2((i - (k - 1) / 2f) * 1.02f, 0f), new Vector2(Random.Range(0.85f, 1.15f), Random.Range(0.9f, 1.05f)), spriteBacilo);
        }
        return o;
    }

    void Parte(Organismo o, Vector2 offset, Vector2 escala, Sprite s)
    {
        var img = Imagen();
        img.sprite = s;
        img.color = o.color;
        o.partes.Add(img);
        o.offsets.Add(offset);
        o.tamanos.Add(escala);
    }

    Image Imagen()
    {
        foreach (var i in pool)
            if (!i.gameObject.activeSelf) { i.gameObject.SetActive(true); return i; }
        var img = new GameObject("Microbio", typeof(RectTransform)).AddComponent<Image>();
        img.raycastTarget = false;
        img.rectTransform.SetParent(capa, false);
        pool.Add(img);
        return img;
    }

    // ------------------------------------------------------------------ simulación

    void LateUpdate()
    {
        if (vista == null || organismos.Count == 0) return;
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        var uv = vista.uvRect;

        // Zoom: las posiciones se escalan respecto al centro y se ajusta cuántos organismos caben.
        float z = 1f / Mathf.Max(0.05f, uv.width);
        if (!Mathf.Approximately(z, zoom))
        {
            float k = z / zoom;
            foreach (var o in organismos) o.pos *= k;
            zoom = z;
            Repoblar(false);
        }
        // Platina: mover la muestra desplaza todo el campo (pan en uv -> px).
        var d = uv.center - uvAnterior.center;
        uvAnterior = uv;
        Vector2 arrastre = -d / uv.width * tamVista;

        float t = Time.time;
        float lado = TamBase.x * zoom;
        // Corriente lenta del líquido (Perlin), igual para todos.
        var corriente = new Vector2(Mathf.PerlinNoise(t * 0.05f, 3.1f) - 0.5f, Mathf.PerlinNoise(7.7f, t * 0.05f) - 0.5f) * lado * 0.9f;
        // Browniano: desplazamiento ~ raíz(dt); más visible en organismos pequeños (como en la realidad).
        float browniano = (forma == Forma.Coco ? 2.2f : 1.4f) * lado * Mathf.Sqrt(dt);
        float r = tamVista / 2f, envoltura = r + lado * 2f;

        foreach (var o in organismos)
        {
            var mov = arrastre + corriente * dt + Random.insideUnitCircle * browniano;
            if (movil && !inmoviles)
            {
                o.tEstado -= dt;
                if (o.corriendo)
                {
                    // Nado: ~3 longitudes de cuerpo por segundo, con serpenteo del flagelo.
                    o.angulo += Mathf.Sin(t * 9f + o.fase) * 40f * dt + (Mathf.PerlinNoise(o.fase, t * 0.6f) - 0.5f) * 90f * dt;
                    float a = o.angulo * Mathf.Deg2Rad;
                    mov += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (3f * lado * o.velocidad * dt);
                    if (o.tEstado <= 0f) { o.corriendo = false; o.tEstado = Random.Range(0.12f, 0.35f); o.giro = Random.Range(-1f, 1f) * 520f; }
                }
                else
                {
                    // Volteo: gira sobre sí misma y elige otra dirección al azar.
                    o.angulo += o.giro * dt;
                    if (o.tEstado <= 0f) { o.corriendo = true; o.tEstado = Random.Range(0.6f, 2.4f); }
                }
            }
            else
            {
                // Sin flagelos o muertas: giro lento y aleatorio.
                o.giro += Random.Range(-1f, 1f) * 30f * dt;
                o.giro = Mathf.Clamp(o.giro, -15f, 15f);
                o.angulo += o.giro * dt;
            }

            o.pos += mov;
            // Envoltura toroidal: lo que sale por un borde entra por el opuesto (campo infinito).
            if (o.pos.x > envoltura) o.pos.x -= envoltura * 2f; else if (o.pos.x < -envoltura) o.pos.x += envoltura * 2f;
            if (o.pos.y > envoltura) o.pos.y -= envoltura * 2f; else if (o.pos.y < -envoltura) o.pos.y += envoltura * 2f;
            Dibujar(o);
        }
    }

    void Dibujar(Organismo o)
    {
        var tb = TamBase * zoom;
        var rot = Quaternion.Euler(0f, 0f, o.angulo);
        // Las células que se acercan al borde de la vista se difuminan (fuera del círculo las tapa el recorte).
        float dist = o.pos.magnitude / (tamVista / 2f);
        float alfa = Mathf.Clamp01((1.15f - dist) / 0.15f);
        for (int i = 0; i < o.partes.Count; i++)
        {
            var img = o.partes[i];
            var off = (Vector2)(rot * (o.offsets[i] * tb.x));   // racimos y cadenas giran juntos
            var rt = img.rectTransform;
            rt.anchoredPosition = o.pos + off;
            rt.localRotation = rot;
            rt.sizeDelta = Vector2.Scale(tb, o.tamanos[i]);
            var c = o.color; c.a *= alfa;
            img.color = c;
        }
    }

    // ------------------------------------------------------------------ análisis de la imagen

    // Color del fondo (lo más claro del centro de la imagen), color medio de las células (lo que se
    // separa del fondo) y fracción de la imagen que ocupan. Se lee en la GPU: no hace falta que la
    // textura sea legible.
    static Perfil Analizar(Texture t)
    {
        if (perfiles.TryGetValue(t, out var p)) return p;
        Sprites();
        const int n = 64;
        var rt = RenderTexture.GetTemporary(n, n, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var previa = RenderTexture.active;
        Graphics.Blit(t, rt);
        RenderTexture.active = rt;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, n, n), 0, 0, false);
        tex.Apply(false);
        RenderTexture.active = previa;
        RenderTexture.ReleaseTemporary(rt);
        var px = tex.GetPixels();
        Object.Destroy(tex);

        // Solo el centro (algunas fotos son del ocular, con borde negro alrededor).
        var centro = new List<Color>();
        for (int y = n / 5; y < n - n / 5; y++)
            for (int x = n / 5; x < n - n / 5; x++)
                centro.Add(px[y * n + x]);
        centro.Sort((a, b) => Lum(b).CompareTo(Lum(a)));
        Color fondo = Promedio(centro, 0, Mathf.Max(1, centro.Count * 3 / 10));

        Color suma = Color.black; int k = 0;
        foreach (var c in centro)
        {
            float dc = Mathf.Abs(c.r - fondo.r) + Mathf.Abs(c.g - fondo.g) + Mathf.Abs(c.b - fondo.b);
            if (dc < 0.18f) continue;
            suma += c; k++;
        }
        Color org = k > 0 ? suma / k : Color.Lerp(fondo, Color.black, 0.35f);
        // Más contraste que el promedio (las células se ven nítidas en el plano de foco).
        org = Color.Lerp(fondo, org, 1.35f);
        org.a = 1f; fondo.a = 1f;
        p = new Perfil { fondo = fondo, organismo = org, cobertura = k / (float)Mathf.Max(1, centro.Count) };
        perfiles[t] = p;
        return p;
    }

    static float Lum(Color c) => c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;

    static Color Promedio(List<Color> l, int desde, int cuantos)
    {
        Color s = Color.black;
        for (int i = desde; i < desde + cuantos && i < l.Count; i++) s += l[i];
        return s / Mathf.Max(1, cuantos);
    }

    // ------------------------------------------------------------------ sprites procedurales

    // Bacilo (cápsula) y coco (círculo) con el borde algo más oscuro y el centro más claro, como una
    // célula en campo claro. Blancos: el color lo pone la Image.
    static void Sprites()
    {
        if (spriteBacilo == null) spriteBacilo = Sprite.Create(Celula(64, 24), new Rect(0, 0, 64, 24), new Vector2(0.5f, 0.5f), 100f);
        if (spriteCoco == null) spriteCoco = Sprite.Create(Celula(32, 32), new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
    }

    static Texture2D Celula(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Celula" };
        float rad = h / 2f - 1f;
        float x0 = rad + 1f, x1 = w - rad - 1f;
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var q = new Vector2(Mathf.Clamp(p.x, x0, x1), h / 2f);
                float d = (p - q).magnitude / rad;          // 0 en el eje, 1 en el borde
                float alfa = Mathf.Clamp01((1f - d) * rad * 0.8f);
                float tono = Mathf.Lerp(1f, 0.7f, Mathf.SmoothStep(0.45f, 1f, d));   // borde más oscuro
                px[y * w + x] = new Color(tono, tono, tono, alfa);
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return tex;
    }

    static void Ajustar(RectTransform rt, RectTransform padre, Vector2 pos, Vector2 tam)
    {
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }
}
