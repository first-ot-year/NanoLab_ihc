using TMPro;
using UnityEngine;
using K = KitVacuna;

// Check de la tinción de Gram, PASO A PASO, en la vista del jugador (a la derecha), con el mismo estilo que el aviso de
// RECUERDO RECUPERADO (RecuerdoAnimado). Revisión de la demo: "no quiero una tabla, quiero un check de 1/4, 2/4, 3/4 y
// 4/4 en el apartado derecho de la vista, como el que dice recuerdo recuperado".
// Cada toque a un gotero (Portaobjetos.AplicarReactivo) lo muestra unos segundos:
//   correcto  > círculo verde con un check, "TINCIÓN DE GRAM · n/4", el reactivo y qué gotero sigue, y 4 ranuras
//               (las hechas, en verde).
//   no válido > círculo ámbar (falta un paso) o rojo (error) con "!" y la indicación.
// Entra con un rebote, sigue a la cabeza con suavidad y se recoge hacia la derecha. Si llega otro toque mientras se ve,
// se actualiza en el mismo sitio (no se apilan). Va un poco más arriba que el de los recuerdos, para no taparlo.
public class ProgresoTincion : MonoBehaviour
{
    public static readonly Vector3 Desplazamiento = new Vector3(0.23f, 0.03f, 0.65f);
    const float Ancho = 0.27f, Alto = 0.095f, Duracion = 3.2f, TRecoge = 2.7f;
    static readonly Color Verde = new Color(0.25f, 0.85f, 0.45f);
    static readonly Color Ambar = new Color(1f, 0.72f, 0.2f);
    static readonly Color RojoError = new Color(0.95f, 0.35f, 0.3f);
    static readonly Color Apagada = new Color(0.25f, 0.3f, 0.36f);
    static readonly Vector3 PosIcono = new Vector3(-0.105f, 0.004f, -0.004f);

    public static ProgresoTincion Actual { get; private set; }
    public int Pasos { get; private set; }
    // Dónde está respecto a la cabeza (para las pruebas: x > 0 = a la derecha de la vista).
    public Vector3 PosicionEnLaVista => cabeza != null ? cabeza.InverseTransformPoint(transform.position) : Vector3.zero;

    Transform cabeza, contenido, icono, check;
    Renderer circulo;
    Renderer[] ranuras;
    TextMeshPro cabecera, detalle, exclamacion;
    float t;

    // Lo llama Portaobjetos al tocar un gotero. false: no hay cabeza del jugador (que use el Aviso de siempre).
    public static bool Mostrar(Portaobjetos p, string texto, Aviso.Tipo tipo)
    {
        var cab = ManosRig.Cabeza;
        if (cab == null || p == null) return false;
        if (Actual == null)
        {
            Actual = new GameObject("CheckTincion").AddComponent<ProgresoTincion>();
            Actual.Construir(cab);
        }
        Actual.Rellenar(p, texto, tipo);
        return true;
    }

    void Construir(Transform cab)
    {
        cabeza = cab;
        Colocar(1f);
        contenido = K.Grupo(transform, "Contenido", Vector3.zero);
        K.Panel(contenido, "Fondo", new Vector3(0f, 0f, 0.003f), Ancho, Alto, new Color(0.05f, 0.1f, 0.18f, 0.97f), Verde, 0.002f, 0.024f);

        // Icono: círculo con un check dibujado con dos trazos (no depende de que la fuente tenga el símbolo).
        icono = K.Grupo(contenido, "Icono", PosIcono);
        circulo = K.Pieza(icono, "Circulo", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.04f, 0.04f, 0.006f), K.Plano(Verde));
        check = K.Grupo(icono, "Check", new Vector3(0f, 0f, -0.004f));
        K.Caja(check, "TrazoCorto", new Vector3(-0.0075f, -0.002f, 0f), new Vector3(0.011f, 0.0042f, 0.001f), K.Plano(Color.white)).transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
        K.Caja(check, "TrazoLargo", new Vector3(0.003f, 0.001f, 0f), new Vector3(0.022f, 0.0042f, 0.001f), K.Plano(Color.white)).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        exclamacion = K.Texto(icono, "<b>!</b>", new Vector3(0f, 0f, -0.004f), 0.2f, Color.white, 0.04f, 0.04f, TextAlignmentOptions.Center);

