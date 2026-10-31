using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Proyector holográfico: al encajar un portaobjetos con virus en su ranura
// muestra el modelo 3D grande. Se manipula con las manos en 3D:
//  - una mano: acércala al virus y aprieta el grip o el gatillo (o pellizca con la mano rastreada):
//    el virus queda agarrado y sigue la posición y el giro de la mano,
//  - dos manos: agárralo con ambas; separarlas lo amplía, juntarlas lo reduce, y girarlas lo gira,
//  - al soltarlo vuelve flotando al proyector y conserva la orientación en la que lo dejaste.
// También se maneja con los botones físicos: rotar, zoom, vista superior / lateral y corte.
public class EstacionAnalisis3D : MonoBehaviour
{
    public Platina platina;
    public Transform holograma;          // punto donde flota el modelo
    public float diametro = 0.7f;
    public GameObject haz;               // cono de luz del proyector
    public Transform plataforma;         // anillos holográficos (giran despacio; más intensos con un modelo)
    public TMP_Text titulo, ficha, estado;
    public Renderer micrografia;
    public BotonFisico rotarIzq, rotarDer, zoomMas, zoomMenos, vistaSuperior, vistaLateral, corte;

    [Header("Manipulación con las manos")]
    [Tooltip("Margen (m) alrededor del virus dentro del cual la mano puede agarrarlo.")]
    public float margenAgarre = 0.15f;
    [Tooltip("Escala mínima y máxima al ampliar con dos manos.")]
    public Vector2 limitesEscala = new Vector2(0.4f, 2.5f);
    [Tooltip("Distancia máxima (m) a la que se puede alejar el virus del proyector mientras se sostiene.")]
    public float alejamientoMax = 1.2f;
    [Tooltip("Velocidad con la que vuelve al proyector al soltarlo.")]
    public float vuelta = 3f;

    class Mano
    {
        public OVRInput.Controller mando;
        public OVRHand.Hand tipo;
        public Transform ancla;
        public OVRHand hand;
        public bool agarra, apretadoAntes, avisada;
        public Vector3 posLocal;          // posición del virus en el espacio de la mano al agarrar
        public Quaternion rotLocal;
    }
    readonly Mano[] manos = new Mano[2];
    float dist0, escala0;
    Vector3 eje0;
    Quaternion rot0;
    bool dosManos;

    GameObject modelo;
    readonly List<Material> mats = new List<Material>();
    Material holoColor;
    Transform ojo;
    float escala = 1f, escalaObjetivo = 1f;
    bool auto = true, conCorte;
    Quaternion inclinacion = Quaternion.identity, inclinacionObjetivo = Quaternion.identity;
    Vector3 basePos;

    void Start()
    {
        holoColor = Resources.Load<Material>("NanoLab/Holograma_Color");
        var rig = FindObjectOfType<OVRCameraRig>();
        ojo = rig != null ? rig.centerEyeAnchor : (Camera.main != null ? Camera.main.transform : null);
        basePos = holograma.localPosition;
        PrepararManos(rig);
        platina.Cambio += AlCambiar;
        if (rotarIzq) rotarIzq.Pulsado += () => auto = false;
        if (rotarDer) rotarDer.Pulsado += () => auto = false;
        if (zoomMas) zoomMas.Pulsado += () => escalaObjetivo = Mathf.Min(1.8f, escalaObjetivo + 0.2f);
        if (zoomMenos) zoomMenos.Pulsado += () => escalaObjetivo = Mathf.Max(0.5f, escalaObjetivo - 0.2f);
        if (vistaSuperior) vistaSuperior.Pulsado += () => { inclinacionObjetivo = Quaternion.Euler(80f, 0f, 0f); auto = false; };
        if (vistaLateral) vistaLateral.Pulsado += () => { inclinacionObjetivo = Quaternion.identity; auto = true; };
        if (corte) corte.Pulsado += () => { conCorte = !conCorte; AplicarCorte(); };
        AlCambiar(platina.Actual);
    }

