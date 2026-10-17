using UnityEngine;

// Avatar del jugador: Baby Baymax ("Baby Baymax" de hlaphoneshwe, CC BY 4.0).
// El modelo no tiene esqueleto: son piezas rígidas. El cuerpo se coloca bajo la cabeza del
// jugador y gira con ella; la cabeza sigue al visor; cada brazo sale del hombro hacia su mando
// y se estira o encoge (como un robot inflable) para que la mano llegue al mando.
// Para la cámara del jugador se oculta la cabeza (vería su interior); los espejos sí la ven.
[DefaultExecutionOrder(100)]
public class AvatarBaymax : MonoBehaviour
{
    public const int CapaCabeza = 29;

    [Header("Piezas (las rellena ConvertirBaymax)")]
    public Transform cuello;
    public Transform hombroIzq, hombroDer;
    public Renderer[] cabeza;
    [Tooltip("Posición de los ojos respecto a la raíz del avatar, a escala 1.")]
    public Vector3 ojos = new Vector3(0f, 1.45f, 0.08f);
    [Tooltip("Distancia del hombro a la punta de la mano, a escala 1.")]
    public float largoBrazo = 0.7f;

    [Header("Comportamiento")]
    public bool ocultarManosMeta = true;
    [Tooltip("Grados que puede girar la cabeza antes de que el cuerpo la siga.")]
    public float giroLibre = 35f;
    public float escalaMin = 0.6f, escalaMax = 1.35f;

    OVRCameraRig rig;
    float yawCuerpo;
    bool iniciado;
    float alturaRef;
    float escala = 1f;

    void Start()
    {
        rig = GetComponentInParent<OVRCameraRig>();
        if (rig == null) rig = FindObjectOfType<OVRCameraRig>();
        foreach (var r in cabeza) if (r != null) r.gameObject.layer = CapaCabeza;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

    // Posición de la mano si el mando (o la mano rastreada) está siendo seguido.
    static Vector3? Mano(Transform ancla, OVRInput.Controller mando, OVRInput.Controller mano)
    {
        if (ancla == null) return null;
        bool seguida = OVRInput.GetControllerPositionTracked(mando) || OVRInput.GetControllerPositionTracked(mano);
        return seguida ? ancla.position : (Vector3?)null;
    }

    // Coloca el avatar a partir de la cabeza y las manos (también lo usan las pruebas sin visor).
    public void Posar(Vector3 cabezaPos, Quaternion cabezaRot, Vector3? manoIzq, Vector3? manoDer, float suelo)
    {
        float alturaOjos = cabezaPos.y - suelo;
        if (alturaOjos < 0.5f) return;   // sin seguimiento todavía

        // Escala según la altura del jugador de pie (se toma la mayor altura vista).
        if (!iniciado || alturaOjos > alturaRef) alturaRef = alturaOjos;
        float objetivo = Mathf.Clamp(alturaRef / ojos.y, escalaMin, escalaMax);
        escala = iniciado ? Mathf.MoveTowards(escala, objetivo, Time.deltaTime * 0.5f) : objetivo;
        transform.localScale = Vector3.one * escala;

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
        var ojosMundo = rot * (ojos * escala);
        float y = Mathf.Min(suelo, cabezaPos.y - ojosMundo.y);
        transform.SetPositionAndRotation(new Vector3(cabezaPos.x - ojosMundo.x, y, cabezaPos.z - ojosMundo.z), rot);

        if (cuello != null) cuello.rotation = cabezaRot;
        Brazo(hombroIzq, manoIzq, rot);
        Brazo(hombroDer, manoDer, rot);
        iniciado = true;
    }

    void Brazo(Transform hombro, Vector3? mano, Quaternion cuerpo)
    {
        if (hombro == null) return;
        if (mano == null)
        {
            // Sin mando: brazo en reposo, colgando.
            hombro.localRotation = Quaternion.Slerp(hombro.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            hombro.localScale = Vector3.Lerp(hombro.localScale, Vector3.one, Time.deltaTime * 6f);
            return;
        }
        var dir = mano.Value - hombro.position;
        float largo = dir.magnitude;
        if (largo < 1e-3f) return;
        // El eje -Y local del brazo apunta a la mano; el frente del brazo, hacia delante del cuerpo.
        var frente = Vector3.ProjectOnPlane(cuerpo * Vector3.forward, dir);
        if (frente.sqrMagnitude < 1e-4f) frente = Vector3.ProjectOnPlane(cuerpo * Vector3.up, dir);
        hombro.rotation = Quaternion.LookRotation(frente, -dir);
        float estirar = Mathf.Clamp(largo / (largoBrazo * escala), 0.45f, 1.7f);
        hombro.localScale = new Vector3(1f, estirar, 1f);
    }
}
