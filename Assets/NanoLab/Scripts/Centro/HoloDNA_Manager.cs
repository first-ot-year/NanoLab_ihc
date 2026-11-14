using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// ROMPECABEZAS DEL ADN HOLOGRÁFICO (puente entre la MISIÓN 2 y la MISIÓN 3).
// Baymax aisló el virus del brote (la MUESTRA C), pero su banco genómico quedó corrupto: de un fragmento real del
// gen de la espiga S solo queda una hebra. En el PROYECTOR DE ADN (14) flota esa media cadena como holograma y el
// estudiante reconstruye la hebra complementaria con bloques agarrables A, T, C y G:
//   · cada hueco (ZonaEncaje, el "socket" del proyecto) solo admite piezas de tipo "baseADN" (filtro del socket);
//   · al soltar una base, se valida la regla de Chargaff/Watson-Crick: A con T (2 puentes de hidrógeno) y C con G
//     (3 puentes). Correcta: sonido, partículas, puentes de hidrógeno y la pieza queda fija (cinemática y sin agarre).
//     Incorrecta: se explica por qué y la pieza vuelve a la bandeja (no hay castigo, cuenta como error en las métricas);
//   · con la secuencia completa, el holograma brilla, el núcleo de memoria de Baymax se restaura y la MISIÓN 3
//     (la vacuna, que usa justo ese gen) se desbloquea. Se guarda en la partida.
// Hasta terminar la MISIÓN 2 el proyector muestra DATOS CORRUPTOS y las bases no aparecen.
public class HoloDNA_Manager : MonoBehaviour
{
    public const string TipoSocket = "baseADN";
    public const int NumeroGuia = 14;

    // Fragmento del gen S del SARS-CoV-2 (nucleótidos 25 a 32 de la secuencia codificante: ...TTG CCA CTA GTC...).
    public string plantilla = "CCACTAGT";
    public int basesDeCadaTipo = 3;

    public static HoloDNA_Manager Instancia { get; private set; }
    public bool Activo { get; private set; }
    public bool Completo { get; private set; }
    public int Errores { get; private set; }
    public int Colocadas => colocadas.Count(x => x);
    public EstacionGuia guia;
    public ZonaEncaje[] sockets;
    public readonly List<BaseADN> bloques = new List<BaseADN>();

    bool[] colocadas;
    readonly List<Material> holo = new List<Material>();
    readonly List<Color> holoBase = new List<Color>();
    readonly List<GameObject> puentes = new List<GameObject>();
    Transform raiz, nucleo, cadena;
    Renderer nucleoR;
    Material matNucleo;
    TMP_Text titulo, estado;
    Light luz;
    ParticleSystem chispas;
    MetricsLogger metricas;
    float tInicio;

    static readonly Dictionary<char, Color> Colores = new Dictionary<char, Color>
    {
        { 'A', new Color(0.25f, 0.85f, 0.35f) }, { 'T', new Color(0.95f, 0.3f, 0.3f) },
        { 'C', new Color(0.3f, 0.55f, 1f) },     { 'G', new Color(1f, 0.82f, 0.2f) },
    };
    static readonly Dictionary<char, string> Nombres = new Dictionary<char, string>
    { { 'A', "adenina" }, { 'T', "timina" }, { 'C', "citosina" }, { 'G', "guanina" } };

    public static char Complemento(char b) => b == 'A' ? 'T' : b == 'T' ? 'A' : b == 'C' ? 'G' : 'C';
    public static int Puentes(char b) => b == 'A' || b == 'T' ? 2 : 3;

    // ------------------------------------------------------------------ montaje (MontajeMisiones)

    public static HoloDNA_Manager Montar(Transform centro, Vector3 pos, float yaw)
    {
        if (Instancia != null) Destroy(Instancia.gameObject);
        Misiones.AdnReparado = false;   // lo fija la partida al restaurar
        var go = new GameObject("HoloDNA");
        go.transform.SetParent(centro, false);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        var m = go.AddComponent<HoloDNA_Manager>();
        m.Construir();
        return m;
    }

