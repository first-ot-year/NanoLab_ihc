using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Aplica a las escenas guardadas, sin volver a montarlas ni hornear la luz, lo que trajo feat-respaldo:
//  Microbiología: puerta corredera automática del Centro de Experimentación, compuerta de guillotina de
//                 la cámara experimental, ventana de introducción paso a paso, panel ☰ con el botón de
//                 checkpoint y bloqueos del jugador en los muebles.
//  Química:       panel ☰ con el checkpoint y bloqueos del jugador en los muebles.
// Montar TODO / Reconstruir el centro ya incluyen la puerta y la compuerta; este menú es para no esperar al bake.
public static class AplicarNovedades
{
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Quimica = "Assets/NanoLab/Scenes/Laboratorio.unity";

    [MenuItem("NanoLab/Aplicar novedades (puertas, compuerta, colisiones, panel ☰)", false, 3)]
    public static void Aplicar()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Novedades] Sal del modo Play antes de aplicar los cambios."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AsegurarColisiones.CapaYMatriz();

        var s = EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        MontarMicrobiologia.CrearMateriales();
        MontarMicrobiologia.suelo = MontarLaboratorio.AlturaSuelo(MontarLaboratorio.BoundsEscena());

        var centro = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == MontarCentroExperimentacion.NombreRaiz);
        var grupo = centro != null ? centro.Find("Puerta") : null;
        if (grupo == null) Debug.LogError("[Novedades] No se encontró CentroExperimentacion/Puerta: ejecuta 'Reconstruir solo el centro de experimentación'.");
        else MontarCentroExperimentacion.PuertaDeslizante(grupo);

        foreach (var cam in Object.FindObjectsOfType<CamaraExperimental>(true))
            MontarCentroExperimentacion.Compuerta(cam.transform, cam);

        MontarIntroduccion.Aplicar(s);
        Panel();
        AsegurarColisiones.Aplicar(s);
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);

        if (File.Exists(Quimica))
        {
            var q = EditorSceneManager.OpenScene(Quimica, OpenSceneMode.Single);
            Panel();
            AsegurarColisiones.Aplicar(q);
            EditorSceneManager.MarkSceneDirty(q);
            EditorSceneManager.SaveScene(q);
        }

        MontarIntroduccion.OrdenarEscenas();
        EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        Debug.Log("[Novedades] Listo: Microbiología y Química guardadas con las novedades (sin hornear la luz).");
    }

    // E4 (RF-05/RF-06): terminal con hipótesis + estrategia y tablero de agentes con su estrategia. Solo toca el
    // Centro de Experimentación de Microbiología; el resto de la escena (cajones, cabina, puertas) queda igual.
    [MenuItem("NanoLab/Aplicar E4 (terminal de hipótesis y estrategia)", false, 4)]
    public static void AplicarE4()
    {
        if (Application.isPlaying) { Debug.LogWarning("[E4] Sal del modo Play antes de aplicar los cambios."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AsegurarColisiones.CapaYMatriz();
        var s = EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        MontarMicrobiologia.CrearMateriales();
        MontarMicrobiologia.suelo = MontarLaboratorio.AlturaSuelo(MontarLaboratorio.BoundsEscena());
        if (!MontarCentroExperimentacion.AplicarE4()) return;
        Panel();   // panel ☰ al día (con GUARDAR, NUEVA PARTIDA y CHECKPOINT)
        AsegurarColisiones.Aplicar(s);
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        Debug.Log("[E4] Listo: Microbiología guardada con la terminal de hipótesis y estrategia (sin hornear la luz).");
    }

    // Heurísticas de Nielsen: panel ☰ con AYUDA, lo que borra avance separado de lo que guarda y botones "poke" de Meta.
    // Solo rehace el panel del rig; el resto de la escena queda igual (sin hornear la luz).
    [MenuItem("NanoLab/Aplicar heurísticas (panel ☰ con AYUDA y botones poke)", false, 5)]
    public static void AplicarHeuristicas()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Heurísticas] Sal del modo Play antes de aplicar los cambios."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var s = EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        Panel();
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        Debug.Log("[Heurísticas] Listo: panel ☰ rehecho con AYUDA y botones poke (sin hornear la luz).");
    }

    // Rehace el panel ☰ del rig con los botones de partida y checkpoint.
    static void Panel()
    {
        var rig = Object.FindObjectsOfType<OVRCameraRig>(true).FirstOrDefault();
        if (rig == null) { Debug.LogError("[Novedades] No hay rig de Meta en la escena: ejecuta 'Configurar interacción Meta'."); return; }
        MontarAjustes.Construir(PrefabUtility.GetOutermostPrefabInstanceRoot(rig.gameObject) ?? rig.gameObject);
    }
}
