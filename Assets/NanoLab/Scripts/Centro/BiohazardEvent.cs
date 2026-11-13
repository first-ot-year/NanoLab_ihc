using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// EVENTO SORPRESA DE BIOSEGURIDAD (MISIÓN 2).
// Al terminar el PRIMER experimento de la cámara experimental (7) falla el sellado de la compuerta: salen
// aerosoles de S. aureus y caen en varias superficies alrededor de la cámara. El laboratorio entra en ALARMA:
//   1) luces rojas intermitentes (Corrutina Parpadeo) y pitido de alarma;
//   2) la guía (tarjeta + flechas + cartel AHORA) lleva al KIT DE DERRAMES, junto al autoclave;
//   3) el estudiante toma el PULVERIZADOR (agarrable del Meta Interaction SDK, como el resto del proyecto) y,
//      con el GATILLO, rocía: un SphereCast hacia delante desde la boquilla; las zonas con la etiqueta
//      "Contaminated" que reciben el rocío pierden opacidad poco a poco hasta desaparecer (SetActive(false));
//   4) la guía va a la zona contaminada más cercana; mientras quede alguna, la misión 2 no sigue
//      (RetoManager.Bloqueado): microscopio DESPUÉS, terminal, prensa, cámara y CONCLUSIONES avisan;
//   5) sin zonas contaminadas, las luces vuelven a su estado normal y Baymax felicita (clip de audio
//      Resources/Audio/BaymaxFelicitacion si existe; si no, una melodía generada).
// Pasa una sola vez por partida (se guarda) y vuelve a pasar tras REINICIAR en el ARCHIVO (8).
//
// Notas de la escena: casi todas las luces de la sala están HORNEADAS (Light Mode = Baked); cambiar su color o
// intensidad en ejecución no tiene efecto. Por eso la alarma (IluminacionAlarma) añade 2 luces puntuales en tiempo
// real (sin sombras), balizas emisivas, y además tiñe las luces de la escena que sí son Realtime/Mixed. Todo se
// guarda y se restaura al terminar.
public class BiohazardEvent : MonoBehaviour
{
    public const string Tag = "Contaminated";
    public const string NombreCapa = "Biohazard";
    public enum Estado { Inactivo, Alarma, Resuelto }

    public static BiohazardEvent Instancia { get; private set; }
    public static bool Activo => Instancia != null && Instancia.estado == Estado.Alarma;
    public static bool YaOcurrio => Instancia != null && Instancia.estado == Estado.Resuelto;
    public static int Capa => LayerMask.NameToLayer(NombreCapa);

    [Header("Ajustes")]
    public int zonasContaminadas = 4;
    public float alcanceSpray = 1.8f, radioSpray = 0.2f;
    public float segundosPorZona = 1.2f;          // rociando de lleno
    public Color rojoAlarma = new Color(1f, 0.08f, 0.05f);
    public AudioClip felicitacion;                // si es null: Resources/Audio/BaymaxFelicitacion o melodía generada

    public Estado estado { get; private set; } = Estado.Inactivo;
    public Pulverizador spray { get; private set; }
    public readonly List<Contaminacion> zonas = new List<Contaminacion>();
    public EstacionGuia guiaKit, guiaZona;
    public bool TieneSpray { get; private set; }

    IluminacionAlarma luces;
    Coroutine parpadeo;
    float tInicio, tSpray = -1f;
    Transform kit;

    public int Restantes => zonas.Count(z => z != null && !z.Limpia);
    public EstacionGuia GuiaActual => !TieneSpray ? guiaKit : guiaZona;

    public string Objetivo => !TieneSpray
        ? "¡ALERTA DE BIOSEGURIDAD! La cámara experimental soltó aerosoles de S. aureus. Toma el PULVERIZADOR del KIT DE DERRAMES (junto al AUTOCLAVE)."
        : $"¡ALERTA DE BIOSEGURIDAD! Busca las manchas contaminadas (anillo rojo) y rocíalas con el GATILLO hasta que desaparezcan. Quedan {Restantes}.";

