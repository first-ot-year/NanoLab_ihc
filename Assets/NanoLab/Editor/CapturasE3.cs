using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ML = MontarLaboratorio;

// Capturas del E3 (Logs/Capturas): vista sentado ante un microscopio, el panel ☰ de ajustes y la ficha
// del virus abierta. Abre la escena sin guardarla (los cambios para la foto se descartan).
public static class CapturasE3
{
    const string Escena = "Assets/NanoLab/Scenes/Microbiologia.unity";

    [MenuItem("NanoLab/Capturas del E3 (sillas, ajustes, fichas)")]
    public static void Capturas()
    {
        EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);

        // Sentado en la silla más cercana a un microscopio de puesto.
        var micros = Object.FindObjectsOfType<MicroscopioOptico>(true).Where(m => !m.modoReto).ToArray();
        var sillas = Object.FindObjectsOfType<Silla>(true);
        Debug.Log($"[E3] {sillas.Length} sillas en Microbiología.");
        // Sentado: cuánto hay que inclinarse para llegar al ocular (se activa a menos de 'distancia').
        foreach (var s in sillas)
        {
            var m = micros.Where(x => x.ocular != null).OrderBy(x => Vector3.Distance(x.transform.position, s.transform.position)).FirstOrDefault();
            if (m == null) continue;
            var ojos = s.PuntoAsiento + Vector3.up * s.ojosSobreAsiento;
            var d = m.ocular.position - ojos;
            Debug.Log($"[E3] {s.name}: ocular a {new Vector2(d.x, d.z).magnitude:F2} m en horizontal y {d.y:+0.00;-0.00} m en vertical de los ojos sentados (se activa a {m.distancia:F2} m)");
        }
        var silla = sillas.OrderBy(s => micros.Length == 0 ? 0f : micros.Min(m => Vector3.Distance(m.transform.position, s.transform.position))).FirstOrDefault();
        if (silla != null)
        {
            var ojos = silla.PuntoAsiento + Vector3.up * silla.ojosSobreAsiento;
            ML.Capturar(dir, "silla_sentado_puesto", ojos, Quaternion.AngleAxis(20f, silla.transform.right) * silla.transform.forward);
            var lado = silla.transform.position - silla.transform.forward * 1.3f + silla.transform.right * 0.8f + Vector3.up * 1.5f;
            ML.Capturar(dir, "silla_prefab", lado, (silla.transform.position + Vector3.up * 0.45f) - lado);
        }

        // Panel de ajustes delante de la vista de entrada.
        var menu = Object.FindObjectOfType<MenuAjustes>(true);
        if (menu != null && menu.panel != null)
        {
            float y = ML.AlturaSuelo(ML.BoundsEscena());
            var pos = new Vector3(-3.3f, y + 1.6f, 2.24f);
            var f = Quaternion.Euler(0f, 24f, 0f) * Vector3.forward;
            menu.panel.SetActive(true);
            menu.panel.transform.SetPositionAndRotation(pos + f * 0.55f - Vector3.up * 0.12f, Quaternion.LookRotation(f));
            ML.Capturar(dir, "ajustes_menu", pos, (menu.panel.transform.position - pos));
            menu.panel.SetActive(false);
        }

        // Ficha del virus abierta.
        var ficha = Object.FindObjectsOfType<EstacionFicha>(true).FirstOrDefault(x => x.id == "Hepatitis B");
        if (ficha != null)
        {
            ficha.abierta.SetActive(true); ficha.cerrada.SetActive(false);
            var hacia = ficha.boton != null ? ficha.boton.transform.position - ficha.transform.position : -ficha.transform.forward;
            hacia.y = 0f;
            var cam = ficha.transform.position + hacia.normalized * 1.25f - Vector3.up * 0.1f;
            // Dos veces: en batchmode la primera foto sale antes de que las texturas terminen de subir a la GPU.
            for (int i = 0; i < 2; i++) ML.Capturar(dir, "ficha_virus", cam, ficha.transform.position - cam);
        }
        Debug.Log("[E3] Capturas guardadas.");
    }
}
