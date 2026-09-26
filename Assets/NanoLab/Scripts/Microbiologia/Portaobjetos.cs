using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Portaobjetos agarrable. Guarda la muestra que se le ha puesto y se encaja
// en la platina de un microscopio (o de la estación 3D) al soltarlo encima.
[RequireComponent(typeof(OVRGrabbable), typeof(Rigidbody))]
public class Portaobjetos : MonoBehaviour
{
    public MuestraData muestra;
    public Renderer frotis;
    public TMP_Text etiqueta;

    public static readonly List<Portaobjetos> Todos = new List<Portaobjetos>();
    public static bool AlgunoAgarrado { get { foreach (var p in Todos) if (p.Agarrado) return true; return false; } }

    public bool Agarrado => grab != null && grab.isGrabbed;
    public Platina Encajada { get; private set; }

    OVRGrabbable grab;
    Rigidbody rb;
    bool estabaAgarrado;
    Platina platinaCerca;
    ManoNanoLab ultimaMano;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        grab = GetComponent<OVRGrabbable>();
        rb = GetComponent<Rigidbody>();
        mpb = new MaterialPropertyBlock();
        Refrescar();
    }

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    void Update()
    {
        bool agarrado = Agarrado;
        if (agarrado && !estabaAgarrado)
        {
            ultimaMano = grab.grabbedBy ? grab.grabbedBy.GetComponent<ManoNanoLab>() : null;
            if (Encajada != null) { Encajada.Liberar(this); Encajada = null; }
            ultimaMano?.Vibrar(0.3f, 0.05f);
            Sonidos.Clic(transform.position);
        }
        else if (!agarrado && estabaAgarrado)
        {
            if (platinaCerca != null && platinaCerca.Libre) Encajar(platinaCerca);
            else { rb.isKinematic = false; rb.useGravity = true; }
        }
        estabaAgarrado = agarrado;
    }

    public void Encajar(Platina p)
    {
        Encajada = p;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(p.anclaje.position, p.anclaje.rotation);
        p.Recibir(this);
        ultimaMano?.Vibrar(0.7f, 0.12f);
        Sonidos.Encaje(transform.position);
    }

    public void Cargar(MuestraData m)
    {
        if (m == null || m == muestra) return;
        muestra = m;
        Refrescar();
        ultimaMano?.Vibrar(0.5f, 0.1f);
        Sonidos.Carga(transform.position);
        if (isActiveAndEnabled) StartCoroutine(AnimarFrotis());
    }

    public void Limpiar()
    {
        if (muestra == null) return;
        muestra = null;
        Refrescar();
        ultimaMano?.Vibrar(0.3f, 0.08f);
        Sonidos.Clic(transform.position);
    }

    void Refrescar()
    {
        if (frotis != null)
        {
            frotis.enabled = muestra != null;
            if (muestra != null)
            {
                frotis.GetPropertyBlock(mpb);
                var c = muestra.color; c.a = 0.85f;
                mpb.SetColor("_Color", c);
                frotis.SetPropertyBlock(mpb);
            }
        }
        if (etiqueta != null) etiqueta.text = muestra != null ? muestra.nombre : "LIMPIO";
    }

    IEnumerator AnimarFrotis()
    {
        if (frotis == null) yield break;
        var t = frotis.transform;
        var final = t.localScale;
        for (float k = 0; k < 1f; k += Time.deltaTime * 4f)
        {
            t.localScale = final * Mathf.SmoothStep(0.2f, 1f, k);
            yield return null;
        }
        t.localScale = final;
    }

    void OnTriggerEnter(Collider c)
    {
        var p = c.GetComponentInParent<Platina>();
        if (p != null) platinaCerca = p;
        if (!Agarrado) return;
        var f = c.GetComponentInParent<FuenteMuestra>();
        if (f != null) f.Aplicar(this);
    }

    void OnTriggerExit(Collider c)
    {
        var p = c.GetComponentInParent<Platina>();
        if (p != null && p == platinaCerca) platinaCerca = null;
    }
}
