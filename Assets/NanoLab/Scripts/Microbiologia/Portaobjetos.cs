using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using TMPro;
using UnityEngine;

// Portaobjetos que se agarra con el Meta Interaction SDK (Grabbable + GrabInteractable +
// PhysicsGrabbable + HandGrab/DistanceGrab). Este script solo añade la lógica del
// laboratorio: cargar la muestra al tocar un tubo/placa y encajarse en la platina al soltarlo.
// Si se cae al suelo o queda muy lejos, vuelve solo a su bandeja (tolerancia al error, RNF-04).
[RequireComponent(typeof(Rigidbody))]
public class Portaobjetos : MonoBehaviour
{
    public MuestraData muestra;
    public Renderer frotis;
    public TMP_Text etiqueta;
    public Renderer brillo;              // se enciende cuando el SDK apunta/pasa por encima (hover)
    public float radioPlatina = 0.12f;
    public float radioMuestra = 0.06f;
    [Tooltip("Si queda suelto a más de esta distancia de su bandeja (m), vuelve a ella.")]
    public float distanciaMaxima = 8f;

    public static readonly List<Portaobjetos> Todos = new List<Portaobjetos>();
    public static bool AlgunoAgarrado { get { foreach (var p in Todos) if (p.Agarrado) return true; return false; } }
    public static event System.Action<Portaobjetos> TincionCambiada;

    public bool Agarrado => grabbable != null && grabbable.SelectingPointsCount > 0;
    public Platina Encajada { get; private set; }

    // Tinción de Gram: 0 = sin teñir; 1..4 = pasos aplicados en orden. Un orden incorrecto la invalida.
    public static readonly string[] Reactivos = { "", "Cristal violeta", "Lugol", "Alcohol-acetona", "Safranina" };
    public int PasosTincion { get; private set; }
    public bool TincionErronea { get; private set; }
    public bool TincionCompleta => PasosTincion == 4 && !TincionErronea;
    public bool Despues { get; private set; }      // portaobjetos que entrega la cámara tras el experimento
    public bool DeCaso => etiquetaTexto != null;   // muestra de un reto (su identidad no se revela)
    string etiquetaTexto;

    Grabbable grabbable;
    Rigidbody rb;
    bool estabaAgarrado;
    int hovers;
    FuenteMuestra ultimaFuente;
    Vector3 casaPos;
    Quaternion casaRot;
    float tiempoFuera, ultimoGolpe;
    MaterialPropertyBlock mpb;
    static readonly Collider[] buffer = new Collider[32];

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        mpb = new MaterialPropertyBlock();
        casaPos = transform.position;
        casaRot = transform.rotation;
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

