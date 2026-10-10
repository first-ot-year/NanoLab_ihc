using TMPro;
using UnityEngine;

// Ficha consultable de la biblioteca: se abre con su botón y registra la consulta (evidencia, RF-04).
public class EstacionFicha : MonoBehaviour
{
    public string id;
    public GameObject abierta, cerrada;
    public BotonFisico boton;
    public TMP_Text botonTxt;

    public bool Abierta { get; private set; }

    void Awake() => Poner(false);
    void OnEnable() { if (boton != null) boton.Pulsado += Alternar; }
    void OnDisable() { if (boton != null) boton.Pulsado -= Alternar; }

    public void Alternar()
    {
        Poner(!Abierta);
        if (Abierta && RetoManager.Instancia != null) RetoManager.Instancia.FichaConsultada(id);
    }

    public void Poner(bool v)
    {
        Abierta = v;
        if (abierta != null) abierta.SetActive(v);
        if (cerrada != null) cerrada.SetActive(!v);
        if (botonTxt != null) botonTxt.text = v ? "CERRAR" : "ABRIR";
    }
}
