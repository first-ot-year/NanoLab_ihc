using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using A = AgarreRuntime;
using C = ContenidoVacuna;
using K = KitVacuna;
using I = EstiloNanoLab.Icono;
using P = ContenidoVacuna.Pieza;

public enum EtapaVacuna { Intro, Antigeno, Diseno, Sintesis, Encapsulado, Calidad, Neutralizacion, Titulo, Fases, Eficacia, Cierre }

// CASO 01 · "Crea la vacuna" (único reto del Centro de Experimentación). El estudiante crea una vacuna de ARNm contra
// el coronavirus del brote con sus manos: elige el antígeno en el virus, ordena los bloques del ARNm en el molde,
// lo fabrica con la pipeta y el termobloque, lo envuelve en nanopartículas con el mezclador, pasa el control de
// calidad y la cadena de frío, comprueba con un ensayo de neutralización que genera anticuerpos, lee el título y
// termina con las fases clínicas y la eficacia. Una meta visible a la vez (la franja de luz marca la mesa); el error
// explica por qué y se puede reintentar sin penalización; AYUDA enseña el gesto (y da pistas graduales) y todo queda en el informe y en
// las métricas. La estación la monta MontajeVacuna al empezar.
public class RetoVacuna : MonoBehaviour
{
    public static RetoVacuna Instancia { get; private set; }

    // Panel del caso (izquierda) y cabecera con la barra de pasos
    public TMP_Text titulo, enunciado, opcionesTxt, respuesta, estado, cabecera;
    public Renderer[] segmentos;
    public TMP_Text[] etiquetasSegmento;
    // Pantalla de visualización (derecha)
    public TMP_Text tituloVisual, pieVisual;
    public VirusVacuna virus;
    public DiagramaARNm diagramaArnm;
    public DiagramaSintesis sintesis;
    public DiagramaLNP lnp;
    public PanelCalidad calidad;
    public PlacaNeutralizacion placa;
    public LineaFases fases;
    public GraficaEficacia grafica;
    public PanelSiguiente siguiente;   // presentación del paso al pulsar CONTINUAR
    public GameObject cierre;
    public TMP_Text cierreTxt;
    public SelectorVacuna[] selectoresVirus, selectoresColumna;
    // Ayudas para no tener que leer: icono + acción en grande (modo PASO A PASO), clave de colores del paso ANTÍGENO
    // y el virus holográfico del mostrador (se toca y se gira con la mano).
    public SpriteRenderer iconoPaso;
    public TMP_Text accionGrande, leyendaAntigeno;
    public GameObject grupoPasoAPaso, grupoHolograma;
    public BotonFisico pasoAPaso;
    public VirusVacuna holograma;
    public Transform anclaMirar;   // arriba de la pantalla derecha: donde sale "MIRA LAS BARRAS" / "MIRA LA FILA…"
    // Botones
    public BotonFisico[] opciones;
    public GameObject[] cartelesOpcion;
    public TMP_Text[] textosCartel;
    public BotonFisico pista, continuar, repetir;
    public GameObject cartelPista, cartelContinuar, cartelRepetir;
    public BotonFisico incubarTubo, mezclar, incubarPlaca;
    public GameObject grupoIncubarTubo, grupoMezclar, grupoIncubarPlaca;
    // Mostrador
    public Renderer[] franjas;
    public TMP_Text[] rotulosMesa;
    public GameObject grupoDiseno, grupoCartuchos, grupoPlaca;
    public ZonaEncaje[] huecosMolde, huecosLipido, huecosAlmacen;
    public ZonaEncaje huecoTermo, huecoMezclaTubo, salidaVial, huecoAnalizador;
    public PiezaVacuna[] bloques, cartuchos;
    public PiezaVacuna tubo, vial;
    public PipetaVacuna pipeta;
    public PuntoPipeta[] frascos;
    public PuntoPipeta puntoTubo, puntoPlaca;
    public TMP_Text pantallaTermo, pantallaMezclador, pantallaAnalizador;
    public Renderer canalMezcla;
    public Renderer[] pocillos;
    public Bounds zona;

    public EtapaVacuna Actual { get; private set; }
    public bool Resuelta { get; private set; }
    public int Errores { get; private set; }
    public int Pistas { get; private set; }

    const int NumPasos = 9;
    static readonly string[] Letras = { "A", "B", "C", "D", "E" };
    static readonly string[] IdsSintesis = { "molde", "t7", "ntp" };
    // Orden en que se ven los frascos en la gradilla (desordenados, para que no se adivine por la posición).
    static readonly int[] OrdenFrascosSintesis = { 3, 0, 4, 2, 1 };
    static readonly int[] OrdenFrascosEnsayo = { 2, 0, 3, 1 };
    static readonly Color Rojo = new Color(1f, 0.54f, 0.44f);

    readonly Dictionary<EtapaVacuna, int> erroresEtapa = new Dictionary<EtapaVacuna, int>();
    readonly Dictionary<EtapaVacuna, int> nivelPista = new Dictionary<EtapaVacuna, int>();
    readonly List<string> decisiones = new List<string>();
    MetricsLogger metricas;
    int seleccion = -1, progreso;
    string mensaje;
    bool dentro, saludado, guardado, silencio;
    bool incubando, mezclando, analizando, analizado, mezclado, tuboConsumido;
    float altoEnunciado;
    // Guía del paso: plan de acciones (se va agregando), flecha flotante sobre el objeto de la siguiente acción y
    // presentación del paso en la pantalla derecha.
    SubPaso[] plan = new SubPaso[0];
    int indicePlan = -1, planVisible;
    float tPlan, finPresentacion;
    bool presentando;
    VentanaGuia ventana;
    DemoAyuda demo;
    float tVentana;
    int claveGuia = -1;   // PASO A PASO: acción que la animación está enseñando
    bool guiando;
    public bool PasoAPaso { get; private set; }
    string tituloPasoVisual = "", piePasoVisual = "";
    Transform marcador, flecha;
    TMP_Text marcadorTxt;

    // ------------------------------------------------------------------ ciclo de vida

    void Awake() => Instancia = this;
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    void Start()
    {
        for (int i = 0; i < opciones.Length; i++)
        {
            int k = i;
            opciones[i].Pulsado += () => AlPulsarOpcion(k);
        }
        pista.Pulsado += PedirPista;
        continuar.Pulsado += Continuar;
        repetir.Pulsado += Reiniciar;
        incubarTubo.Pulsado += IncubarTubo;
        mezclar.Pulsado += Mezclar;
        incubarPlaca.Pulsado += () => PasoEnsayo(2, incubarPlaca.transform.position + Vector3.up * 0.25f);
        for (int i = 0; i < selectoresVirus.Length; i++) { int k = i; selectoresVirus[i].alActivar = () => ElegirAntigeno(k); }
        if (holograma != null)
        {
            holograma.Colocar(Vector3.zero, holograma.transform.localScale.x, false);   // tamaño del montaje, sin rótulos (la clave va en la pantalla)
            for (int i = 0; i < holograma.selectores.Length; i++)
            {
                int k = i;
                if (holograma.selectores[i] != null) holograma.selectores[i].alActivar = () => ElegirAntigeno(k);
            }
        }
        if (pasoAPaso != null) pasoAPaso.Pulsado += AlternarPasoAPaso;
        // Apartado fijo "AHORA" bajo la pantalla del caso (no sigue la vista), inclinado hacia el usuario.
        ventana = VentanaGuia.Crear(transform, new Vector3(0.64f, 1.012f, -0.738f), Quaternion.Euler(-18f, 0f, 0f) * Quaternion.Euler(0f, 180f, 0f));
        demo = DemoAyuda.Crear(transform);
        demo.pantalla = anclaMirar;
        for (int i = 0; i < selectoresColumna.Length; i++) { int k = i; selectoresColumna[i].alActivar = () => ElegirColumna(k); }
        foreach (var h in huecosMolde) h.Cambio += AlCambiarMolde;
        foreach (var h in huecosLipido) h.Cambio += AlCambiarLipido;
        huecoMezclaTubo.Cambio += _ => AlCambiarEntradaTubo();
        huecoTermo.Cambio += _ => { if (!silencio) { RefrescarMaquinas(); Refrescar(); } };
        huecoAnalizador.Cambio += AlCambiarAnalizador;
        for (int i = 0; i < huecosAlmacen.Length; i++) { int k = i; huecosAlmacen[i].Cambio += e => AlGuardar(k, e); }
        puntoTubo.recibir = AlDispensarTubo;
        puntoPlaca.recibir = AlDispensarPlaca;
        metricas = new MetricsLogger(C.Codigo);
        RecuerdosBaymax.Cambiado += Refrescar;
        PintarPlacaFisica(0);
        RefrescarMaquinas();
        CrearMarcador();
        Poner(EtapaVacuna.Intro, false);
    }

    void OnDisable() => RecuerdosBaymax.Cambiado -= Refrescar;

    void Update()
    {
        var cab = ManosRig.Cabeza;
        bool ahora = cab != null && zona.Contains(new Vector3(cab.position.x, zona.center.y, cab.position.z));
        if (ahora && !dentro && !saludado && Actual == EtapaVacuna.Intro)
        {
            // Primera vez junto a la estación: bienvenida y el cronómetro del caso empieza aquí.
            saludado = true;
            metricas = new MetricsLogger(C.Codigo);
            metricas.Etapa(Actual.ToString());
            var frente = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
            Aviso.Mostrar($"{C.Codigo} · {C.Titulo.ToUpper()}: vas a fabricar una vacuna de ARNm. Pulsa el botón verde CONTINUAR para empezar.",
                cab.position + frente * 0.9f + Vector3.down * 0.15f, Aviso.Tipo.Info, 7f);
            Sonidos.Etapa(cab.position);
        }
        dentro = ahora;

        // Franja de la mesa del paso actual: late suavemente.
        var mesas = MesasDe(Actual);
        float pulso = 0.65f + 0.35f * Mathf.Sin(Time.time * 3f);
        for (int i = 0; i < franjas.Length; i++)
        {
            bool si = System.Array.IndexOf(mesas, i) >= 0;
            K.Pintar(franjas[i], si ? K.Cian * pulso + new Color(0f, 0f, 0f, 1f) : new Color(0.22f, 0.27f, 0.33f));
            rotulosMesa[i].color = si ? K.Cian : K.Gris * 0.8f;
        }
        if (presentando && Time.time > finPresentacion) TerminarPresentacion();
        ActualizarPlan(cab);
        ActualizarVentana();
        GuiarPasoAPaso();
#if UNITY_EDITOR
        DepurarTeclado();
#endif
    }

    // Mesas (índices de MontajeVacuna.Mesas) donde se trabaja en cada paso.
    static int[] MesasDe(EtapaVacuna e)
    {
        switch (e)
        {
            case EtapaVacuna.Diseno: return new[] { 0 };
            case EtapaVacuna.Sintesis: return new[] { 1 };
            case EtapaVacuna.Encapsulado: return new[] { 2 };
            case EtapaVacuna.Calidad: return new[] { 3 };
            case EtapaVacuna.Neutralizacion: return new[] { 0, 1 };
            default: return new int[0];
        }
    }

    // ------------------------------------------------------------------ etapas

    void Poner(EtapaVacuna e, bool conSonido = true)
    {
        Actual = e;
        Resuelta = e == EtapaVacuna.Intro;
        seleccion = -1;
        progreso = 0;
        mensaje = null;
        metricas?.Etapa(e.ToString());
        if (demo != null) demo.Detener();   // el gesto de AYUDA es del paso anterior
        PrepararVisual(e);
        PrepararMesa(e);
        plan = Plan(e);
        indicePlan = -1;
        planVisible = 0;
        tPlan = Time.time;
        bool paso = e >= EtapaVacuna.Antigeno && e <= EtapaVacuna.Eficacia;
        if (paso) Presentar(e); else { presentando = false; siguiente.Ocultar(); }
        Refrescar();
        if (conSonido) Sonidos.Etapa(transform.position + Vector3.up * 1.2f);
        if (conSonido && paso) Banner(e);
    }

    void PrepararVisual(EtapaVacuna e)
    {
        MostrarVisuales(e);
        virus.Resaltar(VirusVacuna.Parte.Ninguna);
        if (holograma != null)
        {
            holograma.Resaltar(VirusVacuna.Parte.Ninguna);
            holograma.Anticuerpos(e == EtapaVacuna.Cierre);
        }
        switch (e)
        {
            case EtapaVacuna.Intro:
            case EtapaVacuna.Antigeno:
                virus.Colocar(Vector3.zero, 1f, true);
                virus.Anticuerpos(false);
                tituloVisual.text = "EL VIRUS DEL BROTE";
                pieVisual.text = e == EtapaVacuna.Intro
                    ? "Coronavirus del brote (modelo). Mide unos 100 nm: mil veces más pequeño que el grosor de un cabello."
                    : C.Antigeno.visual;
                break;
            case EtapaVacuna.Diseno:
                tituloVisual.text = "TU ARN MENSAJERO";
                pieVisual.text = "Lo que colocas en el MOLDE aparece aquí en grande. Al llenar los 5 huecos se comprueba el diseño.";
                MostrarMolde(null, "");
                break;
            case EtapaVacuna.Sintesis:
                tituloVisual.text = "TRANSCRIPCIÓN IN VITRO";
                pieVisual.text = "Copiar ADN en ARN, en un tubo y sin células.";
                break;
            case EtapaVacuna.Encapsulado:
                tituloVisual.text = "NANOPARTÍCULA LIPÍDICA (corte)";
                pieVisual.text = "Cada lípido aparece al encajar su cartucho en el mezclador.";
                ActualizarLnp();
                break;
            case EtapaVacuna.Calidad:
                tituloVisual.text = "CONTROL DE CALIDAD · LOTE 001";
                pieVisual.text = "Si un parámetro sale fuera de especificación, el lote se descarta.";
                break;
            case EtapaVacuna.Neutralizacion:
                placa.Reiniciar();
                placa.MarcarTitulo(false);
                tituloVisual.text = "ENSAYO DE NEUTRALIZACIÓN · VISTA AMPLIADA";
                pieVisual.text = "La placa de tu mesa, ampliada: 3 filas de una placa de 96 pocillos. Cada columna lleva el suero más diluido.";
                break;
            case EtapaVacuna.Titulo:
                tituloVisual.text = "LECTURA DE LA PLACA";
                pieVisual.text = "<color=#B07CFF>VIOLETA</color> = células vivas (virus neutralizado)   ·   <color=#D6DEE8>TRANSPARENTE con X</color> = células destruidas";
                break;
            case EtapaVacuna.Fases:
                fases.Reiniciar();
                tituloVisual.text = "DEL LABORATORIO A LAS PERSONAS";
                pieVisual.text = "Cada tarjeta se rellena al pulsar la etapa correcta.";
                break;
            case EtapaVacuna.Eficacia:
                grafica.Reiniciar();
                grafica.Mostrar();
                tituloVisual.text = "RESULTADOS DE LA FASE III (se abre el ciego)";
                pieVisual.text = C.Eficacia.visual;
                break;
            case EtapaVacuna.Cierre:
                virus.Colocar(new Vector3(0f, 0.12f, 0f), 0.7f, false);
                virus.Anticuerpos(true);
                tituloVisual.text = "RECUERDO RECUPERADO";
                pieVisual.text = "Anticuerpos (verde) pegados a las espigas: el virus ya no puede entrar en las células.";
                break;
        }
        tituloPasoVisual = tituloVisual.text;
    }

