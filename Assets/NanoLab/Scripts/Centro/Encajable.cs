using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

// Objeto agarrable (Meta Interaction SDK) que encaja en una ZonaEncaje de su tipo al soltarlo cerca.
// Si se cae al suelo o se aleja demasiado, vuelve solo a su sitio (tolerancia al error, RNF-05).
[RequireComponent(typeof(Rigidbody))]
public class Encajable : MonoBehaviour
{
    public string tipo = "placa";
    public ZonaEncaje casa;                 // sitio inicial y de retorno
    public float radioEncaje = 0.14f;
    public float distanciaMaxima = 7f;
    public Renderer brillo;                 // hover del SDK

    public static readonly List<Encajable> Todos = new List<Encajable>();
    public static bool AlgunoAgarrado(string tipo)
    {
        foreach (var e in Todos) if (e.tipo == tipo && e.Agarrado) return true;
        return false;
    }

    public bool Agarrado => grabbable != null && grabbable.SelectingPointsCount > 0;
    public ZonaEncaje Encajada { get; private set; }
    public event System.Action<Encajable> Agarrar, Soltar;

    Grabbable grabbable;
    Rigidbody rb;
    bool estabaAgarrado;
    int hovers;
    float tiempoFuera;
    static readonly Collider[] buffer = new Collider[32];

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        if (brillo != null) brillo.enabled = false;
    }

    void Start()
    {
        if (Encajada == null && casa != null && casa.Libre) Encajar(casa, false);
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

        if (agarrado) return;
        if (Encajada != null)
        {
            // Sigue al anclaje (p. ej. la bandeja móvil de la cámara).
            if (Encajada.anclaje != null) transform.SetPositionAndRotation(Encajada.anclaje.position, Encajada.anclaje.rotation);
            return;
        }
        // Suelto: si está en el suelo o muy lejos, vuelve a casa al cabo de un momento.
        bool perdido = casa != null && (transform.position.y < casa.transform.position.y - 0.4f || (transform.position - casa.transform.position).sqrMagnitude > distanciaMaxima * distanciaMaxima);
        tiempoFuera = perdido ? tiempoFuera + Time.deltaTime : 0f;
        if (tiempoFuera > 1.5f) VolverACasa();
    }

    void AlAgarrar()
    {
        if (brillo != null) brillo.enabled = false;
        if (Encajada != null) { Encajada.Liberar(this); Encajada = null; }
        ManosRig.Vibrar(transform.position, 0.3f, 0.05f);
        Sonidos.Clic(transform.position);
        Agarrar?.Invoke(this);
    }

    IEnumerator AlSoltar()
    {
        yield return null;   // el SDK restaura la física un frame después
        if (Agarrado) yield break;
        var z = ZonaCercana();
        if (z != null) Encajar(z, true);
        else { rb.isKinematic = false; rb.useGravity = true; }
        Soltar?.Invoke(this);
    }

    ZonaEncaje ZonaCercana()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, radioEncaje, buffer, ~0, QueryTriggerInteraction.Collide);
        ZonaEncaje mejor = null;
        float d = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var z = buffer[i].GetComponentInParent<ZonaEncaje>();
            if (z == null || z.tipo != tipo || !z.aceptaSoltar || !z.Libre || z.anclaje == null) continue;
            float x = (z.anclaje.position - transform.position).sqrMagnitude;
            if (x < d) { d = x; mejor = z; }
        }
        return mejor;
    }

    public void Encajar(ZonaEncaje z, bool conEfecto)
    {
        if (Encajada != null && Encajada != z) Encajada.Liberar(this);
        Encajada = z;
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(z.anclaje.position, z.anclaje.rotation);
        z.Recibir(this);
        if (!conEfecto) return;
        ManosRig.Vibrar(transform.position, 0.7f, 0.12f);
        Sonidos.Encaje(transform.position);
    }

    // Impide agarrarlo mientras una máquina lo está usando (p. ej. la cámara experimental).
    public void Bloquear(bool b)
    {
        foreach (var c in GetComponents<MonoBehaviour>()) if (c is IInteractable) c.enabled = !b;
    }

    public void Desencajar()
    {
        if (Encajada != null) { Encajada.Liberar(this); Encajada = null; }
    }

    public void VolverACasa()
    {
        tiempoFuera = 0f;
        if (casa == null) return;
        if (casa.Libre) Encajar(casa, false);
        else
        {
            Desencajar();
            rb.isKinematic = true;
            transform.SetPositionAndRotation(casa.anclaje.position + Vector3.up * 0.05f, casa.anclaje.rotation);
        }
        Sonidos.Clic(transform.position);
        Aviso.Mostrar("Ha vuelto a su sitio.", transform.position + Vector3.up * 0.15f, Aviso.Tipo.Info, 2.5f);
    }
}
