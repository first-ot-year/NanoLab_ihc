using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Proyector holográfico: al encajar un portaobjetos con virus en su ranura
// muestra el modelo 3D grande. Se maneja con botones físicos: rotar, zoom,
// vista superior / lateral y corte.
public class EstacionAnalisis3D : MonoBehaviour
{
    public Platina platina;
    public Transform holograma;          // punto donde flota el modelo
    public float diametro = 0.7f;
    public GameObject haz;               // cono de luz del proyector
    public TMP_Text titulo, ficha, estado;
    public Renderer micrografia;
    public BotonFisico rotarIzq, rotarDer, zoomMas, zoomMenos, vistaSuperior, vistaLateral, corte;

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

        estado.text = "Toca los botones para <b>rotar</b>, hacer <b>zoom</b>, cambiar de <b>vista</b> o ver el <b>corte</b>";
        escala = escalaObjetivo = 1f;
        inclinacion = inclinacionObjetivo = Quaternion.identity;
        auto = true; conCorte = false;
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
        if (modelo == null) return;
        float dt = Time.deltaTime;
        if (auto) holograma.Rotate(0f, 18f * dt, 0f, Space.World);
        if (rotarIzq && rotarIzq.Presionado) holograma.Rotate(0f, 90f * dt, 0f, Space.World);
        if (rotarDer && rotarDer.Presionado) holograma.Rotate(0f, -90f * dt, 0f, Space.World);

        escala = Mathf.Lerp(escala, escalaObjetivo, dt * 6f);
        inclinacion = Quaternion.Slerp(inclinacion, inclinacionObjetivo, dt * 5f);
        modelo.transform.localScale = Vector3.one * escala;
        modelo.transform.localRotation = inclinacion;
        holograma.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 1.2f) * 0.02f;

        if (conCorte) AplicarCorte();
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
