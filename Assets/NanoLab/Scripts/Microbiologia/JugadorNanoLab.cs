using UnityEngine;

// Va en la raíz del OVRCameraRigInteraction. Al arrancar:
//  - fuerza el seguimiento a nivel de suelo (la cabeza queda a la altura real del usuario),
//  - desactiva la locomoción por teletransporte del rig de Meta,
// y en cada frame permite caminar con el joystick izquierdo (hacia donde se mira, con
// colisiones contra paredes y muebles) y girar a saltos con el joystick derecho.
// El agarre sigue siendo el del Meta Interaction SDK.
public class JugadorNanoLab : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 1.4f;
    public float radioCuerpo = 0.2f;
    [Header("Giro")]
    public float anguloGiro = 45f;

    static readonly string[] LocomocionDelRig = { "LocomotionControllerInteractorGroup", "LocomotionHandInteractorGroup" };

    OVRCameraRig rig;
    bool girando;

    void Awake()
    {
        rig = GetComponentInChildren<OVRCameraRig>(true);
        ForzarSuelo();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            foreach (var n in LocomocionDelRig)
                if (t.name.StartsWith(n)) t.gameObject.SetActive(false);
    }

    // Se repite en Start y medio segundo después por si el OVRManager aplica su valor guardado al iniciar.
    void Start()
    {
        ForzarSuelo();
        Invoke(nameof(ForzarSuelo), 0.5f);
    }

    void ForzarSuelo()
    {
        var mgr = GetComponentInChildren<OVRManager>(true);
        if (mgr == null) mgr = FindObjectOfType<OVRManager>();
        if (mgr != null && mgr.trackingOriginType != OVRManager.TrackingOrigin.FloorLevel)
            mgr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
    }

    void Update()
    {
        if (rig == null || rig.centerEyeAnchor == null) return;
        var cabeza = rig.centerEyeAnchor;

        // Caminar: joystick izquierdo, relativo a la dirección de la mirada (solo en horizontal).
        var eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (eje.sqrMagnitude > 0.04f)
        {
            var adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
            var derecha = Vector3.Cross(Vector3.up, adelante);
            var mov = (adelante * eje.y + derecha * eje.x) * velocidad * Time.deltaTime;
            transform.position += Limitar(cabeza.position, mov);
        }

        // Girar a saltos: joystick derecho, alrededor de la cabeza.
        float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
        if (!girando && Mathf.Abs(x) > 0.7f)
        {
            girando = true;
            transform.RotateAround(cabeza.position, Vector3.up, Mathf.Sign(x) * anguloGiro);
        }
        else if (Mathf.Abs(x) < 0.3f) girando = false;
    }

    // Evita atravesar paredes y muebles: prueba el movimiento completo y, si choca,
    // desliza a lo largo de la superficie.
    Vector3 Limitar(Vector3 cabeza, Vector3 mov)
    {
        float suelo = transform.position.y;
        var abajo = new Vector3(cabeza.x, suelo + 0.35f, cabeza.z);
        var arriba = new Vector3(cabeza.x, Mathf.Max(suelo + 0.4f, cabeza.y - 0.1f), cabeza.z);
        float dist = mov.magnitude;
        if (dist < 1e-5f) return Vector3.zero;
        if (!Physics.CapsuleCast(abajo, arriba, radioCuerpo, mov / dist, out var hit, dist + 0.02f, ~0, QueryTriggerInteraction.Ignore)
            || (hit.rigidbody != null && !hit.rigidbody.isKinematic))
            return mov;
        var deslizar = Vector3.ProjectOnPlane(mov, hit.normal);
        deslizar.y = 0f;
        float d2 = deslizar.magnitude;
        if (d2 < 1e-5f) return Vector3.zero;
        return Physics.CapsuleCast(abajo, arriba, radioCuerpo, deslizar / d2, d2 + 0.02f, ~0, QueryTriggerInteraction.Ignore) ? Vector3.zero : deslizar;
    }
}
