using Oculus.Interaction.Input;
using UnityEngine;

// Envuelve el joystick de caminar: si se empuja casi recto (adelante/atrás o a un lado), anula
// el pequeño componente perpendicular para avanzar en línea recta y no "haciendo eses" (RNF-01).
// Lo instala JugadorNanoLab entre el joystick y el SlideLocomotionBroadcaster de Meta.
public class EjeEstable : MonoBehaviour, IAxis2D
{
    [Tooltip("Si el eje menor es menor que esta fracción del mayor, se anula (≈ 24° a cada lado).")]
    [Range(0f, 1f)] public float umbralRecto = 0.45f;

    public IAxis2D original;

    public Vector2 Value()
    {
        if (original == null) return Vector2.zero;
        var v = original.Value();
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y);
        if (ax < ay * umbralRecto) v.x = 0f;
        else if (ay < ax * umbralRecto) v.y = 0f;
        return v;
    }
}
