using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Aplica a Microbiologia.unity, sin volver a montarla ni hornear la luz:
//  1) quita el portal a la sala de Química (que se eliminó del proyecto) y su baliza de área nº 5,
//  2) pone la puerta corredera automática en la entrada del Centro de Experimentación y la compuerta
//     de guillotina de la cámara experimental,
//  3) rehace el panel ☰ del rig con el botón VOLVER AL CHECKPOINT,
//  4) deja la ventana "¡Hola! Eres Baymax" como cristal transparente que se avanza con X.
// Montar TODO / Reconstruir el centro ya incluyen estos cambios; este menú es para no esperar al bake.
public static class AplicarCambiosE4
{
    const string Escena = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Quimica = "Assets/NanoLab/Scenes/Laboratorio.unity";

    [MenuItem("NanoLab/Aplicar: sin Química + puertas del centro + checkpoint", false, 3)]
    public static void Aplicar()
    {
        if (Application.isPlaying) { Debug.LogWarning("[E4] Sal del modo Play antes de aplicar los cambios."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);
        MontarMicrobiologia.CrearMateriales();
        MontarMicrobiologia.suelo = MontarLaboratorio.AlturaSuelo(MontarLaboratorio.BoundsEscena());

        // 1) Portal y baliza de Química.
        var todos = Object.FindObjectsOfType<Transform>(true);
        int quitados = 0;
        foreach (var t in todos.Where(t => t != null && (t.name == "Portal" || t.name.StartsWith("Portal_"))).ToArray())
        {
            if (t == null) continue;   // ya se borró con su padre
            Object.DestroyImmediate(t.gameObject);
            quitados++;
        }
        var areas = todos.FirstOrDefault(t => t != null && t.name == "AreasLaboratorio");
        var area5 = areas != null ? areas.Find("Area_5") : null;
        if (area5 != null) { Object.DestroyImmediate(area5.gameObject); quitados++; }
        Debug.Log($"[E4] Objetos de Química quitados: {quitados} (portal y baliza 5).");

        // 2) Puerta corredera automática del centro.
        var centro = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == MontarCentroExperimentacion.NombreRaiz);
        var grupo = centro != null ? centro.Find("Puerta") : null;
        if (grupo == null) Debug.LogError("[E4] No se encontró CentroExperimentacion/Puerta: ejecuta 'Reconstruir solo el centro de experimentación'.");
        else MontarCentroExperimentacion.PuertaDeslizante(grupo);

        // 2b) Compuerta dinámica de la cámara experimental.
        foreach (var cam in Object.FindObjectsOfType<CamaraExperimental>(true))
            MontarCentroExperimentacion.Compuerta(cam.transform, cam);

        // 2c) Ventana "¡Hola! Eres Baymax": cristal transparente y textos nuevos (X para continuar, sin Química).
        foreach (var intro in Object.FindObjectsOfType<VentanaIntro>(true))
        {
            intro.pasos = VentanaIntro.PasosPorDefecto();
            var fondo = intro.ventana != null ? intro.ventana.transform.Find("Fondo") : null;
            var panel = fondo != null ? fondo.GetComponent<PanelRedondeado>() : null;
            if (panel != null) { panel.color = intro.colorCristal; panel.Aplicar(); EditorUtility.SetDirty(panel); }
            EditorUtility.SetDirty(intro);
        }

        // 3) Panel ☰ con el botón del checkpoint.
        var rig = Object.FindObjectsOfType<OVRCameraRig>(true).FirstOrDefault();
        if (rig == null) Debug.LogError("[E4] No hay rig de Meta en la escena: ejecuta 'Configurar interacción Meta'.");
        else MontarAjustes.Construir(PrefabUtility.GetOutermostPrefabInstanceRoot(rig.gameObject) ?? rig.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // La build solo lleva Microbiología. Por si la escena de Química sigue en disco (otra rama), se borra.
        if (File.Exists(Quimica)) AssetDatabase.DeleteAsset(Quimica);
        MontarIntroduccion.OrdenarEscenas();
        Debug.Log("[E4] Listo: Microbiologia.unity guardada sin Química, con la puerta automática del centro y el botón de checkpoint.");
    }
}
