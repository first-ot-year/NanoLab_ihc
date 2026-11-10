#pragma warning disable 618   // PhysicsGrabbable está marcado como obsoleto, pero es el que usan el resto de agarrables del proyecto
using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using TMPro;
using UnityEngine;
using P = ContenidoVacuna.Pieza;

// Piezas interactivas del CASO 01 · "Crea la vacuna". Todo se crea EN TIEMPO DE EJECUCIÓN (MontajeVacuna), así que
// los componentes del Meta Interaction SDK se añaden e inyectan por código con los mismos ajustes que pone
// ConfigurarMetaISDK.HacerAgarrable en el Editor: se agarran con el grip (o el rayo corto) y encajan en su zona.

// ---------------------------------------------------------------------------------------------- agarre

public static class AgarreRuntime
{
    // Convierte 'go' en un objeto agarrable que encaja en zonas de 'tipo' y vuelve a 'casa' si se pierde.
    // 'centro' y 'tam' son el collider (en local). El objeto queda activo o inactivo como estaba.
    public static Encajable Hacer(GameObject go, string tipo, ZonaEncaje casa, Vector3 centro, Vector3 tam, float masa = 0.05f)
    {
        bool activo = go.activeSelf;
        go.SetActive(false);   // los Awake/Start del SDK, cuando todo está inyectado

        var bc = go.AddComponent<BoxCollider>();
        bc.center = centro;
        bc.size = tam;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = masa;
        rb.useGravity = true;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var grab = go.AddComponent<Grabbable>();
        grab.InjectOptionalRigidbody(rb);
        var fisica = go.AddComponent<PhysicsGrabbable>();
        fisica.InjectAllPhysicsGrabbable(grab, rb);

        var gi = go.AddComponent<GrabInteractable>();
        gi.InjectAllGrabInteractable(rb);
        gi.InjectOptionalPointableElement(grab);
        gi.InjectOptionalPhysicsGrabbable(fisica);

        var dgi = go.AddComponent<DistanceGrabInteractable>();
        dgi.InjectAllGrabInteractable(rb);
        dgi.InjectOptionalPointableElement(grab);
        dgi.InjectOptionalPhysicsGrabbable(fisica);

        var hgi = go.AddComponent<HandGrabInteractable>();
        hgi.InjectRigidbody(rb);
        hgi.InjectOptionalPointableElement(grab);
        hgi.InjectOptionalPhysicsGrabbable(fisica);

        var dhgi = go.AddComponent<DistanceHandGrabInteractable>();
        dhgi.InjectRigidbody(rb);
        dhgi.InjectOptionalPointableElement(grab);
        dhgi.InjectOptionalPhysicsGrabbable(fisica);

        var e = go.AddComponent<Encajable>();
        e.tipo = tipo;
        e.casa = casa;
        e.radioEncaje = 0.12f;
        e.distanciaMaxima = 4f;

        go.SetActive(activo);
        return e;
    }

    // Zona de encaje con su anclaje, un trigger pequeño (lo detecta Encajable) y una silueta que se ve
    // mientras se lleva en la mano un objeto de su tipo.
    public static ZonaEncaje Zona(Transform padre, string nombre, string tipo, Vector3 pos, Quaternion rot, Renderer fantasma, bool aceptaSoltar = true)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        var sc = go.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.035f;
        var z = go.AddComponent<ZonaEncaje>();
        z.tipo = tipo;
        z.anclaje = go.transform;
        z.aceptaSoltar = aceptaSoltar;
        z.fantasma = fantasma;
        if (fantasma != null) { fantasma.transform.SetParent(go.transform, false); fantasma.enabled = false; }
        return z;
    }

    // Devuelve un agarrable a su sitio sin aviso (al cambiar de paso o al repetir el caso).
    public static void Recolocar(Encajable e)
    {
        if (e == null) return;
        if (!e.gameObject.activeSelf) e.gameObject.SetActive(true);
        e.Bloquear(false);
        e.Desencajar();
        if (e.casa != null && e.casa.Libre) e.Encajar(e.casa, false);
    }
}

// ---------------------------------------------------------------------------------------------- piezas

// Identidad de una pieza agarrable (bloque, cartucho, tubo, vial) y lo que lleva dentro.
public class PiezaVacuna : MonoBehaviour
{
    public P datos;
    public Renderer liquido;              // cilindro de Unity (alto 2 a escala 1)
    public float baseLiquido, altoLiquido = 0.05f;
    public TMP_Text etiqueta;
    public readonly HashSet<string> contenido = new HashSet<string>();
    public Encajable Encajable { get; private set; }
    public string Id => datos != null ? datos.id : name;

    void Awake() => Encajable = GetComponent<Encajable>();

