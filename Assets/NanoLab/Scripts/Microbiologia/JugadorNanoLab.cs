using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using Oculus.Interaction.Locomotion;
using UnityEngine;

// Va en la raíz del OVRCameraRigInteraction. Al arrancar:
//  - fuerza el seguimiento a nivel de suelo (la cabeza queda a la altura real del usuario),
//  - configura la locomoción de Meta (Locomotor del Interaction SDK, instalado por ConfigurarMetaISDK).
//    Modo DESLIZAMIENTO (por defecto): joystick izquierdo = caminar a velocidad constante, con marco de
//    reposo (JaulaConfort) contra el mareo; joystick derecho = giro por pasos.
//    Modo TELETRANSPORTE (opcional): joystick adelante = arco, soltar = saltar; a los lados = girar.
//  - marcha estable: la dirección se fija al empezar a caminar (no sigue la cabeza ni gira sola;
//    solo cambia al parar o con el giro por pasos), el joystick casi recto se endereza (EjeEstable)
//    y la altura de la cámara no da saltitos (AlturaEstable),
//  - agarre con el rayo a corta distancia: el agarre a distancia de Meta responde también al
//    gatillo y su alcance se limita (el objeto viene a la mano); con un portaobjetos en la mano,
//    el rayo toma muestras y lo deja en platinas o mesas (PunteroMuestras).
// Si no encuentra el Locomotor, usa un movimiento propio de respaldo.
[DefaultExecutionOrder(-100)]   // antes que los SlideLocomotionBroadcaster, que leen la dirección
public class JugadorNanoLab : MonoBehaviour
{
    [Header("Modo de desplazamiento")]
    [Tooltip("Desactivado (por defecto): caminar con el joystick. Activado: teletransporte con arco.")]
    public bool teletransporte = false;

    [Header("Caminar (Locomotor de Meta)")]
    [Tooltip("Velocidad de paseo con el joystick a fondo (m/s). La estabilidad de la cámara permite ir a paso normal.")]
    public float velocidad = 1.5f;
    [Tooltip("Tiempo aproximado para arrancar o frenar (s). Más alto = más suave.")]
    public float suavizado = 0.05f;
    [Tooltip("Zona muerta del joystick (0..1).")]
    [Range(0f, 0.5f)] public float zonaMuerta = 0.15f;

    [Header("Confort (RNF-01)")]
    [Tooltip("Fracción de la velocidad al moverse de lado (lo que más marea).")]
    [Range(0.2f, 1f)] public float factorLateral = 0.5f;
    [Tooltip("Fracción de la velocidad al ir hacia atrás.")]
    [Range(0.2f, 1f)] public float factorAtras = 0.6f;
    [Tooltip("Marco de reposo: rejilla periférica que se mueve contigo al caminar (lo que más reduce el mareo).")]
    public bool marcoReposo = true;
    [Tooltip("Ángulo del centro de la vista que el marco de reposo deja siempre libre (grados).")]
    [Range(40f, 120f)] public float campoLibre = 70f;
    [Tooltip("Sin marco de reposo: campo de visión que deja la viñeta de Meta al caminar. 360 = sin viñeta.")]
    [Range(60f, 360f)] public float fovCaminando = 80f;

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
    GameObject[] teletransportes = new GameObject[0];
    bool respaldo, girando, pausado, caminando;
    Transform direccion;   // dirección de marcha estable (hija del OVRCameraRig: gira con los giros por pasos)
    JaulaConfort jaula;

