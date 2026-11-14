using UnityEngine;

// Guía del reto en el Centro de Experimentación. Para no romper la presencia, nada sigue a la cabeza:
//  - OBJETIVO: va al BRAZALETE de la muñeca izquierda (BrazaleteBaymax), que vibra con cada paso nuevo y enseña
//    la misión, el paso y la acción al girar la muñeca hacia la cara (antes era una tarjeta que perseguía la vista).
//  - LUZ GUÍA (antes: flechas en el suelo + anillo + nombre flotante, que el profesor vio como "señales"): una sola
//    luz pequeña de Baymax que aparece junto al jugador con cada paso nuevo, vuela por delante de él hacia la estación
//    (esperándolo si se para) y, al llegar, se funde en la estación con un destello. Si la estación queda a la
//    espalda, la luz se asoma por el borde de la vista del lado hacia el que hay que girarse. La estación activa
//    además late (marco de su cartel). Un solo elemento en vez de 40 flechas: menos ruido visual.
// La crea RetoManager; todo se construye por código.
public class GuiaReto : MonoBehaviour
{
    const float DistanciaLlegada = 1.6f;
    const float Adelanto = 1.9f;          // cuánto va la luz por delante del jugador (m)
    const float AlturaVuelo = 1.45f;      // sobre el suelo, algo por debajo de los ojos
    const float VelocidadMax = 2.4f;      // m/s
    static readonly Color ColorLuz = new Color(0.45f, 0.9f, 1f);

