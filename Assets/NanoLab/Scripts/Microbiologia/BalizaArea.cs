using UnityEngine;

// Rótulo de un área del laboratorio (orientación, RF-01): mira al jugador (solo en Y) y crece con
// la distancia para que se lea igual desde lejos que desde cerca (tamaño angular casi constante).
public class BalizaArea : MonoBehaviour
{
    public Transform rotulo;
    [Tooltip("Distancia (m) a la que el rótulo tiene su tamaño original.")]
    public float distanciaBase = 4f;
    public float escalaMax = 3f;

    // Solo se ve el rótulo de arriba: el anillo del suelo y el haz que subía hasta él se ocultan
    // (también en escenas montadas antes de quitarlos del constructor).
    void Awake()
    {
        foreach (Transform h in transform)
            if (h != rotulo && (h.name == "Haz" || h.name == "Anillo")) h.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        var cab = ManosRig.Cabeza;
        if (cab == null || rotulo == null) return;
        var d = rotulo.position - cab.position;
        float s = Mathf.Clamp(d.magnitude / distanciaBase, 1f, escalaMax);
        rotulo.localScale = Vector3.one * s;
        d.y = 0f;
        if (d.sqrMagnitude > 1e-4f) rotulo.rotation = Quaternion.LookRotation(d);
    }
}
