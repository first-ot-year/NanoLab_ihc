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
//    el rayo toma muestras y lo deja en platinas o mesas (PunteroMuestras),
//  - accesibilidad (RNF-07): panel ☰ de ajustes (MenuAjustes), modo sentado que sube la vista a la
//    de alguien de pie y sillas en las que sentarse (Silla); ambos usan el HeightOffset del Locomotor.
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

    [Header("Altura (RNF-07)")]
    [Tooltip("Altura de los ojos de una persona de pie (m): a ella se sube la vista en modo sentado.")]
    public float alturaDePie = 1.6f;

    static readonly string[] Teletransporte = { "TeleportControllerInteractor", "ControllerStepInteractor", "ControllerLocomotionSlideActions" };

    public static JugadorNanoLab Instancia { get; private set; }

    OVRCameraRig rig;
    FirstPersonLocomotor locomotor;
    SlideLocomotionBroadcaster[] slides = new SlideLocomotionBroadcaster[0];
    TurnerEventBroadcaster[] turners = new TurnerEventBroadcaster[0];
    bool respaldo, girando, pausado, esperarSoltar;
    Silla sentadoEn;

    // Desplazamiento vertical de la vista que se aplica ahora (m).
    public float OffsetAltura { get; private set; }
    // Lo que el MODO SENTADO sube la vista (no cuenta la de una silla). El avatar lo usa para crecer con los pies
    // en el suelo real, en vez de subir entero y quedarse flotando.
    public static float AlturaExtraVista => Instancia != null && Instancia.sentadoEn == null ? Instancia.OffsetAltura : 0f;

    public static bool Sentado(out Silla silla)
    {
        silla = Instancia != null ? Instancia.sentadoEn : null;
        return silla != null;
    }

    // ---------------------------------------------------------------- aparición segura (no caer al vacío)
    Vector3 spawnPies, spawnAdelante, ultimoSeguro;
    bool haySeguro;
    float tRevision, ultimoRescate = -100f;

    // ¿Hay suelo del laboratorio bajo estos pies? (un rayo hacia abajo que no cuenta los colliders del jugador)
    public static bool PosicionValida(Vector3 pies)
    {
        if (float.IsNaN(pies.x) || float.IsNaN(pies.y) || float.IsNaN(pies.z)) return false;
        var yo = Instancia != null ? Instancia.transform : null;
        var golpes = Physics.RaycastAll(pies + Vector3.up * 1.2f, Vector3.down, 2.4f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var g in golpes)
        {
            if (yo != null && g.collider.transform.IsChildOf(yo)) continue;
            if (g.point.y > pies.y - 0.6f && g.point.y < pies.y + 0.6f) return true;
        }
        return false;
    }

    // Punto de aparición de la escena (dentro del laboratorio), mirando hacia donde mira el rig.
    public Vector3 PuntoAparicion(out Vector3 adelante) { adelante = spawnAdelante; return spawnPies; }

    // Si el jugador cae (por debajo del suelo) o se queda sin suelo un rato, vuelve al último sitio seguro
    // (o al punto de aparición si vuelve a caer enseguida).
    void VigilarCaida()
    {
        if (sentadoEn != null || !Pies(out var pies, out _)) return;
        var cab = rig.centerEyeAnchor;
        float referencia = haySeguro ? ultimoSeguro.y : spawnPies.y;
        bool cayendo = cab.position.y < referencia - 2f || transform.position.y < referencia - 1.5f;
        tRevision -= Time.deltaTime;
        if (tRevision <= 0f && !cayendo)
        {
            tRevision = 0.5f;
            if (PosicionValida(pies)) { ultimoSeguro = pies; haySeguro = true; }
        }
        // Solo una caída real (por debajo del suelo): un trozo de suelo sin collider no debe teletransportar.
        if (!cayendo) return;
        bool repetido = Time.time - ultimoRescate < 10f;
        ultimoRescate = Time.time;
        var destino = haySeguro && !repetido ? ultimoSeguro : spawnPies;
        Debug.LogWarning($"[Jugador] Sin suelo en {pies}: de vuelta a {destino}.");
        // Se conserva hacia dónde se mira (antes, al volver a la entrada, quedaba de cara al espejo).
        Teletransportar(destino, Vector3.ProjectOnPlane(cab.forward, Vector3.up));
        tRevision = 1f;
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
        Aviso.Mostrar("Has vuelto al laboratorio.", destino + Vector3.up * 1.4f + f * 0.9f, Aviso.Tipo.Info, 3f);
    }

    void Awake()
    {
        Instancia = this;
        rig = GetComponentInChildren<OVRCameraRig>(true);
        spawnPies = transform.position;
        spawnAdelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (spawnAdelante.sqrMagnitude < 1e-4f) spawnAdelante = Vector3.forward;
        ForzarSuelo();
        locomotor = GetComponentInChildren<FirstPersonLocomotor>(true);
        respaldo = locomotor == null;
        if (respaldo) Debug.LogWarning("[Jugador] No hay FirstPersonLocomotor en el rig: se usa el movimiento de respaldo. Ejecuta 'NanoLab/Configurar interacción Meta'.");
        var puntero = GetComponent<PunteroMuestras>();
        if (puntero == null) puntero = gameObject.AddComponent<PunteroMuestras>();
        puntero.alcance = alcanceRayo;
        if (GetComponent<ProteccionCabeza>() == null) gameObject.AddComponent<ProteccionCabeza>();   // no ver dentro de los muebles
        if (GetComponent<GestorPartida>() == null) gameObject.AddComponent<GestorPartida>();         // guardar y continuar la partida
        if (GetComponent<AdaptadorEscena>() == null) gameObject.AddComponent<AdaptadorEscena>();     // sin química, puerta del centro, tableros de cristal
        if (GetComponent<AvisoInicio>() == null) gameObject.AddComponent<AvisoInicio>();             // aviso cuadrado + tutorial corto (X para continuar)
        ConfigurarGrupos();
        ConfigurarLocomotor();
        ConfigurarAgarre();
    }

    void OnEnable() => AjustesNanoLab.Cambiado += AplicarAltura;
    void OnDisable() => AjustesNanoLab.Cambiado -= AplicarAltura;

    // Se repite en Start y medio segundo después por si el OVRManager aplica su valor guardado al iniciar.
    void Start()
    {
        ForzarSuelo();
        Invoke(nameof(ForzarSuelo), 0.5f);
        AplicarAltura();
        // La cabeza conserva su desplazamiento dentro del área de juego (en el simulador, lo que se movió con
        // el teclado): al recargar la escena aparecía lejos del punto de aparición. Se coloca la cabeza en él
        // en cuanto el seguimiento está listo (si hay partida guardada, GestorPartida la lleva después a su sitio).
        Invoke(nameof(IrAlPuntoDeAparicion), 0.25f);
    }

    void IrAlPuntoDeAparicion()
    {
        if (sentadoEn != null) return;
        Teletransportar(spawnPies, spawnAdelante);
        ultimoSeguro = spawnPies; haySeguro = true;
    }

    // ---------------------------------------------------------------- altura, modo sentado y sillas

    // Altura real de los ojos sobre el suelo real (no cambia con el offset). Sin visor vale ~0.
    float AlturaCabeza => rig != null && rig.centerEyeAnchor != null && rig.trackingSpace != null
        ? rig.centerEyeAnchor.position.y - rig.trackingSpace.position.y : 0f;

    // Modo sentado: mide la cabeza ahora (el usuario sentado) y guarda cuánto hay que subirla.
    public void CalibrarSentado()
    {
        float h = AlturaCabeza;
        if (h < 0.3f) h = 1.15f;   // sin visor (pruebas): altura típica de unos ojos sentados
        AjustesNanoLab.AlturaSentado = alturaDePie - h;
    }

    public void AplicarAltura()
    {
        if (sentadoEn != null) return;   // sentado en una silla manda la silla
        float antes = OffsetAltura;
        float y = AjustesNanoLab.ModoSentado ? AjustesNanoLab.AlturaSentado : 0f;
        PonerOffset(y);
        // El Locomotor de Meta solo aplica el HeightOffset al recolocar al jugador: al subirlo la vista sube, pero al
        // bajarlo (quitar el modo sentado) la cápsula se encoge por abajo y la vista se quedaba arriba. Se vuelve a
        // poner al jugador con los pies en el suelo, en el mismo sitio, para que el cambio se note en los dos sentidos.
        if (locomotor != null && Mathf.Abs(y - antes) > 0.01f && isActiveAndEnabled)
        {
            StopCoroutine(nameof(Reasentar));
            StartCoroutine(Reasentar(antes));
        }
    }

    // Vista a la altura que toca (real, o la del modo sentado) y pies en el suelo, aunque la altura no haya cambiado.
    public void AlSuelo()
    {
        if (sentadoEn != null) return;
        float antes = OffsetAltura;
        PonerOffset(AjustesNanoLab.ModoSentado ? AjustesNanoLab.AlturaSentado : 0f);
        if (locomotor != null && isActiveAndEnabled) StartCoroutine(Reasentar(antes));
    }

    System.Collections.IEnumerator Reasentar(float offsetAnterior)
    {
        // Dos veces: al frame siguiente (el Locomotor ya tomó la nueva altura de la cápsula) y un poco después,
        // por si la cápsula aún se estaba ajustando; así los pies acaban en el suelo, sin quedarse flotando.
        for (int i = 0; i < 2; i++)
        {
            if (i == 0) yield return null; else yield return new WaitForSeconds(0.2f);
            if (sentadoEn != null || rig == null || rig.centerEyeAnchor == null) yield break;
            var cab = rig.centerEyeAnchor;
            float porDefecto = i == 0 ? transform.position.y - offsetAnterior : transform.position.y - OffsetAltura;
            Mover(SueloBajo(cab.position, porDefecto), cab.forward);
        }
    }

    // Punto del suelo bajo la cabeza (sin contar la cápsula del propio jugador ni los triggers).
    Vector3 SueloBajo(Vector3 cabeza, float alturaPorDefecto)
    {
        float mejor = float.MaxValue, y = alturaPorDefecto;
        foreach (var h in Physics.RaycastAll(cabeza, Vector3.down, 3.5f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform) || h.normal.y < 0.7f || h.distance >= mejor) continue;
            mejor = h.distance;
            y = h.point.y;
        }
        return new Vector3(cabeza.x, y, cabeza.z);
    }

    void PonerOffset(float y)
    {
        OffsetAltura = y;
        if (locomotor != null) locomotor.HeightOffset = y;
        else if (rig != null && rig.trackingSpace != null)
        {
            var p = rig.trackingSpace.localPosition;
            p.y = y;
            rig.trackingSpace.localPosition = p;
        }
    }

    // Se sienta en la silla mirando hacia donde mira ella (la mesa) con los ojos a la altura de sentado.
    public void Sentarse(Silla silla)
    {
        if (silla == null || silla == sentadoEn) return;
        if (sentadoEn != null) sentadoEn.Colisiones(true);
        sentadoEn = silla;
        silla.Colisiones(false);
        var pies = silla.PuntoAsiento;
        pies.y = silla.transform.position.y;
        Mover(pies, silla.transform.forward);
        float h = AlturaCabeza;
        if (h < 0.3f) h = alturaDePie;   // sin visor (pruebas)
        PonerOffset(silla.PuntoAsiento.y + silla.ojosSobreAsiento - pies.y - h);
        esperarSoltar = true;
        ActualizarPausa();
        Aviso.Mostrar("Sentado. Mueve el joystick izquierdo para levantarte.", pies + Vector3.up * 1.1f + silla.transform.forward * 0.5f, Aviso.Tipo.Info, 4f);
        Sonidos.Clic(pies);
    }

    public void Levantarse()
    {
        var s = sentadoEn;
        if (s == null) return;
        sentadoEn = null;
        var cabeza = rig != null ? rig.centerEyeAnchor : null;
        var adelante = cabeza != null ? cabeza.forward : s.transform.forward;
        var salida = s.PuntoSalida;
        salida.y = s.transform.position.y;
        Mover(salida, adelante);
        AplicarAltura();
        ActualizarPausa();
        StartCoroutine(ReactivarSilla(s));   // cuando la cápsula ya salió de ella
    }

    System.Collections.IEnumerator ReactivarSilla(Silla s)
    {
        yield return new WaitForSeconds(0.25f);
        if (s != null && s != sentadoEn) s.Colisiones(true);
    }

    // Partida guardada: dónde están los pies (bajo la cabeza) y hacia dónde se mira.
    public bool Pies(out Vector3 pies, out float yaw)
    {
        pies = Vector3.zero; yaw = 0f;
        var cab = rig != null ? rig.centerEyeAnchor : null;
        if (cab == null) return false;
        pies = new Vector3(cab.position.x, transform.position.y, cab.position.z);
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        yaw = f.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(f).eulerAngles.y : transform.eulerAngles.y;
        return true;
    }

    // Lleva al jugador a un punto (al continuar una partida). Si estaba sentado, se levanta antes.
    public void Teletransportar(Vector3 pies, Vector3 adelante)
    {
        if (sentadoEn != null) { sentadoEn.Colisiones(true); sentadoEn = null; AplicarAltura(); ActualizarPausa(); }
        Mover(pies, adelante);
    }

    // Lleva los pies a un punto del suelo y gira la vista hacia 'adelante'.
    void Mover(Vector3 pies, Vector3 adelante)
    {
        adelante = Vector3.ProjectOnPlane(adelante, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f) adelante = transform.forward;
        var rot = Quaternion.LookRotation(adelante.normalized, Vector3.up);
        if (locomotor != null)
        {
            locomotor.HandleLocomotionEvent(new LocomotionEvent(0, new Pose(pies, rot),
                LocomotionEvent.TranslationType.Absolute, LocomotionEvent.RotationType.Absolute));
            return;
        }
        var cab = rig != null ? rig.centerEyeAnchor : transform;
        float giro = Vector3.SignedAngle(Vector3.ProjectOnPlane(cab.forward, Vector3.up), adelante, Vector3.up);
        transform.RotateAround(cab.position, Vector3.up, giro);
        var d = pies - cab.position;
        d.y = pies.y - transform.position.y;
        transform.position += d;
    }

    // Sentado: se levanta al mover el joystick izquierdo (después de soltarlo una vez y fuera del microscopio).
    void ComprobarLevantarse()
    {
        if (sentadoEn == null) return;
        if (MicroscopioOptico.AlgunoActivo) { esperarSoltar = true; return; }   // ahí el joystick mueve la muestra
        float eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch).magnitude;
        if (esperarSoltar) { if (eje < 0.3f) esperarSoltar = false; return; }
        if (eje > 0.6f) Levantarse();
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
                // El giro del SDK va en el mismo grupo que el agarre de esa mano: con algo agarrado se apagaba y no
                // se podía girar (había que andar hacia atrás). Se gira con el giro propio de Update.
                else if (hijo.name == "ControllerTurnerInteractor") hijo.gameObject.SetActive(false);
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
    // Sentado en una silla también se pausa (se levanta con el joystick izquierdo).
    public void PausarLocomocion(bool pausar)
    {
        pausado = pausar;
        ActualizarPausa();
    }

    public bool LocomocionPausada => pausado || sentadoEn != null;

    void ActualizarPausa()
    {
        bool parar = LocomocionPausada;
        foreach (var s in slides) if (s != null) s.enabled = !parar;
        foreach (var t in turners) if (t != null) t.enabled = !parar;
    }

    void Update()
    {
        ComprobarLevantarse();
        if (rig != null && rig.centerEyeAnchor != null && Time.timeSinceLevelLoad > 1.5f) VigilarCaida();
        if (LocomocionPausada || rig == null || rig.centerEyeAnchor == null) return;
        var cabeza = rig.centerEyeAnchor;
        Girar(cabeza);
        if (!respaldo) return;

        // Respaldo: caminar con el joystick izquierdo hacia donde se mira, con colisiones.
        var eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (eje.magnitude > zonaMuerta)
        {
            var adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up).normalized;
            var derecha = Vector3.Cross(Vector3.up, adelante);
            var mov = (adelante * eje.y + derecha * eje.x) * velocidad * Time.deltaTime;
            transform.position += Limitar(cabeza.position, mov);
        }

    }

    // Joystick derecho: a los lados gira 30° y hacia atrás da media vuelta. Funciona aunque se lleve algo en la mano.
    void Girar(Transform cabeza)
    {
        var j = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        if (girando) { if (j.magnitude < 0.3f) girando = false; return; }
        float grados = Mathf.Abs(j.x) > 0.7f ? Mathf.Sign(j.x) * anguloGiro : j.y < -0.8f && Mathf.Abs(j.x) < 0.5f ? 180f : 0f;
        if (grados == 0f) return;
        girando = true;
        if (locomotor != null)
            locomotor.HandleLocomotionEvent(new LocomotionEvent(0, Quaternion.Euler(0f, grados, 0f), LocomotionEvent.RotationType.Relative));
        else transform.RotateAround(cabeza.position, Vector3.up, grados);
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