    // Luz guía
    Transform destino;
    Transform luz;
    SpriteRenderer nucleo, halo;
    TrailRenderer estela;
    Vector3 velLuz;
    float alfaLuz, destello = -1f;
    Transform destinoMostrado;
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
    }

    // Meta fuera del ciclo de las misiones 1 y 2 (en la MISIÓN 3: un equipo del centro que hace falta, de EquiposCentro).
    // La misma luz guía lleva hasta él; el objetivo va al brazalete (vibra si es un destino nuevo).
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
        // con la luz guía hasta él; el espejo de la vacuna no la pisa.
        if (si && rm.GuiaExternaActiva) return;
        if (!si)
        {
            if (espejoVacuna) { espejoVacuna = false; pasoVacuna = null; if (rm != null) rm.RefrescarGuia(); }
            return;
        }
        espejoVacuna = true;
        destino = null;   // en la vacuna guían la franja de la mesa y el apartado AHORA, no la luz guía
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
        Luz(cab, dentro && !MicroscopioOptico.AlgunoActivo && destino != null);
    }

    // Posición de la luz que se usa en las pruebas y en las capturas.
    public bool LuzVisible => luz != null && luz.gameObject.activeSelf && alfaLuz > 0.05f;
    public Vector3 PosicionLuz => luz != null ? luz.position : transform.position;
    public Transform Destino => destino;
    public bool Llegado => llegado;

    void Luz(Transform cab, bool activa)
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        Vector3 pies = Vector3.zero;
        bool hayPies = cab != null && JugadorNanoLab.Instancia != null && JugadorNanoLab.Instancia.Pies(out pies, out _);
        if (!activa || !hayPies)
        {
            Apagar(dt);
            return;
        }
        float suelo = pies.y;
        var objetivo = new Vector3(destino.position.x, suelo + AlturaVuelo + 0.15f, destino.position.z);
        var h = new Vector3(cab.position.x, 0f, cab.position.z);
        var d = new Vector3(objetivo.x, 0f, objetivo.z) - h;
        float largo = d.magnitude;
        var dir = largo > 1e-3f ? d / largo : Vector3.forward;

        // ¿Ha llegado? (con histéresis: si se aleja, la luz vuelve a por él).
        bool cerca = largo < radioLlegada;
        if (cerca && !llegado)
        {
            llegado = true;
            destello = 0f;
            Sonidos.Etapa(objetivo);
            ManosRig.Vibrar(objetivo, 0.3f, 0.08f);
        }
        else if (!cerca && largo > radioLlegada + 1f && llegado) llegado = false;

        // Paso nuevo (o vuelve tras alejarse): la luz nace junto al jugador, a un lado de la vista, y avisa con un tono.
        bool nuevo = destinoMostrado != destino || (!llegado && alfaLuz <= 0.01f && destello < 0f);
        if (nuevo && !llegado)
        {
            destinoMostrado = destino;
            var f0 = Horizontal(cab.forward);
            luz.position = cab.position + f0 * 0.75f + Vector3.Cross(Vector3.up, f0) * 0.18f + Vector3.down * 0.2f;
            velLuz = Vector3.zero;
            estela.Clear();
            alfaLuz = 0f;
            destello = -1f;
            Sonidos.Pista(luz.position);
        }

        if (!luz.gameObject.activeSelf) luz.gameObject.SetActive(true);
        Vector3 deseado;
        if (llegado)
        {
            // Al llegar: entra en la estación, destella y se apaga (la estación sigue marcada por su cartel).
            deseado = objetivo;
            if (destello >= 0f) destello += dt / 0.6f;
            alfaLuz = Mathf.MoveTowards(alfaLuz, destello >= 1f ? 0f : 1f, dt / 0.35f);
        }
        else
        {
            var fwd = Horizontal(cab.forward);
            float ang = Vector3.SignedAngle(fwd, dir, Vector3.up);
            if (Mathf.Abs(ang) > 65f)
            {
                // Estación fuera de la vista: la luz se asoma por el borde, del lado hacia el que hay que girar.
                var borde = Quaternion.AngleAxis(Mathf.Sign(ang) * 48f, Vector3.up) * fwd;
                deseado = cab.position + borde * 1.1f;
                deseado.y = cab.position.y - 0.12f;
            }
            else
            {
                // Por delante del jugador, en la recta hacia la estación; si está cerca, va directa a ella.
                float tramo = Mathf.Min(largo, Adelanto);
                deseado = Vector3.Lerp(new Vector3(cab.position.x, suelo + AlturaVuelo, cab.position.z), objetivo, tramo / Mathf.Max(largo, 1e-3f));
            }
            deseado.y += 0.04f * Mathf.Sin(Time.time * 2.6f);   // flota
            alfaLuz = Mathf.MoveTowards(alfaLuz, 1f, dt / 0.3f);
        }
        luz.position = Vector3.SmoothDamp(luz.position, deseado, ref velLuz, llegado ? 0.25f : 0.45f, VelocidadMax, dt);

        // Aspecto: núcleo que late, halo suave que mira a la cámara y estela; destello al llegar.
        float latido = 1f + 0.12f * Mathf.Sin(Time.time * 5f);
        float flash = destello >= 0f && destello < 1f ? Mathf.Sin(destello * Mathf.PI) : 0f;
        nucleo.transform.localScale = Vector3.one * 0.05f * latido * (1f + flash * 1.5f);
        halo.transform.localScale = Vector3.one * 0.2f * (1f + 0.15f * Mathf.Sin(Time.time * 3f)) * (1f + flash * 2.5f);
        var mirar = luz.position - cab.position;
        if (mirar.sqrMagnitude > 1e-4f) luz.rotation = Quaternion.LookRotation(mirar, Vector3.up);
        Pintar(alfaLuz);
        if (llegado && alfaLuz <= 0.001f) luz.gameObject.SetActive(false);
    }

    void Apagar(float dt)
    {
        if (luz == null || !luz.gameObject.activeSelf) return;
        alfaLuz = Mathf.MoveTowards(alfaLuz, 0f, dt / 0.25f);
        Pintar(alfaLuz);
        if (alfaLuz <= 0.001f) { luz.gameObject.SetActive(false); if (destino == null) destinoMostrado = null; }
    }

    void Pintar(float a)
    {
        var c = ColorLuz;
        nucleo.color = new Color(1f, 1f, 1f, a);
        c.a = 0.55f * a; halo.color = c;
        var g = estela.colorGradient;
        var ck = new[] { new GradientColorKey(ColorLuz, 0f), new GradientColorKey(ColorLuz, 1f) };
        var ak = new[] { new GradientAlphaKey(0.6f * a, 0f), new GradientAlphaKey(0f, 1f) };
        g.SetKeys(ck, ak);
        estela.colorGradient = g;
    }

    static Vector3 Horizontal(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
    }

    // ------------------------------------------------------------------ construcción

    void Montar()
    {
        var spNucleo = Sprite.Create(Radial(64, 0.55f, 6f), new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        var spHalo = Sprite.Create(Radial(128, 0f, 2.2f), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f);
        luz = new GameObject("LuzGuia").transform;
        luz.SetParent(transform, false);
        halo = new GameObject("Halo").AddComponent<SpriteRenderer>();
        halo.transform.SetParent(luz, false);
        halo.sprite = spHalo;
        halo.sortingOrder = 4;
        nucleo = new GameObject("Nucleo").AddComponent<SpriteRenderer>();
        nucleo.transform.SetParent(luz, false);
        nucleo.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        nucleo.sprite = spNucleo;
        nucleo.sortingOrder = 5;
        foreach (var r in new Renderer[] { halo, nucleo }) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        estela = luz.gameObject.AddComponent<TrailRenderer>();
        estela.time = 0.45f;
        estela.minVertexDistance = 0.02f;
        estela.widthCurve = new AnimationCurve(new Keyframe(0f, 0.035f), new Keyframe(1f, 0f));
        estela.material = new Material(Shader.Find("Sprites/Default"));
        estela.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        estela.receiveShadows = false;
        estela.numCapVertices = 2;
        luz.gameObject.SetActive(false);
    }

    // Disco con borde suave: núcleo (centro sólido) o halo (degradado hasta el borde).
    static Texture2D Radial(int n, float solido, float caida)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "LuzGuia" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f).magnitude * 2f;
                float a = r <= solido ? 1f : Mathf.Pow(Mathf.Clamp01(1f - (r - solido) / Mathf.Max(1e-3f, 1f - solido)), caida);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
