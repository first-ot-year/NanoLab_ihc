using TMPro;
using UnityEngine;

// Panel de ajustes (RNF-07): el botón ☰ del mando izquierdo lo abre delante de la cara, en cualquier sala.
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
    public float distanciaCierre = 2f;

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
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch) || OVRInput.GetDown(OVRInput.Button.Start))
            Abrir(!Abierto);
        // Se cierra solo si el usuario se aleja de él (p. ej. caminando por la habitación real).
        var cab = ManosRig.Cabeza;
        if (Abierto && cab != null && Vector3.Distance(cab.position, panel.transform.position) > distanciaCierre) Abrir(false);
    }

    public void Abrir(bool abrir)
    {
        if (panel == null) return;
        if (abrir)
        {
            var cab = ManosRig.Cabeza;
            if (cab != null)
            {
                var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up);
                if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
                f.Normalize();
                panel.transform.position = cab.position + f * distancia - Vector3.up * 0.12f;
                panel.transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            }
            Refrescar();
        }
        panel.SetActive(abrir);
        Sonidos.Clic(panel.transform.position);
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
                if (JugadorNanoLab.Instancia != null) JugadorNanoLab.Instancia.CalibrarSentado();
                AjustesNanoLab.ModoSentado = true;
                break;
            case "guardar":
                bool ok = Partida.Guardar();
                if (partida != null) partida.text = ok ? "<color=#7CF29A><b>Partida guardada</b></color>" : "<color=#FF7A7A><b>No se pudo guardar</b></color>";
                if (ok) Sonidos.Logro(panel.transform.position); else Sonidos.Error(panel.transform.position);
                return;
            case "checkpoint":
                bool vuelto = GestorPartida.VolverAlCheckpoint(out var mensaje);
                if (partida != null) partida.text = vuelto ? $"<color=#7CF29A><b>{mensaje}</b></color>" : $"<color=#FFD25A><b>{mensaje}</b></color>";
                if (vuelto) { Sonidos.Logro(panel.transform.position); Abrir(false); }
                else Sonidos.Error(panel.transform.position);
                return;
            case "nueva":
                // Doble confirmación: se pierde el avance guardado.
                if (Time.unscaledTime - confirmarNueva > 4f)
                {
                    confirmarNueva = Time.unscaledTime;
                    if (partida != null) partida.text = "<color=#FFD25A><b>¿Seguro? Pulsa NUEVA PARTIDA otra vez</b></color>";
                    Sonidos.Pista(panel.transform.position);
                    return;
                }
                Partida.Borrar();
                Abrir(false);
                UnityEngine.SceneManagement.SceneManager.LoadScene("Microbiologia");
                return;
            case "cerrar": Abrir(false); break;
        }
    }

    float confirmarNueva = -10f;

    string EstadoPartida()
    {
        if (Partida.UltimoGuardado.HasValue)
        {
            int min = (int)(System.DateTime.Now - Partida.UltimoGuardado.Value).TotalMinutes;
            return $"Autoguardado: {(min < 1 ? "hace un momento" : $"hace {min} min")}";
        }
        return Partida.Existe ? "Hay una partida guardada" : "Se guarda sola al avanzar";
    }

    void Refrescar()
    {
        Nivel(musica, barraMusica, AjustesNanoLab.VolumenMusica);
        Nivel(efectos, barraEfectos, AjustesNanoLab.VolumenEfectos);
        if (partida != null && Time.unscaledTime - confirmarNueva > 4f) partida.text = $"<color=#A9BED2>{EstadoPartida()}</color>";
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
