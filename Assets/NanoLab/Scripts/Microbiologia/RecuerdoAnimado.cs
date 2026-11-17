using TMPro;
using UnityEngine;
using K = KitVacuna;

// Baymax recupera un recuerdo DE VERDAD (revisión: recompensa visible al terminar cada misión, no solo un texto).
// El jugador es Baymax: delante de él aparece un núcleo de memoria que crece con un acorde, con un anillo de luces
// y un panel con el recuerdo (n/3 y la frase que recordó). A los pocos segundos todo se encoge y vuela hacia su
// pecho (su núcleo), con un destello, un sonido y una vibración en las dos manos.
// Lo llaman RetoManager (misiones 1 y 2) y RetoVacuna (misión 3) justo después de RecuerdosBaymax.Recuperar.
public class RecuerdoAnimado : MonoBehaviour
{
    public const float Duracion = 6.2f;
    const float TamNucleo = 0.2f;   // diámetro del núcleo de memoria (m); el latido escala sobre este valor
    static readonly Color Oro = new Color(1f, 0.82f, 0.35f);
    static readonly Color Cian = new Color(0.45f, 0.9f, 1f);

    public static RecuerdoAnimado Actual { get; private set; }
    public string Titulo { get; private set; }

    Transform raiz, nucleo, anillo, panel;
    Renderer[] ranuras;
    float t;
    Vector3 origen;
    int numero;

    public static RecuerdoAnimado Reproducir(string titulo, string frase)
    {
        var cab = ManosRig.Cabeza;
        if (cab == null) return null;
        if (Actual != null) Destroy(Actual.gameObject);
        var go = new GameObject("RecuerdoBaymax");
        var r = go.AddComponent<RecuerdoAnimado>();
        r.Construir(cab, titulo, frase);
        Actual = r;
        return r;
    }

    void Construir(Transform cab, string titulo, string frase)
    {
        Titulo = titulo;
        numero = Mathf.Clamp(RecuerdosBaymax.Cantidad, 1, RecuerdosBaymax.Total);
        raiz = transform;
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
        f.Normalize();
        origen = cab.position + f * 1.3f - Vector3.up * 0.15f;
        raiz.SetPositionAndRotation(origen, Quaternion.LookRotation(f, Vector3.up));

        nucleo = K.Esfera(raiz, "Nucleo", Vector3.zero, TamNucleo, K.Plano(Oro)).transform;
        K.Esfera(nucleo, "Brillo", Vector3.zero, 1.45f, K.Vidrio(new Color(1f, 0.85f, 0.4f, 0.16f)));
        anillo = K.Grupo(raiz, "Anillo", Vector3.zero);
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f;
            K.Esfera(anillo, "Luz", new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.2f, 0.025f, K.Plano(i % 3 == 0 ? Oro : Cian));
        }

        panel = K.Grupo(raiz, "Panel", new Vector3(0f, -0.33f, 0f));
        K.Panel(panel, "Fondo", new Vector3(0f, 0f, 0.003f), 0.9f, 0.3f, new Color(0.05f, 0.1f, 0.18f, 0.97f), Oro, 0.004f, 0.04f);
        K.Texto(panel, $"<b>RECUERDO RECUPERADO · {numero}/{RecuerdosBaymax.Total}</b>  <color=#8FD8FF>{titulo}</color>",
                new Vector3(0f, 0.105f, -0.002f), 0.2f, Oro, 0.84f, 0.06f, TextAlignmentOptions.Center);
        var tx = K.Texto(panel, frase ?? "", new Vector3(0f, -0.005f, -0.002f), 0.15f, Color.white, 0.84f, 0.14f, TextAlignmentOptions.Center);
        K.AutoTam(tx, 0.09f);
        // Las 3 ranuras de recuerdos: llenas hasta n, la nueva parpadea.
        ranuras = new Renderer[RecuerdosBaymax.Total];
        for (int i = 0; i < ranuras.Length; i++)
            ranuras[i] = K.Esfera(panel, "Ranura", new Vector3((i - 1) * 0.07f, -0.115f, -0.004f), 0.035f, K.Plano(i < numero ? Oro : new Color(0.25f, 0.3f, 0.36f)));
        foreach (var c in raiz.GetComponentsInChildren<Collider>(true)) Destroy(c);
        raiz.localScale = Vector3.one * 0.01f;
        Sonidos.Exito(origen);
        ManosRig.Vibrar(origen, 0.4f, 0.15f);
    }

    void Update()
    {
        t += Time.deltaTime;
        var cab = ManosRig.Cabeza;
        // 0-0,6 s crece · 0,6-4,8 s flota y gira · 4,8-6,2 s vuela al pecho.
        if (t < 0.6f) raiz.localScale = Vector3.one * Mathf.SmoothStep(0.01f, 1f, t / 0.6f);
        else if (t < 4.8f) raiz.localScale = Vector3.one;
        anillo.localRotation = Quaternion.Euler(0f, 0f, t * 60f);
        nucleo.localScale = Vector3.one * (TamNucleo * (1f + 0.08f * Mathf.Sin(t * 6f)));
        if (ranuras != null && numero >= 1)
        {
            var r = ranuras[numero - 1];
            if (r != null) r.enabled = t > 4.8f || Mathf.Repeat(t, 0.5f) < 0.33f;
        }
        if (t >= 4.8f && cab != null)
        {
            float k = Mathf.Clamp01((t - 4.8f) / 1.2f);
            var pecho = cab.position - Vector3.up * 0.38f;
            raiz.position = Vector3.Lerp(origen, pecho, k * k);
            raiz.localScale = Vector3.one * Mathf.Lerp(1f, 0.05f, k);
            if (panel != null) panel.gameObject.SetActive(k < 0.3f);
            if (k >= 1f && t < 6.05f)
            {
                Sonidos.Capsula(pecho);
                ManosRig.Vibrar(pecho, 0.9f, 0.25f);
                t = 6.05f;
            }
        }
        if (t > Duracion) Destroy(gameObject);
    }

    void OnDestroy() { if (Actual == this) Actual = null; }
}
