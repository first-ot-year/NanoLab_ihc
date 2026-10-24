using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using UnityEngine;

// Va en la raíz del OVRCameraRigInteraction. Al arrancar:
//  - fuerza el seguimiento a nivel de suelo (la cabeza queda a la altura real del usuario),
//  - configura la locomoción de Meta (Locomotor del Interaction SDK, instalado por ConfigurarMetaISDK):
//    joystick izquierdo = deslizamiento a velocidad constante, colisiones de cápsula y viñeta de
//    confort; joystick derecho = giro por pasos de Meta; sin teletransporte,
//  - agarre con el rayo a corta distancia: el agarre a distancia de Meta responde también al
//    gatillo y su alcance se limita (el objeto viene a la mano); con un portaobjetos en la mano,
//    el rayo toma muestras y lo deja en platinas o mesas (PunteroMuestras).
// Si no encuentra el Locomotor, usa un movimiento propio de respaldo.
public class JugadorNanoLab : MonoBehaviour
{
    [Header("Caminar (Locomotor de Meta)")]
    [Tooltip("Velocidad de paseo con el joystick a fondo (m/s).")]
    public float velocidad = 1.5f;
    [Tooltip("Tiempo para arrancar o frenar (s). Bajo = velocidad constante, sin aceleración artificial (RNF-01).")]
    public float suavizado = 0.05f;
    [Tooltip("Zona muerta del joystick (0..1).")]
    [Range(0f, 0.5f)] public float zonaMuerta = 0.15f;

    [Header("Agarre con el rayo")]
    [Tooltip("Alcance máximo (m) del agarre a distancia. Corto, para no perder realismo.")]
    [Range(0.3f, 3f)] public float alcanceRayo = 1.2f;

    [Header("Giro")]
    public float anguloGiro = 30f;

    [Header("Respaldo (solo si falta el Locomotor de Meta)")]
    public float radioCuerpo = 0.16f;

    static readonly string[] Teletransporte = { "TeleportControllerInteractor", "ControllerStepInteractor", "ControllerLocomotionSlideActions" };

    OVRCameraRig rig;
    FirstPersonLocomotor locomotor;
    SlideLocomotionBroadcaster[] slides = new SlideLocomotionBroadcaster[0];
    TurnerEventBroadcaster[] turners = new TurnerEventBroadcaster[0];
    bool respaldo, girando, pausado;