    public string Bloqueo => "ALERTA DE BIOSEGURIDAD: primero descontamina el área. " +
                             (!TieneSpray ? "Toma el PULVERIZADOR del KIT DE DERRAMES (junto al AUTOCLAVE)." : $"Quedan {Restantes} zonas contaminadas.");

    // ------------------------------------------------------------------ montaje (MontajeMisiones)

    public static BiohazardEvent Montar(Transform centro)
    {
        if (Instancia != null) Destroy(Instancia.gameObject);
        var ev = new GameObject("BiohazardEvent").AddComponent<BiohazardEvent>();
        ev.transform.SetParent(centro, false);
        ev.CrearKit();
        ev.luces = new IluminacionAlarma(ev.transform, centro);
        return ev;
    }

    void Awake() => Instancia = this;
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    // Kit de derrames en la esquina de bioseguridad (entre la incubadora y el autoclave), o junto a la cámara.
    void CrearKit()
    {
        var inc = EquiposCentro.Incubadora != null ? EquiposCentro.Incubadora.transform : null;
        var aut = EquiposCentro.Autoclave != null ? EquiposCentro.Autoclave.transform : null;
        Vector3 pos; Quaternion rot;
        if (inc != null && aut != null)
        {
            pos = Vector3.Lerp(inc.position, aut.position, 0.5f) + inc.forward * 0.05f;
            rot = inc.rotation;
        }
        else
        {
            var cam = RetoManager.Instancia != null && RetoManager.Instancia.camara != null ? RetoManager.Instancia.camara.transform : transform;
            pos = cam.position + cam.right * 1.2f;
            rot = cam.rotation;
        }
        kit = K.Grupo(transform, "KitDerrames", Vector3.zero);
        kit.SetPositionAndRotation(pos, rot);

        // Pedestal + caja amarilla con el símbolo y el soporte del pulverizador (frente: +z local).
        K.Solido(K.Caja(kit, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(0.36f, 0.9f, 0.3f), K.Escena("BlancoMate", new Color(0.9f, 0.9f, 0.9f))));
        K.Caja(kit, "Caja", new Vector3(0f, 0.97f, 0f), new Vector3(0.38f, 0.14f, 0.32f), K.Plano(new Color(1f, 0.82f, 0.1f)));
        var r = K.Texto(kit, "<b>KIT DE DERRAMES</b>\n<size=70%>RIESGO BIOLÓGICO</size>", new Vector3(0f, 0.62f, 0.152f), 0.26f, new Color(0.7f, 0.04f, 0.04f),
                        0.34f, 0.12f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        K.Caja(kit, "Franja", new Vector3(0f, 0.75f, 0.151f), new Vector3(0.36f, 0.03f, 0.002f), K.Plano(rojoAlarma));

        var casa = AgarreRuntime.Zona(kit, "SoporteSpray", "spray", new Vector3(0f, 1.045f, 0.02f), Quaternion.identity, null);
        spray = Pulverizador.Crear(kit, this);
        spray.Encajable.casa = casa;
        AgarreRuntime.Recolocar(spray.Encajable);

        guiaKit = CrearGuia("Guia_Kit", kit, "KIT DE DERRAMES", "Pulverizador desinfectante para derrames biológicos.", new Vector3(0f, 0f, 0.8f));
        guiaZona = CrearGuia("Guia_Zona", transform, "ZONA CONTAMINADA", "Rocía las manchas con anillo rojo.", Vector3.zero);
        kit.gameObject.SetActive(true);
    }

    static EstacionGuia CrearGuia(string nombre, Transform padre, string titulo, string desc, Vector3 baliza)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var g = go.AddComponent<EstacionGuia>();
        g.numero = 0;
        g.titulo = titulo;
        g.descripcion = desc;
        g.puntoBaliza = baliza == Vector3.zero ? go.transform : K.Grupo(go.transform, "PuntoBaliza", baliza);
        return g;
    }

    // ------------------------------------------------------------------ ciclo del evento

