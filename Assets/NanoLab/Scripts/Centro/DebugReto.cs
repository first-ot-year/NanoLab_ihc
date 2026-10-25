using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

// Recorre el Caso 01 completo llamando a las mismas funciones que disparan las manos
// (sin visor): sirve para probar el flujo en el Editor (F9 = acierto directo, F10 = falla primero)
// y en las pruebas automáticas (NanoLab/Probar reto completo).
public class DebugReto : MonoBehaviour
{
    public static bool Terminado { get; private set; }
    public static bool Correcto { get; private set; }
    public static string Informe { get; private set; }
    public static event System.Action<string> Momento;     // para que las pruebas hagan capturas

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
        var otro = Portaobjetos.Todos.Where(p => p != porta && !p.Despues && !p.Agarrado && p.Encajada == null).OrderBy(p => p.name).FirstOrDefault();
        if (Comprobar(otro != null, "Hay otro portaobjetos para probar la recuperación"))
        {
            var casa = otro.transform.position;
            otro.transform.position = casa + Vector3.down * 1.2f;   // simula que se cae al suelo
            float t0 = Time.time;
            while (Time.time - t0 < 3f && (otro.transform.position - casa).sqrMagnitude > 0.0001f) yield return null;
            Comprobar((otro.transform.position - casa).sqrMagnitude <= 0.0001f, $"Un portaobjetos caído vuelve a su sitio en {Time.time - t0:F1} s (RNF-04)");
        }

        // 1) Frotis y tinción (con un error de orden a propósito para probar el feedback).
        fuentePlaca.Aplicar(porta);
        Comprobar(porta.muestra == rm.reto.muestra && rm.Actual == EtapaReto.Tincion, "Frotis del CASO 01 cargado > etapa Tinción");
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
        Momento?.Invoke("hud_2_observacion_1000x");
        Comprobar(rm.Evidencias.All(x => x) && rm.Actual == EtapaReto.Investigacion, "A 1000x se completan las evidencias > etapa Investigación");
        micro.DejarDeObservar();

        // 4) Biblioteca.
        var ficha = rm.fichas.FirstOrDefault(f => f.id == rm.reto.muestra.nombre) ?? rm.fichas.First();
        ficha.Alternar();
        Comprobar(rm.Actual == EtapaReto.Hipotesis, "Consultar una ficha > etapa Hipótesis");

        int correcto = System.Array.FindIndex(rm.reto.agentes, a => rm.reto.Eficacia(a) >= rm.reto.umbralExito);
        int incorrecto = System.Array.FindIndex(rm.reto.agentes, a => rm.reto.Eficacia(a) < rm.reto.umbralExito);
        var opcionCorrecta = System.Array.IndexOf(rm.terminal.opciones, rm.reto.muestra);
        var opcionIncorrecta = opcionCorrecta == 0 ? 1 : 0;

        if (fallarPrimero)
        {
            yield return Intento(rm, opcionIncorrecta, incorrecto, viales, prensa, camara, micro, porta);
            Comprobar(rm.Actual == EtapaReto.Hipotesis && rm.Intentos == 1, "Tras un fallo y el análisis se vuelve a Hipótesis con una pista");
            Comprobar(!rm.placa.Tratada, "La placa se regenera para reintentar");
        }
        yield return Intento(rm, opcionCorrecta, correcto, viales, prensa, camara, micro, porta);
        Comprobar(rm.Actual == EtapaReto.Cierre, "Tras el éxito y el análisis el caso se cierra");
        Comprobar(rm.Metricas.Datos.comparacionAntesDespues, "Se registró la comparación antes/después");
        Comprobar(RecuerdosBaymax.Cantidad >= 1 && indicador != null && indicador.texto.text.Contains($"{RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}"),
                  $"Baymax recupera un recuerdo: el indicador muestra {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}");
        string pista = rm.PedirPista();
        Paso($"Pista en cierre: {pista}");
        yield return null;
        Momento?.Invoke("archivo");
        Fin();
    }

    IEnumerator Intento(RetoManager rm, int opcion, int agente, VialAgente[] viales, PrensaCapsulas prensa, CamaraExperimental camara, MicroscopioOptico micro, Portaobjetos porta)
    {
        int n = rm.Intentos + 1;
        rm.terminal.Elegir(opcion);
        Comprobar(rm.Hipotesis == rm.terminal.opciones[opcion] && rm.Actual == EtapaReto.Preparacion, $"Intento {n}: hipótesis {rm.Hipotesis?.nombre} > etapa Preparación");

        var vial = viales.First(v => v.agente == agente);
        vial.Encajable.Encajar(prensa.ranura, false);
        prensa.Formular();
        float t0 = Time.time;
        while (rm.Actual != EtapaReto.Experimento && Time.time - t0 < 5f) yield return null;
        Comprobar(rm.Actual == EtapaReto.Experimento && prensa.capsula.gameObject.activeSelf, $"Intento {n}: cápsula {rm.reto.agentes[agente].sigla} formulada > etapa Experimento");
        Comprobar(vial.Encajable.Encajada == vial.Encajable.casa, $"Intento {n}: el vial vuelve a su sitio");

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

    void Fin()
    {
        Correcto = !fallo;
        Informe = log.ToString();
        Terminado = true;
        Debug.Log($"[PruebaReto] {(Correcto ? "TODO CORRECTO" : "HAY FALLOS")}\n{Informe}");
    }
}
