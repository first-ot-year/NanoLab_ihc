using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

// Sitio de una vitrina, cajón, ultracongelador o criotanque donde se guarda un objeto agarrable
// (portaobjetos, criotubo, placa de Petri). Al soltar el objeto cerca de un hueco libre se encaja
// en él y se queda quieto (se mueve con el cajón). Si su compartimento está cerrado no se puede
// guardar ni sacar nada: los interactables del objeto guardado se desactivan.
public class Hueco : MonoBehaviour
{
    public Compartimento compartimento;
    [Tooltip("Silueta que se enciende al acercar la mano con algo agarrado.")]
    public Renderer fantasma;
    public float radio = 0.08f;

    public static readonly List<Hueco> Todos = new List<Hueco>();

    public Transform Ocupante { get; private set; }
    public bool Libre => Ocupante == null;
    public bool Accesible => compartimento == null || compartimento.Abierto;

    bool bloqueado;

    void Awake()
    {
        // Objeto colocado desde el editor (hijo con Rigidbody).
        foreach (Transform h in transform)
            if (h.GetComponent<Rigidbody>() != null) { Recibir(h, false); break; }
        if (fantasma != null) fantasma.enabled = false;
    }

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    void Update()
    {
        if (Ocupante != null && Ocupante.parent != transform) Ocupante = null;   // se movió por otra vía
        bool acc = Accesible;
        if (Ocupante != null && bloqueado == acc) Bloquear(!acc);
        if (fantasma != null)
            fantasma.enabled = Libre && acc && AlgoEnLaMano && ManosRig.MasCercana(transform.position, 0.45f, out _);
    }

    // Se calcula una vez por frame para todos los huecos.
    static int frameMano = -1;
    static bool enLaMano;
    static bool AlgoEnLaMano
    {
        get
        {
            if (frameMano != Time.frameCount) { frameMano = Time.frameCount; enLaMano = Portaobjetos.AlgunoAgarrado || Guardable.AlgunoAgarrado; }
            return enLaMano;
        }
    }

    public void Recibir(Transform obj, bool efectos = true)
    {
        Ocupante = obj;
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true;
        }
        obj.SetParent(transform, true);
        obj.SetPositionAndRotation(transform.position + transform.up * Elevacion(obj), transform.rotation);
        bloqueado = false;
        Bloquear(!Accesible);
        if (!efectos) return;
        Sonidos.Encaje(obj.position);
        ManosRig.Vibrar(obj.position, 0.5f, 0.08f);
    }

    // Los portaobjetos tienen el pivote en el centro del vidrio (1 mm): se elevan para no hundirse.
    static float Elevacion(Transform obj) => obj.GetComponent<Portaobjetos>() != null ? 0.0016f : 0f;

    void Bloquear(bool b)
    {
        bloqueado = b;
        if (Ocupante != null) Interactuable(Ocupante, !b);
    }

    static void Interactuable(Transform obj, bool si)
    {
        foreach (var mb in obj.GetComponents<MonoBehaviour>())
            if (mb is IInteractable) mb.enabled = si;
    }

    // ------------------------------------------------------------------ uso desde los objetos

    public static Hueco De(Transform obj)
    {
        var h = obj.parent != null ? obj.parent.GetComponent<Hueco>() : null;
        return h != null && h.Ocupante == obj ? h : null;
    }

    // Lo encaja en el hueco libre y accesible más cercano, si hay alguno a su alcance.
    public static bool Guardar(Transform obj)
    {
        Hueco mejor = null;
        float d = float.MaxValue;
        foreach (var h in Todos)
        {
            if (!h.Libre || !h.Accesible) continue;
            float x = (h.transform.position - obj.position).sqrMagnitude;
            if (x < h.radio * h.radio && x < d) { d = x; mejor = h; }
        }
        if (mejor == null) return false;
        mejor.Recibir(obj);
        return true;
    }

    // Lo saca de su hueco (al agarrarlo) y lo deja suelto en la escena.
    public static void Sacar(Transform obj)
    {
        var h = De(obj);
        if (h == null) return;
        h.Ocupante = null;
        h.bloqueado = false;
        obj.SetParent(null, true);
        Interactuable(obj, true);
    }

    // Altura del suelo bajo un punto: la superficie más baja que encuentra un rayo hacia abajo.
    public static float SueloBajo(Vector3 p, Transform ignorar)
    {
        float y = p.y - 1f;
        bool alguno = false;
        foreach (var hit in Physics.RaycastAll(p + Vector3.up * 0.05f, Vector3.down, 5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (ignorar != null && hit.collider.transform.IsChildOf(ignorar)) continue;
            if (!alguno || hit.point.y < y) { y = hit.point.y; alguno = true; }
        }
        return y;
    }
}
