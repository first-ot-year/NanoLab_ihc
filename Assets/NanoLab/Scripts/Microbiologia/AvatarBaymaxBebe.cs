using UnityEngine;

// Avatar del jugador: Baymax Bebé v4.2 (modelo de Blender de un compañero, malla con esqueleto de 36
// huesos). Lo prepara ConvertirBaymaxBebe: la malla se parte en "Manos" (antebrazos y manos, capa 0, en primera
// persona) y "Cuerpo"/"Cara" (capa 29, solo los espejos), y el Animator reproduce Idle o Walk.
// Encima, cada frame: escala uniforme para que los ojos del modelo coincidan con los del jugador, el
// cuerpo gira con la cabeza al pasar 'giroLibre', la cabeza sigue al visor, cada brazo va a su mando
// con IK de dos huesos (codo limitado a 65°, como pide el modelo) y, si el mando queda más lejos de lo
// que alcanzan los brazos del bebé, el brazo se estira separando los huesos (no escalándolos, que
// deformaría la mano). Los dedos se cierran con el grip y el índice con el gatillo. Sentado en una
// Silla, las piernas se doblan.
[DefaultExecutionOrder(100)]
public class AvatarBaymaxBebe : MonoBehaviour, IAvatarJugador
{
    [Header("Huesos (los rellena ConvertirBaymaxBebe)")]
    public Animator animator;
    public Transform cuello, cabeza;
    public Transform brazoIzq, antebrazoIzq, manoIzq;
    public Transform brazoDer, antebrazoDer, manoDer;
    public Transform musloIzq, musloDer;
    public Transform[] dedosIzq, dedosDer;
    [Tooltip("Rotación local de cada dedo con el puño cerrado (del clip Gestos_Manos).")]
    public Quaternion[] punoIzq, punoDer;
    [Tooltip("Posición de los ojos respecto a la raíz del avatar, a escala 1 (el modelo mira a +Z).")]
    public Vector3 ojos = new Vector3(0f, 0.94f, 0.12f);

    [Header("Brazos y manos")]
    [Tooltip("Flexión máxima del codo (grados): más allá la cara interna del codo se interpenetra.")]
    public float codoMax = 65f;
    [Tooltip("Cuánto se puede estirar el brazo para llegar al mando (x largo del brazo).")]
    public float estiramientoMax = 2.2f;
    [Tooltip("Tamaño del antebrazo y la mano respecto al modelo escalado: a 1 la mano sería el doble de una real. Se escala desde el codo; solo la mano, la piel de la muñeca se abriría.")]
    public float escalaManos = 0.6f;
    [Tooltip("Retroceso de la mano respecto al mando (m) para no tapar el punto de agarre.")]
    public float retrocesoMano = 0.02f;
    [Tooltip("Los dedos apuntan hacia delante y un poco hacia abajo respecto al mando (grados).")]
    public float inclinacionMano = 25f;
    [Tooltip("Cierre de los dedos en reposo (0 abierta, 1 puño).")]
    public float cierreReposo = 0.15f;

    [Header("Comportamiento")]
    [Tooltip("Ver también la barriga y los brazos en primera persona.")]
    public bool cuerpoEnPrimeraPersona = false;
    [Tooltip("En primera persona: activado = manos/mandos de Meta; desactivado (por defecto) = manos de Baymax.")]
    public bool manosMetaEnPrimeraPersona = false;
    [Tooltip("Grados que puede girar la cabeza antes de que el cuerpo la siga.")]
    public float giroLibre = 35f;
    public float escalaMin = 0.6f, escalaMax = 2.2f;
    [Tooltip("La cámara queda esta distancia (m) por delante de la cara de Baymax: el cuerpo se coloca detrás de la vista y nunca se ve su interior.")]
    public float separacionCara = 0.15f;

    class Brazo
    {
        public Transform hombro, codo, mano, medio;
        public Quaternion hombroReposo, codoReposo, eje;
        public Vector3 codoPos, manoPos;
        public float lado;
        public Transform[] dedos;
        public Quaternion[] abiertos, puno;
        public OVRInput.Controller mando;
    }

