using TMPro;
using UnityEngine;

// Botón que se pulsa acercando la mano o el mando del rig de Meta.
public class BotonFisico : MonoBehaviour
{
    public Transform tapa;
    public TMP_Text etiqueta;
    public float radio = 0.06f;
    public event System.Action Pulsado;
    public bool Presionado { get; private set; }

    Vector3 tapaPos;
    float ultimo = -1f;

    void Awake() { if (tapa != null) tapaPos = tapa.localPosition; }

    void Update()
    {
        var punto = transform.TransformPoint(new Vector3(0f, 0.02f, 0f));
        bool ahora = ManosRig.MasCercana(punto, radio, out _);
        if (ahora && !Presionado && Time.time - ultimo > 0.25f)
        {
            ultimo = Time.time;
            Pulsado?.Invoke();
            ManosRig.Vibrar(punto, 0.4f, 0.05f);
            Sonidos.Clic(transform.position);
        }
        Presionado = ahora;

        if (tapa == null) return;
        var objetivo = tapaPos - Vector3.up * (Presionado ? 0.006f : 0f);
        tapa.localPosition = Vector3.Lerp(tapa.localPosition, objetivo, Time.deltaTime * 20f);
    }
}
