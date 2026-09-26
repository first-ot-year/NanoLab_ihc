using TMPro;
using UnityEngine;

// Botón que se pulsa tocándolo con el mando. Necesita un collider trigger.
public class BotonFisico : MonoBehaviour
{
    public Transform tapa;
    public TMP_Text etiqueta;
    public event System.Action Pulsado;
    public bool Presionado => contactos > 0;

    int contactos;
    Vector3 tapaPos;
    float ultimo = -1f;

    void Awake() { if (tapa != null) tapaPos = tapa.localPosition; }

    void OnTriggerEnter(Collider c)
    {
        var mano = c.GetComponent<ManoNanoLab>();
        if (mano == null) return;
        contactos++;
        if (Time.time - ultimo < 0.25f) return;
        ultimo = Time.time;
        Pulsado?.Invoke();
        mano.Vibrar(0.4f, 0.05f);
        Sonidos.Clic(transform.position);
    }

    void OnTriggerExit(Collider c)
    {
        if (c.GetComponent<ManoNanoLab>() != null) contactos = Mathf.Max(0, contactos - 1);
    }

    void Update()
    {
        if (tapa == null) return;
        var objetivo = tapaPos - Vector3.up * (Presionado ? 0.006f : 0f);
        tapa.localPosition = Vector3.Lerp(tapa.localPosition, objetivo, Time.deltaTime * 20f);
    }
}
