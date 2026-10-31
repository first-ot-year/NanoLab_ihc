using UnityEngine;

// Puerta corredera hermética de la cámara de descontaminación (CamaraDescontaminacion la abre y la
// cierra). La hoja lleva un Rigidbody cinemático y un collider sólido: cerrada, el jugador no pasa.
// Semáforo junto a la puerta: rojo = bloqueada, verde = abierta / se puede pasar (con texto en las
// pantallas: no depende solo del color, RNF-03).
public class PuertaCorrediza : MonoBehaviour
{
    public Transform hoja;
    [Tooltip("Posición local de la hoja cerrada y abierta.")]
    public Vector3 cerrada, abierta;
    [Tooltip("Segundos que tarda en abrirse o cerrarse.")]
    public float duracion = 1.1f;
    public GameObject[] luzRoja, luzVerde;
    [Tooltip("Suena al abrirse o cerrarse (en una puerta de dos hojas basta con que suene una).")]
    public bool sonido = true;

    float t, objetivo;
    Rigidbody rb;

    public bool Abierta => t > 0.98f;
    public bool Cerrada => t < 0.02f;
    public bool Abriendo => objetivo > 0.5f;

    void Awake()
    {
        if (hoja != null) rb = hoja.GetComponent<Rigidbody>();
        t = objetivo = 0f;
        Poner();
        Semaforo(false);
    }

    public void Abrir(bool abrir)
    {
        float nuevo = abrir ? 1f : 0f;
        if (Mathf.Approximately(nuevo, objetivo)) return;
        objetivo = nuevo;
        Semaforo(abrir);
        if (sonido && hoja != null) Sonidos.Puerta(hoja.position + Vector3.up * 1.2f);
    }

    public void Semaforo(bool verde)
    {
        if (luzRoja != null) foreach (var l in luzRoja) if (l) l.SetActive(!verde);
        if (luzVerde != null) foreach (var l in luzVerde) if (l) l.SetActive(verde);
    }

    void Update()
    {
        if (Mathf.Approximately(t, objetivo)) return;
        t = Mathf.MoveTowards(t, objetivo, Time.deltaTime / Mathf.Max(0.05f, duracion));
        Poner();
    }

    void Poner()
    {
        if (hoja == null) return;
        var local = Vector3.Lerp(cerrada, abierta, Mathf.SmoothStep(0f, 1f, t));
        if (rb != null && Application.isPlaying)
            rb.MovePosition(hoja.parent != null ? hoja.parent.TransformPoint(local) : local);
        else hoja.localPosition = local;
    }
}
