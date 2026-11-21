using TMPro;
using UnityEngine;
using K = KitVacuna;

// Indicador de RECUERDO RECUPERADO, anclado a la vista abajo a la derecha (siempre se ve, sin tapar el centro).
// Revisión de la demo ("ese conocimiento hazlo más animado y pon la cara de Baymax allí"): ahora es una pequeña escena
// de ~6 s con la cara de Baymax:
//   0,0-0,4 s  el panel aparece con un rebote y la cara de Baymax asoma (escala con rebote).
//   0,3-1,1 s  el núcleo de memoria dorado flota arriba a la derecha, late y lo rodean chispas; el título se escribe.
//   1,1-1,7 s  el núcleo vuela en arco hasta su ranura y se encoge.
//   1,7 s      la ranura se enciende con un destello, suena y vibra; Baymax inclina la cabeza (su gesto) y parpadea.
//   5,2-6,0 s  todo se recoge hacia la derecha.
// Sigue a la cabeza con suavidad (sin tirones). Lo llaman RetoManager (misiones 1 y 2) y RetoVacuna (misión 3) justo
// después de RecuerdosBaymax.Recuperar.
public class RecuerdoAnimado : MonoBehaviour
{
    public const float Duracion = 6f;
    // Respecto a los ojos: a la derecha, algo por debajo y a ~0,65 m (fuera del centro de la vista, legible).
    public static readonly Vector3 Desplazamiento = new Vector3(0.23f, -0.08f, 0.65f);
    static readonly Color Oro = new Color(1f, 0.82f, 0.35f);
    static readonly Color Cian = new Color(0.45f, 0.9f, 1f);
    static readonly Color Apagada = new Color(0.25f, 0.3f, 0.36f);
    const float Ancho = 0.28f, Alto = 0.1f;
    const float TVuelo = 1.1f, TLlega = 1.7f, TRecoge = 5.2f;
    static readonly Vector3 PosCara = new Vector3(-0.098f, 0.002f, -0.006f);
    static readonly Vector3 InicioNucleo = new Vector3(0.165f, 0.072f, -0.012f);

    public static RecuerdoAnimado Actual { get; private set; }
    public string Titulo { get; private set; }
    // Dónde está respecto a la cabeza (para las pruebas: x > 0 = a la derecha de la vista).
    public Vector3 PosicionEnLaVista => cabeza != null ? cabeza.InverseTransformPoint(transform.position) : Vector3.zero;

    Transform cabeza, contenido, cara, ojos, nucleo, anillo, chispas, destello;
    Renderer[] ranuras;
    Renderer ranuraNueva;
    TextMeshPro cabecera, nombre;
    float t;
    int numero;
    bool llegado;

    public static RecuerdoAnimado Reproducir(string titulo, string frase)
    {
        var cab = ManosRig.Cabeza;
        if (cab == null) return null;
        if (Actual != null) Destroy(Actual.gameObject);
        var go = new GameObject("RecuerdoBaymax");
        var r = go.AddComponent<RecuerdoAnimado>();
        r.Construir(cab, titulo);
        Actual = r;
        return r;
    }

    void Construir(Transform cab, string titulo)
    {
        cabeza = cab;
        Titulo = titulo;
        numero = Mathf.Clamp(RecuerdosBaymax.Cantidad, 1, RecuerdosBaymax.Total);
        Colocar(1f);
        contenido = K.Grupo(transform, "Contenido", Vector3.zero);

        K.Panel(contenido, "Fondo", new Vector3(0f, 0f, 0.003f), Ancho, Alto, new Color(0.05f, 0.1f, 0.18f, 0.97f), Oro, 0.002f, 0.024f);

        // Cara de Baymax: cabeza blanca ovalada, dos ojos negros unidos por una línea fina.
        cara = K.Grupo(contenido, "CaraBaymax", PosCara);
        K.Esfera(cara, "Halo", new Vector3(0f, 0f, 0.004f), 0.082f, K.Plano(new Color(0.12f, 0.2f, 0.32f)));
        K.Pieza(cara, "Cabeza", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.068f, 0.05f, 0.02f), K.Plano(Color.white));
        ojos = K.Grupo(cara, "Ojos", new Vector3(0f, 0.003f, -0.0105f));
        K.Esfera(ojos, "OjoI", new Vector3(-0.016f, 0f, 0f), 0.0105f, K.Plano(new Color(0.05f, 0.05f, 0.06f)));
        K.Esfera(ojos, "OjoD", new Vector3(0.016f, 0f, 0f), 0.0105f, K.Plano(new Color(0.05f, 0.05f, 0.06f)));
        K.Caja(ojos, "Linea", new Vector3(0f, 0f, -0.001f), new Vector3(0.03f, 0.0016f, 0.001f), K.Plano(new Color(0.05f, 0.05f, 0.06f)));

