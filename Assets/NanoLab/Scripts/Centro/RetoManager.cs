using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum EtapaReto { Exploracion, Tincion, Observacion, Investigacion, Hipotesis, Preparacion, Experimento, Analisis, Cierre }

// Coordina el reto del Centro de Experimentación: ciclo exploración > observación > investigación >
// hipótesis > preparación > experimentación > análisis > aprendizaje. Una meta visible a la vez,
// evidencias, decisiones, intentos, pistas y métricas. No resuelve nada por el usuario.
public class RetoManager : MonoBehaviour
{
    public static RetoManager Instancia { get; private set; }
    public static readonly string[] Ciclo = { "EXPLORAR", "OBSERVAR", "INVESTIGAR", "HIPÓTESIS", "PREPARAR", "EXPERIMENTAR", "ANALIZAR", "APRENDER" };

    public RetoData reto;
    public Bounds zona;                          // sala del centro (para mostrar el objetivo en la muñeca)
    public EstacionGuia[] estaciones;            // 0 banco, 1 tinción, 2 microscopía, 3 biblioteca, 4 terminal, 5 prensa, 6 cámara, 7 archivo
    public PanelObjetivo[] paneles;
    public Baliza baliza;
    public PlacaCaso placa;
    public CamaraExperimental camara;
    public PanelResumen resumen;
    public TerminalHipotesis terminal;
    public EstacionFicha[] fichas;
    public Material materialPanel;

    public EtapaReto Actual { get; private set; }
    public MuestraData Hipotesis { get; private set; }
    public bool[] Evidencias { get; private set; }
    public int Pistas => metricas.Datos.pistasPedidas;
    public int Intentos => metricas.Datos.intentos.Count;
    public MetricsLogger Metricas => metricas;
    public string ObjetivoActual => Objetivo(Actual);

    MetricsLogger metricas;
    ObjetivoMuneca muneca;
    readonly Dictionary<EtapaReto, int> nivelPista = new Dictionary<EtapaReto, int>();
    bool dentro, sesionGuardada, cronometro;
    int ultimoAgente = -1;
    float ultimaEficacia;
    string pistaFallo, pistaPendiente;

    static readonly int[] EstacionDeEtapa = { 0, 1, 2, 3, 4, 5, 6, 2, 7 };
    static readonly int[] CicloDeEtapa = { 0, 1, 1, 2, 3, 4, 5, 6, 7 };

    void Awake()
    {
        Instancia = this;
        Evidencias = new bool[reto.evidencias.Length];
        metricas = new MetricsLogger(reto.codigo);
    }

    void OnEnable()
    {
        FuenteMuestra.Usada += AlUsarFuente;
        Portaobjetos.TincionCambiada += AlTenir;
        MicroscopioOptico.Observado += AlObservar;
        HUDMicroscopio.AumentoCambiado += AlCambiarAumento;
    }

    void OnDisable()
    {
        FuenteMuestra.Usada -= AlUsarFuente;
        Portaobjetos.TincionCambiada -= AlTenir;
        MicroscopioOptico.Observado -= AlObservar;
        HUDMicroscopio.AumentoCambiado -= AlCambiarAumento;
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        muneca = ObjetivoMuneca.Crear(materialPanel);
        if (resumen != null) resumen.Pendiente(reto);
        metricas.Etapa(Actual.ToString());
        Refrescar(false);
    }

    void Update()
    {
        var cab = ManosRig.Cabeza;
        bool ahora = cab != null && zona.Contains(new Vector3(cab.position.x, zona.center.y, cab.position.z));
        if (ahora && !cronometro && Actual == EtapaReto.Exploracion)
        {
            // El cronómetro del caso empieza la primera vez que se entra en el centro.
            cronometro = true;
            metricas = new MetricsLogger(reto.codigo);
            metricas.Etapa(Actual.ToString());
            Sonidos.Etapa(cab.position);
        }
        dentro = ahora;
        if (muneca != null) muneca.Visible(dentro && !MicroscopioOptico.AlgunoActivo);
        if (pistaPendiente != null && !MicroscopioOptico.AlgunoActivo && cab != null)
        {
            var frente = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
            Aviso.Mostrar(pistaPendiente, cab.position + frente * 0.9f + Vector3.down * 0.2f, Aviso.Tipo.Pista, 10f);
            Sonidos.Pista(cab.position);
            pistaPendiente = null;
        }
    }

