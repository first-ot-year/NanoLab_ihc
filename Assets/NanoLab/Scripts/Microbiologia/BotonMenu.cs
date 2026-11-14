using Oculus.Interaction;
using TMPro;
using UnityEngine;

// Botón de un panel (MenuAjustes). Con el bloque "poke" oficial del Interaction SDK de Meta (lo pone MontarAjustes)
// se pulsa con la punta del dedo o del mando, como los botones de las estaciones; también apuntándolo con el rayo.
// Sin bloque poke (escenas antiguas) queda el respaldo: se activa al acercar la mano.
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
    PokeInteractable poke;

    public override string Accion => accion;

    void Awake() => poke = GetComponentInChildren<PokeInteractable>(true);
    void OnEnable() { if (poke != null) poke.WhenStateChanged += AlCambiarEstado; }
    void AlCambiarEstado(InteractableStateChangeArgs a) { if (a.NewState == InteractableState.Select) Activar(); }

    public override void Activar()
    {
        if (Time.unscaledTime - ultimo < 0.25f) return;
        ultimo = Time.unscaledTime;
        Pulsado?.Invoke(id);
        ManosRig.Vibrar(transform.position, 0.4f, 0.05f);
        Sonidos.Clic(transform.position);
        // Con poke, el verde al pulsar lo da el InteractableColorVisual del SDK; el destello es del botón plano.
        if (fondo != null && fondo.gameObject.activeInHierarchy && isActiveAndEnabled) { StopAllCoroutines(); StartCoroutine(Destello()); }
    }

    void OnDisable()
    {
        if (poke != null) poke.WhenStateChanged -= AlCambiarEstado;
        tocando = false;
        // Si se cierra el panel a mitad del destello, el botón vuelve a su color y tamaño.
        if (fondo != null && colorOriginal.HasValue) { fondo.color = colorOriginal.Value; fondo.Aplicar(); }
        if (etiqueta != null && escalaOriginal.HasValue) etiqueta.transform.localScale = escalaOriginal.Value;
    }

    void Update()
    {
        if (poke != null) return;   // se pulsa con el poke de Meta
        bool ahora = ManosRig.MasCercana(transform.position, radio, out _);
        if (ahora && !tocando) Activar();
        tocando = ahora;
    }

    Color? colorOriginal;
    Vector3? escalaOriginal;

    // El botón se pone verde y vuelve poco a poco a su color; la etiqueta da un pequeño salto.
    System.Collections.IEnumerator Destello()
    {
        if (colorOriginal == null) colorOriginal = fondo.color;
        var c = colorOriginal.Value;
        var verde = new Color(0.25f, 0.85f, 0.45f, c.a);
        if (escalaOriginal == null) escalaOriginal = etiqueta != null ? etiqueta.transform.localScale : Vector3.one;
        var escala = escalaOriginal.Value;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
        {
            fondo.color = Color.Lerp(verde, c, t * t);
            fondo.Aplicar();
            if (etiqueta != null) etiqueta.transform.localScale = escala * (1f + 0.15f * (1f - t));
            yield return null;
        }
        fondo.color = c;
        fondo.Aplicar();
        if (etiqueta != null) etiqueta.transform.localScale = escala;
    }
}