    // Nivel 0..1 del líquido, que crece desde el fondo.
    public void Liquido(Color c, float nivel)
    {
        if (liquido == null) return;
        float h = altoLiquido * Mathf.Clamp01(nivel);
        liquido.enabled = h > 0.001f;
        var t = liquido.transform;
        t.localScale = new Vector3(t.localScale.x, Mathf.Max(0.0005f, h / 2f), t.localScale.z);
        t.localPosition = new Vector3(t.localPosition.x, baseLiquido + h / 2f, t.localPosition.z);
        KitVacuna.Pintar(liquido, c);
    }
}

// ---------------------------------------------------------------------------------------------- pipeta

// Punto donde la punta de la pipeta carga (frasco de reactivo) o descarga (tubo de reacción, placa).
public class PuntoPipeta : MonoBehaviour
{
    public static readonly List<PuntoPipeta> Todos = new List<PuntoPipeta>();

    public string nombre = "el tubo";
    public float radio = 0.06f;
    public P fuente;                                  // si no es null, aquí se CARGA ese reactivo
    public System.Func<P, Vector3, bool> recibir;     // destino: devuelve true si la pipeta se vacía
    public TMP_Text rotulo;
    public Renderer liquido;

    void OnEnable() => Todos.Add(this);
    void OnDisable() => Todos.Remove(this);

    public bool EsFuente => fuente != null;

    public void PonerFuente(P p)
    {
        fuente = p;
        if (rotulo != null) rotulo.text = p != null ? $"<b>{p.nombre}</b>\n<size=70%>{p.sub}</size>" : "";
        if (liquido != null) { liquido.enabled = p != null; if (p != null) KitVacuna.Pintar(liquido, p.color); }
    }
}

// Micropipeta: con ella en la mano, se acerca la punta a un frasco y se aprieta el gatillo (o A/X) para cargar;
// se acerca a un tubo o a la placa y se aprieta otra vez para descargar. Igual que el portaobjetos del
// microscopio (gatillo para tomar la muestra), para que el gesto sea el mismo en todo el laboratorio.
public class PipetaVacuna : MonoBehaviour
{
    public Transform punta;
    public Renderer liquido;
    public TMP_Text etiqueta;

    public P Contenido { get; private set; }
    public Encajable Encajable { get; private set; }
    PuntoPipeta cerca, avisado;

    void Awake()
    {
        Encajable = GetComponent<Encajable>();
        Vaciar();
    }

    void Update()
    {
        if (Encajable == null || !Encajable.Agarrado) { cerca = avisado = null; return; }
        cerca = PuntoCercano();
        if (cerca == null) { avisado = null; return; }
        if (cerca != avisado)
        {
            avisado = cerca;
            string accion = cerca.EsFuente ? $"cargar {cerca.fuente.nombre}" : Contenido != null ? $"descargar en {cerca.nombre}" : "(la pipeta está vacía)";
            Aviso.Mostrar($"Gatillo o A/X: {accion}", cerca.transform.position + Vector3.up * 0.12f, Aviso.Tipo.Info, 2f);
            ManosRig.Vibrar(punta.position, 0.15f, 0.03f);
        }
        var mando = Mando();
        if (mando != OVRInput.Controller.None &&
            (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, mando) || OVRInput.GetDown(OVRInput.Button.One, mando)))
            Usar(cerca);
    }

    PuntoPipeta PuntoCercano()
    {
        PuntoPipeta mejor = null;
        float d = float.MaxValue;
        foreach (var p in PuntoPipeta.Todos)
        {
            if (p == null || !p.isActiveAndEnabled) continue;
            if (p.EsFuente == false && p.recibir == null) continue;
            float x = (p.transform.position - punta.position).sqrMagnitude;
            if (x < p.radio * p.radio && x < d) { d = x; mejor = p; }
        }
        return mejor;
    }

    // Mando que sostiene la pipeta (la mano más cercana).
    OVRInput.Controller Mando()
    {
        ManosRig.MasCercana(transform.position, 0.35f, out var c);
        return c;
    }

    // Lo que hace el gatillo junto a un punto (público para las pruebas sin visor).
    public void Usar(PuntoPipeta p)
    {
        if (p == null) return;
        if (p.EsFuente) { Cargar(p.fuente, p.transform.position); return; }
        if (Contenido == null)
        {
            Aviso.Mostrar("La pipeta está vacía: carga primero un reactivo en un frasco.", p.transform.position + Vector3.up * 0.15f, Aviso.Tipo.Info, 3f);
            Sonidos.Error(p.transform.position);
            return;
        }
        var c = Contenido;
        Sonidos.Gota(p.transform.position);
        if (p.recibir != null && p.recibir(c, p.transform.position)) Vaciar();
    }

    public void Cargar(P p, Vector3 donde)
    {
        Contenido = p;
        if (liquido != null) { liquido.enabled = true; KitVacuna.Pintar(liquido, p.color); }
        if (etiqueta != null) { etiqueta.text = p.nombre; etiqueta.color = Color.Lerp(p.color, Color.white, 0.3f); }
        Sonidos.Gota(donde);
        ManosRig.Vibrar(donde, 0.35f, 0.05f);
        Aviso.Mostrar($"Pipeta cargada: {p.nombre}", donde + Vector3.up * 0.15f, Aviso.Tipo.Info, 2f);
    }

    public void Vaciar()
    {
        Contenido = null;
        if (liquido != null) liquido.enabled = false;
        if (etiqueta != null) { etiqueta.text = "vacía"; etiqueta.color = KitVacuna.Gris; }
    }
}