    OVRCameraRig rig;
    Brazo[] brazos;
    Quaternion cuelloOffset, cabezaOffset;
    float yawCuerpo, alturaRef, escala = 1f, velSuave, extraVista;
    bool iniciado;
    Renderer manosRenderer;
    bool manosOcultas;
    readonly System.Collections.Generic.List<Renderer> cuerpo = new System.Collections.Generic.List<Renderer>();
    static readonly System.Collections.Generic.List<AvatarBaymaxBebe> todos = new System.Collections.Generic.List<AvatarBaymaxBebe>();

    void OnEnable() { if (!todos.Contains(this)) todos.Add(this); }
    void OnDisable() => todos.Remove(this);

    // Lo llama el espejo justo antes y después de renderizar: el cuerpo y la cara de Baymax solo existen
    // para él. Así la vista del jugador nunca los dibuja, sea cual sea su cámara o su máscara de capas
    // (con la cámara dentro de la cabeza se veía "por el ojo de Baymax").
    public static void Espejo(bool renderizando)
    {
        foreach (var a in todos)
        {
            foreach (var r in a.cuerpo) if (r != null) r.forceRenderingOff = !renderizando;
            if (a.manosRenderer != null && a.manosOcultas) a.manosRenderer.forceRenderingOff = !renderizando;
        }
    }

    void Awake()
    {
        if (brazos != null) return;
        // Todo se mide en la pose de enlace (el Animator aún no ha escrito nada).
        brazos = new[]
        {
            NuevoBrazo(brazoIzq, antebrazoIzq, manoIzq, dedosIzq, punoIzq, OVRInput.Controller.LTouch),
            NuevoBrazo(brazoDer, antebrazoDer, manoDer, dedosDer, punoDer, OVRInput.Controller.RTouch),
        };
        var inv = Quaternion.Inverse(transform.rotation);
        if (cuello) cuelloOffset = inv * cuello.rotation;
        if (cabeza) cabezaOffset = inv * cabeza.rotation;
    }

    Brazo NuevoBrazo(Transform hombro, Transform codo, Transform mano, Transform[] dedos, Quaternion[] puno, OVRInput.Controller mando)
    {
        if (hombro == null || codo == null || mano == null) return null;
        var b = new Brazo
        {
            hombro = hombro, codo = codo, mano = mano, mando = mando,
            hombroReposo = hombro.localRotation, codoReposo = codo.localRotation,
            codoPos = codo.localPosition, manoPos = mano.localPosition,
            lado = Mathf.Sign(transform.InverseTransformPoint(hombro.position).x),
            dedos = dedos ?? new Transform[0],
        };
        b.abiertos = System.Array.ConvertAll(b.dedos, d => d.localRotation);
        b.puno = puno != null && puno.Length == b.dedos.Length ? puno : b.abiertos;
        b.medio = System.Array.Find(b.dedos, d => d.name.StartsWith("Medio_01"));
        var pulgar = System.Array.Find(b.dedos, d => d.name.StartsWith("Pulgar_01"));
        // Ejes de la mano: los dedos (hacia el medio) y el lado del pulgar, en local de la muñeca.
        // Al coger el mando el pulgar queda encima y los dedos hacia delante.
        var f = b.medio != null ? mano.InverseTransformDirection(b.medio.position - mano.position) : Vector3.up;
        var s = pulgar != null ? mano.InverseTransformDirection(pulgar.position - mano.position) : Vector3.forward;
        s = Vector3.ProjectOnPlane(s, f);
        b.eje = Quaternion.Inverse(Quaternion.LookRotation(f, s));
        return b;
    }

