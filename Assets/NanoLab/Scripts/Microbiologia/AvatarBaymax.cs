using UnityEngine;

// Avatar del jugador: Baby Baymax ("Baby Baymax" de hlaphoneshwe, CC BY 4.0).
// El modelo no tiene esqueleto: son piezas rígidas. El cuerpo se coloca bajo la cabeza del
// jugador y gira con ella; la cabeza sigue al visor; cada mano va a su mando y el brazo se
// estira del hombro a la muñeca (como un robot inflable).
// Igual que Meta Avatars, en primera persona solo se ven las manos (el cuerpo, los brazos y la
// cabeza no tapan la mesa ni lo que se manipula); los espejos sí ven a Baymax completo.
[DefaultExecutionOrder(100)]
public class AvatarBaymax : MonoBehaviour
{
    // Capa que la cámara del jugador no renderiza y los espejos sí.
    public const int CapaCabeza = 29;

    [Header("Piezas (las rellena ConvertirBaymax)")]
    public Transform cuello;
    public Transform hombroIzq, hombroDer;
    public Transform manoIzq, manoDer;
    public Renderer[] cabeza;
    [Tooltip("Posición de los ojos respecto a la raíz del avatar, a escala 1.")]
    public Vector3 ojos = new Vector3(0f, 1.45f, 0.08f);
    [Tooltip("Distancia del hombro a la muñeca, a escala 1.")]
    public float largoBrazo = 0.5f;
    [Tooltip("Largo de la mano (muñeca a la punta), a escala 1.")]
    public float largoMano = 0.14f;

    [Header("Proporciones")]
    [Tooltip("Proporción del cuerpo (ancho, alto, fondo). Los ojos siempre quedan a la altura real del jugador.")]
    public Vector3 proporcion = new Vector3(0.9f, 1.2f, 0.9f);

    [Header("Comportamiento")]
    [Tooltip("Ver también la barriga y los brazos en primera persona (por defecto, solo las manos, como Meta Avatars).")]
    public bool cuerpoEnPrimeraPersona = false;
    public bool ocultarManosMeta = true;
    [Tooltip("Retroceso de la mano respecto al mando (m) para no tapar el punto de agarre.")]
    public float retrocesoMano = 0.03f;
    [Tooltip("Grados que puede girar la cabeza antes de que el cuerpo la siga.")]
    public float giroLibre = 35f;
    public float escalaMin = 0.5f, escalaMax = 1.3f;

    OVRCameraRig rig;
    float yawCuerpo;
    bool iniciado;
    float alturaRef;
    float escala = 1f;
    Vector3 cuelloReposo, hombroIzqReposo, hombroDerReposo, manoIzqReposo, manoDerReposo;
    Transform[] estaticas;

    void Awake()
    {
        if (cuello) cuelloReposo = cuello.localPosition;
        if (hombroIzq) hombroIzqReposo = hombroIzq.localPosition;
        if (hombroDer) hombroDerReposo = hombroDer.localPosition;
        if (manoIzq) manoIzqReposo = manoIzq.localPosition;
        if (manoDer) manoDerReposo = manoDer.localPosition;
        // Cuerpo y piernas: hijos directos con pivote en el suelo.
        var lista = new System.Collections.Generic.List<Transform>();
        foreach (Transform t in transform)
            if (t != cuello && t != hombroIzq && t != hombroDer && t != manoIzq && t != manoDer) lista.Add(t);
        estaticas = lista.ToArray();
    }

