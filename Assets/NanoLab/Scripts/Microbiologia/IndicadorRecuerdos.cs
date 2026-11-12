using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Recuerdos científicos que Baymax recupera al resolver casos (hilo narrativo, RF-01 / RF-09).
public static class RecuerdosBaymax
{
    public const int Total = 3;   // uno por misión: OBSERVAR (1), EXPERIMENTAR (2) y CREAR la vacuna (3); ver Misiones
    static readonly HashSet<string> recuperados = new HashSet<string>();

    public static int Cantidad => recuperados.Count;
    public static IEnumerable<string> Lista => recuperados;
    public static event System.Action Cambiado;

    // Partida guardada.
    public static void Restaurar(IEnumerable<string> casos)
    {
        if (casos == null) return;
        bool cambio = false;
        // Solo los recuerdos que existen hoy (las partidas antiguas guardaban otros nombres).
        foreach (var c in casos) { var id = Misiones.Normalizar(c); if (id != null) cambio |= recuperados.Add(id); }
        if (cambio) Cambiado?.Invoke();
    }

    public static void Reiniciar()
    {
        recuperados.Clear();
        Cambiado?.Invoke();
    }

    public static void Recuperar(string caso)
    {
        if (string.IsNullOrEmpty(caso) || !recuperados.Add(caso)) return;
        Cambiado?.Invoke();
    }
}

// Indicador visual del progreso de los recuerdos: "Recuerdos científicos: n/3" y una barra de ranuras.
public class IndicadorRecuerdos : MonoBehaviour
{
    public TMP_Text texto;
    public Renderer[] ranuras;

    MaterialPropertyBlock mpb;

    void OnEnable() { AsegurarRanuras(); RecuerdosBaymax.Cambiado += Refrescar; Refrescar(); }

    // La escena se guardó cuando el caso daba 1 recuerdo (una sola ranura): se crean las que falten copiando la
    // primera, con la misma separación que usa MontarIntroduccion (4,5 cm).
    void AsegurarRanuras()
    {
        if (ranuras == null || ranuras.Length == 0 || ranuras[0] == null || ranuras.Length >= RecuerdosBaymax.Total) return;
        var lista = new List<Renderer>(ranuras);
        var b = ranuras[0].transform;
        for (int i = ranuras.Length; i < RecuerdosBaymax.Total; i++)
        {
            var c = Instantiate(b.gameObject, b.parent);
            c.name = "Recuerdo" + i;
            c.transform.localPosition = b.localPosition + new Vector3(0.045f * i, 0f, 0f);
            c.transform.localRotation = b.localRotation;
            c.transform.localScale = b.localScale;
            lista.Add(c.GetComponent<Renderer>());
        }
        ranuras = lista.ToArray();
    }
    void OnDisable() { RecuerdosBaymax.Cambiado -= Refrescar; }

    void Refrescar()
    {
        int n = RecuerdosBaymax.Cantidad;
        if (texto != null)
            texto.text = n == 0
                ? $"RECUERDOS CIENTÍFICOS: <color=#FFD25A>0/{RecuerdosBaymax.Total}</color>"
                : $"RECUERDOS CIENTÍFICOS: <color=#39FF88>{n}/{RecuerdosBaymax.Total}</color>";
        if (ranuras == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        for (int i = 0; i < ranuras.Length; i++)
        {
            if (ranuras[i] == null) continue;
            ranuras[i].enabled = i < RecuerdosBaymax.Total;   // la escena trae 3 ranuras; solo se ven las que hay
            ranuras[i].GetPropertyBlock(mpb);
            mpb.SetColor("_Color", i < n ? new Color(0.3f, 1f, 0.55f) : new Color(0.15f, 0.2f, 0.27f));
            ranuras[i].SetPropertyBlock(mpb);
        }
    }
}
