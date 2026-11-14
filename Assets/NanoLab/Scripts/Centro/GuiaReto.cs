using TMPro;
using UnityEngine;

// Guía del reto en el Centro de Experimentación. Para no romper la presencia, nada sigue a la cabeza:
//  - OBJETIVO: va al BRAZALETE de la muñeca izquierda (BrazaleteBaymax), que vibra con cada paso nuevo y enseña
//    la misión, el paso y la acción al girar la muñeca hacia la cara (antes era una tarjeta que perseguía la vista).
//  - RUTA: flechas en el suelo que fluyen desde el jugador hasta la estación del paso actual y un anillo con su
//    número y nombre. Al llegar, la ruta se apaga y el anillo late.
// La crea RetoManager; todo se construye por código.
public class GuiaReto : MonoBehaviour
{
    const float DistanciaLlegada = 1.6f, Paso = 0.45f;
    const int MaxFlechas = 40;
    static readonly Color ColorRuta = new Color(0.3f, 0.85f, 1f);
    static readonly Color ColorMeta = new Color(1f, 0.8f, 0.3f);

    // Ruta
    Transform destino;
    SpriteRenderer[] flechas;
    SpriteRenderer anillo;
    TextMeshPro etiquetaMeta;
    GameObject meta;
    bool dentro, llegado;
    int etapa = -1;
    string nombreEstacion;
    int numeroEstacion;
    float radioLlegada = DistanciaLlegada;   // a qué distancia se da por llegado (más grande para el laboratorio de vacunas)
    // Misión 3: con las misiones 1 y 2 terminadas, el brazalete enseña el paso de la vacuna.
    bool espejoVacuna;
    float tVacuna;
    string pasoVacuna;

    public static GuiaReto Crear()
    {
        var g = new GameObject("GuiaReto").AddComponent<GuiaReto>();
        g.Montar();
        return g;
    }

    static BrazaleteBaymax Brazalete => BrazaleteBaymax.Instancia;

    // ------------------------------------------------------------------ API para RetoManager

    // 'clave' identifica el paso (cambia con la misión y con el subpaso); 'pasoN' de 'total' es lo que se enseña.
    public void Etapa(int clave, string titulo, string obj, EstacionGuia estacion, bool conAviso, int pasoN, int total, string nombrePaso)
    {
        bool nueva = clave != etapa;
        etapa = clave;
        var antes = destino;
        destino = estacion != null ? (estacion.puntoBaliza != null ? estacion.puntoBaliza : estacion.transform) : null;
        numeroEstacion = estacion != null ? estacion.numero : 0;
        nombreEstacion = estacion != null ? estacion.titulo : "";
        radioLlegada = DistanciaLlegada;
        // El mismo paso puede cambiar de sitio (p. ej. del microscopio al banco para cargar la muestra B).
        if (nueva || destino != antes) llegado = false;
        if (etiquetaMeta != null) etiquetaMeta.text = numeroEstacion > 0 ? $"<b>{numeroEstacion}</b>  {nombreEstacion}" : nombreEstacion;

        if (espejoVacuna) { tVacuna = 0f; return; }   // el brazalete está enseñando la misión 3
        var b = Brazalete;
        if (b == null) return;
        string cab = total > 0 ? $"{titulo.Split('·')[0].Trim()} · PASO {pasoN} DE {total} · {nombrePaso}" : titulo;
        b.Objetivo(cab, obj, pasoN, total);
        if (nueva && conAviso && dentro) b.Notificar(null, Aviso.Tipo.Info);   // solo vibra: "hay paso nuevo"
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
    }

    // Meta fuera del ciclo de las misiones 1 y 2 (en la MISIÓN 3: un equipo del centro que hace falta, de EquiposCentro).
    // Mismas flechas y anillo hasta él; el objetivo va al brazalete (vibra si es un destino nuevo).
    public void Externo(string titulo, string pasoTxt, string obj, EstacionGuia estacion)
    {
        var nuevoDestino = estacion != null ? (estacion.puntoBaliza != null ? estacion.puntoBaliza : estacion.transform) : null;
        bool nueva = estacion == null || destino != nuevoDestino;
        destino = nuevoDestino;
        numeroEstacion = estacion != null ? estacion.numero : 0;
        nombreEstacion = estacion != null ? estacion.titulo : "";
        radioLlegada = DistanciaLlegada;
        if (nueva) llegado = false;
        etapa = -1;   // al volver a Etapa(...) cuenta como paso nuevo
        if (etiquetaMeta != null) etiquetaMeta.text = numeroEstacion > 0 ? $"<b>{numeroEstacion}</b>  {nombreEstacion}" : nombreEstacion;
        var b = Brazalete;
        if (b == null) return;
        b.Objetivo($"{titulo} · {pasoTxt}", obj, 0, 0);
        if (nueva && dentro) b.Notificar(null, Aviso.Tipo.Info);
    }

