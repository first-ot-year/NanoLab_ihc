using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Recorre las MISIONES 1 y 2 completas llamando a las mismas funciones que disparan las manos
// (sin visor): sirve para probar el flujo en el Editor (F9 = acierto directo, F10 = falla primero)
// y en las pruebas automáticas (NanoLab/Probar misiones 1 y 2).
public class DebugReto : MonoBehaviour
{
    public static bool Terminado { get; private set; }
    public static bool Correcto { get; private set; }
    public static string Informe { get; private set; }
    public static event System.Action<string> Momento;     // para que las pruebas hagan capturas
    public static bool SinPruebaPartida;                    // NanoLab/Capturas paso a paso: no toca la partida

    readonly StringBuilder log = new StringBuilder();
    bool fallo;

#if UNITY_EDITOR
    void Update()
    {
        bool f9 = false, f10 = false;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null) { f9 = kb.f9Key.wasPressedThisFrame; f10 = kb.f10Key.wasPressedThisFrame; }
#else
        f9 = Input.GetKeyDown(KeyCode.F9); f10 = Input.GetKeyDown(KeyCode.F10);
#endif
        if (f9 || f10) StartCoroutine(Simular(f10));
    }
#endif

    void Paso(string s) { log.AppendLine("OK   " + s); Debug.Log("[PruebaReto] " + s); }

    bool Comprobar(bool cond, string s)
    {
        if (cond) { Paso(s); return true; }
        fallo = true;
        log.AppendLine("FALLO " + s);
        Debug.LogError("[PruebaReto] FALLO: " + s);
        return false;
    }

    public IEnumerator Simular(bool fallarPrimero)
    {
        Terminado = false; fallo = false; log.Clear();
        yield return null;
        var rm = RetoManager.Instancia;
        if (!Comprobar(rm != null, "Hay RetoManager en la escena")) { Fin(); yield break; }
        // MontajeMisiones conecta las muestras B y C y el panel CONCLUSIONES en el Start de AdaptadorEscena.
        for (float t = 0f; t < 5f && rm.conclusiones == null; t += Time.deltaTime) yield return null;
        if (!Comprobar(rm.conclusiones != null && rm.muestraHongo != null && rm.muestraVirus != null && rm.enabled,
                       "Misiones listas: RetoManager activo, MUESTRAS B y C y panel CONCLUSIONES")) { Fin(); yield break; }
        if (rm.Actual != EtapaReto.Exploracion) rm.Reiniciar();
        var zona = rm.zona;
        bool EnZona(Component c) => zona.Contains(new Vector3(c.transform.position.x, zona.center.y, c.transform.position.z));

        var porta = Portaobjetos.Todos.Where(p => EnZona(p) && !p.Despues && p.muestra == null).OrderBy(p => p.name).FirstOrDefault();
        var fuentePlaca = rm.placa.GetComponent<FuenteMuestra>();
        var fuentes = FindObjectsOfType<FuenteMuestra>().Where(EnZona).ToArray();
        var micro = FindObjectsOfType<MicroscopioOptico>().FirstOrDefault(m => m.modoReto);
        var prensa = FindObjectOfType<PrensaCapsulas>();
        var viales = FindObjectsOfType<VialAgente>();
        var camara = rm.camara;
        Comprobar(porta != null && fuentePlaca != null && micro != null && prensa != null && viales.Length == rm.reto.agentes.Length && camara != null,
                  $"Estaciones encontradas (portaobjetos, placa, microscopio, prensa, {viales.Length} viales, cámara)");
        if (fallo) { Fin(); yield break; }
        FuenteMuestra Gotero(int r) => fuentes.FirstOrDefault(f => f.reactivo == r);
        var desinfectante = fuentes.FirstOrDefault(f => f.esDesinfectante);

        // 0) Introducción (RF-01) y tolerancia al error (RNF-04): un portaobjetos que cae al suelo vuelve solo.
        var indicador = FindObjectOfType<IndicadorRecuerdos>();
        Comprobar(indicador != null && indicador.texto != null && indicador.texto.text.Contains($"{RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}"),
                  $"Introducción con indicador de recuerdos ({(indicador != null && indicador.texto != null ? indicador.texto.text : "no encontrado")})");
        var otro = Portaobjetos.Todos.Where(p => p != porta && EnZona(p) && !p.Despues && !p.Agarrado && p.Encajada == null && Hueco.De(p.transform) == null).OrderBy(p => p.name).FirstOrDefault();
        if (Comprobar(otro != null, "Hay otro portaobjetos para probar la recuperación"))
        {
            var casa = otro.transform.position;
            otro.transform.position = casa + Vector3.down * 1.2f;   // simula que se cae al suelo
            float t0 = Time.time;
            while (Time.time - t0 < 6f && (otro.transform.position - casa).sqrMagnitude > 0.0001f) yield return null;
            Comprobar((otro.transform.position - casa).sqrMagnitude <= 0.0001f, $"Un portaobjetos caído vuelve a su sitio en {Time.time - t0:F1} s (RNF-04)");
        }

        // 0b) INCUBADORA (9): las placas A y B pasaron la noche dentro; hasta sacarlas no hay frotis.
        var inc = EquiposCentro.Incubadora;
        if (Comprobar(inc != null && EquiposCentro.Cabina != null && EquiposCentro.Autoclave != null && EquiposCentro.Ultracongelador != null && EquiposCentro.IncubadoraCO2 != null,
                      $"Consolas de los equipos del centro: {string.Join(", ", EquiposCentro.Todos.Select(x => x.titulo))}"))
        {
            Comprobar(rm.ObjetivoActual.Contains("INCUBADORA") && inc.guia.Activa, "Al empezar, el objetivo y las flechas llevan a la INCUBADORA (9)");
            Momento?.Invoke("equipo_9_antes");
            fuentePlaca.Aplicar(porta);
            Comprobar(porta.muestra == null && rm.Actual == EtapaReto.Exploracion, "Con las placas en la incubadora no se puede hacer el frotis");
            yield return EquiposCentro.Usar(inc);
            Comprobar(inc.Hecho && !inc.guia.Activa && rm.estaciones[0].Activa, "SACAR PLACAS: las placas A y B pasan al BANCO (1) y la guía va allí");
            yield return new WaitForSeconds(0.3f);
            Momento?.Invoke("equipo_9_hecho");
        }
        Momento?.Invoke("inicio_banco");
        // 1) Frotis y tinción (con un error de orden a propósito para probar el feedback).
        fuentePlaca.Aplicar(porta);
        Momento?.Invoke("banco_frotis_a");
        Comprobar(porta.muestra == rm.reto.muestra && rm.Actual == EtapaReto.Tincion && rm.Mision == 1, "MISIÓN 1: frotis de la MUESTRA A cargado > etapa Tinción");
        Gotero(1).Aplicar(porta);
        Gotero(3).Aplicar(porta);
        Comprobar(porta.TincionErronea, "Saltarse el lugol invalida la tinción");
        porta.Encajar(micro.platina);
        yield return null;
        micro.ObservarAhora();
        Momento?.Invoke("hud_0_tincion_fallida");
        Comprobar(!rm.Evidencias.Any(x => x), "Una tinción no válida no da evidencias");
        micro.DejarDeObservar();
        porta.ColocarEn(porta.transform.position + Vector3.up * 0.2f, Quaternion.identity);
        desinfectante.Aplicar(porta);
        Comprobar(porta.muestra == null, "El desinfectante limpia el portaobjetos");
        fuentePlaca.Aplicar(porta);
        for (int r = 1; r <= 4; r++) Gotero(r).Aplicar(porta);
        Comprobar(porta.TincionCompleta && rm.Actual == EtapaReto.Observacion, "Tinción de Gram completa > etapa Observación");
        Momento?.Invoke("tincion_completa");

        // 2) Hipótesis antes de tiempo: debe rechazarse.
        Comprobar(!rm.PuedeHipotesis(out _), "La terminal rechaza una hipótesis sin evidencias");

        // 3) Microscopio: evidencias a 400x y 1000x.
        porta.Encajar(micro.platina);
        yield return null;
        micro.ObservarAhora();
        Momento?.Invoke("hud_1_observacion_400x");
        yield return null;
        var hud = HUDMicroscopio.Instancia;
        Comprobar(hud != null, "El HUD del microscopio aparece");
        Comprobar(rm.Evidencias[0] && !rm.Evidencias[1], "A 400x se registra la forma, pero no la agrupación");
        hud.CambiarAumento(+1);
        yield return null;
        Momento?.Invoke("hud_2_observacion_1000x");
        Comprobar(rm.Evidencias.All(x => x) && rm.Actual == EtapaReto.Observacion, "A 1000x se completan las evidencias de la A; falta comparar con la B y la C");
        micro.DejarDeObservar();
        Comprobar(!rm.PuedeHipotesis(out var sinComparar) && sinComparar.Contains("MUESTRA B"), "Sin comparar las 3 muestras, la terminal no acepta la identificación");

        // 3b) Comparación de muestras (RF-03 / RF-08): MUESTRA B (moho) y MUESTRA C (hisopado del brote).
        var fuenteB = fuentes.FirstOrDefault(f => f.muestra == rm.muestraHongo);
        var fuenteC = fuentes.FirstOrDefault(f => f.muestra == rm.muestraVirus);
        Comprobar(fuenteB != null && fuenteC != null, "El BANCO tiene la MUESTRA B (placa con moho) y la MUESTRA C (hisopado)");
        if (fallo) { Fin(); yield break; }
        foreach (var (fuente, letra) in new[] { (fuenteB, "B"), (fuenteC, "C") })
        {
            porta.ColocarEn(porta.transform.position + Vector3.up * 0.2f, Quaternion.identity);
            desinfectante.Aplicar(porta);
            if (letra == "C" && EquiposCentro.Cabina != null)
            {
                // CABINA DE BIOSEGURIDAD (10): el hisopado C solo se abre con el flujo encendido.
                Comprobar(EquiposCentro.Cabina.guia.Activa && rm.ObjetivoActual.Contains("CABINA"), "Para la MUESTRA C, el objetivo y las flechas llevan a la CABINA (10)");
                fuente.Aplicar(porta);
                Comprobar(porta.muestra == null, "Sin encender el flujo de la cabina, el hisopado C no se abre");
                Momento?.Invoke("equipo_10_antes");
                yield return EquiposCentro.Usar(EquiposCentro.Cabina);
                Comprobar(EquiposCentro.Cabina.Hecho, "ENCENDER FLUJO en la cabina");
                yield return new WaitForSeconds(0.3f);
                Momento?.Invoke("equipo_10_hecho");
            }
            fuente.Aplicar(porta);
            Comprobar(porta.DeCaso && porta.Etiqueta == "MUESTRA " + letra, $"Frotis de la MUESTRA {letra} con su etiqueta (no revela qué es)");
            porta.Encajar(micro.platina);
            yield return null;
            micro.ObservarAhora();
            yield return null;
            Momento?.Invoke("hud_comparacion_muestra_" + letra.ToLower());
            Comprobar(letra == "B" ? rm.VistoHongo : rm.VistoVirus, $"La MUESTRA {letra} queda registrada al mirarla, con la imagen de otra muestra al lado para comparar");
            micro.DejarDeObservar();
        }
        Comprobar(rm.Actual == EtapaReto.Investigacion, "Con las 3 muestras comparadas > etapa Investigación");

        // 3c) Panel CONCLUSIONES: clasificar A, B y C (un error a propósito).
        var conc = rm.conclusiones;
        Comprobar(!rm.PuedeHipotesis(out var sinClasificar) && sinClasificar.Contains("CONCLUSIONES"), "Sin clasificar las muestras, la terminal no acepta la identificación");
        int errores = rm.Metricas.Datos.errores;
        conc.botones[2].Activar();   // A = virus (mal)
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        Comprobar(rm.Clasificadas == 0 && rm.Metricas.Datos.errores == errores + 1 && conc.respuesta.text.Contains("AÚN NO"), "Clasificar la A como VIRUS se explica y cuenta como error");
        Momento?.Invoke("conclusiones_error");
        conc.botones[0].Activar();   // A = bacteria
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        conc.botones[1].Activar();   // B = hongo
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        Momento?.Invoke("conclusiones_clasificacion");
        conc.botones[2].Activar();   // C = virus
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        Comprobar(rm.Clasificadas == 3 && rm.Actual == EtapaReto.Investigacion, "A = BACTERIA, B = HONGO, C = VIRUS en CONCLUSIONES; falta la ficha de la biblioteca");
        // RF-05: la hipótesis se decide con las evidencias y la información consultada.
        Comprobar(!rm.PuedeHipotesis(out var sinFicha) && sinFicha.Contains("BIBLIOTECA"), "Con las muestras clasificadas, la terminal pide consultar antes una ficha (RF-05)");

        // 4) Biblioteca.
        var ficha = rm.fichas.FirstOrDefault(f => f.id == rm.reto.muestra.nombre) ?? rm.fichas.First();
        ficha.Alternar();
        Comprobar(rm.Actual == EtapaReto.Hipotesis, "Consultar una ficha > etapa Hipótesis");
        Momento?.Invoke("biblioteca_ficha");
        // RF-04: la ficha del virus (A6) y abrir con el rayo o tocando el panel.
        var fichaVirus = rm.fichas.FirstOrDefault(f => f.id == "Hepatitis B");
        bool textoElectronica = fichaVirus != null && fichaVirus.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text.Contains("electrónica"));
        Comprobar(rm.fichas.Length == 5 && textoElectronica, $"La biblioteca tiene {rm.fichas.Length} fichas, con la del virus como micrografía electrónica (A6)");
        if (fichaVirus != null)
        {
            fichaVirus.Activar();   // lo que hace el rayo al pulsar el gatillo
            Comprobar(fichaVirus.Abierta && rm.Metricas.Datos.fichasConsultadas.Contains("Hepatitis B"), "Una ficha se abre apuntándola con el rayo y se registra la consulta");
            var cara = fichaVirus.transform.position + fichaVirus.transform.forward * -0.03f;
            Comprobar(fichaVirus.Contiene(cara) && !fichaVirus.Contiene(cara + fichaVirus.transform.forward * -0.5f) && fichaVirus.GetComponentInChildren<Collider>() != null,
                      "La ficha se abre al tocar el panel (zona de toque y collider para el rayo)");
            yield return new WaitForSeconds(0.45f);
            fichaVirus.Activar();
            Comprobar(!fichaVirus.Abierta, "La misma acción la cierra");
        }

        int correcto = System.Array.FindIndex(rm.reto.agentes, a => rm.reto.Eficacia(a) >= rm.reto.umbralExito);
        int incorrecto = System.Array.FindIndex(rm.reto.agentes, a => rm.reto.Eficacia(a) < rm.reto.umbralExito);
        var opcionCorrecta = System.Array.IndexOf(rm.terminal.opciones, rm.reto.muestra);
        var opcionIncorrecta = opcionCorrecta == 0 ? 1 : 0;
        int estrategiaCorrecta = System.Array.FindIndex(rm.reto.estrategias ?? new RetoData.Estrategia[0], s => s.diana == rm.reto.DianaCorrecta);
        Comprobar(estrategiaCorrecta >= 0 && rm.terminal.botonesEstrategia != null && rm.terminal.botonesEstrategia.Length == rm.reto.estrategias.Length,
                  $"La terminal tiene {rm.terminal.botones.Length} hipótesis y {rm.terminal.botonesEstrategia?.Length ?? 0} estrategias (RF-05)");
        if (fallo) { Fin(); yield break; }

        // RNF-06: el aviso de contenido simplificado se ve al inicio, durante la intervención y al cierre.
        bool Tiene(Component c) => c != null && c.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text.IndexOf("simplificad", System.StringComparison.OrdinalIgnoreCase) >= 0);
        var intro = FindObjectOfType<VentanaIntro>(true);
        var recepcion = FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Recepcion");
        var avisos = new (string donde, bool ok)[]
        {
            ("introducción", intro != null && intro.pasos.Any(p => p.texto != null && p.texto.Contains("simplificad"))),
            ("recepción", Tiene(recepcion)),
            ("terminal", Tiene(rm.terminal)),
            ("prensa y tablero de agentes", Tiene(prensa) && prensa.pantalla.text.Contains("simplificad")),
            ("panel ☰", MenuAjustes.Instancia != null && Tiene(MenuAjustes.Instancia.panel.transform)),
        };
        Comprobar(avisos.All(a => a.ok), "Aviso de contenido simplificado (RNF-06) en: " + string.Join(", ", avisos.Select(a => a.donde + (a.ok ? "" : " (FALTA)"))));

        if (fallarPrimero)
        {
            // MISIÓN 1: una identificación que no encaja con las evidencias se explica y no se registra.
            int err = rm.Metricas.Datos.errores;
            rm.terminal.Elegir(opcionIncorrecta);
            Comprobar(rm.Hipotesis == null && rm.Mision == 1 && rm.Metricas.Datos.errores == err + 1, "MISIÓN 1: identificar mal la A se explica, cuenta como error y no termina la misión");
            rm.terminal.Elegir(opcionCorrecta);
            Comprobar(rm.Hipotesis == rm.reto.muestra && rm.Mision == 2 && Misiones.Tiene(Misiones.Recuerdo1) && rm.Metricas.Datos.caso == Misiones.Codigo2,
                      $"MISIÓN 1 completa al identificar {rm.reto.muestra.nombre}: recuerdo 1, métricas de la misión 2");
            Momento?.Invoke("mision1_completa");
            // MISIÓN 2: estrategia buena y un vial que no la sigue: la prensa avisa antes de formular.
            yield return Intento(rm, opcionCorrecta, estrategiaCorrecta, incorrecto, viales, prensa, camara, micro, porta);
            Comprobar(rm.Actual == EtapaReto.Hipotesis && rm.Intentos == 1, "Tras un fallo y el análisis se vuelve a Hipótesis con una pista");
            Comprobar(!rm.placa.Tratada, "La placa se regenera para reintentar");
            Momento?.Invoke("terminal_3_pista_fallo");
            var it = rm.Metricas.Datos.intentos.LastOrDefault();
            Comprobar(it != null && !it.coherente && it.estrategia == rm.reto.estrategias[estrategiaCorrecta].nombre,
                      $"El registro guarda la estrategia del intento y que el vial no la seguía ({it?.estrategia}, coherente {it?.coherente})");
            Comprobar(rm.PistaFallo != null && rm.PistaFallo.Contains("estrategia") && rm.EstrategiaElegida == null && rm.Hipotesis != null,
                      $"La pista dice que la estrategia era buena y el vial no; la estrategia se borra para repensarla y la hipótesis se mantiene: \"{rm.PistaFallo}\"");
        }
        yield return Intento(rm, opcionCorrecta, estrategiaCorrecta, correcto, viales, prensa, camara, micro, porta);
        Comprobar(rm.Actual == EtapaReto.Analisis && rm.ExitoAnalizado, "Tras el éxito y el análisis, falta explicar el resultado en CONCLUSIONES");
        Comprobar(!rm.PuedeFormular(out _), "Mientras falta la explicación no se formula otra cápsula");
        int e2 = rm.Metricas.Datos.errores;
        conc.botones[1].Activar();   // "es un virus" (mal)
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        Comprobar(rm.Explicadas == 0 && rm.Metricas.Datos.errores == e2 + 1, "Una explicación equivocada se corrige y cuenta como error");
        conc.botones[0].Activar();   // pared gruesa
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        Momento?.Invoke("conclusiones_explicacion");
        conc.botones[1].Activar();   // no serviría contra el virus
        yield return new WaitForSeconds(0.25f);   // los botones ignoran pulsaciones a menos de 0,2 s
        if (EquiposCentro.Autoclave != null)
        {
            // AUTOCLAVE (11): último paso de la misión 2.
            Comprobar(rm.PendienteAutoclave && rm.Actual == EtapaReto.Analisis && !Misiones.Tiene(Misiones.Recuerdo2) && EquiposCentro.Autoclave.guia.Activa,
                      "Explicado el resultado, falta esterilizar: las flechas llevan al AUTOCLAVE (11)");
            Momento?.Invoke("equipo_11_antes");
            yield return EquiposCentro.Usar(EquiposCentro.Autoclave);
            Momento?.Invoke("equipo_11_hecho");
        }
        Comprobar(rm.Actual == EtapaReto.Cierre && Misiones.Tiene(Misiones.Recuerdo2) && Misiones.VacunaDesbloqueada,
                  "MISIÓN 2 completa: recuerdo 2 y la vacuna (MISIÓN 3) desbloqueada");
        yield return new WaitForSeconds(0.3f);
        Momento?.Invoke("mision2_completa");
        Comprobar(rm.Metricas.Datos.comparacionAntesDespues, "Se registró la comparación antes/después");
        Comprobar(RecuerdosBaymax.Cantidad >= 1 && indicador != null && indicador.texto.text.Contains($"{RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}"),
                  $"Baymax recupera un recuerdo: el indicador muestra {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}");
        string pista = rm.PedirPista();
        Paso($"Pista en cierre: {pista}");
        Comprobar(Tiene(rm.resumen) && rm.resumen.izquierda.text.Contains(rm.reto.estrategias[estrategiaCorrecta].nombre.ToLower()),
                  "El informe del caso muestra hipótesis · estrategia · agente de cada intento y el aviso de contenido simplificado (RNF-06)");
        yield return null;
        Momento?.Invoke("archivo");
        yield return ProbarPartida(rm);
        Fin();
    }

    // Partida guardada: checkpoint del caso, respaldo ante un archivo dañado, versión nueva y NUEVA PARTIDA.
    IEnumerator ProbarPartida(RetoManager rm)
    {
#if UNITY_EDITOR
        // Solo con el archivo de prueba (NanoLab/Probar reto completo): con F9/F10 se borraría la partida real.
        if (SinPruebaPartida) yield break;
        if (string.IsNullOrEmpty(UnityEditor.SessionState.GetString(Partida.ClaveRutaPrueba, ""))) { log.AppendLine("(partida: se prueba solo desde NanoLab/Probar reto completo)"); yield break; }
        var cp = Partida.UltimoCheckpoint;
        Comprobar(cp != null && cp.reto == Misiones.Codigo2 && cp.etapa == (int)EtapaReto.Cierre,
                  $"Al cerrar el caso se guarda un checkpoint ({(cp != null ? $"{cp.reto}, etapa {(EtapaReto)cp.etapa}" : "ninguno")})");

        bool guardada = Partida.GuardarManual();
        Partida.Olvidar();
        var d = Partida.Leer();
        Comprobar(guardada && d != null && d.reto == rm.reto.codigo && d.etapa == (int)EtapaReto.Cierre && d.recuerdos.Count == RecuerdosBaymax.Cantidad,
                  $"GUARDAR PARTIDA escribe el caso, la etapa, {d?.muestras.Count ?? 0} muestras y {d?.recuerdos.Count ?? 0} recuerdos");

        Partida.Guardar();   // el anterior pasa al respaldo
        File.WriteAllText(Partida.Ruta, "{\"version\": 2, \"fecha\": ");   // como si la app se cerrara a mitad de escribir
        Partida.Olvidar();
        d = Partida.Leer();
        Comprobar(d != null && Partida.DesdeRespaldo && d.etapa == (int)EtapaReto.Cierre && File.Exists(Partida.RutaCorrupta) && !File.Exists(Partida.Ruta),
                  "Con partida.json dañado se carga partida.json.bak y el dañado se aparta a partida.corrupta.json");
        Comprobar(Partida.Guardar() && File.Exists(Partida.Ruta), "El siguiente guardado vuelve a crear partida.json");

        rm.Reiniciar();
        bool vuelto = GestorPartida.VolverAlCheckpoint(out var mensaje);
        yield return null;
        Comprobar(vuelto && rm.Actual == EtapaReto.Cierre, $"VOLVER AL CHECKPOINT tras reiniciar el caso: {mensaje} (etapa {rm.Actual})");
        string diana = rm.reto.DianaCorrecta.ToString();
        Comprobar(cp != null && cp.estrategia == diana && d != null && d.estrategia == diana && rm.EstrategiaElegida != null && rm.EstrategiaElegida.diana.ToString() == diana,
                  $"La partida y el checkpoint guardan la estrategia y VOLVER AL CHECKPOINT la restaura ({cp?.estrategia})");
        var todas = Enumerable.Repeat(true, rm.Evidencias.Length).ToArray();
        rm.Restaurar(EtapaReto.Preparacion, todas, rm.reto.muestra.name, null);
        bool sinEstrategia = rm.Actual == EtapaReto.Hipotesis && rm.Hipotesis == rm.reto.muestra && rm.EstrategiaElegida == null;
        rm.Restaurar(EtapaReto.Preparacion, todas, rm.reto.muestra.name, diana);
        Comprobar(sinEstrategia && rm.Actual == EtapaReto.Preparacion && rm.EstrategiaElegida != null,
                  "Una partida en Preparación sin estrategia (anterior al E4) vuelve a Hipótesis; con estrategia sigue en Preparación");

        string antes = File.ReadAllText(Partida.Ruta);
        File.WriteAllText(Partida.Ruta, antes.Replace($"\"version\": {Partida.Version}", "\"version\": 99"));
        Partida.Olvidar();
        string nueva = File.ReadAllText(Partida.Ruta);
        Comprobar(Partida.Leer() != null && !Partida.Guardar() && File.ReadAllText(Partida.Ruta) == nueva,
                  "Una partida de una versión más nueva de la app se carga, pero no se sobrescribe");
        File.WriteAllText(Partida.Ruta, antes);
        Partida.Olvidar();

        Partida.Borrar();
        Comprobar(!Partida.Existe && !Partida.Guardar() && RecuerdosBaymax.Cantidad == 0 && Progreso.Vistas.Count() == 0,
                  "NUEVA PARTIDA borra la partida y el respaldo, y no se vuelve a guardar el estado viejo antes de recargar");
        Partida.Suspendido = false;
#else
        yield break;
#endif
    }

    IEnumerator Intento(RetoManager rm, int opcion, int estrategia, int agente, VialAgente[] viales, PrensaCapsulas prensa, CamaraExperimental camara, MicroscopioOptico micro, Portaobjetos porta)
    {
        int n = rm.Intentos + 1;
        var s = rm.reto.estrategias[estrategia];
        if (rm.Hipotesis == null)
        {
            rm.terminal.ElegirEstrategia(estrategia);
            Comprobar(rm.EstrategiaElegida == null && rm.Actual == EtapaReto.Hipotesis, $"Intento {n}: sin hipótesis, la terminal no acepta todavía la estrategia");
        }
        rm.terminal.Elegir(opcion);
        Comprobar(rm.Hipotesis == rm.terminal.opciones[opcion] && rm.Actual == EtapaReto.Hipotesis && rm.EstrategiaElegida == null,
                  $"Intento {n}: hipótesis {rm.Hipotesis?.nombre} registrada; sin estrategia sigue en Hipótesis");
        yield return null;
        if (n == 1) Momento?.Invoke("terminal_1_hipotesis");
        rm.terminal.botonesEstrategia[estrategia].Activar();   // lo que hace el rayo al pulsar el gatillo
        Comprobar(rm.EstrategiaElegida == s && rm.Actual == EtapaReto.Preparacion,
                  $"Intento {n}: estrategia \"{s.nombre}\" elegida con el rayo > etapa Preparación");
        yield return null;
        if (n == 1) Momento?.Invoke("terminal_2_estrategia");

        var vial = viales.First(v => v.agente == agente);
        vial.Encajable.Encajar(prensa.ranura, false);
        yield return null;
        bool coherente = rm.EsCoherente(agente);
        string sigla = rm.reto.agentes[agente].sigla;
        Comprobar(prensa.pantalla.text.Contains(rm.Hipotesis.nombre) && prensa.pantalla.text.Contains(coherente ? "[OK] COINCIDE" : "NO COINCIDE"),
                  $"Intento {n}: con el vial {sigla} encajado, la pantalla de la prensa muestra la hipótesis y que {(coherente ? "coincide" : "NO coincide")} con la estrategia");
        if (coherente) Momento?.Invoke("prensa_2_coincide");
        bool activaAntes = prensa.capsula.gameObject.activeSelf;
        prensa.Formular();
        if (!coherente)
        {
            yield return null;
            Comprobar(prensa.EsperandoConfirmacion && rm.Actual == EtapaReto.Preparacion && prensa.capsula.gameObject.activeSelf == activaAntes,
                      $"Intento {n}: con el vial {sigla} la prensa avisa y no formula hasta pulsar FORMULAR otra vez");
            yield return new WaitForSeconds(0.5f);   // que el aviso termine de aparecer (sigue dentro de la ventana de confirmación)
            Momento?.Invoke("prensa_1_no_coincide");
            prensa.Formular();   // segunda pulsación: se prueba igual (el error también informa)
        }
        else Comprobar(!prensa.EsperandoConfirmacion, $"Intento {n}: el vial {sigla} sigue la estrategia y se formula al primer pulsado");
        float t0 = Time.time;
        while (rm.Actual != EtapaReto.Experimento && Time.time - t0 < 5f) yield return null;
        Comprobar(rm.Actual == EtapaReto.Experimento && prensa.capsula.gameObject.activeSelf, $"Intento {n}: cápsula {rm.reto.agentes[agente].sigla} formulada > etapa Experimento");
        Comprobar(vial.Encajable.Encajada == vial.Encajable.casa, $"Intento {n}: el vial vuelve a su sitio");
        if (coherente) Momento?.Invoke("prensa_3_capsula_lista");

        rm.placa.GetComponent<Encajable>().Encajar(camara.zonaPlaca, false);
        prensa.capsula.Encajable.Encajar(camara.zonaCapsula, false);
        camara.Activar();
        Comprobar(camara.Ocupada, $"Intento {n}: la cámara arranca");
        t0 = Time.time;
        while (camara.Ocupada && Time.time - t0 < 20f) yield return null;
        Momento?.Invoke($"camara_intento{n}");
        bool exito = rm.reto.Eficacia(rm.reto.agentes[agente]) >= rm.reto.umbralExito;
        Comprobar(rm.Actual == EtapaReto.Analisis && camara.portaDespues.gameObject.activeSelf && rm.placa.Tratada, $"Intento {n}: resultado {(exito ? "EFICAZ" : "SIN EFECTO")} > etapa Análisis, portaobjetos DESPUÉS entregado");
        Comprobar(!rm.PuedeFormular(out _), $"Intento {n}: no se puede formular otra cápsula sin analizar");
        if (n == 1 && BiohazardEvent.Instancia != null) yield return Bioseguridad(rm, camara, micro, porta);

        porta.ColocarEn(porta.transform.position + Vector3.up * 0.3f, Quaternion.identity);
        camara.portaDespues.Encajar(micro.platina);
        yield return null;
        micro.ObservarAhora();
        Momento?.Invoke($"hud_3_comparacion_intento{n}");
        yield return null;
        micro.DejarDeObservar();
        Paso($"Intento {n}: comparación ANTES | DESPUÉS observada (etapa ahora {rm.Actual})");
        camara.portaDespues.ColocarEn(camara.anclajeDespues.position, camara.anclajeDespues.rotation);
    }

    // Evento sorpresa tras el primer experimento: alarma, bloqueo, pulverizador, limpieza y vuelta a la normalidad.
    IEnumerator Bioseguridad(RetoManager rm, CamaraExperimental camara, MicroscopioOptico micro, Portaobjetos porta)
    {
        var ev = BiohazardEvent.Instancia;
        Comprobar(BiohazardEvent.Activo && ev.zonas.Count >= 2, $"Tras el primer experimento salta la ALERTA DE BIOSEGURIDAD ({ev.zonas.Count} zonas contaminadas)");
        Comprobar(ev.zonas.All(z => z.gameObject.layer == BiohazardEvent.Capa || BiohazardEvent.Capa < 0), "Las zonas están en la capa Biohazard");
        bool tag = false; try { tag = ev.zonas[0].CompareTag(BiohazardEvent.Tag); } catch (UnityException) { }
        Comprobar(tag, "Las zonas tienen la etiqueta Contaminated");
        Comprobar(rm.ObjetivoActual.Contains("ALERTA") && ev.guiaKit.Activa, "El objetivo y las flechas llevan al KIT DE DERRAMES");
        yield return new WaitForSeconds(0.6f);
        var luz = GameObject.Find("LuzAlarma_Sala");
        Comprobar(luz != null && luz.activeInHierarchy && luz.GetComponent<Light>().color.r > 0.9f, "Luces rojas de alarma encendidas");
        Momento?.Invoke("biohazard_1_alarma");

        porta.ColocarEn(porta.transform.position + Vector3.up * 0.3f, Quaternion.identity);
        camara.portaDespues.Encajar(micro.platina);
        yield return null;
        micro.ObservarAhora();
        Comprobar(rm.Actual == EtapaReto.Analisis && !rm.PuedeExperimentar(out _), "Con la alarma no se analiza el portaobjetos DESPUÉS ni se experimenta");
        micro.DejarDeObservar();
        camara.portaDespues.ColocarEn(camara.anclajeDespues.position, camara.anclajeDespues.rotation);
        Momento?.Invoke("biohazard_2_kit");

        ev.MarcarSpray();   // en el visor: agarrar el pulverizador del kit
        yield return null;
        Comprobar(ev.guiaZona.Activa && rm.ObjetivoActual.Contains("Quedan"), "Con el pulverizador en la mano, la guía va a la zona contaminada más cercana");
        Momento?.Invoke("biohazard_3_zona");
        bool primera = true;
        foreach (var z in ev.zonas.ToList())
        {
            var origen = z.transform.position + Vector3.up * 0.9f;
            int tocadas = BiohazardEvent.Rociar(origen + Vector3.up * 3f, Vector3.up, 0.01f);   // apuntando al techo: nada
            if (primera) Comprobar(tocadas == 0, "Rociar hacia otro lado no limpia nada");
            float t = 0f;
            while (!z.Limpia && t < 5f)
            {
                BiohazardEvent.Rociar(origen, z.transform.position - origen, Time.deltaTime);
                t += Time.deltaTime;
                if (primera && t > 0.55f && t - Time.deltaTime <= 0.55f) { Comprobar(z.carga < 0.9f && z.carga > 0f, "La mancha pierde opacidad poco a poco con el rocío"); Momento?.Invoke("biohazard_4_rociando"); }
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            Comprobar(z.Limpia && !z.gameObject.activeSelf, $"Zona contaminada limpiada en {t:F1} s y desactivada");
            primera = false;
        }
        yield return new WaitForSeconds(1.5f);
        Comprobar(!BiohazardEvent.Activo && BiohazardEvent.YaOcurrio, "Sin zonas contaminadas termina la alerta");
        Comprobar(luz == null || !luz.activeInHierarchy, "Las luces vuelven a la normalidad");
        Comprobar(!rm.ObjetivoActual.Contains("ALERTA") && rm.PuedeExperimentar(out _) == false, "La misión 2 sigue donde estaba (analizar el DESPUÉS)");
        Momento?.Invoke("biohazard_5_resuelto");
    }

    void Fin()
    {
        Correcto = !fallo;
        Informe = log.ToString();
        Terminado = true;
        Debug.Log($"[PruebaReto] {(Correcto ? "TODO CORRECTO" : "HAY FALLOS")}\n{Informe}");
    }
}
