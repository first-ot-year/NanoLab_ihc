using TMPro;
using UnityEngine;

// Cartel de una estación del centro: número, nombre, qué se hace aquí y botón PISTA.
// Se ilumina cuando es la estación del objetivo actual (una meta a la vez).
public class EstacionGuia : MonoBehaviour
{
    public int numero;
    public string titulo;
    [TextArea(2, 4)] public string descripcion;
    public TMP_Text tituloTxt, textoTxt;
    public Renderer marcoActivo;            // brillo pulsante cuando es la estación actual
    public BotonFisico pista;
    public Transform puntoBaliza;

    public bool Activa { get; private set; }
    string pistaMostrada;

    void OnEnable() { if (pista != null) pista.Pulsado += PedirPista; }
    void OnDisable() { if (pista != null) pista.Pulsado -= PedirPista; }

    public void Mostrar(bool activa, string objetivo)
    {
        Activa = activa;
        if (marcoActivo != null) marcoActivo.enabled = activa;
        if (tituloTxt != null) tituloTxt.text = $"<color=#{(activa ? "FFD25A" : "5FD4FF")}>{numero}</color>  {titulo}";
        Refrescar(objetivo);
    }

    void Refrescar(string objetivo)
    {
        if (textoTxt == null) return;
        string s = Activa && !string.IsNullOrEmpty(objetivo)
            ? $"<color=#FFD25A><b>AHORA:</b></color> {objetivo}"
            : $"<color=#A9BED2>{descripcion}</color>";
        if (!string.IsNullOrEmpty(pistaMostrada)) s += $"\n<color=#FFD25A><b>PISTA:</b> {pistaMostrada}</color>";
        textoTxt.text = s;
    }

    public void LimpiarPista() => pistaMostrada = null;

    public void PedirPista()
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        pistaMostrada = rm.PedirPista();
        Refrescar(rm.ObjetivoActual);
        Sonidos.Pista(transform.position);
        Aviso.Mostrar(pistaMostrada, transform.position + Vector3.up * 0.35f, Aviso.Tipo.Pista, 8f);
    }
}
