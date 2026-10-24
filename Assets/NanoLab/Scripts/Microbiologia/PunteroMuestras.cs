using UnityEngine;

// Rayo corto mientras se lleva un portaobjetos en la mano (RNF-04: sin tener que acercar la mano
// con precisión). Apuntando a:
//  - un tubo, placa, desinfectante o gotero -> gatillo o A/X: toma la muestra (o la limpia/tiñe),
//  - la platina de un microscopio o la ranura de la estación 3D -> al soltar, se encaja allí,
//  - una mesa -> al soltar, el portaobjetos se deja plano en ese punto.
// Alcance corto (1,2 m) para no perder realismo. Lo instala JugadorNanoLab en la raíz del rig.
[DefaultExecutionOrder(50)]   // después de Portaobjetos (que detecta el soltar) y antes de su corrutina
public class PunteroMuestras : MonoBehaviour
{
    public float alcance = 1.2f;

    class Mano
    {
        public OVRInput.Controller mando;
        public Transform ancla;
        public LineRenderer linea;
        public Portaobjetos sostenido;
        public FuenteMuestra fuente, fuenteAvisada;
        public Platina platina;
        public bool superficie;
        public Vector3 punto;
    }

    static PunteroMuestras instancia;
    static readonly RaycastHit[] hits = new RaycastHit[24];
    readonly Mano[] manos = new Mano[2];
    Material normal, valido;

    // Último portaobjetos soltado y su destino (lo consulta Portaobjetos un frame después).
    Portaobjetos soltado;
    Platina destinoPlatina;
    bool destinoSuperficie;
    Vector3 destinoPunto;
    Quaternion destinoRot;
    float tSoltado;

    void Start()
    {
        instancia = this;
        var rig = GetComponentInChildren<OVRCameraRig>(true);
        if (rig == null) rig = FindObjectOfType<OVRCameraRig>();
        normal = Resources.Load<Material>("NanoLab/LineaPuntero");
        valido = Resources.Load<Material>("NanoLab/LineaPunteroOk");
        manos[0] = Crear(OVRInput.Controller.LTouch, rig != null ? rig.leftHandAnchor : null);
        manos[1] = Crear(OVRInput.Controller.RTouch, rig != null ? rig.rightHandAnchor : null);
    }

    Mano Crear(OVRInput.Controller mando, Transform ancla)
    {
        var go = new GameObject("Puntero_" + mando);
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<LineRenderer>();
        l.positionCount = 2;
        l.useWorldSpace = true;
        l.widthCurve = new AnimationCurve(new Keyframe(0f, 0.004f), new Keyframe(1f, 0.0015f));
        l.numCapVertices = 4;
        l.sharedMaterial = normal;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        l.receiveShadows = false;
        l.enabled = false;
        return new Mano { mando = mando, ancla = ancla, linea = l };
    }

    void Update()
    {
        foreach (var m in manos)
        {
            if (m == null || m.ancla == null) continue;
            var antes = m.sostenido;
            m.sostenido = Sostenido(m.ancla);
            if (antes != null && m.sostenido != antes)
            {
                // Acaba de soltarlo: se guarda adónde apuntaba en ese momento.
                soltado = antes;
                destinoPlatina = m.platina;
                destinoSuperficie = m.superficie;
                destinoPunto = m.punto;
                var f = Vector3.ProjectOnPlane(m.ancla.forward, Vector3.up);
                destinoRot = f.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(f) * Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
                tSoltado = Time.time;
            }
            Apuntar(m);
        }
    }

    // Portaobjetos agarrado por esta mano (el más cercano a ella).
    static Portaobjetos Sostenido(Transform ancla)
    {
        Portaobjetos mejor = null;
        float d = 0.3f * 0.3f;
        foreach (var p in Portaobjetos.Todos)
        {
            if (p == null || !p.Agarrado) continue;
            float x = (p.transform.position - ancla.position).sqrMagnitude;
            if (x < d) { d = x; mejor = p; }
        }
        return mejor;
    }

    void Apuntar(Mano m)
    {
        m.fuente = null; m.platina = null; m.superficie = false;
        if (m.sostenido == null) { m.linea.enabled = false; m.fuenteAvisada = null; return; }

        var o = m.ancla.position;
        var dir = m.ancla.forward;
        var fin = o + dir * alcance;
        int n = Physics.RaycastNonAlloc(o, dir, hits, alcance, ~0, QueryTriggerInteraction.Collide);
        OrdenarPorDistancia(n);
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c == null || c.transform.IsChildOf(transform) || c.transform.IsChildOf(m.sostenido.transform)) continue;
            var f = c.GetComponentInParent<FuenteMuestra>();
            if (f != null) { m.fuente = f; fin = hits[i].point; break; }
            var pl = PlatinaDe(c);
            if (pl != null) { m.platina = pl; fin = hits[i].point; break; }
            if (c.isTrigger) continue;
            fin = hits[i].point;
            if (hits[i].normal.y > 0.7f && c.attachedRigidbody == null) { m.superficie = true; m.punto = hits[i].point; }
            break;
        }

        m.linea.enabled = true;
        m.linea.SetPosition(0, o);
        m.linea.SetPosition(1, fin);
        bool objetivo = m.fuente != null || m.platina != null;
        var mat = objetivo ? valido : normal;
        if (mat != null && m.linea.sharedMaterial != mat) m.linea.sharedMaterial = mat;

        if (m.fuente != null)
        {
            if (m.fuente != m.fuenteAvisada)
            {
                m.fuenteAvisada = m.fuente;
                string accion = m.fuente.esDesinfectante ? "limpiar el portaobjetos" : m.fuente.reactivo > 0 ? "aplicar el reactivo" : "tomar la muestra";
                Aviso.Mostrar($"Gatillo o A/X: {accion}", fin + Vector3.up * 0.1f, Aviso.Tipo.Info, 2.5f);
            }
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, m.mando) || OVRInput.GetDown(OVRInput.Button.One, m.mando))
                m.fuente.Aplicar(m.sostenido);
        }
        else m.fuenteAvisada = null;
    }

    // Platina libre apuntada: su zona, o cualquier parte del microscopio / estación 3D que la tenga.
    static Platina PlatinaDe(Collider c)
    {
        var pl = c.GetComponentInParent<Platina>();
        if (pl == null)
        {
            var mo = c.GetComponentInParent<MicroscopioOptico>();
            if (mo != null) pl = mo.platina;
            else
            {
                var est = c.GetComponentInParent<EstacionAnalisis3D>();
                if (est != null) pl = est.platina;
            }
        }
        return pl != null && pl.Libre ? pl : null;
    }

    static void OrdenarPorDistancia(int n)
    {
        for (int i = 1; i < n; i++)
        {
            var h = hits[i];
            int j = i - 1;
            while (j >= 0 && hits[j].distance > h.distance) { hits[j + 1] = hits[j]; j--; }
            hits[j + 1] = h;
        }
    }

    // Adónde apuntaba la mano al soltar este portaobjetos (si fue hace un instante).
    public static bool Destino(Portaobjetos p, out Platina platina, out Vector3 punto, out Quaternion rot)
    {
        platina = null; punto = Vector3.zero; rot = Quaternion.identity;
        var i = instancia;
        if (i == null || i.soltado != p || Time.time - i.tSoltado > 0.5f) return false;
        i.soltado = null;
        platina = i.destinoPlatina != null && i.destinoPlatina.Libre ? i.destinoPlatina : null;
        punto = i.destinoPunto;
        rot = i.destinoRot;
        return platina != null || i.destinoSuperficie;
    }
}