    void Awake()
    {
        rig = GetComponentInChildren<OVRCameraRig>(true);
        ForzarSuelo();
        locomotor = GetComponentInChildren<FirstPersonLocomotor>(true);
        respaldo = locomotor == null;
        if (respaldo) Debug.LogWarning("[Jugador] No hay FirstPersonLocomotor en el rig: se usa el movimiento de respaldo. Ejecuta 'NanoLab/Configurar interacción Meta'.");
        // El locomotor mueve y gira el OVRCameraRig (su _playerOrigin): ahí van la dirección y la altura.
        var origen = rig != null ? rig.transform : transform;
        direccion = new GameObject("DireccionMarcha").transform;
        direccion.SetParent(origen, false);
        if (origen.GetComponent<AlturaEstable>() == null) origen.gameObject.AddComponent<AlturaEstable>();
        if (marcoReposo && !teletransporte && rig != null && rig.centerEyeAnchor != null)
        {
            jaula = JaulaConfort.Crear(origen, rig.centerEyeAnchor, AvatarBaymax.CapaManosMeta);
            if (jaula != null) jaula.campoLibre = campoLibre;
        }
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

    // Teletransporte: los dos mandos teletransportan (adelante) y giran (a los lados), como en Quest.
    // Deslizamiento: mando izquierdo solo desliza, mando derecho solo gira.
    // Las manos rastreadas no tienen locomoción (gestos de teletransporte desactivados).
    void ConfigurarGrupos()
    {
        var listaTp = new System.Collections.Generic.List<GameObject>();
        var curva = CurvaJoystick(zonaMuerta, 1f, factorAtras);
        var curvaLateral = CurvaJoystick(zonaMuerta, factorLateral, factorLateral);
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
                if (teletransporte && !respaldo)
                {
                    if (hijo.name == "TeleportControllerInteractor") { hijo.gameObject.SetActive(true); listaTp.Add(hijo.gameObject); }
                    else if (hijo.name == "ControllerTurnerInteractor") hijo.gameObject.SetActive(true);
                    else if (hijo.name == "ControllerSlideInteractor" || System.Array.IndexOf(Teletransporte, hijo.name) >= 0) hijo.gameObject.SetActive(false);
                }
                else if (System.Array.IndexOf(Teletransporte, hijo.name) >= 0) hijo.gameObject.SetActive(false);
                else if (hijo.name == "ControllerSlideInteractor") hijo.gameObject.SetActive(izquierdo);
                else if (hijo.name == "ControllerTurnerInteractor") hijo.gameObject.SetActive(!izquierdo);
            }
            foreach (var s in t.GetComponentsInChildren<SlideLocomotionBroadcaster>(true))
            {
                s.VerticalDeadZone = curva;
                s.HorizontalDeadZone = curvaLateral;
                s.Aiming = direccion;
                Enderezar(s);
                if (izquierdo) listaSlides.Add(s);
            }
            foreach (var tu in t.GetComponentsInChildren<TurnerEventBroadcaster>(true))
            {
                tu.TurnMethod = TurnerEventBroadcaster.TurnMode.Snap;
                tu.SnapTurnDegrees = anguloGiro;
                if (!izquierdo || teletransporte) listaTurners.Add(tu);
            }
        }
        slides = listaSlides.ToArray();
        turners = listaTurners.ToArray();
        teletransportes = listaTp.ToArray();
        Debug.Log($"[Jugador] Modo {(teletransporte && !respaldo ? "teletransporte" : "deslizamiento")}: {teletransportes.Length} teletransporte(s), {turners.Length} giro(s).");
    }

    // Pone EjeEstable entre el joystick y el broadcaster (una sola vez).
    static void Enderezar(SlideLocomotionBroadcaster s)
    {
        var campo = typeof(SlideLocomotionBroadcaster).GetField("_axis2D", BindingFlags.NonPublic | BindingFlags.Instance);
        if (!(campo?.GetValue(s) is IAxis2D original) || original is EjeEstable) return;
        var eje = s.gameObject.AddComponent<EjeEstable>();
        eje.original = original;
        s.InjectAxis2D(eje);
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

    // Dirección de marcha: en reposo sigue a la cabeza; al caminar queda fija (mirar a los lados no
    // tuerce el camino ni gira la vista). Cambia al parar y volver a empujar, o con el giro por pasos.
    void ActualizarDireccion()
    {
        var cab = rig != null ? rig.centerEyeAnchor : null;
        if (cab == null || direccion == null) return;
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) return;
        var haciaCabeza = Quaternion.LookRotation(f.normalized);
        var eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        bool mueve = !pausado && eje.magnitude > zonaMuerta;
        if (!mueve || !caminando) direccion.rotation = haciaCabeza;
        caminando = mueve;
    }

    static bool EsIzquierdo(Transform t)
    {
        for (var p = t; p != null; p = p.parent) if (p.name.StartsWith("Left")) return true;
        return false;
    }

    // Respuesta progresiva: nada dentro de la zona muerta y luego crece suave (empujar poco = andar despacio).
    // 'maxPos'/'maxNeg' limitan la velocidad en cada sentido (p. ej. hacia atrás o de lado, más lento).
    static AnimationCurve CurvaJoystick(float zona, float maxPos, float maxNeg)
    {
        var c = new AnimationCurve(
            new Keyframe(-1f, -maxNeg), new Keyframe(-0.6f, -0.35f * maxNeg), new Keyframe(-zona, 0f),
            new Keyframe(zona, 0f), new Keyframe(0.6f, 0.35f * maxPos), new Keyframe(1f, maxPos));
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
        ConfigurarVineta();
    }

    // Viñeta de confort del SDK: al caminar se oscurece la periferia (donde el movimiento visual
    // que el oído interno no siente produce el mareo); al girar por pasos, un parpadeo breve.
    void ConfigurarVineta()
    {
        // Con marco de reposo, la viñeta negra de Meta sobra (el marco ya ocupa la periferia).
        bool vineta = !marcoReposo || teletransporte;
        foreach (var t in GetComponentsInChildren<LocomotionTunneling>(true))
        {
            if (!vineta)
            {
                var nada = AnimationCurve.Constant(0f, 1000f, 360f);
                t.MovementStrength = nada; t.AccelerationStrength = nada; t.RotationStrength = nada;
                continue;
            }
            t.MovementStrength = new AnimationCurve(new Keyframe(0f, 360f), new Keyframe(0.15f, 140f), new Keyframe(velocidad, fovCaminando));
            t.AccelerationStrength = new AnimationCurve(new Keyframe(0f, 360f), new Keyframe(3f, fovCaminando + 10f));
            t.RotationStrength = new AnimationCurve(new Keyframe(0f, 360f), new Keyframe(90f, 100f), new Keyframe(360f, 60f));
            t.FadeOutTime = 0.15f;
            t.FadeOutWait = 0.1f;
        }
        foreach (var e in GetComponentsInChildren<TunnelingEffect>(true))
        {
            e.ExtraFeatheredFOV = 30f;   // borde difuminado, sin línea dura
            e.AlphaStrength = 1f;
        }
    }

    // Mientras se mira por el microscopio el joystick cambia pestañas: se pausa caminar y girar.
    public void PausarLocomocion(bool pausar)
    {
        pausado = pausar;
        foreach (var s in slides) if (s != null) s.enabled = !pausar;
        foreach (var t in turners) if (t != null) t.enabled = !pausar;
        foreach (var tp in teletransportes) if (tp != null) tp.SetActive(!pausar);
        if (jaula != null) jaula.Pausar(pausar);
    }

    void Update()
    {
        ActualizarDireccion();
        if (!respaldo || pausado || rig == null || rig.centerEyeAnchor == null) return;
        var cabeza = rig.centerEyeAnchor;

        // Respaldo: caminar con el joystick izquierdo hacia donde se mira, con colisiones.
        var eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (eje.magnitude > zonaMuerta)
        {
            var adelante = direccion.forward;
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
