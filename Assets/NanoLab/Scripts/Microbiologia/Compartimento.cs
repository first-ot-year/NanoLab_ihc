using UnityEngine;

// Puerta de vidrio, tapa o cajón. Se abre de dos formas:
//  - apuntándolo con el rayo de la mano vacía (gatillo o A/X): se abre o se cierra del todo;
//  - con la mano (solo cajones): acerca el mando al frente del cajón, aprieta el grip y JALA. El cajón
//    sigue a la mano a lo largo de su guía y se queda donde lo sueltes (empujándolo se cierra).
// Va en la pieza que se mueve: sus colliders son los que encuentra el rayo. Mientras está cerrado,
// lo guardado en sus huecos no se puede agarrar (Hueco desactiva los interactables del objeto).
public class Compartimento : Apuntable
{
    public enum Tipo { Bisagra, Corredera }

    public Tipo tipo = Tipo.Bisagra;
    [Tooltip("Bisagra: eje de giro en el espacio del padre.")]
    public Vector3 eje = Vector3.up;
    [Tooltip("Bisagra: grados que gira al abrirse.")]
    public float angulo = -105f;
    [Tooltip("Corredera: desplazamiento al abrirse, en el espacio del padre (m).")]
    public Vector3 recorrido = new Vector3(0f, 0f, 0.4f);
    [Tooltip("Cómo se nombra en el aviso: \"abrir {nombre}\".")]
    public string nombre = "la puerta";
    [Tooltip("Aviso la primera vez que se abre (qué hay dentro).")]
    [TextArea(2, 4)] public string avisoAlAbrir;
    public float velocidad = 2.2f;
    [Tooltip("Corredera: se puede agarrar el frente con el grip y jalar.")]
    public bool jalarConLaMano = true;
    [Tooltip("Distancia (m) del mando al frente del cajón para poder agarrarlo.")]
    public float alcanceMano = 0.12f;

    // Accesible a partir de algo menos de media apertura.
    public bool Abierto => t > 0.4f && (objetivo > 0.4f || mano != null);

    bool avisado;
    float t, objetivo;
    Vector3 pos0;
    Quaternion rot0;

    // Agarre con la mano.
    Transform mano;
    OVRInput.Controller mandoAgarre;
    float tAlAgarrar, proyAlAgarrar, ultimaVibracion;
    readonly bool[] gripAntes = new bool[2];
    Bounds local;            // caja de la malla, en local del cajón
    bool hayMalla;

