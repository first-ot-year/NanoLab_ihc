using TMPro;
using UnityEngine;

// Guía del reto en el Centro de Experimentación (sustituye al panel que iba pegado a la mano y a la
// columna de luz con el rombo):
//  - TARJETA DE OBJETIVO: tarjeta blanca en la parte baja de la vista con el caso, el paso (n de 9 y
//    puntos de progreso), qué hay que hacer y a qué estación ir. Aparece sola al entrar al centro, en
//    cada paso nuevo y al llegar a la estación; se esconde a los pocos segundos para no estorbar. Con el
//    botón Y (mando izquierdo) se muestra u oculta cuando se quiera.
//  - RUTA: flechas en el suelo que fluyen desde el jugador hasta la estación del paso actual y un anillo
//    con su número y nombre. Al llegar, la ruta se apaga y el anillo late.
// La crea RetoManager; todo se construye por código.
public class GuiaReto : MonoBehaviour
{
    const int MaxPuntos = 9;   // misión 1: 6 pasos · misión 2: 5 · misión 3 (vacuna): 9

    const float W = 0.42f, H = 0.24f;
    const float SegundosVisible = 8f, DistanciaLlegada = 1.6f, Paso = 0.45f;
    const int MaxFlechas = 40;
    static readonly Color Fondo = new Color(0.97f, 0.985f, 1f, 0.95f);
    static readonly Color Tinta = new Color(0.06f, 0.13f, 0.2f);
    static readonly Color Acento = new Color(0.12f, 0.55f, 0.82f);
    static readonly Color ColorRuta = new Color(0.3f, 0.85f, 1f);
    static readonly Color ColorMeta = new Color(1f, 0.8f, 0.3f);

    // Tarjeta
    GameObject tarjeta;
    TextMeshPro caso, paso, objetivo, destinoTxt;
    PanelRedondeado[] puntos;
    float hastaOcultar, escalaTarjeta;
    bool fijada;          // mostrada con Y: no se esconde sola
    bool colocada;

    // Ruta
    Transform destino;
    SpriteRenderer[] flechas;
    SpriteRenderer anillo;
    TextMeshPro etiquetaMeta;
    GameObject meta;
    bool dentro, llegado;
    int etapa = -1;
    string textoObjetivo, nombreEstacion;
    int numeroEstacion;
    float radioLlegada = DistanciaLlegada;   // a qué distancia se da por llegado (más grande para el laboratorio de vacunas)
    // Misión 3: con las misiones 1 y 2 terminadas, la tarjeta enseña el paso de la vacuna (la tecla Y sirve en las tres).
    bool espejoVacuna;
    float tVacuna;
    string pasoVacuna;

    public static GuiaReto Crear()
    {
        var g = new GameObject("GuiaReto").AddComponent<GuiaReto>();
        g.Montar();
        return g;
    }

    // ------------------------------------------------------------------ API para RetoManager

    // 'clave' identifica el paso (cambia con la misión y con el subpaso); 'pasoN' de 'total' es lo que se enseña.
    public void Etapa(int clave, string titulo, string obj, EstacionGuia estacion, bool conAviso, int pasoN, int total, string nombrePaso)
    {
        bool nueva = clave != etapa;
        etapa = clave;
        var antes = destino;
        textoObjetivo = obj;
        destino = estacion != null ? (estacion.puntoBaliza != null ? estacion.puntoBaliza : estacion.transform) : null;
        numeroEstacion = estacion != null ? estacion.numero : 0;
        nombreEstacion = estacion != null ? estacion.titulo : "";
        radioLlegada = DistanciaLlegada;
        // El mismo paso puede cambiar de sitio (p. ej. del microscopio al banco para cargar la muestra B).
        bool otroDestino = destino != antes;
        if (nueva || otroDestino) llegado = false;

        caso.text = titulo;
        paso.text = total > 0 ? $"PASO {pasoN} DE {total}  ·  {nombrePaso}" : nombrePaso;
        objetivo.text = obj;
        RefrescarDestino();
        Puntos(pasoN, total);
        if (etiquetaMeta != null) etiquetaMeta.text = numeroEstacion > 0 ? $"<b>{numeroEstacion}</b>  {nombreEstacion}" : nombreEstacion;
        if (nueva && conAviso && dentro) Mostrar(SegundosVisible);
        else if (otroDestino && dentro) Mostrar(5f);
        if (espejoVacuna) tVacuna = 0f;   // la tarjeta estaba enseñando la misión 3: se vuelve a poner enseguida
    }

