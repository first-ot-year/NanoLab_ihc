using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Mete todos los objetos sueltos de una sala bajo un único objeto "LAB_<escena>"
// para poder copiarla/pegarla entera. El jugador (OVRPlayerController / OVRCameraRig)
// se deja fuera para no duplicar cámaras al pegar en otra escena.
//
//   LAB_<escena>
//   ├── Sala          (muebles, paredes, techo, suelo… del pack de Animatics)
//   ├── Iluminacion   (luces, sondas de luz, reflection probes)
//   ├── Colisiones    (suelo de seguridad)
//   └── Microbiologia / Props_Descargados / Portal  (lo añadido por NanoLab)
public static class AgruparLab
{
    static readonly string[] Escenas = { "Assets/NanoLab/Scenes/Laboratorio.unity", "Assets/NanoLab/Scenes/Microbiologia.unity" };
    static readonly string[] PropiosNanoLab = { "Microbiologia", "Props_Descargados", "Portal" };

    [MenuItem("NanoLab/Agrupar las dos salas (un objeto por sala)", false, 20)]
    public static void AgruparAmbas()
    {
        foreach (var path in Escenas)
        {
            if (!System.IO.File.Exists(path)) continue;
            var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Agrupar(s);
            EditorSceneManager.SaveScene(s);
        }
    }

    [MenuItem("NanoLab/Agrupar la escena abierta", false, 21)]
    public static void AgruparAbierta()
    {
        var s = SceneManager.GetActiveScene();
        Agrupar(s);
        EditorSceneManager.MarkSceneDirty(s);
    }

    public static GameObject Agrupar(Scene s)
    {
        string nombre = "LAB_" + s.name;
        var raiz = s.GetRootGameObjects().FirstOrDefault(g => g.name == nombre) ?? new GameObject(nombre);
        SceneManager.MoveGameObjectToScene(raiz, s);

        Transform Sub(string n)
        {
            var t = raiz.transform.Find(n);
            if (t == null) { t = new GameObject(n).transform; t.SetParent(raiz.transform, false); }
            return t;
        }

        int movidos = 0;
        foreach (var go in s.GetRootGameObjects())
        {
            if (go == raiz) continue;
            if (EsJugador(go)) continue;

            Transform destino;
            if (go.GetComponent<Light>() || go.GetComponent<LightProbeGroup>() || go.GetComponent<ReflectionProbe>()) destino = Sub("Iluminacion");
            else if (PropiosNanoLab.Contains(go.name)) destino = raiz.transform;
            else if (go.name == "SueloSeguridad") destino = Sub("Colisiones");
            else destino = Sub("Sala");

            go.transform.SetParent(destino, true);
            movidos++;
        }
        Debug.Log($"[Agrupar] {s.name}: {movidos} objetos movidos dentro de '{nombre}'.");
        return raiz;
    }

    static bool EsJugador(GameObject go) =>
        go.GetComponent<OVRPlayerController>() != null || go.GetComponentInChildren<OVRCameraRig>(true) != null;
}
