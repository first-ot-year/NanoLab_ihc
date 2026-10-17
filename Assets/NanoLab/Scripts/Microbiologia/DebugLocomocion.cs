using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using Oculus.Interaction.Locomotion;
using UnityEngine;

// Prueba sin visor de la locomoción de Meta: comprueba la configuración (qué mando camina y cuál
// gira, giro de 30°, sin teletransporte, cápsula) e inyecta "joystick a fondo" en el
// FirstPersonLocomotor para medir la curva de velocidad (arranque, velocidad de paseo y frenado).
public class DebugLocomocion : MonoBehaviour
{
    public static bool Terminado { get; private set; }
    public static bool Correcto { get; private set; }
    public static string Informe { get; private set; }

    readonly StringBuilder log = new StringBuilder();
    bool fallo;

    void Comprobar(bool c, string s)
    {
        log.AppendLine((c ? "OK    " : "FALLO ") + s);
        if (!c) { fallo = true; Debug.LogError("[PruebaLocomocion] FALLO: " + s); }
        else Debug.Log("[PruebaLocomocion] " + s);
    }

    public IEnumerator Simular()
    {
        Terminado = false; fallo = false; log.Clear();
        yield return new WaitForSeconds(1f);
        var jugador = FindObjectOfType<JugadorNanoLab>();
        var fpl = FindObjectOfType<FirstPersonLocomotor>();
        var rig = FindObjectOfType<OVRCameraRig>();
        Comprobar(jugador != null && fpl != null && fpl.isActiveAndEnabled && rig != null, "Hay JugadorNanoLab y un FirstPersonLocomotor activo");
        if (fallo) { Fin(); yield break; }

        var conexiones = FindObjectsOfType<LocomotionEventsConnection>(true).Where(c => c.transform.parent != null && c.name.StartsWith("LocomotionController")).ToArray();
        var campo = typeof(LocomotionEventsConnection).GetField("_handler", BindingFlags.NonPublic | BindingFlags.Instance);
        Comprobar(conexiones.Length == 2 && conexiones.All(c => (Object)campo.GetValue(c) == fpl), $"Los {conexiones.Length} grupos de los mandos envían sus eventos al FirstPersonLocomotor");
        Comprobar(FindObjectsOfType<PlayerLocomotor>(true).All(p => !p.enabled), "El PlayerLocomotor heredado está desactivado");

        foreach (var g in conexiones)
        {
            bool izq = g.transform.GetComponentsInParent<Transform>(true).Any(p => p.name.StartsWith("Left"));
            bool Activo(string n) { var h = g.transform.Find(n); return h != null && h.gameObject.activeInHierarchy; }
            Comprobar(!Activo("TeleportControllerInteractor") && !Activo("ControllerStepInteractor"), $"{(izq ? "Mando izq." : "Mando der.")}: sin teletransporte ni pasos");
            Comprobar(Activo("ControllerSlideInteractor") == izq && Activo("ControllerTurnerInteractor") == !izq, $"{(izq ? "Mando izq.: camina" : "Mando der.: gira")}");
            if (!izq)
            {
                var tu = g.GetComponentInChildren<TurnerEventBroadcaster>();
                Comprobar(tu != null && tu.TurnMethod == TurnerEventBroadcaster.TurnMode.Snap && Mathf.Approximately(tu.SnapTurnDegrees, 30f), $"Giro por pasos de {(tu != null ? tu.SnapTurnDegrees : 0)}°");
            }
        }
        var cap = fpl.transform.root.GetComponentsInChildren<CapsuleCollider>(true).FirstOrDefault(c => c.GetComponentInParent<FirstPersonLocomotor>() != null || c.name.Contains("PlayerController"));
        Comprobar(cap != null && cap.radius <= 0.17f, $"Cápsula del cuerpo de {(cap != null ? cap.radius : 0):F2} m de radio");

        // Joystick a fondo durante 2 s y luego soltar: se mide la velocidad del personaje.
        // (El rig sigue a la cápsula al final de cada frame con WaitForEndOfFrame, que en batchmode
        //  no se ejecuta; por eso se mide la velocidad con la que se mueve la cápsula.)
        var origen = rig.transform;
        var personaje = FindObjectOfType<Oculus.Interaction.Locomotion.CharacterController>();
        var adelante = Vector3.ProjectOnPlane(origen.forward, Vector3.up).normalized;
        var muestras = new System.Collections.Generic.List<(float t, float v)>();
        float t0 = Time.time; bool diag = false;
        while (Time.time - t0 < 3f)
        {
            bool pulsado = Time.time - t0 < 2f;
            if (pulsado) fpl.HandleLocomotionEvent(new LocomotionEvent(0, new Pose(adelante, Quaternion.identity), LocomotionEvent.TranslationType.Velocity, LocomotionEvent.RotationType.None));
            yield return null;
            var v = fpl.Velocity; v.y = 0f;
            muestras.Add((Time.time - t0, v.magnitude));
            if (!diag && Time.time - t0 > 1f)
            {
                diag = true;

                log.AppendLine($"Diagnóstico a 1 s: en el suelo={fpl.IsGrounded}, velocidad ignorada={fpl.IgnoringVelocity}, cápsula {(personaje != null ? "presente" : "no encontrada")}");
            }
        }
        float vmax = muestras.Where(m => m.t < 2f).Select(m => m.v).DefaultIfEmpty(0f).Max();
        float t90 = muestras.FirstOrDefault(m => m.v >= vmax * 0.9f).t;
        var parado = muestras.FirstOrDefault(m => m.t > 2f && m.v < vmax * 0.1f);
        float frenado = parado.t > 0f ? parado.t - 2f : 99f;
        var curva = string.Join("  ", new[] { 0.05f, 0.1f, 0.2f, 0.3f, 0.5f, 1f, 1.9f, 2.1f, 2.2f, 2.4f, 2.8f }.Select(x => $"{x:F2}s:{muestras.OrderBy(m => Mathf.Abs(m.t - x)).First().v:F2}"));
        log.AppendLine("Curva de velocidad (m/s): " + curva);
        Comprobar(vmax > 0.9f && vmax < 1.5f, $"Velocidad de paseo {vmax:F2} m/s (objetivo ~{jugador.velocidad:F1})");
        Comprobar(t90 > 0.08f && t90 < 0.8f, $"Arranque suave: 90% de la velocidad en {t90:F2} s");
        Comprobar(frenado < 0.8f, $"Frenado suave: parado en {frenado:F2} s");

        // Mirar por el microscopio pausa caminar y girar.
        jugador.PausarLocomocion(true);
        bool pausado = FindObjectsOfType<SlideLocomotionBroadcaster>().All(s => !s.enabled) && FindObjectsOfType<TurnerEventBroadcaster>().All(s => !s.enabled);
        jugador.PausarLocomocion(false);
        Comprobar(pausado, "Al mirar por el microscopio se pausa la locomoción");
        Fin();
    }

    void Fin()
    {
        Correcto = !fallo;
        Informe = log.ToString();
        Terminado = true;
        Debug.Log($"[PruebaLocomocion] {(Correcto ? "TODO CORRECTO" : "HAY FALLOS")}\n{Informe}");
    }
}