    // Puntos de progreso: solo los 'total' del recorrido actual, alineados a la derecha.
    void Puntos(int pasoN, int total)
    {
        for (int k = 0; k < puntos.Length; k++)
        {
            bool ver = k < total;
            if (puntos[k].gameObject.activeSelf != ver) puntos[k].gameObject.SetActive(ver);
            if (!ver) continue;
            puntos[k].transform.localPosition = new Vector3(W / 2f - 0.025f - (total - 1 - k) * 0.014f, H / 2f - 0.052f, -0.001f);
            puntos[k].color = k < pasoN - 1 ? new Color(0.35f, 0.75f, 0.5f, 1f) : k == pasoN - 1 ? Acento : new Color(0.78f, 0.84f, 0.9f, 1f);
            puntos[k].Aplicar();
        }
    }

    // Destino que no es una estación numerada (el laboratorio de vacunas al terminar las misiones 1 y 2).
    public void DestinoLibre(Transform t, string nombre, float radio)
    {
        if (destino != t) llegado = false;
        destino = t;
        numeroEstacion = 0;
        nombreEstacion = nombre;
        radioLlegada = radio;
        if (etiquetaMeta != null) etiquetaMeta.text = nombre;
        RefrescarDestino();
    }

    // La tarjeta enseña el paso de la vacuna mientras la misión 3 está en marcha.
    void EspejoVacuna(RetoManager rm)
    {
        var rv = RetoVacuna.Instancia;
        bool si = rm != null && rm.Actual == EtapaReto.Cierre && rv != null && rv.Actual >= EtapaVacuna.Antigeno && rv.Actual <= EtapaVacuna.Eficacia;
        if (!si)
        {
            if (espejoVacuna) { espejoVacuna = false; pasoVacuna = null; if (rm != null) rm.RefrescarGuia(); }
            return;
        }
        espejoVacuna = true;
        string t = $"{Misiones.Codigo3} · {ContenidoVacuna.Titulo.ToUpper()}";
        string p = $"PASO {(int)rv.Actual} DE 9  ·  {rv.TituloActual}";
        string o = rv.ObjetivoActual;
        if (caso.text == t && paso.text == p && objetivo.text == o && destino == null) return;
        bool nuevoPaso = pasoVacuna != p;
        pasoVacuna = p;
        caso.text = t; paso.text = p; objetivo.text = o;
        destino = null;   // en la vacuna guían la franja de la mesa y el apartado AHORA, no las flechas
        RefrescarDestino();
        Puntos((int)rv.Actual, 9);
        if (nuevoPaso && dentro) Mostrar(SegundosVisible);
    }

    void RefrescarDestino()
    {
        if (destinoTxt == null) return;
        const string pie = "<color=#7A8C9C>Y: ocultar  ·  botón menú: ayuda</color>";
        string donde = numeroEstacion > 0 ? $"{numeroEstacion} · {nombreEstacion}" : nombreEstacion;
        destinoTxt.text = destino == null || string.IsNullOrEmpty(donde) ? pie
            : llegado ? $"<color=#2E9E5B><b>Estás en {donde}</b></color>   {pie}"
            : $"Sigue las flechas hasta <b>{donde}</b>   {pie}";
    }

    // ------------------------------------------------------------------ cada frame