    public static void Disparar(Transform foco)
    {
        if (Instancia == null || Instancia.estado != Estado.Inactivo || foco == null) return;
        Instancia.Iniciar(foco);
    }

    public void Iniciar(Transform foco)
    {
        if (estado == Estado.Alarma) return;
        estado = Estado.Alarma;
        TieneSpray = spray != null && spray.Encajable.Agarrado;
        tInicio = Time.time; tSpray = TieneSpray ? 0f : -1f;
        LimpiarZonas();
        CrearZonas(foco);
        parpadeo = StartCoroutine(Parpadeo());
        StartCoroutine(Sirena());
        var cab = ManosRig.Cabeza != null ? ManosRig.Cabeza.position + ManosRig.Cabeza.forward * 0.9f : foco.position + Vector3.up * 1.5f;
        Aviso.Mostrar("<b>¡ALERTA DE BIOSEGURIDAD!</b>\nFalló el sellado de la cámara: hay aerosoles de S. aureus en el área. Toma el PULVERIZADOR del KIT DE DERRAMES.",
                      cab, Aviso.Tipo.Error, 8f);
        ManosRig.Vibrar(cab, 1f, 0.4f);
        Debug.Log($"[Biohazard] Alarma: {zonas.Count} zonas contaminadas alrededor de {foco.name}");
        RetoManager.Instancia?.BiohazardCambio(false);
    }

    void Update()
    {
        if (estado != Estado.Alarma) return;
        if (!TieneSpray && spray != null && spray.Encajable.Agarrado) MarcarSpray();
    }

    public void MarcarSpray()
    {
        if (TieneSpray) return;
        TieneSpray = true;
        tSpray = Time.time - tInicio;
        MoverGuiaZona();
        RetoManager.Instancia?.BiohazardCambio(true);
    }

    void AlLimpiar(Contaminacion c)
    {
        var p = c.transform.position + Vector3.up * 0.4f;
        Sonidos.Encaje(p);
        int r = Restantes;
        if (r > 0)
        {
            Aviso.Mostrar($"Zona descontaminada. Quedan {r}.", p, Aviso.Tipo.Ok, 3f);
            MoverGuiaZona();
            RetoManager.Instancia?.BiohazardCambio(false);
            return;
        }
        Resolver();
    }

    void Resolver()
    {
        estado = Estado.Resuelto;
        if (parpadeo != null) StopCoroutine(parpadeo);
        StartCoroutine(VolverANormal());
        float total = Time.time - tInicio;
        RetoManager.Instancia?.BiohazardResuelto(tSpray, total, zonas.Count);
        Felicitar();
        if (spray != null) spray.Encajable.VolverACasa();
    }

    // La guía apunta a la mancha más cercana al jugador (las flechas del suelo la siguen).
    void MoverGuiaZona()
    {
        var ref0 = ManosRig.Cabeza != null ? ManosRig.Cabeza.position : transform.position;
        var z = zonas.Where(x => x != null && !x.Limpia).OrderBy(x => (x.transform.position - ref0).sqrMagnitude).FirstOrDefault();
        if (z != null) guiaZona.transform.position = z.transform.position;
    }

    // REINICIAR en el ARCHIVO (8) o partida nueva: el evento puede volver a pasar.
    public static void Reiniciar()
    {
        var i = Instancia;
        if (i == null) return;
        i.StopAllCoroutines();
        i.luces.Restaurar();
        i.LimpiarZonas();
        i.estado = Estado.Inactivo;
        i.TieneSpray = false;
        if (i.spray != null) AgarreRuntime.Recolocar(i.spray.Encajable);
    }

    // Partida guardada: si ya se resolvió, no se repite.
    public static void Restaurar(bool resuelto)
    {
        if (Instancia == null || !resuelto || Instancia.estado == Estado.Alarma) return;
        Instancia.estado = Estado.Resuelto;
    }

    // ------------------------------------------------------------------ zonas contaminadas

    void LimpiarZonas()
    {
        foreach (var z in zonas) if (z != null) Destroy(z.gameObject);
        zonas.Clear();
    }

