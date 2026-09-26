using Oculus.Interaction.Locomotion;
using UnityEngine;

// Al acercar la cara al ocular con un portaobjetos con muestra en la platina,
// se muestra el HUD de análisis. Mientras se mira, la locomoción del rig de Meta
// queda desactivada y los botones controlan el aumento y las pestañas.
public class MicroscopioOptico : MonoBehaviour
{
    public Platina platina;
    public Transform ocular;
    public GameObject ayuda;          // "Coloca un portaobjetos con muestra"
    public GameObject ayudaMirar;     // "Acerca la vista al ocular"
    public float distancia = 0.22f;

    static MicroscopioOptico activo;
    Material holoAzul;
    float ejeAnterior;

    void Start() => holoAzul = Resources.Load<Material>("NanoLab/Holograma_Azul");

    void Update()
    {
        var ojo = ManosRig.Cabeza;
        if (ojo == null || platina == null || ocular == null) return;
        float d = Vector3.Distance(ojo.position, ocular.position);
        var p = platina.Actual;
        var m = p != null ? p.muestra : null;

        if (ayuda != null) ayuda.SetActive(activo == null && m == null && d < 0.6f);
        if (ayudaMirar != null) ayudaMirar.SetActive(activo == null && m != null && d < 1.2f);

        if (activo == null && m != null && d < distancia) Entrar(m, ojo);
        else if (activo == this && (m == null || d > distancia * 1.35f)) Salir();

        if (activo == this) Controles();
    }

    void Entrar(MuestraData m, Transform ojo)
    {
        activo = this;
        HUDMicroscopio.Obtener(ojo, holoAzul).Mostrar(m);
        Bloquear(true);
        Progreso.Marcar(m);
        Sonidos.Scan(ocular.position);
    }

    void Salir()
    {
        if (HUDMicroscopio.Instancia != null) HUDMicroscopio.Instancia.Ocultar();
        activo = null;
        Bloquear(false);
    }

    // Mientras se mira por el ocular no hay teletransporte ni giro (el joystick cambia pestañas).
    static void Bloquear(bool b)
    {
        foreach (var l in FindObjectsOfType<PlayerLocomotor>(true)) l.enabled = !b;
    }

    void Controles()
    {
        var hud = HUDMicroscopio.Instancia;
        if (hud == null) return;
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three)) { hud.CambiarAumento(+1); Sonidos.Clic(ocular.position); }
        if (OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four)) { hud.CambiarAumento(-1); Sonidos.Clic(ocular.position); }
        float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x + OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;
        if (Mathf.Abs(x) > 0.7f && Mathf.Abs(ejeAnterior) <= 0.7f) { hud.CambiarPestana(x > 0 ? 1 : -1); Sonidos.Clic(ocular.position); }
        ejeAnterior = x;
    }

    void OnDisable() { if (activo == this) Salir(); }
}
