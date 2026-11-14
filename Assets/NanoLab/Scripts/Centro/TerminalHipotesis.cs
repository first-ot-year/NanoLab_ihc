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
        // Con la A ya identificada, la fila 1 no vuelve a contar errores (pulsarla sin querer no ensucia las métricas).
        if (rm.Hipotesis != null)
        {
            Aviso.Mostrar($"La MUESTRA A ya está identificada: {rm.Hipotesis.nombre}. Ahora decide la estrategia en la fila 2.", Encima, Aviso.Tipo.Info);
            Sonidos.Clic(transform.position);
            return;
        }
        // Misión 1: la identificación se comprueba; si no encaja con las evidencias, RetoManager explica por qué.
        if (rm.RegistrarHipotesis(opciones[i])) Sonidos.Encaje(transform.position);
        else Sonidos.Error(transform.position);
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
        // No es un error: todavía falta un paso (se dice cuál).
        Aviso.Mostrar(motivo, Encima, Aviso.Tipo.Falta);
        Sonidos.Pista(transform.position);
    }

    public void Refrescar()
    {
        var rm = RetoManager.Instancia;
        var h = rm != null ? rm.Hipotesis : null;
        var s = rm != null ? rm.EstrategiaElegida : null;
        // Los botones que aún no se pueden usar se ven grises (al pulsarlos explican qué falta).
        bool puedeH = rm != null && rm.Hipotesis == null && rm.PuedeHipotesis(out _);
        bool puedeE = rm != null && rm.PuedeEstrategia(out _);
        foreach (var b in botones) if (b != null) b.Habilitar(puedeH);
        if (botonesEstrategia != null) foreach (var b in botonesEstrategia) if (b != null) b.Habilitar(puedeE);
        for (int i = 0; i < marcos.Length; i++) if (marcos[i] != null) marcos[i].enabled = opciones[i] == h;
        if (marcosEstrategia != null)
            for (int i = 0; i < marcosEstrategia.Length; i++) if (marcosEstrategia[i] != null) marcosEstrategia[i].enabled = rm != null && rm.Estrategia == i;
        if (pantalla == null) return;

        var sb = new StringBuilder();
        // Lo observado, para decidir a partir de las evidencias y no de memoria.
        if (rm != null && rm.Evidencias.Any(x => x))
        {
            var ev = rm.reto.evidencias.Select((e, k) => rm.Evidencias[k] ? e.valor : "???");
            sb.AppendLine($"<size=72%><color=#8FD8FF>TUS EVIDENCIAS (MUESTRA A):</color> {string.Join("  ·  ", ev)}</size>");
        }
        else sb.AppendLine($"<size=72%><color=#8FD8FF>TUS EVIDENCIAS (MUESTRA A):</color> <color=#FFD25A>aún ninguna: observa la muestra al microscopio (3)</color></size>");

        sb.AppendLine(h == null
            ? $"<b>MISIÓN 1 · ¿Qué bacteria es la MUESTRA A?</b>  <color=#FFD25A>[ PENDIENTE ]</color>"
            : $"MISIÓN 1 · La MUESTRA A es <b><color=#FFD25A>{h.nombre}</color></b>  <color=#39FF88>[ IDENTIFICADA ]</color>");
        // Reconocer, no recordar: lo que decía cada ficha ya consultada, junto a su botón.
        if (h == null && rm != null)
        {
            sb.Append("<size=62%>");
            for (int i = 0; i < opciones.Length; i++)
            {
                if (opciones[i] == null) continue;
                char letra = (char)('A' + i);
                sb.AppendLine(rm.FichaVistaDe(opciones[i])
                    ? $"<b>{letra}</b> · {opciones[i].nombre}: {rm.RasgosFicha(opciones[i])}"
                    : $"<color=#8095AA><b>{letra}</b> · {opciones[i].nombre}: ficha sin consultar (BIBLIOTECA 4)</color>");
            }
            sb.Append("</size>");
        }
        sb.AppendLine(s == null
            ? (h == null ? "<color=#8FA6BC>MISIÓN 2 · ¿Qué estructura atacarás?  (después de identificarla)</color>" : "<b>MISIÓN 2 · ¿Qué estructura atacarás?</b>  <color=#FFD25A>[ PENDIENTE ]</color>")
            : $"MISIÓN 2 · Estrategia: <b><color=#FFD25A>{s.nombre}</color></b>  <color=#39FF88>[ ELEGIDA ]</color>");
        sb.Append("<size=70%><color=#A9BED2>");
        sb.Append(h != null && s != null
            ? "Ahora busca en la PRENSA (6) el vial que hace lo que decidiste y formula la cápsula. Puedes cambiar la estrategia antes de experimentar."
            : "Elige según tus evidencias y las fichas de la BIBLIOTECA (4). La estrategia la puedes cambiar antes de experimentar.");
        sb.Append($"</color>\n<color=#7F93A8><i>{AvisoSimplificado}</i></color></size>");
        pantalla.text = sb.ToString();
    }
}