    // Manchas en superficies (suelo y mesas) alrededor de la cámara, a la vista desde ella (nada de atravesar paredes).
    void CrearZonas(Transform foco)
    {
        var origen = foco.position + Vector3.up * 0.6f;
        var candidatos = new List<Vector3>();
        int mascara = ~(1 << 2);   // sin Ignore Raycast
        for (int anillo = 0; anillo < 3; anillo++)
        {
            float radio = 1.0f + anillo * 0.55f;
            for (int k = 0; k < 10; k++)
            {
                float ang = (k * 36f + anillo * 17f) * Mathf.Deg2Rad;
                var p = foco.position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * radio;
                var arriba = new Vector3(p.x, foco.position.y + 2.3f, p.z);
                if (!Physics.Raycast(arriba, Vector3.down, out var hit, 3.5f, mascara, QueryTriggerInteraction.Ignore)) continue;
                if (hit.normal.y < 0.85f || hit.collider.transform.IsChildOf(foco)) continue;
                var visto = hit.point + Vector3.up * 0.05f;
                if (Physics.Linecast(origen, visto, out var bloq, mascara, QueryTriggerInteraction.Ignore) && (bloq.point - visto).sqrMagnitude > 0.04f) continue;
                candidatos.Add(hit.point);
            }
        }
        // Elige las más separadas entre sí.
        var elegidas = new List<Vector3>();
        if (candidatos.Count > 0) elegidas.Add(candidatos[0]);
        while (elegidas.Count < zonasContaminadas && elegidas.Count < candidatos.Count)
        {
            var mejor = candidatos.OrderByDescending(c => elegidas.Min(e => (e - c).sqrMagnitude)).First();
            if (elegidas.Min(e => (e - mejor).sqrMagnitude) < 0.36f) break;
            elegidas.Add(mejor);
        }
        if (elegidas.Count == 0) elegidas.Add(foco.position + foco.forward * 1.2f);
        for (int i = 0; i < elegidas.Count; i++)
        {
            var c = Contaminacion.Crear(transform, elegidas[i] + Vector3.up * 0.004f, 0.42f + 0.1f * (i % 3), segundosPorZona, rojoAlarma);
            c.Limpiada += AlLimpiar;
            zonas.Add(c);
        }
        MoverGuiaZona();
    }

    // ------------------------------------------------------------------ rociado (lo llama el Pulverizador)

    // SphereCast desde la boquilla hacia delante: toda zona "Contaminated" que reciba el rocío pierde carga.
    // Devuelve cuántas zonas se rociaron. Lo usan el pulverizador cada frame con el gatillo apretado y las pruebas.
    public static int Rociar(Vector3 origen, Vector3 direccion, float dt)
    {
        var ev = Instancia;
        float alcance = ev != null ? ev.alcanceSpray : 1.8f, radio = ev != null ? ev.radioSpray : 0.2f;
        int capa = Capa;
        int mascara = capa >= 0 ? 1 << capa : ~0;
        var hits = Physics.SphereCastAll(origen, radio, direccion.normalized, alcance, mascara, QueryTriggerInteraction.Collide);
        var cerca = Physics.OverlapSphere(origen + direccion.normalized * 0.15f, radio, mascara, QueryTriggerInteraction.Collide);
        int ocl = ~((capa >= 0 ? 1 << capa : 0) | (1 << 2));
        int n = 0;
        var vistas = new HashSet<Contaminacion>();
        foreach (var col in hits.Select(h => h.collider).Concat(cerca))
        {
            if (col == null || !EsContaminado(col)) continue;
            var c = col.GetComponentInParent<Contaminacion>();
            if (c == null || c.Limpia || !vistas.Add(c)) continue;
            var punto = col.ClosestPoint(origen);
            // Nada sólido entre la boquilla y la mancha (no se rocía a través de una mesa).
            var desde = origen + (punto - origen).normalized * 0.12f;
            if (Physics.Linecast(desde, punto, out var b, ocl, QueryTriggerInteraction.Ignore) && (b.point - punto).sqrMagnitude > 0.01f) continue;
            c.Rociar(dt);
            n++;
        }
        return n;
    }