    // El brazalete enseña el paso de la vacuna mientras la misión 3 está en marcha.
    void EspejoVacuna(RetoManager rm)
    {
        var rv = RetoVacuna.Instancia;
        bool si = rm != null && rm.Actual == EtapaReto.Cierre && rv != null && rv.Actual >= EtapaVacuna.Antigeno && rv.Actual <= EtapaVacuna.Eficacia;
        // Un equipo del centro (ultracongelador, incubadora CO2) que hace falta para la vacuna: manda su guía (Externo),
        // con flechas hasta él; el espejo de la vacuna no la pisa.
        if (si && rm.GuiaExternaActiva) return;
        if (!si)
        {
            if (espejoVacuna) { espejoVacuna = false; pasoVacuna = null; if (rm != null) rm.RefrescarGuia(); }
            return;
        }
        espejoVacuna = true;
        destino = null;   // en la vacuna guían la franja de la mesa y el apartado AHORA, no las flechas
        string p = $"{Misiones.Codigo3} · PASO {(int)rv.Actual} DE 9 · {rv.TituloActual}";
        bool nuevoPaso = pasoVacuna != p;
        pasoVacuna = p;
        var b = Brazalete;
        if (b == null) return;
        b.Objetivo(p, rv.ObjetivoActual, (int)rv.Actual, 9);
        if (nuevoPaso && dentro) b.Notificar(null, Aviso.Tipo.Info);
    }

    // ------------------------------------------------------------------ cada frame

    void Update()
    {
        var rm = RetoManager.Instancia;
        var cab = ManosRig.Cabeza;
        bool ahora = rm != null && rm.JugadorDentro;
        if (ahora && !dentro && Brazalete != null) Brazalete.Notificar(null, Aviso.Tipo.Info);   // al entrar: "mira tu muñeca"
        dentro = ahora;
        if (Time.time >= tVacuna) { tVacuna = Time.time + 0.5f; EspejoVacuna(rm); }
        Ruta(cab, dentro && !MicroscopioOptico.AlgunoActivo && destino != null);
    }

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

        // Meta: anillo en el suelo de la estación y su nombre encima (solo gira en horizontal, como un letrero).
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
            Sonidos.Etapa(b);
            ManosRig.Vibrar(b, 0.3f, 0.08f);
        }
        else if (!cerca && largo > radioLlegada + 1f && llegado) llegado = false;   // se alejó: vuelve la ruta

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
        TMP_FontAsset fuente = EstiloNanoLab.Fuente;
        if (fuente == null) foreach (var tx in FindObjectsOfType<TextMeshPro>(true)) if (tx.font != null) { fuente = tx.font; break; }

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
        // Nombre de la estación sobre el anillo: más discreto que antes (0,9 > 0,6).
        var go = new GameObject("Nombre");
        go.transform.SetParent(meta.transform, false);
        go.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        etiquetaMeta = go.AddComponent<TextMeshPro>();
        if (fuente != null) etiquetaMeta.font = fuente;
        etiquetaMeta.fontSize = 0.6f;
        etiquetaMeta.color = ColorMeta;
        etiquetaMeta.alignment = TextAlignmentOptions.Center;
        etiquetaMeta.enableWordWrapping = true;
        etiquetaMeta.rectTransform.sizeDelta = new Vector2(1.4f, 0.3f);
        etiquetaMeta.fontStyle = FontStyles.Bold;
        etiquetaMeta.sortingOrder = 5;
        etiquetaMeta.outlineWidth = 0.2f;
        etiquetaMeta.outlineColor = new Color32(0, 20, 40, 230);
        meta.SetActive(false);
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