        if (!agarrado)
        {
            Recuperar();
            return;
        }
        // Mientras se lleva en la mano: tocar un tubo/placa carga la muestra (o la limpia el desinfectante).
        var f = Buscar<FuenteMuestra>(transform.position, radioMuestra);
        if (f != null && f != ultimaFuente) f.Aplicar(this);
        ultimaFuente = f;
    }

    // Sonido de vidrio al chocar (más fuerte cuanto más rápido); no se repite en el mismo instante.
    void OnCollisionEnter(Collision c)
    {
        float v = c.relativeVelocity.magnitude;
        if (v < 0.25f || Time.time - ultimoGolpe < 0.12f) return;
        ultimoGolpe = Time.time;
        Sonidos.Tintineo(transform.position, v / 2f);
    }

    // Suelto en el suelo (por debajo de su bandeja) o muy lejos: vuelve a la bandeja al cabo de un momento.
    void Recuperar()
    {
        bool perdido = Encajada == null &&
                       (transform.position.y < casaPos.y - 0.4f || (transform.position - casaPos).sqrMagnitude > distanciaMaxima * distanciaMaxima);
        tiempoFuera = perdido ? tiempoFuera + Time.deltaTime : 0f;
        if (tiempoFuera < 1.5f) return;
        tiempoFuera = 0f;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.SetPositionAndRotation(casaPos + Vector3.up * 0.01f, casaRot);
        Sonidos.Clic(transform.position);
        Aviso.Mostrar("El portaobjetos ha vuelto a su bandeja.", transform.position + Vector3.up * 0.15f, Aviso.Tipo.Info, 3f);
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

    public void Cargar(MuestraData m, string etiquetaCaso = null)
    {
        if (m == null || m == muestra) return;
        muestra = m;
        etiquetaTexto = string.IsNullOrEmpty(etiquetaCaso) ? null : etiquetaCaso;
        PasosTincion = 0; TincionErronea = false; Despues = false;
        Refrescar();
        ManosRig.Vibrar(transform.position, 0.5f, 0.1f);
        Sonidos.Carga(transform.position);
        if (isActiveAndEnabled) StartCoroutine(AnimarFrotis());
        TincionCambiada?.Invoke(this);
    }

    public void Limpiar()
    {
        if (muestra == null) return;
        muestra = null;
        etiquetaTexto = null;
        PasosTincion = 0; TincionErronea = false; Despues = false;
        Refrescar();
        ManosRig.Vibrar(transform.position, 0.3f, 0.08f);
        Sonidos.Clic(transform.position);
        TincionCambiada?.Invoke(this);
    }

    // Lo deja quieto en un punto (p. ej. la bandeja de salida de la cámara), soltándolo de la platina.
    public void ColocarEn(Vector3 pos, Quaternion rot)
    {
        if (Encajada != null) { Encajada.Liberar(this); Encajada = null; }
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        transform.SetPositionAndRotation(pos, rot);
        casaPos = pos; casaRot = rot;   // si luego se cae, vuelve aquí
    }

    // Portaobjetos ya teñido que entrega la cámara experimental tras la intervención.
    public void PrepararDespues(MuestraData m)
    {
        muestra = m;
        etiquetaTexto = "DESPUÉS";
        PasosTincion = 4; TincionErronea = false; Despues = true;
        Refrescar();
    }

    // Toca un gotero de la tinción de Gram (1 cristal violeta, 2 lugol, 3 alcohol-acetona, 4 safranina).
    public void AplicarReactivo(int r)
    {
        var aviso = transform.position + Vector3.up * 0.12f;
        if (muestra == null)
        {
            Aviso.Mostrar("Primero haz el frotis: toca la placa de la muestra con este portaobjetos.", aviso, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        if (Despues) return;
        if (TincionErronea)
        {
            Aviso.Mostrar("Tinción NO válida. Límpialo con el desinfectante y empieza otra vez: 1 > 2 > 3 > 4.", aviso, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        int esperado = PasosTincion + 1;
        if (r < esperado)
        {
            Aviso.Mostrar(esperado <= 4 ? $"Ese reactivo ya está aplicado. Ahora toca el gotero {esperado}: {Reactivos[esperado]}." : "La tinción ya está completa. Llévalo al microscopio.", aviso, Aviso.Tipo.Info);
            return;
        }
        if (r > esperado)
        {
            TincionErronea = true;
            string porQue = r == 3 && esperado <= 2 ? "Sin cristal violeta fijado con lugol, el alcohol decolora TODAS las células y el resultado engaña."
                          : r == 4 ? "La safranina va al final, después de decolorar con alcohol."
                          : $"Te saltaste el paso {esperado} ({Reactivos[esperado]}).";
            Aviso.Mostrar($"Orden incorrecto. {porQue}\nLímpialo con el desinfectante y repite 1 > 2 > 3 > 4.", aviso, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            ManosRig.Vibrar(transform.position, 0.8f, 0.2f);
        }
        else
        {
            PasosTincion = r;
            Aviso.Mostrar(r < 4 ? $"Paso {r}/4: {Reactivos[r]} - correcto\nSigue con el gotero {r + 1}." : "Paso 4/4: Safranina - correcto\nTinción de Gram completa. Llévalo al microscopio.", aviso, Aviso.Tipo.Ok);
            Sonidos.Gota(transform.position);
            ManosRig.Vibrar(transform.position, 0.4f, 0.06f);
        }
        Refrescar();
        TincionCambiada?.Invoke(this);
    }

    // Color del frotis según la tinción (las Gram + retienen el violeta; las Gram − se ven rosas al final).
    Color ColorFrotis()
    {
        bool gramPos = muestra.tipo != TipoMuestra.Bacteria || muestra.gramPositiva;
        var violeta = new Color(0.45f, 0.15f, 0.75f, 0.9f);
        if (TincionErronea) return new Color(0.8f, 0.55f, 0.65f, 0.85f);
        switch (PasosTincion)
        {
            case 1: return violeta;
            case 2: return new Color(0.28f, 0.08f, 0.5f, 0.92f);
            case 3: return gramPos ? violeta : new Color(0.92f, 0.88f, 0.84f, 0.5f);
            case 4: return gramPos ? violeta : new Color(0.95f, 0.4f, 0.6f, 0.9f);
        }
        var c = etiquetaTexto != null ? new Color(0.92f, 0.86f, 0.62f) : muestra.color;   // muestra sin teñir
        c.a = 0.85f;
        return c;
    }

    void Refrescar()
    {
        if (frotis != null)
        {
            frotis.enabled = muestra != null;
            if (muestra != null)
            {
                frotis.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", ColorFrotis());
                frotis.SetPropertyBlock(mpb);
            }
        }
        if (etiqueta != null) etiqueta.text = muestra == null ? "LIMPIO" : etiquetaTexto ?? muestra.nombre;
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