    static bool EsContaminado(Collider c)
    {
        try { if (c.CompareTag(Tag)) return true; } catch (UnityException) { }   // etiqueta aún no creada
        return c.GetComponentInParent<Contaminacion>() != null;
    }

    // ------------------------------------------------------------------ luces y sonido

    IEnumerator Parpadeo()
    {
        luces.Encender(rojoAlarma);
        float t = 0f;
        while (estado == Estado.Alarma)
        {
            t += Time.deltaTime;
            float f = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f * 0.9f);   // ~0,9 Hz: llama la atención sin marear
            luces.Fase(f);
            yield return null;
        }
    }

    IEnumerator VolverANormal()
    {
        for (float t = 0f; t < 1.2f; t += Time.deltaTime) { luces.Apagando(1f - t / 1.2f); yield return null; }
        luces.Restaurar();
    }

    IEnumerator Sirena()
    {
        float espera = 1.6f;
        while (estado == Estado.Alarma)
        {
            var p = ManosRig.Cabeza != null ? ManosRig.Cabeza.position : transform.position;
            if (RetoManager.Instancia == null || RetoManager.Instancia.JugadorDentro) Sonidos.Error(p);
            yield return new WaitForSeconds(espera);
            espera = Mathf.Min(espera + 0.6f, 5f);
        }
    }

    void Felicitar()
    {
        var cab = ManosRig.Cabeza;
        var p = cab != null ? cab.position : transform.position;
        var clip = felicitacion != null ? felicitacion : Resources.Load<AudioClip>("Audio/BaymaxFelicitacion");
        float vol = AjustesNanoLab.VolumenEfectos;
        if (clip != null)
        {
            var go = new GameObject("VozBaymax");
            go.transform.position = p;
            if (cab != null) go.transform.SetParent(cab, true);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.volume = vol; src.spatialBlend = 0f;
            src.Play();
            Destroy(go, clip.length + 0.2f);
        }
        else StartCoroutine(Melodia(p));
        var delante = cab != null ? p + cab.forward * 0.9f : p + Vector3.up;
        Aviso.Mostrar("<b>BAYMAX:</b> ¡Laboratorio seguro! Descontaminaste todas las zonas. Así se contiene un derrame: aislar, desinfectar y seguir.",
                      delante, Aviso.Tipo.Ok, 8f);
        Sonidos.Logro(p);
    }

    IEnumerator Melodia(Vector3 p)
    {
        Sonidos.Etapa(p);
        yield return new WaitForSeconds(0.35f);
        Sonidos.Logro(p);
    }
}

// ---------------------------------------------------------------------------------------------- iluminación

// Gestor de la iluminación de alarma: luces puntuales rojas en tiempo real + balizas emisivas + tinte de las luces
// Realtime/Mixed que ya hubiera en el centro. Guarda todo y lo deja como estaba.
public class IluminacionAlarma
{
    readonly List<Light> propias = new List<Light>();
    readonly List<Renderer> balizas = new List<Renderer>();
    readonly List<(Light l, Color c, float i)> escena = new List<(Light, Color, float)>();
    readonly Transform raiz;
    readonly Material matBaliza;
    Color rojo = Color.red;
    readonly Color apagadaBaliza = new Color(0.25f, 0.05f, 0.05f);

