using UnityEngine;

// Puerta de vidrio, tapa o cajón que se abre apuntándolo con el rayo de la mano vacía (gatillo o A/X).
// Va en la pieza que se mueve: sus colliders son los que encuentra el rayo. Mientras está cerrado,
// lo guardado en sus huecos no se puede agarrar (Hueco desactiva los interactables del objeto).
public class Compartimento : Apuntable
{
    public enum Tipo { Bisagra, Corredera }

    public Tipo tipo = Tipo.Bisagra;
    [Tooltip("Bisagra: eje de giro en el espacio del padre.")]
    public Vector3 eje = Vector3.up;
    [Tooltip("Bisagra: grados que gira al abrirse.")]
    public float angulo = -105f;
    [Tooltip("Corredera: desplazamiento al abrirse, en el espacio del padre (m).")]
    public Vector3 recorrido = new Vector3(0f, 0f, 0.4f);
    [Tooltip("Cómo se nombra en el aviso: \"abrir {nombre}\".")]
    public string nombre = "la puerta";
    [Tooltip("Aviso la primera vez que se abre (qué hay dentro).")]
    [TextArea(2, 4)] public string avisoAlAbrir;
    public float velocidad = 2.2f;

    // Accesible a partir de media apertura (y en cuanto se manda cerrar deja de serlo).
    public bool Abierto => abrir && t > 0.5f;

    bool abrir, avisado;
    float t;
    Vector3 pos0;
    Quaternion rot0;

    void Awake()
    {
        pos0 = transform.localPosition;
        rot0 = transform.localRotation;
    }

    public override string Accion => (abrir ? "cerrar " : "abrir ") + nombre;

    public override void Activar()
    {
        abrir = !abrir;
        Sonidos.Puerta(transform.position, abrir);
        ManosRig.Vibrar(transform.position, 0.35f, 0.06f);
        if (abrir && !avisado && !string.IsNullOrEmpty(avisoAlAbrir))
        {
            avisado = true;
            Aviso.Mostrar(avisoAlAbrir, transform.position + Vector3.up * 0.25f, Aviso.Tipo.Info, 4f);
        }
    }

    void Update()
    {
        float objetivo = abrir ? 1f : 0f;
        if (Mathf.Approximately(t, objetivo)) return;
        t = Mathf.MoveTowards(t, objetivo, Time.deltaTime * velocidad);
        float k = Mathf.SmoothStep(0f, 1f, t);
        if (tipo == Tipo.Bisagra) transform.localRotation = Quaternion.AngleAxis(angulo * k, eje) * rot0;
        else transform.localPosition = pos0 + recorrido * k;
    }
}
