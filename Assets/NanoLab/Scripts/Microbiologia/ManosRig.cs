using System.Collections;
using UnityEngine;

// Acceso a la cabeza y las manos del rig de Meta (OVRCameraRigInteraction / OVRCameraRig)
// y vibración del mando más cercano. No agarra nada: el agarre lo hace el Interaction SDK.
public static class ManosRig
{
    static OVRCameraRig rig;

    static OVRCameraRig Rig
    {
        get
        {
            if (rig == null) rig = Object.FindObjectOfType<OVRCameraRig>();
            return rig;
        }
    }

    public static Transform Cabeza => Rig != null ? Rig.centerEyeAnchor : (Camera.main != null ? Camera.main.transform : null);

    // Mano (mando o mano rastreada) más cercana a un punto, dentro de un radio.
    public static bool MasCercana(Vector3 punto, float radio, out OVRInput.Controller controlador)
    {
        controlador = OVRInput.Controller.None;
        var r = Rig;
        if (r == null) return false;
        float d2 = radio * radio;
        if (r.leftHandAnchor != null)
        {
            float d = (r.leftHandAnchor.position - punto).sqrMagnitude;
            if (d < d2) { d2 = d; controlador = OVRInput.Controller.LTouch; }
        }
        if (r.rightHandAnchor != null)
        {
            float d = (r.rightHandAnchor.position - punto).sqrMagnitude;
            if (d < d2) { d2 = d; controlador = OVRInput.Controller.RTouch; }
        }
        return controlador != OVRInput.Controller.None;
    }

    public static void Vibrar(Vector3 cercaDe, float fuerza = 0.5f, float duracion = 0.08f)
    {
        if (MasCercana(cercaDe, 0.8f, out var c)) Vibrador.Instancia.Pulso(c, fuerza, duracion);
    }

    class Vibrador : MonoBehaviour
    {
        static Vibrador instancia;
        public static Vibrador Instancia
        {
            get
            {
                if (instancia == null)
                {
                    var go = new GameObject("VibradorMandos") { hideFlags = HideFlags.HideInHierarchy };
                    instancia = go.AddComponent<Vibrador>();
                }
                return instancia;
            }
        }

        public void Pulso(OVRInput.Controller c, float fuerza, float duracion) => StartCoroutine(Rutina(c, fuerza, duracion));

        IEnumerator Rutina(OVRInput.Controller c, float fuerza, float duracion)
        {
            OVRInput.SetControllerVibration(1f, fuerza, c);
            yield return new WaitForSeconds(duracion);
            OVRInput.SetControllerVibration(0f, 0f, c);
        }
    }
}
