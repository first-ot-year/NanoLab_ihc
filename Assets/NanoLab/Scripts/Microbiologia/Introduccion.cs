using System.Collections;
using TMPro;
using UnityEngine;

// Introducción y orientación narrativa (RF-01) y selección de la primera muestra (salida de E2).
//  - Diálogo con subtítulos: el estudiante descubre que es Baby Baymax y que perdió su Archivo
//    Científico. Imagen estática de Baymax + texto, sin voz ni animación corporal (alcance del MVP).
//  - El texto aparece letra a letra; A / X o gatillo lo completa y, si ya está completo, avanza.
//    B / Y vuelve atrás (acciones reversibles, RNF-04).
//  - El panel está en el mundo, delante del jugador, y solo se recoloca si sale de la vista (lazy
//    follow). Mientras dura, la locomoción está en pausa: la introducción se vive quieto (RNF-01).
//  - Un paso de orientación enciende balizas sobre las áreas del laboratorio; siguen visibles hasta
//    que se elige la primera muestra.
//  - Al tomar la primera muestra con un portaobjetos aparece una tarjeta con su identificación.
public class Introduccion : MonoBehaviour
{
    [System.Serializable]
    public class Paso
    {
        public string titulo;
        [TextArea(2, 6)] public string texto;
        [Tooltip("Enciende las balizas de las áreas del laboratorio durante este paso.")]
        public bool mostrarAreas;
    }

    [Header("Diálogo")]
    public Paso[] pasos;
    public Transform dialogo;                 // raíz del panel (se recoloca delante del jugador)
    public TMP_Text titulo, texto;
    public TMP_Text accionSiguiente;          // "Continuar" / "Empezar"
    public GameObject chipAtras;
    public PanelRedondeado[] puntos;          // indicador de progreso
    public Transform figura;                  // Baby Baymax (modelo estático)
    [Tooltip("Caracteres por segundo del texto.")]
    public float velocidadTexto = 55f;

    [Header("Orientación")]
    public GameObject areas;

    [Header("Muestra seleccionada")]
    public Transform tarjeta;
    public TMP_Text tarjetaNombre, tarjetaDetalle, tarjetaTipo;
    public PanelRedondeado tarjetaFranja, tarjetaChip;
    public float duracionTarjeta = 8f;

    [Header("Colocación")]
    public float distancia = 1.35f;
    public float bajoOjos = 0.12f;
    public float anguloMax = 32f;

    public static bool EnCurso { get; private set; }
    public static bool MuestraElegida { get; private set; }
    // Primera muestra tomada con un portaobjetos (salida de la entrega E2).
    public static event System.Action<MuestraData> MuestraSeleccionada;

    static readonly Color Activo = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color Hecho = new Color(0.35f, 0.85f, 1f, 0.45f);
    static readonly Color Pendiente = new Color(1f, 1f, 1f, 0.18f);

    int actual = -1;
    float bloqueo, inicioTexto;
    bool escribiendo;
    JugadorNanoLab jugador;
    readonly Seguidor seguirDialogo = new Seguidor(), seguirTarjeta = new Seguidor();
    Coroutine animTarjeta;

    void OnEnable() => FuenteMuestra.Usada += AlUsarFuente;

    void OnDisable()
    {
        FuenteMuestra.Usada -= AlUsarFuente;
        if (EnCurso) Terminar(false);
    }

    void Start()
    {
        MuestraElegida = false;
        if (dialogo != null) dialogo.gameObject.SetActive(false);
        if (tarjeta != null) tarjeta.gameObject.SetActive(false);
        if (areas != null) areas.SetActive(false);
        jugador = FindObjectOfType<JugadorNanoLab>();
        // Margen para que el visor tenga posición antes de colocar el panel.
        Invoke(nameof(Empezar), 1.2f);
    }

    void Empezar()
    {
        if (pasos == null || pasos.Length == 0 || dialogo == null) { Terminar(true); return; }
        EnCurso = true;
        if (jugador != null) jugador.PausarLocomocion(true);
        dialogo.gameObject.SetActive(true);
        seguirDialogo.Colocar(dialogo, distancia, bajoOjos, anguloMax, true);
        StartCoroutine(Aparecer(dialogo, 0.3f));
        Mostrar(0);
    }