    // ------------------------------------------------------------------ etapas

    string Objetivo(EtapaReto e)
    {
        switch (e)
        {
            case EtapaReto.Exploracion: return "Lleva la placa del CASO 01 del BANCO (1) a la mesa de TINCIÓN (2) y tócala con un portaobjetos para hacer el frotis.";
            case EtapaReto.Tincion: return "Tiñe el frotis: toca los goteros en orden 1 > 2 > 3 > 4 (tinción de Gram).";
            case EtapaReto.Observacion: return $"Pon el portaobjetos en un MICROSCOPIO (3) y mira por el ocular. Evidencias: {Evidencias.Count(x => x)}/{Evidencias.Length} (A/X: más aumento).";
            case EtapaReto.Investigacion: return "Compara tus evidencias con las fichas de la BIBLIOTECA (4) y el panel de agentes.";
            case EtapaReto.Hipotesis: return Intentos > 0
                ? $"Intento {Intentos + 1}: revisa tu hipótesis en la TERMINAL (5) y elige otro agente."
                : "Registra en la TERMINAL (5) qué microorganismo crees que es.";
            case EtapaReto.Preparacion: return "Elige un agente, encájalo en la PRENSA (6) y pulsa FORMULAR.";
            case EtapaReto.Experimento: return "Pon la placa y la cápsula en la CÁMARA (7) y pulsa ACTIVAR.";
            case EtapaReto.Analisis: return "Lleva el portaobjetos DESPUÉS a un MICROSCOPIO (3) y compáralo con el ANTES.";
            default: return "Caso resuelto. Lee el informe en el ARCHIVO (8).";
        }
    }

    void Avanzar(EtapaReto e)
    {
        if (e <= Actual) return;
        Poner(e);
    }

    void Poner(EtapaReto e)
    {
        Actual = e;
        metricas.Etapa(e.ToString());
        foreach (var est in estaciones) if (est != null) est.LimpiarPista();
        Refrescar(true);
    }

    void Refrescar(bool conSonido)
    {
        string obj = Objetivo(Actual);
        int est = EstacionDeEtapa[(int)Actual];
        for (int i = 0; i < estaciones.Length; i++) if (estaciones[i] != null) estaciones[i].Mostrar(i == est, obj);
        string titulo = $"{reto.codigo} · {reto.titulo.ToUpper()}";
        string estado = $"Intentos: {Intentos}   ·   Pistas: {Pistas}" + (pistaFallo != null && Actual == EtapaReto.Hipotesis ? $"\n<color=#FFD25A>PISTA: {pistaFallo}</color>" : "");
        foreach (var p in paneles) if (p != null) p.Mostrar(CicloDeEtapa[(int)Actual], titulo, obj, estado);
        if (baliza != null && est < estaciones.Length && estaciones[est] != null) baliza.MoverA(estaciones[est].puntoBaliza);
        if (muneca != null) muneca.Poner(obj);
        if (terminal != null) terminal.Refrescar();
        if (conSonido && ManosRig.Cabeza != null) Sonidos.Etapa(ManosRig.Cabeza.position);
    }

    // ------------------------------------------------------------------ eventos de las estaciones

    void AlUsarFuente(FuenteMuestra f, Portaobjetos p)
    {
        if (f.muestra == reto.muestra && f.etiqueta == reto.codigo && f.reactivo == 0 && !f.esDesinfectante) Avanzar(EtapaReto.Tincion);
    }

    void AlTenir(Portaobjetos p)
    {
        if (p.muestra != reto.muestra || !p.DeCaso || p.Despues) return;
        if (p.TincionErronea) metricas.Datos.tincionesErroneas++;
        if (p.TincionCompleta) Avanzar(EtapaReto.Observacion);
    }

    Portaobjetos observando;

    void AlObservar(MicroscopioOptico m, Portaobjetos p)
    {
        if (p == null || p.muestra != reto.muestra || !p.DeCaso) { observando = null; return; }
        observando = p;
        metricas.Datos.observacionesMicroscopio++;
        var hud = HUDMicroscopio.Instancia;
        if (p.Despues)
        {
            hud.MostrarReto(p.muestra, DatosComparacion);
            if (Actual == EtapaReto.Analisis) AnalisisCompletado();
        }
        else
        {
            EvaluarEvidencias(p, hud.Aumento);
            hud.MostrarReto(p.muestra, DatosObservacion);
        }
    }

