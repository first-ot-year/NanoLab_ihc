using System.Collections;
using TMPro;
using UnityEngine;

// Prensa de cápsulas (RF-06): se encaja un vial de agente, se pulsa FORMULAR y sale una cápsula
// del color del agente lista para la cámara experimental. La pantalla recuerda la hipótesis y la
// estrategia de la terminal y dice si el vial encajado la sigue. Si no la sigue, avisa y pide pulsar
// otra vez: no se bloquea, porque probar y equivocarse también da información (RF-09).
public class PrensaCapsulas : MonoBehaviour
{
    public ZonaEncaje ranura;          // tipo "vial"
    public ZonaEncaje salida;          // tipo "capsula"
    public BotonFisico formular;
    public Capsula capsula;
    public Transform piston;
    public TMP_Text pantalla;
    public Renderer luz;
    [Tooltip("Segundos para confirmar un vial que no sigue la estrategia.")]
    public float tiempoConfirmar = 4f;

    bool ocupada;
    Vector3 pistonPos;
    float confirmarHasta = -1f;
    VialAgente confirmarVial;

    // Hay un aviso de "no coincide" a la espera de la segunda pulsación (lo usa la prueba).
    public bool EsperandoConfirmacion => confirmarVial != null && Time.time < confirmarHasta;

    void Awake()
    {
        if (piston != null) pistonPos = piston.localPosition;
        if (capsula != null) capsula.gameObject.SetActive(false);
        if (ranura != null) ranura.Cambio += _ => { confirmarVial = null; Refrescar(); };
    }

    void OnEnable()
    {
        if (formular != null) formular.Pulsado += Formular;
        RetoManager.Cambio += Refrescar;
    }

    void OnDisable()
    {
        if (formular != null) formular.Pulsado -= Formular;
        RetoManager.Cambio -= Refrescar;
    }

    void Start() => Refrescar();

    VialAgente Encajado => ranura != null && ranura.Actual != null ? ranura.Actual.GetComponent<VialAgente>() : null;

    void Refrescar()
    {
        var rm = RetoManager.Instancia;
        // FORMULAR se ve gris mientras no se puede usar (sin vial, o aún no toca).
        if (formular != null) formular.Habilitar(!ocupada && Encajado != null && (rm == null || rm.PuedeFormular(out _)));
        if (pantalla == null || ocupada) return;
        var reto = rm != null ? rm.reto : null;
        var v = Encajado;
        string decision = rm == null ? "" : rm.Hipotesis == null
            ? "<color=#FFD25A>Sin hipótesis: ve a la TERMINAL (5)</color>"
            : $"{rm.Hipotesis.nombre} · {(rm.EstrategiaElegida != null ? rm.EstrategiaElegida.corto : "<color=#FFD25A>sin estrategia</color>")}";
        string cuerpo;
        if (v != null && reto != null)
        {
            var a = reto.agentes[v.agente];
            string encaja = rm.EstrategiaElegida == null ? ""
                : rm.EsCoherente(v.agente) ? "\n<color=#39FF88>[OK] COINCIDE con tu estrategia</color>"
                : "\n<color=#FFB347>[!] NO COINCIDE con tu estrategia</color>";
            cuerpo = $"<b>{a.sigla}</b> · {a.nombre}\n<size=75%>{a.accion}</size>{encaja}\n<color=#8FD8FF>Pulsa FORMULAR</color>";
        }
        else cuerpo = "Encaja aquí un vial de agente\n<size=75%>(arrástralo hasta la silueta)</size>";
        pantalla.text = $"<size=70%>{decision}</size>\n{cuerpo}\n<size=55%><color=#7F93A8><i>{TerminalHipotesis.AvisoSimplificado}</i></color></size>";
    }

    public void Formular()
    {
        if (ocupada) return;
        // Encima de la pantalla de la prensa (el origen de la estación está en el suelo, dentro de la mesa).
        var aviso = (pantalla != null ? pantalla.transform.position : transform.position + Vector3.up * 0.9f) + Vector3.up * 0.32f;
        var v = Encajado;
        if (v == null)
        {
            Aviso.Mostrar("Primero encaja un vial de agente en la ranura de la prensa.", aviso, Aviso.Tipo.Falta);
            Sonidos.Pista(transform.position);
            return;
        }
        var rm = RetoManager.Instancia;
        if (rm != null && !rm.PuedeFormular(out var motivo))
        {
            Aviso.Mostrar(motivo, aviso, Aviso.Tipo.Falta);
            Sonidos.Pista(transform.position);
            return;
        }
        // Vial que no sigue la estrategia: primera pulsación avisa, la segunda (en pocos segundos) formula.
        if (rm != null && rm.EstrategiaElegida != null && !rm.EsCoherente(v.agente) && !(confirmarVial == v && Time.time < confirmarHasta))
        {
            confirmarVial = v;
            confirmarHasta = Time.time + tiempoConfirmar;
            var a = rm.reto.agentes[v.agente];
            Aviso.Mostrar($"El agente {a.sigla} no hace lo que decidiste ({rm.EstrategiaElegida.nombre.ToLower()}).\nPulsa FORMULAR otra vez para probarlo igual, o cambia de vial.", aviso, Aviso.Tipo.Pista, tiempoConfirmar);
            Sonidos.Pista(transform.position);
            return;
        }
        confirmarVial = null;
        StartCoroutine(Secuencia(v));
    }

    IEnumerator Secuencia(VialAgente v)
    {
        ocupada = true;
        if (formular != null) formular.Habilitar(false);
        var a = RetoManager.Instancia.reto.agentes[v.agente];
        Sonidos.Camara(transform.position);
        if (pantalla != null) pantalla.text = $"Formulando cápsula <b>{a.sigla}</b>...";
        for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
        {
            if (piston != null) piston.localPosition = pistonPos + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.06f;
            if (luz != null) luz.enabled = Mathf.Repeat(t * 6f, 1f) > 0.5f;
            yield return null;
        }
        if (piston != null) piston.localPosition = pistonPos;
        if (luz != null) luz.enabled = true;

        // La cápsula anterior (si no se usó) se reemplaza: solo hay una a la vez.
        capsula.gameObject.SetActive(true);
        capsula.Encajable.Desencajar();
        capsula.Configurar(v.agente, a);
        capsula.Encajable.Encajar(salida, true);
        v.Encajable.VolverACasa();
        Sonidos.Logro(transform.position);
        Aviso.Mostrar($"Cápsula {a.sigla} lista. Llévala a la CÁMARA EXPERIMENTAL (7).", salida.transform.position + Vector3.up * 0.2f, Aviso.Tipo.Ok);
        ocupada = false;
        RetoManager.Instancia.CapsulaFormulada(v.agente);
        Refrescar();
    }
}
