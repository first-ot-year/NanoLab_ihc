using UnityEngine;

// Algo que se usa apuntando con el rayo de la mano vacía y pulsando el gatillo o A/X
// (fichas de la biblioteca, botones del panel ☰, sillas). Lo detecta PunteroMuestras;
// necesita un collider en el objeto o en sus hijos.
public abstract class Apuntable : MonoBehaviour
{
    [Tooltip("Alcance máximo del rayo para este objeto (m).")]
    public float alcanceRayo = 2f;

    // Verbo que se muestra en el aviso: "Gatillo o A/X: {Accion}".
    public abstract string Accion { get; }

    public virtual bool Disponible => isActiveAndEnabled;

    public abstract void Activar();
}