    // Qué se ve en la pantalla derecha en cada paso (sin reiniciar nada).
    void MostrarVisuales(EtapaVacuna e)
    {
        bool holo = holograma != null;
        // Con holograma, en ANTÍGENO el virus se trabaja en el mostrador y la pantalla solo muestra la clave de colores.
        virus.gameObject.SetActive(holo ? e == EtapaVacuna.Intro || e == EtapaVacuna.Cierre : e <= EtapaVacuna.Antigeno || e == EtapaVacuna.Cierre);
        if (grupoHolograma != null) grupoHolograma.SetActive(e <= EtapaVacuna.Antigeno || e == EtapaVacuna.Cierre);
        if (leyendaAntigeno != null) leyendaAntigeno.gameObject.SetActive(holo && e == EtapaVacuna.Antigeno);
        diagramaArnm.gameObject.SetActive(e == EtapaVacuna.Diseno);
        sintesis.gameObject.SetActive(e == EtapaVacuna.Sintesis);
        lnp.gameObject.SetActive(e == EtapaVacuna.Encapsulado);
        calidad.gameObject.SetActive(e == EtapaVacuna.Calidad);
        placa.gameObject.SetActive(e == EtapaVacuna.Neutralizacion || e == EtapaVacuna.Titulo);
        fases.gameObject.SetActive(e == EtapaVacuna.Fases);
        grafica.gameObject.SetActive(e == EtapaVacuna.Eficacia);
        cierre.SetActive(e == EtapaVacuna.Cierre);
        foreach (var s in selectoresVirus) s.gameObject.SetActive(e == EtapaVacuna.Antigeno && !holo);
        foreach (var s in selectoresColumna) s.gameObject.SetActive(e == EtapaVacuna.Titulo);
    }

    void OcultarVisuales()
    {
        foreach (var g in new[] { virus.gameObject, diagramaArnm.gameObject, sintesis.gameObject, lnp.gameObject, calidad.gameObject,
                                  placa.gameObject, fases.gameObject, grafica.gameObject, cierre })
            g.SetActive(false);
        if (leyendaAntigeno != null) leyendaAntigeno.gameObject.SetActive(false);
    }

    // Muestra en el mostrador solo lo que se usa en este paso (una meta visible a la vez) y lo deja en su sitio.
    void PrepararMesa(EtapaVacuna e)
    {
        silencio = true;
        bool diseno = e == EtapaVacuna.Diseno || e == EtapaVacuna.Sintesis;
        grupoDiseno.SetActive(diseno);
        foreach (var b in bloques)
        {
            Ver(b, diseno);
            if (diseno) b.Encajable.Bloquear(e != EtapaVacuna.Diseno);
        }

        P[] fuentes = e == EtapaVacuna.Sintesis ? OrdenFrascosSintesis.Select(i => C.ReactivosSintesis[i]).ToArray()
                    : e == EtapaVacuna.Neutralizacion ? OrdenFrascosEnsayo.Select(i => C.ReactivosEnsayo[i]).ToArray() : null;
        for (int i = 0; i < frascos.Length; i++)
        {
            var p = fuentes != null && i < fuentes.Length ? fuentes[i] : null;
            frascos[i].transform.parent.gameObject.SetActive(p != null);
            frascos[i].PonerFuente(p);
        }

        bool conPipeta = e == EtapaVacuna.Sintesis || e == EtapaVacuna.Neutralizacion;
        pipeta.Vaciar();
        if (conPipeta && pipeta.Encajable.Encajada == null) A.Recolocar(pipeta.Encajable);
        pipeta.gameObject.SetActive(conPipeta);

        Ver(tubo, !tuboConsumido && (e == EtapaVacuna.Sintesis || e == EtapaVacuna.Encapsulado));
        grupoCartuchos.SetActive(e == EtapaVacuna.Encapsulado);
        foreach (var c in cartuchos) Ver(c, e == EtapaVacuna.Encapsulado && !(mezclado && EnMezclador(c)));
        Ver(vial, mezclado && e >= EtapaVacuna.Encapsulado);
        if (vial.gameObject.activeSelf) vial.Encajable.Bloquear(e > EtapaVacuna.Calidad);

        grupoPlaca.SetActive(e == EtapaVacuna.Neutralizacion || e == EtapaVacuna.Titulo);
        grupoIncubarTubo.SetActive(e == EtapaVacuna.Sintesis);
        grupoMezclar.SetActive(e == EtapaVacuna.Encapsulado);
        grupoIncubarPlaca.SetActive(e == EtapaVacuna.Neutralizacion);
        RefrescarMaquinas();
        silencio = false;
    }

    static void Ver(PiezaVacuna p, bool si)
    {
        if (p == null) return;
        if (!si) { if (p.gameObject.activeSelf) p.gameObject.SetActive(false); return; }
        if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
        if (p.Encajable.Encajada == null) A.Recolocar(p.Encajable);
    }

    // ------------------------------------------------------------------ 1 · antígeno

