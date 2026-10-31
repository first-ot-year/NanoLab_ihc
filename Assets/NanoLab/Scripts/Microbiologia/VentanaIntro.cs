using TMPro;
using UnityEngine;

// Introducción narrativa (RF-01) como ventana pequeña paso a paso: quién es Baymax, el objetivo de
// NanoLab VR, cómo se investiga, los controles, el espejo y dónde empezar. Aparece delante de la cara
// al entrar en la sala; se avanza con SIGUIENTE / ATRÁS (tocando los botones o apuntándolos con el
// rayo y pulsando el gatillo) y se puede SALTAR. Al cerrarla queda una insignia pequeña con los
// recuerdos recuperados y un botón para volver a verla. La monta MontarIntroduccion.
public class VentanaIntro : MonoBehaviour
{
    [System.Serializable]
    public struct Paso
    {
        public string titulo;
        [TextArea(3, 8)] public string texto;
    }

    [Header("Ventana")]
    public GameObject ventana;
    public TMP_Text titulo, texto, contador, etiquetaSiguiente;
    public PanelRedondeado[] puntos;
    public BotonMenu atras, siguiente, saltar;

    [Header("Insignia (ventana cerrada)")]
    public GameObject insignia;
    public BotonMenu reabrir;

    [Header("Colocación")]
    [Tooltip("Distancia de la ventana a la cabeza (m): al alcance de la mano.")]
    public float distancia = 0.7f;
    [Tooltip("Cuánto queda por debajo de los ojos (m).")]
    public float bajoOjos = 0.12f;
    [Tooltip("Si la cabeza se aleja más que esto (m), la ventana vuelve a ponerse delante.")]
    public float distanciaRecolocar = 2.2f;

    public Paso[] pasos = PasosPorDefecto();

    // Solo se abre sola la primera vez en la sesión (al volver de Química no se repite).
    static bool vista;

    int actual;
    bool colocada;

    static readonly Color Activo = new Color(0.37f, 0.83f, 1f), Inactivo = new Color(0.2f, 0.26f, 0.33f);

    public bool Abierta => ventana != null && ventana.activeSelf;

    void Awake()
    {
        if (atras) atras.Pulsado += _ => Ir(actual - 1);
        if (siguiente) siguiente.Pulsado += _ => { if (actual >= pasos.Length - 1) Cerrar(); else Ir(actual + 1); };
        if (saltar) saltar.Pulsado += _ => Cerrar();
        if (reabrir) reabrir.Pulsado += _ => Abrir();
    }

    void Start()
    {
        if (vista) { Mostrar(false); return; }
        actual = 0;
        Mostrar(true);
        Refrescar();
        // El seguimiento de la cabeza tarda un instante en estar listo al arrancar.
        Invoke(nameof(Colocar), 0.6f);
    }

    public void Abrir()
    {
        actual = 0;
        Mostrar(true);
        Refrescar();
        Colocar();
        Sonidos.Clic(ventana.transform.position);
    }

    public void Cerrar()
    {
        vista = true;
        Mostrar(false);
        if (insignia != null) Sonidos.Clic(insignia.transform.position);
    }

    void Mostrar(bool abierta)
    {
        if (ventana != null) ventana.SetActive(abierta);
        if (insignia != null) insignia.SetActive(!abierta);
        colocada = colocada && abierta;
    }

    void Ir(int paso)
    {
        actual = Mathf.Clamp(paso, 0, pasos.Length - 1);
        Refrescar();
    }

    void Refrescar()
    {
        if (pasos == null || pasos.Length == 0) return;
        var p = pasos[actual];
        if (titulo) titulo.text = p.titulo;
        if (texto) texto.text = p.texto;
        if (contador) contador.text = $"PASO {actual + 1} DE {pasos.Length}";
        bool ultimo = actual == pasos.Length - 1;
        if (etiquetaSiguiente) etiquetaSiguiente.text = ultimo ? "¡EMPEZAR!" : "SIGUIENTE >";
        if (siguiente) siguiente.accion = ultimo ? "empezar" : "ver el siguiente paso";
        if (atras) atras.gameObject.SetActive(actual > 0);
        if (saltar) saltar.gameObject.SetActive(!ultimo);
        if (puntos != null)
            for (int i = 0; i < puntos.Length; i++)
            {
                if (puntos[i] == null) continue;
                puntos[i].gameObject.SetActive(i < pasos.Length);
                puntos[i].color = i == actual ? Activo : Inactivo;
                puntos[i].Aplicar();
            }
    }

