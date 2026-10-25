using TMPro;
using UnityEngine;

// Archivo de recuerdos de Baymax: informe final del caso (decisiones, intentos, ayudas, tiempo),
// recuerdo recuperado y botón para repetir el caso.
public class PanelResumen : MonoBehaviour
{
    public TMP_Text titulo, izquierda, derecha, fragmentoTxt;
    public Renderer[] fragmentos;           // 3 ranuras; la del caso se enciende al resolverlo
    public int indiceFragmento;
    public BotonFisico reiniciar;

    MaterialPropertyBlock mpb;

    void OnEnable() { if (reiniciar != null) reiniciar.Pulsado += Reiniciar; }
    void OnDisable() { if (reiniciar != null) reiniciar.Pulsado -= Reiniciar; }

    void Reiniciar() { if (RetoManager.Instancia != null) RetoManager.Instancia.Reiniciar(); }

    public void Pendiente(RetoData reto)
    {
        if (titulo != null) titulo.text = "ARCHIVO DE RECUERDOS DE BAYMAX";
        if (izquierda != null) izquierda.text = $"<color=#8FD8FF><b>{reto.codigo} · {reto.titulo.ToUpper()}</b></color>\n\n{reto.historia}";
        if (derecha != null) derecha.text = "<color=#A9BED2>El informe del caso aparecerá aquí cuando lo resuelvas: lo que observaste, lo que decidiste y lo que cambió en la muestra.</color>";
        if (fragmentoTxt != null) fragmentoTxt.text = $"Recuerdos científicos recuperados: {RecuerdosBaymax.Cantidad}/{RecuerdosBaymax.Total}";
        Encender(false);
    }

    public void Mostrar(string tituloTxt, string izq, string der, string fragmento)
    {
        if (titulo != null) titulo.text = tituloTxt;
        if (izquierda != null) izquierda.text = izq;
        if (derecha != null) derecha.text = der;
        if (fragmentoTxt != null) fragmentoTxt.text = fragmento;
        Encender(true);
    }

    void Encender(bool v)
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        for (int i = 0; i < fragmentos.Length; i++)
        {
            var r = fragmentos[i];
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            var c = v && i == indiceFragmento ? new Color(0.3f, 1f, 0.55f) : new Color(0.15f, 0.2f, 0.27f);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }
    }
}
