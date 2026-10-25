using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Recuerdos científicos que Baymax recupera al resolver casos (hilo narrativo, RF-01 / RF-09).
public static class RecuerdosBaymax
{
    public const int Total = 3;
    static readonly HashSet<string> recuperados = new HashSet<string>();

    public static int Cantidad => recuperados.Count;
    public static event System.Action Cambiado;

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

    void OnEnable() { RecuerdosBaymax.Cambiado += Refrescar; Refrescar(); }
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
            ranuras[i].GetPropertyBlock(mpb);
            mpb.SetColor("_Color", i < n ? new Color(0.3f, 1f, 0.55f) : new Color(0.15f, 0.2f, 0.27f));
            ranuras[i].SetPropertyBlock(mpb);
        }
    }
}
