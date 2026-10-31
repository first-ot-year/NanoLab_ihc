using Oculus.Interaction;
using TMPro;
using UnityEngine;

// Botón de las estaciones. Con el Interaction SDK de Meta se pulsa con "poke" (la punta del dedo o
// del mando): la tapa se hunde (PokeInteractableVisual) y cambia de color en reposo, al pasar por
// encima y al pulsar (InteractableColorVisual). Además da respuesta propia: la etiqueta se ilumina
// al acercarse y destella al pulsar, vibra el mando y suena al pulsar y al soltar. Los botones que
// alternan (CORTE, ETIQUETAS…) muestran su estado con un LED. Al pulsar, la tapa se queda verde un
// momento (y mientras la función esté activa), se enciende un halo y saltan chispas. También se pulsa a distancia con el
// rayo de la mano vacía (gatillo o A/X). Sin PokeInteractable, se pulsa al acercar la mano (respaldo).
public class BotonFisico : Apuntable
{
    public Transform tapa;               // solo en el respaldo sin poke
    public TMP_Text etiqueta;
    public Renderer led;                 // estado de los botones que alternan
    public Renderer halo;                // anillo de luz que se expande al pulsar
    public ParticleSystem chispas;       // destellos al pulsar
    [Tooltip("Color de la tapa en reposo (el de encima se aclara a partir de él).")]
    public Color colorReposo = new Color(0.12f, 0.45f, 0.62f, 1f);
    public float radio = 0.06f;
    public event System.Action Pulsado;
    public bool Presionado => seleccionado || Time.time < hastaRayo;
    public bool Activo { get; private set; }

    static readonly Color Reposo = new Color(0.85f, 0.95f, 1f);
    static readonly Color Encima = new Color(0.45f, 0.95f, 1f);
    static readonly Color Pulsando = new Color(0.55f, 1f, 0.7f);
    static readonly Color LedApagado = new Color(0.08f, 0.12f, 0.16f);
    static readonly Color LedEncendido = new Color(0.3f, 1f, 0.5f);
    static readonly Color TapaVerde = new Color(0.35f, 1f, 0.5f, 1f);

    PokeInteractable poke;
    InteractableColorVisual colorTapa;
    bool seleccionado, encima, tapaVerde;
    float ultimo = -1f, hastaRayo, destello, verde;
    Vector3 tapaPos, escalaEtiqueta, escalaHalo;
    MaterialPropertyBlock mpb;

    public override string Accion => "pulsar " + (etiqueta != null ? etiqueta.text : "el botón");

    void Awake()
    {
        poke = GetComponentInChildren<PokeInteractable>(true);
        colorTapa = GetComponentInChildren<InteractableColorVisual>(true);
        if (tapa != null) tapaPos = tapa.localPosition;
        if (halo != null) { escalaHalo = halo.transform.localScale; halo.enabled = false; }
        if (etiqueta != null) escalaEtiqueta = etiqueta.transform.localScale;
        mpb = new MaterialPropertyBlock();
        PintarLed();
    }

    void OnEnable() { if (poke != null) poke.WhenStateChanged += AlCambiarEstado; }
    void OnDisable() { if (poke != null) poke.WhenStateChanged -= AlCambiarEstado; }

    void AlCambiarEstado(InteractableStateChangeArgs a)
    {
        bool antesEncima = encima;
        encima = a.NewState == InteractableState.Hover || a.NewState == InteractableState.Select;
        if (encima && !antesEncima) ManosRig.Vibrar(transform.position, 0.15f, 0.02f);   // "estás sobre el botón"
        if (a.NewState == InteractableState.Select) { seleccionado = true; Pulsar(); }
        else if (a.PreviousState == InteractableState.Select)
        {
            seleccionado = false;
            Sonidos.Soltar(transform.position);
        }
    }

    // Rayo de la mano vacía: pulsación corta (los botones de mantener, como ROTAR, giran un poco).
    public override void Activar()
    {
        hastaRayo = Time.time + 0.4f;
        Pulsar();
    }

