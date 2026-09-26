using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Portal entre salas: cuando la cabeza del jugador entra en la zona (collider trigger
// de este objeto), carga la otra escena. Funciona con el rig de Meta (sin CharacterController).
[RequireComponent(typeof(Collider))]
public class PuertaEscena : MonoBehaviour
{
    public string escena;
    bool cargando;
    Collider zona;

    void Awake() => zona = GetComponent<Collider>();

    void Update()
    {
        if (cargando) return;
        var cabeza = ManosRig.Cabeza;
        if (cabeza == null) return;
        var b = zona.bounds;
        var p = cabeza.position;
        p.y = b.center.y;   // basta con estar encima de la zona, a cualquier altura
        if (!b.Contains(p)) return;
        cargando = true;
        StartCoroutine(Cargar());
    }

    IEnumerator Cargar()
    {
        var fade = FindObjectOfType<OVRScreenFade>();
        if (fade != null) fade.FadeOut();
        Sonidos.Scan(transform.position);
        yield return new WaitForSeconds(0.6f);
        SceneManager.LoadScene(escena);
    }
}