    void Start()
    {
        rig = GetComponentInParent<OVRCameraRig>();
        if (rig == null) rig = FindObjectOfType<OVRCameraRig>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bool esMano = (manoIzq && r.transform.IsChildOf(manoIzq)) || (manoDer && r.transform.IsChildOf(manoDer));
            bool esCabeza = cuello && r.transform.IsChildOf(cuello);
            r.gameObject.layer = esMano || (cuerpoEnPrimeraPersona && !esCabeza) ? 0 : CapaCabeza;
        }
        if (rig != null)
            foreach (var c in rig.GetComponentsInChildren<Camera>(true)) c.cullingMask &= ~(1 << CapaCabeza);
        OcultarManosMeta();
        Invoke(nameof(OcultarManosMeta), 1f);   // por si el SDK activa modelos de mando al conectarse
    }

    // Los mandos y las manos sintéticas de Meta se ocultan: las manos son las de Baymax.
    // (Los rayos y cursores de interacción se mantienen.)
    void OcultarManosMeta()
    {
        if (!ocultarManosMeta || rig == null) return;
        var raiz = rig.transform.parent != null ? rig.transform.parent : rig.transform;
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform.IsChildOf(transform)) continue;
            if (EsVisualDeMano(r.transform)) r.forceRenderingOff = true;
        }
    }

    static bool EsVisualDeMano(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            var n = p.name;
            if (n.Contains("Interactor")) return false;   // rayos, cursores, flechas de giro
            if (n == "OVRControllerVisual" || n == "OVRLeftHandVisual" || n == "OVRRightHandVisual" || n.StartsWith("HandVisuals")) return true;
        }
        return false;
    }

    void LateUpdate()
    {
        if (rig == null || rig.centerEyeAnchor == null) return;
        var cab = rig.centerEyeAnchor;
        float suelo = rig.trackingSpace != null ? rig.trackingSpace.position.y : rig.transform.position.y;
        Posar(cab.position, cab.rotation, Mano(rig.leftHandAnchor, OVRInput.Controller.LTouch, OVRInput.Controller.LHand),
              Mano(rig.rightHandAnchor, OVRInput.Controller.RTouch, OVRInput.Controller.RHand), suelo);
    }

    // Pose de la mano si el mando (o la mano rastreada) está siendo seguido.
    static Pose? Mano(Transform ancla, OVRInput.Controller mando, OVRInput.Controller mano)
    {
        if (ancla == null) return null;
        bool seguida = OVRInput.GetControllerPositionTracked(mando) || OVRInput.GetControllerPositionTracked(mano);
        return seguida ? new Pose(ancla.position, ancla.rotation) : (Pose?)null;
    }

    // Coloca el avatar a partir de la cabeza y las manos (también lo usan las pruebas sin visor).
    public void Posar(Vector3 cabezaPos, Quaternion cabezaRot, Pose? manoIzqPose, Pose? manoDerPose, float suelo)
    {
        float alturaOjos = cabezaPos.y - suelo;
        if (alturaOjos < 0.5f) return;   // sin seguimiento todavía
        if (estaticas == null) Awake();

        // Escala uniforme según la altura del jugador de pie (la mayor vista); la proporción se aplica
        // por pieza para que las piezas que giran no se deformen.
        if (!iniciado || alturaOjos > alturaRef) alturaRef = alturaOjos;
        float objetivo = Mathf.Clamp(alturaRef / (ojos.y * proporcion.y), escalaMin, escalaMax);
        escala = iniciado ? Mathf.MoveTowards(escala, objetivo, Time.deltaTime * 0.5f) : objetivo;
        transform.localScale = Vector3.one * escala;
        foreach (var t in estaticas) t.localScale = proporcion;
        if (cuello) { cuello.localPosition = Vector3.Scale(cuelloReposo, proporcion); cuello.localScale = proporcion; }
        if (hombroIzq) hombroIzq.localPosition = Vector3.Scale(hombroIzqReposo, proporcion);
        if (hombroDer) hombroDer.localPosition = Vector3.Scale(hombroDerReposo, proporcion);

        // El cuerpo sigue el giro de la cabeza cuando esta se pasa de 'giroLibre'.
        var adelante = Vector3.ProjectOnPlane(cabezaRot * Vector3.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f) adelante = Vector3.ProjectOnPlane(cabezaRot * Vector3.up, Vector3.up);
        float yawCabeza = Quaternion.LookRotation(adelante).eulerAngles.y;
        if (!iniciado) yawCuerpo = yawCabeza;
        float dif = Mathf.DeltaAngle(yawCuerpo, yawCabeza);
        if (Mathf.Abs(dif) > giroLibre) yawCuerpo += dif - Mathf.Sign(dif) * giroLibre;
        yawCuerpo = Mathf.MoveTowardsAngle(yawCuerpo, yawCabeza, Time.deltaTime * 40f);   // se recoloca despacio
        var rot = Quaternion.Euler(0f, yawCuerpo, 0f);

        // Raíz bajo los ojos; si el jugador se agacha, el cuerpo baja con él.
        var ojosMundo = rot * (Vector3.Scale(ojos, proporcion) * escala);
        float y = Mathf.Min(suelo, cabezaPos.y - ojosMundo.y);
        transform.SetPositionAndRotation(new Vector3(cabezaPos.x - ojosMundo.x, y, cabezaPos.z - ojosMundo.z), rot);

        if (cuello != null) cuello.rotation = cabezaRot;
        Brazo(hombroIzq, manoIzq, manoIzqReposo, manoIzqPose, rot);
        Brazo(hombroDer, manoDer, manoDerReposo, manoDerPose, rot);
        iniciado = true;
    }

    void Brazo(Transform hombro, Transform mano, Vector3 manoReposo, Pose? pose, Quaternion cuerpo)
    {
        if (hombro == null) return;
        float s = escala * proporcion.y;
        if (pose == null)
        {
            // Sin mando: brazo en reposo, colgando, con la mano en su sitio.
            hombro.localRotation = Quaternion.Slerp(hombro.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            hombro.localScale = Vector3.Lerp(hombro.localScale, proporcion, Time.deltaTime * 6f);
            if (mano != null)
            {
                mano.localPosition = Vector3.Scale(manoReposo, proporcion);
                mano.localRotation = Quaternion.identity;
                mano.localScale = proporcion;
            }
            return;
        }
        // La mano va al mando (un poco retrasada para no tapar lo que se agarra); la muñeca queda
        // por encima de la mano, en la línea que viene del hombro.
        var ancla = pose.Value.position - pose.Value.forward * retrocesoMano;
        var dir = ancla - hombro.position;
        if (dir.sqrMagnitude < 1e-6f) return;
        var dirN = dir.normalized;
        var frente = Vector3.ProjectOnPlane(cuerpo * Vector3.forward, dirN);
        if (frente.sqrMagnitude < 1e-4f) frente = Vector3.ProjectOnPlane(cuerpo * Vector3.up, dirN);
        var orient = Quaternion.LookRotation(frente, -dirN);   // -Y local apunta del hombro a la mano
        var muneca = ancla - dirN * (largoMano * s * 0.5f);

        var aMuneca = muneca - hombro.position;
        hombro.rotation = orient;
        float estirar = Mathf.Clamp(aMuneca.magnitude / (largoBrazo * s), 0.3f, 1.8f);
        hombro.localScale = new Vector3(proporcion.x, proporcion.y * estirar, proporcion.z);
        if (mano != null)
        {
            mano.position = muneca;
            mano.rotation = orient;
            mano.localScale = proporcion;
        }
    }
}
