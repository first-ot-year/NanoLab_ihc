using System.Collections;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// Brazalete de Baymax: una pantallita en la muñeca izquierda, como un reloj (interfaz diegética, por la presencia).
// Sustituye a la tarjeta de objetivo que perseguía la vista y a los avisos que salían delante de la cara:
//  - Va pegado al mando izquierdo (se mueve con la mano, nunca con la cabeza).
//  - En reposo está atenuado y solo dice la misión y el paso. Al girar la muñeca hacia la cara "se despierta"
//    (como un reloj inteligente) y enseña el objetivo, el progreso y los recuerdos.
//  - Las novedades (paso nuevo, misión completa, pista) no se imponen: vibra el mando izquierdo, late el LED y
//    suena en la muñeca; el texto espera en la pantalla hasta que se mira.
//  - Y (mando izquierdo) lo llama: vibra y se queda encendido unos segundos (con el microscopio, Y es "menos aumento").
// Lo añade JugadorNanoLab; se construye por código. No tiene colliders: no estorba al agarrar.
// Revisión del profesor ("no tiene sentido lo que tiene en la mano"): la pantalla de 15 × 9,5 cm iba sobre el mando,
// dentro de la mano, y parecía un móvil. Ahora es un RELOJ de verdad: esfera pequeña (5,5 × 4,2 cm) y correa en la
// MUÑECA, detrás de la mano. El texto no va en la esfera (no se leería): al girar la muñeca hacia la cara, sale un
// holograma encima del reloj que mira a los ojos, crece al mirarlo y se recoge al bajar la mano.
public class BrazaleteBaymax : MonoBehaviour
{
    public static BrazaleteBaymax Instancia { get; private set; }

    // Pose sobre el mando izquierdo (local al leftHandAnchor): encima del dorso, hacia la muñeca, con la pantalla
    // mirando hacia arriba y un poco hacia el usuario. El texto se lee "hacia la mano", como la esfera de un reloj.
    // Muñeca izquierda con el mando de Quest en la mano: ~11 cm detrás del agarre y algo por debajo; la esfera mira
    // hacia el dorso (arriba y hacia fuera). Si en el visor queda desplazado, se ajusta aquí.
    static readonly Vector3 PosLocal = new Vector3(-0.012f, -0.012f, -0.115f);
    static readonly Vector3 NormalLocal = new Vector3(-0.45f, 0.88f, -0.15f);
    const float AnchoReloj = 0.055f, AltoReloj = 0.042f;
    const float Ancho = 0.15f, Alto = 0.095f;   // holograma: a ~0,35 m de los ojos se lee como un cartel a 0,8 m
    const float AlturaHolograma = 0.085f;       // encima de la esfera
    const float DistanciaLectura = 0.65f;
    const float SegundosLeido = 4f;

    static readonly Color Pantalla = new Color(0.03f, 0.08f, 0.13f, 0.97f);
    static readonly Color PantallaApagada = new Color(0.02f, 0.035f, 0.05f, 0.97f);
    static readonly Color Cian = new Color(0.4f, 0.88f, 1f);
    static readonly Color Amarillo = new Color(1f, 0.82f, 0.35f);
    static readonly Color Verde = new Color(0.35f, 1f, 0.55f);

    Transform mano, raiz, pantallaT, holograma;
    TextMeshPro mini;
    PanelRedondeado pantalla, barraFondo, barra;
    TextMeshPro cabecera, cuerpo, pie;
    Renderer led;

    string cabTxt = "NANOLAB";
    string objTxt = "Ve al CENTRO DE EXPERIMENTACIÓN y sigue la LUZ GUÍA de Baymax.";
    string corto = "";
    int paso, total;
    string pendiente;
    Aviso.Tipo tipoPendiente;
    bool despierto;
    float brillo, tLeido, forzarHasta, latirHasta;

    public bool Despierto => despierto;
    public bool HayPendiente => pendiente != null;
    public string Pendiente => pendiente;
    public string Objetivo_ => objTxt;
    public bool Montado => raiz != null;

    void Awake() => Instancia = this;
    void OnDestroy() { if (Instancia == this) Instancia = null; }
    void OnEnable() => RecuerdosBaymax.Cambiado += Refrescar;
    void OnDisable() => RecuerdosBaymax.Cambiado -= Refrescar;

