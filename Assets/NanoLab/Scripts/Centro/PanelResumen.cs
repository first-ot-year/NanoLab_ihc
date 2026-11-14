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

    void OnEnable() { if (reiniciar != null) reiniciar.Pulsado += PedirReinicio; }
    void OnDisable() { if (reiniciar != null) reiniciar.Pulsado -= PedirReinicio; }

    // REINICIAR borra el avance de las misiones 1 y 2: pide una segunda pulsación (control y libertad del usuario).
    public void PedirReinicio()
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        var pos = reiniciar != null ? reiniciar.transform.position + Vector3.up * 0.3f : transform.position + Vector3.up * 1.4f;
        if (rm.Actual != EtapaReto.Exploracion && !Confirmacion.Pedir("reiniciar-misiones"))
        {
            Aviso.Mostrar("¿Repetir las misiones 1 y 2 desde el principio? Se borra su avance; tus recuerdos se conservan.\nPulsa REINICIAR otra vez para confirmar.",
                pos, Aviso.Tipo.Pista, Confirmacion.Segundos);
            Sonidos.Pista(pos);
            return;
        }
        rm.Reiniciar();
        Aviso.Mostrar("Misiones 1 y 2 reiniciadas. Empieza en la mesa de TINCIÓN (2).", pos, Aviso.Tipo.Ok);
    }

    public void Pendiente(RetoData reto)
    {
        if (titulo != null) titulo.text = "ARCHIVO DE RECUERDOS DE BAYMAX";
        if (izquierda != null) izquierda.text = $"<color=#8FD8FF><b>{Misiones.Codigo1} · {Misiones.Titulo1.ToUpper()}</b></color>\nCompara 3 muestras (herida, moho y nariz) al microscopio y clasifícalas.\n\n" +
                                              $"<color=#8FD8FF><b>{Misiones.Codigo2} · {Misiones.Titulo2.ToUpper()}</b></color>\nPrueba qué detiene a la bacteria de la herida y compara ANTES | DESPUÉS.\n\n" +
                                              $"<color=#8FD8FF><b>{Misiones.Codigo3} · {ContenidoVacuna.Titulo.ToUpper()}</b></color>\nSe desbloquea al terminar la misión 2.";
        if (derecha != null) derecha.text = "<color=#A9BED2>El informe de las misiones 1 y 2 aparecerá aquí cuando las completes: lo que observaste, lo que decidiste y lo que cambió en la muestra.</color>";
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