    public IluminacionAlarma(Transform padre, Transform centro)
    {
        raiz = K.Grupo(padre, "IluminacionAlarma", Vector3.zero);
        matBaliza = new Material(K.Plano(apagadaBaliza)) { name = "BalizaAlarma" };
        var rv = RetoVacuna.Instancia != null ? RetoVacuna.Instancia.transform.position : centro.position;
        var cam = RetoManager.Instancia != null && RetoManager.Instancia.camara != null ? RetoManager.Instancia.camara.transform.position : rv;
        // Dos luces: una sobre el centro de la sala (la "importante", por píxel) y otra sobre la cámara.
        CrearLuz("LuzAlarma_Sala", rv + Vector3.up * 2.6f, 14f, LightRenderMode.ForcePixel);
        CrearLuz("LuzAlarma_Camara", cam + Vector3.up * 2.3f, 6f, LightRenderMode.Auto);
        // Balizas en los equipos y en la cámara.
        foreach (var e in EquiposCentro.Todos) CrearBaliza(e.transform.position + Vector3.up * (e == EquiposCentro.Cabina ? 2.12f : e == EquiposCentro.Autoclave ? 1.55f : 2.0f));
        CrearBaliza(cam + Vector3.up * 1.9f);
        raiz.gameObject.SetActive(false);
    }

    void CrearLuz(string n, Vector3 p, float rango, LightRenderMode modo)
    {
        var go = new GameObject(n);
        go.transform.SetParent(raiz, false);
        go.transform.position = p;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = rango;
        l.intensity = 0f;
        l.shadows = LightShadows.None;
        l.renderMode = modo;

        propias.Add(l);
    }

    void CrearBaliza(Vector3 p)
    {
        var r = K.Pieza(raiz, "Baliza", PrimitiveType.Cylinder, raiz.InverseTransformPoint(p), new Vector3(0.12f, 0.05f, 0.12f), matBaliza);
        balizas.Add(r);
    }

    public void Encender(Color c)
    {
        rojo = c;
        escena.Clear();
        // Luces de la escena que pueden cambiar en ejecución (no horneadas) y que no son nuestras.
        foreach (var l in Object.FindObjectsOfType<Light>())
        {
            if (propias.Contains(l) || !l.enabled) continue;
            if (l.bakingOutput.isBaked && l.bakingOutput.lightmapBakeType == LightmapBakeType.Baked) continue;
            if (l.transform.IsChildOf(raiz)) continue;
            escena.Add((l, l.color, l.intensity));
        }
        raiz.gameObject.SetActive(true);
    }

    // f en 0..1: pulso de la alarma.
    public void Fase(float f)
    {
        foreach (var l in propias) { l.color = rojo; l.intensity = Mathf.Lerp(0.7f, 3.6f, f); }
        matBaliza.color = Color.Lerp(apagadaBaliza, rojo, f);
        foreach (var (l, c, i) in escena)
            if (l != null) { l.color = Color.Lerp(c, rojo, 0.75f * f); l.intensity = Mathf.Lerp(i, i * 0.55f, f); }
    }

    // De la alarma a lo normal (k: 1 = alarma, 0 = normal).
    public void Apagando(float k)
    {
        foreach (var l in propias) l.intensity = Mathf.Min(l.intensity, 3.6f * k);
        matBaliza.color = Color.Lerp(apagadaBaliza, rojo, k * 0.5f);
        foreach (var (l, c, i) in escena) if (l != null) { l.color = Color.Lerp(c, rojo, 0.5f * k); l.intensity = Mathf.Lerp(i, i * 0.7f, k); }
    }

    public void Restaurar()
    {
        foreach (var (l, c, i) in escena) if (l != null) { l.color = c; l.intensity = i; }
        escena.Clear();
        foreach (var l in propias) l.intensity = 0f;
        matBaliza.color = apagadaBaliza;
        raiz.gameObject.SetActive(false);
    }

    public bool Encendida => raiz.gameObject.activeSelf;
}

// ---------------------------------------------------------------------------------------------- zona contaminada

// Mancha de aerosol (etiqueta "Contaminated", capa "Biohazard"): un charco semitransparente con un anillo rojo
// que late. Con el rocío baja su 'carga' y su opacidad; al llegar a 0 se desvanece y se desactiva.
public class Contaminacion : MonoBehaviour
{
    public float carga = 1f;
    public float segundos = 1.2f;
    public bool Limpia { get; private set; }
    public event System.Action<Contaminacion> Limpiada;

    Material mancha, anillo;
    Color cMancha, cAnillo;
    float ultimoRocio = -9f;

