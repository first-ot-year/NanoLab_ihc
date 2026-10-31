using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

// Objeto agarrable que se saca y se guarda en los huecos de vitrinas, cajones, el ultracongelador
// o el criotanque (criotubos, placas de Petri). Al soltarlo cerca de un hueco libre y abierto se
// encaja en él; si no, cae con física normal. Si acaba en el suelo o muy lejos, vuelve a su sitio
// de origen (tolerancia al error, RNF-04). Los portaobjetos hacen lo mismo desde Portaobjetos.
[RequireComponent(typeof(Rigidbody))]
public class Guardable : MonoBehaviour
{
    [Tooltip("Si queda suelto a más de esta distancia de su sitio (m), vuelve a él.")]
    public float distanciaMaxima = 30f;

    public static readonly List<Guardable> Todos = new List<Guardable>();
    public static bool AlgunoAgarrado { get { foreach (var g in Todos) if (g.Agarrado) return true; return false; } }

    public bool Agarrado => grabbable != null && grabbable.SelectingPointsCount > 0;

    Grabbable grabbable;
    Rigidbody rb;
    bool estabaAgarrado;
    Hueco casa;
    Vector3 casaPos;
    Quaternion casaRot;
    float suelo, tiempoFuera, ultimoGolpe;

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        casa = transform.parent != null ? transform.parent.GetComponent<Hueco>() : null;
        casaPos = transform.position;
        casaRot = transform.rotation;
    }

    void Start() => suelo = Hueco.SueloBajo(casaPos, transform);

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    void Update()
    {
        bool agarrado = Agarrado;
        if (agarrado && !estabaAgarrado)
        {
            Hueco.Sacar(transform);
            ManosRig.Vibrar(transform.position, 0.3f, 0.05f);
            Sonidos.Clic(transform.position);
        }
        else if (!agarrado && estabaAgarrado) StartCoroutine(AlSoltar());
        estabaAgarrado = agarrado;

        if (agarrado || Hueco.De(transform) != null) { tiempoFuera = 0f; return; }
        bool perdido = transform.position.y < suelo + 0.2f || (transform.position - casaPos).sqrMagnitude > distanciaMaxima * distanciaMaxima;
        tiempoFuera = perdido ? tiempoFuera + Time.deltaTime : 0f;
        if (tiempoFuera > 1.5f) VolverACasa();
    }

    // Un frame después de soltarlo (el SDK ya restauró la física): a un hueco cercano o con gravedad.
    IEnumerator AlSoltar()
    {
        yield return null;
        if (Agarrado || Hueco.Guardar(transform)) yield break;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void OnCollisionEnter(Collision c)
    {
        float v = c.relativeVelocity.magnitude;
        if (v < 0.25f || Time.time - ultimoGolpe < 0.12f) return;
        ultimoGolpe = Time.time;
        Sonidos.Tintineo(transform.position, v / 3f);
    }

    public void VolverACasa()
    {
        tiempoFuera = 0f;
        if (!rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        if (casa != null && casa.Libre) casa.Recibir(transform, false);
        else
        {
            rb.isKinematic = true;
            transform.SetPositionAndRotation(casaPos, casaRot);
        }
        Sonidos.Clic(transform.position);
        Aviso.Mostrar("Ha vuelto a su sitio.", transform.position + Vector3.up * 0.15f, Aviso.Tipo.Info, 2.5f);
    }
}