        cabecera = K.Texto(contenido, $"<b>RECUERDO RECUPERADO · {numero}/{RecuerdosBaymax.Total}</b>", new Vector3(0.04f, 0.026f, -0.002f), 0.09f, Oro, 0.18f, 0.025f, TextAlignmentOptions.Left);
        K.AutoTam(cabecera, 0.06f);
        nombre = K.Texto(contenido, titulo ?? "", new Vector3(0.04f, 0.002f, -0.002f), 0.085f, Color.white, 0.18f, 0.025f, TextAlignmentOptions.Left);
        K.AutoTam(nombre, 0.055f);
        cabecera.maxVisibleCharacters = 0;
        nombre.maxVisibleCharacters = 0;

        ranuras = new Renderer[RecuerdosBaymax.Total];
        for (int i = 0; i < ranuras.Length; i++)
            ranuras[i] = K.Esfera(contenido, "Ranura", PosRanura(i), 0.014f, K.Plano(i < numero - 1 ? Oro : Apagada));
        ranuraNueva = ranuras[numero - 1];

        // Núcleo de memoria con su anillo de luces y chispas alrededor.
        nucleo = K.Esfera(contenido, "Nucleo", InicioNucleo, 0.045f, K.Plano(Oro)).transform;
        K.Esfera(nucleo, "Brillo", Vector3.zero, 1.6f, K.Vidrio(new Color(1f, 0.85f, 0.4f, 0.22f)));
        anillo = K.Grupo(nucleo, "Anillo", Vector3.zero);
        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            K.Esfera(anillo, "Luz", new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.95f, 0.15f, K.Plano(i % 3 == 0 ? Oro : Cian));
        }
        chispas = K.Grupo(contenido, "Chispas", InicioNucleo);
        for (int i = 0; i < 6; i++) K.Esfera(chispas, "Chispa", Vector3.zero, 0.005f, K.Plano(i % 2 == 0 ? Oro : Cian));
        destello = K.Grupo(contenido, "Destello", PosRanura(numero - 1));
        for (int i = 0; i < 12; i++) K.Esfera(destello, "Rayo", Vector3.zero, 0.004f, K.Plano(i % 2 == 0 ? Oro : Color.white));
        destello.gameObject.SetActive(false);

        foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var tx in GetComponentsInChildren<TextMeshPro>(true)) tx.sortingOrder = AdaptadorEscena.OrdenTexto + 2;
        contenido.localScale = Vector3.one * 0.01f;
        cara.localScale = Vector3.one * 0.01f;
        Sonidos.Exito(transform.position);
        ManosRig.Vibrar(transform.position, 0.5f, 0.15f);
    }

    static Vector3 PosRanura(int i) => new Vector3(-0.042f + i * 0.024f, -0.03f, -0.004f);

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

        // Entrada con rebote y salida hacia la derecha.
        float sale = t > TRecoge ? Mathf.SmoothStep(0f, 1f, (t - TRecoge) / (Duracion - TRecoge)) : 0f;
        contenido.localScale = Vector3.one * Mathf.Max(0.01f, Rebote(t / 0.4f) * (1f - sale));
        contenido.localPosition = new Vector3(0.08f * sale, 0f, 0f);

        // Baymax: asoma, flota, inclina la cabeza al llegar el recuerdo y parpadea.
        cara.localScale = Vector3.one * Mathf.Max(0.01f, Rebote((t - 0.1f) / 0.45f));
        float inclina = llegado ? Mathf.Sin(Mathf.Clamp01((t - TLlega) / 1.4f) * Mathf.PI) * 18f : 0f;
        cara.localRotation = Quaternion.Euler(0f, 0f, inclina + 2.5f * Mathf.Sin(t * 2.2f));
        cara.localPosition = PosCara + new Vector3(0f, 0.002f * Mathf.Sin(t * 3f), 0f);
        bool parpadeo = Mathf.Abs(t - 0.9f) < 0.07f || Mathf.Abs(t - (TLlega + 0.5f)) < 0.07f || Mathf.Abs(t - 3.9f) < 0.07f;
        ojos.localScale = new Vector3(1f, parpadeo ? 0.15f : 1f, 1f);

        // Título que se escribe letra a letra.
        cabecera.maxVisibleCharacters = Mathf.Max(0, Mathf.FloorToInt((t - 0.3f) * 45f));
        nombre.maxVisibleCharacters = Mathf.Max(0, Mathf.FloorToInt((t - 0.9f) * 40f));

        // Núcleo: flota y late, vuela en arco a su ranura y se encoge.
        if (!llegado)
        {
            float k = Mathf.Clamp01((t - TVuelo) / (TLlega - TVuelo));
            float s = Mathf.SmoothStep(0f, 1f, k);
            var destino = PosRanura(numero - 1);
            var p = Vector3.Lerp(InicioNucleo, destino, s) + new Vector3(0f, 0.045f * Mathf.Sin(s * Mathf.PI), -0.01f * Mathf.Sin(s * Mathf.PI));
            if (k <= 0f) p += new Vector3(0f, 0.004f * Mathf.Sin(t * 5f), 0f);
            nucleo.localPosition = p;
            nucleo.localScale = Vector3.one * Mathf.Lerp(0.045f, 0.014f, s) * (1f + 0.14f * Mathf.Sin(t * 8f));
            anillo.localRotation = Quaternion.Euler(0f, 0f, t * 140f);
            chispas.localPosition = p;
            for (int i = 0; i < chispas.childCount; i++)
            {
                float a = t * 3.2f + i * Mathf.PI * 2f / chispas.childCount;
                float r = 0.034f * (1f - s) + 0.008f * Mathf.Sin(t * 6f + i);
                chispas.GetChild(i).localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, Mathf.Sin(a * 0.7f) * 0.006f);
            }
            if (k >= 1f) Llegar();
        }
        else
        {
            // Destello de la ranura: rayos que salen y se apagan; la ranura nueva late y se queda encendida.
            float d = (t - TLlega) / 0.6f;
            if (d < 1f)
            {
                for (int i = 0; i < destello.childCount; i++)
                {
                    float a = i * Mathf.PI * 2f / destello.childCount;
                    destello.GetChild(i).localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * (0.006f + 0.04f * d);
                    destello.GetChild(i).localScale = Vector3.one * 0.004f * (1f - d);
                }
            }
            else if (destello.gameObject.activeSelf) destello.gameObject.SetActive(false);
            float pop = d < 1f ? 1f + 0.9f * Mathf.Sin(Mathf.Clamp01(d * 1.5f) * Mathf.PI) : 1f + 0.12f * Mathf.Sin(t * 5f);
            ranuraNueva.transform.localScale = Vector3.one * 0.014f * pop;
        }
        if (t > Duracion) Destroy(gameObject);
    }

    void Llegar()
    {
        llegado = true;
        nucleo.gameObject.SetActive(false);
        chispas.gameObject.SetActive(false);
        K.Pintar(ranuraNueva, Oro);
        destello.gameObject.SetActive(true);
        Sonidos.Encaje(ranuraNueva.transform.position);
        Sonidos.Carga(ranuraNueva.transform.position);
        ManosRig.Vibrar(transform.position, 0.6f, 0.12f);
    }

    void OnDestroy() { if (Actual == this) Actual = null; }
}
