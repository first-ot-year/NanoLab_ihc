using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Silla de laboratorio modelada en Blender (taburete con respaldo lumbar, aro reposapiés y base de 5
// radios) con la misma interacción que Silla.prefab: componente Silla (Apuntable, lo encuentra el rayo
// de PunteroMuestras), anclas SitPoint/ExitPoint, un collider trigger para el rayo y un bloqueo sólido.
// Modelo de Blender hecho por un compañero (fuente en Fuentes_Blender/Silla_Laboratorio_NanoLabVR).
// MontarSillas la pone en todos los puestos de Microbiología y en Química.
public static class MontarSillaLaboratorio
{
    const string Dir = "Assets/NanoLab/Silla_Laboratorio_Interactuable";
    public const string Fbx = Dir + "/Models/Silla_Laboratorio_Interactuable.fbx";
    public const string PrefabRuta = Dir + "/Prefabs/Silla_Laboratorio_Interactuable.prefab";
    const string MatDir = "Assets/NanoLab/Materiales/Micro";

    [MenuItem("NanoLab/Silla de laboratorio: crear prefab", false, 28)]
    public static GameObject CrearPrefab()
    {
        var imp = AssetImporter.GetAtPath(Fbx) as ModelImporter;
        if (imp == null) { Debug.LogError("[SillaLab] Falta el modelo " + Fbx); return null; }
        imp.animationType = ModelImporterAnimationType.None;
        imp.importAnimation = false;
        imp.importCameras = false;
        imp.importLights = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.SaveAndReimport();
        // Los materiales del FBX se llaman como los del proyecto: se usan SillaAsiento y SillaMetal, sin duplicarlos.
        imp.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
        imp.SaveAndReimport();

        var raiz = new GameObject("Silla_Laboratorio_Interactuable");
        var silla = raiz.AddComponent<Silla>();
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
        modelo.name = "Modelo";
        modelo.transform.SetParent(raiz.transform, false);

        var rs = modelo.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in rs)
        {
            r.receiveGI = ReceiveGI.LightProbes;   // la sala está horneada; la silla se ilumina con las sondas
            r.lightProbeUsage = LightProbeUsage.BlendProbes;
            foreach (var m in r.sharedMaterials)
                if (!AssetDatabase.GetAssetPath(m).StartsWith(MatDir))
                    Debug.LogError($"[SillaLab] '{r.name}' no usa los materiales de {MatDir} ({(m != null ? m.name : "ninguno")}).");
        }

        // Comprobaciones del modelo: escala (≈0.93 m de alto) y frente hacia +Z (el respaldo detrás, en -Z).
        Bounds Caja(string nombre) => rs.First(r => r.name == nombre).bounds;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        if (Caja("Respaldo").center.z > 0f)
        {
            modelo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Debug.LogWarning("[SillaLab] El respaldo quedaba delante: se gira el modelo 180°. Revisa los ejes del FBX.");
        }
        if (b.size.y < 0.85f || b.size.y > 1.0f)
            Debug.LogError($"[SillaLab] Alto inesperado {b.size.y:F2} m (debe ser ≈0.93): revisa la escala del FBX.");
        float tapa = Caja("Asiento").max.y;

        // Anclas que usa Silla: los nombres que pedía el diseño; los campos son los de siempre.
        silla.asiento = new GameObject("SitPoint").transform;
        silla.asiento.SetParent(raiz.transform, false);
        silla.asiento.localPosition = new Vector3(0f, tapa, 0f);
        silla.salida = new GameObject("ExitPoint").transform;
        silla.salida.SetParent(raiz.transform, false);
        silla.salida.localPosition = new Vector3(0f, 0f, -0.6f);   // detrás, en el pasillo
        // Asiento a la misma altura que el taburete anterior (0.65 m): los ojos quedan igual, a la altura del ocular.

        // Trigger en la raíz para el rayo (sentarse) y una caja sólida en la capa BloqueoJugador para que
        // el jugador no la atraviese; Silla.Colisiones la desactiva mientras está sentado en ella.
        var t = raiz.AddComponent<BoxCollider>();
        t.isTrigger = true;
        t.center = b.center;
        t.size = b.size;
        AsegurarColisiones.BloqueoSilla(raiz);

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabRuta));
        var p = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabRuta);
        Object.DestroyImmediate(raiz);
        Debug.Log($"[SillaLab] Prefab en {PrefabRuta}: {b.size.x:F2}×{b.size.y:F2}×{b.size.z:F2} m, asiento a {tapa:F2} m.");
        return p;
    }

}