    void AlCambiarAumento(int aumento)
    {
        metricas.Datos.maxAumentoUsado = Mathf.Max(metricas.Datos.maxAumentoUsado, aumento);
        if (observando == null || observando.Despues || HUDMicroscopio.Instancia == null) return;
        if (EvaluarEvidencias(observando, aumento)) HUDMicroscopio.Instancia.RefrescarReto();
    }

    string nuevaEvidencia;

    bool EvaluarEvidencias(Portaobjetos p, int aumento)
    {
        nuevaEvidencia = null;
        if (p.PasosTincion == 0 || p.TincionErronea) return false;
        bool cambio = false;
        for (int i = 0; i < reto.evidencias.Length; i++)
        {
            var ev = reto.evidencias[i];
            if (Evidencias[i] || aumento < ev.aumentoMinimo || (ev.requiereGram && !p.TincionCompleta)) continue;
            Evidencias[i] = true;
            cambio = true;
            nuevaEvidencia = $"{ev.clave}: {ev.valor}";
            metricas.Agregar(metricas.Datos.evidencias, nuevaEvidencia);
        }
        if (!cambio) return false;
        Sonidos.Carga(ManosRig.Cabeza != null ? ManosRig.Cabeza.position : transform.position);
        if (Evidencias.All(x => x)) Avanzar(EtapaReto.Investigacion);
        else Refrescar(false);
        return true;
    }

    public void FichaConsultada(string id)
    {
        metricas.Agregar(metricas.Datos.fichasConsultadas, id);
        Sonidos.Clic(transform.position);
        if (Actual == EtapaReto.Investigacion) Avanzar(EtapaReto.Hipotesis);
    }

    public bool PuedeHipotesis(out string motivo)
    {
        motivo = null;
        if (Actual == EtapaReto.Cierre) motivo = "El caso ya está resuelto. Pulsa REINICIAR en el ARCHIVO (8) para repetirlo.";
        else if (Actual == EtapaReto.Analisis) motivo = "Antes de cambiar tu hipótesis, analiza el resultado: mira el portaobjetos DESPUÉS en un MICROSCOPIO (3).";
        else if (!Evidencias.All(x => x))
        {
            var faltan = reto.evidencias.Where((e, i) => !Evidencias[i]).Select(e => e.clave.ToLower());
            motivo = $"Aún no tienes evidencia suficiente. Observa la muestra teñida al microscopio. Te falta: {string.Join(", ", faltan)}.";
        }
        return motivo == null;
    }

    public void RegistrarHipotesis(MuestraData m)
    {
        Hipotesis = m;
        metricas.Datos.hipotesis.Add(m.nombre);
        pistaFallo = null;
        Aviso.Mostrar($"Hipótesis registrada: {m.nombre}. Ahora prepara la intervención en la PRENSA (6).", terminal != null ? terminal.transform.position + Vector3.up * 0.6f : transform.position, Aviso.Tipo.Ok);
        if (Actual < EtapaReto.Preparacion) Poner(EtapaReto.Preparacion);
        else Refrescar(false);
    }

    public bool PuedeFormular(out string motivo)
    {
        motivo = null;
        if (Actual == EtapaReto.Cierre) motivo = "El caso ya está resuelto. Pulsa REINICIAR en el ARCHIVO (8) para repetirlo.";
        else if (Actual == EtapaReto.Analisis) motivo = "Antes de probar otro agente, analiza el resultado: mira el portaobjetos DESPUÉS en un MICROSCOPIO (3).";
        else if (Hipotesis == null || Actual < EtapaReto.Preparacion) motivo = "Primero registra tu hipótesis en la TERMINAL (5): ¿qué microorganismo crees que es?";
        return motivo == null;
    }

    public void CapsulaFormulada(int agente) => Avanzar(EtapaReto.Experimento);

    public bool PuedeExperimentar(out string motivo)
    {
        motivo = null;
        if (Actual == EtapaReto.Cierre) motivo = "El caso ya está resuelto. Pulsa REINICIAR en el ARCHIVO (8) para repetirlo.";
        else if (Actual == EtapaReto.Analisis) motivo = "Primero analiza el resultado anterior: mira el portaobjetos DESPUÉS en un MICROSCOPIO (3).";
        else if (Hipotesis == null) motivo = "Registra antes tu hipótesis en la TERMINAL (5).";
        return motivo == null;
    }