    void Pulsar()
    {
        if (Time.time - ultimo < 0.2f) return;
        ultimo = Time.time;
        destello = 1f;
        verde = 1f;
        PonerVerde(true);
        if (chispas != null) chispas.Emit(18);
        ManosRig.Vibrar(transform.position, 0.6f, 0.06f);
        Sonidos.Clic(transform.position);
        Pulsado?.Invoke();
    }

    // Para los botones que alternan: el LED queda encendido mientras la función está activa.
    public void MarcarActivo(bool activo)
    {
        Activo = activo;
        PintarLed();
        PonerVerde(activo || verde > 0f);
    }

    // Cambia los colores de reposo y "encima" de la tapa del poke (el de pulsar ya es verde) y fuerza
    // al InteractableColorVisual a aplicarlos (lo hace al reactivarse).
    void PonerVerde(bool si)
    {
        if (colorTapa == null || si == tapaVerde) return;
        tapaVerde = si;
        var encimaReposo = Color.Lerp(colorReposo * 1.8f, Color.white, 0.2f); encimaReposo.a = 1f;
        colorTapa.InjectOptionalNormalColorState(new InteractableColorVisual.ColorState { Color = si ? TapaVerde : colorReposo, ColorTime = si ? 0.05f : 0.4f });
        colorTapa.InjectOptionalHoverColorState(new InteractableColorVisual.ColorState { Color = si ? Color.Lerp(TapaVerde, Color.white, 0.25f) : encimaReposo, ColorTime = 0.08f });
        if (colorTapa.isActiveAndEnabled) { colorTapa.enabled = false; colorTapa.enabled = true; }
    }

    void PintarLed()
    {
        if (led == null) return;
        led.GetPropertyBlock(mpb);
        var c = Activo ? LedEncendido : LedApagado;
        mpb.SetColor("_Color", c);
        mpb.SetColor("_EmissionColor", Activo ? c * 2f : Color.black);
        led.SetPropertyBlock(mpb);
    }

    void Update()
    {
        if (poke == null) Respaldo();

        // Tapa verde ~1 s tras pulsar (o mientras la función esté activa) y halo que se expande y se apaga.
        verde = Mathf.MoveTowards(verde, 0f, Time.deltaTime / 1.1f);
        if (verde <= 0f && tapaVerde && !Activo && !seleccionado) PonerVerde(false);
        if (halo != null)
        {
            float a = Mathf.Max(verde, Activo ? 0.35f : 0f);
            halo.enabled = a > 0.01f;
            if (halo.enabled)
            {
                halo.transform.localScale = new Vector3(escalaHalo.x * (1f + 0.5f * (1f - verde) * (verde > 0f ? 1f : 0f)), escalaHalo.y, escalaHalo.z * (1f + 0.5f * (1f - verde) * (verde > 0f ? 1f : 0f)));
                halo.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", new Color(0.35f, 1f, 0.55f, 1f) * (0.4f + a));
                halo.SetPropertyBlock(mpb);
            }
        }

        // Etiqueta: se ilumina al acercarse, verde al pulsar, y destella un instante.
        destello = Mathf.MoveTowards(destello, 0f, Time.deltaTime * 4f);
        if (etiqueta != null)
        {
            var c = Presionado || verde > 0.3f || Activo ? Pulsando : encima ? Encima : Reposo;
            etiqueta.color = Color.Lerp(etiqueta.color, c, Time.deltaTime * 15f);
            etiqueta.transform.localScale = escalaEtiqueta * (1f + 0.18f * destello);
        }
    }

    // Sin poke: pulsar al acercar la mano y hundir la tapa a mano.
    void Respaldo()
    {
        var punto = transform.TransformPoint(new Vector3(0f, 0.02f, 0f));
        bool cerca = ManosRig.MasCercana(punto, radio, out _);
        encima = ManosRig.MasCercana(punto, radio * 2f, out _);
        if (cerca && !seleccionado) Pulsar();
        seleccionado = cerca;
        if (tapa == null) return;
        var objetivo = tapaPos - Vector3.up * (Presionado ? 0.006f : 0f);
        tapa.localPosition = Vector3.Lerp(tapa.localPosition, objetivo, Time.deltaTime * 20f);
    }
}