    void Update()
    {
        var rm = RetoManager.Instancia;
        var cab = ManosRig.Cabeza;
        bool ahora = rm != null && rm.JugadorDentro;
        bool micro = MicroscopioOptico.AlgunoActivo;
        if (ahora && !dentro) Mostrar(SegundosVisible);       // al entrar al centro: recordar el objetivo
        dentro = ahora;
        if (dentro && Time.time >= tVacuna) { tVacuna = Time.time + 0.5f; EspejoVacuna(rm); }

        // Y: ver u ocultar la tarjeta (con el microscopio Y es "menos aumento").
        if (dentro && !micro && OVRInput.GetDown(OVRInput.Button.Four))
        {
            if (tarjeta.activeSelf && escalaTarjeta > 0.5f) { fijada = false; hastaOcultar = 0f; }
            else { Mostrar(SegundosVisible); fijada = true; }
            if (cab != null) Sonidos.Clic(cab.position);
        }

        bool verTarjeta = dentro && !micro && (fijada || Time.time < hastaOcultar);
        escalaTarjeta = Mathf.MoveTowards(escalaTarjeta, verTarjeta ? 1f : 0f, Time.deltaTime * 5f);
        tarjeta.SetActive(escalaTarjeta > 0.01f);
        if (tarjeta.activeSelf)
        {
            tarjeta.transform.localScale = Vector3.one * Mathf.SmoothStep(0f, 1f, escalaTarjeta);
            if (cab != null) Seguir(cab);
        }
        else colocada = false;

        Ruta(cab, dentro && !micro && destino != null);
    }

    void Mostrar(float segundos)
    {
        hastaOcultar = Time.time + segundos;
    }