        cabecera = K.Texto(contenido, "", new Vector3(0.025f, 0.024f, -0.002f), 0.1f, Verde, 0.2f, 0.028f, TextAlignmentOptions.Left);
        K.AutoTam(cabecera, 0.06f);
        detalle = K.Texto(contenido, "", new Vector3(0.025f, -0.006f, -0.002f), 0.075f, Color.white, 0.2f, 0.034f, TextAlignmentOptions.Left);
        K.AutoTam(detalle, 0.045f);

        ranuras = new Renderer[4];
        for (int i = 0; i < 4; i++)
            ranuras[i] = K.Esfera(contenido, "Ranura", new Vector3(-0.065f + i * 0.022f + 0.09f, -0.034f, -0.004f), 0.012f, K.Plano(Apagada));

        foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var tx in GetComponentsInChildren<TextMeshPro>(true)) tx.sortingOrder = AdaptadorEscena.OrdenTexto + 2;
        contenido.localScale = Vector3.one * 0.01f;
    }

    void Rellenar(Portaobjetos p, string texto, Aviso.Tipo tipo)
    {
        bool ok = tipo == Aviso.Tipo.Ok;
        Pasos = p.muestra != null ? Mathf.Clamp(p.PasosTincion, 0, 4) : 0;
        var color = ok ? Verde : tipo == Aviso.Tipo.Error ? RojoError : Ambar;

        K.Pintar(circulo, color);
        check.gameObject.SetActive(ok);
        exclamacion.gameObject.SetActive(!ok);
        var marco = contenido.Find("Fondo") != null ? contenido.Find("Fondo").GetComponent<PanelRedondeado>() : null;
        if (marco != null) { marco.colorBorde = color; marco.Aplicar(); }

        string hex = "#" + ColorUtility.ToHtmlStringRGB(color);
        if (ok)
        {
            cabecera.text = $"<b>TINCIÓN DE GRAM · <color={hex}>{Pasos}/4</color></b>";
            string reactivo = Pasos >= 1 ? Portaobjetos.Reactivos[Pasos].ToUpperInvariant() : "";
            detalle.text = Pasos >= 4 ? $"{reactivo} listo · <color=#39FF88>¡Completa!</color> Al MICROSCOPIO"
                                      : $"{reactivo} listo · sigue con el gotero {Pasos + 1}";
        }
        else
        {
            cabecera.text = $"<b><color={hex}>TINCIÓN DE GRAM · {Pasos}/4</color></b>";
            // Solo la primera línea: el check es pequeño (el resto lo cuentan las pistas de la estación).
            int salto = texto != null ? texto.IndexOf('\n') : -1;
            detalle.text = salto > 0 ? texto.Substring(0, salto) : texto ?? "";
        }
        for (int i = 0; i < ranuras.Length; i++)
            K.Pintar(ranuras[i], i < Pasos && !p.TincionErronea ? Verde : i < Pasos ? RojoError : Apagada);

        // Reinicia la animación (si ya se veía, da un pequeño salto en vez de volver a entrar desde cero).
        t = t > 0.4f && t < TRecoge ? 0.25f : 0f;
    }

    // Rebote al aparecer (pasa un poco de 1 y vuelve).
    static float Rebote(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // Sigue a la cabeza: 'suavidad' 1 = de golpe; menos = se queda un poco atrás (sin tirones al girar).
    void Colocar(float suavidad)
    {
        if (cabeza == null) return;
        var objetivo = cabeza.TransformPoint(Desplazamiento);
        transform.position = Vector3.Lerp(transform.position, objetivo, suavidad);
        var mira = transform.position - cabeza.position;
        if (mira.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(mira, cabeza.up);
    }

    void LateUpdate()
    {
        t += Time.deltaTime;
        if (cabeza == null) cabeza = ManosRig.Cabeza;
        Colocar(1f - Mathf.Exp(-10f * Time.deltaTime));

        float sale = t > TRecoge ? Mathf.SmoothStep(0f, 1f, (t - TRecoge) / (Duracion - TRecoge)) : 0f;
        contenido.localScale = Vector3.one * Mathf.Max(0.01f, Rebote(t / 0.35f) * (1f - sale));
        contenido.localPosition = new Vector3(0.08f * sale, 0f, 0f);
        // El icono late al llegar.
        icono.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(Mathf.Clamp01((t - 0.15f) / 0.4f) * Mathf.PI));
        if (t > Duracion) Destroy(gameObject);
    }

    void OnDestroy() { if (Actual == this) Actual = null; }
}
