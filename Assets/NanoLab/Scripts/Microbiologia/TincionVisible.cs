using TMPro;
using UnityEngine;
using K = KitVacuna;

// Tinción de Gram VISIBLE paso a paso (revisión: que se vea por qué una bacteria es Gram + o Gram −).
// Al tocar un gotero con el portaobjetos:
//  1) cae una gota del color del reactivo sobre el vidrio y salpica,
//  2) el frotis cambia de color poco a poco (no de golpe),
//  3) encima aparece una LUPA con tres células de la muestra: se ve su pared (gruesa o delgada) y cómo cada reactivo
//     las tiñe — el violeta las tiñe todas, el lugol lo fija, el alcohol lo saca solo de las de pared delgada y la
//     safranina tiñe de rosa las que quedaron sin color — con una frase de qué pasó en este paso.
// La lupa se apaga sola a los pocos segundos. Lo añade Portaobjetos al aplicar un reactivo.
public class TincionVisible : MonoBehaviour
{
    static readonly Color Violeta = new Color(0.45f, 0.15f, 0.75f);
    static readonly Color VioletaOscuro = new Color(0.28f, 0.08f, 0.5f);
    static readonly Color Rosa = new Color(0.95f, 0.4f, 0.6f);
    static readonly Color SinColor = new Color(0.92f, 0.9f, 0.86f);
    static readonly Color[] ColorGota = { Color.clear, Violeta, new Color(0.55f, 0.35f, 0.15f), new Color(0.85f, 0.92f, 1f), Rosa };
    static readonly string[] QueHace =
    {
        "",
        "<b>1 · Cristal violeta:</b> tiñe de violeta TODAS las células.",
        "<b>2 · Lugol:</b> fija el violeta dentro de la pared (forma un complejo grande).",
        "<b>3 · Alcohol-acetona:</b> saca el violeta de las paredes DELGADAS; las GRUESAS lo retienen.",
        "<b>4 · Safranina:</b> tiñe de rosa las células que perdieron el violeta.",
    };

    Portaobjetos porta;
    Transform lupa, gota, salpicadura;
    SpriteRenderer[] celulas, paredes;
    TextMeshPro texto;
    float tGota = -1f, tLupa = -1f, tColor = 1f;
    Color celulaDesde, celulaHasta;
    int paso;
    static Sprite disco, aro;

    public bool LupaVisible => lupa != null && lupa.gameObject.activeSelf;
    public string TextoLupa => texto != null ? texto.text : "";

    public static TincionVisible De(Portaobjetos p)
    {
        var v = p.GetComponent<TincionVisible>();
        if (v == null) { v = p.gameObject.AddComponent<TincionVisible>(); v.porta = p; }
        return v;
    }

    // Reactivo r (1-4) aplicado; correcto = en su orden.
    public void Aplicar(int r, bool correcto)
    {
        if (porta == null) porta = GetComponent<Portaobjetos>();
        if (lupa == null) Construir();
        paso = r;
        tGota = 0f;
        gota.gameObject.SetActive(true);
        gota.GetComponent<Renderer>().material.color = ColorGota[Mathf.Clamp(r, 1, 4)];
        var m = porta.muestra;
        bool bacteria = m != null && m.tipo == TipoMuestra.Bacteria;
        bool gramPos = m == null || m.tipo != TipoMuestra.Bacteria || m.gramPositiva;
        // Lupa: células con la pared de la muestra (gruesa si Gram +).
        float grosor = gramPos ? 1.32f : 1.12f;
        for (int i = 0; i < paredes.Length; i++) paredes[i].transform.localScale = Vector3.one * 0.05f * grosor;
        celulaDesde = celulas[0].color;
        celulaHasta = !correcto ? new Color(0.8f, 0.55f, 0.65f) : ColorCelula(r, gramPos);
        tColor = 0f;
        string resultado = !correcto ? "<color=#FF8A7A>Orden incorrecto: la tinción ya no sirve para decidir.</color>"
            : r == 3 ? (gramPos ? "<color=#C9A8FF>Tu muestra SIGUE VIOLETA: su pared es gruesa.</color>" : "<color=#FFFFFF>Tu muestra se DECOLORÓ: su pared es delgada.</color>")
            : r == 4 ? (gramPos ? "<color=#C9A8FF>Queda VIOLETA → GRAM POSITIVA.</color>" : "<color=#FF9EC4>Queda ROSA → GRAM NEGATIVA.</color>")
            : "";
        if (!bacteria && r >= 3 && correcto) resultado = "<color=#A9BED2>(La tinción de Gram clasifica BACTERIAS.)</color>";
        texto.text = $"{QueHace[Mathf.Clamp(r, 1, 4)]}\n{resultado}";
        tLupa = 0f;
        lupa.gameObject.SetActive(true);
    }

    static Color ColorCelula(int r, bool gramPos)
    {
        switch (r)
        {
            case 1: return Violeta;
            case 2: return VioletaOscuro;
            case 3: return gramPos ? VioletaOscuro : SinColor;
            default: return gramPos ? VioletaOscuro : Rosa;
        }
    }

