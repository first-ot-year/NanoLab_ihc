using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Acerca cada microscopio al borde de su mesa del lado del usuario (su +Z local), para que no
// haya que inclinarse sobre la mesa: la platina queda a 'DistanciaBorde' del canto.
public static class AcercarMicroscopios
{
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";
    public const float DistanciaBorde = 0.15f;

    [MenuItem("NanoLab/Acercar microscopios al borde de la mesa", false, 25)]
    public static void AplicarEscenaMicrobiologia()
    {
        var s = EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        Aplicar(s);
        EditorSceneManager.SaveScene(s);
    }

    public static void Aplicar(Scene s)
    {
        Physics.SyncTransforms();
        int movidos = 0;
        foreach (var m in Object.FindObjectsOfType<MicroscopioOptico>(true))
        {
            if (m.gameObject.scene != s || m.platina == null || m.platina.anclaje == null) continue;
            var t = m.transform;
            var haciaUsuario = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
            var p = m.platina.anclaje.position;
            float alturaMesa = t.position.y;
            float borde = DistanciaAlBorde(m, p, haciaUsuario, alturaMesa);
            if (borde < 0f) { Debug.LogWarning($"[Microscopios] {Ruta(t)}: no se encontró el borde de la mesa."); continue; }
            float mover = borde - DistanciaBorde;
            if (Mathf.Abs(mover) < 0.01f) continue;
            t.position += haciaUsuario * mover;
            movidos++;
            Debug.Log($"[Microscopios] {Ruta(t)}: platina a {borde:F2} m del borde -> {DistanciaBorde:F2} m (movido {mover:F2} m)");
        }
        Physics.SyncTransforms();
        Debug.Log($"[Microscopios] {s.name}: {movidos} microscopios acercados al borde.");
    }

    // Avanza desde la platina hacia el usuario hasta que debajo ya no hay superficie de mesa.
    static float DistanciaAlBorde(MicroscopioOptico m, Vector3 desde, Vector3 dir, float alturaMesa)
    {
        bool sobreMesa = false;
        for (float d = 0f; d < 1.5f; d += 0.005f)
        {
            var o = desde + dir * d;
            o.y = alturaMesa + 0.4f;
            bool hay = Physics.RaycastAll(o, Vector3.down, 0.8f, ~0, QueryTriggerInteraction.Ignore)
                .Any(h => !h.collider.transform.IsChildOf(m.transform) && h.collider.attachedRigidbody == null
                          && Mathf.Abs(h.point.y - alturaMesa) < 0.04f);
            if (hay) sobreMesa = true;
            else if (sobreMesa) return d;
        }
        return -1f;
    }

    static string Ruta(Transform t) => t.parent == null ? t.name : Ruta(t.parent) + "/" + t.name;
}