    void Awake()
    {
        pos0 = transform.localPosition;
        rot0 = transform.localRotation;
        var mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null) { local = mf.sharedMesh.bounds; hayMalla = true; }
        else
        {
            var col = GetComponent<BoxCollider>();
            if (col != null) { local = new Bounds(col.center, col.size); hayMalla = true; }
        }
    }

    public override string Accion => (objetivo > 0.5f ? "cerrar " : "abrir ") + nombre;

    public override void Activar()
    {
        bool abrir = objetivo < 0.5f;
        objetivo = abrir ? 1f : 0f;
        Sonidos.Puerta(transform.position, abrir);
        ManosRig.Vibrar(transform.position, 0.35f, 0.06f);
        if (abrir) AvisoPrimeraVez();
    }

    void AvisoPrimeraVez()
    {
        if (avisado || string.IsNullOrEmpty(avisoAlAbrir)) return;
        avisado = true;
        Aviso.Mostrar(avisoAlAbrir, transform.position + Vector3.up * 0.25f, Aviso.Tipo.Info, 4f);
    }

    void Update()
    {
        if (tipo == Tipo.Corredera && jalarConLaMano && hayMalla) Jalar();
        if (mano == null && !Mathf.Approximately(t, objetivo))
        {
            t = Mathf.MoveTowards(t, objetivo, Time.deltaTime * velocidad);
            Poner(Mathf.SmoothStep(0f, 1f, t));
        }
    }

    void Poner(float k)
    {
        if (tipo == Tipo.Bisagra) transform.localRotation = Quaternion.AngleAxis(angulo * k, eje) * rot0;
        else transform.localPosition = pos0 + recorrido * k;
    }

    // Dirección de apertura (mundo) y largo total del recorrido (m).
    Vector3 Direccion(out float largo)
    {
        var w = transform.parent != null ? transform.parent.TransformVector(recorrido) : recorrido;
        largo = w.magnitude;
        return largo > 1e-4f ? w / largo : transform.forward;
    }

    void Jalar()
    {
        var dir = Direccion(out float largo);
        if (largo < 0.02f) return;

        if (mano != null)
        {
            // Mientras se aprieta el grip, el cajón sigue a la mano a lo largo de la guía.
            if (!Grip(mandoAgarre))
            {
                objetivo = t;   // se queda donde se suelta
                if (t > 0.4f) AvisoPrimeraVez();
                mano = null;
                return;
            }
            float proy = Vector3.Dot(mano.position, dir);
            float nuevo = Mathf.Clamp01(tAlAgarrar + (proy - proyAlAgarrar) / largo);
            if (Mathf.Abs(nuevo - t) > 1e-4f)
            {
                if (Time.time - ultimaVibracion > 0.07f && Mathf.Abs(nuevo - t) * largo > 0.004f)
                {
                    ultimaVibracion = Time.time;
                    OVRInput.SetControllerVibration(0.3f, 0.12f, mandoAgarre);
                    CancelInvoke(nameof(PararVibracion));
                    Invoke(nameof(PararVibracion), 0.06f);
                }
                if ((nuevo >= 1f && t < 1f) || (nuevo <= 0f && t > 0f)) Sonidos.Encaje(transform.position);   // tope
                t = nuevo;
                objetivo = t;
                transform.localPosition = pos0 + recorrido * t;   // lineal: sigue a la mano sin suavizado
            }
            return;
        }

        // ¿Algún mano acaba de apretar el grip junto al frente del cajón?
        Comprobar(ManosRig.ManoIzq, OVRInput.Controller.LTouch, 0, dir);
        if (mano == null) Comprobar(ManosRig.ManoDer, OVRInput.Controller.RTouch, 1, dir);
    }

    void Comprobar(Transform m, OVRInput.Controller c, int i, Vector3 dir)
    {
        bool g = Grip(c);
        bool empieza = g && !gripAntes[i];
        gripAntes[i] = g;
        if (!empieza || m == null || !CercaDelFrente(m.position, dir)) return;
        mano = m;
        mandoAgarre = c;
        // El 't' visible puede venir suavizado: se continúa desde la posición real del cajón.
        var d = transform.localPosition - pos0;
        t = recorrido.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector3.Dot(d, recorrido) / recorrido.sqrMagnitude) : t;
        tAlAgarrar = t;
        proyAlAgarrar = Vector3.Dot(m.position, dir);
        Sonidos.Puerta(transform.position, t < 0.5f);
        OVRInput.SetControllerVibration(0.5f, 0.35f, c);
        CancelInvoke(nameof(PararVibracion));
        Invoke(nameof(PararVibracion), 0.08f);
    }

    // El mando está delante del frente del cajón (o un poco dentro) y dentro de su ancho y alto.
    bool CercaDelFrente(Vector3 p, Vector3 dir)
    {
        var l = transform.InverseTransformPoint(p);
        var dirL = transform.InverseTransformDirection(dir).normalized;
        var s = transform.lossyScale;
        float escala = Mathf.Max(1e-4f, Mathf.Abs(Vector3.Dot(Vector3.Scale(dirL, s), dirL)));
        // Distancia (m) del mando al plano del frente, a lo largo de la dirección de apertura.
        float frenteL = Vector3.Dot(local.center, dirL) + Mathf.Abs(Vector3.Dot(local.extents, new Vector3(Mathf.Abs(dirL.x), Mathf.Abs(dirL.y), Mathf.Abs(dirL.z))));
        float dist = (Vector3.Dot(l, dirL) - frenteL) * escala;
        if (dist < -0.05f || dist > alcanceMano) return false;
        // Dentro del ancho y el alto del frente (con margen).
        var lateral = l - dirL * Vector3.Dot(l, dirL);
        var c = local.center - dirL * Vector3.Dot(local.center, dirL);
        var dl = Vector3.Scale(lateral - c, s);
        var ext = Vector3.Scale(local.extents, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));
        return Mathf.Abs(dl.x) < ext.x + 0.05f && Mathf.Abs(dl.y) < ext.y + 0.05f && Mathf.Abs(dl.z) < ext.z + 0.05f;
    }

    static bool Grip(OVRInput.Controller c) => OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, c) > 0.55f;

    void PararVibracion()
    {
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
    }
}
