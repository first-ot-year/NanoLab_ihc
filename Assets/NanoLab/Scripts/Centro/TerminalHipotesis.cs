using TMPro;
using UnityEngine;

// Terminal donde el estudiante registra qué microorganismo cree que es la muestra (RF-05).
public class TerminalHipotesis : MonoBehaviour
{
    public BotonFisico[] botones;
    public MuestraData[] opciones;
    public Renderer[] marcos;               // se encienden en la opción elegida
    public TMP_Text pantalla;

    void Awake()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            int k = i;
            botones[i].Pulsado += () => Elegir(k);
        }
    }

    void Start() => Refrescar();

    public void Elegir(int i)
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        if (!rm.PuedeHipotesis(out var motivo))
        {
            Aviso.Mostrar(motivo, transform.position + Vector3.up * 0.6f, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        rm.RegistrarHipotesis(opciones[i]);
        Sonidos.Encaje(transform.position);
        Refrescar();
    }

    public void Refrescar()
    {
        var rm = RetoManager.Instancia;
        var h = rm != null ? rm.Hipotesis : null;
        for (int i = 0; i < marcos.Length; i++) if (marcos[i] != null) marcos[i].enabled = opciones[i] == h;
        if (pantalla == null) return;
        pantalla.text = h == null
            ? "¿Qué microorganismo es la muestra del <b>CASO 01</b>?\n<size=75%><color=#A9BED2>Elige según tus evidencias y las fichas de la biblioteca. Puedes cambiarla antes de experimentar.</color></size>"
            : $"Hipótesis registrada: <b><color=#FFD25A>{h.nombre}</color></b>\n<size=75%><color=#A9BED2>Ahora elige el agente que crees que afectará a este microorganismo y fórmalo en la PRENSA (6).</color></size>";
    }
}