    public void AlCambiar(Portaobjetos p)
    {
        if (modelo != null) { Destroy(modelo); modelo = null; }
        mats.Clear();
        var m = p != null ? p.muestra : null;
        if (haz) haz.SetActive(false);

        if (m == null)
        {
            titulo.text = "ESTACIÓN DE ANÁLISIS 3D";
            ficha.text = "";
            estado.text = "Coloca un portaobjetos con una <b>muestra viral</b> en la ranura iluminada";
            if (micrografia) micrografia.enabled = false;
            return;
        }

        titulo.text = m.nombre.ToUpper();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=#8FA6BC>{m.nombreCientifico}</color>");
        if (m.caracteristicas != null)
            foreach (var d in m.caracteristicas) sb.AppendLine($"<color=#5FD3FF>{d.clave}:</color> {d.valor}");
        sb.AppendLine($"<color=#FF7070><b>Riesgo: {m.riesgo}</b></color>");
        ficha.text = sb.ToString();
        if (micrografia) { micrografia.enabled = m.micrografia != null; micrografia.material.mainTexture = m.micrografia; }

        if (m.tipo != TipoMuestra.Virus)
        {
            estado.text = "El modelo 3D está disponible para las <b>muestras virales</b>. Observa esta muestra en un microscopio.";
            return;
        }

        estado.text = "<b>Agarra el virus</b> con el grip o el gatillo (o pellízcalo) para girarlo; con <b>las dos manos</b>, sepáralas para ampliarlo. También puedes usar los botones.";
        escala = escalaObjetivo = 1f;
        inclinacion = inclinacionObjetivo = Quaternion.identity;
        auto = true; conCorte = false;
        SoltarTodo();
        holograma.localPosition = basePos;
        holograma.localRotation = Quaternion.identity;
        // Pivote intermedio en el centro del virus: el zoom y la inclinación giran/escalan sobre él.
        modelo = new GameObject("Centro");
        modelo.transform.SetParent(holograma, false);
        ModeloVirus.Instanciar(m, holoColor, modelo.transform, diametro);
        foreach (var r in modelo.GetComponentsInChildren<Renderer>()) mats.AddRange(r.materials);
        AplicarCorte();
        if (haz) haz.SetActive(true);
        Progreso.Marcar(m);
        Sonidos.Scan(holograma.position);
    }

    void Update()
    {
        if (plataforma != null)
        {
            plataforma.Rotate(0f, (modelo != null ? -14f : -5f) * Time.deltaTime, 0f, Space.Self);
            float s = Mathf.Lerp(plataforma.localScale.x, modelo != null ? 1f : 0.85f, Time.deltaTime * 3f);
            plataforma.localScale = Vector3.one * s;
        }
        if (modelo == null) { SoltarTodo(); return; }
        float dt = Time.deltaTime;
        bool sostenido = Manipular();
        if (!sostenido)
        {
            if (auto) holograma.Rotate(0f, 18f * dt, 0f, Space.World);
            if (rotarIzq && rotarIzq.Presionado) holograma.Rotate(0f, 90f * dt, 0f, Space.World);
            if (rotarDer && rotarDer.Presionado) holograma.Rotate(0f, -90f * dt, 0f, Space.World);
            // Vuelve flotando al proyector (con el leve vaivén del holograma).
            var objetivo = basePos + Vector3.up * Mathf.Sin(Time.time * 1.2f) * 0.02f;
            holograma.localPosition = Vector3.Lerp(holograma.localPosition, objetivo, Mathf.Clamp01(dt * vuelta));
        }

        escala = Mathf.Lerp(escala, escalaObjetivo, dt * (dosManos ? 20f : 6f));
        inclinacion = Quaternion.Slerp(inclinacion, inclinacionObjetivo, dt * 5f);
        modelo.transform.localScale = Vector3.one * escala;
        modelo.transform.localRotation = inclinacion;

        if (conCorte) AplicarCorte();
    }

    // ------------------------------------------------------------------ manos

    void PrepararManos(OVRCameraRig rig)
    {
        manos[0] = new Mano { mando = OVRInput.Controller.LTouch, tipo = OVRHand.Hand.HandLeft, ancla = rig != null ? rig.leftHandAnchor : null };
        manos[1] = new Mano { mando = OVRInput.Controller.RTouch, tipo = OVRHand.Hand.HandRight, ancla = rig != null ? rig.rightHandAnchor : null };
        foreach (var h in FindObjectsOfType<OVRHand>(true))
            foreach (var m in manos)
                if ((int)h.GetHand() == (int)m.tipo) m.hand = h;
    }

    float Radio => diametro * 0.5f * escala;