    public void ResultadoExperimento(int agente, float eficacia)
    {
        ultimoAgente = agente;
        ultimaEficacia = eficacia;
        var a = reto.agentes[agente];
        metricas.Datos.intentos.Add(new MetricsLogger.Intento
        {
            numero = Intentos + 1, hipotesis = Hipotesis != null ? Hipotesis.nombre : "", agente = a.sigla,
            eficacia = eficacia, exito = Exito, minuto = metricas.Minutos,
        });
        Poner(EtapaReto.Analisis);
    }

    bool Exito => ultimoAgente >= 0 && ultimaEficacia >= reto.umbralExito;

    void AnalisisCompletado()
    {
        metricas.Datos.comparacionAntesDespues = true;
        if (Exito)
        {
            Poner(EtapaReto.Cierre);
            MostrarResumen();
            if (!sesionGuardada) { metricas.Guardar(true); sesionGuardada = true; }
            if (ManosRig.Cabeza != null) Sonidos.Logro(ManosRig.Cabeza.position);
        }
        else
        {
            // El error es información: pista ligada a la evidencia y nueva placa para reintentar.
            pistaFallo = reto.agentes[ultimoAgente].pistaSiFalla;
            if (placa != null) placa.Reiniciar();
            Poner(EtapaReto.Hipotesis);
            pistaPendiente = pistaFallo;   // se muestra al apartar la cara del ocular (el HUD ya la explica)
        }
    }

    public string PedirPista()
    {
        string[] lista;
        switch (Actual)
        {
            case EtapaReto.Exploracion: lista = reto.pistasExploracion; break;
            case EtapaReto.Tincion: lista = reto.pistasTincion; break;
            case EtapaReto.Observacion: lista = reto.pistasObservacion; break;
            case EtapaReto.Investigacion: lista = reto.pistasInvestigacion; break;
            case EtapaReto.Hipotesis: lista = pistaFallo != null ? new[] { pistaFallo }.Concat(reto.pistasHipotesis).ToArray() : reto.pistasHipotesis; break;
            case EtapaReto.Preparacion: lista = reto.pistasPreparacion; break;
            case EtapaReto.Experimento: lista = reto.pistasExperimento; break;
            case EtapaReto.Analisis: lista = reto.pistasAnalisis; break;
            default: return "Ya resolviste el caso. Puedes repetirlo desde el ARCHIVO (8).";
        }
        if (lista == null || lista.Length == 0) return ObjetivoActual;
        nivelPista.TryGetValue(Actual, out int n);
        string p = lista[Mathf.Min(n, lista.Length - 1)];
        nivelPista[Actual] = n + 1;
        metricas.Datos.pistasPedidas++;
        metricas.Datos.pistas.Add($"{Actual}: {p}");
        Refrescar(false);
        return p;
    }

    public void Reiniciar()
    {
        if (!sesionGuardada) metricas.Guardar(false);
        metricas = new MetricsLogger(reto.codigo);
        sesionGuardada = false;
        Hipotesis = null;
        Evidencias = new bool[reto.evidencias.Length];
        nivelPista.Clear();
        ultimoAgente = -1; ultimaEficacia = 0f; pistaFallo = null; pistaPendiente = null;
        if (camara != null) camara.Reiniciar();
        if (placa != null)
        {
            placa.Reiniciar();
            var e = placa.GetComponent<Encajable>();
            if (e != null) e.VolverACasa();
        }
        if (fichas != null) foreach (var f in fichas) if (f != null) f.Poner(false);
        if (resumen != null) resumen.Pendiente(reto);
        Poner(EtapaReto.Exploracion);
    }

    // ------------------------------------------------------------------ microscopio

