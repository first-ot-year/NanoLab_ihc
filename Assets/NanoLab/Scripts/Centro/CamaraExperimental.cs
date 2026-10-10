using System.Collections;
using TMPro;
using UnityEngine;

// Cámara experimental: recibe la placa (en una bandeja que entra en la cámara) y la cápsula,
// y al pulsar ACTIVAR simula la intervención con feedback de luz, forma, sonido, vibración y texto.
// Al terminar entrega un portaobjetos DESPUÉS para comparar en el microscopio.
public class CamaraExperimental : MonoBehaviour
{
    public ZonaEncaje zonaPlaca;            // en la bandeja móvil
    public ZonaEncaje zonaCapsula;
    public BotonFisico activar;
    public Transform bandeja;
    public Vector3 bandejaDentro;           // posición local de la bandeja dentro de la cámara
    public Transform puntoLiberacion;       // donde se disuelve la cápsula
    public ParticleSystem niebla;
    public Renderer[] lucesEstado;          // franjas emisivas que cambian de color
    public Light luzResultado;
    public GameObject iconoExito, iconoFallo;
    public TMP_Text pantalla;
    public TMP_Text resultado;
    public Portaobjetos portaDespues;
    public Transform anclajeDespues;

    public bool Ocupada { get; private set; }
    public float UltimaEficacia { get; private set; }

