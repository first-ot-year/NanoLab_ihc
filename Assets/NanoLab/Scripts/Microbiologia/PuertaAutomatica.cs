using UnityEngine;

// Puerta corredera automática (la entrada al Centro de Experimentación): sus hojas se abren solas cuando
// la cabeza del jugador se acerca y se cierran al alejarse, como las de un laboratorio real. Mientras el
// jugador está en el umbral nunca se cierra. También se abre si hay un objeto agarrable cerca de ella
// (alguien que pasa llevando una placa o un portaobjetos), para que no choque con la hoja.
public class PuertaAutomatica : MonoBehaviour
{
    public PuertaCorrediza[] hojas;
    [Tooltip("Mitad del tamaño de la zona de detección, en local (x: a ambos lados de la pared; z: a lo ancho).")]
    public Vector3 zona = new Vector3(1.6f, 1.5f, 1.4f);
    [Tooltip("Segundos que sigue abierta después de salir de la zona.")]
    public float esperaCierre = 1.2f;
    public GameObject[] luzAbierta, luzCerrada;

    float ultimaPresencia = -100f;
    bool abierta;

    void Start() => Poner(false);

    void Update()
    {
        var cab = ManosRig.Cabeza;
        bool presente = cab != null && Dentro(cab.position);
        if (!presente)
        {
            // Objetos que se llevan en la mano (o que el rayo trae) cerca de la hoja.
            foreach (var p in Portaobjetos.Todos) if (p != null && p.Agarrado && Dentro(p.transform.position)) { presente = true; break; }
            if (!presente) foreach (var e in Encajable.Todos) if (e != null && e.Agarrado && Dentro(e.transform.position)) { presente = true; break; }
        }
        if (presente) ultimaPresencia = Time.time;
        bool abrir = Time.time - ultimaPresencia < esperaCierre;
        if (abrir != abierta) Poner(abrir);
    }

    bool Dentro(Vector3 mundo)
    {
        var l = transform.InverseTransformPoint(mundo);
        return Mathf.Abs(l.x) < zona.x && l.y > -0.2f && l.y < zona.y * 2f && Mathf.Abs(l.z) < zona.z;
    }

    void Poner(bool abrir)
    {
        abierta = abrir;
        if (hojas != null) foreach (var h in hojas) if (h != null) h.Abrir(abrir);
        if (luzAbierta != null) foreach (var l in luzAbierta) if (l) l.SetActive(abrir);
        if (luzCerrada != null) foreach (var l in luzCerrada) if (l) l.SetActive(!abrir);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.35f);
        Gizmos.DrawCube(new Vector3(0f, zona.y - 0.1f, 0f), new Vector3(zona.x * 2f, zona.y * 2f + 0.2f, zona.z * 2f));
    }
}