    void Awake() => Instancia = this;
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    Material Holo(Color c, float alfa)
    {
        var m = new Material(K.Vidrio(new Color(c.r, c.g, c.b, alfa))) { name = "Holograma" };
        m.color = new Color(c.r, c.g, c.b, alfa);
        holo.Add(m); holoBase.Add(m.color);
        return m;
    }

    void Construir()
    {
        raiz = transform;
        var cian = new Color(0.35f, 0.9f, 1f);
        int n = plantilla.Length;
        float paso = 0.17f, x0 = -(n - 1) * paso / 2f;

        // Proyector: base oscura con anillo cian y haz translúcido.
        K.Solido(K.Caja(raiz, "Base", new Vector3(0f, 0.4f, -0.1f), new Vector3(1.55f, 0.8f, 0.42f), K.Escena("Marco", new Color(0.15f, 0.17f, 0.2f))));
        K.Caja(raiz, "Emisor", new Vector3(0f, 0.805f, -0.1f), new Vector3(1.45f, 0.01f, 0.3f), K.Plano(cian));
        K.Pieza(raiz, "Haz", PrimitiveType.Cube, new Vector3(0f, 1.27f, -0.1f), new Vector3(1.5f, 0.9f, 0.02f), Holo(cian, 0.06f));

        // Pantalla del título y del estado (se lee desde -z local; girada 180° mira al usuario en +z).
        var pant = K.Grupo(raiz, "Pantalla", new Vector3(0f, 1.98f, -0.1f), Quaternion.Euler(0f, 180f, 0f));
        K.Panel(pant, "Fondo", Vector3.zero, 1.2f, 0.26f, AdaptadorEscena.ColorPanel, AdaptadorEscena.BordePanel, 0.003f, 0.03f);
        titulo = K.Texto(pant, "", new Vector3(0f, 0.07f, -0.004f), 0.3f, K.Cian, 1.14f, 0.08f, TextAlignmentOptions.Center, true, false);
        K.AutoTam(titulo, 0.18f);
        estado = K.Texto(pant, "", new Vector3(0f, -0.04f, -0.004f), 0.2f, Color.white, 1.14f, 0.14f, TextAlignmentOptions.Center, false, true);
        K.AutoTam(estado, 0.12f);

        // Cadena: hebra molde arriba (fija, holográfica) y huecos abajo, con un leve giro de hélice en z.
        cadena = K.Grupo(raiz, "Cadena", new Vector3(0f, 0f, -0.1f));
        K.Pieza(cadena, "EsqueletoMolde", PrimitiveType.Cylinder, new Vector3(0f, 1.62f, 0f), new Vector3(0.025f, n * paso / 2f + 0.05f, 0.025f), Holo(cian, 0.55f), Quaternion.Euler(0f, 0f, 90f));
        K.Pieza(cadena, "EsqueletoNuevo", PrimitiveType.Cylinder, new Vector3(0f, 0.94f, 0f), new Vector3(0.025f, n * paso / 2f + 0.05f, 0.025f), Holo(cian, 0.25f), Quaternion.Euler(0f, 0f, 90f));
        var e5 = K.Texto(cadena, "5'", new Vector3(-x0 + 0.16f, 1.62f, 0.03f), 0.22f, K.Cian, 0.1f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        var e3 = K.Texto(cadena, "3'", new Vector3(x0 - 0.16f, 1.62f, 0.03f), 0.22f, K.Cian, 0.1f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        K.Texto(cadena, "3'", new Vector3(-x0 + 0.16f, 0.94f, 0.03f), 0.22f, K.Cian, 0.1f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        K.Texto(cadena, "5'", new Vector3(x0 - 0.16f, 0.94f, 0.03f), 0.22f, K.Cian, 0.1f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));

        sockets = new ZonaEncaje[n];
        colocadas = new bool[n];
        // En el mundo la hebra se lee de izquierda a derecha para el usuario (que mira hacia -z local).
        for (int i = 0; i < n; i++)
        {
            char b = plantilla[i];
            float x = -(x0 + i * paso);   // girado: la primera base queda a la izquierda del usuario
            float z = 0.06f * Mathf.Sin(i * 0.9f);
            var col = Colores[b];
            K.Pieza(cadena, "Molde" + i, PrimitiveType.Cube, new Vector3(x, 1.5f, z), new Vector3(0.1f, 0.14f, 0.1f), Holo(col, 0.45f));
            var tl = K.Texto(cadena, $"<b>{b}</b>", new Vector3(x, 1.5f, z + 0.052f), 0.5f, Color.white, 0.1f, 0.12f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
            tl.outlineWidth = 0.2f; tl.outlineColor = new Color32(0, 0, 0, 200);
            K.Pieza(cadena, "Varilla" + i, PrimitiveType.Cylinder, new Vector3(x, 1.38f, z), new Vector3(0.012f, 0.06f, 0.012f), Holo(cian, 0.4f));
            // Hueco: fantasma de contorno donde se suelta la base.
            var fantasma = K.Pieza(cadena, "Fantasma" + i, PrimitiveType.Cube, Vector3.zero, new Vector3(0.1f, 0.14f, 0.1f), Holo(Color.white, 0.12f));
            var s = AgarreRuntime.Zona(cadena, "Socket" + i, TipoSocket, new Vector3(x, 1.08f, z), Quaternion.identity, fantasma);
            fantasma.enabled = true;
            s.GetComponent<SphereCollider>().radius = 0.06f;
            int idx = i;
            s.Cambio += e => AlCambiarSocket(idx, e);
            sockets[i] = s;
        }

        // Núcleo de memoria de Baymax: esfera gris (corrupta) que se vuelve dorada al completar.
        matNucleo = new Material(K.Plano(new Color(0.35f, 0.35f, 0.4f))) { name = "NucleoBaymax" };
        nucleo = K.Grupo(raiz, "Nucleo", new Vector3(0f, 1.27f, -0.32f));
        nucleoR = K.Pieza(nucleo, "Esfera", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.16f, matNucleo);
        K.Pieza(nucleo, "Anillo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.26f, 0.004f, 0.26f), Holo(cian, 0.35f), Quaternion.Euler(70f, 0f, 0f));

        // Bandeja flotante delante: 3 bases de cada tipo, mezcladas.
        var bandeja = K.Grupo(raiz, "Bandeja", new Vector3(0f, 0f, 0.36f));
        K.Caja(bandeja, "Repisa", new Vector3(0f, 0.86f, 0f), new Vector3(1.2f, 0.012f, 0.28f), Holo(cian, 0.3f));
        var tipos = new List<char>();
        foreach (var t in "ATCG") for (int k = 0; k < basesDeCadaTipo; k++) tipos.Add(t);
        var rnd = new System.Random(7);
        tipos = tipos.OrderBy(_ => rnd.Next()).ToList();
        int porFila = Mathf.CeilToInt(tipos.Count / 2f);
        for (int i = 0; i < tipos.Count; i++)
        {
            int fila = i / porFila, colu = i % porFila;
            var p = new Vector3((colu - (porFila - 1) / 2f) * 0.18f, 0.93f, fila == 0 ? -0.07f : 0.07f);
            var casa = AgarreRuntime.Zona(bandeja, "Hueco" + i, TipoSocket, p, Quaternion.identity, null);
            var bl = BaseADN.Crear(bandeja, tipos[i], Colores[tipos[i]]);
            bl.Encajable.casa = casa;
            AgarreRuntime.Recolocar(bl.Encajable);
            bloques.Add(bl);
        }

        // Luz del brillo final (tiempo real, sin sombras, solo al completar) y chispas.
        var gl = new GameObject("LuzHolograma");
        gl.transform.SetParent(raiz, false);
        gl.transform.localPosition = new Vector3(0f, 1.3f, 0.2f);
        luz = gl.AddComponent<Light>();
        luz.type = LightType.Point; luz.color = cian; luz.range = 3.5f; luz.intensity = 0f; luz.shadows = LightShadows.None;
        luz.enabled = false;
        chispas = CrearChispas();

        guia = gameObject.AddComponent<EstacionGuia>();
        guia.numero = NumeroGuia;
        guia.titulo = "PROYECTOR DE ADN";
        guia.descripcion = "Banco genómico de Baymax: repara la hebra de ADN del virus.";
        guia.puntoBaliza = K.Grupo(raiz, "PuntoBaliza", new Vector3(0f, 0f, 1.0f));

        Activar(false);
    }

    ParticleSystem CrearChispas()
    {
        var rm = RetoManager.Instancia;
        var origen = rm != null && rm.camara != null ? rm.camara.niebla : null;
        var mat = origen != null ? origen.GetComponent<ParticleSystemRenderer>().sharedMaterial : null;
        if (mat == null) return null;
        var go = new GameObject("Chispas");
        go.transform.SetParent(raiz, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.playOnAwake = false; main.duration = 0.3f;
        main.startLifetime = 0.6f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.04f);
        main.startColor = new Color(0.6f, 1f, 1f, 0.9f);
        main.maxParticles = 200; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.05f;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.05f;
        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return ps;
    }

    // ------------------------------------------------------------------ progreso

    void Update()
    {
        if (!Activo && !Completo && Misiones.Tiene(Misiones.Recuerdo2)) Activar(true);
        if (nucleo != null) nucleo.Rotate(0f, (Completo ? 90f : 12f) * Time.deltaTime, 0f, Space.Self);
        if (!Completo && Activo && cadena != null)
        {
            // Parpadeo leve de "datos corruptos" en los huecos vacíos.
            float f = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
            for (int i = 0; i < sockets.Length; i++)
                if (!colocadas[i] && sockets[i].fantasma != null) sockets[i].fantasma.enabled = f > 0.25f;
        }
    }

    void Activar(bool on)
    {
        Activo = on;
        foreach (var b in bloques) b.gameObject.SetActive(on || Completo);
        titulo.text = "BANCO GENÓMICO DE BAYMAX · GEN DE LA ESPIGA S";
        if (Completo) estado.text = "<color=#39FF88><b>SECUENCIA REPARADA</b> · núcleo de memoria restaurado</color>";
        else if (!on) estado.text = "<color=#FF8A70><b>DATOS CORRUPTOS</b></color>\nSe puede reparar al terminar la MISIÓN 2.";
        else
        {
            estado.text = Estado();
            if (metricas == null) { metricas = new MetricsLogger("ADN"); tInicio = Time.time; }
            RetoManager.Instancia?.AdnCambio(true);
        }
    }

    string Estado() => $"Completa la hebra que falta: <b>A</b> con <b>T</b> y <b>C</b> con <b>G</b>. Bases colocadas: {Colocadas}/{plantilla.Length}";

    bool restaurando;

    void AlCambiarSocket(int i, Encajable e)
    {
        if (e == null || colocadas[i] || restaurando) return;
        var b = e.GetComponent<BaseADN>();
        if (b == null) return;
        var pos = sockets[i].transform.position + Vector3.up * 0.22f;
        char molde = plantilla[i], esperado = Complemento(molde);
        if (!Activo)
        {
            StartCoroutine(Devolver(e));
            return;
        }
        metricas?.Actividad();
        if (b.letra != esperado)
        {
            Errores++;
            metricas?.Error("ADN");
            Aviso.Mostrar($"<b>No encaja.</b> La {Nombres[molde]} ({molde}) solo se empareja con la {Nombres[esperado]} ({esperado}): " +
                          $"{(Puentes(molde) == 2 ? "A y T se unen con 2 puentes de hidrógeno" : "C y G se unen con 3 puentes de hidrógeno")}. " +
                          $"La {b.letra} no tiene la forma ni los puentes que hacen falta.", pos, Aviso.Tipo.Error, 5f);
            Sonidos.Error(pos);
            ManosRig.Vibrar(pos, 0.5f, 0.15f);
            StartCoroutine(Devolver(e));
            return;
        }
        // Correcta: queda fija (cinemática, sin agarre), puentes de hidrógeno, chispas y sonido.
        colocadas[i] = true;
        e.Bloquear(true);
        var rb = e.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        if (sockets[i].fantasma != null) sockets[i].fantasma.enabled = false;
        CrearPuentes(i, Puentes(molde));
        if (chispas != null) { chispas.transform.position = sockets[i].transform.position; chispas.Emit(40); }
        Sonidos.Encaje(pos);
        Sonidos.Carga(pos);
        ManosRig.Vibrar(pos, 0.8f, 0.12f);
        estado.text = Estado();
        RetoManager.Instancia?.AdnCambio(false);
        if (Colocadas == plantilla.Length) StartCoroutine(Completar());
    }

    // La base incorrecta sale del hueco (un frame después: el SDK aún está soltándola) y vuelve a la bandeja.
    IEnumerator Devolver(Encajable e)
    {
        yield return null;
        e.Desencajar();
        e.VolverACasa();
    }

    void CrearPuentes(int i, int n)
    {
        var s = sockets[i].transform.localPosition;
        for (int k = 0; k < n; k++)
        {
            float dx = (k - (n - 1) / 2f) * 0.025f;
            var r = K.Pieza(cadena, $"Puente{i}_{k}", PrimitiveType.Cylinder, new Vector3(s.x + dx, 1.29f, s.z), new Vector3(0.006f, 0.06f, 0.006f), K.Plano(new Color(0.85f, 1f, 1f)));
            puentes.Add(r.gameObject);
        }
    }

    IEnumerator Completar()
    {
        Completo = true;
        Misiones.AdnReparado = true;
        estado.text = "<color=#39FF88><b>SECUENCIA REPARADA</b> · núcleo de memoria restaurado</color>";
        var pos = nucleo.position;
        Sonidos.Logro(pos);
        luz.enabled = true;
        // Brillo intenso: el holograma se vuelve casi opaco y blanco-cian, la luz sube y baja, el núcleo pasa a dorado.
        for (float t = 0f; t < 2.2f; t += Time.deltaTime)
        {
            float k = Mathf.Sin(Mathf.Clamp01(t / 2.2f) * Mathf.PI);
            for (int i = 0; i < holo.Count; i++)
            {
                var c0 = holoBase[i];
                holo[i].color = Color.Lerp(c0, new Color(0.85f, 1f, 1f, Mathf.Min(1f, c0.a + 0.5f)), k);
            }
            luz.intensity = 4f * k;
            matNucleo.color = Color.Lerp(new Color(0.35f, 0.35f, 0.4f), new Color(1f, 0.8f, 0.25f), Mathf.Clamp01(t / 1.2f));
            nucleo.localScale = Vector3.one * (1f + 0.35f * k);
            yield return null;
        }
        for (int i = 0; i < holo.Count; i++) holo[i].color = holoBase[i] + new Color(0.1f, 0.1f, 0.1f, 0.1f);
        luz.intensity = 0.8f;
        if (chispas != null) { chispas.transform.position = pos; chispas.Emit(150); }

        if (metricas != null)
        {
            metricas.Datos.totalComprension = plantilla.Length;
            metricas.Datos.postAciertos = Mathf.Max(0, plantilla.Length - Errores);
            metricas.Guardar(true);
        }
        var cab = ManosRig.Cabeza;
        var delante = cab != null ? cab.position + cab.forward * 0.9f : pos + Vector3.up * 0.4f;
        Aviso.Mostrar("<b>BAYMAX:</b> ¡Núcleo de memoria restaurado! Ya recuerdo el gen de la espiga S del virus.\n" +
                      "<color=#FFD25A>MISIÓN 3 DESBLOQUEADA: con este gen fabricaremos la vacuna en el LABORATORIO DE VACUNAS.</color>",
                      delante, Aviso.Tipo.Ok, 9f);
        Debug.Log($"[ADN] Secuencia reparada en {Time.time - tInicio:F0} s con {Errores} errores");
        RetoManager.Instancia?.AdnCompletado();
        Partida.Guardar();
    }

    // Partida guardada: la secuencia ya estaba reparada (piezas en su sitio, sin animación).
    public void Restaurar(bool reparado)
    {
        if (!reparado || Completo) return;
        restaurando = true;
        for (int i = 0; i < plantilla.Length; i++)
        {
            char esp = Complemento(plantilla[i]);
            var b = bloques.FirstOrDefault(x => x.letra == esp && x.Encajable.Encajada != null && !sockets.Contains(x.Encajable.Encajada));
            if (b == null) continue;
            b.gameObject.SetActive(true);
            b.Encajable.Encajar(sockets[i], false);
        }
        restaurando = false;
        for (int i = 0; i < plantilla.Length; i++)
        {
            var e = sockets[i].Actual;
            if (e == null) continue;
            colocadas[i] = true;
            e.Bloquear(true);
            if (sockets[i].fantasma != null) sockets[i].fantasma.enabled = false;
            CrearPuentes(i, Puentes(plantilla[i]));
        }
        Completo = true;
        Misiones.AdnReparado = true;
        matNucleo.color = new Color(1f, 0.8f, 0.25f);
        luz.enabled = true; luz.intensity = 0.8f;
        Activar(true);
    }

    // Pruebas sin visor: suelta la base 'letra' en el hueco i (como si la mano la soltara allí).
    public bool Soltar(int i, char letra)
    {
        var b = bloques.FirstOrDefault(x => x.letra == letra && x.gameObject.activeInHierarchy && !sockets.Contains(x.Encajable.Encajada));
        if (b == null || !sockets[i].Libre) return false;
        b.Encajable.Encajar(sockets[i], true);
        return true;
    }
}

// Bloque agarrable de una base nitrogenada (A, T, C o G).
public class BaseADN : MonoBehaviour
{
    public char letra;
    public Encajable Encajable { get; private set; }

    public static BaseADN Crear(Transform padre, char letra, Color color)
    {
        var go = new GameObject("Base_" + letra);
        go.transform.SetParent(padre, false);
        var mat = K.Plano(color);
        K.Caja(go.transform, "Cuerpo", Vector3.zero, new Vector3(0.085f, 0.12f, 0.085f), mat);
        // Muesca: A/T rectangular (2 puentes) y C/G en punta (3 puentes), como en las fichas de clase.
        bool dos = letra == 'A' || letra == 'T';
        bool arriba = letra == 'T' || letra == 'G';   // las de la hebra nueva apuntan hacia el molde
        K.Caja(go.transform, "Muesca", new Vector3(0f, arriba ? 0.07f : -0.07f, 0f), dos ? new Vector3(0.05f, 0.02f, 0.05f) : new Vector3(0.025f, 0.03f, 0.025f), mat);
        foreach (var rot in new[] { 0f, 180f })
        {
            var t = K.Texto(go.transform, $"<b>{letra}</b>", Quaternion.Euler(0f, rot, 0f) * new Vector3(0f, 0f, 0.0435f), 0.45f, Color.white, 0.08f, 0.1f,
                            TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f + rot, 0f));
            t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 220);
        }
        foreach (var col in go.GetComponentsInChildren<Collider>()) Object.Destroy(col);
        var b = go.AddComponent<BaseADN>();
        b.letra = letra;
        AgarreRuntime.Hacer(go, HoloDNA_Manager.TipoSocket, null, Vector3.zero, new Vector3(0.09f, 0.13f, 0.09f), 0.05f);
        b.Encajable = go.GetComponent<Encajable>();
        b.Encajable.radioEncaje = 0.09f;
        return b;
    }
}
