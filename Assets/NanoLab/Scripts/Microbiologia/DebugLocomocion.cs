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
        Comprobar(Mathf.Abs(vmax - jugador.velocidad) < 0.15f, $"Velocidad de paseo {vmax:F2} m/s (objetivo ~{jugador.velocidad:F1})");
        Comprobar(t90 <= 0.2f, $"Velocidad constante sin rampa (RNF-01): 90% de la velocidad en {t90:F2} s");
        Comprobar(frenado <= 0.25f, $"Frenado inmediato: parado en {frenado:F2} s");

        // Mirar por el microscopio pausa caminar y girar.
        jugador.PausarLocomocion(true);
        bool pausado = FindObjectsOfType<SlideLocomotionBroadcaster>().All(s => !s.enabled) && FindObjectsOfType<TurnerEventBroadcaster>().All(s => !s.enabled);
        jugador.PausarLocomocion(false);
        Comprobar(pausado, "Al mirar por el microscopio se pausa la locomoción");

        yield return Accesibilidad(jugador, fpl, personaje);
        Fin();
    }

    // RNF-07: volumen de música y efectos, modo sentado, panel ☰ y sillas en las que sentarse.
    IEnumerator Accesibilidad(JugadorNanoLab jugador, FirstPersonLocomotor fpl, Oculus.Interaction.Locomotion.CharacterController personaje)
    {
        AjustesNanoLab.VolumenEfectos = 0f;
        Sonidos.Clic(Vector3.zero);
        bool mudo = Sonidos.UltimoVolumen == 0f;
        AjustesNanoLab.VolumenEfectos = 0.5f;
        Sonidos.Clic(Vector3.zero);
        Comprobar(mudo && Mathf.Abs(Sonidos.UltimoVolumen - 0.25f) < 0.001f, $"Volumen de efectos: 0 % silencia y 50 % reduce a la mitad ({Sonidos.UltimoVolumen:F2})");

        var musica = FindObjectOfType<MusicaAmbiente>();
        if (musica != null)
        {
            AjustesNanoLab.VolumenMusica = 0f;
            yield return new WaitForSeconds(1.2f);
            var fuente = musica.GetComponent<AudioSource>();
            Comprobar(fuente.volume < 0.01f, $"Volumen de música al 0 %: la música baja a {fuente.volume:F3}");
        }
        else log.AppendLine("(sin música en la escena)");

        var menu = MenuAjustes.Instancia;
        Comprobar(menu != null && menu.panel != null && menu.botones != null && menu.botones.Length == 7 && !menu.Abierto,
                  $"Panel ☰ de ajustes montado en el rig ({(menu != null && menu.botones != null ? menu.botones.Length : 0)} botones), cerrado al empezar");
        if (menu != null && menu.panel != null)
        {
            menu.Abrir(true);
            bool abierto = menu.Abierto;
            AjustesNanoLab.VolumenMusica = 1f;
            menu.Pulsar("musica-");
            menu.Pulsar("musica-");
            bool baja = Mathf.Abs(AjustesNanoLab.VolumenMusica - 0.8f) < 0.001f;
            menu.Pulsar("cerrar");
            Comprobar(abierto && baja && !menu.Abierto, "El panel se abre, sus botones cambian el volumen (100 % > 80 %) y CERRAR lo cierra");
        }

        // Modo sentado: sin visor la cabeza se toma a 1.15 m, así que la vista sube 0.45 m.
        AjustesNanoLab.ModoSentado = false;
        jugador.CalibrarSentado();
        AjustesNanoLab.ModoSentado = true;
        yield return null;
        Comprobar(Mathf.Abs(fpl.HeightOffset - (jugador.alturaDePie - 1.15f)) < 0.01f, $"Modo sentado: la vista sube {fpl.HeightOffset:F2} m hasta la altura de alguien de pie");
        AjustesNanoLab.ModoSentado = false;
        yield return null;
        Comprobar(Mathf.Abs(fpl.HeightOffset) < 0.001f, "Sin modo sentado no hay desplazamiento de altura");

        // Sentarse en la silla más cercana a un microscopio de puesto y levantarse.
        var sillas = FindObjectsOfType<Silla>();
        var micros = FindObjectsOfType<MicroscopioOptico>().Where(m => !m.modoReto).ToArray();
        var silla = sillas.OrderBy(s => micros.Length == 0 ? 0f : micros.Min(m => Vector3.Distance(m.transform.position, s.transform.position))).FirstOrDefault();
        Comprobar(silla != null && silla.asiento != null && silla.salida != null, $"Hay {sillas.Length} sillas en las que sentarse");
        if (silla == null) yield break;
        float distMicro = micros.Length == 0 ? -1f : micros.Min(m => Vector3.Distance(m.transform.position, silla.transform.position));
        // En batchmode no corre el fin de frame en el que el rig sigue a la cápsula: se emula, porque si no
        // el Locomotor devuelve la cápsula hacia la cabeza (que no se movió) en el siguiente Update.
        var finDeFrame = gameObject.AddComponent<FinDeFrame>();
        finDeFrame.locomotor = fpl;
        fpl.ResetPlayerToCharacter();   // la prueba de velocidad dejó la cápsula 3 m por delante del rig
        yield return null;
        silla.Activar();   // lo que hace el rayo
        for (int i = 0; i < 5; i++) yield return null;
        var asiento = silla.PuntoAsiento;
        float dx = personaje != null ? Vector2.Distance(new Vector2(personaje.transform.position.x, personaje.transform.position.z), new Vector2(asiento.x, asiento.z)) : 99f;
        float esperado = asiento.y + silla.ojosSobreAsiento - silla.transform.position.y - jugador.alturaDePie;
        bool solidosApagados = silla.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).All(c => !c.enabled);
        Comprobar(JugadorNanoLab.Sentado(out var s1) && s1 == silla && jugador.LocomocionPausada && Mathf.Abs(fpl.HeightOffset - esperado) < 0.01f && dx < 0.2f && solidosApagados,
                  $"Sentado en '{silla.name}' (a {distMicro:F2} m del microscopio): cuerpo a {dx:F2} m del asiento, ojos a {asiento.y + silla.ojosSobreAsiento - silla.transform.position.y:F2} m, locomoción pausada");
        yield return null;
        jugador.Levantarse();
        yield return new WaitForSeconds(0.4f);
        var salida = silla.PuntoSalida;
        float dsal = personaje != null ? Vector2.Distance(new Vector2(personaje.transform.position.x, personaje.transform.position.z), new Vector2(salida.x, salida.z)) : 99f;
        bool solidos = silla.GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).All(c => c.enabled);
        Comprobar(!JugadorNanoLab.Sentado(out _) && !jugador.LocomocionPausada && Mathf.Abs(fpl.HeightOffset) < 0.001f && dsal < 0.25f && solidos,
                  $"Al levantarse queda detrás de la silla (a {dsal:F2} m de la salida), altura normal y locomoción activa");

        Destroy(finDeFrame);

        // Deja los ajustes como estaban por defecto.
        AjustesNanoLab.VolumenMusica = 1f; AjustesNanoLab.VolumenEfectos = 1f; AjustesNanoLab.AlturaSentado = 0f;
    }

    // Llama al LastUpdate del Locomotor (lo que hace su corrutina de fin de frame en el visor).
    [DefaultExecutionOrder(32000)]
    class FinDeFrame : MonoBehaviour
    {
        public FirstPersonLocomotor locomotor;
        static readonly MethodInfo Ultimo = typeof(FirstPersonLocomotor).GetMethod("LastUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
        void LateUpdate() { if (locomotor != null && Ultimo != null) Ultimo.Invoke(locomotor, null); }
    }


    void Fin()
    {
        Correcto = !fallo;
        Informe = log.ToString();
        Terminado = true;
        Debug.Log($"[PruebaLocomocion] {(Correcto ? "TODO CORRECTO" : "HAY FALLOS")}\n{Informe}");
    }
}