// ---------------------------------------------------------------------------------------------- selector

// Algo que se elige apuntándolo con el rayo de la mano vacía y apretando el gatillo (rótulos del virus, columnas
// de la placa). Lo detecta PunteroMuestras como cualquier Apuntable; también se activa al tocarlo con la mano.
public class SelectorVacuna : Apuntable
{
    public string accion = "elegir";
    public System.Action alActivar;
    public bool tactil = true;
    public float radioTactil = 0.06f;
    float ultimo = -1f;
    bool dentro;

    public override string Accion => accion;

    public override void Activar()
    {
        if (Time.time - ultimo < 0.3f) return;
        ultimo = Time.time;
        ManosRig.Vibrar(transform.position, 0.4f, 0.04f);
        alActivar?.Invoke();
    }

    void Update()
    {
        if (!tactil) return;
        bool ahora = ManosRig.MasCercana(transform.position, radioTactil, out _);
        if (ahora && !dentro) Activar();
        dentro = ahora;
    }
}

// ---------------------------------------------------------------------------------------------- almacenes con puerta

// Ultracongelador o nevera de la mesa CALIDAD Y FRÍO. La puerta se abre sola al acercar el vial en la mano; al
// soltarlo en la boca, el vial ENTRA (su anclaje se desliza hacia dentro) y la puerta se cierra (se ve por la ventana).
// Si no era el sitio correcto, Expulsar() abre la puerta y saca el vial antes de devolverlo.
public class AlmacenFrio : MonoBehaviour
{
    public Transform puerta;            // bisagra (gira en Y)
    public Transform ancla;             // anclaje de la zona: la boca al recibir, el fondo al terminar de entrar
    public ZonaEncaje zona;
    public float anguloAbierta = -105f;
    public Vector3 dentro = new Vector3(0f, 0f, -0.075f);
    public float radioApertura = 0.35f;

    float angulo, tDentro = -1f, kSalida = 1f, abiertaHasta;
    bool expulsando, abiertaAntes;

    public bool Abierta => Mathf.Abs(angulo - anguloAbierta) < 2f;
    public bool Cerrada => Mathf.Abs(angulo) < 0.5f;

    // Abre la puerta un momento (p. ej. mientras la guía enseña dónde guardar).
    public void Entreabrir(float segundos) => abiertaHasta = Mathf.Max(abiertaHasta, Time.time + segundos);

    void Update()
    {
        bool ocupado = zona != null && zona.Actual != null;
        tDentro = ocupado ? (tDentro < 0f ? 0f : tDentro + Time.deltaTime) : -1f;

        bool cerca = false;
        if (!ocupado && zona != null)
            foreach (var e in Encajable.Todos)
                if (e != null && e.tipo == zona.tipo && e.Agarrado && (e.transform.position - zona.transform.position).sqrMagnitude < radioApertura * radioApertura) { cerca = true; break; }

        // Entrada: espera a que la puerta esté abierta, desliza el vial al fondo y después cierra.
        float k = expulsando ? kSalida : ocupado ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tDentro - 0.1f) / 0.55f)) : 0f;
        if (ancla != null) ancla.localPosition = Vector3.Lerp(Vector3.zero, dentro, k);

        bool abrir = expulsando || cerca || (ocupado && tDentro < 0.8f) || Time.time < abiertaHasta;
        angulo = Mathf.MoveTowards(angulo, abrir ? anguloAbierta : 0f, 300f * Time.deltaTime);
        if (puerta != null) puerta.localRotation = Quaternion.Euler(0f, angulo, 0f);
        if (abrir != abiertaAntes)
        {
            abiertaAntes = abrir;
            if (puerta != null) Sonidos.Puerta(puerta.position, abrir);
        }
    }

    // Abre, saca el vial a la boca y llama a 'alSalir' (que lo devuelve a su sitio).
    public IEnumerator Expulsar(System.Action alSalir)
    {
        expulsando = true;
        kSalida = 1f;
        float t0 = Time.time;
        while (!Abierta && Time.time - t0 < 1f) yield return null;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
        {
            kSalida = 1f - Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }
        kSalida = 0f;
        alSalir?.Invoke();
        expulsando = false;
    }
}
