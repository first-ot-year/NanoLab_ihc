using UnityEngine;

// Zona donde se encaja el portaobjetos. Muestra una silueta que pulsa
// mientras el usuario lleva un portaobjetos en la mano y la platina está libre.
public class Platina : MonoBehaviour
{
    public Transform anclaje;
    public Renderer fantasma;

    public Portaobjetos Actual { get; private set; }
    public bool Libre => Actual == null;
    public event System.Action<Portaobjetos> Cambio;

    public void Recibir(Portaobjetos p) { Actual = p; Cambio?.Invoke(p); }

    public void Liberar(Portaobjetos p)
    {
        if (Actual != p) return;
        Actual = null;
        Cambio?.Invoke(null);
    }

    void Update()
    {
        if (fantasma != null) fantasma.enabled = Libre && Portaobjetos.AlgunoAgarrado;
    }
}
