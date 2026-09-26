using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Portal entre salas: al entrar el jugador en el trigger, carga la otra escena.
public class PuertaEscena : MonoBehaviour
{
    public string escena;
    bool cargando;

    void OnTriggerEnter(Collider c)
    {
        if (cargando || c.GetComponent<CharacterController>() == null) return;
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
