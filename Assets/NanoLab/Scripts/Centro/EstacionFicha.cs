using TMPro;
using UnityEngine;

// Ficha consultable de la biblioteca: se abre con su botón, apuntándola con el rayo o tocando el panel,
// y registra la consulta (evidencia, RF-04).
public class EstacionFicha : Apuntable
{
    public string id;
    public GameObject abierta, cerrada;
    public BotonFisico boton;
    public TMP_Text botonTxt;
    [Tooltip("Medio tamaño del panel que se puede tocar (m, en local).")]
    public Vector3 zonaToque = new Vector3(0.8f, 0.62f, 0.08f);

    public bool Abierta { get; private set; }
    public override string Accion => Abierta ? "cerrar la ficha" : "abrir la ficha";

    bool tocando;
    float ultimo = -1f;

    void Awake() => Poner(false);
    void OnEnable() { if (boton != null) boton.Pulsado += Alternar; }
    void OnDisable() { if (boton != null) boton.Pulsado -= Alternar; }

    public override void Activar() => Alternar();

    public void Alternar()
    {
        if (Time.time - ultimo < 0.4f) return;   // botón, toque y rayo no se suman
        ultimo = Time.time;
        Poner(!Abierta);
        if (Abierta && RetoManager.Instancia != null) RetoManager.Instancia.FichaConsultada(id);
    }

    public void Poner(bool v)
    {
        Abierta = v;
        if (abierta != null) abierta.SetActive(v);
        if (cerrada != null) cerrada.SetActive(!v);
        if (botonTxt != null) botonTxt.text = v ? "CERRAR" : "ABRIR";
    }

    // Tocar el panel con la mano (o el mando) lo abre o lo cierra.
    void Update()
    {
        bool ahora = ManoDentro();
        if (ahora && !tocando)
        {
            Alternar();
            ManosRig.Vibrar(transform.position, 0.3f, 0.05f);
        }
        tocando = ahora;
    }

    public bool Contiene(Vector3 mundo)
    {
        var l = transform.InverseTransformPoint(mundo);
        return Mathf.Abs(l.x) < zonaToque.x && Mathf.Abs(l.y) < zonaToque.y && Mathf.Abs(l.z) < zonaToque.z;
    }

    static OVRCameraRig rig;

    bool ManoDentro()
    {
        if (rig == null) rig = FindObjectOfType<OVRCameraRig>();
        if (rig == null) return false;
        return (rig.leftHandAnchor != null && Contiene(rig.leftHandAnchor.position))
            || (rig.rightHandAnchor != null && Contiene(rig.rightHandAnchor.position));
    }
}
