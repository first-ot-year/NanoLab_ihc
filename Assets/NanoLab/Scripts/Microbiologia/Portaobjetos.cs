using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using TMPro;
using UnityEngine;

// Portaobjetos que se agarra con el Meta Interaction SDK (Grabbable + GrabInteractable +
// PhysicsGrabbable + HandGrab/DistanceGrab). Este script solo añade la lógica del
// laboratorio: cargar la muestra al tocar un tubo/placa y encajarse en la platina al soltarlo.
[RequireComponent(typeof(Rigidbody))]
public class Portaobjetos : MonoBehaviour
{
    public MuestraData muestra;
    public Renderer frotis;
    public TMP_Text etiqueta;
    public Renderer brillo;              // se enciende cuando el SDK apunta/pasa por encima (hover)
    public float radioPlatina = 0.12f;
    public float radioMuestra = 0.06f;

    public static readonly List<Portaobjetos> Todos = new List<Portaobjetos>();
    public static bool AlgunoAgarrado { get { foreach (var p in Todos) if (p.Agarrado) return true; return false; } }

    public bool Agarrado => grabbable != null && grabbable.SelectingPointsCount > 0;
    public Platina Encajada { get; private set; }

    Grabbable grabbable;
    Rigidbody rb;
    bool estabaAgarrado;
    int hovers;
    FuenteMuestra ultimaFuente;
    MaterialPropertyBlock mpb;
    static readonly Collider[] buffer = new Collider[32];

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        mpb = new MaterialPropertyBlock();
        if (brillo != null) brillo.enabled = false;
        Refrescar();
    }

    void OnEnable()
    {
        Todos.Add(this);
        if (grabbable != null) grabbable.WhenPointerEventRaised += AlEvento;
    }

    void OnDisable()
    {
        Todos.Remove(this);
        if (grabbable != null) grabbable.WhenPointerEventRaised -= AlEvento;
    }

    void AlEvento(PointerEvent e)
    {
        if (e.Type == PointerEventType.Hover) hovers++;
        else if (e.Type == PointerEventType.Unhover) hovers = Mathf.Max(0, hovers - 1);
        if (brillo != null) brillo.enabled = hovers > 0 && !Agarrado;
    }

    void Update()
    {
        bool agarrado = Agarrado;
        if (agarrado && !estabaAgarrado) AlAgarrar();
        else if (!agarrado && estabaAgarrado) StartCoroutine(AlSoltar());
        estabaAgarrado = agarrado;

        if (!agarrado) return;
        // Mientras se lleva en la mano: tocar un tubo/placa carga la muestra (o la limpia el desinfectante).
        var f = Buscar<FuenteMuestra>(transform.position, radioMuestra);
        if (f != null && f != ultimaFuente) f.Aplicar(this);
        ultimaFuente = f;
    }

    void AlAgarrar()
    {
        if (brillo != null) brillo.enabled = false;
        if (Encajada != null) { Encajada.Liberar(this); Encajada = null; }
        ManosRig.Vibrar(transform.position, 0.3f, 0.05f);
        Sonidos.Clic(transform.position);
    }

    // Un frame después de soltar: el SDK ya restauró la física; si hay platina cerca se encaja.
    IEnumerator AlSoltar()
    {
        ultimaFuente = null;
        yield return null;
        if (Agarrado) yield break;
        var p = PlatinaCercana();
        if (p != null) Encajar(p);
        else { rb.isKinematic = false; rb.useGravity = true; }
    }

    Platina PlatinaCercana()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, radioPlatina, buffer, ~0, QueryTriggerInteraction.Collide);
        Platina mejor = null;
        float d = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var p = buffer[i].GetComponentInParent<Platina>();
            if (p == null || !p.Libre || p.anclaje == null) continue;
            float x = (p.anclaje.position - transform.position).sqrMagnitude;
            if (x < d) { d = x; mejor = p; }
        }
        return mejor;
    }

    static T Buscar<T>(Vector3 pos, float radio) where T : Component
    {
        int n = Physics.OverlapSphereNonAlloc(pos, radio, buffer, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var c = buffer[i].GetComponentInParent<T>();
            if (c != null) return c;
        }
        return null;
    }

    public void Encajar(Platina p)
    {
        Encajada = p;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(p.anclaje.position, p.anclaje.rotation);
        p.Recibir(this);
        ManosRig.Vibrar(transform.position, 0.7f, 0.12f);
        Sonidos.Encaje(transform.position);
    }

    public void Cargar(MuestraData m)
    {
        if (m == null || m == muestra) return;
        muestra = m;
        Refrescar();
        ManosRig.Vibrar(transform.position, 0.5f, 0.1f);
        Sonidos.Carga(transform.position);
        if (isActiveAndEnabled) StartCoroutine(AnimarFrotis());
    }

    public void Limpiar()
    {
        if (muestra == null) return;
        muestra = null;
        Refrescar();
        ManosRig.Vibrar(transform.position, 0.3f, 0.08f);
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
}
