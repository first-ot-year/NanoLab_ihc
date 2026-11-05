using TMPro;
using UnityEngine;

// Aviso cuadrado delante de la vista que sustituye a las pancartas del inicio (la ventana grande de
// introducción y el tótem del tutorial). Cada vez que empieza la partida muestra un tutorial corto de
// tarjetas (quién eres, la misión, cómo moverte, el microscopio, dónde empezar y el menú), y cada vez que
// el usuario se pone el visor aparece un aviso de "casco listo". Todo se avanza con el botón X del mando
// izquierdo (en el Editor sin visor, la tecla X). Lo añade JugadorNanoLab; se construye por código.
public class AvisoInicio : MonoBehaviour
{
    public static AvisoInicio Instancia { get; private set; }

    struct Tarjeta { public string titulo, texto; }

    static readonly Tarjeta[] Bienvenida =
    {
        new Tarjeta { titulo = "BIENVENIDO A NANOLAB VR", texto = "Ajusta el casco hasta que este texto se vea nítido.\nPonte cómodo: puedes jugar de pie o sentado." },
    };

    static readonly Tarjeta[] Tutorial =
    {
        new Tarjeta { titulo = "ERES BAYMAX", texto = "Un pequeño robot asistente de salud. Sus manos son tus manos: míralo en el <b>espejo</b> de la entrada." },
        new Tarjeta { titulo = "TU MISIÓN", texto = "Baymax perdió sus <b>recuerdos científicos</b>. Explora el laboratorio y <b>crea una vacuna</b> para recuperar tu recuerdo." },
        new Tarjeta { titulo = "MOVERTE", texto = "<b>Joystick izquierdo:</b> caminar\n<b>Joystick derecho:</b> girar\n<b>Grip o gatillo:</b> agarrar objetos" },
        new Tarjeta { titulo = "EL MICROSCOPIO", texto = "1. Coge un <b>portaobjetos</b>\n2. Toca una muestra\n3. Déjalo en la <b>platina</b>\n4. Acerca la cara al ocular" },
        new Tarjeta { titulo = "¿DÓNDE EMPIEZO?", texto = "Ve al <b>Centro de Experimentación</b> (puerta de la derecha): en el centro está el <b>LABORATORIO DE VACUNAS</b> (CASO 01)." },
        new Tarjeta { titulo = "MENÚ Y CHECKPOINT", texto = "<b>Botón menú</b> (mando izq.): volumen, modo sentado y volver al checkpoint.\n<size=80%>Contenido educativo simplificado.</size>" },
    };

    [Tooltip("Distancia del aviso a los ojos (m).")]
    public float distancia = 0.65f;
    [Tooltip("Cuánto queda por debajo de la línea de los ojos (m).")]
    public float bajoOjos = 0.06f;
    public float suavidad = 6f;

    const float Lado = 0.34f;   // aviso cuadrado
    static readonly Color Fondo = new Color(0.97f, 0.985f, 1f, 0.94f);
    static readonly Color Tinta = new Color(0.06f, 0.13f, 0.2f);
    static readonly Color Acento = new Color(0.12f, 0.62f, 0.85f);

    GameObject raiz;
    TextMeshPro titulo, texto, contador, boton;
    PanelRedondeado[] puntos;
    Tarjeta[] actuales;
    int indice;
    float ultimo, mostradoEn;
    bool colocado, conTutorial;

    public bool Abierto => raiz != null && raiz.activeSelf;

    void Awake()
    {
        Instancia = this;
        Montar();
        raiz.SetActive(false);
    }

    void OnEnable() => OVRManager.HMDMounted += AlPonerseCasco;
    void OnDisable() => OVRManager.HMDMounted -= AlPonerseCasco;
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    void Start()
    {
        QuitarPancartas();
        // Cada inicio de partida: aviso de casco + tutorial corto.
        Invoke(nameof(MostrarInicio), 0.8f);   // el seguimiento de la cabeza tarda un instante
    }

    void MostrarInicio() => Mostrar(true);

    // Al volver a ponerse el visor: solo el aviso (si el tutorial estaba abierto, sigue donde iba).
    void AlPonerseCasco()
    {
        if (Abierto) { colocado = false; return; }
        Mostrar(false);
    }

    // Se puede reabrir desde la insignia de la introducción.
    public void MostrarTutorial()
    {
        actuales = Tutorial;
        conTutorial = false;
        Ir(0);
    }

    void Mostrar(bool tutorial)
    {
        conTutorial = tutorial;
        actuales = Bienvenida;
        Ir(0);
    }

    void Ir(int i)
    {
        indice = i;
        if (!raiz.activeSelf) colocado = false;   // recién abierto: aparece directamente delante
        raiz.SetActive(true);
        mostradoEn = Time.unscaledTime;
        var t = actuales[i];
        titulo.text = t.titulo;
        texto.text = t.texto;
        bool tut = actuales == Tutorial;
        int n = tut ? Tutorial.Length : 0;
        contador.text = tut ? $"{i + 1} / {n}" : "";
        bool ultimaTarjeta = i == actuales.Length - 1 && !(actuales == Bienvenida && conTutorial);
        boton.text = $"<b>X</b>   {(ultimaTarjeta ? "Empezar" : "Continuar")}";
        for (int k = 0; k < puntos.Length; k++)
        {
            puntos[k].gameObject.SetActive(tut);
            puntos[k].color = k == i ? Acento : new Color(0.75f, 0.82f, 0.88f, 1f);
            puntos[k].Aplicar();
        }
        Sonidos.Clic(raiz.transform.position);
    }

