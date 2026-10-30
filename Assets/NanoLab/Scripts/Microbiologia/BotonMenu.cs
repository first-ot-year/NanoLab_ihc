using TMPro;
using UnityEngine;

// Botón plano de un panel (MenuAjustes): se pulsa tocándolo con la mano o apuntándolo con el rayo.
public class BotonMenu : Apuntable
{
    public string id;
    public string accion = "pulsar";
    public TMP_Text etiqueta;
    public PanelRedondeado fondo;
    [Tooltip("Radio de toque (m).")]
    public float radio = 0.045f;

    public event System.Action<string> Pulsado;

    bool tocando;
    float ultimo = -1f;

    public override string Accion => accion;

    public override void Activar()
    {
        if (Time.unscaledTime - ultimo < 0.25f) return;
        ultimo = Time.unscaledTime;
        Pulsado?.Invoke(id);
        ManosRig.Vibrar(transform.position, 0.4f, 0.05f);
        Sonidos.Clic(transform.position);
        if (fondo != null) StartCoroutine(Destello());
    }

    void OnDisable() { tocando = false; }

    void Update()
    {
        bool ahora = ManosRig.MasCercana(transform.position, radio, out _);
        if (ahora && !tocando) Activar();
        tocando = ahora;
    }

    System.Collections.IEnumerator Destello()
    {
        var c = fondo.color;
        fondo.color = Color.Lerp(c, Color.white, 0.35f);
        fondo.Aplicar();
        yield return new WaitForSecondsRealtime(0.12f);
        fondo.color = c;
        fondo.Aplicar();
    }
}