    public void ElegirAntigeno(int i)
    {
        if (Actual != EtapaVacuna.Antigeno) return;
        TerminarPresentacion();
        var p = C.Antigeno;
        var o = p.opciones[i];
        seleccion = i;
        var pos = holograma != null ? holograma.Centro + Vector3.up * 0.3f : selectoresVirus[i].transform.position + Vector3.up * 0.12f;
        virus.Resaltar((VirusVacuna.Parte)i);   // se aprende viendo: también las incorrectas se iluminan
        if (holograma != null) holograma.Resaltar((VirusVacuna.Parte)i);
        if (o.correcta)
        {
            if (!Resuelta)
            {
                Resuelta = true;
                Registrar($"Antígeno: {o.corto}", true);
                Sonidos.Logro(pos);
                ManosRig.Vibrar(pos, 0.5f, 0.12f);
                virus.Anticuerpos(true);
                if (holograma != null) holograma.Anticuerpos(true);
                pieVisual.text = "Anticuerpos neutralizantes (verde) bloqueando las espigas: el virus ya no puede unirse a nuestras células.";
            }
            mensaje = $"<color=#39FF88><b>CORRECTO.</b></color> {o.respuesta}\n<color=#8FD8FF>{p.concepto}</color>";
        }
        else
        {
            if (!Resuelta) Fallo($"Antígeno: {o.corto}", pos); else Sonidos.Clic(pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {o.respuesta}" + (Resuelta ? "\n<color=#A9BED2>(Ya resolviste este paso: pulsa CONTINUAR.)</color>" : "\n<color=#A9BED2>Toca otra parte.</color>");
        }
        Refrescar();
    }

    // ------------------------------------------------------------------ 2 · diseño del ARNm

    string[] IdsMolde() => huecosMolde.Select(h => h.Actual != null ? h.Actual.GetComponent<PiezaVacuna>()?.Id : null).ToArray();

    void MostrarMolde(bool?[] ok, string texto) => diagramaArnm.Mostrar(IdsMolde(), ok, texto);

    void AlCambiarMolde(Encajable e)
    {
        if (silencio || Actual != EtapaVacuna.Diseno || Resuelta) return;
        TerminarPresentacion();
        var ids = IdsMolde();
        int n = ids.Count(x => x != null);
        var p = e != null ? e.GetComponent<PiezaVacuna>() : null;
        string texto = p != null ? p.datos.explica : "";
        if (n < ids.Length)
        {
            MostrarMolde(null, texto);
            mensaje = n == 0 ? null : $"<b>Bloques en el molde: {n}/5.</b>" + (p != null ? $"\n<color=#8FD8FF>{texto}</color>" : "");
            if (p != null) Sonidos.Encaje(e.transform.position);
            Refrescar();
            return;
        }

        // Molde lleno: se comprueba el diseño.
        var ok = ids.Select((id, i) => (bool?)(id == C.OrdenDiseno[i])).ToArray();
        var pos = huecosMolde[2].transform.position + Vector3.up * 0.2f;
        string orden = string.Join(" > ", ids.Select(id => System.Array.Find(C.Bloques, b => b.id == id).nombre));
        if (ok.All(x => x == true))
        {
            Resuelta = true;
            Registrar("Diseño del ARNm: " + orden, true);
            Sonidos.Logro(pos);
            ManosRig.Vibrar(pos, 0.5f, 0.12f);
            foreach (var b in bloques) b.Encajable.Bloquear(true);
            MostrarMolde(ok, "<color=#39FF88>CAP 5' > UTR 5' > GEN S-2P > UTR 3' > COLA POLI(A)</color>\nTambién lleva una U modificada (N1-metilpseudouridina) para que la célula no lo destruya como si fuera un virus.");
            mensaje = C.DisenoHecho;
        }
        else
        {
            string porque = C.ErrorDiseno(ids, out _);
            Fallo("Diseño: " + orden, pos);
            MostrarMolde(ok, $"<color=#FF8A70>{porque}</color>");
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {porque}\n<color=#A9BED2>Saca los bloques marcados con X (pantalla derecha) y prueba otra vez.</color>";
            Aviso.Mostrar("Revisa los bloques marcados con X", pos, Aviso.Tipo.Error, 3f);
        }
        Refrescar();
    }

    // ------------------------------------------------------------------ 3 · síntesis

    int IngredientesTubo() => IdsSintesis.Count(id => tubo.contenido.Contains(id));

    bool AlDispensarTubo(P p, Vector3 pos)
    {
        var aviso = pos + Vector3.up * 0.15f;
        if (Actual != EtapaVacuna.Sintesis) { Aviso.Mostrar("Ahora no hace falta añadir nada al tubo.", aviso, Aviso.Tipo.Info, 2.5f); return false; }
        if (Resuelta || incubando) { Aviso.Mostrar("El ARNm ya se está fabricando.", aviso, Aviso.Tipo.Info, 2.5f); return false; }
        TerminarPresentacion();
        if (!p.correcta)
        {
            Fallo($"Síntesis: {p.nombre} en el tubo", aviso);
            mensaje = $"<color=#FF8A70><b>ALTO.</b></color> {p.explica}\n<color=#A9BED2>Ese reactivo va a residuos y el tubo sigue bien. Prueba con otro frasco.</color>";
            Aviso.Mostrar($"{p.nombre} no va en esta reacción", aviso, Aviso.Tipo.Error, 3f);
            Refrescar();
            return true;
        }
        if (tubo.contenido.Contains(p.id)) { Aviso.Mostrar($"{p.nombre} ya está en el tubo.", aviso, Aviso.Tipo.Info, 2.5f); return true; }

        tubo.contenido.Add(p.id);
        int n = IngredientesTubo();
        sintesis.Marcar(p.id, true);
        sintesis.Estado($"Tubo de reacción: {n}/3 ingredientes");
        tubo.Liquido(Color.Lerp(new Color(0.85f, 0.92f, 1f), p.color, 0.4f), n / 3f * 0.55f);
        tubo.etiqueta.text = $"{n}/3";
        Sonidos.Carga(pos);
        mensaje = $"<color=#39FF88><b>AÑADIDO · {n}/3.</b></color> {p.explica}" +
                  (n == 3 ? "\n<color=#8FD8FF>Tubo completo: llévalo al TERMOBLOQUE y pulsa INCUBAR.</color>" : "");
        RefrescarMaquinas();
        Refrescar();
        return true;
    }

    public void IncubarTubo()
    {
        if (Actual != EtapaVacuna.Sintesis) return;
        TerminarPresentacion();
        var pos = incubarTubo.transform.position + Vector3.up * 0.25f;
        if (Resuelta || incubando) { Aviso.Mostrar("El tubo ya está incubado.", pos, Aviso.Tipo.Info, 2.5f); return; }
        if (huecoTermo.Actual == null || huecoTermo.Actual != tubo.Encajable)
        {
            Aviso.Mostrar("Primero pon el TUBO DE REACCIÓN en el pocillo del termobloque.", pos, Aviso.Tipo.Info, 3f);
            Sonidos.Error(pos);
            return;
        }
        var falta = IdsSintesis.FirstOrDefault(id => !tubo.contenido.Contains(id));
        if (falta != null)
        {
            Fallo($"Síntesis: incubar con {IngredientesTubo()}/3 ingredientes", pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {C.FaltaSintesis(falta)}";
            Refrescar();
            return;
        }
        incubando = true;
        tubo.Encajable.Bloquear(true);
        pantallaTermo.text = "INCUBANDO\n37 °C · 2 h";
        mensaje = "Incubando a 37 °C: la T7 recorre el molde una y otra vez…";
        Refrescar();
        sintesis.Incubar(() =>
        {
            incubando = false;
            tubo.contenido.Add("arnm");
            tubo.Liquido(new Color(1f, 0.6f, 0.15f), 0.55f);
            tubo.etiqueta.text = "ARNm";
            tubo.Encajable.Bloquear(false);
            Resuelta = true;
            Registrar("ARNm fabricado (transcripción in vitro)", true);
            Sonidos.Logro(pos);
            mensaje = C.SintesisHecho;
            RefrescarMaquinas();
            Refrescar();
        });
    }

    // ------------------------------------------------------------------ 4 · nanopartículas

    bool EnMezclador(PiezaVacuna c) => c != null && c.Encajable != null && huecosLipido.Contains(c.Encajable.Encajada);
    int LipidosCorrectos() => huecosLipido.Count(h => h.Actual != null && h.Actual.GetComponent<PiezaVacuna>() is PiezaVacuna p && p.datos.correcta);
    bool TuboEnMezclador() => huecoMezclaTubo.Actual != null && huecoMezclaTubo.Actual == tubo.Encajable;

    void ActualizarLnp()
    {
        foreach (var l in C.Lipidos)
            if (l.correcta) lnp.Lipido(l.id, huecosLipido.Any(h => h.Actual != null && h.Actual.GetComponent<PiezaVacuna>()?.Id == l.id));
        lnp.Arnm(TuboEnMezclador());
    }

    void AlCambiarLipido(Encajable e)
    {
        if (silencio || Actual != EtapaVacuna.Encapsulado || mezclando || mezclado) return;
        TerminarPresentacion();
        var p = e != null ? e.GetComponent<PiezaVacuna>() : null;
        if (p != null)
        {
            var pos = e.transform.position + Vector3.up * 0.2f;
            if (!p.datos.correcta)
            {
                Fallo($"Nanopartículas: {p.datos.nombre} en el mezclador", pos);
                mensaje = $"<color=#FF8A70><b>ALTO.</b></color> {p.datos.explica}";
                Aviso.Mostrar($"{p.datos.nombre}: no va en el mezclador", pos, Aviso.Tipo.Error, 3f);
                StartCoroutine(Devolver(e, 1.3f));
            }
            else
            {
                Sonidos.Carga(pos);
                mensaje = $"<color=#39FF88><b>{p.datos.nombre} EN EL MEZCLADOR · {LipidosCorrectos()}/4.</b></color> {p.datos.explica}";
            }
        }
        ActualizarLnp();
        RefrescarMaquinas();
        Refrescar();
    }

    IEnumerator Devolver(Encajable e, float espera)
    {
        yield return new WaitForSeconds(espera);
        if (e != null && e.Encajada != null && huecosLipido.Contains(e.Encajada)) e.VolverACasa();
    }

    void AlCambiarEntradaTubo()
    {
        if (silencio || Actual != EtapaVacuna.Encapsulado || mezclando || mezclado) return;
        if (TuboEnMezclador())
        {
            Sonidos.Encaje(huecoMezclaTubo.transform.position);
            mensaje = "<color=#39FF88><b>ARNm EN LA ENTRADA ACUOSA.</b></color> En el mezclador se disuelve en un tampón ácido (pH 4): así el lípido ionizable se carga positivamente y lo atrapa.";
        }
        ActualizarLnp();
        RefrescarMaquinas();
        Refrescar();
    }

    public void Mezclar()
    {
        if (Actual != EtapaVacuna.Encapsulado) return;
        TerminarPresentacion();
        var pos = mezclar.transform.position + Vector3.up * 0.25f;
        if (mezclado || mezclando) { Aviso.Mostrar("La vacuna ya está formulada.", pos, Aviso.Tipo.Info, 2.5f); return; }
        int n = LipidosCorrectos();
        if (n < 4)
        {
            Fallo($"Nanopartículas: mezclar con {n}/4 lípidos", pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> Hay {n}/4 lípidos en el mezclador. La nanopartícula necesita los cuatro: cada uno cumple una función (atrapar el ARNm, formar la pared, dar estabilidad y evitar que se peguen).";
            Refrescar();
            return;
        }
        if (!TuboEnMezclador())
        {
            Aviso.Mostrar("Coloca el TUBO con tu ARNm en la entrada de fase acuosa (ARNm) del mezclador.", pos, Aviso.Tipo.Info, 3f);
            Sonidos.Error(pos);
            return;
        }
        mezclando = true;
        foreach (var h in huecosLipido) h.Actual?.Bloquear(true);
        tubo.Encajable.Bloquear(true);
        pantallaMezclador.text = "MEZCLANDO…\nflujo 12 mL/min";
        mensaje = "Mezclando: las dos corrientes se encuentran en el chip y los lípidos se autoensamblan alrededor del ARNm…";
        StartCoroutine(AnimarCanal());
        Refrescar();
        lnp.Mezclar(() =>
        {
            mezclando = false;
            mezclado = true;
            silencio = true;
            foreach (var h in huecosLipido) if (h.Actual != null) h.Actual.gameObject.SetActive(false);   // consumidos
            tubo.gameObject.SetActive(false);
            tuboConsumido = true;
            Ver(vial, true);
            vial.Encajable.Bloquear(false);
            silencio = false;
            Resuelta = true;
            Registrar("Vacuna formulada en nanopartículas lipídicas", true);
            Sonidos.Logro(pos);
            Aviso.Mostrar("Vial de la vacuna listo en la SALIDA", vial.transform.position + Vector3.up * 0.15f, Aviso.Tipo.Ok, 3f);
            mensaje = C.EncapsuladoHecho;
            RefrescarMaquinas();
            Refrescar();
        });
    }

    IEnumerator AnimarCanal()
    {
        var c = new Color(0.4f, 0.9f, 1f);
        while (mezclando)
        {
            K.Pintar(canalMezcla, Color.Lerp(c, new Color(1f, 0.95f, 0.85f), 0.5f + 0.5f * Mathf.Sin(Time.time * 12f)));
            yield return null;
        }
        K.Pintar(canalMezcla, new Color(0.95f, 0.95f, 0.9f));
    }

    // ------------------------------------------------------------------ 5 · calidad y cadena de frío

    void AlCambiarAnalizador(Encajable e)
    {
        if (silencio || Actual != EtapaVacuna.Calidad || e == null || e != vial.Encajable || analizado || analizando) return;
        TerminarPresentacion();
        analizando = true;
        vial.Encajable.Bloquear(true);
        pantallaAnalizador.text = "ANALIZANDO…";
        mensaje = "Analizando el lote: tamaño de partícula, ARNm encapsulado, integridad y esterilidad…";
        Sonidos.Encaje(e.transform.position);
        Refrescar();
        calidad.Analizar(() =>
        {
            analizando = false;
            analizado = true;
            vial.Encajable.Bloquear(false);
            vial.Encajable.casa = huecoAnalizador;   // si se suelta mal, vuelve al analizador
            pantallaAnalizador.text = "LOTE 001\n<color=#39FF88>APROBADO</color>";
            Registrar("Control de calidad: lote aprobado", true);
            Sonidos.Carga(huecoAnalizador.transform.position);
            mensaje = "<color=#39FF88><b>LOTE APROBADO.</b></color> Los 4 parámetros están dentro de especificación.\n<color=#FFD25A>Ahora guárdalo: ULTRACONGELADOR, NEVERA o MESA.</color>";
            Refrescar();
        });
    }

    void AlGuardar(int i, Encajable e)
    {
        if (silencio || Actual != EtapaVacuna.Calidad || e == null || e != vial.Encajable || Resuelta) return;
        var a = C.Almacenes[i];
        var pos = huecosAlmacen[i].transform.position + Vector3.up * 0.25f;
        if (!analizado)
        {
            Aviso.Mostrar("Primero el CONTROL DE CALIDAD: ningún lote se guarda sin analizarlo.", pos, Aviso.Tipo.Error, 3f);
            Sonidos.Error(pos);
            mensaje = "<color=#FFD25A><b>PRIMERO EL ANALIZADOR.</b></color> Ningún lote se guarda ni se libera sin pasar el control de calidad.";
            StartCoroutine(DevolverVial(1.2f));
            Refrescar();
            return;
        }
        if (a.correcta)
        {
            Resuelta = true;
            vial.Encajable.casa = huecosAlmacen[i];
            vial.Encajable.Bloquear(true);
            Registrar($"Cadena de frío: {a.nombre} ({a.sub})", true);
            Sonidos.Logro(pos);
            ManosRig.Vibrar(pos, 0.5f, 0.12f);
            calidad.Estado("<color=#39FF88>LOTE 001 · APROBADO Y GUARDADO A -70 °C</color>");
            mensaje = $"{C.CalidadHecho}\n<color=#8FD8FF>{a.explica}</color>";
        }
        else
        {
            Fallo($"Cadena de frío: {a.nombre} ({a.sub})", pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {a.explica}";
            Aviso.Mostrar($"{a.nombre} ({a.sub}): no es el sitio", pos, Aviso.Tipo.Error, 3f);
            StartCoroutine(DevolverVial(1.5f));
        }
        Refrescar();
    }

    IEnumerator DevolverVial(float espera)
    {
        yield return new WaitForSeconds(espera);
        var z = vial.Encajable.Encajada;
        if (Resuelta || z == null || z == vial.Encajable.casa) yield break;
        // Si está dentro de un armario, la puerta se abre y el vial sale antes de volver a su sitio.
        var armario = z.GetComponentInParent<AlmacenFrio>();
        if (armario != null)
            yield return armario.Expulsar(() => { if (!Resuelta && vial.Encajable.Encajada == z) vial.Encajable.VolverACasa(); });
        else vial.Encajable.VolverACasa();
    }

    // ------------------------------------------------------------------ 6 · ensayo de neutralización

    bool AlDispensarPlaca(P p, Vector3 pos)
    {
        var aviso = pos + Vector3.up * 0.15f;
        if (Actual != EtapaVacuna.Neutralizacion) { Aviso.Mostrar("Ahora no hace falta añadir nada a la placa.", aviso, Aviso.Tipo.Info, 2.5f); return false; }
        if (placa.Animando) { Aviso.Mostrar("Espera a que termine el paso anterior.", aviso, Aviso.Tipo.Info, 2f); return false; }
        int paso = C.PasoDeReactivo(p.id);
        if (paso < 0) { Aviso.Mostrar("Ese reactivo no es de este ensayo.", aviso, Aviso.Tipo.Info, 2.5f); return true; }
        PasoEnsayo(paso, aviso);
        return true;
    }

    public void PasoEnsayo(int paso, Vector3 pos)
    {
        if (Actual != EtapaVacuna.Neutralizacion) return;
        TerminarPresentacion();
        var pasos = C.Neutralizacion;
        if (placa.Animando) { Aviso.Mostrar("Espera a que termine el paso anterior.", pos, Aviso.Tipo.Info, 2f); return; }
        if (Resuelta) { mensaje = "<color=#A9BED2>El ensayo ya está completo: pulsa CONTINUAR (verde).</color>"; Sonidos.Clic(pos); Refrescar(); return; }
        if (paso < progreso)
        {
            mensaje = $"<color=#A9BED2>{pasos[paso].boton} ya está hecho.</color>";
            Sonidos.Clic(pos);
            Refrescar();
            return;
        }
        if (paso == progreso)
        {
            progreso++;
            placa.Aplicar(paso);
            PintarPlacaFisica(progreso);
            mensaje = $"<color=#39FF88><b>BIEN · PASO {progreso}/{pasos.Length}.</b></color> {pasos[paso].hecho}";
            Sonidos.Carga(pos);
            if (progreso == pasos.Length)
            {
                Resuelta = true;
                Registrar("Ensayo de neutralización completo", true);
                mensaje += "\n<color=#8FD8FF>¡Placa lista! Pulsa CONTINUAR para leerla.</color>";
            }
        }
        else
        {
            string porque = C.ErrorNeutralizacion(progreso, paso, out bool arruina);
            Fallo($"Ensayo: {pasos[paso].boton} en lugar de {pasos[progreso].boton}", pos);
            if (arruina)
            {
                progreso = 0;
                placa.Reiniciar();
                PintarPlacaFisica(0);
            }
            mensaje = $"<color=#FF8A70><b>{(arruina ? "ALTO." : "AÚN NO.")}</b></color> {porque}";
        }
        Refrescar();
    }

    // La placa de la mesa (pequeña) sigue a grandes rasgos lo que muestra la vista ampliada.
    void PintarPlacaFisica(int pasos)
    {
        int nc = PlacaNeutralizacion.Columnas;
        for (int f = 0; f < PlacaNeutralizacion.Filas; f++)
            for (int c = 0; c < nc; c++)
            {
                Color col;
                if (pasos <= 0) col = new Color(0.78f, 0.81f, 0.85f);
                else if (pasos >= 5) col = PlacaNeutralizacion.Vive(f, c) ? new Color(0.5f, 0.22f, 0.82f) : new Color(0.9f, 0.92f, 0.95f);
                else if (pasos >= 4) col = new Color(0.95f, 0.5f, 0.58f);
                else if (f == 2) col = new Color(0.95f, 0.72f, 0.75f);
                else if (pasos >= 2) col = Color.Lerp(new Color(1f, 0.88f, 0.55f), new Color(1f, 0.35f, 0.3f), 0.45f);
                else col = Color.Lerp(new Color(1f, 0.9f, 0.55f), new Color(0.95f, 0.72f, 0.75f), c / (nc - 1f) * 0.8f);
                K.Pintar(pocillos[f * nc + c], col);
            }
    }

    // ------------------------------------------------------------------ 7 · título

    public void ElegirColumna(int c)
    {
        if (Actual != EtapaVacuna.Titulo) return;
        TerminarPresentacion();
        string r = C.RespuestaColumna(c, out bool ok);
        var pos = selectoresColumna[c].transform.position + Vector3.up * 0.1f;
        if (ok)
        {
            if (!Resuelta)
            {
                Resuelta = true;
                Registrar($"Título: {PlacaNeutralizacion.Diluciones[c]}", true);
                Sonidos.Logro(pos);
                ManosRig.Vibrar(pos, 0.5f, 0.12f);
                placa.MarcarTitulo(true);
            }
            mensaje = $"<color=#39FF88><b>CORRECTO.</b></color> {r}\n<color=#8FD8FF>{C.TituloConcepto}</color>";
        }
        else
        {
            if (!Resuelta) Fallo($"Título: {PlacaNeutralizacion.Diluciones[c]}", pos); else Sonidos.Clic(pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {r}";
        }
        Refrescar();
    }

    // ------------------------------------------------------------------ 8 · fases y 9 · eficacia (botones)

    void AlPulsarOpcion(int boton)
    {
        TerminarPresentacion();
        if (Actual == EtapaVacuna.Fases) PasoFases(boton);
        else if (Actual == EtapaVacuna.Eficacia) ResponderEficacia(boton);
    }

    void PasoFases(int boton)
    {
        var pasos = C.Fases;
        int paso = C.OrdenBotonesFases[boton];
        var pos = opciones[boton].transform.position + Vector3.up * 0.3f;
        if (Resuelta) { mensaje = "<color=#A9BED2>Ya están ordenadas: pulsa CONTINUAR (verde).</color>"; Sonidos.Clic(pos); Refrescar(); return; }
        if (paso < progreso) { mensaje = $"<color=#A9BED2>{pasos[paso].boton} ya está colocada.</color>"; Sonidos.Clic(pos); Refrescar(); return; }
        if (paso == progreso)
        {
            progreso++;
            fases.Rellenar(paso);
            mensaje = $"<color=#39FF88><b>BIEN · {progreso}/{pasos.Length}.</b></color> {pasos[paso].hecho}";
            Sonidos.Carga(pos);
            if (progreso == pasos.Length)
            {
                Resuelta = true;
                Registrar("Fases clínicas ordenadas", true);
                mensaje += "\n<color=#8FD8FF>De pocas a millones de personas: primero seguridad y después eficacia. Pulsa CONTINUAR.</color>";
            }
        }
        else
        {
            Fallo($"Fases: {pasos[paso].boton} en lugar de {pasos[progreso].boton}", pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {C.ErrorFases(progreso, paso)}";
        }
        Refrescar();
    }

    // Pregunta de EFICACIA que se está respondiendo (progreso = preguntas ya acertadas).
    ContenidoVacuna.Pregunta PreguntaEficacia() => C.PreguntasEficacia[Mathf.Min(progreso, C.PreguntasEficacia.Length - 1)];

    // EFICACIA: 4 preguntas sencillas sobre la gráfica. Cada acierto revela algo en la pantalla y pasa a la siguiente.
    void ResponderEficacia(int i)
    {
        var qs = C.PreguntasEficacia;
        var pos = opciones[Mathf.Clamp(i, 0, opciones.Length - 1)].transform.position + Vector3.up * 0.3f;
        if (Resuelta) { mensaje = "<color=#A9BED2>Ya respondiste todo: pulsa CONTINUAR (verde).</color>"; Sonidos.Clic(pos); Refrescar(); return; }
        var q = qs[progreso];
        if (i < 0 || i >= q.opciones.Length) return;
        var o = q.opciones[i];
        seleccion = i;
        if (o.correcta)
        {
            Registrar($"Eficacia {progreso + 1}/{qs.Length}: {o.corto}", true);
            grafica.Revelar(progreso + 1);
            progreso++;
            seleccion = -1;
            nivelPista[Actual] = 0;   // las ayudas vuelven a empezar con la nueva pregunta
            if (progreso == qs.Length)
            {
                Resuelta = true;
                Sonidos.Logro(pos);
                ManosRig.Vibrar(pos, 0.5f, 0.12f);
                mensaje = $"<color=#39FF88><b>¡BIEN! {qs.Length}/{qs.Length}.</b></color> {o.respuesta}\n<color=#8FD8FF>{C.Eficacia.concepto}</color>";
            }
            else
            {
                Sonidos.Carga(pos);
                mensaje = $"<color=#39FF88><b>¡BIEN! {progreso}/{qs.Length}.</b></color> {o.respuesta}\n<color=#FFD25A>Siguiente: {qs[progreso].enunciado}</color>";
            }
        }
        else
        {
            Fallo($"Eficacia {progreso + 1}/{qs.Length}: {o.corto}", pos);
            mensaje = $"<color=#FF8A70><b>AÚN NO.</b></color> {o.respuesta}\n<color=#A9BED2>Mira otra vez las barras y prueba otra letra.</color>";
        }
        Refrescar();
    }

    // ------------------------------------------------------------------ guía del paso (plan, flecha y presentación)

    class SubPaso
    {
        public System.Func<string> texto;
        public System.Func<Transform> objetivo;   // sobre qué objeto va la flecha (null = la acción es en la pantalla)
        public System.Func<bool> hecho;
        public I icono;                                  // botón del mando que se usa (ventana flotante y PASO A PASO)
        public System.Func<Transform> desde, hasta;      // gesto que enseña AYUDA (de dónde a dónde)
        public System.Func<string> nino, txtDesde, txtHasta;   // PASO A PASO: frase sencilla y rótulos de la animación
        public SubPaso Con(I i, System.Func<Transform> d = null, System.Func<Transform> h = null) { icono = i; desde = d; hasta = h; return this; }
        public SubPaso Nino(string frase, string d = null, string h = null) =>
            Nino(() => frase, d != null ? () => d : (System.Func<string>)null, h != null ? () => h : (System.Func<string>)null);
        public SubPaso Nino(System.Func<string> frase, System.Func<string> d, System.Func<string> h) { nino = frase; txtDesde = d; txtHasta = h; return this; }
        public string Sencillo() => nino != null ? nino() : texto();
        public SubPaso(string t, System.Func<Transform> o, System.Func<bool> h) : this(() => t, o, h) { }
        public SubPaso(System.Func<string> t, System.Func<Transform> o, System.Func<bool> h) { texto = t; objetivo = o; hecho = h; }
    }

    static Transform T(Component c) => c != null && c.gameObject.activeInHierarchy ? c.transform : null;
    // Para las acciones de "mirar la pantalla": el rótulo sale arriba dentro de la pantalla derecha (si lo que hay
    // que mirar está visible), no en medio de la gráfica.
    Transform Mirar(Component visual) => T(visual) != null ? (anclaMirar != null ? anclaMirar : visual.transform) : null;
    bool EnMolde(PiezaVacuna b) => b != null && huecosMolde.Contains(b.Encajable.Encajada);

    // Acciones del paso, en orden. La primera sin hacer es la actual: la flecha la señala y el plan la resalta.
    SubPaso[] Plan(EtapaVacuna e)
    {
        var l = new List<SubPaso>();
        System.Func<Transform> holo = () => holograma != null ? T(holograma) : T(virus);
        if (PasoAPaso && PlanGuiado(e, l))
        {
            l.Add(new SubPaso("Pulsa CONTINUAR (verde) para seguir", () => T(continuar), () => false).Con(I.Pulsar, () => T(continuar))
                .Nino("¡Muy bien! PULSA el botón verde CONTINUAR", "PULSA CONTINUAR"));
            return l.ToArray();
        }
        switch (e)
        {
            case EtapaVacuna.Antigeno:
                l.Add(new SubPaso("Toca una parte del VIRUS HOLOGRÁFICO del mostrador (mano o rayo + gatillo)", holo, () => seleccion >= 0 || Resuelta).Con(I.Pulsar, holo)
                    .Nino("TOCA el virus con la mano", "TÓCALO"));
                l.Add(new SubPaso("Elige la parte que el virus usa para ENTRAR en la célula. Grip junto al virus = girarlo", holo, () => Resuelta).Con(I.Grip, holo)
                    .Nino("Busca la parte de AFUERA que el virus usa para entrar. GIRA el virus con el grip", "GÍRALO CON EL GRIP"));
                break;
            case EtapaVacuna.Diseno:
                System.Func<Transform> libre = () => T(bloques.FirstOrDefault(b => !EnMolde(b)));
                l.Add(new SubPaso("Agarra un bloque de la BANDEJA (grip)", libre,
                    () => bloques.Any(b => b.Encajable.Agarrado || EnMolde(b)) || Resuelta).Con(I.Grip, libre, () => T(huecosMolde[0]))
                    .Nino("COGE un bloque con el grip", "COGE ESTE BLOQUE", "SUÉLTALO EN EL HUECO 1"));
                l.Add(new SubPaso("Suéltalo sobre el HUECO 1 del molde (extremo 5')", () => T(huecosMolde[0]), () => huecosMolde[0].Actual != null || Resuelta)
                    .Con(I.Grip, libre, () => T(huecosMolde[0])).Nino("PONLO en el hueco 1 y suelta el grip", "COGE ESTE BLOQUE", "SUÉLTALO AQUÍ"));
                l.Add(new SubPaso(() => $"Llena los 5 huecos en orden, de 5' a 3' ({IdsMolde().Count(x => x != null)}/5)",
                    () => T(huecosMolde.FirstOrDefault(h => h.Libre)), () => IdsMolde().All(x => x != null) || Resuelta)
                    .Con(I.Grip, libre, () => T(huecosMolde.FirstOrDefault(h => h.Libre)))
                    .Nino("Pon un bloque en cada hueco, uno detrás de otro", "COGE OTRO", "AL SIGUIENTE HUECO"));
                l.Add(new SubPaso("Si la pantalla marca X, cambia ese bloque por otro", () => T(huecosMolde[2]), () => Resuelta)
                    .Con(I.Grip, () => T(huecosMolde[2]), libre).Nino("¿Ves una X? SACA ese bloque y prueba otro", "SÁCALO", "PRUEBA OTRO"));
                break;
            case EtapaVacuna.Sintesis:
                l.Add(new SubPaso("Agarra la PIPETA de su soporte (grip)", () => T(pipeta),
                    () => pipeta.Encajable.Agarrado || pipeta.Contenido != null || IngredientesTubo() > 0 || Resuelta).Con(I.Grip, () => T(pipeta))
                    .Nino("COGE la pipeta con el grip", "COGE LA PIPETA"));
                l.Add(new SubPaso("Carga un reactivo: punta en un FRASCO + gatillo", () => T(frascos[2]), () => pipeta.Contenido != null || IngredientesTubo() > 0 || Resuelta)
                    .Con(I.Gatillo, () => T(pipeta), () => T(frascos[0])).Nino("Mete la punta en un FRASCO y aprieta el GATILLO", "LA PIPETA", "PUNTA AQUÍ + GATILLO"));
                l.Add(new SubPaso(() => $"Descárgalo en el TUBO DE REACCIÓN + gatillo. Solo 3 sirven ({IngredientesTubo()}/3)", () => T(tubo), () => IngredientesTubo() == 3 || Resuelta)
                    .Con(I.Gatillo, () => T(frascos[0]), () => T(tubo)).Nino("Lleva la pipeta al TUBO y aprieta el GATILLO. Hazlo con 3 frascos", "DEL FRASCO…", "AL TUBO + GATILLO"));
                l.Add(new SubPaso("Lleva el TUBO al pocillo del TERMOBLOQUE", () => T(huecoTermo), () => huecoTermo.Actual == tubo.Encajable || incubando || Resuelta)
                    .Con(I.Grip, () => T(tubo), () => T(huecoTermo)).Nino("COGE el tubo y PONLO en el termobloque", "COGE EL TUBO", "PONLO AQUÍ"));
                l.Add(new SubPaso("Pulsa INCUBAR 37 °C", () => T(incubarTubo), () => incubando || Resuelta).Con(I.Pulsar, () => T(incubarTubo))
                    .Nino("PULSA el botón INCUBAR", "PULSA INCUBAR"));
                break;
            case EtapaVacuna.Encapsulado:
                System.Func<Transform> cartucho = () => T(cartuchos.FirstOrDefault(c => !EnMezclador(c)));
                l.Add(new SubPaso("Lleva un CARTUCHO a una entrada de LÍPIDOS del mezclador", cartucho, () => LipidosCorrectos() > 0 || mezclado)
                    .Con(I.Grip, cartucho, () => T(huecosLipido.FirstOrDefault(h => h.Libre))).Nino("COGE un cartucho y PONLO en una entrada", "COGE UN CARTUCHO", "PONLO AQUÍ"));
                l.Add(new SubPaso(() => $"Completa las 4 entradas. Sobra un cartucho ({(mezclado ? 4 : LipidosCorrectos())}/4)",
                    () => T(huecosLipido.FirstOrDefault(h => h.Libre)), () => LipidosCorrectos() == 4 || mezclado)
                    .Con(I.Grip, cartucho, () => T(huecosLipido.FirstOrDefault(h => h.Libre))).Nino("Llena las 4 entradas. Uno de los cartuchos sobra", "OTRO CARTUCHO", "A LA ENTRADA LIBRE"));
                l.Add(new SubPaso("Pon el TUBO con tu ARNm en la entrada ARNm", () => T(huecoMezclaTubo), () => TuboEnMezclador() || mezclado)
                    .Con(I.Grip, () => T(tubo), () => T(huecoMezclaTubo)).Nino("COGE el tubo y PONLO en la entrada ARNm", "COGE EL TUBO", "PONLO AQUÍ"));
                l.Add(new SubPaso("Pulsa MEZCLAR", () => T(mezclar), () => mezclando || mezclado).Con(I.Pulsar, () => T(mezclar))
                    .Nino("PULSA el botón MEZCLAR", "PULSA MEZCLAR"));
                break;
            case EtapaVacuna.Calidad:
                // El grupo de los tres almacenes (no uno concreto: la guía no da la respuesta).
                System.Func<Transform> almacenes = () => { var h = T(huecosAlmacen[0]); return h != null && h.parent != null ? h.parent.parent : null; };
                l.Add(new SubPaso("Toma el VIAL de la SALIDA del mezclador", () => T(vial), () => vial.Encajable.Agarrado || analizando || analizado || Resuelta)
                    .Con(I.Grip, () => T(vial), () => T(huecoAnalizador)).Nino("COGE el vial de la vacuna", "COGE EL VIAL", "AL ANALIZADOR"));
                l.Add(new SubPaso("Ponlo en el ANALIZADOR y espera el resultado", () => T(huecoAnalizador), () => analizado || Resuelta)
                    .Con(I.Grip, () => T(vial), () => T(huecoAnalizador)).Nino("PONLO en el analizador y espera", "EL VIAL", "PONLO AQUÍ"));
                l.Add(new SubPaso("Guárdalo donde el ARNm se conserve mejor", () => T(huecosAlmacen[1]), () => Resuelta)
                    .Con(I.Grip, () => T(vial), almacenes).Nino("GUÁRDALO: acércalo y la puerta se abre. ¿Cuál enfría más?", "COGE EL VIAL", "¿CUÁL ENFRÍA MÁS?"));
                break;
            case EtapaVacuna.Neutralizacion:
                l.Add(new SubPaso("Agarra la PIPETA", () => T(pipeta), () => pipeta.Encajable.Agarrado || pipeta.Contenido != null || progreso > 0 || Resuelta)
                    .Con(I.Grip, () => T(pipeta)).Nino("COGE la pipeta con el grip", "COGE LA PIPETA"));
                l.Add(new SubPaso("Carga un reactivo en un FRASCO y descárgalo en la PLACA", () => T(puntoPlaca), () => progreso > 0 || Resuelta)
                    .Con(I.Gatillo, () => T(frascos[0]), () => T(puntoPlaca)).Nino("Carga un FRASCO (gatillo) y suéltalo en la PLACA (gatillo)", "DEL FRASCO…", "A LA PLACA + GATILLO"));
                l.Add(new SubPaso(() => "Completa el ensayo en orden: " + string.Join(" > ", C.Neutralizacion.Select((p, i) => i < progreso ? p.boton : "?")),
                    () => T(puntoPlaca), () => Resuelta).Con(I.Gatillo, () => T(frascos[0]), () => T(puntoPlaca))
                    .Nino("Sigue el orden: suero, virus, INCUBAR, células y tinte", "DEL FRASCO…", "A LA PLACA + GATILLO"));
                break;
            case EtapaVacuna.Titulo:
                l.Add(new SubPaso("Mira la fila VACUNADO de la placa ampliada (pantalla derecha)", null, () => Resuelta).Con(I.Ninguno, () => Mirar(placa))
                    .Nino("MIRA la fila VACUNADO de la pantalla", "MIRA LA FILA VACUNADO"));
                l.Add(new SubPaso("Apunta con el rayo a la dilución del título y aprieta el gatillo", null, () => Resuelta).Con(I.Rayo, () => Mirar(placa))
                    .Nino("APUNTA a la última columna que sigue VIOLETA y aprieta el gatillo", "APUNTA A UNA DILUCIÓN + GATILLO"));
                break;
            case EtapaVacuna.Fases:
                l.Add(new SubPaso("Pulsa los botones A-E en el orden en que se hacen las fases", null, () => Resuelta).Con(I.Pulsar, () => T(opciones[0]))
                    .Nino("PULSA las letras en orden: de POCAS personas a MUCHAS", "PULSA LAS LETRAS"));
                break;
            case EtapaVacuna.Eficacia:
                // Una acción por pregunta: se mira la gráfica y se responde con las letras (la guía no señala la letra).
                for (int k = 0; k < C.PreguntasEficacia.Length; k++)
                {
                    int q = k;
                    l.Add(new SubPaso($"Pregunta {q + 1}: {C.PreguntasEficacia[q].titulo}", null, () => progreso > q || Resuelta).Con(I.Ninguno, () => Mirar(grafica))
                        .Nino($"MIRA las barras y PULSA la letra: {C.PreguntasEficacia[q].titulo}", "MIRA LAS BARRAS"));
                }
                break;
            default:
                return new SubPaso[0];
        }
        l.Add(new SubPaso("Pulsa CONTINUAR (verde) para seguir", () => T(continuar), () => false).Con(I.Pulsar, () => T(continuar))
            .Nino("¡Muy bien! PULSA el botón verde CONTINUAR", "PULSA CONTINUAR"));
        return l.ToArray();
    }

    // ------------------------------------------------------------------ PASO A PASO: plan guiado pieza a pieza
    // Como se le enseñaría a un niño: cada acción señala LA pieza concreta y su sitio, en el orden real del paso
    // ("coge CAP 5' > hueco 1", "coge UTR 5' > hueco 2"…). Si algo está mal colocado, primero enseña a quitarlo.
    // Solo los pasos de manipulación; las decisiones (antígeno, título, fases, eficacia) siguen siendo del estudiante.
    static string NombreBloque(string id) => System.Array.Find(C.Bloques, b => b.id == id)?.nombre ?? id;
    PuntoPipeta Frasco(string id) => frascos.FirstOrDefault(f => f != null && f.fuente != null && f.fuente.id == id);
    static Transform CasaDe(Encajable e) => e != null && e.casa != null ? e.casa.transform : null;
    bool CargadaCon(string id) => pipeta.Contenido != null && pipeta.Contenido.id == id;

    bool PlanGuiado(EtapaVacuna e, List<SubPaso> l)
    {
        switch (e)
        {
            case EtapaVacuna.Diseno:
                for (int k = 0; k < C.OrdenDiseno.Length; k++)
                {
                    int q = k;
                    string id = C.OrdenDiseno[q], nombre = NombreBloque(id);
                    System.Func<bool> malo = () => huecosMolde[q].Actual != null && IdsMolde()[q] != id;
                    l.Add(new SubPaso(() => malo() ? $"Saca el bloque equivocado del hueco {q + 1}" : $"Pon {nombre} en el hueco {q + 1}",
                            () => T(huecosMolde[q]), () => IdsMolde()[q] == id || Resuelta)
                        .Con(I.Grip, () => malo() ? T(huecosMolde[q].Actual) : T(Bloque(id)), () => malo() ? CasaDe(huecosMolde[q].Actual) : T(huecosMolde[q]))
                        .Nino(() => malo() ? $"SACA este bloque del hueco {q + 1}: no va ahí" : $"COGE el bloque {nombre} y PONLO en el hueco {q + 1}",
                              () => malo() ? "SÁCALO" : $"COGE {nombre}", () => malo() ? "A LA BANDEJA" : $"HUECO {q + 1}"));
                }
                return true;

            case EtapaVacuna.Sintesis:
                l.Add(new SubPaso("Agarra la PIPETA de su soporte (grip)", () => T(pipeta),
                    () => pipeta.Encajable.Agarrado || pipeta.Contenido != null || IngredientesTubo() > 0 || Resuelta).Con(I.Grip, () => T(pipeta))
                    .Nino("COGE la pipeta con el grip", "COGE LA PIPETA"));
                foreach (var id0 in IdsSintesis)
                {
                    string id = id0, nombre = Reactivo(C.ReactivosSintesis, id).nombre;
                    l.Add(new SubPaso($"Carga {nombre} en la pipeta", () => T(Frasco(id)), () => CargadaCon(id) || tubo.contenido.Contains(id) || Resuelta)
                        .Con(I.Gatillo, () => T(pipeta), () => T(Frasco(id)))
                        .Nino($"Mete la punta en el frasco {nombre} y aprieta el GATILLO", "LA PIPETA…", $"{nombre} + GATILLO"));
                    l.Add(new SubPaso($"Descarga {nombre} en el tubo", () => T(tubo), () => tubo.contenido.Contains(id) || Resuelta)
                        .Con(I.Gatillo, () => T(pipeta), () => T(tubo))
                        .Nino("Lleva la pipeta al TUBO y aprieta el GATILLO", "LA PIPETA…", "TUBO + GATILLO"));
                }
                l.Add(new SubPaso("Lleva el TUBO al pocillo del TERMOBLOQUE", () => T(huecoTermo), () => huecoTermo.Actual == tubo.Encajable || incubando || Resuelta)
                    .Con(I.Grip, () => T(tubo), () => T(huecoTermo)).Nino("COGE el tubo y PONLO en el termobloque", "COGE EL TUBO", "PONLO AQUÍ"));
                l.Add(new SubPaso("Pulsa INCUBAR 37 °C", () => T(incubarTubo), () => incubando || Resuelta).Con(I.Pulsar, () => T(incubarTubo))
                    .Nino("PULSA el botón INCUBAR", "PULSA INCUBAR"));
                return true;

            case EtapaVacuna.Encapsulado:
                foreach (var c0 in cartuchos.Where(c => c.datos != null && c.datos.correcta))
                {
                    var c = c0;
                    string nombre = c.datos.nombre;
                    l.Add(new SubPaso($"Pon el cartucho {nombre} en una entrada de lípidos", () => T(c), () => EnMezclador(c) || mezclado)
                        .Con(I.Grip, () => T(c), () => T(huecosLipido.FirstOrDefault(h => h.Libre)))
                        .Nino($"COGE el cartucho {nombre} y PONLO en una entrada", $"COGE {nombre}", "PONLO AQUÍ"));
                }
                l.Add(new SubPaso("Pon el TUBO con tu ARNm en la entrada ARNm", () => T(huecoMezclaTubo), () => TuboEnMezclador() || mezclado)
                    .Con(I.Grip, () => T(tubo), () => T(huecoMezclaTubo)).Nino("COGE el tubo y PONLO en la entrada ARNm", "COGE EL TUBO", "PONLO AQUÍ"));
                l.Add(new SubPaso("Pulsa MEZCLAR", () => T(mezclar), () => mezclando || mezclado).Con(I.Pulsar, () => T(mezclar))
                    .Nino("PULSA el botón MEZCLAR", "PULSA MEZCLAR"));
                return true;

            case EtapaVacuna.Calidad:
                l.Add(new SubPaso("Toma el VIAL de la SALIDA del mezclador", () => T(vial), () => vial.Encajable.Agarrado || analizando || analizado || Resuelta)
                    .Con(I.Grip, () => T(vial), () => T(huecoAnalizador)).Nino("COGE el vial de la vacuna", "COGE EL VIAL", "AL ANALIZADOR"));
                l.Add(new SubPaso("Ponlo en el ANALIZADOR y espera el resultado", () => T(huecoAnalizador), () => analizado || Resuelta)
                    .Con(I.Grip, () => T(vial), () => T(huecoAnalizador)).Nino("PONLO en el analizador y espera", "EL VIAL", "PONLO AQUÍ"));
                l.Add(new SubPaso("Guárdalo en el ULTRACONGELADOR (-70 °C)", () => T(huecosAlmacen[0]), () => Resuelta)
                    .Con(I.Grip, () => T(vial), () => T(huecosAlmacen[0]))
                    .Nino("GUÁRDALO en el ULTRACONGELADOR, el más frío (-70 °C): acércalo y se abre", "COGE EL VIAL", "ULTRACONGELADOR"));
                return true;

            case EtapaVacuna.Neutralizacion:
                l.Add(new SubPaso("Agarra la PIPETA", () => T(pipeta), () => pipeta.Encajable.Agarrado || pipeta.Contenido != null || progreso > 0 || Resuelta)
                    .Con(I.Grip, () => T(pipeta)).Nino("COGE la pipeta con el grip", "COGE LA PIPETA"));
                for (int k = 0; k < C.Neutralizacion.Length; k++)
                {
                    int q = k;
                    var r = System.Array.Find(C.ReactivosEnsayo, x => C.PasoDeReactivo(x.id) == q);
                    if (r == null)
                    {
                        // Paso de la máquina (incubar suero + virus).
                        l.Add(new SubPaso($"Pulsa {C.Neutralizacion[q].boton}", () => T(incubarPlaca), () => progreso > q || Resuelta)
                            .Con(I.Pulsar, () => T(incubarPlaca)).Nino("PULSA el botón INCUBAR 1 h y espera", "PULSA INCUBAR"));
                        continue;
                    }
                    string id = r.id, nombre = r.nombre;
                    l.Add(new SubPaso($"Carga {nombre} en la pipeta", () => T(Frasco(id)), () => CargadaCon(id) || progreso > q || Resuelta)
                        .Con(I.Gatillo, () => T(pipeta), () => T(Frasco(id)))
                        .Nino($"Mete la punta en el frasco {nombre} y aprieta el GATILLO", "LA PIPETA…", $"{nombre} + GATILLO"));
                    l.Add(new SubPaso($"Descarga {nombre} en la placa", () => T(puntoPlaca), () => progreso > q || Resuelta)
                        .Con(I.Gatillo, () => T(pipeta), () => T(puntoPlaca))
                        .Nino("Lleva la pipeta a la PLACA y aprieta el GATILLO", "LA PIPETA…", "PLACA + GATILLO"));
                }
                return true;
        }
        return false;
    }

    int PasoActualDelPlan()
    {
        for (int i = 0; i < plan.Length; i++) if (!plan[i].hecho()) return i;
        return plan.Length - 1;
    }

    // El plan aparece línea a línea y se marca en vivo; la flecha flota sobre el objeto de la acción actual.
    void ActualizarPlan(Transform cab)
    {
        if (plan.Length == 0) { if (marcador.gameObject.activeSelf) marcador.gameObject.SetActive(false); return; }
        bool cambio = false;
        int visibles = Mathf.Min(plan.Length, Mathf.FloorToInt((Time.time - tPlan) / 0.4f));
        if (visibles != planVisible) { planVisible = visibles; cambio = true; }
        int i = PasoActualDelPlan();
        if (i != indicePlan)
        {
            if (indicePlan >= 0 && i > indicePlan)
            {
                var donde = marcador.gameObject.activeSelf ? marcador.position : transform.position + Vector3.up;
                Sonidos.Clic(donde);
                ManosRig.Vibrar(donde, 0.2f, 0.03f);
                if (i > 0) TerminarPresentacion();
            }
            indicePlan = i;
            cambio = true;
        }
        if (cambio) Refrescar();

        var objetivo = plan[i].objetivo != null ? plan[i].objetivo() : null;
        // En PASO A PASO la animación ya señala el objeto: la flecha amarilla se oculta para no tapar.
        bool ver = objetivo != null && dentro && !Animando && Actual != EtapaVacuna.Cierre && !guiando;
        if (marcador.gameObject.activeSelf != ver) marcador.gameObject.SetActive(ver);
        if (!ver) return;
        marcador.position = objetivo.position;
        flecha.localPosition = new Vector3(0f, 0.17f + 0.025f * Mathf.Sin(Time.time * 4f), 0f);
        flecha.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
        marcadorTxt.text = $"{i + 1}/{plan.Length} · {plan[i].texto()}";
        if (cab != null)
        {
            var d = marcadorTxt.transform.position - cab.position; d.y = 0f;
            if (d.sqrMagnitude > 1e-4f) marcadorTxt.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }

    string PlanTexto()
    {
        if (plan.Length == 0) return "";
        int actual = PasoActualDelPlan();
        var sb = new StringBuilder($"<b><color=#FFD25A>TU PLAN</color></b>   <size=80%><color=#A9BED2>{Mathf.Min(actual, plan.Length - 1)}/{plan.Length - 1} hecho</color></size>\n");
        for (int i = 0; i < Mathf.Min(planVisible, plan.Length); i++)
        {
            string t = plan[i].texto();
            if (i < actual) sb.AppendLine($"<color=#39FF88>[OK] {i + 1}. {t}</color>");
            else if (i == actual) sb.AppendLine($"<color=#FFD25A><b>> {i + 1}. {t}</b></color>");
            else sb.AppendLine($"<color=#7F93A8>      {i + 1}. {t}</color>");
        }
        return sb.ToString();
    }

    // Al pulsar CONTINUAR, la pantalla derecha presenta el paso que viene y su plan (se va agregando línea a línea).
    void Presentar(EtapaVacuna e)
    {
        int n = (int)e;
        OcultarVisuales();
        presentando = true;
        var lineas = PasoAPaso ? new[] { plan[0].Sencillo() } : plan.Take(plan.Length - 1).Select(p => p.texto()).ToArray();
        finPresentacion = Time.time + 0.5f + 0.45f * lineas.Length + 3.5f;
        tituloVisual.text = $"SIGUIENTE · PASO {n} DE {NumPasos}";
        piePasoVisual = pieVisual.text;
        pieVisual.text = "";
        siguiente.Mostrar($"PASO {n}/{NumPasos} · {C.Pasos[n - 1]}", TituloEtapa(), "Dónde: " + C.Donde[n - 1], lineas);
    }

    void TerminarPresentacion()
    {
        if (!presentando) return;
        presentando = false;
        siguiente.Ocultar();
        MostrarVisuales(Actual);
        tituloVisual.text = tituloPasoVisual;
        pieVisual.text = piePasoVisual;
    }

    // Apartado fijo "AHORA": SIEMPRE un solo texto corto (la acción actual) con el icono del botón del mando.
    void ActualizarVentana()
    {
        if (ventana == null || Time.time < tVentana) return;
        tVentana = Time.time + 0.2f;
        switch (Actual)
        {
            case EtapaVacuna.Intro: ventana.Mostrar(true, $"{C.Codigo} · {C.Titulo.ToUpper()}", "Pulsa CONTINUAR (verde) para empezar", I.Pulsar); return;
            case EtapaVacuna.Cierre: ventana.Mostrar(true, "¡CASO RESUELTO!", "Mira tu informe o pulsa REPETIR para volver a jugar", I.Ninguno); return;
        }
        if (plan.Length == 0) { ventana.Mostrar(false, "", "", I.Ninguno); return; }
        int n = (int)Actual, i = PasoActualDelPlan();
        string cab = $"PASO {n}/{NumPasos} · {C.Pasos[n - 1]}   ·   {i + 1}/{plan.Length}" + (PasoAPaso ? "   ·   <color=#B07CFF>PASO A PASO</color>" : "");
        if (Animando) ventana.Mostrar(true, cab, "Espera a que termine la máquina…", I.Ninguno);
        else ventana.Mostrar(true, cab, PasoAPaso ? plan[i].Sencillo() : plan[i].texto(), plan[i].icono);
    }

    // PASO A PASO: la animación de la acción actual se repite sin parar ("COGE ESTO" > "PONLO AQUÍ"), como si se
    // lo enseñáramos a un niño. Cambia sola al pasar a la siguiente acción; se pausa mientras trabaja una máquina.
    void GuiarPasoAPaso()
    {
        if (demo == null) return;
        bool activo = PasoAPaso && Actual >= EtapaVacuna.Antigeno && Actual <= EtapaVacuna.Eficacia && plan.Length > 0 && !Animando;
        if (!activo)
        {
            if (guiando) { demo.Detener(); guiando = false; claveGuia = -1; }
            return;
        }
        int i = PasoActualDelPlan();
        var s = plan[i];
        var d = s.desde != null ? s.desde() : s.objetivo != null ? s.objetivo() : null;
        var h = s.hasta != null ? s.hasta() : null;
        int clave = ((int)Actual * 100 + i) * 31 + (d != null ? d.GetInstanceID() : 0) * 17 + (h != null ? h.GetInstanceID() : 0);
        if (guiando && clave == claveGuia && demo.Reproduciendo) return;
        if (!guiando && demo.Reproduciendo) return;   // termina antes la demostración de AYUDA
        claveGuia = clave;
        guiando = ReproducirGesto(plan[i], -1);
    }

    // Lanza la animación de una acción del plan. Devuelve false si no hay nada que señalar.
    bool ReproducirGesto(SubPaso s, int veces)
    {
        var desde = s.desde != null ? s.desde() : null;
        if (desde == null && s.objetivo != null) desde = s.objetivo();
        var hasta = s.hasta != null ? s.hasta() : null;
        if (desde == null || demo == null) return false;
        demo.Reproducir(desde, hasta, s.icono, veces, s.txtDesde?.Invoke(), s.txtHasta?.Invoke());
        // Al enseñar dónde guardar el vial, los almacenes con puerta se entreabren.
        if (Actual == EtapaVacuna.Calidad && hasta != null)
        {
            var padre = hasta.GetComponentInParent<AlmacenFrio>();
            if (padre != null) padre.Entreabrir(1.5f);
            else foreach (var af in hasta.GetComponentsInChildren<AlmacenFrio>()) af.Entreabrir(1.5f);
        }
        return true;
    }

    // Modo PASO A PASO: el panel muestra una sola acción en grande con su icono, sin textos largos.
    void AlternarPasoAPaso()
    {
        PasoAPaso = !PasoAPaso;
        pasoAPaso.MarcarActivo(PasoAPaso);
        var pos = pasoAPaso.transform.position + Vector3.up * 0.3f;
        Sonidos.Clic(pos);
        Aviso.Mostrar(PasoAPaso ? "PASO A PASO: te enseño cada acción con una animación. ¡Copia lo que hace!" : "Modo normal: vuelve el plan completo.", pos, Aviso.Tipo.Info, 3f);
        tVentana = 0f;
        claveGuia = -1;
        if (Actual >= EtapaVacuna.Antigeno && Actual <= EtapaVacuna.Eficacia)
        {
            plan = Plan(Actual);
            indicePlan = -1;
            planVisible = plan.Length;
            tPlan = Time.time - 100f;
        }
        Refrescar();
    }

    // Primera línea de un mensaje y, si es larga, solo su primera frase (para el modo PASO A PASO).
    static string PrimeraFrase(string m)
    {
        if (string.IsNullOrEmpty(m)) return "";
        int nl = m.IndexOf('\n');
        if (nl >= 0) m = m.Substring(0, nl);
        if (m.Length > 150)
        {
            int punto = m.IndexOf(". ", 70);
            if (punto > 0) m = m.Substring(0, punto + 1);
        }
        return m;
    }

    // Cartel grande delante de la vista al empezar cada paso.
    void Banner(EtapaVacuna e)
    {
        var cab = ManosRig.Cabeza;
        if (cab == null) return;
        int n = (int)e;
        var frente = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
        Aviso.Mostrar($"PASO {n}/{NumPasos} · {C.Pasos[n - 1]}\n<size=75%>{C.Donde[n - 1]}</size>", cab.position + frente * 0.85f + Vector3.down * 0.05f, Aviso.Tipo.Info, 3.5f);
    }

    void CrearMarcador()
    {
        marcador = new GameObject("MarcadorPaso").transform;
        marcador.SetParent(transform, false);
        var amarillo = K.Plano(K.Amarillo);
        flecha = K.Grupo(marcador, "Flecha", new Vector3(0f, 0.17f, 0f));
        K.Pieza(flecha, "Punta", PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.045f, amarillo, Quaternion.Euler(45f, 0f, 45f));
        K.Pieza(flecha, "Tallo", PrimitiveType.Cylinder, new Vector3(0f, 0.065f, 0f), new Vector3(0.016f, 0.04f, 0.016f), amarillo);
        K.Pieza(marcador, "Anillo", PrimitiveType.Cylinder, new Vector3(0f, 0.004f, 0f), new Vector3(0.12f, 0.0008f, 0.12f), amarillo);
        marcadorTxt = K.Texto(marcador, "", new Vector3(0f, 0.36f, 0f), 0.28f, K.Amarillo, 0.75f, 0.2f, TextAlignmentOptions.Bottom, true);
        marcadorTxt.outlineWidth = 0.25f;
        marcadorTxt.outlineColor = new Color32(0, 0, 0, 235);
        marcador.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ comunes

    void Fallo(string que, Vector3 pos)
    {
        Errores++;
        erroresEtapa.TryGetValue(Actual, out int n);
        erroresEtapa[Actual] = n + 1;
        Registrar(que, false);
        Sonidos.Error(pos);
        ManosRig.Vibrar(pos, 0.3f, 0.1f);
        // Tras dos fallos en el mismo paso, se ofrece la pista (no se da sola: el estudiante decide).
        if (n + 1 == 2) Aviso.Mostrar("¿Atascado? Pulsa AYUDA (amarillo): te enseña el gesto.", pista.transform.position + Vector3.up * 0.3f, Aviso.Tipo.Pista, 4f);
    }

    void Registrar(string que, bool bien)
    {
        decisiones.Add((bien ? "<color=#39FF88>[OK]</color> " : "<color=#FF7A6B>[X]</color> ") + que);
        if (metricas == null) return;
        metricas.Datos.hipotesis.Add(que);
        metricas.Datos.intentos.Add(new MetricsLogger.Intento
        {
            numero = metricas.Datos.intentos.Count + 1, hipotesis = Actual.ToString(), agente = que,
            eficacia = bien ? 1f : 0f, exito = bien, minuto = metricas.Minutos,
        });
    }

    public bool Animando => placa.Animando || sintesis.Animando || lnp.Animando || calidad.Animando || incubando || mezclando || analizando;

    void Continuar()
    {
        var pos = continuar.transform.position + Vector3.up * 0.3f;
        if (Actual == EtapaVacuna.Cierre) return;
        if (Animando) { Aviso.Mostrar("Espera a que termine la máquina.", pos, Aviso.Tipo.Info, 2f); return; }
        if (!Resuelta)
        {
            // Aún no se puede avanzar: sin sonido de error; se recuerda qué falta y se enseña el gesto.
            Aviso.Mostrar(QueFalta(), pos, Aviso.Tipo.Info, 3.5f);
            Sonidos.Clic(pos);
            EnsenarGesto();
            return;
        }
        if (Actual == EtapaVacuna.Eficacia) { Completar(); return; }
        Poner(Actual + 1);
    }

    string QueFalta()
    {
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: return "Primero toca la parte del virus holográfico que será tu antígeno.";
            case EtapaVacuna.Diseno: return "Primero completa el molde con los 5 bloques en orden (mesa DISEÑO).";
            case EtapaVacuna.Sintesis: return "Primero fabrica el ARNm: 3 ingredientes en el tubo, tubo al termobloque e INCUBAR.";
            case EtapaVacuna.Encapsulado: return "Primero formula la vacuna: 4 lípidos y el tubo en el mezclador, y pulsa MEZCLAR.";
            case EtapaVacuna.Calidad: return "Primero analiza el vial y guárdalo en el sitio correcto.";
            case EtapaVacuna.Neutralizacion: return "Primero completa los 5 pasos del ensayo en orden.";
            case EtapaVacuna.Titulo: return "Primero apunta a la dilución del título (pantalla derecha).";
            case EtapaVacuna.Fases: return "Primero ordena las fases con los botones A-E.";
            default: return "Primero responde las preguntas mirando las barras (botones del mostrador).";
        }
    }

    void PedirPista()
    {
        string[] lista;
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: lista = C.Antigeno.pistas; break;
            case EtapaVacuna.Diseno: lista = C.PistasDiseno; break;
            case EtapaVacuna.Sintesis: lista = C.PistasSintesis; break;
            case EtapaVacuna.Encapsulado: lista = C.PistasEncapsulado; break;
            case EtapaVacuna.Calidad: lista = C.PistasCalidad; break;
            case EtapaVacuna.Neutralizacion: lista = C.PistasNeutralizacion; break;
            case EtapaVacuna.Titulo: lista = C.PistasTitulo; break;
            case EtapaVacuna.Fases: lista = C.PistasFases; break;
            case EtapaVacuna.Eficacia: lista = PreguntaEficacia().pistas; break;
            default: lista = new[] { Actual == EtapaVacuna.Intro ? "Pulsa el botón verde CONTINUAR para empezar el caso." : "Caso resuelto: lee tu informe o pulsa REPETIR." }; break;
        }
        nivelPista.TryGetValue(Actual, out int n);
        string p = lista[Mathf.Min(n, lista.Length - 1)];
        nivelPista[Actual] = n + 1;
        Pistas++;
        if (metricas != null) { metricas.Datos.pistasPedidas++; metricas.Datos.pistas.Add($"{Actual}: {p}"); }
        mensaje = $"<color=#FFD25A><b>AYUDA {Mathf.Min(n + 1, lista.Length)}/{lista.Length}.</b></color> {p}";
        Sonidos.Pista(pista.transform.position);
        EnsenarGesto();
        Refrescar();
    }

    // Lo principal de AYUDA no se lee: una bola fantasma hace el gesto de la acción actual (de dónde a dónde).
    void EnsenarGesto()
    {
        TerminarPresentacion();
        guiando = false;   // la demostración de AYUDA tiene prioridad; PASO A PASO la retoma al terminar
        if (demo == null) return;
        if (Actual == EtapaVacuna.Intro) demo.Reproducir(continuar.transform, null, I.Pulsar, 3, "PULSA CONTINUAR");
        else if (Actual == EtapaVacuna.Cierre) demo.Reproducir(repetir.transform, null, I.Pulsar, 3, "PULSA REPETIR");
        else if (plan.Length > 0) ReproducirGesto(plan[PasoActualDelPlan()], 3);
    }

    void Completar()
    {
        Poner(EtapaVacuna.Cierre, false);
        var cab = ManosRig.Cabeza;
        var pos = cab != null ? cab.position : transform.position + Vector3.up * 1.5f;
        Sonidos.Logro(pos);
        RecuerdosBaymax.Recuperar(C.Codigo);
        if (!guardado) { metricas?.Guardar(true); guardado = true; }
        bool cp = Partida.GuardarCheckpoint(C.Codigo);
        if (cab != null)
        {
            var frente = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
            Aviso.Mostrar($"{C.Codigo} RESUELTO · ¡Creaste una vacuna! Baymax recupera un recuerdo científico ({RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total})" + (cp ? "\nCHECKPOINT GUARDADO" : ""),
                cab.position + frente * 0.9f + Vector3.down * 0.1f, Aviso.Tipo.Ok, 6f);
        }
        Refrescar();
    }

    public void Reiniciar()
    {
        if (!guardado && metricas != null) metricas.Guardar(false);
        StopAllCoroutines();
        metricas = new MetricsLogger(C.Codigo);
        guardado = false;
        Errores = 0; Pistas = 0;
        erroresEtapa.Clear(); nivelPista.Clear(); decisiones.Clear();
        ReiniciarMesa();
        Poner(EtapaVacuna.Antigeno);
    }

    void ReiniciarMesa()
    {
        silencio = true;
        incubando = mezclando = analizando = analizado = mezclado = tuboConsumido = false;
        progreso = 0;
        // Primero se sacan todos de donde estén y luego cada uno vuelve a su sitio (así ningún sitio queda ocupado).
        var todas = new List<PiezaVacuna>(bloques) { tubo, vial };
        todas.AddRange(cartuchos);
        foreach (var p in todas) { if (!p.gameObject.activeSelf) p.gameObject.SetActive(true); p.Encajable.Bloquear(false); p.Encajable.Desencajar(); }
        if (!pipeta.gameObject.activeSelf) pipeta.gameObject.SetActive(true);
        pipeta.Encajable.Desencajar();
        vial.Encajable.casa = salidaVial;
        foreach (var p in todas) A.Recolocar(p.Encajable);
        A.Recolocar(pipeta.Encajable);
        pipeta.Vaciar();
        tubo.contenido.Clear();
        tubo.Liquido(Color.white, 0f);
        tubo.etiqueta.text = "VACÍO";
        diagramaArnm.Mostrar(new string[5], null, "");
        sintesis.Reiniciar();
        lnp.Reiniciar();
        calidad.Reiniciar();
        placa.Reiniciar();
        placa.MarcarTitulo(false);
        PintarPlacaFisica(0);
        fases.Reiniciar();
        grafica.Reiniciar();
        virus.Anticuerpos(false);
        if (holograma != null) holograma.Anticuerpos(false);
        K.Pintar(canalMezcla, new Color(0.4f, 0.9f, 1f));
        pantallaAnalizador.text = "ANALIZADOR\nlisto";
        silencio = false;
    }

    // ------------------------------------------------------------------ interfaz

    void RefrescarMaquinas()
    {
        if (pantallaMezclador == null) return;
        if (!mezclando)
            pantallaMezclador.text = mezclado ? "MEZCLADOR\n<color=#39FF88>vacuna formulada</color>"
                : $"MEZCLADOR · lípidos {LipidosCorrectos()}/4 · ARNm: {(TuboEnMezclador() ? "<color=#39FF88>sí</color>" : "no")}";
        if (!incubando)
            pantallaTermo.text = tubo.contenido.Contains("arnm") ? "TERMOBLOQUE\n<color=#39FF88>ARNm listo</color>"
                : huecoTermo.Actual == tubo.Encajable && tubo.gameObject.activeSelf ? "TERMOBLOQUE 37 °C\ntubo colocado" : "TERMOBLOQUE 37 °C\nsin tubo";
    }

    string TituloEtapa()
    {
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: return C.Antigeno.titulo;
            case EtapaVacuna.Diseno: return C.DisenoTitulo;
            case EtapaVacuna.Sintesis: return C.SintesisTitulo;
            case EtapaVacuna.Encapsulado: return C.EncapsuladoTitulo;
            case EtapaVacuna.Calidad: return C.CalidadTitulo;
            case EtapaVacuna.Neutralizacion: return C.NeutralizacionTitulo;
            case EtapaVacuna.Titulo: return C.TituloTitulo;
            case EtapaVacuna.Fases: return C.FasesTitulo;
            default: return C.Eficacia.titulo;
        }
    }

    string EnunciadoEtapa()
    {
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: return C.Antigeno.enunciado;
            case EtapaVacuna.Diseno: return C.DisenoEnunciado;
            case EtapaVacuna.Sintesis: return C.SintesisEnunciado;
            case EtapaVacuna.Encapsulado: return C.EncapsuladoEnunciado;
            case EtapaVacuna.Calidad: return C.CalidadEnunciado;
            case EtapaVacuna.Neutralizacion: return C.NeutralizacionEnunciado;
            case EtapaVacuna.Titulo: return C.TituloEnunciado;
            case EtapaVacuna.Fases: return C.FasesEnunciado;
            default: return C.Eficacia.enunciado;
        }
    }

    static string Ok(bool si, string texto) => si ? $"<color=#39FF88>[OK] {texto}</color>" : $"<color=#8095AA>[  ] {texto}</color>";

    // Estado de la mesa en el paso actual (sin revelar la respuesta).
    string Estado()
    {
        var sb = new StringBuilder();
        switch (Actual)
        {
            case EtapaVacuna.Antigeno:
                for (int i = 0; i < C.Antigeno.opciones.Length; i++)
                {
                    var o = C.Antigeno.opciones[i];
                    string linea = $"• <b>{o.corto}</b>: {o.texto}";
                    if (i == seleccion) linea = $"<color={(o.correcta ? "#39FF88" : "#FF8A70")}>> {linea}</color>";
                    sb.AppendLine(linea);
                }
                break;
            case EtapaVacuna.Diseno:
            {
                var ids = IdsMolde();
                sb.AppendLine($"<b>Molde (5' > 3'): {ids.Count(x => x != null)}/5 huecos llenos</b>");
                sb.AppendLine(string.Join("  ", ids.Select((id, i) => id == null ? $"<color=#8095AA>[{i + 1}] ?</color>" : $"[{i + 1}] {System.Array.Find(C.Bloques, b => b.id == id).nombre}")));
                sb.AppendLine("<color=#A9BED2>En la bandeja hay 6 bloques: sobra uno.</color>");
                break;
            }
            case EtapaVacuna.Sintesis:
            {
                var nombres = IdsSintesis.Where(id => tubo.contenido.Contains(id)).Select(id => System.Array.Find(C.ReactivosSintesis, x => x.id == id).nombre).ToList();
                sb.AppendLine(Ok(IngredientesTubo() == 3, $"Tubo de reacción: {IngredientesTubo()}/3 ingredientes" + (nombres.Count > 0 ? $" ({string.Join(", ", nombres)})" : "")));
                sb.AppendLine(Ok(huecoTermo.Actual == tubo.Encajable, "Tubo en el termobloque"));
                sb.AppendLine(Ok(Resuelta, "INCUBAR 37 °C"));
                sb.AppendLine($"<color=#A9BED2>Pipeta: {(pipeta.Contenido != null ? "cargada con " + pipeta.Contenido.nombre : "vacía")}</color>");
                break;
            }
            case EtapaVacuna.Encapsulado:
                sb.AppendLine(Ok(LipidosCorrectos() == 4 || mezclado, $"Lípidos en el mezclador: {(mezclado ? 4 : LipidosCorrectos())}/4"));
                sb.AppendLine(Ok(TuboEnMezclador() || mezclado, "Tubo con el ARNm en la entrada acuosa"));
                sb.AppendLine(Ok(mezclado, "MEZCLAR"));
                break;
            case EtapaVacuna.Calidad:
                sb.AppendLine(Ok(analizado, analizando ? "Control de calidad: analizando…" : "Control de calidad (analizador)"));
                sb.AppendLine(Ok(Resuelta, "Lote guardado en el sitio correcto"));
                break;
            case EtapaVacuna.Neutralizacion:
                sb.AppendLine($"<b>Pasos completados: {progreso}/{C.Neutralizacion.Length}</b>");
                for (int i = 0; i < C.Neutralizacion.Length; i++)
                    sb.AppendLine(i < progreso ? $"<color=#39FF88>[OK] {i + 1}. {C.Neutralizacion[i].boton}</color>" : $"<color=#4A6278>[  ] {i + 1}. ?</color>");
                sb.AppendLine($"<color=#A9BED2>Pipeta: {(pipeta.Contenido != null ? "cargada con " + pipeta.Contenido.nombre : "vacía")}</color>");
                break;
            case EtapaVacuna.Titulo:
                sb.AppendLine("Filas: <color=#73BFFF>VACUNADO</color> · <color=#FFB359>PLACEBO</color> · CONTROL (sin virus)");
                sb.AppendLine("Diluciones: 1:10  1:20  1:40  1:80  1:160  1:320  1:640  1:1280");
                break;
            case EtapaVacuna.Fases:
                sb.AppendLine($"<b>Etapas colocadas: {progreso}/{C.Fases.Length}</b>");
                for (int i = 0; i < C.Fases.Length; i++)
                    sb.AppendLine(i < progreso ? $"<color=#39FF88>[OK] {i + 1}. {C.Fases[i].boton}</color>" : $"<color=#4A6278>[  ] {i + 1}. ?</color>");
                break;
            case EtapaVacuna.Eficacia:
            {
                var qs = C.PreguntasEficacia;
                for (int k = 0; k < Mathf.Min(progreso, qs.Length); k++)
                    sb.AppendLine($"<color=#39FF88>[OK] {k + 1}. {qs[k].titulo} {qs[k].opciones.First(x => x.correcta).corto}</color>");
                if (Resuelta) { sb.AppendLine("<color=#39FF88><b>¡Analizaste el ensayo!</b></color>"); break; }
                var q = qs[progreso];
                sb.AppendLine($"<b><color=#FFD25A>PREGUNTA {progreso + 1}/{qs.Length}</color></b>  {q.enunciado}");
                for (int i = 0; i < q.opciones.Length; i++)
                {
                    var o = q.opciones[i];
                    string linea = $"<b>{Letras[i]}</b>  {o.texto}";
                    if (i == seleccion) linea = $"<color={(o.correcta ? "#39FF88" : "#FF8A70")}>> {linea}</color>";
                    sb.AppendLine(linea);
                }
                break;
            }
        }
        return sb.ToString();
    }

    string Indicacion()
    {
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: return "Toca con la mano una parte del virus holográfico del mostrador (o apúntala con el rayo + gatillo). Grip junto al virus para girarlo.";
            case EtapaVacuna.Diseno: return "Mesa DISEÑO (iluminada): agarra un bloque con el grip y suéltalo sobre un hueco del MOLDE. Encaja solo.";
            case EtapaVacuna.Sintesis: return "Mesa SÍNTESIS: agarra la PIPETA. Carga en un frasco (gatillo) y descarga en el TUBO DE REACCIÓN (gatillo).";
            case EtapaVacuna.Encapsulado: return "Mesa NANOPARTÍCULAS: lleva los cartuchos a las entradas de LÍPIDOS y el tubo a la entrada de ARNm.";
            case EtapaVacuna.Calidad: return "Mesa CALIDAD Y FRÍO: lleva el VIAL de la salida del mezclador al ANALIZADOR.";
            case EtapaVacuna.Neutralizacion: return "Carga la pipeta en los frascos de SÍNTESIS y descarga en la PLACA (mesa izquierda). Usa INCUBAR 1 h cuando toque.";
            case EtapaVacuna.Titulo: return "Apunta con el rayo a una dilución (arriba de la placa ampliada) y aprieta el gatillo.";
            case EtapaVacuna.Fases: return "Pulsa los botones A-E en el orden en que se hacen.";
            default: return "Mira las barras de la pantalla derecha y responde con las letras del mostrador.";
        }
    }

    void Refrescar()
    {
        if (titulo == null) return;
        int paso = (int)Actual;   // 1..9 para los pasos del reto
        bool fin = Actual == EtapaVacuna.Cierre;

        cabecera.text = $"LABORATORIO DE VACUNAS   ·   {C.Codigo}: {C.Titulo.ToUpper()} DE ARNm";
        for (int i = 0; i < segmentos.Length; i++)
        {
            var c = i + 1 < paso || fin ? K.Verde : i + 1 == paso ? K.Cian : new Color(0.25f, 0.32f, 0.4f);
            K.Pintar(segmentos[i], c);
            etiquetasSegmento[i].color = i + 1 == paso ? Color.white : i + 1 < paso || fin ? K.Verde : K.Gris;
        }

        // Botones A-E: solo en FASES (5) y EFICACIA (4).
        int n = Actual == EtapaVacuna.Fases ? C.Fases.Length : Actual == EtapaVacuna.Eficacia ? PreguntaEficacia().opciones.Length : 0;
        for (int i = 0; i < opciones.Length; i++)
        {
            bool ver = i < n;
            opciones[i].gameObject.SetActive(ver);
            cartelesOpcion[i].SetActive(ver);
            if (!ver) continue;
            opciones[i].etiqueta.text = Letras[i];
            textosCartel[i].text = Actual == EtapaVacuna.Fases ? $"<b>{Letras[i]}</b> · {C.Fases[C.OrdenBotonesFases[i]].corto}" : $"<b>{Letras[i]}</b> · {PreguntaEficacia().opciones[i].corto}";
        }
        pista.gameObject.SetActive(!fin); cartelPista.SetActive(!fin);
        continuar.gameObject.SetActive(!fin); cartelContinuar.SetActive(!fin);
        repetir.gameObject.SetActive(fin); cartelRepetir.SetActive(fin);
        continuar.MarcarActivo(Resuelta && !fin);

        // En el informe final, "Lo que aprendiste" ocupa también el hueco de las opciones.
        if (altoEnunciado <= 0f) altoEnunciado = enunciado.rectTransform.sizeDelta.y;
        enunciado.rectTransform.sizeDelta = new Vector2(enunciado.rectTransform.sizeDelta.x, fin ? 0.6f : altoEnunciado);

        switch (Actual)
        {
            case EtapaVacuna.Intro:
            {
                titulo.text = $"{C.Codigo} · {C.Titulo.ToUpper()} DE ARNm";
                enunciado.text = C.Historia + "\n<color=#FFD25A>9 pasos en 4 mesas, de izquierda a derecha. La franja de luz del borde del mostrador marca la mesa del paso actual.</color>";
                var sb = new StringBuilder();
                sb.AppendLine("<b>Cómo se juega</b>");
                sb.AppendLine("• <b>Agarra</b> con el GRIP y suelta sobre la silueta brillante: encaja sola.");
                sb.AppendLine("• <b>Pipeta</b>: punta junto a un frasco + gatillo (o A/X) = cargar; junto al tubo o la placa + gatillo = descargar.");
                sb.AppendLine("• <b>Botones y rótulos</b>: acerca la mano o apúntalos con el rayo de la mano vacía y aprieta el gatillo.");
                sb.AppendLine("• Los errores no penalizan. <color=#FFD25A>AYUDA</color> te enseña el gesto · <color=#B07CFF>PASO A PASO</color> = una acción a la vez · <color=#39FF88>CONTINUAR</color> avanza.");
                opcionesTxt.text = sb.ToString();
                respuesta.text = (RecuerdosBaymax.Lista.Contains(C.Codigo) ? "<color=#39FF88>Ya resolviste este caso antes: puedes repetirlo para repasar.</color>\n" : "") +
                                 "Pulsa <color=#39FF88><b>CONTINUAR</b></color> para empezar.\n\n<size=85%><color=#A9BED2>" + C.Aviso + "</color></size>";
                break;
            }
            case EtapaVacuna.Cierre:
            {
                titulo.text = $"{C.Codigo} RESUELTO · ¡CREASTE UNA VACUNA!";
                enunciado.text = $"<color=#8FD8FF><b>Lo que aprendiste</b></color>\n<size=85%>{C.Aprendido}</size>\n<size=80%>{C.EnElCuerpo}</size>";
                opcionesTxt.text = "";
                var sb = new StringBuilder();
                sb.AppendLine($"<color=#8FD8FF><b>Tu recorrido</b></color>   <size=85%>Errores: {Errores}   ·   Ayudas: {Pistas}   ·   Tiempo: {Tiempo()}</size>");
                foreach (var d in decisiones.Skip(Mathf.Max(0, decisiones.Count - 8))) sb.AppendLine($"<size=78%>{d}</size>");
                respuesta.text = sb.ToString();
                cierreTxt.text = $"<b>{C.Fragmento} recuperado · Baymax recuerda {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}</b>\n<size=85%>{C.TextoFragmento}</size>";
                break;
            }
            default:
                if (PasoAPaso && plan.Length > 0)
                {
                    int ia = PasoActualDelPlan();
                    titulo.text = $"PASO {paso}/{NumPasos} · {C.Pasos[paso - 1]}";
                    enunciado.text = "";
                    opcionesTxt.text = Actual == EtapaVacuna.Fases || Actual == EtapaVacuna.Eficacia ? Estado() : "";
                    accionGrande.text = $"<size=55%><color=#FFD25A>ACCIÓN {ia + 1} DE {plan.Length}</color></size>\n{plan[ia].Sencillo()}";
                    EstiloNanoLab.Poner(iconoPaso, plan[ia].icono);
                    respuesta.text = mensaje != null ? PrimeraFrase(mensaje) : $"<color=#A9BED2>{C.Donde[paso - 1].ToUpper()}</color>";
                    break;
                }
                titulo.text = $"PASO {paso}/{NumPasos} · {TituloEtapa()}";
                enunciado.text = EnunciadoEtapa();
                opcionesTxt.text = PlanTexto() + (Actual == EtapaVacuna.Fases || Actual == EtapaVacuna.Eficacia ? "\n" + Estado() : "");
                respuesta.text = mensaje ?? $"<color=#A9BED2>{C.Donde[paso - 1].ToUpper()}.</color> {Indicacion()}";
                break;
        }
        bool grande = PasoAPaso && !fin && Actual != EtapaVacuna.Intro && plan.Length > 0;
        if (accionGrande != null) accionGrande.gameObject.SetActive(grande);
        if (iconoPaso != null && !grande) iconoPaso.enabled = false;
        if (grupoPasoAPaso != null) grupoPasoAPaso.SetActive(!fin);
        estado.text = fin ? "<color=#A9BED2>Pulsa REPETIR para volver a hacer el caso.</color>"
            : $"Errores: {Errores}   ·   Ayudas: {Pistas}   ·   " + (Resuelta
                ? $"<color=#39FF88><b>¡Hecho! Siguiente: {(paso < NumPasos ? C.Pasos[paso] : "INFORME FINAL")} > pulsa CONTINUAR (verde)</b></color>"
                : "<color=#FFD25A>AYUDA (amarillo) si te atascas</color>");
    }

    string Tiempo()
    {
        float s = metricas != null ? metricas.Segundos : 0f;
        return $"{Mathf.FloorToInt(s / 60f)}:{Mathf.FloorToInt(s % 60f):00} min";
    }

    // ------------------------------------------------------------------ pruebas sin visor

    // Las mismas acciones que haría el estudiante con las manos (encajar piezas, cargar y descargar la pipeta,
    // pulsar botones), sin el agarre físico. Las usan F7/F6 en el Editor y la prueba NanoLab/Probar reto de vacuna.
    static P Reactivo(P[] lista, string id) => System.Array.Find(lista, x => x.id == id);
    PiezaVacuna Bloque(string id) => bloques.First(b => b.Id == id);

    void Pipetear(P p, PuntoPipeta destino)
    {
        pipeta.Cargar(p, pipeta.transform.position);
        pipeta.Usar(destino);
    }

    void ColocarEn(PiezaVacuna p, ZonaEncaje z)
    {
        if (z.Actual == p.Encajable) return;
        if (z.Actual != null) z.Actual.VolverACasa();
        p.Encajable.Encajar(z, true);
    }

    // Un error típico del paso actual (cuenta como error).
    public IEnumerator Fallar()
    {
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: ElegirAntigeno(0); break;
            case EtapaVacuna.Diseno:
            {
                string[] mal = { "cap", "utr5", "genN", "utr3", "polyA" };
                for (int i = 0; i < mal.Length; i++) { ColocarEn(Bloque(mal[i]), huecosMolde[i]); yield return new WaitForSeconds(0.1f); }
                break;
            }
            case EtapaVacuna.Sintesis: Pipetear(Reactivo(C.ReactivosSintesis, "adnpol"), puntoTubo); break;
            case EtapaVacuna.Encapsulado:
                ColocarEn(cartuchos.First(c => !c.datos.correcta), huecosLipido[0]);
                yield return new WaitForSeconds(1.7f);   // vuelve solo a la gradilla
                break;
            case EtapaVacuna.Calidad:
                if (!analizado)
                {
                    ColocarEn(vial, huecoAnalizador);
                    while (analizando) yield return null;
                }
                ColocarEn(vial, huecosAlmacen[1]);   // nevera
                // Espera a que el vial salga de la nevera y vuelva (puerta, salida y regreso).
                for (float t = 0f; t < 4f && vial.Encajable.Encajada == huecosAlmacen[1]; t += Time.deltaTime) yield return null;
                yield return new WaitForSeconds(0.2f);
                break;
            case EtapaVacuna.Neutralizacion: Pipetear(Reactivo(C.ReactivosEnsayo, "celulas"), puntoPlaca); break;
            case EtapaVacuna.Titulo: ElegirColumna(7); break;
            case EtapaVacuna.Fases: if (progreso == 0) PasoFases(System.Array.IndexOf(C.OrdenBotonesFases, 2)); break;
            case EtapaVacuna.Eficacia: ResponderEficacia(0); break;
        }
        while (Animando) yield return null;
    }

    // Resuelve el paso actual con las acciones correctas (desde cualquier estado).
    public IEnumerator Acertar()
    {
        var espera = new WaitForSeconds(0.12f);
        switch (Actual)
        {
            case EtapaVacuna.Antigeno: ElegirAntigeno(1); break;
            case EtapaVacuna.Diseno:
                for (int i = 0; i < C.OrdenDiseno.Length && !Resuelta; i++) { ColocarEn(Bloque(C.OrdenDiseno[i]), huecosMolde[i]); yield return espera; }
                break;
            case EtapaVacuna.Sintesis:
                foreach (var id in IdsSintesis)
                    if (!tubo.contenido.Contains(id)) { Pipetear(Reactivo(C.ReactivosSintesis, id), puntoTubo); yield return espera; }
                ColocarEn(tubo, huecoTermo);
                yield return espera;
                IncubarTubo();
                break;
            case EtapaVacuna.Encapsulado:
                foreach (var h in huecosLipido)
                    if (h.Actual != null && !h.Actual.GetComponent<PiezaVacuna>().datos.correcta) A.Recolocar(h.Actual);
                foreach (var c in cartuchos.Where(c => c.datos.correcta && !EnMezclador(c)))
                {
                    var libre = huecosLipido.FirstOrDefault(h => h.Libre);
                    if (libre != null) { ColocarEn(c, libre); yield return espera; }
                }
                ColocarEn(tubo, huecoMezclaTubo);
                yield return espera;
                Mezclar();
                break;
            case EtapaVacuna.Calidad:
                if (!analizado)
                {
                    ColocarEn(vial, huecoAnalizador);
                    yield return null;
                    while (analizando) yield return null;
                }
                ColocarEn(vial, huecosAlmacen[0]);
                break;
            case EtapaVacuna.Neutralizacion:
                while (!Resuelta)
                {
                    while (placa.Animando) yield return null;
                    if (progreso == 2) PasoEnsayo(2, puntoPlaca.transform.position);
                    else Pipetear(System.Array.Find(C.ReactivosEnsayo, r => C.PasoDeReactivo(r.id) == progreso), puntoPlaca);
                    yield return null;
                }
                break;
            case EtapaVacuna.Titulo: ElegirColumna(PlacaNeutralizacion.ColumnaTitulo); break;
            case EtapaVacuna.Fases:
                while (!Resuelta) { PasoFases(System.Array.IndexOf(C.OrdenBotonesFases, progreso)); yield return espera; }
                break;
            case EtapaVacuna.Eficacia:
                while (!Resuelta) { ResponderEficacia(System.Array.FindIndex(PreguntaEficacia().opciones, o => o.correcta)); yield return espera; }
                break;
        }
        yield return null;
        while (Animando) yield return null;
    }

    // Recorre el caso entero: en cada paso comete un error típico y luego lo resuelve. Comprueba que los errores se
    // cuentan, que no se avanza sin resolver, que se llega al informe y que hay recuerdo. (NanoLab/Probar reto de vacuna)
    public static bool PruebaTerminada { get; private set; }
    public static bool PruebaCorrecta { get; private set; }
    public static string Informe { get; private set; }
    public static event System.Action<string> Momento;   // para que la prueba del Editor haga capturas

    public IEnumerator Simular()
    {
        PruebaTerminada = false;
        var log = new StringBuilder();
        bool ok = true;
        void Comprobar(bool cond, string s) { log.AppendLine((cond ? "OK    " : "FALLO ") + s); if (!cond) { ok = false; Debug.LogError("[PruebaVacuna] FALLO: " + s); } else Debug.Log("[PruebaVacuna] " + s); }

        yield return null;
        yield return null;
        if (Actual != EtapaVacuna.Intro) { Reiniciar(); Poner(EtapaVacuna.Intro, false); }
        Comprobar(RetoManager.Instancia == null && GameObject.Find("7_CamaraExperimental") == null && Object.FindObjectOfType<EstacionGuia>() == null,
                  "El reto anterior (bacterias) está desactivado: la vacuna es el único reto");
        Comprobar(GameObject.Find("Estaciones") != null && GameObject.Find("1_BancoMuestras") != null && GameObject.Find("3_Microscopia") != null,
                  "El mobiliario de las estaciones del centro está en su sitio");
        Comprobar(pipeta != null && pipeta.GetComponent<Oculus.Interaction.Grabbable>() != null, "La pipeta es agarrable (Grabbable del Interaction SDK)");
        Comprobar(bloques.All(b => b.Encajable.Encajada != null || !b.gameObject.activeSelf), "Los bloques están en su sitio");
        Momento?.Invoke("vacuna_0_intro");
        Continuar();
        int esperados = 0, vueltas = 0;
        while (Actual != EtapaVacuna.Cierre && vueltas++ < 20)
        {
            var etapa = Actual;
            yield return new WaitForSeconds(0.2f);
            Momento?.Invoke($"vacuna_{(int)etapa}_{etapa.ToString().ToLower()}_a");
            int antes = Errores;
            yield return Fallar();
            esperados++;
            Comprobar(Errores == antes + 1 && !Resuelta, $"{etapa}: el error típico cuenta como error y no deja avanzar");
            Continuar();
            Comprobar(Actual == etapa, $"{etapa}: CONTINUAR no avanza sin resolver");
            yield return Acertar();
            Comprobar(Resuelta, $"{etapa}: se resuelve con las acciones correctas");
            yield return new WaitForSeconds(0.3f);
            Momento?.Invoke($"vacuna_{(int)etapa}_{etapa.ToString().ToLower()}_b");
            Continuar();
            yield return new WaitForSeconds(0.2f);
        }
        Comprobar(Actual == EtapaVacuna.Cierre, "Se llega al informe final (CIERRE)");
        Comprobar(RecuerdosBaymax.Lista.Contains(C.Codigo), $"Baymax recupera el recuerdo del {C.Codigo}");
        Comprobar(Errores == esperados, $"Errores contados: {Errores} (esperados {esperados})");
        Comprobar(placa.Pasos == 5, "La placa de neutralización completó sus 5 pasos");
        Comprobar(vial.Encajable.Encajada == huecosAlmacen[0], "El vial quedó en el ultracongelador");
        yield return new WaitForSeconds(0.6f);
        Momento?.Invoke("vacuna_9_cierre");
        Informe = log.ToString();
        PruebaCorrecta = ok;
        PruebaTerminada = true;
    }

#if UNITY_EDITOR
    bool depurando;

    void DepurarTeclado()
    {
        bool f6 = false, f7 = false;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null) { f6 = kb.f6Key.wasPressedThisFrame; f7 = kb.f7Key.wasPressedThisFrame; }
#else
        f6 = Input.GetKeyDown(KeyCode.F6); f7 = Input.GetKeyDown(KeyCode.F7);
#endif
        if ((!f6 && !f7) || depurando || Animando) return;
        if (Actual == EtapaVacuna.Cierre) { if (f7) Reiniciar(); return; }
        if (Resuelta) { if (f7) Continuar(); return; }
        StartCoroutine(Depurar(f6 ? Fallar() : Acertar()));
    }

    IEnumerator Depurar(IEnumerator accion)
    {
        depurando = true;
        yield return accion;
        depurando = false;
    }
#endif
}
