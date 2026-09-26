using UnityEngine;

// Mantiene un cartel/etiqueta mirando hacia el jugador (solo gira en Y).
public class Cartel : MonoBehaviour
{
    Transform cam;

    void LateUpdate()
    {
        if (cam == null)
        {
            var c = Camera.main;
            if (c == null) return;
            cam = c.transform;
        }
        var d = transform.position - cam.position;
        d.y = 0f;
        if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d);
    }
}
