using System.Collections;
using TMPro;
using UnityEngine;

// Cápsula experimental: prototipo de intervención con un agente. La formula la prensa
// y se consume dentro de la cámara experimental.
[RequireComponent(typeof(Encajable))]
public class Capsula : MonoBehaviour
{
    public Renderer cuerpo;
    public Renderer nucleo;
    public TMP_Text etiqueta;

    public int Agente { get; private set; } = -1;
    public Encajable Encajable { get; private set; }
    MaterialPropertyBlock mpb;
    Vector3 escala;

    void Awake()
    {
        Encajable = GetComponent<Encajable>();
        mpb = new MaterialPropertyBlock();
        escala = transform.localScale;
    }

    public void Configurar(int agente, RetoData.Agente a)
    {
        Agente = agente;
        transform.localScale = escala;
        foreach (var r in new[] { cuerpo, nucleo })
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            var c = a.color; c.a = r == cuerpo ? 0.55f : 1f;
            mpb.SetColor("_Color", c);
            mpb.SetColor("_EmissionColor", a.color * (r == nucleo ? 1.5f : 0.4f));
            r.SetPropertyBlock(mpb);
        }
        if (etiqueta != null) etiqueta.text = a.sigla;
    }

    // Se disuelve dentro de la cámara (el agente se libera sobre la placa).
    public IEnumerator Consumir(Vector3 destino, float duracion)
    {
        Encajable.Desencajar();
        Encajable.Bloquear(true);
        GetComponent<Rigidbody>().isKinematic = true;
        var inicio = transform.position;
        for (float t = 0; t < 1f; t += Time.deltaTime / duracion)
        {
            float k = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(inicio, destino, Mathf.Min(1f, k * 1.6f));
            transform.localScale = escala * (1f - Mathf.Max(0f, k - 0.5f) * 2f);
            yield return null;
        }
        Agente = -1;
        Encajable.Bloquear(false);
        gameObject.SetActive(false);
    }
}
