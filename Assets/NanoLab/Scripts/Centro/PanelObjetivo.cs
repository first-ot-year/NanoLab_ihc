using TMPro;
using UnityEngine;

// Panel con el ciclo de investigación (8 etapas que se iluminan) y el objetivo actual.
public class PanelObjetivo : MonoBehaviour
{
    public TMP_Text titulo, objetivo, estado;
    public Renderer[] segmentos;
    public TMP_Text[] etiquetas;

    static readonly Color Hecho = new Color(0.25f, 0.85f, 0.45f);
    static readonly Color Actual = new Color(1f, 0.8f, 0.3f);
    static readonly Color Pendiente = new Color(0.12f, 0.17f, 0.24f);
    MaterialPropertyBlock mpb;
    int etapaCiclo;

    public void Mostrar(int etapa, string tituloCaso, string textoObjetivo, string textoEstado)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        etapaCiclo = etapa;
        if (titulo != null) titulo.text = tituloCaso;
        if (objetivo != null) objetivo.text = textoObjetivo;
        if (estado != null) estado.text = textoEstado;
        for (int i = 0; i < segmentos.Length; i++)
        {
            var c = i < etapa ? Hecho : i == etapa ? Actual : Pendiente;
            segmentos[i].GetPropertyBlock(mpb);
            mpb.SetColor("_Color", c);
            segmentos[i].SetPropertyBlock(mpb);
            if (etiquetas != null && i < etiquetas.Length && etiquetas[i] != null)
            {
                // Además del color, un prefijo de forma: [OK] hecho, > actual.
                string n = RetoManager.Ciclo[i];
                etiquetas[i].text = i < etapa ? $"<color=#7CFF9E>OK {n}</color>" : i == etapa ? $"<color=#FFD25A><b>> {n}</b></color>" : $"<color=#6F8296>{n}</color>";
            }
        }
    }

    void Update()
    {
        // El segmento actual late suavemente.
        if (segmentos == null || etapaCiclo >= segmentos.Length || mpb == null) return;
        var r = segmentos[etapaCiclo];
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", Color.Lerp(Actual, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 4f)));
        r.SetPropertyBlock(mpb);
    }
}