    static readonly Color Reposo = new Color(0.3f, 0.85f, 1f);
    static readonly Color Verde = new Color(0.25f, 1f, 0.45f);
    static readonly Color Rojo = new Color(1f, 0.3f, 0.25f);
    Vector3 bandejaFuera;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
        if (bandeja != null) bandejaFuera = bandeja.localPosition;
        Reiniciar();
    }

    void OnEnable() { if (activar != null) activar.Pulsado += Activar; }
    void OnDisable() { if (activar != null) activar.Pulsado -= Activar; }

    public void Reiniciar()
    {
        StopAllCoroutines();
        Ocupada = false;
        if (bandeja != null) bandeja.localPosition = bandejaFuera;
        if (iconoExito != null) iconoExito.SetActive(false);
        if (iconoFallo != null) iconoFallo.SetActive(false);
        if (luzResultado != null) luzResultado.enabled = false;
        if (niebla != null) niebla.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (portaDespues != null) portaDespues.gameObject.SetActive(false);
        PonerColor(Reposo);
        if (pantalla != null) pantalla.text = "1. Placa en la bandeja\n2. Cápsula en su ranura\n3. Pulsa ACTIVAR";
        if (resultado != null) resultado.text = "";
    }

    public void Activar()
    {
        if (Ocupada) return;
        var aviso = transform.position + Vector3.up * 1.4f;
        var placa = zonaPlaca.Actual != null ? zonaPlaca.Actual.GetComponent<PlacaCaso>() : null;
        var capsula = zonaCapsula.Actual != null ? zonaCapsula.Actual.GetComponent<Capsula>() : null;
        string falta = placa == null ? "Coloca la placa del CASO 01 en la bandeja de la cámara."
                     : capsula == null || capsula.Agente < 0 ? "Encaja una cápsula en la ranura de la cámara (fórmala en la PRENSA)."
                     : null;
        var rm = RetoManager.Instancia;
        if (falta == null && rm != null && !rm.PuedeExperimentar(out var motivo)) falta = motivo;
        if (falta != null)
        {
            Aviso.Mostrar(falta, aviso, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        StartCoroutine(Secuencia(placa, capsula));
    }

    IEnumerator Secuencia(PlacaCaso placa, Capsula capsula)
    {
        Ocupada = true;
        var rm = RetoManager.Instancia;
        int agente = capsula.Agente;
        var a = rm.reto.agentes[agente];
        float eficacia = rm.reto.Eficacia(a);
        var placaEnc = placa.GetComponent<Encajable>();
        placaEnc.Bloquear(true);
        if (iconoExito != null) iconoExito.SetActive(false);
        if (iconoFallo != null) iconoFallo.SetActive(false);
        if (resultado != null) resultado.text = "";
        if (portaDespues != null) portaDespues.gameObject.SetActive(false);
        Sonidos.Encaje(transform.position);

        // 1) La bandeja entra en la cámara.
        if (pantalla != null) pantalla.text = "Cargando la placa...";
        yield return MoverBandeja(bandejaFuera, bandejaDentro, 0.9f);

        // 2) La cápsula se libera sobre la placa.
        if (pantalla != null) pantalla.text = $"Liberando agente <b>{a.sigla}</b>...";
        PonerColor(a.color);
        if (niebla != null)
        {
            var main = niebla.main;
            var c = a.color; c.a = 0.35f;
            main.startColor = c;
            niebla.Play();
        }
        yield return capsula.Consumir(puntoLiberacion != null ? puntoLiberacion.position : placa.transform.position, 1.0f);

        // 3) Incubación simulada (24 h comprimidas en unos segundos).
        Sonidos.Camara(transform.position);
        var anim = StartCoroutine(placa.Tratar(eficacia, 3.2f));
        for (float t = 0; t < 1f; t += Time.deltaTime / 3.4f)
        {
            if (pantalla != null) pantalla.text = $"Incubación simulada\n<size=140%><b>{Mathf.RoundToInt(t * 24):00} h</b></size> / 24 h";
            PonerColor(Color.Lerp(a.color, Reposo, Mathf.PingPong(t * 4f, 1f)));
            yield return null;
        }
        yield return anim;
        if (niebla != null) niebla.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        // 4) Resultado: color + icono (forma) + sonido + vibración + texto.
        bool exito = eficacia >= rm.reto.umbralExito;
        UltimaEficacia = eficacia;
        PonerColor(exito ? Verde : Rojo);
        if (iconoExito != null) iconoExito.SetActive(exito);
        if (iconoFallo != null) iconoFallo.SetActive(!exito);
        if (luzResultado != null) { luzResultado.color = exito ? Verde : Rojo; luzResultado.enabled = true; }
        if (exito) Sonidos.Logro(transform.position); else Sonidos.Fallo(transform.position);
        ManosRig.Vibrar(transform.position, exito ? 0.6f : 1f, exito ? 0.15f : 0.35f);
        string texto = exito ? $"EFICAZ · Halo de inhibición alrededor del agente {a.sigla}"
                             : eficacia > 0.05f ? $"SIN EFECTO CLARO · Las colonias siguen ahí ({a.sigla})"
                                                : $"SIN EFECTO · Las colonias siguieron creciendo ({a.sigla})";
        if (resultado != null) { resultado.text = texto; resultado.color = exito ? Verde : Rojo; }
        if (pantalla != null) pantalla.text = "Toma el portaobjetos DESPUÉS\ny compáralo en el MICROSCOPIO (3)";

        // 5) Sale la bandeja y se entrega el portaobjetos DESPUÉS.
        yield return MoverBandeja(bandejaDentro, bandejaFuera, 0.9f);
        placaEnc.Bloquear(false);
        if (portaDespues != null)
        {
            portaDespues.gameObject.SetActive(true);
            portaDespues.ColocarEn(anclajeDespues.position, anclajeDespues.rotation);
            portaDespues.PrepararDespues(rm.reto.muestra);
            Aviso.Mostrar("Portaobjetos DESPUÉS listo: obsérvalo en el MICROSCOPIO (3).", anclajeDespues.position + Vector3.up * 0.15f, Aviso.Tipo.Info, 6f);
        }
        rm.ResultadoExperimento(agente, eficacia);
        Ocupada = false;
    }

    IEnumerator MoverBandeja(Vector3 desde, Vector3 hasta, float duracion)
    {
        if (bandeja == null) yield break;
        for (float t = 0; t < 1f; t += Time.deltaTime / duracion)
        {
            bandeja.localPosition = Vector3.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        bandeja.localPosition = hasta;
    }

    void PonerColor(Color c)
    {
        if (lucesEstado == null) return;
        foreach (var r in lucesEstado)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c * 0.6f);
            mpb.SetColor("_EmissionColor", c * 1.8f);
            r.SetPropertyBlock(mpb);
        }
    }
}
