using TMPro;
using UnityEngine;

// Introducción narrativa (RF-01) como ventana pequeña paso a paso: quién es Baymax, el objetivo de
// NanoLab VR, cómo se investiga, los controles, el espejo y dónde empezar. Aparece delante de la cara
// al entrar en la sala, como un cristal transparente (se sigue viendo el laboratorio detrás); se avanza
// con el botón X del mando izquierdo o con SIGUIENTE / ATRÁS (tocando los botones o apuntándolos con el
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
    [Tooltip("Tutorial pegado a la cámara: la ventana acompaña a la vista (con un pequeño retardo suave).")]
    public bool pegadaACamara = true;
    [Tooltip("Rapidez con la que la ventana alcanza la vista al girar la cabeza.")]
    public float suavidad = 5f;

    [Header("Cristal")]
    [Tooltip("Color del fondo de la ventana: transparente para ver el laboratorio detrás.")]
    public Color colorCristal = new Color(0.55f, 0.82f, 1f, 0.16f);
    [Tooltip("Contorno oscuro del texto para que se lea sobre cualquier fondo (RNF-03).")]
    public Color contornoTexto = new Color(0f, 0.06f, 0.12f, 0.85f);

    public Paso[] pasos = PasosPorDefecto();

    // Solo se abre sola la primera vez en la sesión (si se recarga la escena no se repite).
    static bool vista;

    int actual;
    bool colocada;
    float ultimoAvance = -1f;

    static readonly Color Activo = new Color(0.37f, 0.83f, 1f), Inactivo = new Color(0.2f, 0.26f, 0.33f);

    public bool Abierta => ventana != null && ventana.activeSelf;

    void Awake()
    {
        // Los textos del tutorial salen siempre del código (la escena guardada puede llevar una versión antigua).
        pasos = PasosPorDefecto();
        if (atras) atras.Pulsado += _ => Ir(actual - 1);
        if (siguiente) siguiente.Pulsado += _ => Continuar();
        if (saltar) saltar.Pulsado += _ => Cerrar();
        if (reabrir) reabrir.Pulsado += _ => Abrir();
        Cristal();
    }

    // SIGUIENTE (o ¡EMPEZAR! en el último paso): también con el botón X.
    void Continuar()
    {
        // X y el rayo del mando izquierdo pueden pulsar SIGUIENTE en el mismo instante: cuenta una vez.
        if (Time.unscaledTime - ultimoAvance < 0.3f) return;
        ultimoAvance = Time.unscaledTime;
        if (actual >= pasos.Length - 1) Cerrar(); else Ir(actual + 1);
    }

    // Fondo de cristal transparente (no negro: es un laboratorio y se ve detrás) con el texto contorneado,
    // y una indicación del botón X junto a los puntos de progreso.
    void Cristal()
    {
        if (ventana == null) return;
        var fondo = ventana.transform.Find("Fondo");
        var panel = fondo != null ? fondo.GetComponent<PanelRedondeado>() : null;
        if (panel != null)
        {
            panel.color = colorCristal;
            panel.colorBorde = new Color(0.55f, 0.9f, 1f, 0.9f);
            panel.Aplicar();
        }
        foreach (var t in new[] { titulo, texto, contador })
        {
            if (t == null) continue;
            t.outlineWidth = 0.18f;   // crea una instancia del material de la fuente solo para estos textos
            t.outlineColor = contornoTexto;
        }
        if (contador != null && ventana.transform.Find("PistaX") == null)
        {
            var x = Instantiate(contador.gameObject, contador.transform.parent);
            x.name = "PistaX";
            var tx = x.GetComponent<TMP_Text>();
            tx.alignment = TextAlignmentOptions.Right;
            tx.rectTransform.sizeDelta = new Vector2(0.2f, tx.rectTransform.sizeDelta.y);
            var p = contador.transform.localPosition;
            x.transform.localPosition = new Vector3(0.21f, -0.145f, p.z);
            tx.text = "<color=#FFD25A><b>X</b></color>: continuar";
        }
    }

    void Start()
    {
        // La ventana grande se sustituyó por el aviso cuadrado (AvisoInicio): aquí solo queda la insignia.
        if (vista || AvisoInicio.Instancia != null) { Mostrar(false); return; }
        actual = 0;
        Mostrar(true);
        Refrescar();
        // El seguimiento de la cabeza tarda un instante en estar listo al arrancar.
        Invoke(nameof(Colocar), 0.6f);
    }

    public void Abrir()
    {
        if (AvisoInicio.Instancia != null) { AvisoInicio.Instancia.MostrarTutorial(); return; }   // reabrir: el tutorial corto
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
        // Botón X (mando izquierdo): pasa al siguiente paso; en el último, empieza.
        if (OVRInput.GetDown(OVRInput.Button.Three) || XEnTeclado())
        {
            Continuar();
            Sonidos.Clic(ventana.transform.position);
        }
        var cab = ManosRig.Cabeza;
        if (cab == null) return;
        if (pegadaACamara) { Seguir(cab); return; }
        // Si el usuario se aleja caminando, la ventana vuelve delante de él.
        if (!colocada || Vector3.Distance(cab.position, ventana.transform.position) > distanciaRecolocar) Colocar();
        // Siempre de cara al usuario (solo gira en horizontal, sin balanceos).
        var d = Vector3.ProjectOnPlane(ventana.transform.position - cab.position, Vector3.up);
        if (d.sqrMagnitude > 1e-4f)
            ventana.transform.rotation = Quaternion.Slerp(ventana.transform.rotation, Quaternion.LookRotation(d.normalized, Vector3.up), Time.deltaTime * 4f);
    }

    // Pancarta ajustada a la cámara: siempre delante de la vista, un poco por debajo de los ojos y de cara
    // al usuario; al girar la cabeza la alcanza con suavidad (sin temblores ni mareo).
    void Seguir(Transform cab)
    {
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) f = ventana.transform.forward;
        f.Normalize();
        var destino = cab.position + f * distancia - Vector3.up * bajoOjos;
        var rot = Quaternion.LookRotation(f, Vector3.up);
        float k = colocada ? 1f - Mathf.Exp(-suavidad * Time.deltaTime) : 1f;
        ventana.transform.position = Vector3.Lerp(ventana.transform.position, destino, k);
        ventana.transform.rotation = Quaternion.Slerp(ventana.transform.rotation, rot, k);
        colocada = true;
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
                    "<b>X:</b> continuar en esta ventana  ·  <b>Botón menú (mando izq.):</b> volumen, modo sentado y checkpoint",
        },
        new Paso
        {
            titulo = "MÍRATE EN EL ESPEJO",
            texto = "Gira hacia el <color=#FFD25A><b>ESPEJO de tu derecha</b></color>: verás a Baymax repetir tus movimientos " +
                    "(esta ventana te acompaña, es de cristal y se ve a través). Mueve las manos y la cabeza para comprobarlo.\n" +
                    "Cuando quieras, pulsa <b>X</b> para seguir.",
        },
        new Paso
        {
            titulo = "¿DÓNDE EMPIEZO?",
            texto = "Sigue las <b>balizas numeradas</b>:  <b>1</b> Microscopios · <b>2</b> Cepario · <b>3</b> Alto riesgo (virus en 3D)\n" +
                    "Para recuperar tu recuerdo ve al <color=#FFD25A><b>4 · CENTRO DE EXPERIMENTACIÓN</b></color>: en el centro está el " +
                    "<b>LABORATORIO DE VACUNAS</b> (CASO 01 · Crea la vacuna). Al terminarlo se guarda un <b>checkpoint</b>.\n" +
                    "Para la sala de alto riesgo pasa antes por la <b>cabina de descontaminación</b> (11 s, quédate quieto). ¡Suerte, Baymax!",
        },
    };

    static bool XEnTeclado()
    {
#if UNITY_EDITOR
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb.xKey.wasPressedThisFrame && !OVRManager.isHmdPresent;
#else
        return Input.GetKeyDown(KeyCode.X) && !OVRManager.isHmdPresent;
#endif
#else
        return false;
#endif
    }
}
