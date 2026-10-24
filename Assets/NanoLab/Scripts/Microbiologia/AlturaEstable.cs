using UnityEngine;

// Cámara estable al caminar (RNF-01): el suelo del laboratorio es plano, así que la altura del
// origen del jugador se fija y la cápsula del locomotor no transmite saltitos al pasar por
// juntas, umbrales o rejillas. Solo corrige desviaciones pequeñas (no impide cambiar de nivel).
// Va en el OVRCameraRig (el _playerOrigin que mueve el FirstPersonLocomotor).
[DefaultExecutionOrder(10000)]   // después del locomotor, antes de dibujar
public class AlturaEstable : MonoBehaviour
{
    [Tooltip("Desviación máxima (m) que se corrige.")]
    public float margen = 0.3f;

    float altura;
    bool listo;

    // Espera a que el seguimiento a nivel de suelo y el locomotor se asienten.
    void Start() => Invoke(nameof(Fijar), 0.75f);

    void Fijar()
    {
        altura = transform.position.y;
        listo = true;
    }

    void LateUpdate()
    {
        if (!listo) return;
        var p = transform.position;
        if (Mathf.Abs(p.y - altura) > margen) { altura = p.y; return; }   // cambio de nivel real
        if (p.y != altura) transform.position = new Vector3(p.x, altura, p.z);
    }
}
