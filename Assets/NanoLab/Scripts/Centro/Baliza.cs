using UnityEngine;

// Columna de luz con un rombo que flota sobre la estación del objetivo actual.
public class Baliza : MonoBehaviour
{
    public Transform rombo;
    public float velocidad = 3f;
    Vector3 destino;
    bool tieneDestino;

    public void MoverA(Transform t)
    {
        if (t == null) { gameObject.SetActive(false); return; }
        gameObject.SetActive(true);
        destino = t.position;
        if (!tieneDestino) transform.position = destino;
        tieneDestino = true;
    }

    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, destino, Time.deltaTime * velocidad);
        if (rombo == null) return;
        rombo.localPosition = new Vector3(0f, 0.08f * Mathf.Sin(Time.time * 2.5f), 0f);
        rombo.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }
}
