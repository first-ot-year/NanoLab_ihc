using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Aspecto realista del entorno (armarios, sillas, mesas, suelo, paredes) sin cambiar la geometría:
//  1. Texturas del pack de Animatics: las MaskMap (HDRP: R metal, G oclusión, A suavidad) se
//     importaban como color (sRGB) y la suavidad llegaba apagada (0,70 -> 0,45): ahora son lineales.
//  2. Sus materiales Standard usan también la oclusión ambiental de la MaskMap (canal G).
//  3. Filtrado anisotrópico en las texturas grandes: suelos y encimeras nítidos en ángulo.
//  4. 4× MSAA en el nivel de calidad de Android (recomendación de Meta para Quest).
//  5. Sondas de reflexión con proyección de caja por sala: reflejos correctos en suelo, vidrio y metal.
public static class Realismo
{
    const string PackDir = "Assets/3D Laboratory Environment with Appratus";
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string SondaDir = "Assets/NanoLab/Scenes/Microbiologia";
    const int Aniso = 8;

    [MenuItem("NanoLab/Aplicar realismo al entorno (sin hornear)", false, 8)]
    public static void Aplicar()
    {
        int tex = Texturas();
        int mats = Materiales();
        Calidad();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        int sondas = SondasReflexion();
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Realismo] Texturas: {tex} · materiales: {mats} · sondas de reflexión: {sondas} · MSAA 4× en Android.");
        MontarMicrobiologia.Capturas();
    }

    // ------------------------------------------------------------------ texturas

    static int Texturas()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PackDir, "Assets/FreeLabAssets" }))
        {
            var ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(ruta) is TextureImporter ti)) continue;
            bool cambio = false;
            bool mascara = ruta.Contains("MaskMap");   // datos (metal/oclusión/suavidad), no color
            if (mascara && ti.sRGBTexture) { ti.sRGBTexture = false; cambio = true; }
            if (ti.anisoLevel < Aniso) { ti.anisoLevel = Aniso; cambio = true; }
            if (ti.mipmapEnabled == false) { ti.mipmapEnabled = true; cambio = true; }
            if (!cambio) continue;
            ti.SaveAndReimport();
            n++;
        }
        return n;
    }

    // ------------------------------------------------------------------ materiales

    static int Materiales()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { PackDir + "/Materials" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || m.shader == null || m.shader.name != "Standard" || !m.HasProperty("_MetallicGlossMap")) continue;
            var mascara = m.GetTexture("_MetallicGlossMap");
            if (mascara == null) continue;
            // Standard lee la oclusión del canal G, igual que la MaskMap de HDRP.
            m.SetTexture("_OcclusionMap", mascara);
            m.SetFloat("_OcclusionStrength", 1f);
            m.SetFloat("_GlossMapScale", 1f);
            m.EnableKeyword("_METALLICGLOSSMAP");
            if (m.GetTexture("_BumpMap") != null) { m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            m.SetFloat("_SpecularHighlights", 1f);
            m.SetFloat("_GlossyReflections", 1f);
            m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.DisableKeyword("_GLOSSYREFLECTIONS_OFF");
            EditorUtility.SetDirty(m);
            n++;
        }
        AssetDatabase.SaveAssets();
        return n;
    }

    // ------------------------------------------------------------------ calidad

    // 4× MSAA y anisotrópico por textura en el nivel que usa Android (Quest).
    static void Calidad()
    {
        var qs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").FirstOrDefault();
        if (qs == null) return;
        var so = new SerializedObject(qs);
        int nivel = 2;
        var porPlataforma = so.FindProperty("m_PerPlatformDefaultQuality");
        if (porPlataforma != null)
            for (int i = 0; i < porPlataforma.arraySize; i++)
            {
                var e = porPlataforma.GetArrayElementAtIndex(i);
                if (e.FindPropertyRelative("first").stringValue == "Android") nivel = e.FindPropertyRelative("second").intValue;
            }
        var niveles = so.FindProperty("m_QualitySettings");
        if (niveles == null || nivel >= niveles.arraySize) return;
        var q = niveles.GetArrayElementAtIndex(nivel);
        q.FindPropertyRelative("antiAliasing").intValue = 4;
        q.FindPropertyRelative("anisotropicTextures").intValue = 1;   // "Per Texture": usa el anisoLevel de cada textura
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[Realismo] Calidad '{q.FindPropertyRelative("name").stringValue}' (Android): MSAA 4×.");
    }

    // ------------------------------------------------------------------ sondas de reflexión

    static readonly (string nombre, Vector3 centro, Vector3 tam)[] Salas =
    {
        ("ReflexionMicrobiologia", new Vector3(-2.16f, 0f, 8.1f), new Vector3(17.6f, 4.25f, 16.6f)),
        ("ReflexionAltoRiesgo", new Vector3((MontarSalaAltoRiesgo.X0 + MontarSalaAltoRiesgo.X1) / 2f, 0f, 21.1f), new Vector3(MontarSalaAltoRiesgo.X1 - MontarSalaAltoRiesgo.X0, 4.25f, 9.1f)),
        ("ReflexionCentro", new Vector3((MontarCentroExperimentacion.X0 + MontarCentroExperimentacion.X1) / 2f, 0f, (MontarCentroExperimentacion.Z0 + MontarCentroExperimentacion.Z1) / 2f),
            new Vector3(MontarCentroExperimentacion.X1 - MontarCentroExperimentacion.X0, 4.25f, MontarCentroExperimentacion.Z1 - MontarCentroExperimentacion.Z0)),
    };

    static int SondasReflexion()
    {
        float suelo = MontarLaboratorio.AlturaSuelo(MontarLaboratorio.BoundsEscena());
        var padre = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Iluminacion" && t.parent != null && t.parent.name.StartsWith("LAB_"));
        Directory.CreateDirectory(SondaDir);
        int n = 0;
        foreach (var (nombre, centro, tam) in Salas)
        {
            foreach (var vieja in Object.FindObjectsOfType<ReflectionProbe>(true).Where(r => r.name == nombre).ToArray())
                Object.DestroyImmediate(vieja.gameObject);
            var go = new GameObject(nombre);
            if (padre != null) go.transform.SetParent(padre, false);
            // A la altura de los ojos; la caja cubre la sala entera desde el suelo.
            go.transform.position = new Vector3(centro.x, suelo + 1.6f, centro.z);
            var p = go.AddComponent<ReflectionProbe>();
            p.mode = ReflectionProbeMode.Custom;
            p.boxProjection = true;
            p.size = tam;
            p.center = new Vector3(0f, tam.y / 2f - 1.6f, 0f);
            p.resolution = 256;
            p.hdr = true;
            p.importance = 2;
            p.blendDistance = 0.5f;
            string ruta = $"{SondaDir}/{nombre}.exr";
            if (Lightmapping.BakeReflectionProbe(p, ruta))
            {
                AssetDatabase.ImportAsset(ruta);
                p.customBakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(ruta);
                if (p.customBakedTexture == null) Debug.LogWarning($"[Realismo] {ruta} no se importó como cubemap.");
                else n++;
            }
            else Debug.LogWarning($"[Realismo] No se pudo hornear {nombre}.");
        }
        return n;
    }
}
