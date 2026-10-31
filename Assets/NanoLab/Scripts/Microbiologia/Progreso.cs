using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// Panel "Muestras analizadas": se marca cada muestra la primera vez que se observa.
public class Progreso : MonoBehaviour
{
    public MuestraData[] muestras;
    public TMP_Text lista;
    public TMP_Text contador;

    // Por nombre del asset: así se guarda en la partida y sobrevive al cambio de sala.
    static readonly HashSet<string> vistas = new HashSet<string>();
    static Progreso instancia;

    public static IEnumerable<string> Vistas => vistas;
    public static event System.Action Cambiado;

    void Awake() { instancia = this; Refrescar(); }

    public static void Marcar(MuestraData m)
    {
        if (m == null || !vistas.Add(m.name)) return;
        Cambiado?.Invoke();
        if (instancia == null) return;
        instancia.Refrescar();
        int n = instancia.Contar();
        if (n == instancia.muestras.Length) Sonidos.Logro(instancia.transform.position);
    }

    // Partida guardada: se suman a las ya vistas en esta sesión.
    public static void Restaurar(IEnumerable<string> nombres)
    {
        if (nombres == null) return;
        foreach (var n in nombres) if (!string.IsNullOrEmpty(n)) vistas.Add(n);
        if (instancia != null) instancia.Refrescar();
    }

    public static void Reiniciar()
    {
        vistas.Clear();
        if (instancia != null) instancia.Refrescar();
    }

    int Contar()
    {
        int n = 0;
        foreach (var m in muestras) if (vistas.Contains(m.name)) n++;
        return n;
    }

    void Refrescar()
    {
        if (muestras == null) return;
        var sb = new StringBuilder();
        foreach (var m in muestras)
        {
            bool v = vistas.Contains(m.name);
            sb.AppendLine(v
                ? $"<color=#39FF88>[OK]</color>  <color=#FFFFFF>{m.nombre}</color>"
                : $"<color=#4A6278>[  ]</color>  <color=#8FA6BC>{m.nombre}</color>");
        }
        if (lista != null) lista.text = sb.ToString();
        if (contador != null)
        {
            int n = Contar();
            contador.text = n == muestras.Length ? $"<color=#39FF88>{n}/{muestras.Length}  COMPLETADO</color>" : $"{n}/{muestras.Length}";
        }
    }
}
