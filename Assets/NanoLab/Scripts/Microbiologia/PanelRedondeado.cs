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

    void OnEnable() => Aplicar();
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
    }
}
