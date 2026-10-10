using UnityEngine;

// Zona donde encaja un objeto agarrable de un tipo concreto (placa, vial, cápsula).
// Muestra una silueta que pulsa mientras el usuario lleva en la mano un objeto de ese tipo.
public class ZonaEncaje : MonoBehaviour
{
    public string tipo = "placa";
    public Transform anclaje;
    public Renderer fantasma;
    [Tooltip("Si es falso solo acepta objetos colocados por código (p. ej. la salida de la prensa).")]
    public bool aceptaSoltar = true;

    public Encajable Actual { get; private set; }
    public bool Libre => Actual == null;
    public event System.Action<Encajable> Cambio;

    public void Recibir(Encajable e) { Actual = e; Cambio?.Invoke(e); }

    public void Liberar(Encajable e)
    {
        if (Actual != e) return;
        Actual = null;
        Cambio?.Invoke(null);
    }

    void Update()
    {
        if (fantasma != null) fantasma.enabled = aceptaSoltar && Libre && Encajable.AlgunoAgarrado(tipo);
    }
}