    // ------------------------------------------------------------------ API

    // Objetivo actual: 'cabecera' (misión y paso), 'texto' (una frase con la acción) y el progreso del recorrido.
    public void Objetivo(string cabeceraTxt, string texto, int pasoN, int totalN)
    {
        cabTxt = cabeceraTxt ?? "";
        objTxt = texto ?? "";
        paso = pasoN;
        total = totalN;
        corto = totalN > 0 ? $"{Abreviar(cabTxt)} · {pasoN}/{totalN}" : Abreviar(cabTxt);
        Refrescar();
    }

    // Novedad: vibra, late el LED y suena. Con texto, queda pendiente en la pantalla hasta que se mira.
    public void Notificar(string texto, Aviso.Tipo tipo)
    {
        if (!string.IsNullOrEmpty(texto)) { pendiente = Breve(texto); tipoPendiente = tipo; tLeido = 0f; }
        latirHasta = Time.time + (string.IsNullOrEmpty(texto) ? 2.5f : 30f);
        StopCoroutine(nameof(Vibrar));
        StartCoroutine(nameof(Vibrar), string.IsNullOrEmpty(texto) ? 1 : 2);
        var p = raiz != null ? raiz.position : (ManosRig.ManoIzq != null ? ManosRig.ManoIzq.position : transform.position);
        if (tipo == Aviso.Tipo.Ok && !string.IsNullOrEmpty(texto)) Sonidos.Logro(p); else Sonidos.Pista(p);
        Refrescar();
    }

    // Y: "mira tu muñeca". Vibra y la pantalla se queda encendida unos segundos.
    public void Llamar()
    {
        forzarHasta = Time.time + 5f;
        latirHasta = Time.time + 3f;
        StopCoroutine(nameof(Vibrar));
        StartCoroutine(nameof(Vibrar), 1);
        if (raiz != null) Sonidos.Clic(raiz.position);
    }

    // ------------------------------------------------------------------ cada frame

    void Update()
    {
        if (raiz == null)
        {
            var m = ManosRig.ManoIzq;
            if (m == null) return;
            Montar(m);
        }
        bool micro = MicroscopioOptico.AlgunoActivo;
        if (raiz.gameObject.activeSelf == micro) raiz.gameObject.SetActive(!micro);   // con la cara en el ocular no se ve
        if (micro) { if (holograma != null && holograma.gameObject.activeSelf) holograma.gameObject.SetActive(false); return; }

        bool tutorial = AvisoInicio.Instancia != null && AvisoInicio.Instancia.Abierto;   // ahí Y es "atrás"
        if (!tutorial && OVRInput.GetDown(OVRInput.Button.Four)) Llamar();

        // ¿Mira la muñeca? La pantalla se lee desde su -z: su normal apunta a la cara y está cerca.
        var cab = ManosRig.Cabeza;
        bool mirando = false;
        if (cab != null)
        {
            var hacia = cab.position - raiz.position;
            float d = hacia.magnitude;
            float dot = d > 1e-3f ? Vector3.Dot(-raiz.forward, hacia / d) : 0f;
            mirando = d < DistanciaLectura && dot > (despierto ? 0.4f : 0.55f);   // histéresis
        }
        bool antes = despierto;
        despierto = mirando || Time.time < forzarHasta;
        if (despierto != antes) Refrescar();

        // Lo pendiente se da por leído tras unos segundos con la muñeca a la vista.
        if (despierto && mirando && pendiente != null)
        {
            tLeido += Time.deltaTime;
            if (tLeido > SegundosLeido) { pendiente = null; latirHasta = 0f; Refrescar(); }
        }

        brillo = Mathf.MoveTowards(brillo, despierto ? 1f : 0f, Time.deltaTime / 0.2f);
        AplicarBrillo();
        ColocarHolograma(cab);
    }