    void Start()
    {
        rig = GetComponentInParent<OVRCameraRig>();
        if (rig == null) rig = FindObjectOfType<OVRCameraRig>();
        if (animator != null) animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // el cuerpo solo lo ven los espejos
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bool esMano = r.name == "Manos";
            if (esMano) manosRenderer = r;
            bool visible = esMano ? !manosMetaEnPrimeraPersona : cuerpoEnPrimeraPersona && !r.name.StartsWith("Cara");
            r.gameObject.layer = visible ? 0 : AvatarBaymax.CapaCabeza;
            if (!visible) { cuerpo.Add(r); r.forceRenderingOff = true; }
        }
        if (rig != null)
            foreach (var c in rig.GetComponentsInChildren<Camera>(true)) c.cullingMask &= ~(1 << AvatarBaymax.CapaCabeza);
        PrepararManosMeta();
        Invoke(nameof(PrepararManosMeta), 1f);   // por si el SDK activa modelos de mando al conectarse
    }

    // Manos y mandos de Meta: visibles en primera persona (en su propia capa, que los espejos no ven)
    // o, si se eligen las manos de Baymax, ocultos. Los rayos y cursores de interacción no se tocan.
    void PrepararManosMeta()
    {
        if (rig == null) return;
        var raiz = rig.transform.parent != null ? rig.transform.parent : rig.transform;
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true))
        {
            if (r.transform.IsChildOf(transform) || !EsVisualDeMano(r.transform)) continue;
            r.forceRenderingOff = !manosMetaEnPrimeraPersona;
            r.gameObject.layer = AvatarBaymax.CapaManosMeta;
        }
    }

    static bool EsVisualDeMano(Transform t)
    {
        for (var p = t; p != null; p = p.parent)
        {
            var n = p.name;
            if (n.Contains("Interactor")) return false;
            if (n == "OVRControllerVisual" || n == "OVRLeftHandVisual" || n == "OVRRightHandVisual" || n.StartsWith("HandVisuals")) return true;
        }
        return false;
    }

    static Camera[] camaras = new Camera[8];

    // La vista del jugador nunca dibuja el cuerpo ni la cara de Baymax (solo los espejos, que pintan en
    // una textura). Se comprueba cada frame porque el simulador de Meta o el SDK pueden crear o rehacer
    // las cámaras de los ojos después de Start.
    static void OcultarCuerpoEnVista()
    {
        if (camaras.Length < Camera.allCamerasCount) camaras = new Camera[Camera.allCamerasCount * 2];
        int n = Camera.GetAllCameras(camaras);
        int bit = 1 << AvatarBaymax.CapaCabeza;
        for (int i = 0; i < n; i++)
        {
            var c = camaras[i];
            if (c != null && c.targetTexture == null && (c.cullingMask & bit) != 0) c.cullingMask &= ~bit;
        }
    }

    void LateUpdate()
    {
        OcultarCuerpoEnVista();
        if (rig == null || rig.centerEyeAnchor == null) return;
        var cab = rig.centerEyeAnchor;
        // El modo sentado sube el rig entero (y con él su "suelo"): se resta para apoyar los pies en el suelo real.
        extraVista = JugadorNanoLab.AlturaExtraVista;
        float suelo = (rig.trackingSpace != null ? rig.trackingSpace.position.y : rig.transform.position.y) - extraVista;
        Posar(cab.position, cab.rotation, Mano(rig.leftHandAnchor, OVRInput.Controller.LTouch, OVRInput.Controller.LHand),
              Mano(rig.rightHandAnchor, OVRInput.Controller.RTouch, OVRInput.Controller.RHand), suelo);
        ManosLejosDeLaCara(cab.position);
    }

    // Las manos y antebrazos de Baymax son la única parte que se ve en primera persona. Si un mando queda
    // pegado a la cara (al asomarse a algo, al estirar el brazo hacia la vista o en el simulador), la
    // cámara quedaba dentro de esa malla y se veía Baymax por dentro: en ese caso las manos se ocultan
    // (los espejos siguen mostrando el cuerpo entero).
    void ManosLejosDeLaCara(Vector3 ojo)
    {
        if (manosRenderer == null || manosMetaEnPrimeraPersona) return;
        float minimo = float.MaxValue;
        if (brazos != null)
            foreach (var b in brazos)
            {
                if (b == null) continue;
                minimo = Mathf.Min(minimo, DistanciaSegmento(ojo, b.codo.position, b.mano.position));
                if (b.medio != null) minimo = Mathf.Min(minimo, DistanciaSegmento(ojo, b.mano.position, b.medio.position));
            }
        // Grosor del antebrazo y la mano escalados (el bebé es redondo) + margen del plano cercano.
        float radio = 0.07f * escala * escalaManos + 0.06f;
        manosOcultas = minimo < radio;
        manosRenderer.forceRenderingOff = manosOcultas;
    }

    static float DistanciaSegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return Vector3.Distance(p, a + ab * t);
    }

    static Pose? Mano(Transform ancla, OVRInput.Controller mando, OVRInput.Controller mano)
    {
        if (ancla == null) return null;
        bool seguida = OVRInput.GetControllerPositionTracked(mando) || OVRInput.GetControllerPositionTracked(mano);
        return seguida ? new Pose(ancla.position, ancla.rotation) : (Pose?)null;
    }

    public void Posar(Vector3 cabezaPos, Quaternion cabezaRot, Pose? manoIzqPose, Pose? manoDerPose, float suelo)
    {
        float alturaOjos = cabezaPos.y - suelo;
        if (alturaOjos < 0.5f) return;   // sin seguimiento todavía
        Awake();

        // Escala uniforme (una no uniforme deformaría la malla al girar los huesos) según la altura del
        // jugador de pie: la mayor vista.
        // Con el modo sentado el cuerpo crece lo que sube la vista (la altura real de pie se sigue midiendo aparte).
        float real = alturaOjos - extraVista;
        if (!iniciado || real > alturaRef) alturaRef = real;
        float objetivo = Mathf.Clamp((alturaRef + extraVista) / ojos.y, escalaMin, escalaMax);
        escala = iniciado ? Mathf.MoveTowards(escala, objetivo, Time.deltaTime * 0.5f) : objetivo;
        transform.localScale = Vector3.one * escala;

        var adelante = Vector3.ProjectOnPlane(cabezaRot * Vector3.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f) adelante = Vector3.ProjectOnPlane(cabezaRot * Vector3.up, Vector3.up);
        float yawCabeza = Quaternion.LookRotation(adelante).eulerAngles.y;
        if (!iniciado) yawCuerpo = yawCabeza;
        float dif = Mathf.DeltaAngle(yawCuerpo, yawCabeza);
        if (Mathf.Abs(dif) > giroLibre) yawCuerpo += dif - Mathf.Sign(dif) * giroLibre;
        yawCuerpo = Mathf.MoveTowardsAngle(yawCuerpo, yawCabeza, Time.deltaTime * 40f);
        var rot = Quaternion.Euler(0f, yawCuerpo, 0f);

        // Los ojos del modelo quedan 'separacionCara' por detrás de la cámara (la barriga y la cabeza de
        // Baymax son anchas: con los ojos justo en la cámara, la vista quedaba dentro de la cabeza).
        var ojosMundo = rot * (ojos * escala) + rot * Vector3.forward * separacionCara;
        float y = Mathf.Min(suelo, cabezaPos.y - ojosMundo.y);
        var antes = transform.position;
        transform.SetPositionAndRotation(new Vector3(cabezaPos.x - ojosMundo.x, y, cabezaPos.z - ojosMundo.z), rot);

        // Idle o Walk según lo que se desplaza el cuerpo (el ciclo Walk avanza 0.1875 m/s a escala 1).
        if (animator != null && iniciado && Time.deltaTime > 0f)
        {
            var v = transform.position - antes;
            v.y = 0f;
            velSuave = Mathf.Lerp(velSuave, v.magnitude / Time.deltaTime, Mathf.Clamp01(Time.deltaTime * 8f));
            bool sentado = JugadorNanoLab.Sentado(out _);
            animator.SetFloat("Velocidad", sentado ? 0f : velSuave);
            animator.SetFloat("Ritmo", Mathf.Clamp(velSuave / (0.1875f * escala), 0.6f, 3f));
        }

        // La cabeza sigue al visor; el cuello acompaña un poco.
        var rel = Quaternion.Inverse(rot) * cabezaRot;
        if (cuello != null) cuello.rotation = rot * Quaternion.Slerp(Quaternion.identity, rel, 0.35f) * cuelloOffset;
        if (cabeza != null) cabeza.rotation = rot * rel * cabezaOffset;

        if (JugadorNanoLab.Sentado(out _)) Sentar(rot);
        if (brazos[0] != null) PosarBrazo(brazos[0], manoIzqPose, rot);
        if (brazos[1] != null) PosarBrazo(brazos[1], manoDerPose, rot);
        iniciado = true;
    }

    // Muslos hacia delante y piernas colgando.
    void Sentar(Quaternion cuerpo)
    {
        foreach (var muslo in new[] { musloIzq, musloDer })
        {
            if (muslo == null || muslo.childCount == 0) continue;
            var rodilla = muslo.GetChild(0);
            muslo.rotation = Quaternion.FromToRotation(rodilla.position - muslo.position, cuerpo * Vector3.forward) * muslo.rotation;
            if (rodilla.childCount > 0)
                rodilla.rotation = Quaternion.FromToRotation(rodilla.GetChild(0).position - rodilla.position, Vector3.down) * rodilla.rotation;
        }
    }

    void PosarBrazo(Brazo b, Pose? pose, Quaternion cuerpo)
    {
        b.codo.localScale = Vector3.one * escalaManos;
        b.codo.localPosition = b.codoPos;
        b.mano.localPosition = b.manoPos;
        if (pose == null) return;   // sin mando: el brazo y los dedos los mueve la animación

        b.hombro.localRotation = b.hombroReposo;
        b.codo.localRotation = b.codoReposo;
        var p = pose.Value;
        var r = p.rotation * Quaternion.Euler(inclinacionMano, 0f, 0f);
        var F = r * Vector3.forward;
        float palma = b.medio != null ? (b.medio.position - b.mano.position).magnitude * 0.55f : 0.05f;
        var W = p.position - F * (palma + retrocesoMano);   // muñeca: la palma queda en el mando

        var S = b.hombro.position;
        float L1 = Vector3.Distance(S, b.codo.position), L2 = Vector3.Distance(b.codo.position, b.mano.position);
        var SW = W - S;
        float d = SW.magnitude;
        if (d < 1e-4f || L1 < 1e-4f || L2 < 1e-4f) return;
        float max = L1 + L2;
        float min = Mathf.Sqrt(L1 * L1 + L2 * L2 + 2f * L1 * L2 * Mathf.Cos(codoMax * Mathf.Deg2Rad));
        float k = d > max ? Mathf.Min(d / max, estiramientoMax) : d < min ? Mathf.Max(d / min, 0.5f) : 1f;
        if (k != 1f)
        {
            b.codo.localPosition = b.codoPos * k;
            b.mano.localPosition = b.manoPos * k;
            L1 *= k; L2 *= k;
        }

        // Codo: hacia fuera, abajo y atrás.
        var dir = SW / d;
        float dd = Mathf.Clamp(d, Mathf.Abs(L1 - L2) + 1e-4f, L1 + L2 - 1e-4f);
        float cosA = Mathf.Clamp((L1 * L1 + dd * dd - L2 * L2) / (2f * L1 * dd), -1f, 1f);
        var perp = Vector3.ProjectOnPlane(cuerpo * new Vector3(b.lado * 0.6f, -1f, -0.4f), dir);
        if (perp.sqrMagnitude < 1e-6f) perp = cuerpo * Vector3.down;
        perp.Normalize();
        var E = S + L1 * (cosA * dir + Mathf.Sqrt(1f - cosA * cosA) * perp);
        b.hombro.rotation = Quaternion.FromToRotation(b.codo.position - S, E - S) * b.hombro.rotation;
        b.codo.rotation = Quaternion.FromToRotation(b.mano.position - b.codo.position, W - E) * b.codo.rotation;
        // El antebrazo gira sobre su eje con la mano (como el radio y el cúbito): si solo girase la
        // muñeca, la piel de entre los dos se retorcería en picos.
        var rotMano = Quaternion.LookRotation(F, r * Vector3.up) * b.eje;
        var rel = Quaternion.Inverse(b.codo.rotation) * rotMano;
        var eje = b.manoPos.normalized;
        var proy = Vector3.Dot(new Vector3(rel.x, rel.y, rel.z), eje) * eje;
        var giro = new Quaternion(proy.x, proy.y, proy.z, rel.w);
        float largo = Mathf.Sqrt(giro.x * giro.x + giro.y * giro.y + giro.z * giro.z + giro.w * giro.w);
        if (largo > 1e-5f)
            b.codo.rotation *= new Quaternion(giro.x / largo, giro.y / largo, giro.z / largo, giro.w / largo);
        b.mano.rotation = rotMano;

        bool jugando = Application.isPlaying;   // las capturas del editor también posan el avatar
        float grip = jugando ? OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, b.mando) : 0f;
        float gatillo = jugando ? OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, b.mando) : 0f;
        for (int i = 0; i < b.dedos.Length; i++)
        {
            var n = b.dedos[i].name;
            float c = n.StartsWith("Indice") ? gatillo : n.StartsWith("Pulgar") ? Mathf.Max(grip, gatillo) * 0.6f : grip;
            b.dedos[i].localRotation = Quaternion.Slerp(b.abiertos[i], b.puno[i], Mathf.Max(cierreReposo, c));
        }
    }
}
