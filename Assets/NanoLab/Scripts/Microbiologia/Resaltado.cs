using UnityEngine;

// Enciende un brillo alrededor de un tubo/placa/vaso cuando una mano del rig está cerca
// (affordance: "toca aquí con el portaobjetos").
public class Resaltado : MonoBehaviour
{
    public Renderer[] brillos;
    public float radio = 0.15f;
    bool encendido = true;

    void Awake() => Poner(false);

    void Update()
    {
        bool cerca = ManosRig.MasCercana(transform.position, radio, out _);
        if (cerca != encendido) Poner(cerca);
    }

    void Poner(bool v)
    {
        encendido = v;
        if (brillos == null) return;
        foreach (var r in brillos) if (r != null) r.enabled = v;
    }
}