    // El holograma flota encima del reloj, mirando a los ojos; crece al despertar y se recoge al dormir.
    void ColocarHolograma(Transform cab)
    {
        if (holograma == null) return;
        float e = Mathf.SmoothStep(0f, 1f, brillo);
        bool ver = e > 0.02f;
        if (holograma.gameObject.activeSelf != ver) holograma.gameObject.SetActive(ver);
        if (!ver) return;
        var p = raiz.position + Vector3.up * AlturaHolograma * e;
        if (cab != null) p += (cab.position - raiz.position).normalized * 0.03f;
        holograma.position = p;
        if (cab != null)
        {
            var d = p - cab.position;
            if (d.sqrMagnitude > 1e-4f) holograma.rotation = Quaternion.LookRotation(d, Vector3.up);
        }
        holograma.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, e);
    }

    void AplicarBrillo()
    {
        var c = Color.Lerp(PantallaApagada, Pantalla, brillo);
        if (pantalla.color != c) { pantalla.color = c; pantalla.Aplicar(); }
        float a = Mathf.Lerp(0.55f, 1f, brillo);
        cabecera.alpha = a;
        cuerpo.alpha = brillo;
        pie.alpha = brillo;
        barra.gameObject.SetActive(brillo > 0.05f && total > 0);
        barraFondo.gameObject.SetActive(brillo > 0.05f && total > 0);
        // LED: late mientras haya novedad; si no, un punto tenue.
        bool late = Time.time < latirHasta || pendiente != null;
        float pulso = late ? 0.5f + 0.5f * Mathf.Sin(Time.time * 7f) : 0.25f;
        var col = pendiente != null && tipoPendiente == Aviso.Tipo.Ok ? Verde : late ? Amarillo : Cian;
        K.Pintar(led, Color.Lerp(new Color(0.05f, 0.08f, 0.1f), col, pulso));
    }

    void Refrescar()
    {
        if (cabecera == null) return;
        int n = RecuerdosBaymax.Cantidad;
        if (mini != null) mini.text = pendiente != null ? "<color=#FFD25A>!</color>" : total > 0 ? $"{paso}/{total}" : "";
        if (!despierto)
        {
            cabecera.text = string.IsNullOrEmpty(corto) ? "NANOLAB" : corto;
            cuerpo.text = "";
            pie.text = "";
            return;
        }
        cabecera.text = pendiente != null ? $"<color=#FFD25A>NUEVO</color>  ·  {cabTxt}" : cabTxt;
        cuerpo.text = pendiente ?? objTxt;
        cuerpo.color = pendiente != null ? (tipoPendiente == Aviso.Tipo.Ok ? Verde : Color.white) : Color.white;
        pie.text = $"Recuerdos de Baymax {n}/{RecuerdosBaymax.Total}   <color=#7F93A8>Y: avisar</color>";
        if (total > 0)
        {
            float t = Mathf.Clamp01(paso / (float)total);
            barra.ancho = Mathf.Max(0.004f, (Ancho - 0.016f) * t);
            barra.transform.localPosition = new Vector3(-(Ancho - 0.016f) / 2f + barra.ancho / 2f, -Alto / 2f + 0.022f, -0.0012f);
            barra.Aplicar();
        }
    }

    // En la muñeca cabe poco: sin cambios de tamaño, como mucho 3 líneas y ~150 caracteres (el texto completo queda
    // en el historial de avisos, que enseña la AYUDA del menú).
    static string Breve(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, "</?size[^>]*>", "");
        var lineas = s.Split('\n');
        if (lineas.Length > 3) s = string.Join("\n", lineas, 0, 3);
        if (s.Length > 150) s = s.Substring(0, 147).TrimEnd() + "...";
        return s;
    }

    // "MISIÓN 1 · PASO 3 DE 6 · OBSERVAR" -> "MISIÓN 1"
    static string Abreviar(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int i = s.IndexOf(" · ", System.StringComparison.Ordinal);
        return i > 0 ? s.Substring(0, i) : s;
    }

    IEnumerator Vibrar(int veces)
    {
        for (int i = 0; i < veces; i++)
        {
            OVRInput.SetControllerVibration(1f, 0.55f, OVRInput.Controller.LTouch);
            yield return new WaitForSeconds(0.07f);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
            yield return new WaitForSeconds(0.09f);
        }
    }

    // ------------------------------------------------------------------ construcción

    void Montar(Transform ancla)
    {
        mano = ancla;
        raiz = new GameObject("BrazaleteBaymax").transform;
        raiz.SetParent(mano, false);
        raiz.localPosition = PosLocal;
        var n = NormalLocal.normalized;
        var arriba = Vector3.Cross(n, Vector3.right).normalized;   // hacia la mano (como las 12 de un reloj)
        if (Vector3.Dot(arriba, Vector3.forward) < 0f) arriba = -arriba;
        raiz.localRotation = Quaternion.LookRotation(-n, arriba);   // el -z local mira hacia fuera de la esfera

        var blanco = K.Escena("BlancoMate", new Color(0.93f, 0.94f, 0.95f));
        var oscuro = K.Plano(new Color(0.06f, 0.07f, 0.08f));
        // Correa alrededor de la muñeca (aro a lo largo del antebrazo) y caja del reloj.
        K.Pieza(raiz, "Correa", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.026f), new Vector3(0.062f, 0.012f, 0.052f), blanco, Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(90f, 0f, 0f));
        K.Caja(raiz, "Carcasa", new Vector3(0f, 0f, 0.004f), new Vector3(AnchoReloj + 0.006f, AltoReloj + 0.006f, 0.009f), blanco);
        K.Caja(raiz, "Esfera", new Vector3(0f, 0f, -0.0003f), new Vector3(AnchoReloj, AltoReloj, 0.001f), oscuro);
        mini = K.Texto(raiz, "", new Vector3(0f, -0.003f, -0.0012f), 0.05f, Cian, AnchoReloj - 0.008f, 0.02f, TextAlignmentOptions.Center, true);
        led = K.Esfera(raiz, "Led", new Vector3(0f, AltoReloj / 2f - 0.008f, -0.0015f), 0.006f, K.Plano(Color.white));

        // Holograma con el objetivo (fuera de la jerarquía de la mano: lo coloca ColocarHolograma cada frame).
        holograma = new GameObject("BrazaleteHolograma").transform;
        holograma.SetParent(transform, false);
        pantallaT = K.Grupo(holograma, "Pantalla", Vector3.zero);
        pantalla = Panel(K.Panel(pantallaT, "Fondo", Vector3.zero, Ancho, Alto, PantallaApagada, new Color(0.25f, 0.55f, 0.7f, 1f), 0.0015f, 0.012f));
        pantalla.radio = 0.008f;
        pantalla.Aplicar();
        cabecera = K.Texto(pantallaT, "", new Vector3(0f, Alto / 2f - 0.014f, -0.001f), 0.055f, Cian, Ancho - 0.016f, 0.018f, TextAlignmentOptions.Left, true);
        K.AutoTam(cabecera, 0.04f);
        cuerpo = K.Texto(pantallaT, "", new Vector3(0f, 0.001f, -0.001f), 0.075f, Color.white, Ancho - 0.016f, 0.05f, TextAlignmentOptions.Left);
        cuerpo.verticalAlignment = VerticalAlignmentOptions.Middle;
        K.AutoTam(cuerpo, 0.05f);
        pie = K.Texto(pantallaT, "", new Vector3(0f, -Alto / 2f + 0.01f, -0.001f), 0.04f, new Color(0.66f, 0.75f, 0.85f), Ancho - 0.016f, 0.012f, TextAlignmentOptions.Left);
        K.AutoTam(pie, 0.032f);
        barraFondo = Panel(K.Panel(pantallaT, "BarraFondo", new Vector3(0f, -Alto / 2f + 0.022f, -0.001f), Ancho - 0.016f, 0.003f, new Color(1f, 1f, 1f, 0.12f), Color.clear, 0f, 0.0015f));
        barra = Panel(K.Panel(pantallaT, "Barra", new Vector3(0f, -Alto / 2f + 0.022f, -0.0012f), 0.01f, 0.003f, Cian, Color.clear, 0f, 0.0015f));
        foreach (var t in GetComponentsInChildren<TextMeshPro>(true)) t.sortingOrder = AdaptadorEscena.OrdenTexto + 1;
        foreach (var t in raiz.GetComponentsInChildren<TextMeshPro>(true)) t.sortingOrder = AdaptadorEscena.OrdenTexto + 1;
        foreach (var c in raiz.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var c in holograma.GetComponentsInChildren<Collider>(true)) Destroy(c);
        holograma.gameObject.SetActive(false);
        Refrescar();
        AplicarBrillo();
    }

    // K.Panel devuelve el quad; sin el material del SDK no lleva PanelRedondeado y se le pone (respaldo).
    static PanelRedondeado Panel(Renderer r)
    {
        var p = r.GetComponent<PanelRedondeado>();
        return p != null ? p : r.gameObject.AddComponent<PanelRedondeado>();
    }
}