    public static Contaminacion Crear(Transform padre, Vector3 pos, float diametro, float segundos, Color rojo)
    {
        var go = new GameObject("ZonaContaminada");
        go.transform.SetParent(padre, false);
        go.transform.position = pos;
        int capa = BiohazardEvent.Capa;
        if (capa >= 0) go.layer = capa;
        try { go.tag = BiohazardEvent.Tag; } catch (UnityException) { Debug.LogWarning($"[Biohazard] Falta la etiqueta \"{BiohazardEvent.Tag}\" (Edit > Project Settings > Tags and Layers)."); }

        var c = go.AddComponent<Contaminacion>();
        c.segundos = segundos;
        c.cMancha = new Color(0.62f, 0.78f, 0.12f, 0.72f);
        c.cAnillo = rojo;
        c.mancha = new Material(K.Vidrio(c.cMancha)) { name = "Mancha" };
        c.mancha.color = c.cMancha;
        c.anillo = new Material(K.Plano(rojo)) { name = "AnilloMancha" };
        var m = K.Pieza(go.transform, "Mancha", PrimitiveType.Cylinder, Vector3.up * 0.002f, new Vector3(diametro, 0.002f, diametro * 0.85f), c.mancha);
        m.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var a = K.Pieza(go.transform, "Anillo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(diametro + 0.08f, 0.0015f, diametro * 0.85f + 0.08f), c.anillo);
        a.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // Gotitas alrededor.
        for (int i = 0; i < 5; i++)
        {
            float ang = i * 72f * Mathf.Deg2Rad + 0.4f;
            var p = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * diametro * 0.62f + Vector3.up * 0.002f;
            var g = K.Pieza(go.transform, "Gota" + i, PrimitiveType.Cylinder, p, new Vector3(0.05f, 0.002f, 0.05f), c.mancha);
            g.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);
        foreach (Transform t in go.transform) { if (capa >= 0) t.gameObject.layer = capa; try { t.gameObject.tag = BiohazardEvent.Tag; } catch (UnityException) { } }
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = Vector3.up * 0.12f;
        bc.size = new Vector3(diametro + 0.1f, 0.26f, diametro + 0.1f);
        return c;
    }

    public void Rociar(float dt)
    {
        if (Limpia) return;
        ultimoRocio = Time.time;
        carga = Mathf.Max(0f, carga - dt / Mathf.Max(0.1f, segundos));
        mancha.color = new Color(Mathf.Lerp(0.85f, cMancha.r, carga), Mathf.Lerp(0.9f, cMancha.g, carga), Mathf.Lerp(0.95f, cMancha.b, carga), cMancha.a * carga);
        if (carga <= 0f) StartCoroutine(Desvanecer());
    }

    void Update()
    {
        if (Limpia) return;
        bool rociando = Time.time - ultimoRocio < 0.15f;
        float f = 0.5f + 0.5f * Mathf.Sin(Time.time * (rociando ? 12f : 4f));
        var c = rociando ? Color.Lerp(cAnillo, Color.white, 0.5f) : cAnillo;
        anillo.color = new Color(c.r, c.g, c.b, 1f) * Mathf.Lerp(0.35f, 1f, f) * Mathf.Max(0.3f, carga);
    }

    IEnumerator Desvanecer()
    {
        Limpia = true;
        var c0 = mancha.color;
        var a0 = anillo.color;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            float k = 1f - t / 0.4f;
            mancha.color = new Color(c0.r, c0.g, c0.b, c0.a * k);
            anillo.color = a0 * k;
            yield return null;
        }
        gameObject.SetActive(false);
        Limpiada?.Invoke(this);
    }
}

// ---------------------------------------------------------------------------------------------- pulverizador

// Botella de spray agarrable (grip, como todos los agarrables del proyecto con el Meta Interaction SDK).
// Con el GATILLO de la mano que la sostiene rocía: niebla, sonido, vibración y BiohazardEvent.Rociar.
// En el Editor, sin visor, también rocía manteniendo la tecla R mientras está agarrada.
public class Pulverizador : MonoBehaviour
{
    public Encajable Encajable { get; private set; }
    public Transform boquilla;
    ParticleSystem niebla;
    float siguienteSonido;
    public bool Rociando { get; private set; }