    // Abajo en la vista y con seguimiento "perezoso": solo se recoloca cuando se gira bastante la cabeza.
    void Seguir(Transform cab)
    {
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) return;
        f.Normalize();
        var destinoPos = cab.position + f * 0.75f - Vector3.up * 0.24f;
        var actual = tarjeta.transform.position - cab.position; actual.y = 0f;
        bool lejos = !colocada || Vector3.Angle(actual, f) > 28f || Mathf.Abs(actual.magnitude - 0.75f) > 0.25f
                     || Mathf.Abs(tarjeta.transform.position.y - destinoPos.y) > 0.2f;
        if (lejos || moviendo)
        {
            moviendo = true;
            float k = colocada ? 1f - Mathf.Exp(-5f * Time.deltaTime) : 1f;
            tarjeta.transform.position = Vector3.Lerp(tarjeta.transform.position, destinoPos, k);
            if ((tarjeta.transform.position - destinoPos).sqrMagnitude < 0.0004f) moviendo = false;
        }
        var mira = tarjeta.transform.position - cab.position; mira.y = 0f;
        if (mira.sqrMagnitude > 1e-4f) tarjeta.transform.rotation = Quaternion.LookRotation(mira.normalized, Vector3.up);
        colocada = true;
    }
    bool moviendo;

    void Ruta(Transform cab, bool activa)
    {
        if (!activa || cab == null || JugadorNanoLab.Instancia == null || !JugadorNanoLab.Instancia.Pies(out var pies, out _))
        {
            foreach (var f in flechas) if (f.enabled) f.enabled = false;
            if (meta.activeSelf) meta.SetActive(false);
            return;
        }
        float suelo = pies.y + 0.012f;
        var a = new Vector3(pies.x, suelo, pies.z);
        var b = new Vector3(destino.position.x, suelo, destino.position.z);
        var d = b - a;
        float largo = d.magnitude;

        // Meta: anillo en el suelo de la estación y su nombre flotando encima.
        if (!meta.activeSelf) meta.SetActive(true);
        meta.transform.position = b;
        float latido = 1f + 0.08f * Mathf.Sin(Time.time * (llegado ? 6f : 3f));
        anillo.transform.localScale = Vector3.one * 1.1f * latido;
        var c = ColorMeta; c.a = llegado ? 0.95f : 0.75f;
        anillo.color = c;
        var haciaCab = etiquetaMeta.transform.position - cab.position; haciaCab.y = 0f;
        if (haciaCab.sqrMagnitude > 1e-4f) etiquetaMeta.transform.rotation = Quaternion.LookRotation(haciaCab.normalized, Vector3.up);
        etiquetaMeta.gameObject.SetActive(!llegado);

        bool cerca = largo < radioLlegada;
        if (cerca && !llegado)
        {
            llegado = true;
            RefrescarDestino();
            Mostrar(6f);   // al llegar: qué hay que hacer aquí
            Sonidos.Etapa(b);
            ManosRig.Vibrar(b, 0.3f, 0.08f);
        }
        else if (!cerca && largo > radioLlegada + 1f && llegado)
        {
            llegado = false;   // se alejó: vuelve la ruta
            RefrescarDestino();
        }

        // Flechas: desde 0,7 m delante del jugador hasta 1 m antes de la estación, con una onda de brillo que
        // avanza hacia la meta (indica el sentido sin texto).
        var dir = largo > 1e-3f ? d / largo : Vector3.forward;
        var rot = Quaternion.LookRotation(Vector3.down, dir);
        int n = 0;
        if (!llegado)
            for (float s = 0.7f; s < largo - Mathf.Max(1f, radioLlegada - 0.6f) && n < flechas.Length; s += Paso, n++)
            {
                var fl = flechas[n];
                fl.enabled = true;
                fl.transform.SetPositionAndRotation(a + dir * s, rot);
                float onda = Mathf.Repeat(s * 0.9f - Time.time * 1.6f, 2.2f);
                float brillo = Mathf.Clamp01(1f - Mathf.Abs(onda - 0.4f) * 1.6f);
                var col = ColorRuta;
                col.a = Mathf.Lerp(0.3f, 0.95f, brillo) * Mathf.Clamp01(s / 1.2f);   // suaves junto a los pies
                fl.color = col;
            }
        for (int i = n; i < flechas.Length; i++) if (flechas[i].enabled) flechas[i].enabled = false;
    }

    // ------------------------------------------------------------------ construcción

    void Montar()
    {
        Material mat = null;
        var ejemplo = FindObjectOfType<PanelRedondeado>(true);
        if (ejemplo != null) mat = ejemplo.GetComponent<MeshRenderer>().sharedMaterial;
        TMP_FontAsset fuente = null;
        foreach (var tx in FindObjectsOfType<TextMeshPro>(true)) if (tx.font != null) { fuente = tx.font; break; }

        // Tarjeta
        tarjeta = new GameObject("TarjetaObjetivo");
        tarjeta.transform.SetParent(transform, false);
        Quad(tarjeta.transform, "Fondo", Vector3.zero, W, H, 0.025f, Fondo, new Color(0.55f, 0.85f, 1f, 1f), 0.004f, mat, 0);
        Quad(tarjeta.transform, "Franja", new Vector3(-W / 2f + 0.012f, 0f, -0.001f), 0.006f, H - 0.04f, 0.003f, Acento, Color.clear, 0f, mat, 1);
        caso = Texto(tarjeta.transform, "Caso", new Vector3(0.008f, H / 2f - 0.025f, -0.003f), 0.13f, false, new Color(0.4f, 0.5f, 0.6f), fuente, W - 0.06f, TextAlignmentOptions.Left);
        paso = Texto(tarjeta.transform, "Paso", new Vector3(0.008f, H / 2f - 0.052f, -0.003f), 0.17f, true, Acento, fuente, W - 0.06f, TextAlignmentOptions.Left);
        objetivo = Texto(tarjeta.transform, "Objetivo", new Vector3(0.008f, -0.01f, -0.003f), 0.155f, false, Tinta, fuente, W - 0.06f, TextAlignmentOptions.Left);
        objetivo.rectTransform.sizeDelta = new Vector2(W - 0.06f, 0.11f);
        objetivo.verticalAlignment = VerticalAlignmentOptions.Middle;
        // Un objetivo largo se encoge en lugar de salirse de la tarjeta.
        objetivo.enableAutoSizing = true;
        objetivo.fontSizeMax = 0.155f;
        objetivo.fontSizeMin = 0.105f;
        destinoTxt = Texto(tarjeta.transform, "Destino", new Vector3(0.008f, -H / 2f + 0.03f, -0.003f), 0.12f, false, Tinta, fuente, W - 0.06f, TextAlignmentOptions.Left);
        puntos = new PanelRedondeado[MaxPuntos];
        for (int i = 0; i < puntos.Length; i++)
            puntos[i] = Quad(tarjeta.transform, "Punto" + i, new Vector3(W / 2f - 0.025f - (puntos.Length - 1 - i) * 0.014f, H / 2f - 0.052f, -0.001f),
                             0.008f, 0.008f, 0.004f, Acento, Color.clear, 0f, mat, 1);
        tarjeta.SetActive(false);

        // Ruta
        var spFlecha = Sprite.Create(Flecha(), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        var spAnillo = Sprite.Create(Anillo(), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f);
        flechas = new SpriteRenderer[MaxFlechas];
        for (int i = 0; i < flechas.Length; i++)
        {
            var f = new GameObject("Flecha" + i).AddComponent<SpriteRenderer>();
            f.transform.SetParent(transform, false);
            f.transform.localScale = Vector3.one * 0.32f;
            f.sprite = spFlecha;
            f.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.enabled = false;
            flechas[i] = f;
        }
        meta = new GameObject("MetaEstacion");
        meta.transform.SetParent(transform, false);
        anillo = new GameObject("Anillo").AddComponent<SpriteRenderer>();
        anillo.transform.SetParent(meta.transform, false);
        anillo.transform.localRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        anillo.sprite = spAnillo;
        etiquetaMeta = Texto(meta.transform, "Nombre", new Vector3(0f, 2.05f, 0f), 0.9f, true, ColorMeta, fuente, 1.6f, TextAlignmentOptions.Center);
        etiquetaMeta.outlineWidth = 0.2f;
        etiquetaMeta.outlineColor = new Color32(0, 20, 40, 230);
        meta.SetActive(false);
    }

    PanelRedondeado Quad(Transform padre, string n, Vector3 pos, float ancho, float alto, float radio, Color c, Color borde, float grosor, Material mat, int orden)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = n;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(padre, false);
        q.transform.localPosition = pos;
        var mr = q.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.sortingOrder = orden;
        if (mat != null) mr.sharedMaterial = mat;
        var p = q.AddComponent<PanelRedondeado>();
        p.ancho = ancho; p.alto = alto; p.radio = radio; p.color = c; p.colorBorde = borde; p.borde = grosor;
        p.Aplicar();
        return p;
    }

    static TextMeshPro Texto(Transform padre, string n, Vector3 pos, float tam, bool negrita, Color color, TMP_FontAsset fuente, float ancho, TextAlignmentOptions al)
    {
        var go = new GameObject(n);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.alignment = al;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.rectTransform.sizeDelta = new Vector2(ancho, 0.03f);
        if (negrita) t.fontStyle = FontStyles.Bold;
        t.sortingOrder = 5;
        return t;
    }

    // Flecha en "V" que apunta hacia +y de la textura (hacia la estación), con bordes suaves.
    static Texture2D Flecha()
    {
        const int N = 64;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "FlechaRuta" };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N;
                float pico = 0.78f - Mathf.Abs(u) * 1.1f;            // línea de la V
                float d = pico - v;                                   // > 0 por debajo de la línea
                float a = Mathf.Clamp01(Mathf.Min(d, 0.2f - d) * 40f) * Mathf.Clamp01((0.44f - Mathf.Abs(u)) * 40f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    // Anillo con un punto central, para marcar la estación en el suelo.
    static Texture2D Anillo()
    {
        const int N = 128;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "AnilloMeta" };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float r = new Vector2((x + 0.5f) / N - 0.5f, (y + 0.5f) / N - 0.5f).magnitude * 2f;
                float aro = Mathf.Clamp01(Mathf.Min(r - 0.78f, 0.96f - r) * 30f);
                float aro2 = Mathf.Clamp01(Mathf.Min(r - 0.5f, 0.6f - r) * 30f) * 0.6f;
                float a = Mathf.Max(aro, aro2);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
