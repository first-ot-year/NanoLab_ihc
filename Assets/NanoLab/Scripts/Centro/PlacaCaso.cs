using System.Collections;
using UnityEngine;

// Placa de Petri del caso: sus colonias cambian de forma visible tras la intervención
// (halo de inhibición y colonias que desaparecen, o colonias que siguen creciendo).
public class PlacaCaso : MonoBehaviour
{
    public Transform[] colonias;
    public Transform[] coloniasExtra;     // ocultas: aparecen si la bacteria sigue creciendo
    public Transform halo;                // disco claro alrededor del centro (zona sin bacterias)
    public float radioHaloMax = 0.05f;
    public TMPro.TMP_Text estado;

    public bool Tratada { get; private set; }
    Vector3[] escalas, escalasExtra;
    Vector3 escalaHalo;

    void Awake()
    {
        escalas = new Vector3[colonias.Length];
        for (int i = 0; i < colonias.Length; i++) escalas[i] = colonias[i].localScale;
        escalasExtra = new Vector3[coloniasExtra.Length];
        for (int i = 0; i < coloniasExtra.Length; i++) escalasExtra[i] = coloniasExtra[i].localScale;
        if (halo != null) escalaHalo = halo.localScale;
        Reiniciar();
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        Tratada = false;
        for (int i = 0; i < colonias.Length; i++) { colonias[i].localScale = escalas[i]; colonias[i].gameObject.SetActive(true); }
        foreach (var c in coloniasExtra) c.gameObject.SetActive(false);
        if (halo != null) halo.gameObject.SetActive(false);
        if (estado != null) estado.text = "SIN TRATAR";
    }

    // Anima 'duracion' segundos el efecto de una intervención con esta eficacia (0..1).
    public IEnumerator Tratar(float eficacia, float duracion)
    {
        Tratada = true;
        float radio = radioHaloMax * Mathf.Clamp01(eficacia);
        var finales = new Vector3[colonias.Length];
        for (int i = 0; i < colonias.Length; i++)
        {
            float d = new Vector2(colonias[i].localPosition.x, colonias[i].localPosition.z).magnitude;
            float f = d < radio ? 0f : eficacia >= 0.7f ? 0.75f : eficacia > 0.05f ? 1f : 1.35f;
            finales[i] = escalas[i] * f;
        }
        bool crecen = eficacia <= 0.05f;
        if (halo != null) { halo.gameObject.SetActive(radio > 0.004f); halo.localScale = Vector3.zero; }
        foreach (var c in coloniasExtra) { c.gameObject.SetActive(crecen); c.localScale = Vector3.zero; }

        for (float t = 0; t < 1f; t += Time.deltaTime / duracion)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            for (int i = 0; i < colonias.Length; i++) colonias[i].localScale = Vector3.Lerp(escalas[i], finales[i], k);
            for (int i = 0; i < coloniasExtra.Length; i++) coloniasExtra[i].localScale = escalasExtra[i] * k;
            if (halo != null && radio > 0.004f)
            {
                float s = radio / radioHaloMax;
                halo.localScale = new Vector3(escalaHalo.x * s * k, escalaHalo.y, escalaHalo.z * s * k);
            }
            yield return null;
        }
        for (int i = 0; i < colonias.Length; i++)
        {
            colonias[i].localScale = finales[i];
            if (finales[i] == Vector3.zero) colonias[i].gameObject.SetActive(false);
        }
        for (int i = 0; i < coloniasExtra.Length; i++) coloniasExtra[i].localScale = escalasExtra[i];
        if (estado != null) estado.text = eficacia >= 0.7f ? "HALO DE INHIBICIÓN" : crecen ? "SIGUE CRECIENDO" : "SIN CAMBIOS CLAROS";
    }
}
