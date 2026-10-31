using UnityEngine;

// Cabina de descontaminación a la entrada de la sala BSL-3. La puerta de vidrio corredera está
// cerrada: el usuario se queda en el centro de la cabina, sale niebla de las boquillas con sonido de
// aire y, al terminar, suena un aviso y la puerta se abre. Desde dentro de la sala la puerta se abre
// al acercarse y, al pasar por la cabina, se rocía de nuevo (desinfección de salida, sin bloquear).
// Va en la raíz de la cabina (MontarSalaAltoRiesgo); todo en coordenadas del mundo.
public class CabinaDescontaminacion : MonoBehaviour
{
    public Transform hojaIzq, hojaDer;          // se deslizan hacia -x y +x
    public float apertura = 0.76f;
    [Tooltip("Centro de la puerta, a ras de suelo.")]
    public Vector3 puerta;
    [Tooltip("Interior de la cabina (donde hay que quedarse).")]
    public Bounds zona;
    [Tooltip("Hacia dónde queda la sala BSL-3 desde la puerta.")]
    public Vector3 haciaDentro = Vector3.forward;
    public ParticleSystem[] humo;
    public float duracion = 3.5f;

    enum Estado { Cerrada, Rociando, Abierta }
    Estado estado;
    bool salida, estuvoDentro, rocioSalidaHecho;
    float t, tEnCabina, tLejos, k;
    Vector3 izq0, der0;

    void Awake()
    {
        if (hojaIzq != null) izq0 = hojaIzq.localPosition;
        if (hojaDer != null) der0 = hojaDer.localPosition;
        Humo(false);
    }

    void Update()
    {
        var cabeza = ManosRig.Cabeza;
        if (cabeza != null) Logica(cabeza.position);
        k = Mathf.MoveTowards(k, estado == Estado.Abierta ? 1f : 0f, Time.deltaTime * 1.4f);
        float s = Mathf.SmoothStep(0f, 1f, k) * apertura;
        if (hojaIzq != null) hojaIzq.localPosition = izq0 + Vector3.left * s;
        if (hojaDer != null) hojaDer.localPosition = der0 + Vector3.right * s;
    }

    void Logica(Vector3 h)
    {
        var plano = new Vector3(h.x, zona.center.y, h.z);
        bool enCabina = zona.Contains(plano);
        float lado = Vector3.Dot(h - puerta, haciaDentro);            // > 0: dentro de la BSL-3
        float lateral = Vector3.Dot(h - puerta, Vector3.Cross(Vector3.up, haciaDentro));
        bool dentro = lado > 0.1f;
        bool cercaDentro = dentro && lado < 1.5f && Mathf.Abs(lateral) < 1.5f;
        bool cerca = enCabina || (Mathf.Abs(lado) < 1.6f && Mathf.Abs(lateral) < 1.5f);
        if (dentro) estuvoDentro = true;
        else if (!enCabina && lado < -0.2f && !cerca) { estuvoDentro = false; rocioSalidaHecho = false; }

        switch (estado)
        {
            case Estado.Cerrada:
                if (cercaDentro) { Abrir(false); break; }
                tEnCabina = enCabina ? tEnCabina + Time.deltaTime : 0f;
                if (tEnCabina > 0.6f && !(estuvoDentro && rocioSalidaHecho)) Rociar(estuvoDentro);
                break;

            case Estado.Rociando:
                t += Time.deltaTime;
                if (!salida && !enCabina)
                {
                    estado = Estado.Cerrada; tEnCabina = 0f;
                    Humo(false);
                    Avisar("Descontaminación interrumpida.\nVuelve al centro de la cabina y quédate quieto.", Aviso.Tipo.Error);
                    Sonidos.Error(puerta);
                }
                else if (t >= duracion)
                {
                    Humo(false);
                    if (salida) { estado = Estado.Cerrada; Avisar("Desinfección de salida completa.", Aviso.Tipo.Ok); }
                    else Abrir(true);
                }
                break;

            case Estado.Abierta:
                // Saliendo de la sala: al pasar por la cabina se cierra la puerta y se rocía.
                if (estuvoDentro && enCabina && !rocioSalidaHecho && lado < -0.35f) { Rociar(true); break; }
                tLejos = cerca ? 0f : tLejos + Time.deltaTime;
                if (tLejos > 1.5f) { estado = Estado.Cerrada; tEnCabina = 0f; Sonidos.Puerta(puerta, false); }
                break;
        }
    }

    void Rociar(bool deSalida)
    {
        estado = Estado.Rociando;
        salida = deSalida;
        if (deSalida) rocioSalidaHecho = true;
        t = 0f;
        Humo(true);
        Sonidos.Rocio(zona.center, duracion);
        ManosRig.Vibrar(zona.center, 0.25f, 0.3f);
        Avisar(deSalida ? "Desinfección de salida…" : "DESCONTAMINACIÓN EN CURSO\nQuédate quieto en la cabina.", Aviso.Tipo.Info);
    }

    void Abrir(bool trasRociar)
    {
        estado = Estado.Abierta;
        tLejos = 0f;
        Sonidos.Puerta(puerta, true);
        if (trasRociar)
        {
            Sonidos.Etapa(puerta);
            Avisar("Descontaminación completa.\nPuedes pasar.", Aviso.Tipo.Ok);
        }
    }

    void Humo(bool si)
    {
        if (humo == null) return;
        foreach (var p in humo)
        {
            if (p == null) continue;
            if (si) p.Play(); else p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // El aviso aparece delante de la cara (dentro de la cabina no se ve bien un cartel fijo).
    static void Avisar(string texto, Aviso.Tipo tipo)
    {
        var c = ManosRig.Cabeza;
        if (c == null) return;
        var f = Vector3.ProjectOnPlane(c.forward, Vector3.up).normalized;
        Aviso.Mostrar(texto, c.position + f * 0.9f, tipo, 3f);
    }
}
