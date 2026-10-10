using System.Collections;
using TMPro;
using UnityEngine;

// Prensa de cápsulas: se encaja un vial de agente, se pulsa FORMULAR y sale una cápsula
// del color del agente lista para la cámara experimental.
public class PrensaCapsulas : MonoBehaviour
{
    public ZonaEncaje ranura;          // tipo "vial"
    public ZonaEncaje salida;          // tipo "capsula"
    public BotonFisico formular;
    public Capsula capsula;
    public Transform piston;
    public TMP_Text pantalla;
    public Renderer luz;

    bool ocupada;
    Vector3 pistonPos;

    void Awake()
    {
        if (piston != null) pistonPos = piston.localPosition;
        if (capsula != null) capsula.gameObject.SetActive(false);
        if (ranura != null) ranura.Cambio += _ => Refrescar();
    }

    void OnEnable() { if (formular != null) formular.Pulsado += Formular; }
    void OnDisable() { if (formular != null) formular.Pulsado -= Formular; }
    void Start() => Refrescar();

    void Refrescar()
    {
        if (pantalla == null) return;
        var reto = RetoManager.Instancia != null ? RetoManager.Instancia.reto : null;
        var v = ranura != null && ranura.Actual != null ? ranura.Actual.GetComponent<VialAgente>() : null;
        pantalla.text = v != null && reto != null
            ? $"<b>{reto.agentes[v.agente].sigla}</b> · {reto.agentes[v.agente].nombre}\n<size=75%>{reto.agentes[v.agente].accion}</size>\n<color=#8FD8FF>Pulsa FORMULAR</color>"
            : "Encaja aquí un vial de agente\n<size=75%>(arrástralo hasta la silueta)</size>";
    }

    public void Formular()
    {
        if (ocupada) return;
        var aviso = transform.position + Vector3.up * 0.55f;
        var v = ranura.Actual != null ? ranura.Actual.GetComponent<VialAgente>() : null;
        if (v == null)
        {
            Aviso.Mostrar("Primero encaja un vial de agente en la ranura de la prensa.", aviso, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        var rm = RetoManager.Instancia;
        if (rm != null && !rm.PuedeFormular(out var motivo))
        {
            Aviso.Mostrar(motivo, aviso, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        StartCoroutine(Secuencia(v));
    }

    IEnumerator Secuencia(VialAgente v)
    {
        ocupada = true;
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
        RetoManager.Instancia.CapsulaFormulada(v.agente);
        Refrescar();
        ocupada = false;
    }
}