    void Update()
    {
        if (figura != null && figura.gameObject.activeInHierarchy)
            figura.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(Time.time * 0.6f) * 25f, 0f);
        if (tarjeta != null && tarjeta.gameObject.activeSelf) seguirTarjeta.Colocar(tarjeta, 1.1f, 0.25f, anguloMax, false);
        if (!EnCurso) return;

        seguirDialogo.Colocar(dialogo, distancia, bajoOjos, anguloMax, false);
        if (escribiendo)
        {
            int n = Mathf.FloorToInt((Time.time - inicioTexto) * velocidadTexto);
            texto.maxVisibleCharacters = n;
            if (n >= texto.textInfo.characterCount) escribiendo = false;
        }
        if (Time.time < bloqueo) return;   // un toque no salta dos pasos
        if (Siguiente())
        {
            if (escribiendo) { CompletarTexto(); bloqueo = Time.time + 0.2f; }
            else if (actual + 1 < pasos.Length) Mostrar(actual + 1);
            else Terminar(true);
        }
        else if (Anterior() && actual > 0) Mostrar(actual - 1);
    }

    static bool Siguiente()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three)) return true;
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)) return true;
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)) return true;
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);   // pruebas en el editor
    }

    static bool Anterior() =>
        OVRInput.GetDown(OVRInput.Button.Two) || OVRInput.GetDown(OVRInput.Button.Four) || Input.GetKeyDown(KeyCode.Backspace);

    void Mostrar(int i, bool conSonido = true)
    {
        actual = i;
        bloqueo = Time.time + 0.3f;
        var p = pasos[i];
        titulo.text = p.titulo;
        texto.text = p.texto;
        texto.maxVisibleCharacters = 0;
        texto.ForceMeshUpdate();
        inicioTexto = Time.time;
        escribiendo = true;
        bool ultimo = i == pasos.Length - 1;
        if (accionSiguiente != null) accionSiguiente.text = ultimo ? "Empezar" : "Continuar";
        if (chipAtras != null) chipAtras.SetActive(i > 0);
        for (int k = 0; k < puntos.Length; k++)
        {
            if (puntos[k] == null) continue;
            puntos[k].ancho = k == i ? 0.045f : 0.014f;
            puntos[k].color = k == i ? Activo : k < i ? Hecho : Pendiente;
            puntos[k].Aplicar();
        }
        if (areas != null) areas.SetActive(p.mostrarAreas || areas.activeSelf && i > IndiceAreas());
        if (conSonido && ManosRig.Cabeza != null) Sonidos.Clic(ManosRig.Cabeza.position);
    }

    // Para las capturas del editor: pinta un paso completo, sin sonido ni animación.
    public void Previsualizar(int i)
    {
        Mostrar(i, false);
        CompletarTexto();
    }

    int IndiceAreas()
    {
        for (int k = 0; k < pasos.Length; k++) if (pasos[k].mostrarAreas) return k;
        return int.MaxValue;
    }

    void CompletarTexto()
    {
        escribiendo = false;
        texto.maxVisibleCharacters = 99999;
    }

    void Terminar(bool conAviso)
    {
        EnCurso = false;
        CancelInvoke();
        if (jugador != null) jugador.PausarLocomocion(false);
        if (!conAviso) { if (dialogo != null) dialogo.gameObject.SetActive(false); return; }
        if (dialogo != null && dialogo.gameObject.activeSelf) StartCoroutine(Desaparecer(dialogo, 0.25f));
        // Las balizas siguen encendidas hasta elegir la primera muestra.
        if (areas != null && !MuestraElegida) areas.SetActive(true);
        if (ManosRig.Cabeza != null) Sonidos.Etapa(ManosRig.Cabeza.position);
    }

    // ------------------------------------------------------------------ selección de muestra

    void AlUsarFuente(FuenteMuestra f, Portaobjetos p)
    {
        if (MuestraElegida || f.muestra == null || f.esDesinfectante || f.reactivo > 0) return;
        MuestraElegida = true;
        var m = f.muestra;
        bool caso = !string.IsNullOrEmpty(f.etiqueta);
        Debug.Log($"[Intro] Primera muestra seleccionada: {(caso ? f.etiqueta : m.nombre)} ({Time.timeSinceLevelLoad:F0} s desde el inicio).");
        if (areas != null) areas.SetActive(false);
        Sonidos.Logro(p.transform.position);
        ManosRig.Vibrar(p.transform.position, 0.5f, 0.12f);
        MostrarTarjeta(m, caso ? f.etiqueta : null);
        MuestraSeleccionada?.Invoke(m);
    }

    void MostrarTarjeta(MuestraData m, string caso)
    {
        if (tarjeta == null) return;
        RellenarTarjeta(m, caso);
        if (animTarjeta != null) StopCoroutine(animTarjeta);
        animTarjeta = StartCoroutine(Tarjeta());
    }

    public void RellenarTarjeta(MuestraData m, string caso)
    {
        // En un caso del reto la identidad no se revela (se descubre investigando).
        tarjetaNombre.text = caso ?? m.nombre;
        tarjetaDetalle.text = caso != null
            ? "Muestra de origen desconocido.\n<color=#8FD8FF>Siguiente:</color> colócala en la platina de un microscopio."
            : $"<i>{m.nombreCientifico}</i>  ·  {m.riesgo}\n<color=#8FD8FF>Siguiente:</color> colócala en la platina de un microscopio.";
        var c = caso != null ? new Color(1f, 0.82f, 0.35f) : m.color;
        tarjetaTipo.text = caso != null ? "CASO" : m.tipo == TipoMuestra.Virus ? "VIRUS" : "BACTERIA";
        tarjetaFranja.color = c; tarjetaFranja.Aplicar();
        tarjetaChip.colorBorde = c; tarjetaChip.Aplicar();
        tarjetaTipo.color = c;
    }

    IEnumerator Tarjeta()
    {
        tarjeta.gameObject.SetActive(true);
        seguirTarjeta.Colocar(tarjeta, 1.1f, 0.25f, anguloMax, true);
        yield return Aparecer(tarjeta, 0.25f);
        yield return new WaitForSeconds(duracionTarjeta);
        yield return Desaparecer(tarjeta, 0.25f);
    }

    // ------------------------------------------------------------------ animación

    static IEnumerator Aparecer(Transform t, float dur)
    {
        for (float x = 0f; x < 1f; x += Time.deltaTime / dur)
        {
            float e = 1f - Mathf.Pow(1f - x, 3f);   // ease-out
            t.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, e);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    static IEnumerator Desaparecer(Transform t, float dur)
    {
        for (float x = 0f; x < 1f; x += Time.deltaTime / dur)
        {
            t.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, x * x);
            yield return null;
        }
        t.gameObject.SetActive(false);
        t.localScale = Vector3.one;
    }

    // Lazy follow: el panel se queda quieto mientras se vea; si sale de la vista, vuelve suavemente delante.
    class Seguidor
    {
        Vector3 destino;
        bool moviendo;

        public void Colocar(Transform t, float distancia, float bajoOjos, float anguloMax, bool forzar)
        {
            var cab = ManosRig.Cabeza;
            if (cab == null || t == null) return;
            var frente = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
            if (frente.sqrMagnitude < 1e-4f) return;
            frente.Normalize();
            var hacia = Vector3.ProjectOnPlane(t.position - cab.position, Vector3.up);
            if (forzar || Vector3.Angle(frente, hacia) > anguloMax || Mathf.Abs(hacia.magnitude - distancia) > 0.6f)
            {
                destino = cab.position + frente * distancia + Vector3.down * bajoOjos;
                moviendo = true;
                if (forzar) t.position = destino;
            }
            if (moviendo)
            {
                t.position = Vector3.Lerp(t.position, destino, Time.deltaTime * 4f);
                if ((t.position - destino).sqrMagnitude < 0.0004f) moviendo = false;
            }
            var d = Vector3.ProjectOnPlane(t.position - cab.position, Vector3.up);
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d);
        }
    }
}
