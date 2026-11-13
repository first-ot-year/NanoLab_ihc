using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum EtapaReto { Exploracion, Tincion, Observacion, Investigacion, Hipotesis, Preparacion, Experimento, Analisis, Cierre }

// Coordina las MISIONES 1 y 2 del Centro de Experimentación con el mobiliario de sus estaciones (ver Misiones):
//   MISIÓN 1 · OBSERVAR (Exploración > Tinción > Observación > Investigación > identificar en la terminal):
//     frotis y tinción de Gram de la MUESTRA A, y comparación al microscopio con la B (moho) y la C (hisopado del
//     brote); clasificación de las tres en CONCLUSIONES, ficha de la biblioteca e identificación de la A.
//   MISIÓN 2 · EXPERIMENTAR (estrategia > Preparación > Experimento > Análisis > explicar en CONCLUSIONES > Cierre):
//     el ciclo hipótesis-intervención-comparación ANTES | DESPUÉS del caso de E2-E4.
// Cada misión devuelve un recuerdo y guarda sus propias métricas. Una meta visible a la vez; no resuelve nada por el
// usuario: los errores dan una explicación y se reintenta sin penalización.
public class RetoManager : MonoBehaviour
{
    public static RetoManager Instancia { get; private set; }
    public static event System.Action Cambio;   // cualquier avance del caso (lo escucha GestorPartida)
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
    // Misión 1: muestras que se comparan con la A y panel de CONCLUSIONES (los pone MontajeMisiones al empezar).
    public MuestraData muestraHongo, muestraVirus;
    public PanelConclusiones conclusiones;

    public EtapaReto Actual { get; private set; }
    public MuestraData Hipotesis { get; private set; }
    public int Estrategia { get; private set; } = -1;   // índice en reto.estrategias (-1 = sin elegir)
    public RetoData.Estrategia EstrategiaElegida => reto.estrategias != null && Estrategia >= 0 && Estrategia < reto.estrategias.Length ? reto.estrategias[Estrategia] : null;
    public bool[] Evidencias { get; private set; }
    public int Pistas => metricas.Datos.pistasPedidas;
    public int Intentos => metricas.Datos.intentos.Count;
    public MetricsLogger Metricas => metricas;
    public string ObjetivoActual => BiohazardEvent.Activo ? BiohazardEvent.Instancia.Objetivo : Objetivo(Actual);
    public string PistaFallo => pistaFallo;      // pista ligada al último fallo (se muestra en Hipótesis)
    public bool JugadorDentro => dentro;

    // Misiones
    public int Mision => Actual >= EtapaReto.Hipotesis && Hipotesis != null ? 2 : 1;
    public string CodigoMision => Mision == 1 ? Misiones.Codigo1 : Misiones.Codigo2;
    public string TituloMision => Mision == 1 ? Misiones.Titulo1 : Misiones.Titulo2;
    public bool VistoHongo { get; private set; }
    public bool VistoVirus { get; private set; }
    public int Clasificadas { get; private set; }        // muestras A, B, C clasificadas bien en CONCLUSIONES (0-3)
    public bool ExitoAnalizado { get; private set; }     // misión 2: el ANTES | DESPUÉS del intento eficaz ya se miró
    public int Explicadas { get; private set; }          // misión 2: preguntas de CONCLUSIONES acertadas (0-2)
    public bool ComparacionCompleta => VistoHongo && VistoVirus && Evidencias.All(x => x);
    public bool FichaVista => metricas.Datos.fichasConsultadas.Count > 0;
    public bool PendienteAutoclave { get; private set; } // misión 2: explicado el resultado, falta esterilizar (AUTOCLAVE 11)

    MetricsLogger metricas;
    GuiaReto guia;                               // tarjeta de objetivo + ruta de flechas hasta la estación
    readonly Dictionary<EtapaReto, int> nivelPista = new Dictionary<EtapaReto, int>();
    bool dentro, sesionGuardada, cronometro;
    int ultimoAgente = -1;
    float ultimaEficacia;
    string ultimaEstrategia;
    bool ultimoCoherente;
    string pistaFallo, pistaPendiente;
    bool checkpointPendiente;
    string avisoMision;                          // "MISIÓN n COMPLETA…": se muestra al apartar la cara del ocular
    bool fallosClasificacion, fallosExplicacion; // para la métrica R: ¿acertó a la primera?
    string respuestaConclusiones;

    static readonly int[] EstacionDeEtapa = { 0, 1, 2, 3, 4, 5, 6, 2, 7 };
    static readonly int[] CicloDeEtapa = { 0, 1, 1, 2, 3, 4, 5, 6, 7 };
    static readonly string[] Tipos = { "BACTERIA", "HONGO", "VIRUS" };