    void Continuar()
    {
        if (Time.unscaledTime - ultimo < 0.3f || Time.unscaledTime - mostradoEn < 0.25f) return;
        ultimo = Time.unscaledTime;
        if (indice < actuales.Length - 1) { Ir(indice + 1); return; }
        if (actuales == Bienvenida && conTutorial) { conTutorial = false; actuales = Tutorial; Ir(0); return; }
        raiz.SetActive(false);
        Sonidos.Etapa(raiz.transform.position);
    }

    void Update()
    {
        if (!Abierto) return;
        if (OVRInput.GetDown(OVRInput.Button.Three) || XEnTeclado()) Continuar();
    }

    void LateUpdate()
    {
        if (!Abierto) return;
        var cab = ManosRig.Cabeza;
        if (cab == null) return;
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) f = raiz.transform.forward;
        f.Normalize();
        var destino = cab.position + f * distancia - Vector3.up * bajoOjos;
        var rot = Quaternion.LookRotation(f, Vector3.up);
        float k = colocado ? 1f - Mathf.Exp(-suavidad * Time.unscaledDeltaTime) : 1f;
        raiz.transform.position = Vector3.Lerp(raiz.transform.position, destino, k);
        raiz.transform.rotation = Quaternion.Slerp(raiz.transform.rotation, rot, k);
        colocado = true;
    }

    // Las pancartas antiguas del inicio: la ventana de introducción y el tótem "Tutorial" del recibidor.
    static void QuitarPancartas()
    {
        foreach (var t in FindObjectsOfType<Transform>(true))
            if (t.name == "Tutorial" && t.Find("Circulo0") != null) t.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ construcción

    void Montar()
    {
        raiz = new GameObject("AvisoInicio");
        Material mat = null;
        var ejemplo = FindObjectOfType<PanelRedondeado>(true);
        if (ejemplo != null) mat = ejemplo.GetComponent<MeshRenderer>().sharedMaterial;
        if (mat == null) { var sh = Shader.Find("Interaction/RoundedBoxUnlit"); if (sh != null) mat = new Material(sh); }
        TMP_FontAsset fuente = null;
        foreach (var tx in FindObjectsOfType<TextMeshPro>(true)) if (tx.font != null) { fuente = tx.font; break; }

        float h = Lado / 2f;
        Quad("Fondo", new Vector3(0f, 0f, 0f), Lado, Lado, 0.03f, Fondo, new Color(0.55f, 0.85f, 1f, 1f), 0.004f, mat, 0);
        Quad("Franja", new Vector3(0f, h - 0.012f, -0.001f), Lado - 0.06f, 0.006f, 0.003f, Acento, Color.clear, 0f, mat, 1);

        // Cara de Baymax: óvalo blanco con dos ojos y la línea que los une.
        Quad("Cara", new Vector3(0f, h - 0.058f, -0.001f), 0.07f, 0.045f, 0.0225f, Color.white, new Color(0.7f, 0.78f, 0.85f, 1f), 0.002f, mat, 1);
        foreach (float x in new[] { -0.016f, 0.016f })
            Quad("Ojo", new Vector3(x, h - 0.058f, -0.002f), 0.011f, 0.011f, 0.0055f, Tinta, Color.clear, 0f, mat, 2);
        Quad("Linea", new Vector3(0f, h - 0.058f, -0.002f), 0.03f, 0.002f, 0f, Tinta, Color.clear, 0f, mat, 2);

        titulo = Texto("Titulo", new Vector3(0f, h - 0.11f, -0.003f), 0.26f, true, Acento, fuente, 0.03f);
        texto = Texto("Texto", new Vector3(0f, -0.005f, -0.003f), 0.19f, false, Tinta, fuente, 0.15f);
        texto.alignment = TextAlignmentOptions.Center;

        var b = Quad("Boton", new Vector3(0f, -h + 0.05f, -0.001f), 0.17f, 0.042f, 0.021f, Tinta, Color.clear, 0f, mat, 1);
        boton = Texto("TextoBoton", b.transform.localPosition + new Vector3(0f, 0f, -0.002f), 0.2f, false, Color.white, fuente, 0.04f);
        contador = Texto("Contador", new Vector3(h - 0.045f, -h + 0.05f, -0.003f), 0.14f, false, new Color(0.4f, 0.5f, 0.6f), fuente, 0.03f);
        contador.rectTransform.sizeDelta = new Vector2(0.06f, 0.03f);

        puntos = new PanelRedondeado[Tutorial.Length];
        for (int i = 0; i < puntos.Length; i++)
            puntos[i] = Quad("Punto" + i, new Vector3((i - (puntos.Length - 1) / 2f) * 0.018f, -h + 0.016f, -0.001f), 0.009f, 0.009f, 0.0045f, Acento, Color.clear, 0f, mat, 1);
    }

    PanelRedondeado Quad(string n, Vector3 pos, float ancho, float alto, float radio, Color c, Color borde, float grosor, Material mat, int orden)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = n;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(raiz.transform, false);
        q.transform.localPosition = pos;
        var mr = q.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = orden;
        if (mat != null) mr.sharedMaterial = mat;
        var p = q.AddComponent<PanelRedondeado>();
        p.ancho = ancho; p.alto = alto; p.radio = radio; p.color = c; p.colorBorde = borde; p.borde = grosor;
        p.Aplicar();
        return p;
    }

    TextMeshPro Texto(string n, Vector3 pos, float tam, bool negrita, Color color, TMP_FontAsset fuente, float alto)
    {
        var go = new GameObject(n);
        go.transform.SetParent(raiz.transform, false);
        go.transform.localPosition = pos;
        var t = go.AddComponent<TextMeshPro>();
        if (fuente != null) t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.rectTransform.sizeDelta = new Vector2(Lado - 0.04f, alto);
        if (negrita) t.fontStyle = FontStyles.Bold;
        t.sortingOrder = 5;
        return t;
    }

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