    public static Pulverizador Crear(Transform padre, BiohazardEvent ev)
    {
        var go = new GameObject("Pulverizador");
        go.transform.SetParent(padre, false);
        var verde = K.Plano(new Color(0.2f, 0.75f, 0.55f));
        var blanco = K.Escena("BlancoMate", new Color(0.95f, 0.95f, 0.95f));
        K.Pieza(go.transform, "Botella", PrimitiveType.Cylinder, new Vector3(0f, 0.085f, 0f), new Vector3(0.065f, 0.085f, 0.065f), verde);
        K.Pieza(go.transform, "Cuello", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0f), new Vector3(0.03f, 0.012f, 0.03f), blanco);
        K.Caja(go.transform, "Cabezal", new Vector3(0f, 0.205f, 0.012f), new Vector3(0.035f, 0.035f, 0.06f), blanco);
        K.Caja(go.transform, "Gatillo", new Vector3(0f, 0.18f, 0.035f), new Vector3(0.012f, 0.04f, 0.012f), blanco);
        var et = K.Texto(go.transform, "<b>DESINFECTANTE</b>\n<size=70%>etanol 70 %</size>", new Vector3(0f, 0.09f, 0.034f), 0.1f, Color.white, 0.06f, 0.05f,
                         TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        et.outlineWidth = 0.2f; et.outlineColor = new Color32(0, 0, 0, 255);
        foreach (var col in go.GetComponentsInChildren<Collider>()) Object.Destroy(col);

        var p = go.AddComponent<Pulverizador>();
        p.boquilla = K.Grupo(go.transform, "Boquilla", new Vector3(0f, 0.205f, 0.045f));
        p.CrearNiebla();
        AgarreRuntime.Hacer(go, "spray", null, new Vector3(0f, 0.11f, 0.005f), new Vector3(0.07f, 0.22f, 0.08f), 0.3f);
        p.Encajable = go.GetComponent<Encajable>();
        return p;
    }

    void Awake() { if (Encajable == null) Encajable = GetComponent<Encajable>(); }

    // Reutiliza el material de la niebla de la cámara experimental (así el shader está incluido en el build).
    void CrearNiebla()
    {
        var rm = RetoManager.Instancia;
        var origen = rm != null && rm.camara != null ? rm.camara.niebla : null;
        var mat = origen != null ? origen.GetComponent<ParticleSystemRenderer>().sharedMaterial : null;
        if (mat == null) return;
        var go = new GameObject("Niebla");
        go.transform.SetParent(boquilla, false);
        niebla = go.AddComponent<ParticleSystem>();
        niebla.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = niebla.main;
        main.loop = true; main.playOnAwake = false;
        main.startLifetime = 0.45f; main.startSpeed = 3.2f; main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor = new Color(0.9f, 0.97f, 1f, 0.45f);
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = niebla.emission; em.rateOverTime = 110f;
        var sh = niebla.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 9f; sh.radius = 0.004f;
        var col = niebla.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sz = niebla.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.2f));
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
    }

    bool Gatillo()
    {
        if (Encajable == null || !Encajable.Agarrado) return false;
        if (ManosRig.MasCercana(transform.position, 0.35f, out var mando) && OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, mando) > 0.5f) return true;
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.R)) return true;
#endif
        return false;
    }

    void Update()
    {
        bool g = Gatillo();
        if (g != Rociando)
        {
            Rociando = g;
            if (niebla != null) { if (g) niebla.Play(true); else niebla.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        }
        if (!g) return;
        if (Time.time >= siguienteSonido) { Sonidos.Rocio(boquilla.position, 0.35f); siguienteSonido = Time.time + 0.33f; }
        if (BiohazardEvent.Rociar(boquilla.position, boquilla.forward, Time.deltaTime) > 0) ManosRig.Vibrar(boquilla.position, 0.25f, 0.03f);
    }
}