    void Awake()
    {
        Instancia = this;
        Evidencias = new bool[reto.evidencias.Length];
        metricas = new MetricsLogger(Misiones.Codigo1);
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
        // Antes: panel pegado al mando izquierdo y columna de luz con un rombo. Ahora una tarjeta que aparece
        // al entrar y en cada paso (Y para verla) y flechas en el suelo hasta la estación.
        guia = GuiaReto.Crear();
        if (baliza != null) baliza.gameObject.SetActive(false);
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
            // El cronómetro de la misión 1 empieza la primera vez que se entra en el centro.
            cronometro = true;
            metricas = new MetricsLogger(Misiones.Codigo1);
            metricas.Etapa(Actual.ToString());
            Sonidos.Etapa(cab.position);
        }
        dentro = ahora;
        // Actividad del estudiante mientras la misión está en marcha (Engagement: tiempo inactivo).
        if (dentro && Actual != EtapaReto.Cierre) metricas.Muestrear(cab, ManosRig.ManoIzq, ManosRig.ManoDer, Time.deltaTime);
        if (cab == null || MicroscopioOptico.AlgunoActivo) return;
        var frente = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
        if (avisoMision != null)
        {
            Aviso.Mostrar(avisoMision, cab.position + frente * 0.9f + Vector3.down * 0.05f, Aviso.Tipo.Ok, 8f);
            Sonidos.Logro(cab.position);
            avisoMision = null;
        }
        if (pistaPendiente != null)
        {
            Aviso.Mostrar(pistaPendiente, cab.position + frente * 0.9f + Vector3.down * 0.2f, Aviso.Tipo.Pista, 10f);
            Sonidos.Pista(cab.position);
            pistaPendiente = null;
        }
        if (checkpointPendiente)
        {
            // Se avisa al apartar la cara del ocular (con el HUD delante no se vería).
            Aviso.Mostrar("CHECKPOINT GUARDADO\nPara volver aquí: botón de menú del mando izq. > CHECKPOINT.",
                cab.position + frente * 0.9f + Vector3.down * 0.42f, Aviso.Tipo.Ok, 6f);
            checkpointPendiente = false;
        }
    }

    // ------------------------------------------------------------------ etapas

    string Objetivo(EtapaReto e)
    {
        switch (e)
        {
            case EtapaReto.Exploracion:
                if (!EquiposCentro.Listo(EquiposCentro.Incubadora))
                    return "Las placas de las MUESTRAS A y B pasaron la noche en la INCUBADORA (9) a 37 °C. Ve a la incubadora y pulsa SACAR PLACAS.";
                return "En el BANCO (1) hay 3 muestras. Coge un portaobjetos de la mesa de TINCIÓN (2) y toca con él la placa de la MUESTRA A (herida de piel) para hacer el frotis.";
            case EtapaReto.Tincion: return "Tiñe el frotis de la MUESTRA A: toca los goteros en orden 1 > 2 > 3 > 4 (tinción de Gram).";
            case EtapaReto.Observacion:
            {
                int ev = Evidencias.Count(x => x);
                var falta = new List<string>();
                if (ev < Evidencias.Length) falta.Add($"evidencias de la A ({ev}/{Evidencias.Length})");
                if (!VistoHongo) falta.Add("la MUESTRA B");
                if (!VistoVirus) falta.Add("la MUESTRA C");
                string paso = PasoObservacion();
                return $"{paso} Te falta: {string.Join(", ", falta)}.";
            }
            case EtapaReto.Investigacion:
                if (Clasificadas < 3) return $"Ve al panel CONCLUSIONES (junto a los microscopios) y clasifica cada muestra: ¿bacteria, hongo o virus? ({Clasificadas}/3)";
                return "Busca en la BIBLIOTECA (4) la ficha que coincide con la MUESTRA A (forma, agrupación y color de Gram).";
            case EtapaReto.Hipotesis:
                if (Hipotesis == null) return "En la TERMINAL (5), fila 1: ¿qué bacteria es la MUESTRA A? Al acertar terminas la MISIÓN 1.";
                if (Intentos > 0) return $"Intento {Intentos + 1}: elige otra vez en la TERMINAL (5) qué estructura de {Hipotesis.nombre} atacarás (fila 2).";
                return $"MISIÓN 2 · Ya sabes que es {Hipotesis.nombre}. Elige en la TERMINAL (5) qué estructura atacarás (fila 2).";
            case EtapaReto.Preparacion: return EstrategiaElegida != null
                ? $"Estrategia: {EstrategiaElegida.nombre.ToLower()}. Busca el vial que lo hace, encájalo en la PRENSA (6) y pulsa FORMULAR."
                : "Elige un agente, encájalo en la PRENSA (6) y pulsa FORMULAR.";
            case EtapaReto.Experimento: return "Pon la placa de la MUESTRA A y la cápsula en la CÁMARA (7) y pulsa ACTIVAR.";
            case EtapaReto.Analisis:
                if (PendienteAutoclave) return "Último paso de la MISIÓN 2: esteriliza la placa tratada y el material. Ve al AUTOCLAVE (11) y pulsa ESTERILIZAR.";
                if (ExitoAnalizado) return $"Explica tu resultado en el panel CONCLUSIONES, junto a los microscopios ({Explicadas}/2).";
                return "Lleva el portaobjetos DESPUÉS a un MICROSCOPIO (3) y compáralo con el ANTES.";
            default: return "MISIONES 1 y 2 completas. Lee el informe en el ARCHIVO (8) y ve al LABORATORIO DE VACUNAS: la MISIÓN 3 está desbloqueada.";
        }
    }

    // Qué hacer ahora en la Observación (en orden: la A teñida, la B del banco, la C de la cabina).
    enum PasoObs { MuestraA, CargarB, VerB, Cabina, CargarC, VerC, Listo }
    PasoObs SiguienteObservacion()
    {
        if (!Evidencias.All(x => x)) return PasoObs.MuestraA;
        var portas = FindObjectsOfType<Portaobjetos>();
        if (!VistoHongo) return portas.Any(p => p.muestra != null && p.muestra == muestraHongo) ? PasoObs.VerB : PasoObs.CargarB;
        if (!VistoVirus)
        {
            if (!EquiposCentro.Listo(EquiposCentro.Cabina)) return PasoObs.Cabina;
            return portas.Any(p => p.muestra != null && p.muestra == muestraVirus) ? PasoObs.VerC : PasoObs.CargarC;
        }
        return PasoObs.Listo;
    }

    string PasoObservacion()
    {
        switch (SiguienteObservacion())
        {
            case PasoObs.MuestraA: return "Mira la MUESTRA A teñida en un MICROSCOPIO (3) a 400x y a 1000x.";
            case PasoObs.CargarB: return "Limpia un portaobjetos con el desinfectante (o coge otro) y toca la placa de la MUESTRA B en el BANCO (1).";
            case PasoObs.VerB: return "Lleva el portaobjetos de la MUESTRA B a un MICROSCOPIO (3).";
            case PasoObs.Cabina: return "La MUESTRA C está en la CABINA DE BIOSEGURIDAD (10): pulsa ENCENDER FLUJO antes de abrirla.";
            case PasoObs.CargarC: return "En la CABINA (10), toca el tubo de la MUESTRA C con un portaobjetos limpio.";
            case PasoObs.VerC: return "Lleva el portaobjetos de la MUESTRA C a un MICROSCOPIO (3).";
            default: return "Compara las 3 muestras en un MICROSCOPIO (3).";
        }
    }

    // Estación a la que apuntan las flechas: en Investigación y al explicar, el panel CONCLUSIONES; los equipos
    // (incubadora, cabina, autoclave) cuando toca usarlos.
    // Evento sorpresa de bioseguridad (BiohazardEvent): mientras dure la alarma, la misión 2 no avanza.
    public bool Bloqueado(out string motivo)
    {
        motivo = BiohazardEvent.Activo ? BiohazardEvent.Instancia.Bloqueo : null;
        return motivo != null;
    }

    void AvisarBloqueo(Vector3 donde)
    {
        if (!Bloqueado(out var m)) return;
        Aviso.Mostrar(m, donde, Aviso.Tipo.Error, 4f);
        Sonidos.Error(donde);
    }

    EstacionGuia EstacionActual()
    {
        if (BiohazardEvent.Activo) return BiohazardEvent.Instancia.GuiaActual;
        if (externa != null) return externa;
        if (Actual == EtapaReto.Exploracion && !EquiposCentro.Listo(EquiposCentro.Incubadora)) return EquiposCentro.Incubadora.guia;
        if (Actual == EtapaReto.Analisis && PendienteAutoclave && EquiposCentro.Autoclave != null) return EquiposCentro.Autoclave.guia;
        if (Actual == EtapaReto.Observacion)
        {
            var s = SiguienteObservacion();
            if ((s == PasoObs.Cabina || s == PasoObs.CargarC) && EquiposCentro.Cabina != null) return EquiposCentro.Cabina.guia;
            if (s == PasoObs.CargarB) return estaciones[0];
        }
        bool aConclusiones = (Actual == EtapaReto.Investigacion && Clasificadas < 3) || (Actual == EtapaReto.Analisis && ExitoAnalizado);
        if (aConclusiones && conclusiones != null) return conclusiones.guia;
        if (Actual == EtapaReto.Cierre) return null;   // ya no hay estación: lo siguiente es la vacuna (MISIÓN 3)
        int est = EstacionDeEtapa[(int)Actual];
        if (Actual == EtapaReto.Hipotesis && Hipotesis == null) est = 4;
        return est < estaciones.Length ? estaciones[est] : null;
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
        var destino = EstacionActual();
        for (int i = 0; i < estaciones.Length; i++) if (estaciones[i] != null) estaciones[i].Mostrar(estaciones[i] == destino, obj);
        if (conclusiones != null) conclusiones.guia.Mostrar(conclusiones.guia == destino, obj);
        var ev = BiohazardEvent.Instancia;
        if (ev != null) { ev.guiaKit.Mostrar(ev.guiaKit == destino, null); ev.guiaZona.Mostrar(ev.guiaZona == destino, null); }
        foreach (var eq in EquiposCentro.Todos) if (eq.guia != null) eq.guia.Mostrar(eq.guia == destino, eq.guia == destino ? (externa != null ? objetivoExterno : obj) : null);
        string titulo = Actual == EtapaReto.Cierre ? "MISIONES 1 y 2 COMPLETAS" : $"{CodigoMision} · {TituloMision.ToUpper()}";
        string estado = $"Intentos: {Intentos}   ·   Pistas: {Pistas}" + (pistaFallo != null && Actual == EtapaReto.Hipotesis ? $"\n<color=#FFD25A>PISTA: {pistaFallo}</color>" : "");
        foreach (var p in paneles) if (p != null) p.Mostrar(CicloDeEtapa[(int)Actual], titulo, obj, estado);
        if (guia != null)
        {
            if (externa != null) guia.Externo(tituloExterno, "MATERIAL DEL LABORATORIO", objetivoExterno, externa);
            else guia.Etapa(Actual, titulo, obj, destino, true);
        }
        if (terminal != null) terminal.Refrescar();
        RefrescarConclusiones();
        if (conSonido && ManosRig.Cabeza != null) Sonidos.Etapa(ManosRig.Cabeza.position);
        Cambio?.Invoke();
    }

    // Partida guardada. Lo que depende de objetos que no se guardan (el portaobjetos teñido en la mano,
    // la cápsula formulada, el portaobjetos DESPUÉS) se retoma desde el último punto que no los necesita.
    public void Restaurar(EtapaReto etapa, bool[] evidencias, string hipotesis, string estrategia = null)
    {
        for (int i = 0; i < Evidencias.Length && i < evidencias.Length; i++) Evidencias[i] = evidencias[i];
        Hipotesis = null;
        if (!string.IsNullOrEmpty(hipotesis) && reto.opciones != null)
            foreach (var o in reto.opciones) if (o != null && o.name == hipotesis) Hipotesis = o;
        // Desde que hay misiones solo se registra la identificación correcta (las partidas antiguas podían guardar otra).
        if (Hipotesis != reto.muestra) Hipotesis = null;
        // La estrategia se guarda por su diana (no por su posición), así sobrevive a reordenar los botones.
        Estrategia = -1;
        if (!string.IsNullOrEmpty(estrategia) && reto.estrategias != null)
            Estrategia = System.Array.FindIndex(reto.estrategias, s => s != null && s.diana.ToString() == estrategia);
        if (Hipotesis == null) Estrategia = -1;
        bool todas = Evidencias.All(x => x);
        // Una partida que ya pasó de la Observación vio las 3 muestras (si no, no habría avanzado).
        if (etapa >= EtapaReto.Investigacion) { VistoHongo = VistoVirus = true; }
        EtapaReto e;
        switch (etapa)
        {
            case EtapaReto.Cierre: e = EtapaReto.Cierre; break;
            case EtapaReto.Preparacion:
            case EtapaReto.Experimento:
            case EtapaReto.Analisis: e = Hipotesis != null && EstrategiaElegida != null ? EtapaReto.Preparacion : EtapaReto.Hipotesis; break;
            case EtapaReto.Investigacion:
            case EtapaReto.Hipotesis: e = etapa; break;
            default: e = todas && VistoHongo && VistoVirus ? EtapaReto.Investigacion : EtapaReto.Exploracion; break;
        }
        if (e >= EtapaReto.Investigacion && !todas) e = EtapaReto.Exploracion;
        if (e == EtapaReto.Exploracion && Actual == EtapaReto.Exploracion && !Evidencias.Any(x => x)) return;
        // La clasificación de CONCLUSIONES no se guarda aparte: a partir de la Hipótesis ya estaba hecha.
        Clasificadas = e >= EtapaReto.Hipotesis ? 3 : 0;
        Actual = e;
        metricas.Etapa(e.ToString());
        if (Hipotesis != null)
        {
            // La misión 1 ya estaba hecha: se sigue con las métricas de la misión 2.
            RecuerdosBaymax.Recuperar(Misiones.Recuerdo1);
            cronometro = true;
            if (metricas.Datos.caso != Misiones.Codigo2) EmpezarMision2(true);
        }
        if (e == EtapaReto.Cierre)
        {
            sesionGuardada = true;
            ExitoAnalizado = true; Explicadas = 2;
            RecuerdosBaymax.Recuperar(Misiones.Recuerdo2);
            MostrarResumen();
        }
        EquiposCentro.Sincronizar(this);
        Refrescar(false);
    }

    // Lo que la partida guarda aparte para la misión 1 (muestras B y C ya observadas).
    public void RestaurarComparacion(bool hongo, bool virus)
    {
        VistoHongo |= hongo;
        VistoVirus |= virus;
        EquiposCentro.Sincronizar(this);
        if (Actual == EtapaReto.Exploracion && ComparacionCompleta) Avanzar(EtapaReto.Investigacion);
        else Refrescar(false);
    }

    // Lo llama MontajeMisiones al empezar: muestras B y C y panel CONCLUSIONES de la misión 1.
    public void Conectar(PanelConclusiones panel, MuestraData hongo, MuestraData virus)
    {
        muestraHongo = hongo;
        muestraVirus = virus;
        if (conclusiones != null) conclusiones.Elegido -= AlElegirConclusion;
        conclusiones = panel;
        if (conclusiones != null) conclusiones.Elegido += AlElegirConclusion;
        Refrescar(false);
    }

    // Códigos de checkpoint de este gestor (el del caso, de partidas antiguas, y los de las misiones 1 y 2).
    public bool EsSuyo(string codigo) => codigo == reto.codigo || codigo == Misiones.Codigo1 || codigo == Misiones.Codigo2;

    // ------------------------------------------------------------------ eventos de las estaciones

    bool EsMuestraMision(Portaobjetos p) =>
        p != null && p.DeCaso && p.muestra != null && (p.muestra == reto.muestra || p.muestra == muestraHongo || p.muestra == muestraVirus);

    void AlUsarFuente(FuenteMuestra f, Portaobjetos p)
    {
        if (f.muestra == reto.muestra && f.reactivo == 0 && !f.esDesinfectante && p != null && p.DeCaso)
        {
            metricas.Empezar();
            metricas.Actividad();
            Avanzar(EtapaReto.Tincion);
        }
        else if (f.reactivo == 0 && !f.esDesinfectante && p != null && (f.muestra == muestraHongo || f.muestra == muestraVirus))
        {
            metricas.Empezar();
            metricas.Indagacion($"Frotis: {f.etiqueta}");
            Refrescar(false);
        }
        else if (f.esDesinfectante && Actual == EtapaReto.Observacion) Refrescar(false);
    }

    void AlTenir(Portaobjetos p)
    {
        if (p.muestra != reto.muestra || !p.DeCaso || p.Despues) return;
        metricas.Actividad();
        if (p.TincionErronea) { metricas.Datos.tincionesErroneas++; metricas.Error("Tincion"); }
        if (p.TincionCompleta) Avanzar(EtapaReto.Observacion);
    }

    Portaobjetos observando;

    void AlObservar(MicroscopioOptico m, Portaobjetos p)
    {
        if (!EsMuestraMision(p)) { observando = null; return; }
        observando = p;
        metricas.Datos.observacionesMicroscopio++;
        var hud = HUDMicroscopio.Instancia;
        if (p.Despues)
        {
            if (BiohazardEvent.Activo) { AvisarBloqueo(m.transform.position + Vector3.up * 0.5f); return; }
            hud.MostrarReto(p.muestra, DatosComparacion);
            if (Actual == EtapaReto.Analisis && !ExitoAnalizado) AnalisisCompletado();
            return;
        }
        if (p.muestra != reto.muestra)
        {
            // MUESTRA B (moho) o C (hisopado): se registra al mirarla y se compara con las demás.
            bool nueva = p.muestra == muestraHongo ? !VistoHongo : !VistoVirus;
            if (p.muestra == muestraHongo) VistoHongo = true; else VistoVirus = true;
            metricas.Indagacion($"Microscopio: {p.Etiqueta}");
            if (nueva) metricas.PrimerExito();
            hud.MostrarReto(p.muestra, DatosOtraMuestra);
            if (nueva) Sonidos.Carga(ManosRig.Cabeza != null ? ManosRig.Cabeza.position : transform.position);
            ComprobarObservacion();
            return;
        }
        EvaluarEvidencias(p, hud.Aumento);
        hud.MostrarReto(p.muestra, DatosObservacion);
    }

    void AlCambiarAumento(int aumento)
    {
        metricas.Datos.maxAumentoUsado = Mathf.Max(metricas.Datos.maxAumentoUsado, aumento);
        if (observando == null || observando.Despues || observando.muestra != reto.muestra || HUDMicroscopio.Instancia == null) return;
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
            metricas.Indagacion("Evidencia: " + ev.clave);
            metricas.PrimerExito();
        }
        if (!cambio) return false;
        Sonidos.Carga(ManosRig.Cabeza != null ? ManosRig.Cabeza.position : transform.position);
        ComprobarObservacion();
        return true;
    }

    // Misión 1: se pasa a Investigación con las evidencias de la A y las muestras B y C observadas.
    void ComprobarObservacion()
    {
        if (Actual == EtapaReto.Observacion && ComparacionCompleta) Avanzar(EtapaReto.Investigacion);
        else Refrescar(false);
    }

    public void FichaConsultada(string id)
    {
        metricas.Agregar(metricas.Datos.fichasConsultadas, id);
        metricas.Indagacion("Ficha: " + id);
        Sonidos.Clic(transform.position);
        ComprobarInvestigacion();
    }

    // Misión 1: se pasa a identificar en la terminal con las 3 muestras clasificadas y una ficha consultada.
    void ComprobarInvestigacion()
    {
        if (Actual == EtapaReto.Investigacion && Clasificadas >= 3 && FichaVista) Avanzar(EtapaReto.Hipotesis);
        else Refrescar(false);
    }

    public bool PuedeHipotesis(out string motivo)
    {
        motivo = null;
        if (Bloqueado(out motivo)) return false;
        if (Actual == EtapaReto.Cierre) motivo = "Las misiones 1 y 2 ya están completas. Pulsa REINICIAR en el ARCHIVO (8) para repetirlas.";
        else if (Actual == EtapaReto.Analisis) motivo = "Antes de cambiar tu hipótesis, analiza el resultado: mira el portaobjetos DESPUÉS en un MICROSCOPIO (3).";
        else if (!Evidencias.All(x => x))
        {
            var faltan = reto.evidencias.Where((e, i) => !Evidencias[i]).Select(e => e.clave.ToLower());
            motivo = $"Aún no tienes evidencia suficiente. Observa la MUESTRA A teñida al microscopio. Te falta: {string.Join(", ", faltan)}.";
        }
        else if (!VistoHongo || !VistoVirus) motivo = "Antes de decidir, compara la MUESTRA A con la MUESTRA B y la MUESTRA C en el microscopio (3).";
        else if (Clasificadas < 3) motivo = "Antes de identificar la A, clasifica las 3 muestras en el panel CONCLUSIONES (junto a los microscopios).";
        // La hipótesis se apoya también en la información consultada (RF-05): primero una ficha de la biblioteca.
        else if (Actual < EtapaReto.Hipotesis) motivo = "Ya tienes tus evidencias. Antes de decidir, compáralas con una ficha de la BIBLIOTECA (4).";
        return motivo == null;
    }

    public bool PuedeEstrategia(out string motivo)
    {
        if (!PuedeHipotesis(out motivo)) return false;
        if (Hipotesis == null) motivo = "Primero identifica en la fila 1 qué bacteria es la MUESTRA A (así terminas la MISIÓN 1).";
        return motivo == null;
    }

    Vector3 SobreTerminal => terminal != null ? terminal.Encima : transform.position;

    // Misión 1: la identificación se comprueba. Si no coincide con las evidencias, se explica por qué y no se registra.
    public bool RegistrarHipotesis(MuestraData m)
    {
        metricas.Datos.hipotesis.Add(m.nombre);
        metricas.Actividad();
        if (m != reto.muestra)
        {
            metricas.Error("Identificacion");
            Aviso.Mostrar($"AÚN NO. {PorQueNo(m)}", SobreTerminal, Aviso.Tipo.Error, 8f);
            Refrescar(false);
            return false;
        }
        bool primera = Hipotesis == null;
        Hipotesis = m;
        pistaFallo = null;
        if (primera) CompletarMision1();
        if (EstrategiaElegida != null && Actual < EtapaReto.Preparacion) Poner(EtapaReto.Preparacion);
        else Refrescar(false);
        return true;
    }

    // Por qué una identificación no encaja con las evidencias de la A (forma, agrupación, Gram).
    string PorQueNo(MuestraData m)
    {
        string forma = m.caracteristicas?.FirstOrDefault(d => d.clave == "Forma").valor ?? "";
        string gram = m.gramPositiva ? "Gram + (violeta)" : "Gram - (rosa)";
        return $"La ficha de {m.nombre} dice: {(string.IsNullOrEmpty(forma) ? "" : forma.ToLower() + ", ")}{gram}. " +
               $"Tus evidencias de la MUESTRA A: {string.Join(" · ", reto.evidencias.Select(e => e.valor))}. ¿Qué ficha coincide?";
    }

    public void RegistrarEstrategia(int i)
    {
        if (reto.estrategias == null || i < 0 || i >= reto.estrategias.Length) return;
        Estrategia = i;
        var s = reto.estrategias[i];
        metricas.Datos.estrategias.Add(s.nombre);
        metricas.Actividad();
        pistaFallo = null;
        Aviso.Mostrar($"Estrategia: {s.nombre.ToLower()}. Busca en la PRENSA (6) el vial que lo hace y formula la cápsula.", SobreTerminal, Aviso.Tipo.Ok);
        if (Actual < EtapaReto.Preparacion) Poner(EtapaReto.Preparacion);
        else Refrescar(false);
    }

    // ¿El agente ataca la estructura que el estudiante decidió atacar?
    public bool EsCoherente(int agente) =>
        EstrategiaElegida != null && agente >= 0 && agente < reto.agentes.Length && reto.agentes[agente].diana == EstrategiaElegida.diana;

    public bool PuedeFormular(out string motivo)
    {
        if (Bloqueado(out motivo)) return false;
        motivo = null;
        if (Actual == EtapaReto.Cierre) motivo = "Las misiones 1 y 2 ya están completas. Pulsa REINICIAR en el ARCHIVO (8) para repetirlas.";
        else if (Actual == EtapaReto.Analisis) motivo = ExitoAnalizado
            ? "Tu agente ya funcionó: explica el resultado en el panel CONCLUSIONES (junto a los microscopios)."
            : "Antes de probar otro agente, analiza el resultado: mira el portaobjetos DESPUÉS en un MICROSCOPIO (3).";
        else if (Hipotesis == null || Actual < EtapaReto.Preparacion) motivo = Hipotesis == null
            ? "Primero termina la MISIÓN 1: identifica la MUESTRA A en la TERMINAL (5)."
            : "Primero elige tu estrategia en la TERMINAL (5): ¿qué estructura atacarás?";
        return motivo == null;
    }

    public void CapsulaFormulada(int agente) { metricas.Actividad(); Avanzar(EtapaReto.Experimento); }

    public bool PuedeExperimentar(out string motivo)
    {
        motivo = null;
        if (Bloqueado(out motivo)) return false;
        if (Actual == EtapaReto.Cierre) motivo = "Las misiones 1 y 2 ya están completas. Pulsa REINICIAR en el ARCHIVO (8) para repetirlas.";
        else if (Actual == EtapaReto.Analisis) motivo = ExitoAnalizado
            ? "Tu agente ya funcionó: explica el resultado en el panel CONCLUSIONES."
            : "Primero analiza el resultado anterior: mira el portaobjetos DESPUÉS en un MICROSCOPIO (3).";
        else if (Hipotesis == null) motivo = "Primero termina la MISIÓN 1: identifica la MUESTRA A en la TERMINAL (5).";
        return motivo == null;
    }

    // Pista tras un fallo. Si la estrategia era la buena pero el vial no la seguía, se dice eso; si la estrategia
    // era la equivocada, la pista del agente explica por qué esa estructura no sirve con esta muestra.
    string PistaDelFallo()
    {
        var a = reto.agentes[ultimoAgente];
        var buena = reto.EstrategiaDe(reto.DianaCorrecta);
        if (!ultimoCoherente && ultimaEstrategia != null && buena != null && ultimaEstrategia == buena.nombre)
        {
            var real = reto.EstrategiaDe(a.diana);
            return $"Tu estrategia ({ultimaEstrategia.ToLower()}) era buena, pero el agente {a.sigla} no hace eso" +
                   (real != null ? $": {real.nombre.ToLower()}." : ".") + " Busca en el tablero AGENTES DISPONIBLES el vial que sigue tu estrategia.";
        }
        // Pista creativa: el moho de la MUESTRA B tenía un halo sin bacterias alrededor (Fleming, 1928).
        return a.pistaSiFalla + (VistoHongo ? " ¿Recuerdas la MUESTRA B? Alrededor del moho no crecía ninguna colonia: algo rompía a las bacterias." : "");
    }

    public void ResultadoExperimento(int agente, float eficacia)
    {
        ultimoAgente = agente;
        ultimaEficacia = eficacia;
        ultimaEstrategia = EstrategiaElegida != null ? EstrategiaElegida.nombre : null;
        ultimoCoherente = EsCoherente(agente);
        var a = reto.agentes[agente];
        metricas.Datos.intentos.Add(new MetricsLogger.Intento
        {
            numero = Intentos + 1, hipotesis = Hipotesis != null ? Hipotesis.nombre : "",
            estrategia = EstrategiaElegida != null ? EstrategiaElegida.nombre : "", agente = a.sigla, coherente = EsCoherente(agente),
            eficacia = eficacia, exito = Exito, minuto = metricas.Minutos,
        });
        metricas.Actividad();
        ExitoAnalizado = false;
        Explicadas = 0;
        Poner(EtapaReto.Analisis);
        // Evento sorpresa: tras el PRIMER experimento falla el sellado de la cámara (una vez por partida).
        if (metricas.Datos.intentos.Count == 1 && camara != null) BiohazardEvent.Disparar(camara.transform);
    }

    // Avisos del evento de bioseguridad (alarma, spray tomado, zona limpiada).
    public void BiohazardCambio(bool conSonido)
    {
        metricas.Actividad();
        Refrescar(conSonido);
    }

    public void BiohazardResuelto(float segundosHastaSpray, float segundosTotal, int zonas)
    {
        metricas.Actividad();
        metricas.Datos.bioseguridad = new MetricsLogger.Bioseguridad
        {
            ocurrido = true, segundosHastaSpray = segundosHastaSpray, segundosLimpieza = segundosTotal, zonas = zonas, minuto = metricas.Minutos,
        };
        Refrescar(true);
    }

    bool Exito => ultimoAgente >= 0 && ultimaEficacia >= reto.umbralExito;

    void AnalisisCompletado()
    {
        metricas.Datos.comparacionAntesDespues = true;
        metricas.Indagacion("Microscopio: ANTES | DESPUÉS");
        if (Exito)
        {
            // Misión 2: falta explicar el resultado en CONCLUSIONES (Retention: relacionar lo observado con el efecto).
            ExitoAnalizado = true;
            Explicadas = 0;
            fallosExplicacion = false;
            respuestaConclusiones = null;
            pistaPendiente = "¡El agente funcionó! Ahora explica por qué en el panel CONCLUSIONES, junto a los microscopios.";
            Refrescar(true);
        }
        else
        {
            // El error es información: pista ligada a la evidencia y nueva placa para reintentar.
            metricas.Error("Experimento");
            pistaFallo = PistaDelFallo();
            if (placa != null) placa.Reiniciar();
            Estrategia = -1;   // la hipótesis se conserva, pero la estrategia hay que repensarla
            Poner(EtapaReto.Hipotesis);
            pistaPendiente = pistaFallo;   // se muestra al apartar la cara del ocular (el HUD ya la explica)
        }
    }

    // ------------------------------------------------------------------ equipos del centro (EquiposCentro)

    public void EquipoUsado()
    {
        metricas.Actividad();
        metricas.Indagacion("Equipo usado");
        Refrescar(true);
    }

    public void Esterilizado()
    {
        if (!PendienteAutoclave) return;
        PendienteAutoclave = false;
        metricas.Actividad();
        CompletarMision2();
        RefrescarConclusiones();
    }

    // MISIÓN 3: la guía de flechas lleva a un equipo del centro (ultracongelador, incubadora CO2). null = vuelve a lo normal.
    EstacionGuia externa;
    string tituloExterno, objetivoExterno;
    public void GuiaExterna(EstacionGuia est, string titulo, string objetivo)
    {
        externa = est;
        tituloExterno = titulo;
        objetivoExterno = objetivo;
        Refrescar(est != null);
    }

    // ------------------------------------------------------------------ misiones

    void CompletarMision1()
    {
        RecuerdosBaymax.Recuperar(Misiones.Recuerdo1);
        if (!sesionGuardada)
        {
            metricas.Datos.totalComprension = 3;
            metricas.Datos.postAciertos = fallosClasificacion ? Mathf.Max(0, metricas.Datos.postAciertos) : 3;
            metricas.Guardar(true);
        }
        avisoMision = $"MISIÓN 1 COMPLETA · ¡Es {reto.muestra.nombre}!\nRECUERDO CIENTÍFICO {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}\n<size=70%>{Misiones.TextoRecuerdo1}</size>";
        checkpointPendiente = Partida.GuardarCheckpoint(Misiones.Codigo1);
        EmpezarMision2(true);
    }

    void EmpezarMision2(bool nueva)
    {
        if (nueva || metricas.Datos.caso != Misiones.Codigo2)
        {
            metricas = new MetricsLogger(Misiones.Codigo2);
            metricas.Etapa(Actual.ToString());
            metricas.Empezar();
        }
        sesionGuardada = false;
        respuestaConclusiones = null;
    }

    void CompletarMision2()
    {
        RecuerdosBaymax.Recuperar(Misiones.Recuerdo2);
        Poner(EtapaReto.Cierre);
        MostrarResumen();
        if (!sesionGuardada)
        {
            metricas.Datos.totalComprension = 2;
            metricas.Datos.postAciertos = fallosExplicacion ? Mathf.Max(0, metricas.Datos.postAciertos) : 2;
            metricas.Guardar(true);
            sesionGuardada = true;
        }
        avisoMision = $"MISIÓN 2 COMPLETA · RECUERDO CIENTÍFICO {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}\n<size=70%>{Misiones.TextoRecuerdo2}</size>\n" +
                      "<size=80%><color=#FFD25A>MISIÓN 3 DESBLOQUEADA: ve al LABORATORIO DE VACUNAS.</color></size>";
        // Checkpoint: cada misión terminada deja un punto de guardado al que se puede volver.
        checkpointPendiente = Partida.GuardarCheckpoint(Misiones.Codigo2);
    }

    // ------------------------------------------------------------------ panel CONCLUSIONES

    MuestraData MuestraDe(int i) => i == 0 ? reto.muestra : i == 1 ? muestraHongo : muestraVirus;
    static readonly string[] Letras = { "A", "B", "C" };
    static readonly int[] TipoCorrecto = { 0, 1, 2 };   // A bacteria, B hongo, C virus

    // Por qué cada respuesta (muestra, opción) es o no correcta.
    static string Explicacion(int muestra, int tipo)
    {
        switch (muestra * 3 + tipo)
        {
            case 0: return "Células esféricas de ~1 µm, sin núcleo, en racimos: son bacterias (cocos).";
            case 1: return "Los hongos son mucho más grandes: forman filamentos (hifas) o levaduras con núcleo. Aquí hay esferas diminutas en racimo.";
            case 2: return "Si fuera un virus no lo verías: miden ~0,1 µm. Esto sí se ve a 1000x.";
            case 3: return "Las bacterias son células sueltas de ~1 µm. Aquí hay filamentos largos y ramificados, muchísimo más grandes.";
            case 4: return "Filamentos ramificados (hifas) y esporas en forma de pincel: es un moho, un HONGO (Penicillium = \"pincelito\"). Sus células tienen núcleo, como las nuestras.";
            case 5: return "Los virus no se ven al microscopio óptico. Esto se ve, y es grande: filamentos con esporas.";
            case 6: return "A 1000x verías las bacterias como en la MUESTRA A. Aquí solo hay células de la nariz.";
            case 7: return "No hay filamentos ni levaduras: no es un hongo.";
            default: return "Solo hay células de la nariz y ningún microbio, aunque el paciente está enfermo: es un VIRUS, demasiado pequeño (~0,1 µm) para este microscopio.";
        }
    }

    static readonly (string pregunta, string[] opciones, int correcta, string[] porque)[] PreguntasExplicar =
    {
        ("¿Por qué el agente funcionó con la MUESTRA A?",
         new[] { "Pared gruesa", "Es un virus", "Fue suerte" }, 0,
         new[]
         {
             "Sí: S. aureus tiene una pared gruesa de peptidoglucano (por eso quedó VIOLETA en la tinción de Gram) y el agente la rompe. Como el moho de la MUESTRA B: su halo era una sustancia que rompe esa pared, la PENICILINA (Fleming, 1928).",
             "La MUESTRA A es una bacteria (la viste a 1000x). Piensa en la tinción de Gram: ¿qué tiene de especial su pared?",
             "No fue suerte: los agentes que atacaban otra estructura no le hicieron nada. ¿Qué estructura tiene la A que el agente rompe?",
         }),
        ("¿Serviría este agente contra el virus del brote (MUESTRA C)?",
         new[] { "Sí, igual", "No", "Solo más fuerte" }, 1,
         new[]
         {
             "No: los virus no tienen pared de peptidoglucano. Por eso los antibióticos no sirven contra un virus.",
             "Correcto: los virus no tienen pared de peptidoglucano y los antibióticos no les hacen nada. Contra el virus del brote hay que prevenir: una VACUNA (MISIÓN 3).",
             "Más cantidad no sirve: el virus no tiene la estructura que el agente ataca.",
         }),
    };

    void AlElegirConclusion(int opcion)
    {
        if (BiohazardEvent.Activo) { AvisarBloqueo(conclusiones != null ? conclusiones.transform.position + Vector3.up * 1.5f : transform.position); return; }
        var pos = conclusiones != null ? conclusiones.Encima : transform.position;
        metricas.Actividad();
        if (Actual == EtapaReto.Investigacion && Clasificadas < 3)
        {
            int m = Clasificadas;
            bool bien = opcion == TipoCorrecto[m];
            string porque = Explicacion(m, opcion);
            if (bien)
            {
                Clasificadas++;
                metricas.Indagacion($"Conclusiones: muestra {Letras[m]} = {Tipos[opcion]}");
                Sonidos.Carga(pos);
                ManosRig.Vibrar(pos, 0.4f, 0.08f);
                respuestaConclusiones = $"<color=#39FF88><b>CORRECTO · MUESTRA {Letras[m]} = {Tipos[opcion]}.</b></color> {porque}";
                if (Clasificadas == 3)
                {
                    metricas.Datos.totalComprension = 3;
                    if (!fallosClasificacion) metricas.Datos.postAciertos = 3;
                    respuestaConclusiones += "\n<color=#FFD25A>Ahora busca en la BIBLIOTECA (4) qué bacteria es la A.</color>";
                }
                ComprobarInvestigacion();
            }
            else
            {
                if (!fallosClasificacion) metricas.Datos.postAciertos = m;   // aciertos a la primera antes del fallo
                fallosClasificacion = true;
                metricas.Error("Clasificacion");
                Sonidos.Error(pos);
                ManosRig.Vibrar(pos, 0.3f, 0.1f);
                respuestaConclusiones = $"<color=#FF8A70><b>AÚN NO.</b></color> {porque}";
                RefrescarConclusiones();
            }
            return;
        }
        if (Actual == EtapaReto.Analisis && ExitoAnalizado && Explicadas < PreguntasExplicar.Length)
        {
            var q = PreguntasExplicar[Explicadas];
            if (opcion < 0 || opcion >= q.opciones.Length) return;
            if (opcion == q.correcta)
            {
                Explicadas++;
                Sonidos.Carga(pos);
                ManosRig.Vibrar(pos, 0.4f, 0.08f);
                respuestaConclusiones = $"<color=#39FF88><b>¡BIEN!</b></color> {q.porque[opcion]}";
                if (Explicadas >= PreguntasExplicar.Length)
                {
                    if (EquiposCentro.Autoclave != null && !PendienteAutoclave)
                    {
                        PendienteAutoclave = true;
                        respuestaConclusiones = $"<color=#39FF88><b>¡BIEN!</b></color> {q.porque[opcion]}\n<color=#FFD25A>Último paso: esteriliza la placa tratada en el AUTOCLAVE (11).</color>";
                        Refrescar(true);
                    }
                    else { CompletarMision2(); respuestaConclusiones = $"<color=#39FF88><b>¡BIEN!</b></color> {q.porque[opcion]}"; RefrescarConclusiones(); }
                }
                else Refrescar(false);
            }
            else
            {
                if (!fallosExplicacion) metricas.Datos.postAciertos = Explicadas;
                fallosExplicacion = true;
                metricas.Error("Explicacion");
                Sonidos.Error(pos);
                respuestaConclusiones = $"<color=#FF8A70><b>AÚN NO.</b></color> {q.porque[opcion]}";
                RefrescarConclusiones();
            }
            return;
        }
        Sonidos.Clic(pos);
        Aviso.Mostrar(Actual < EtapaReto.Investigacion ? "Primero compara las 3 muestras al microscopio (3)." : ObjetivoActual, pos, Aviso.Tipo.Info, 4f);
    }

    // Cuadro comparativo: lo observado de cada muestra (sin decir qué es hasta clasificarla).
    string CuadroComparativo()
    {
        var sb = new StringBuilder();
        string Fila(string letra, bool vista, string origen, string visto, int indice)
        {
            string tipo = Clasificadas > indice || Actual >= EtapaReto.Hipotesis ? $"  <color=#39FF88>[{Tipos[TipoCorrecto[indice]]}]</color>" : "";
            return vista ? $"<b>{letra}</b> <size=85%>({origen})</size>: {visto}{tipo}"
                         : $"<b>{letra}</b> <size=85%>({origen})</size>: <color=#8095AA>sin observar</color>";
        }
        bool vistaA = Evidencias.Any(x => x);
        string a = vistaA ? string.Join(" · ", reto.evidencias.Select((e, i) => Evidencias[i] ? e.valor : "?")) + ", ~1 µm" : "";
        sb.AppendLine(Fila("A", vistaA, "herida", a, 0));
        sb.AppendLine(Fila("B", VistoHongo, "moho", "filamentos ramificados de 3-5 µm con esporas en pincel; alrededor del moho no crecen colonias (halo)", 1));
        sb.AppendLine(Fila("C", VistoVirus, "nariz", "solo células grandes de la nariz (~50 µm); ningún microbio visible a 1000x", 2));
        return sb.ToString();
    }

    void RefrescarConclusiones()
    {
        if (conclusiones == null) return;
        if (Actual == EtapaReto.Investigacion && Clasificadas < 3)
        {
            conclusiones.Mostrar($"{Misiones.Codigo1} · CLASIFICA LAS MUESTRAS ({Clasificadas}/3)", CuadroComparativo(),
                $"¿Qué hay en la MUESTRA {Letras[Clasificadas]}?", Tipos, respuestaConclusiones, true);
        }
        else if (Actual == EtapaReto.Analisis && ExitoAnalizado && Explicadas < PreguntasExplicar.Length)
        {
            var q = PreguntasExplicar[Explicadas];
            conclusiones.Mostrar($"{Misiones.Codigo2} · EXPLICA TU RESULTADO ({Explicadas}/{PreguntasExplicar.Length})",
                "<b>ANTES:</b> cocos violetas en racimo.  <b>DESPUÉS:</b> células rotas: el agente funcionó.\n" + CuadroComparativo(), q.pregunta, q.opciones, respuestaConclusiones, true);
        }
        else
        {
            string pie = Actual < EtapaReto.Investigacion ? "<color=#A9BED2>Observa las muestras A, B y C al microscopio (3). Aquí se irá llenando el cuadro.</color>"
                       : Actual == EtapaReto.Cierre ? "<color=#39FF88>Misiones 1 y 2 completas. La MISIÓN 3 te espera en el LABORATORIO DE VACUNAS.</color>"
                       : $"<color=#A9BED2>{ObjetivoActual}</color>";
            conclusiones.Mostrar("CUADRO COMPARATIVO DE MUESTRAS", CuadroComparativo(), "", null, respuestaConclusiones ?? pie, false);
        }
    }

    public string PedirPista()
    {
        string[] lista;
        switch (Actual)
        {
            case EtapaReto.Exploracion: lista = !EquiposCentro.Listo(EquiposCentro.Incubadora) ? new[]
            {
                "La INCUBADORA es el armario blanco con puerta de vidrio que marca 37 °C (número 9). Pulsa el botón amarillo de su repisa.",
                "Sigue las flechas del suelo: llevan a la INCUBADORA (9). Las bacterias crecen a la temperatura del cuerpo, por eso las placas estaban ahí.",
            } : new[]
            {
                "Busca el BANCO DE MUESTRAS (zona 1): la placa de la izquierda es la MUESTRA A. Coge un portaobjetos de la mesa de TINCIÓN (2) y toca la placa con él.",
                reto.pistasExploracion != null && reto.pistasExploracion.Length > 1 ? reto.pistasExploracion[1] : "Lleva el portaobjetos en la mano y tócalo con la placa: el frotis aparece en el vidrio.",
            }; break;
            case EtapaReto.Tincion: lista = reto.pistasTincion; break;
            case EtapaReto.Observacion:
                lista = SiguienteObservacion() == PasoObs.Cabina
                    ? new[] { "La CABINA DE BIOSEGURIDAD (10) es la vitrina blanca con luz azul. Pulsa el botón amarillo de su mesa para encender el flujo.",
                              "Un virus respiratorio puede viajar en gotitas: el flujo de aire de la cabina las aleja de tu cara antes de abrir el tubo." }
                    : Evidencias.All(x => x)
                    ? new[] { "Ya tienes las evidencias de la A. Limpia un portaobjetos con el desinfectante (o coge otro), toca la placa B o el tubo C del BANCO (1) y míralo al microscopio.",
                              "La B y la C no hace falta teñirlas. Míralas una detrás de otra: el HUD te deja comparar con la anterior." }
                    : reto.pistasObservacion; break;
            case EtapaReto.Investigacion:
                lista = Clasificadas < 3
                    ? new[] { "En el panel CONCLUSIONES compara el tamaño: los virus no se ven, las bacterias miden ~1 µm y los hongos forman filamentos grandes.",
                              "A = bacteria (cocos en racimo), B = hongo (filamentos y esporas), C = virus (no se ve nada aunque el paciente está enfermo)." }
                    : reto.pistasInvestigacion; break;
            case EtapaReto.Hipotesis:
                if (Hipotesis == null) { lista = reto.pistasHipotesis != null && reto.pistasHipotesis.Length > 0 ? new[] { reto.pistasHipotesis[0] } : new[] { ObjetivoActual }; break; }
                var resto = reto.pistasHipotesis != null ? reto.pistasHipotesis.Skip(1).ToArray() : new string[0];
                lista = pistaFallo != null ? new[] { pistaFallo }.Concat(resto).ToArray() : resto; break;
            case EtapaReto.Preparacion: lista = reto.pistasPreparacion; break;
            case EtapaReto.Experimento: lista = reto.pistasExperimento; break;
            case EtapaReto.Analisis:
                lista = PendienteAutoclave
                    ? new[] { "El AUTOCLAVE (11) es el equipo blanco con puerta redonda. Pulsa el botón amarillo de encima: ESTERILIZAR.",
                              "Todo lo que tocó una bacteria se esteriliza con vapor a 121 °C antes de tirarlo: así no sale nada vivo del laboratorio." }
                    : ExitoAnalizado
                    ? new[] { "Piensa en la tinción de Gram: la A quedó VIOLETA porque tiene una pared gruesa. ¿Qué hizo el agente con ella?",
                              "Los virus no tienen pared de peptidoglucano: un agente que rompe esa pared no les hace nada." }
                    : reto.pistasAnalisis; break;
            default: return "Ya completaste las misiones 1 y 2. Ve al LABORATORIO DE VACUNAS (MISIÓN 3) o repítelas desde el ARCHIVO (8).";
        }
        if (lista == null || lista.Length == 0) return ObjetivoActual;
        nivelPista.TryGetValue(Actual, out int n);
        string p = lista[Mathf.Min(n, lista.Length - 1)];
        nivelPista[Actual] = n + 1;
        metricas.Datos.pistasPedidas++;
        metricas.Datos.pistas.Add($"{Actual}: {p}");
        metricas.Actividad();
        Refrescar(false);
        return p;
    }

    public void Reiniciar()
    {
        if (!sesionGuardada) metricas.Guardar(false);
        metricas = new MetricsLogger(Misiones.Codigo1);
        sesionGuardada = false;
        Hipotesis = null;
        Estrategia = -1;
        Evidencias = new bool[reto.evidencias.Length];
        VistoHongo = VistoVirus = false;
        Clasificadas = 0; Explicadas = 0; ExitoAnalizado = false; PendienteAutoclave = false;
        EquiposCentro.ReiniciarMisiones();
        BiohazardEvent.Reiniciar();
        fallosClasificacion = fallosExplicacion = false;
        respuestaConclusiones = null; avisoMision = null;
        nivelPista.Clear();
        ultimaEstrategia = null; ultimoCoherente = false;
        ultimoAgente = -1; ultimaEficacia = 0f; pistaFallo = null; pistaPendiente = null; checkpointPendiente = false;
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

    // Imagen de otra muestra ya observada para comparar en el HUD (a la derecha, al mismo aumento).
    void Comparar(ref HUDMicroscopio.DatosReto d, MuestraData actual)
    {
        MuestraData otra = null; string letra = null;
        if (actual != reto.muestra && Evidencias.Any(x => x)) { otra = reto.muestra; letra = "A"; }
        else if (actual != muestraHongo && VistoHongo) { otra = muestraHongo; letra = "B"; }
        else if (actual != muestraVirus && VistoVirus) { otra = muestraVirus; letra = "C"; }
        if (otra == null || otra.micrografia == null) return;
        d.antes = otra.micrografia;
        d.antesTitulo = $"COMPARA CON LA MUESTRA {letra}";
    }

    HUDMicroscopio.DatosReto DatosOtraMuestra(int aumento)
    {
        var p = observando;
        bool hongo = p.muestra == muestraHongo;
        string letra = hongo ? "B" : "C";
        var d = new HUDMicroscopio.DatosReto
        {
            subtitulo = $"{Misiones.Codigo1}  ·  MUESTRA {letra}  ·  {(hongo ? Misiones.OrigenB : Misiones.OrigenC).ToUpper()}",
            vista = p.muestra.micrografia,
            riesgo = "POR CLASIFICAR",
            riesgoClave = "Tipo",
            colorRiesgo = new Color(1f, 0.75f, 0.3f),
            claves = hongo
                ? new[] { "Origen", "¿Se ve algo?", "Forma", "Tamaño", "Alrededor" }
                : new[] { "Origen", "¿Se ve algo?", "Qué hay", "Tamaño", "Microbios" },
            valores = hongo
                ? new[] { "placa olvidada", "sí, y es grande", "filamentos ramificados", "3-5 µm de ancho", "halo sin colonias" }
                : new[] { "nariz (paciente del brote)", "solo células", "células de la mucosa", "~50 µm", "ninguno visible" },
            cuadernoTitulo = "CUADRO COMPARATIVO",
            cuaderno = CuadroComparativo(),
            obsTitulo = "PIENSA",
            obs = hongo
                ? "¿Se parece a las bacterias de la A? Fíjate en el tamaño y en los filamentos con bolitas en forma de pincel. Y en la placa: cerca del moho no crecía ninguna colonia."
                : "El paciente está enfermo, pero a 1000x no ves ningún microbio. ¿Qué microbio sería tan pequeño que la luz no lo puede mostrar?",
            aviso = $"MUESTRA {letra} REGISTRADA · " + (ComparacionCompleta ? "Comparación completa: ve al panel CONCLUSIONES" : "Compárala con las demás"),
            colorAviso = new Color(0.35f, 1f, 0.55f),
            guia = "",
        };
        Comparar(ref d, p.muestra);
        if (d.antes == null) d.guia = "• ¿Qué forma tiene lo que ves?\n• ¿Es más grande o más pequeño que una bacteria (~1 µm)?\n• ¿Hay algún microbio?\n\n<color=#8FD8FF>A: más aumento   B / Y: menos   Joystick izq.: mover la muestra   X: mover la vista</color>";
        return d;
    }

    HUDMicroscopio.DatosReto DatosObservacion(int aumento)
    {
        var p = observando;
        var m = reto.muestra;
        bool revelado = Actual >= EtapaReto.Hipotesis && Hipotesis != null;
        string tincion = p.PasosTincion == 0 ? "Sin teñir" : p.TincionErronea ? "NO VÁLIDA" : p.TincionCompleta ? "Gram completa" : $"Incompleta ({p.PasosTincion}/4)";
        var d = new HUDMicroscopio.DatosReto
        {
            subtitulo = revelado ? $"MUESTRA A  ·  {m.nombre.ToUpper()} ({m.nombreCientifico})" : $"{Misiones.Codigo1}  ·  MUESTRA A  ·  DESCONOCIDA",
            vista = p.PasosTincion == 0 ? reto.vistaSinTenir : p.TincionErronea ? reto.vistaTincionFallida : m.micrografia,
            riesgo = revelado ? m.riesgo : "DESCONOCIDO",
            colorRiesgo = revelado ? (Color?)null : new Color(1f, 0.75f, 0.3f),
            cuadernoTitulo = "CUADERNO DE EVIDENCIAS",
            obsTitulo = "QUÉ ESTÁS VIENDO",
            guia = "• ¿Qué forma tienen las células?\n• ¿Están sueltas, en cadenas o en racimos?\n• ¿De qué color quedaron tras la tinción de Gram?\n\n<color=#8FD8FF>A: más aumento   B / Y: menos   Joystick izq.: mover la muestra   X: mover la vista</color>",
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
        sb.AppendLine(VistoHongo ? "<color=#39FF88>[OK]</color> MUESTRA B observada" : "<color=#4A6278>[  ]</color> <color=#8FA6BC>MUESTRA B: ???  <size=85%>(placa con moho del BANCO)</size></color>");
        sb.AppendLine(VistoVirus ? "<color=#39FF88>[OK]</color> MUESTRA C observada" : "<color=#4A6278>[  ]</color> <color=#8FA6BC>MUESTRA C: ???  <size=85%>(tubo del hisopado del BANCO)</size></color>");
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
        else if (!VistoHongo || !VistoVirus)
        {
            d.obs = "Evidencias de la A completas. Ahora mira la MUESTRA B y la C (en el BANCO) para comparar: ¿son el mismo tipo de microbio?";
            d.aviso = nuevaEvidencia != null ? $"EVIDENCIAS COMPLETAS · {nuevaEvidencia}" : "Ahora compara con las MUESTRAS B y C";
            d.colorAviso = new Color(0.35f, 1f, 0.55f);
        }
        else
        {
            d.obs = revelado ? reto.explicacion : "Comparación completa. Clasifica las muestras en el panel CONCLUSIONES y busca la ficha de la A en la BIBLIOTECA (4).";
            d.aviso = nuevaEvidencia != null ? $"EVIDENCIAS COMPLETAS · {nuevaEvidencia}" : "COMPARACIÓN COMPLETA · Ve al panel CONCLUSIONES";
            d.colorAviso = new Color(0.35f, 1f, 0.55f);
        }
        Comparar(ref d, m);
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
            subtitulo = $"{Misiones.Codigo2}  ·  ANTES | DESPUÉS  ·  INTENTO {Intentos}",
            vista = exito ? reto.vistaDespuesEficaz : crecio ? reto.vistaDespuesCrecimiento : m.micrografia,
            antes = m.micrografia,
            antesTitulo = "ANTES (sin intervención)",
            claves = new[] { "Vista central", "Hipótesis", "Estrategia", "Agente", "Acción", "Células vivas" },
            valores = new[] { "DESPUÉS", Hipotesis != null ? Hipotesis.nombre : "-",
                              // Rótulo corto: las filas del HUD son de una línea.
                              (reto.estrategias?.FirstOrDefault(s => s.nombre == ultimaEstrategia)?.corto ?? ultimaEstrategia ?? "-") +
                              (ultimaEstrategia != null && !ultimoCoherente ? " <color=#FFD25A>[!] NO COINCIDE</color>" : ""),
                              $"{a.sigla} · {a.nombre}", a.accion, exito ? "muy pocas (lisadas)" : crecio ? "más que antes" : "casi todas" },
            riesgo = exito ? "EFICAZ" : "SIN EFECTO",
            riesgoClave = "Resultado",
            colorRiesgo = exito ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.35f, 0.3f),
            cuadernoTitulo = "INTERPRETACIÓN",
            cuaderno = exito
                ? $"Las células perdieron su forma y se rompieron. El agente {a.sigla} {a.accion.ToLower()} Tu muestra tiene una pared gruesa de peptidoglucano: por eso quedó VIOLETA en la tinción de Gram."
                : PistaDelFallo(),
            obsTitulo = "SIGUIENTE PASO",
            obs = exito ? "Ve al panel CONCLUSIONES (junto a los microscopios) y explica por qué funcionó: así terminas la MISIÓN 2."
                        : "Vuelve a la TERMINAL (5): revisa tu hipótesis, elige de nuevo la estrategia y prueba otro agente. La cámara ya tiene una placa nueva del cultivo.",
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
        izq.AppendLine("<b>MISIÓN 1 · Lo que observaste:</b>");
        foreach (var ev in reto.evidencias) izq.AppendLine($"  • A: {ev.clave}: {ev.valor}");
        izq.AppendLine("  • B: moho con hifas y esporas (HONGO) · C: ningún microbio visible (VIRUS)");
        izq.AppendLine("<b>MISIÓN 2 · Tus decisiones</b> (hipótesis · estrategia · agente):");
        foreach (var it in dat.intentos)
            izq.AppendLine($"  {it.numero}. {it.hipotesis} · {(string.IsNullOrEmpty(it.estrategia) ? "-" : it.estrategia.ToLower())} · {it.agente}: " +
                           $"{(it.exito ? "<color=#39FF88>eficaz</color>" : "<color=#FF7A6B>sin efecto</color>")}{(it.coherente ? "" : " <color=#FFD25A>(el vial no seguía tu estrategia)</color>")}");
        izq.AppendLine($"<b>Pistas pedidas (misión 2):</b> {dat.pistasPedidas}   <b>Tiempo (misión 2):</b> {Mathf.FloorToInt(metricas.Segundos / 60f)}:{Mathf.FloorToInt(metricas.Segundos % 60f):00} min");

        var der = new StringBuilder();
        der.AppendLine($"<color=#8FD8FF><b>LA MUESTRA A ERA: {m.nombre.ToUpper()}</b></color>  <i>{m.nombreCientifico}</i>");
        der.AppendLine(reto.explicacion);
        der.AppendLine("\n<color=#FFD25A>El moho de la MUESTRA B fabrica una sustancia que rompe la pared de las bacterias: así descubrió Fleming la penicilina (1928). " +
                       "Contra el virus de la MUESTRA C los antibióticos no sirven: hace falta una vacuna (MISIÓN 3).</color>");
        der.AppendLine("\n<size=80%><color=#A9BED2>Modelo educativo simplificado: no es un protocolo clínico ni un tratamiento real.</color></size>");

        resumen.Mostrar("MISIONES 1 y 2 COMPLETAS", izq.ToString(), der.ToString(), $"<b>Baymax recuerda {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}</b>\n{Misiones.TextoRecuerdo2}");
    }
}
