using TMPro;
using UnityEngine;

// Panel de ajustes (RNF-07): el botón ☰ del mando izquierdo lo abre en cualquier sala. Es una TABLET (carcasa con
// grosor y asa) delante del jugador; se pulsa con el índice derecho (poke de Meta) o con el rayo.
// Revisión del profesor ("¿por qué se queda estático? y no me para el juego"):
//  - PAUSA: mientras está abierto el juego se detiene (Time.timeScale = 0: cámara, incubación, guía, cronómetros y
//    métricas no avanzan). Los botones del menú usan tiempo sin escalar, así que siguen respondiendo.
//  - SIGUE AL JUGADOR con retardo ("lazy follow", como el menú del sistema de Quest): si se gira o se aparta, la tablet
//    vuelve suavemente delante de él en vez de quedarse atrás en el mundo.
// Volumen de la música y de los efectos, modo sentado (sube la vista a la de alguien de pie) y
// recalibrar la altura. Los botones se tocan o se apuntan con el rayo. Lo monta MontarAjustes en el rig.
public class MenuAjustes : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text musica, efectos, sentado, partida;
    public PanelRedondeado[] barraMusica, barraEfectos;
    public BotonMenu[] botones;
    [Tooltip("Distancia del panel a la cabeza al abrirlo (m).")]
    public float distancia = 0.55f;
    [Tooltip("Si la cabeza se aleja más que esto del panel (m), se cierra.")]
    public float distanciaCierre = 1.5f;
    [Tooltip("Sin uso: el panel vuelve a su tamaño real (antes era una tablet al 70 %).")]
    public float escalaTablet = 0.7f;

    Transform padreOriginal;   // el rig: la tablet vuelve a él al cerrarse
    float escalaTiempoAntes = 1f;
    bool pausado, siguiendo;
    Vector3 velSeguir;
    TMP_Text rotuloPausa;

    public static bool EnPausa => Instancia != null && Instancia.pausado;

    public static MenuAjustes Instancia { get; private set; }
    public bool Abierto => panel != null && panel.activeSelf;

    void Awake()
    {
        Instancia = this;
        if (panel == null) Debug.LogWarning("[Ajustes] Falta el panel: ejecuta 'NanoLab/Configurar interacción Meta'.");
        else panel.SetActive(false);
        if (botones != null) foreach (var b in botones) if (b != null) b.Pulsado += Pulsar;
    }

    void OnEnable() { AjustesNanoLab.Cambiado += Refrescar; Refrescar(); }
    void OnDisable() => AjustesNanoLab.Cambiado -= Refrescar;

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch) || OVRInput.GetDown(OVRInput.Button.Start) || TeclaMenu())
            Abrir(!Abierto);
        if (Abierto) Seguir();
    }

    // Lazy follow: la tablet se queda quieta mientras esté más o menos delante; si el jugador se gira (> 28°) o se
    // aleja/acerca (> 0,3 m del sitio ideal), vuelve suavemente delante de él. Con tiempo sin escalar (el juego está en pausa).
    void Seguir()
    {
        var cab = ManosRig.Cabeza;
        if (cab == null) return;
        Ideal(cab, out var pos, out var rot);
        var t = panel.transform;
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
        var haciaPanel = Vector3.ProjectOnPlane(t.position - cab.position, Vector3.up).normalized;
        float ang = Vector3.Angle(f, haciaPanel);
        float dist = Vector3.Distance(t.position, pos);
        if (!siguiendo && (ang > 28f || dist > 0.3f)) siguiendo = true;
        if (!siguiendo) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        t.position = Vector3.SmoothDamp(t.position, pos, ref velSeguir, 0.22f, 4f, dt);
        t.rotation = Quaternion.Slerp(t.rotation, rot, 1f - Mathf.Exp(-dt * 10f));
        if (Vector3.Distance(t.position, pos) < 0.02f) siguiendo = false;
    }

    // Delante de los ojos, un poco por debajo, vertical (como el panel de la versión anterior).
    void Ideal(Transform cab, out Vector3 pos, out Quaternion rot)
    {
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
        f.Normalize();
        pos = cab.position + f * distancia - Vector3.up * 0.12f;
        rot = Quaternion.LookRotation(f, Vector3.up);   // vertical, como en la versión anterior
    }

    void Pausar(bool si)
    {
        if (si == pausado) return;
        pausado = si;
        if (si) { escalaTiempoAntes = Time.timeScale > 0f ? Time.timeScale : 1f; Time.timeScale = 0f; }
        else Time.timeScale = escalaTiempoAntes;
        if (rotuloPausa != null) rotuloPausa.gameObject.SetActive(si);
    }

    void OnDestroy() { if (pausado) Time.timeScale = escalaTiempoAntes; if (Instancia == this) Instancia = null; }

    public void Abrir(bool abrir)
    {
        if (panel == null) return;
        if (abrir)
        {
            // Un solo panel delante de la cara: se cierran la ayuda y los avisos.
            if (AvisoInicio.Instancia != null && AvisoInicio.Instancia.Abierto) AvisoInicio.Instancia.CerrarAhora();
            Aviso.OcultarTodos();
            var cab = ManosRig.Cabeza;
            if (cab != null)
            {
                var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
                if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
                f.Normalize();
                panel.transform.position = cab.position + f * distancia - Vector3.up * 0.12f;
                panel.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            }
            Tablet();
            if (padreOriginal == null) padreOriginal = panel.transform.parent;
            panel.transform.SetParent(null, true);   // en el mundo (la mueve Seguir, no la cabeza directamente)
            Colocar();
            siguiendo = false;
            velSeguir = Vector3.zero;
            Refrescar();
        }
        else if (padreOriginal != null && panel.transform.parent != padreOriginal) panel.transform.SetParent(padreOriginal, true);
        panel.SetActive(abrir);
        Pausar(abrir);
        Sonidos.Clic(panel.transform.position);
    }

    public bool FijaEnElMundo => panel != null && panel.transform.parent == null;

    // Delante del jugador (antes salía sobre la mano izquierda y se quedaba quieta).
    void Colocar()
    {
        var cab = ManosRig.Cabeza;
        if (cab == null) return;
        Ideal(cab, out var pos, out var rot);
        panel.transform.SetPositionAndRotation(pos, rot);
    }

    // Carcasa con grosor y asa detrás del panel, y tamaño de tablet (se hace una vez, en tiempo de ejecución).
    // Como el panel de la versión anterior (letras blancas sobre fondo oscuro, tamaño real), sin la carcasa de
    // tablet: solo un marco celeste fino en los BORDES y el rótulo "JUEGO EN PAUSA" encima. Se hace una vez.
    void Tablet()
    {
        var t = panel.transform;
        t.localScale = Vector3.one;
        if (t.Find("Canto") != null) return;
        float ancho = 0.62f, alto = 0.86f, y = -0.15f;
        var fondo = t.Find("Fondo");
        var pr = fondo != null ? fondo.GetComponent<PanelRedondeado>() : null;
        if (pr != null) { ancho = pr.ancho; alto = pr.alto; y = fondo.localPosition.y; }
        var borde = Opaco(new Color(0.37f, 0.83f, 1f));
        const float g = 0.008f;   // ancho del marco
        var canto = new GameObject("Canto").transform;
        canto.SetParent(t, false);
        KitVacuna.Caja(canto, "Arriba", new Vector3(0f, y + alto / 2f + g / 2f, 0.002f), new Vector3(ancho + 2f * g, g, 0.004f), borde);
        KitVacuna.Caja(canto, "Abajo", new Vector3(0f, y - alto / 2f - g / 2f, 0.002f), new Vector3(ancho + 2f * g, g, 0.004f), borde);
        KitVacuna.Caja(canto, "Izq", new Vector3(-ancho / 2f - g / 2f, y, 0.002f), new Vector3(g, alto, 0.004f), borde);
        KitVacuna.Caja(canto, "Der", new Vector3(ancho / 2f + g / 2f, y, 0.002f), new Vector3(g, alto, 0.004f), borde);
        foreach (var c in canto.GetComponentsInChildren<Collider>(true)) Destroy(c);

        // "JUEGO EN PAUSA": copia del título AJUSTES (misma fuente y orden de dibujo), en blanco, encima del marco.
        TMP_Text titulo = null;
        foreach (var tx in t.GetComponentsInChildren<TMP_Text>(true)) if (tx.text == "AJUSTES") { titulo = tx; break; }
        if (titulo != null && rotuloPausa == null)
        {
            rotuloPausa = Instantiate(titulo.gameObject, t).GetComponent<TMP_Text>();
            rotuloPausa.name = "Pausa";
            rotuloPausa.text = "<b>II  JUEGO EN PAUSA</b>";
            rotuloPausa.color = Color.white;
            rotuloPausa.alignment = TextAlignmentOptions.Center;
            rotuloPausa.rectTransform.sizeDelta = new Vector2(ancho, 0.06f);
            rotuloPausa.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var lp = titulo.rectTransform.localPosition;
            rotuloPausa.rectTransform.localPosition = new Vector3(0f, y + alto / 2f + g + 0.035f, lp.z);
            rotuloPausa.gameObject.SetActive(pausado);
        }
    }

    static Material Opaco(Color c)
    {
        var sh = Shader.Find("Standard");
        var m = sh != null ? new Material(sh) : new Material(KitVacuna.Plano(c));
        m.color = c;
        m.renderQueue = 2000;   // siempre antes que los paneles transparentes: nunca los tapa
        return m;
    }

    public void Pulsar(string id)
    {
        switch (id)
        {
            case "musica-": AjustesNanoLab.VolumenMusica -= 0.1f; break;
            case "musica+": AjustesNanoLab.VolumenMusica += 0.1f; break;
            case "efectos-": AjustesNanoLab.VolumenEfectos -= 0.1f; break;
            case "efectos+": AjustesNanoLab.VolumenEfectos += 0.1f; break;
            case "sentado":
                if (!AjustesNanoLab.ModoSentado && JugadorNanoLab.Instancia != null) JugadorNanoLab.Instancia.CalibrarSentado();
                AjustesNanoLab.ModoSentado = !AjustesNanoLab.ModoSentado;
                break;
            case "calibrar":
                // Antes dejaba SIEMPRE el modo sentado activado: pulsarlo otra vez no bajaba nunca la vista.
                // Ahora solo vuelve a medir si el modo sentado ya está activo; si no, deja la vista a la altura real.
                if (AjustesNanoLab.ModoSentado && JugadorNanoLab.Instancia != null) JugadorNanoLab.Instancia.CalibrarSentado();
                if (JugadorNanoLab.Instancia != null) JugadorNanoLab.Instancia.AlSuelo();
                if (partida != null) partida.text = AjustesNanoLab.ModoSentado
                    ? "<color=#7CF29A><b>Altura recalibrada (modo sentado)</b></color>"
                    : "<color=#7CF29A><b>Vista a tu altura real. Para subirla, activa el MODO SENTADO</b></color>";
                Sonidos.Logro(panel.transform.position);
                return;
            case "guardar":
                bool ok = Partida.GuardarManual();
                if (partida != null) partida.text = ok ? "<color=#7CF29A><b>Partida guardada</b></color>" : "<color=#FF7A7A><b>No se pudo guardar. Inténtalo otra vez o libera espacio en el visor</b></color>";
                if (ok) Sonidos.Logro(panel.transform.position); else Sonidos.Error(panel.transform.position);
                return;
            case "checkpoint":
                // Volver al checkpoint descarta lo avanzado después: se confirma como NUEVA PARTIDA.
                if (Partida.UltimoCheckpoint != null && !Confirmacion.Pedir("checkpoint"))
                {
                    if (partida != null) partida.text = "<color=#FFD25A><b>¿Volver al checkpoint? Pierdes lo avanzado después. Pulsa otra vez</b></color>";
                    Sonidos.Pista(panel.transform.position);
                    return;
                }
                bool vuelto = GestorPartida.VolverAlCheckpoint(out var mensaje);
                if (partida != null) partida.text = vuelto ? $"<color=#7CF29A><b>{mensaje}</b></color>" : $"<color=#FFD25A><b>{mensaje}</b></color>";
                if (vuelto) { Sonidos.Logro(panel.transform.position); Abrir(false); }
                else Sonidos.Error(panel.transform.position);
                return;
            case "nueva":
                // Doble confirmación: se pierde el avance guardado.
                if (!Confirmacion.Pedir("nueva"))
                {
                    if (partida != null) partida.text = "<color=#FFD25A><b>¿Borrar todo el avance? Pulsa NUEVA PARTIDA otra vez</b></color>";
                    Sonidos.Pista(panel.transform.position);
                    return;
                }
                Partida.Borrar();
                Abrir(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene("Microbiologia");
                return;
            case "reiniciar":
                // Borra el avance y recarga la escena. Como NUEVA PARTIDA, pide una segunda pulsación (heurísticas 3 y 5).
                if (!Confirmacion.Pedir("reiniciar"))
                {
                    if (partida != null) partida.text = "<color=#FFD25A><b>¿Reiniciar la partida? Se borra todo el avance. Pulsa otra vez</b></color>";
                    Sonidos.Pista(panel.transform.position);
                    return;
                }
                Partida.Borrar();
                Abrir(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                return;
            case "ayuda":
                // Ayuda bajo demanda: qué toca ahora, controles, glosario (heurística 10).
                Abrir(false);
                if (AvisoInicio.Instancia != null) AvisoInicio.Instancia.MostrarAyuda();
                return;
            case "cerrar": Abrir(false); break;
        }
    }


    // En el PC (Editor sin visor, p. ej. Meta XR Simulator): M o Esc abren y cierran el panel.
    static bool TeclaMenu()
    {
#if UNITY_EDITOR
        if (OVRManager.isHmdPresent) return false;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.mKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape);
#endif
#else
        return false;
#endif
    }

    string EstadoPartida()
    {
        string estado;
        if (Partida.UltimoGuardado.HasValue)
        {
            int min = (int)(System.DateTime.Now - Partida.UltimoGuardado.Value).TotalMinutes;
            estado = $"{(Partida.UltimoFueManual ? "Guardada" : "Autoguardado")}: {(min < 1 ? "hace un momento" : $"hace {min} min")}";
        }
        else estado = Partida.Existe ? "Hay una partida guardada" : "Se guarda sola al avanzar";
        var c = Partida.UltimoCheckpoint;
        return c != null ? $"{estado}  ·  Checkpoint: {c.reto} ({c.fecha})" : estado;
    }

    void Refrescar()
    {
        Nivel(musica, barraMusica, AjustesNanoLab.VolumenMusica);
        Nivel(efectos, barraEfectos, AjustesNanoLab.VolumenEfectos);
        if (partida != null && !Confirmacion.Pendiente("nueva") && !Confirmacion.Pendiente("checkpoint") && !Confirmacion.Pendiente("reiniciar")) partida.text = $"<color=#A9BED2>{EstadoPartida()}</color>";
        if (sentado != null) sentado.text = AjustesNanoLab.ModoSentado
            ? $"<color=#7CF29A><b>SÍ</b></color>  <size=70%><color=#A9BED2>vista +{AjustesNanoLab.AlturaSentado:F2} m</color></size>"
            : "<color=#A9BED2><b>NO</b></color>";
    }

    static readonly Color Encendido = new Color(0.37f, 0.83f, 1f), Apagado = new Color(0.2f, 0.26f, 0.33f);

    // Barra de 10 segmentos más el porcentaje: el nivel no depende solo del color (RNF-03).
    static void Nivel(TMP_Text texto, PanelRedondeado[] barra, float v)
    {
        int n = Mathf.RoundToInt(v * 10f);
        if (texto != null) texto.text = $"<b>{n * 10} %</b>";
        if (barra == null) return;
        for (int i = 0; i < barra.Length; i++)
        {
            if (barra[i] == null) continue;
            barra[i].color = i < n ? Encendido : Apagado;
            barra[i].Aplicar();
        }
    }
}