    void Awake()
    {
        rig = GetComponentInChildren<OVRCameraRig>(true);
        ForzarSuelo();
        locomotor = GetComponentInChildren<FirstPersonLocomotor>(true);
        respaldo = locomotor == null;
        if (respaldo) Debug.LogWarning("[Jugador] No hay FirstPersonLocomotor en el rig: se usa el movimiento de respaldo. Ejecuta 'NanoLab/Configurar interacción Meta'.");
        var puntero = GetComponent<PunteroMuestras>();
        if (puntero == null) puntero = gameObject.AddComponent<PunteroMuestras>();
        puntero.alcance = alcanceRayo;
        ConfigurarGrupos();
        ConfigurarLocomotor();
        ConfigurarAgarre();
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

    // Mando izquierdo: solo deslizamiento. Mando derecho: solo giro. Sin teletransporte ni pasos.
    // Las manos rastreadas no tienen locomoción (gestos de teletransporte desactivados).
    void ConfigurarGrupos()
    {
        var curva = CurvaJoystick(zonaMuerta);
        var listaSlides = new System.Collections.Generic.List<SlideLocomotionBroadcaster>();
        var listaTurners = new System.Collections.Generic.List<TurnerEventBroadcaster>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("LocomotionHandInteractorGroup")) { t.gameObject.SetActive(false); continue; }
            if (!t.name.StartsWith("LocomotionControllerInteractorGroup")) continue;
            bool izquierdo = EsIzquierdo(t);
            t.gameObject.SetActive(!respaldo);
            foreach (Transform hijo in t)
            {
                if (System.Array.IndexOf(Teletransporte, hijo.name) >= 0) hijo.gameObject.SetActive(false);
                else if (hijo.name == "ControllerSlideInteractor") hijo.gameObject.SetActive(izquierdo);
                else if (hijo.name == "ControllerTurnerInteractor") hijo.gameObject.SetActive(!izquierdo);
            }
            foreach (var s in t.GetComponentsInChildren<SlideLocomotionBroadcaster>(true))
            {
                s.VerticalDeadZone = curva;
                s.HorizontalDeadZone = curva;
                if (izquierdo) listaSlides.Add(s);
            }
            foreach (var tu in t.GetComponentsInChildren<TurnerEventBroadcaster>(true))
            {
                tu.TurnMethod = TurnerEventBroadcaster.TurnMode.Snap;
                tu.SnapTurnDegrees = anguloGiro;
                if (!izquierdo) listaTurners.Add(tu);
            }
        }
        slides = listaSlides.ToArray();
        turners = listaTurners.ToArray();
    }

    // Agarre a distancia de Meta: también con el gatillo ("click") y con alcance corto.
    void ConfigurarAgarre()
    {
        int selectores = 0, conos = 0;
        foreach (var cs in GetComponentsInChildren<ControllerSelector>(true))
        {
            if ((cs.ControllerButtonUsage & ControllerButtonUsage.GripButton) == 0) continue;   // solo los de agarrar
            cs.ControllerButtonUsage |= ControllerButtonUsage.TriggerButton;
            cs.RequireButtonUsages = ControllerSelector.ControllerSelectorLogicOperator.Any;
            selectores++;
        }
        foreach (var cf in GetComponentsInChildren<ConicalFrustum>(true))
        {
            if (cf.MaxLength <= alcanceRayo) continue;
            cf.MaxLength = alcanceRayo;
            conos++;
        }
        Debug.Log($"[Jugador] Agarre: {selectores} selector(es) con gatillo o grip; alcance del rayo {alcanceRayo} m ({conos} cono(s)).");
    }

    static bool EsIzquierdo(Transform t)
    {
        for (var p = t; p != null; p = p.parent) if (p.name.StartsWith("Left")) return true;
        return false;
    }

    // Respuesta progresiva: nada dentro de la zona muerta y luego crece suave (empujar poco = andar despacio).
    static AnimationCurve CurvaJoystick(float zona)
    {
        var c = new AnimationCurve(
            new Keyframe(-1f, -1f), new Keyframe(-0.6f, -0.5f), new Keyframe(-zona, 0f),
            new Keyframe(zona, 0f), new Keyframe(0.6f, 0.5f), new Keyframe(1f, 1f));
        for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
        return c;
    }

    // Velocidad estable = aceleración · factor / amortiguación; la amortiguación fija el tiempo de arranque/frenado.
    void ConfigurarLocomotor()
    {
        if (locomotor == null) return;
        float amortiguacion = 1f / Mathf.Max(0.02f, suavizado);
        locomotor.Acceleration = 5f;
        locomotor.GroundDamping = amortiguacion;
        locomotor.SpeedFactor = velocidad * amortiguacion / locomotor.Acceleration;
    }

    // Mientras se mira por el microscopio el joystick cambia pestañas: se pausa caminar y girar.
    public void PausarLocomocion(bool pausar)
    {
        pausado = pausar;
        foreach (var s in slides) if (s != null) s.enabled = !pausar;
        foreach (var t in turners) if (t != null) t.enabled = !pausar;
    }

    void Update()
    {
        if (!respaldo || pausado || rig == null || rig.centerEyeAnchor == null) return;
        var cabeza = rig.centerEyeAnchor;

        // Respaldo: caminar con el joystick izquierdo hacia donde se mira, con colisiones.
        var eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (eje.magnitude > zonaMuerta)
        {
            var adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
            var derecha = Vector3.Cross(Vector3.up, adelante);
            var mov = (adelante * eje.y + derecha * eje.x) * velocidad * Time.deltaTime;
            transform.position += Limitar(cabeza.position, mov);
        }

        float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
        if (!girando && Mathf.Abs(x) > 0.7f)
        {
            girando = true;
            transform.RotateAround(cabeza.position, Vector3.up, Mathf.Sign(x) * anguloGiro);
        }
        else if (Mathf.Abs(x) < 0.3f) girando = false;
    }

    // Evita atravesar paredes y muebles: prueba el movimiento completo y, si choca, desliza.
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
