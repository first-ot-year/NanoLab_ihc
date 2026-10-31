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
    [Tooltip("Microscopio del Centro de Experimentación: el RetoManager decide qué se ve.")]
    public bool modoReto;

    // (microscopio, portaobjetos observado). Lo escucha el RetoManager.
    public static event System.Action<MicroscopioOptico, Portaobjetos> Observado;
    public static bool AlgunoActivo => activo != null;

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
        var hud = HUDMicroscopio.Obtener(ojo, holoAzul);
        // Sin teñir = examen en fresco: las bacterias se ven vivas y moviéndose.
        var pa = platina.Actual;
        bool fresco = pa != null && pa.PasosTincion == 0 && !pa.TincionErronea && !pa.Despues && !pa.DeCaso;
        hud.Mostrar(m, fresco);
        Bloquear(true);
        // Una muestra de caso no se revela en ningún microscopio, aunque se traiga a la sala principal.
        bool reto = modoReto || (platina.Actual != null && platina.Actual.DeCaso);
        if (!reto) Progreso.Marcar(m);
        Sonidos.Scan(ocular.position);
        if (reto) Observado?.Invoke(this, platina.Actual);   // puede pasar el HUD a modo reto
    }

    // Para las pruebas sin visor: mira por el ocular como si el usuario acercara la cara.
    public void ObservarAhora()
    {
        var p = platina != null ? platina.Actual : null;
        if (p == null || p.muestra == null || ManosRig.Cabeza == null) return;
        if (activo != null) activo.Salir();
        Entrar(p.muestra, ManosRig.Cabeza);
    }

    public void DejarDeObservar() { if (activo == this) Salir(); }

    void Salir()
    {
        if (HUDMicroscopio.Instancia != null) HUDMicroscopio.Instancia.Ocultar();
        activo = null;
        Bloquear(false);
    }

    // Mientras se mira por el ocular no hay desplazamiento ni giro (los joysticks mueven la muestra y cambian pestañas).
    static void Bloquear(bool b)
    {
        foreach (var j in FindObjectsOfType<JugadorNanoLab>(true)) j.PausarLocomocion(b);
    }

    void Controles()
    {
        var hud = HUDMicroscopio.Instancia;
        if (hud == null) return;
        if (OVRInput.GetDown(OVRInput.Button.One)) { hud.CambiarAumento(+1); Sonidos.Clic(ocular.position); }
        if (OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four)) { hud.CambiarAumento(-1); Sonidos.Clic(ocular.position); }
        // X: el joystick izquierdo pasa de mover la muestra a mover la vista (para ver los paneles laterales).
        if (OVRInput.GetDown(OVRInput.Button.Three))
        {
            hud.AlternarModo();
            Sonidos.Modo(ocular.position, hud.ModoVista);
            Vibrar(OVRInput.Controller.LTouch, hud.ModoVista ? 0.6f : 0.35f);
        }
        // Joystick izquierdo: mover la muestra (o girar el virus) o la vista. Joystick derecho: pestañas.
        var eje = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
        if (hud.ModoVista) hud.MoverVista(eje);
        else hud.Mover(eje);
        float x = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;
        if (Mathf.Abs(x) > 0.7f && Mathf.Abs(ejeAnterior) <= 0.7f) { hud.CambiarPestana(x > 0 ? 1 : -1); Sonidos.Clic(ocular.position); }
        ejeAnterior = x;
    }

    // Pulso corto en el mando que pulsó X (con la cara en el ocular, las manos pueden quedar lejos).
    void Vibrar(OVRInput.Controller mando, float fuerza)
    {
        OVRInput.SetControllerVibration(1f, fuerza, mando);
        CancelInvoke(nameof(PararVibracion));
        Invoke(nameof(PararVibracion), 0.1f);
    }

    void PararVibracion() => OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);

    void OnDisable() { if (activo == this) Salir(); }
}
