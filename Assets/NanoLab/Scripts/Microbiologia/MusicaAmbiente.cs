using UnityEngine;

// Música ambiental de fondo (bucle sin cortes, sintetizado para el proyecto: sin licencias de terceros).
// Entra con un fundido, baja mientras se mira por un microscopio (para concentrarse en la muestra)
// y sigue sonando al cambiar de escena por el portal (una sola instancia).
[RequireComponent(typeof(AudioSource))]
public class MusicaAmbiente : MonoBehaviour
{
    public AudioClip musica;
    [Range(0f, 1f)] public float volumen = 0.22f;
    [Tooltip("Fracción del volumen mientras se mira por el ocular.")]
    [Range(0f, 1f)] public float atenuacionMicroscopio = 0.45f;
    public float fundidoEntrada = 4f;

    static MusicaAmbiente instancia;
    AudioSource fuente;
    float inicio;

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        transform.SetParent(null, true);   // DontDestroyOnLoad solo vale para objetos raíz (AgruparLab lo mete en LAB_)
        DontDestroyOnLoad(gameObject);
        fuente = GetComponent<AudioSource>();
        fuente.clip = musica;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;      // 2D: es música, no sale de ningún objeto
        fuente.priority = 0;
        fuente.volume = 0f;
        inicio = Time.unscaledTime;
        if (musica != null) fuente.Play();
    }

    void Update()
    {
        if (fuente == null) return;
        float entrada = fundidoEntrada > 0f ? Mathf.Clamp01((Time.unscaledTime - inicio) / fundidoEntrada) : 1f;
        float objetivo = volumen * entrada * (MicroscopioOptico.AlgunoActivo ? atenuacionMicroscopio : 1f);
        fuente.volume = Mathf.MoveTowards(fuente.volume, objetivo, Time.unscaledDeltaTime * 0.25f);
    }
}