    // Delante de la cara, un poco por debajo de los ojos, mirando al usuario.
    void Colocar()
    {
        var cab = ManosRig.Cabeza;
        if (cab == null || ventana == null) return;
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) f = transform.forward;
        f.Normalize();
        ventana.transform.position = cab.position + f * distancia - Vector3.up * bajoOjos;
        ventana.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
        colocada = true;
    }

    void Update()
    {
        if (!Abierta) return;
        var cab = ManosRig.Cabeza;
        if (cab == null) return;
        // Si el usuario se aleja caminando, la ventana vuelve delante de él.
        if (!colocada || Vector3.Distance(cab.position, ventana.transform.position) > distanciaRecolocar) Colocar();
        // Siempre de cara al usuario (solo gira en horizontal, sin balanceos).
        var d = Vector3.ProjectOnPlane(ventana.transform.position - cab.position, Vector3.up);
        if (d.sqrMagnitude > 1e-4f)
            ventana.transform.rotation = Quaternion.Slerp(ventana.transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), Time.deltaTime * 4f);
    }

    public static Paso[] PasosPorDefecto() => new[]
    {
        new Paso
        {
            titulo = "¡HOLA! ERES BAYMAX",
            texto = "Eres <b>Baymax</b>, un pequeño robot asistente de salud. En NanoLab VR no lo miras desde fuera: " +
                    "<b>tú eres Baymax</b>. Sus manos son tus manos y, en el espejo, verás su cuerpo moverse contigo.",
        },
        new Paso
        {
            titulo = "PERDISTE TUS RECUERDOS",
            texto = "Baymax ha perdido sus <b>recuerdos científicos</b>. Para recuperarlos tendrás que investigar muestras " +
                    "como un microbiólogo. Cada caso que resuelvas te devuelve un recuerdo: hay <color=#FFD25A><b>3</b></color> por recuperar.",
        },
        new Paso
        {
            titulo = "EL OBJETIVO DE NANOLAB VR",
            texto = "La mayoría de los microorganismos <b>no se ven a simple vista</b>. NanoLab VR es un laboratorio virtual para " +
                    "<b>explorar, observar y experimentar</b> con bacterias y virus, repetir cuantas veces quieras y aprender de tus decisiones.\n" +
                    "<size=80%><color=#A9BED2><i>Complemento educativo: las muestras y resultados están simplificados; no son protocolos reales.</i></color></size>",
        },
        new Paso
        {
            titulo = "CÓMO SE INVESTIGA",
            texto = "Cada caso sigue el ciclo del método científico:\n" +
                    "<color=#5FD3FF><b>Explorar · Observar · Investigar · Hipótesis · Experimentar · Analizar</b></color>\n" +
                    "Las pistas te orientan, pero <b>las decisiones son tuyas</b>. Si te equivocas recibes una pista y puedes volver a intentarlo.",
        },
        new Paso
        {
            titulo = "CONTROLES",
            texto = "<b>Joystick izquierdo:</b> caminar  ·  <b>derecho:</b> girar\n" +
                    "<b>Grip o gatillo:</b> agarrar objetos (también con el rayo)\n" +
                    "<b>Botones:</b> tócalos con la mano o apúntalos con el rayo y pulsa el gatillo\n" +
                    "<b>Botón menú (mando izq.):</b> volumen y modo sentado",
        },
        new Paso
        {
            titulo = "MÍRATE EN EL ESPEJO",
            texto = "Gira hacia el <color=#FFD25A><b>ESPEJO de tu derecha</b></color>: verás a Baymax repetir tus movimientos. " +
                    "Mueve las manos y la cabeza para comprobarlo.\nCuando quieras, vuelve a esta ventana y pulsa SIGUIENTE.",
        },
        new Paso
        {
            titulo = "¿DÓNDE EMPIEZO?",
            texto = "Sigue las <b>balizas numeradas</b>:  <b>1</b> Microscopios · <b>2</b> Cepario · <b>3</b> Alto riesgo (virus en 3D) · <b>5</b> Química\n" +
                    "Para tu primer recuerdo ve al <color=#FFD25A><b>4 · CENTRO DE EXPERIMENTACIÓN</b></color> y elige el " +
                    "<b>CASO 01</b> en el banco de muestras. ¡Suerte, Baymax!",
        },
    };
}