    HUDMicroscopio.DatosReto DatosObservacion(int aumento)
    {
        var p = observando;
        var m = reto.muestra;
        bool revelado = Actual == EtapaReto.Cierre;
        string tincion = p.PasosTincion == 0 ? "Sin teñir" : p.TincionErronea ? "NO VÁLIDA" : p.TincionCompleta ? "Gram completa" : $"Incompleta ({p.PasosTincion}/4)";
        var d = new HUDMicroscopio.DatosReto
        {
            subtitulo = revelado ? $"{reto.codigo}  ·  {m.nombre.ToUpper()} ({m.nombreCientifico})" : $"MUESTRA DESCONOCIDA  ·  {reto.codigo}",
            vista = p.PasosTincion == 0 ? reto.vistaSinTenir : p.TincionErronea ? reto.vistaTincionFallida : m.micrografia,
            riesgo = revelado ? m.riesgo : "DESCONOCIDO",
            colorRiesgo = revelado ? (Color?)null : new Color(1f, 0.75f, 0.3f),
            cuadernoTitulo = "CUADERNO DE EVIDENCIAS",
            obsTitulo = "QUÉ ESTÁS VIENDO",
            guia = "• ¿Qué forma tienen las células?\n• ¿Están sueltas, en cadenas o en racimos?\n• ¿De qué color quedaron tras la tinción de Gram?\n\n<color=#8FD8FF>A / X: más aumento     B / Y: menos aumento</color>",
        };
        var claves = new List<string> { "Origen", "Tinción" };
        var valores = new List<string> { reto.origen, tincion };
        for (int i = 0; i < reto.evidencias.Length && claves.Count < 6; i++)
        {
            claves.Add(reto.evidencias[i].clave);
            valores.Add(Evidencias[i] ? reto.evidencias[i].valor : "???");
        }
        d.claves = claves.ToArray(); d.valores = valores.ToArray();

        var sb = new StringBuilder();
        for (int i = 0; i < reto.evidencias.Length; i++)
        {
            var ev = reto.evidencias[i];
            sb.AppendLine(Evidencias[i]
                ? $"<color=#39FF88>[OK]</color> {ev.clave}: <b>{ev.valor}</b>"
                : $"<color=#4A6278>[  ]</color> <color=#8FA6BC>{ev.clave}: ???  <size=85%>({ev.comoObtenerla})</size></color>");
        }
        d.cuaderno = sb.ToString();

        if (p.PasosTincion == 0)
        {
            d.obs = "Sin teñir, las bacterias son casi transparentes: apenas se distinguen del fondo.";
            d.aviso = "MUESTRA SIN TEÑIR · Vuelve a la mesa de TINCIÓN (2)";
            d.colorAviso = new Color(1f, 0.75f, 0.35f);
        }
        else if (p.TincionErronea)
        {
            d.obs = "El orden de los reactivos fue incorrecto: el color que ves puede engañarte (un falso Gram -). No sirve como evidencia.";
            d.aviso = "TINCIÓN NO VÁLIDA · Limpia el portaobjetos y repite 1 > 2 > 3 > 4";
            d.colorAviso = new Color(1f, 0.45f, 0.4f);
        }
        else if (!p.TincionCompleta)
        {
            d.obs = $"Ya se distingue la forma, pero el resultado Gram solo vale tras los 4 pasos (vas en el {p.PasosTincion}/4).";
            d.aviso = nuevaEvidencia != null ? $"NUEVA EVIDENCIA · {nuevaEvidencia}" : "TINCIÓN INCOMPLETA · Termina los 4 pasos para conocer el Gram";
            d.colorAviso = nuevaEvidencia != null ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.75f, 0.35f);
        }
        else if (!Evidencias.All(x => x))
        {
            d.obs = "Observa la forma de las células y cómo se agrupan. Sube el aumento para ver los detalles.";
            d.aviso = nuevaEvidencia != null ? $"NUEVA EVIDENCIA · {nuevaEvidencia}" : "Sube el aumento para registrar más evidencias";
            d.colorAviso = nuevaEvidencia != null ? new Color(0.35f, 1f, 0.55f) : new Color(0.6f, 0.9f, 1f);
        }
        else
        {
            d.obs = revelado ? reto.explicacion : "Evidencias completas. Compáralas con las fichas de la BIBLIOTECA (4) para decidir qué microorganismo es.";
            d.aviso = nuevaEvidencia != null ? $"EVIDENCIAS COMPLETAS · {nuevaEvidencia}" : "EVIDENCIAS COMPLETAS · Ve a la BIBLIOTECA (4)";
            d.colorAviso = new Color(0.35f, 1f, 0.55f);
        }
        nuevaEvidencia = null;
        return d;
    }

    HUDMicroscopio.DatosReto DatosComparacion(int aumento)
    {
        var m = reto.muestra;
        if (ultimoAgente < 0)
            return new HUDMicroscopio.DatosReto { subtitulo = "PORTAOBJETOS DESPUÉS", vista = m.micrografia, obs = "Aún no hay resultados.", obsTitulo = "", cuadernoTitulo = "" };
        var a = reto.agentes[ultimoAgente];
        bool exito = Exito;
        bool crecio = ultimaEficacia <= 0.05f;
        var d = new HUDMicroscopio.DatosReto
        {
            subtitulo = $"ANTES | DESPUÉS  ·  {reto.codigo}  ·  INTENTO {Intentos}",
            vista = exito ? reto.vistaDespuesEficaz : crecio ? reto.vistaDespuesCrecimiento : m.micrografia,
            antes = m.micrografia,
            antesTitulo = "ANTES (sin intervención)",
            claves = new[] { "Vista central", "Agente", "Acción", "Hipótesis", "Células vivas", "Intento" },
            valores = new[] { "DESPUÉS", $"{a.sigla} · {a.nombre}", a.accion, Hipotesis != null ? Hipotesis.nombre : "-", exito ? "muy pocas (lisadas)" : crecio ? "más que antes" : "casi todas", Intentos.ToString() },
            riesgo = exito ? "EFICAZ" : "SIN EFECTO",
            riesgoClave = "Resultado",
            colorRiesgo = exito ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.35f, 0.3f),
            cuadernoTitulo = "INTERPRETACIÓN",
            cuaderno = exito
                ? $"Las células perdieron su forma y se rompieron. El agente {a.sigla} {a.accion.ToLower()} Tu muestra tiene una pared gruesa de peptidoglucano: por eso quedó VIOLETA en la tinción de Gram."
                : a.pistaSiFalla,
            obsTitulo = "SIGUIENTE PASO",
            obs = exito ? "Ve al ARCHIVO (8): el informe del caso y el fragmento recuperado te esperan."
                        : "Vuelve a la TERMINAL (5): revisa tu hipótesis y prueba otro agente. La cámara ya tiene una placa nueva del cultivo.",
            aviso = exito ? "RESULTADO: EFICAZ · La intervención detuvo a la bacteria" : "RESULTADO: SIN EFECTO · Compara con el ANTES y piensa por qué",
            colorAviso = exito ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.45f, 0.4f),
        };
        return d;
    }

    // ------------------------------------------------------------------ cierre

    void MostrarResumen()
    {
        if (resumen == null) return;
        var m = reto.muestra;
        var dat = metricas.Datos;
        var izq = new StringBuilder();
        izq.AppendLine("<color=#8FD8FF><b>TU INVESTIGACIÓN</b></color>");
        izq.AppendLine("<b>Lo que observaste:</b>");
        foreach (var ev in reto.evidencias) izq.AppendLine($"  • {ev.clave}: {ev.valor}");
        izq.AppendLine($"<b>Fichas consultadas:</b> {(dat.fichasConsultadas.Count > 0 ? string.Join(", ", dat.fichasConsultadas) : "ninguna")}");
        izq.AppendLine($"<b>Tus hipótesis:</b> {string.Join(" > ", dat.hipotesis)}");
        izq.AppendLine("<b>Intervenciones probadas:</b>");
        foreach (var it in dat.intentos) izq.AppendLine($"  {it.numero}. Agente {it.agente}: {(it.exito ? "<color=#39FF88>eficaz</color>" : "<color=#FF7A6B>sin efecto</color>")}");
        izq.AppendLine($"<b>Pistas pedidas:</b> {dat.pistasPedidas}   <b>Tiempo:</b> {Mathf.FloorToInt(metricas.Segundos / 60f)}:{Mathf.FloorToInt(metricas.Segundos % 60f):00} min");

        var der = new StringBuilder();
        der.AppendLine($"<color=#8FD8FF><b>LA MUESTRA ERA: {m.nombre.ToUpper()}</b></color>  <i>{m.nombreCientifico}</i>");
        der.AppendLine(reto.explicacion);
        if (Hipotesis != m) der.AppendLine($"\n<color=#FFD25A>Tu última hipótesis fue {(Hipotesis != null ? Hipotesis.nombre : "-")}. Fíjate: la evidencia (cocos violetas en racimo) apuntaba a {m.nombre}.</color>");
        der.AppendLine("\n<size=80%><color=#A9BED2>Modelo educativo simplificado: no es un protocolo clínico ni un tratamiento real.</color></size>");

        resumen.Mostrar($"{reto.codigo} RESUELTO · {m.nombre.ToUpper()}", izq.ToString(), der.ToString(), $"<b>{reto.fragmento} restaurado</b>\n{reto.textoFragmento}");
    }
}
