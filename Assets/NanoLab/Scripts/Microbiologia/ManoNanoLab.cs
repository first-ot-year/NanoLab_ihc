using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Marca la mano (el OVRGrabber de cada mando) para que los objetos sepan
// cuándo está cerca y puedan hacerla vibrar.
public class ManoNanoLab : MonoBehaviour
{
    public OVRInput.Controller controlador = OVRInput.Controller.RTouch;
    public static readonly List<ManoNanoLab> Todas = new List<ManoNanoLab>();

    void OnEnable() => Todas.Add(this);
    void OnDisable() => Todas.Remove(this);

    public void Vibrar(float fuerza = 0.5f, float duracion = 0.08f)
    {
        if (!isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(Vibracion(fuerza, duracion));
    }

    IEnumerator Vibracion(float fuerza, float duracion)
    {
        OVRInput.SetControllerVibration(1f, fuerza, controlador);
        yield return new WaitForSeconds(duracion);
        OVRInput.SetControllerVibration(0f, 0f, controlador);
    }

    public static ManoNanoLab MasCercana(Vector3 punto, float radio)
    {
        ManoNanoLab mejor = null;
        float d2 = radio * radio;
        foreach (var m in Todas)
        {
            float d = (m.transform.position - punto).sqrMagnitude;
            if (d < d2) { d2 = d; mejor = m; }
        }
        return mejor;
    }
}