    bool Apretado(Mano m)
    {
        if (m.hand != null && m.hand.IsTracked) return m.hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, m.mando) > 0.55f
            || OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, m.mando) > 0.55f;
    }

    // Devuelve true mientras alguna mano sostiene el virus.
    bool Manipular()
    {
        int antes = Agarrando();
        foreach (var m in manos)
        {
            if (m == null) continue;
            if (m.ancla == null || !m.ancla.gameObject.activeInHierarchy) { m.agarra = false; continue; }
            bool apretado = Apretado(m);
            float d = Vector3.Distance(m.ancla.position, holograma.position);
            bool cerca = d < Radio + margenAgarre;
            // Pista la primera vez que la mano se acerca.
            if (cerca && !m.avisada && !m.agarra)
            {
                m.avisada = true;
                Aviso.Mostrar("Grip o gatillo: agarrar el virus · con las dos manos: ampliar", holograma.position + Vector3.up * (Radio + 0.12f), Aviso.Tipo.Info, 3f);
            }
            // Agarra al apretar cerca (flanco de subida: no se pega si ya venía apretado de lejos).
            if (!m.agarra && apretado && !m.apretadoAntes && cerca)
            {
                m.agarra = true;
                ManosRig.Vibrar(m.ancla.position, 0.5f, 0.06f);
            }
            else if (m.agarra && !apretado) m.agarra = false;
            m.apretadoAntes = apretado;
        }

        int ahora = Agarrando();
        if (ahora != antes) Reanclar(ahora);
        if (ahora == 0) return false;

        auto = false;
        if (ahora == 1)
        {
            var m = manos[0].agarra ? manos[0] : manos[1];
            holograma.SetPositionAndRotation(m.ancla.TransformPoint(m.posLocal), m.ancla.rotation * m.rotLocal);
        }
        else
        {
            // Dos manos: escala por la separación y giro por el eje que las une.
            var a = manos[0].ancla.position;
            var b = manos[1].ancla.position;
            var eje = b - a;
            float dist = eje.magnitude;
            if (dist > 1e-3f && dist0 > 1e-3f)
            {
                escalaObjetivo = Mathf.Clamp(escala0 * dist / dist0, limitesEscala.x, limitesEscala.y);
                holograma.rotation = Quaternion.FromToRotation(eje0, eje) * rot0;
            }
            holograma.position = Vector3.Lerp(holograma.position, (a + b) * 0.5f, Mathf.Clamp01(Time.deltaTime * 12f));
        }
        // Siempre cerca del proyector (no se lo lleva por la sala).
        var centro = holograma.parent != null ? holograma.parent.TransformPoint(basePos) : basePos;
        var off = holograma.position - centro;
        if (off.magnitude > alejamientoMax) holograma.position = centro + off.normalized * alejamientoMax;
        return true;
    }

    int Agarrando()
    {
        int n = 0;
        foreach (var m in manos) if (m != null && m.agarra) n++;
        return n;
    }

    // Al cambiar el número de manos se recalcula la referencia para que el virus no salte.
    void Reanclar(int n)
    {
        dosManos = n == 2;
        if (n == 1)
        {
            var m = manos[0].agarra ? manos[0] : manos[1];
            m.posLocal = m.ancla.InverseTransformPoint(holograma.position);
            m.rotLocal = Quaternion.Inverse(m.ancla.rotation) * holograma.rotation;
        }
        else if (n == 2)
        {
            eje0 = manos[1].ancla.position - manos[0].ancla.position;
            dist0 = eje0.magnitude;
            escala0 = escala;
            escalaObjetivo = escala;
            rot0 = holograma.rotation;
        }
        else Sonidos.Clic(holograma.position);
    }

    void SoltarTodo()
    {
        foreach (var m in manos) if (m != null) { m.agarra = false; m.apretadoAntes = false; }
        dosManos = false;
    }

    // Plano que pasa por el centro y mira al usuario: quita la mitad cercana.
    void AplicarCorte()
    {
        Vector3 n = Vector3.forward;
        if (ojo != null) { n = holograma.position - ojo.position; n.y = 0f; n = n.sqrMagnitude > 1e-4f ? n.normalized : Vector3.forward; }
        var plano = new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, holograma.position));
        foreach (var m in mats)
        {
            m.SetFloat("_ClipOn", conCorte ? 1f : 0f);
            m.SetVector("_ClipPlane", plano);
        }
    }
}
