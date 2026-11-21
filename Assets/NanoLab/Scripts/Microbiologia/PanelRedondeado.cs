using UnityEngine;

// Panel con esquinas redondeadas y borde, dibujado con el shader del Interaction SDK de Meta
// (Interaction/RoundedBoxUnlit), el mismo estilo que los paneles del sistema de Quest.
// Va en un Quad; ancho/alto en metros (el Quad se escala solo). Usa un MaterialPropertyBlock,
// así todos los paneles comparten un único material.
[ExecuteAlways, RequireComponent(typeof(MeshRenderer))]
public class PanelRedondeado : MonoBehaviour
{
    public float ancho = 1f, alto = 0.5f;
    public float radio = 0.04f;
    [Tooltip("Grosor del borde (m). 0 = sin borde.")]
    public float borde = 0.004f;
    public Color color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
    public Color colorBorde = new Color(0.35f, 0.85f, 1f, 1f);
    [Range(0f, 1f)] public float opacidad = 1f;

    static readonly int IdColor = Shader.PropertyToID("_Color");
    static readonly int IdBorde = Shader.PropertyToID("_BorderColor");
    static readonly int IdRadios = Shader.PropertyToID("_Radii");
    static readonly int IdDimensiones = Shader.PropertyToID("_Dimensions");

    MeshRenderer mr;
    MaterialPropertyBlock mpb;
    Renderer respaldo;
    static Material matRespaldo;
    const float AreaMinimaRespaldo = 0.02f;   // m²: los botones y puntos pequeños no lo necesitan

    void OnEnable() => Aplicar();

    // Revisión (playtest): el shader del panel solo dibuja su cara delantera y TextMeshPro ambas, así que al rodear
    // un cartel se veía transparente con las letras al revés. Al jugar, cada panel grande lleva detrás una placa
    // OPACA que mira hacia atrás: desde delante no se ve y desde detrás escribe profundidad, así tapa las letras.
    // Un cartel nunca se ve hueco desde ningún ángulo.
    void Start()
    {
        if (!Application.isPlaying || respaldo != null || ancho * alto < AreaMinimaRespaldo || EsInterfazMovil()) return;
        if (matRespaldo == null)
        {
            var oscuro = new Color(0.05f, 0.08f, 0.13f);
            var sh = Shader.Find("Unlit/Color");   // en el Quest puede no estar incluido: entonces el material mate de la escena
            matRespaldo = sh != null ? new Material(sh) { name = "RespaldoPanel", color = oscuro } : KitVacuna.Plano(oscuro);
        }
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.name = "Respaldo";
        q.layer = gameObject.layer;
        q.transform.SetParent(transform, false);
        q.transform.localPosition = new Vector3(0f, 0f, 0.002f);
        q.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        respaldo = q.GetComponent<Renderer>();
        respaldo.sharedMaterial = matRespaldo;
        respaldo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        respaldo.receiveShadows = false;
        Aplicar();
    }
    void OnValidate() => Aplicar();

    public void Aplicar()
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        if (mpb == null) mpb = new MaterialPropertyBlock();
        // Igual que RoundedBoxProperties del SDK: el borde exterior amplía el Quad.
        float ext = borde / 2f;
        transform.localScale = new Vector3(ancho + ext * 2f, alto + ext * 2f, 1f);
        var c = color; c.a *= opacidad;
        var cb = colorBorde; cb.a *= opacidad;
        mpb.SetColor(IdColor, c);
        mpb.SetColor(IdBorde, cb);
        // El shader trabaja en coordenadas al doble de escala (uv * 2 * tamaño): el radio también.
        float r = Mathf.Min(radio, Mathf.Min(ancho, alto) / 2f) * 2f;
        mpb.SetVector(IdRadios, new Vector4(r, r, r, r));
        mpb.SetVector(IdDimensiones, new Vector4(transform.localScale.x, transform.localScale.y, ext, ext));
        mr.SetPropertyBlock(mpb);
        if (respaldo != null) respaldo.enabled = c.a > 0.5f && mr.enabled;   // un panel que se desvanece no deja su placa
    }

    // Solo los carteles fijos del escenario llevan respaldo. Los paneles que siguen al jugador o se abren y cierran
    // (menú ☰, bienvenida, avisos, reloj, recuerdos, rig) no: algunos se ocultan apagando solo su dibujo y la placa
    // quedaba sola en el aire (un "cartel azul oscuro" en la demo).
    bool EsInterfazMovil()
    {
        for (var t = transform; t != null; t = t.parent)
        {
            if (t.GetComponent<OVRCameraRig>() != null || t.GetComponent<MenuAjustes>() != null || t.GetComponent<AvisoInicio>() != null ||
                t.GetComponent<BrazaleteBaymax>() != null || t.GetComponent<RecuerdoAnimado>() != null) return true;
            string n = t.name;
            if (n == "Avisos" || n.StartsWith("Aviso") || n.StartsWith("Brazalete") || n.StartsWith("Recuerdo") || n.StartsWith("Demo")) return true;
        }
        return false;
    }

    // El respaldo sigue al panel: si su dibujo se apaga (sin pasar por Aplicar) o se está moviendo, la placa no se ve.
    Vector3 posAnterior;
    void LateUpdate()
    {
        if (respaldo == null) return;
        bool quieto = (transform.position - posAnterior).sqrMagnitude < 1e-6f;
        posAnterior = transform.position;
        bool ver = mr != null && mr.enabled && color.a * opacidad > 0.5f && quieto;
        if (respaldo.enabled != ver) respaldo.enabled = ver;
    }
}
