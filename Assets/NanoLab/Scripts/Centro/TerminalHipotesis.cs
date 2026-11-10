using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Terminal de hipótesis y estrategia (RF-05). Dos decisiones a partir de las evidencias y las fichas:
//  1) qué microorganismo cree el estudiante que es la muestra;
//  2) qué estructura de ese microorganismo atacará (la estrategia que luego prepara en la prensa).
// Los botones se tocan o se apuntan con el rayo (BotonFisico es Apuntable). Ambas se pueden cambiar antes de experimentar.
public class TerminalHipotesis : MonoBehaviour
{
    public BotonFisico[] botones;
    public MuestraData[] opciones;
    public Renderer[] marcos;               // se encienden en la opción elegida
    public BotonFisico[] botonesEstrategia;
    public Renderer[] marcosEstrategia;
    public TMP_Text pantalla;

    public const string AvisoSimplificado = "Modelo educativo simplificado: no es un protocolo clínico ni un tratamiento real.";

    void Awake()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            int k = i;
            botones[i].Pulsado += () => Elegir(k);
        }
        if (botonesEstrategia != null)
            for (int i = 0; i < botonesEstrategia.Length; i++)
            {
                int k = i;
                if (botonesEstrategia[i] != null) botonesEstrategia[i].Pulsado += () => ElegirEstrategia(k);
            }
    }

    void OnEnable() => RetoManager.Cambio += Refrescar;
    void OnDisable() => RetoManager.Cambio -= Refrescar;
    void Start() => Refrescar();

    // Sobre el borde de la consola, a la altura del pecho (el origen de la estación está en el suelo).
    public Vector3 Encima => transform.TransformPoint(new Vector3(0f, 1.2f, 0.25f));

    public void Elegir(int i)
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        if (!rm.PuedeHipotesis(out var motivo)) { Rechazar(motivo); return; }
        rm.RegistrarHipotesis(opciones[i]);
        Sonidos.Encaje(transform.position);
        Refrescar();
    }

    public void ElegirEstrategia(int i)
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        if (!rm.PuedeEstrategia(out var motivo)) { Rechazar(motivo); return; }
        rm.RegistrarEstrategia(i);
        Sonidos.Encaje(transform.position);
        Refrescar();
    }

    void Rechazar(string motivo)
    {
        Aviso.Mostrar(motivo, Encima, Aviso.Tipo.Error);
        Sonidos.Error(transform.position);
    }

    public void Refrescar()
    {
        var rm = RetoManager.Instancia;
        var h = rm != null ? rm.Hipotesis : null;
        var s = rm != null ? rm.EstrategiaElegida : null;
        for (int i = 0; i < marcos.Length; i++) if (marcos[i] != null) marcos[i].enabled = opciones[i] == h;
        if (marcosEstrategia != null)
            for (int i = 0; i < marcosEstrategia.Length; i++) if (marcosEstrategia[i] != null) marcosEstrategia[i].enabled = rm != null && rm.Estrategia == i;
        if (pantalla == null) return;

        var sb = new StringBuilder();
        string caso = rm != null ? rm.reto.codigo : "CASO 01";
        // Lo observado, para decidir a partir de las evidencias y no de memoria.
        if (rm != null && rm.Evidencias.Any(x => x))
        {
            var ev = rm.reto.evidencias.Select((e, k) => rm.Evidencias[k] ? e.valor : "???");
            sb.AppendLine($"<size=72%><color=#8FD8FF>TUS EVIDENCIAS:</color> {string.Join("  ·  ", ev)}</size>");
        }
        else sb.AppendLine($"<size=72%><color=#8FD8FF>TUS EVIDENCIAS:</color> <color=#FFD25A>aún ninguna: observa la muestra al microscopio (3)</color></size>");

        sb.AppendLine(h == null
            ? $"<b>1) ¿Qué microorganismo es la muestra del {caso}?</b>  <color=#FFD25A>[ PENDIENTE ]</color>"
            : $"1) Hipótesis: <b><color=#FFD25A>{h.nombre}</color></b>  <color=#39FF88>[ ELEGIDA ]</color>");
        sb.AppendLine(s == null
            ? (h == null ? "<color=#8FA6BC>2) ¿Qué estructura atacarás?  (después de la 1)</color>" : "<b>2) ¿Qué estructura atacarás?</b>  <color=#FFD25A>[ PENDIENTE ]</color>")
            : $"2) Estrategia: <b><color=#FFD25A>{s.nombre}</color></b>  <color=#39FF88>[ ELEGIDA ]</color>");
        sb.Append("<size=70%><color=#A9BED2>");
        sb.Append(h != null && s != null
            ? "Ahora busca en la PRENSA (6) el vial que hace lo que decidiste y formula la cápsula. Puedes cambiar de idea antes de experimentar."
            : "Elige según tus evidencias y las fichas de la BIBLIOTECA (4). Puedes cambiar de idea antes de experimentar.");
        sb.Append($"</color>\n<color=#7F93A8><i>{AvisoSimplificado}</i></color></size>");
        pantalla.text = sb.ToString();
    }
}
