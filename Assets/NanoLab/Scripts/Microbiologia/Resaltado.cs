using UnityEngine;

// Enciende un brillo alrededor del objeto cuando una mano está cerca
// (affordance: "esto se puede coger / tocar").
public class Resaltado : MonoBehaviour
{
    public Renderer[] brillos;
    public float radio = 0.14f;
    OVRGrabbable grab;
    bool encendido = true;

    void Awake()
    {
        grab = GetComponent<OVRGrabbable>();
        Poner(false);
    }

    void Update()
    {
        bool cerca = ManoNanoLab.MasCercana(transform.position, radio) != null;
        if (grab != null && grab.isGrabbed) cerca = false;
        if (cerca != encendido) Poner(cerca);
    }

    void Poner(bool v)
    {
        encendido = v;
        if (brillos == null) return;
        foreach (var r in brillos) if (r != null) r.enabled = v;
    }
}