    void Update()
    {
        if (lupa == null) return;
        var cab = ManosRig.Cabeza;
        // Gota: cae 9 cm en 0,25 s y salpica.
        if (tGota >= 0f)
        {
            tGota += Time.deltaTime;
            float k = Mathf.Clamp01(tGota / 0.25f);
            gota.position = transform.position + Vector3.up * Mathf.Lerp(0.09f, 0.005f, k * k);
            if (k >= 1f)
            {
                gota.gameObject.SetActive(false);
                float s = Mathf.Clamp01((tGota - 0.25f) / 0.35f);
                salpicadura.gameObject.SetActive(s < 1f);
                salpicadura.position = transform.position + Vector3.up * 0.004f;
                salpicadura.localScale = Vector3.one * Mathf.Lerp(0.01f, 0.06f, s);
                var sr = salpicadura.GetComponent<SpriteRenderer>();
                var c = ColorGota[Mathf.Clamp(paso, 1, 4)]; c.a = 0.8f * (1f - s); sr.color = c;
                if (s >= 1f) tGota = -1f;
            }
        }
        // Células de la lupa: cambian de color en ~1,2 s (empieza al caer la gota).
        if (tColor < 1f)
        {
            tColor += Time.deltaTime / 1.2f;
            var c = Color.Lerp(celulaDesde, celulaHasta, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tColor - 0.2f)));
            foreach (var cel in celulas) cel.color = c;
            var pared = Color.Lerp(c, Color.white, 0.25f);
            foreach (var p in paredes) p.color = pared;
        }
        // Lupa: encima del portaobjetos, mirando a la cabeza; se va a los 6 s.
        if (tLupa >= 0f)
        {
            tLupa += Time.deltaTime;
            lupa.position = transform.position + Vector3.up * 0.2f;
            if (cab != null)
            {
                var d = lupa.position - cab.position;
                if (d.sqrMagnitude > 1e-4f) lupa.rotation = Quaternion.LookRotation(d, Vector3.up);
            }
            float a = tLupa < 0.2f ? tLupa / 0.2f : tLupa > 5.5f ? Mathf.Clamp01((6f - tLupa) / 0.5f) : 1f;
            lupa.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, a);
            if (tLupa > 6f) { lupa.gameObject.SetActive(false); tLupa = -1f; }
        }
    }

    void Construir()
    {
        if (disco == null)
        {
            disco = Sprite.Create(Circulo(64, 1f, 0f), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
            aro = Sprite.Create(Circulo(64, 1f, 0.78f), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        }
        lupa = new GameObject("LupaTincion").transform;
        lupa.SetParent(null, false);
        K.Panel(lupa, "Fondo", new Vector3(0f, 0f, 0.003f), 0.34f, 0.2f, new Color(0.04f, 0.08f, 0.13f, 0.97f), new Color(0.37f, 0.83f, 1f), 0.002f, 0.015f);
        var campo = new GameObject("Campo").AddComponent<SpriteRenderer>();
        campo.transform.SetParent(lupa, false);
        campo.transform.localPosition = new Vector3(-0.1f, 0.0f, 0.001f);
        campo.transform.localScale = Vector3.one * 0.115f;
        campo.sprite = disco;
        campo.color = new Color(0.95f, 0.95f, 0.92f);
        campo.sortingOrder = 6;
        celulas = new SpriteRenderer[3];
        paredes = new SpriteRenderer[3];
        var pos = new[] { new Vector2(-0.115f, 0.02f), new Vector2(-0.085f, 0.025f), new Vector2(-0.1f, -0.012f) };
        for (int i = 0; i < 3; i++)
        {
            paredes[i] = Sprite2D(lupa, "Pared", pos[i], 0.05f * 1.3f, aro, 7);
            celulas[i] = Sprite2D(lupa, "Celula", pos[i], 0.05f, disco, 8);
            celulas[i].color = SinColor;
        }
        texto = K.Texto(lupa, "", new Vector3(0.07f, 0f, -0.002f), 0.07f, Color.white, 0.14f, 0.17f, TextAlignmentOptions.Left);
        K.AutoTam(texto, 0.04f);
        foreach (var c in lupa.GetComponentsInChildren<Collider>(true)) Destroy(c);
        lupa.gameObject.SetActive(false);

        gota = K.Esfera(null, "GotaReactivo", transform.position, 0.008f, new Material(K.Plano(Color.white))).transform;
        foreach (var c in gota.GetComponentsInChildren<Collider>(true)) Destroy(c);
        gota.gameObject.SetActive(false);
        var sp = new GameObject("Salpicadura").AddComponent<SpriteRenderer>();
        sp.sprite = aro;
        sp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        salpicadura = sp.transform;
        salpicadura.gameObject.SetActive(false);
    }

    static SpriteRenderer Sprite2D(Transform padre, string nombre, Vector2 p, float tam, Sprite s, int orden)
    {
        var sr = new GameObject(nombre).AddComponent<SpriteRenderer>();
        sr.transform.SetParent(padre, false);
        sr.transform.localPosition = new Vector3(p.x, p.y, -0.001f * orden);
        sr.transform.localScale = Vector3.one * tam;
        sr.sprite = s;
        sr.sortingOrder = orden;
        return sr;
    }

    // Disco (hueco = 0) o aro (hueco = radio interior relativo), con borde suave.
    static Texture2D Circulo(int n, float exterior, float hueco)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f).magnitude * 2f;
                float a = Mathf.Clamp01((exterior - r) * n * 0.5f);
                if (hueco > 0f) a *= Mathf.Clamp01((r - hueco) * n * 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    void OnDestroy()
    {
        if (lupa != null) Destroy(lupa.gameObject);
        if (gota != null) Destroy(gota.gameObject);
        if (salpicadura != null) Destroy(salpicadura.gameObject);
    }
}
