using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Utilidades compartidas por los montajes (colliders, suelo, luces horneadas, sondas, capturas).
// Antes también montaba la sala de Química (Laboratorio.unity), que se quitó del proyecto.
public static class MontarLaboratorio
{
    const string AnimaticsDir = "Assets/3D Laboratory Environment with Appratus";

    // ---------- Colliders ----------

    internal static int AnadirColliders()
    {
        int n = 0;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            if (r.GetComponent<Collider>() != null) continue;
            if (!r.TryGetComponent(out MeshFilter mf) || mf.sharedMesh == null) continue;
            var size = r.bounds.size;
            if (Mathf.Max(size.x, size.y, size.z) > 0.5f)
                r.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            else
                r.gameObject.AddComponent<BoxCollider>();
            n++;
        }
        return n;
    }

    internal static void MarcarEstatico()
    {
        var flags = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.ReflectionProbeStatic;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>())
        {
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);
            var s = r.bounds.size;
            // Los objetos pequeños se iluminan con sondas, no ocupan lightmap.
            r.receiveGI = Mathf.Max(s.x, s.y, s.z) < 0.5f ? ReceiveGI.LightProbes : ReceiveGI.Lightmaps;
        }
    }

    internal static Bounds BoundsEscena()
    {
        var rs = Object.FindObjectsOfType<MeshRenderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    internal static float AlturaSuelo(Bounds sala)
    {
        var floor = Object.FindObjectsOfType<MeshRenderer>()
            .Where(r => r.name.ToLowerInvariant().Contains("floor"))
            .OrderByDescending(r => r.bounds.size.x * r.bounds.size.z).FirstOrDefault();
        float y = floor ? floor.bounds.max.y : sala.min.y;
        Debug.Log($"[Montar] Altura del suelo: {y:F3} (objeto {(floor ? floor.name : "ninguno")})");
        return y;
    }

    internal static void CrearSueloSeguridad(Bounds sala, float suelo)
    {
        var go = new GameObject("SueloSeguridad");
        go.transform.position = new Vector3(sala.center.x, suelo - 0.1f, sala.center.z);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(sala.size.x + 10f, 0.2f, sala.size.z + 10f);
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    // ---------- Utilidades ----------

    internal static Bounds BoundsDe(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // ---------- Luz ----------

    internal static void ConfigurarLuces(string nombre)
    {
        foreach (var l in Object.FindObjectsOfType<Light>())
        {
            if (l.type == LightType.Directional) l.lightmapBakeType = LightmapBakeType.Mixed;
            else l.lightmapBakeType = LightmapBakeType.Baked;
        }

        var s = new LightingSettings
        {
            name = nombre,
            bakedGI = true,
            realtimeGI = false,
            lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
            mixedBakeMode = MixedLightingMode.Subtractive,
            lightmapResolution = 10f,
            lightmapPadding = 2,
            lightmapMaxSize = 1024,
            lightmapCompression = LightmapCompression.NormalQuality,
            directionalityMode = LightmapsMode.NonDirectional,
            maxBounces = 2,
            directSampleCount = 32,
            indirectSampleCount = 256,
            environmentSampleCount = 128,
            ao = true,
        };
        string path = "Assets/NanoLab/Scenes/" + nombre + ".lighting";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(s, path);
        Lightmapping.lightingSettings = s;
    }

    internal static void CrearSondas(Bounds sala, float suelo)
    {
        var go = new GameObject("SondasDeLuz");
        var lpg = go.AddComponent<LightProbeGroup>();
        var pts = new List<Vector3>();
        for (float x = sala.min.x + 1f; x <= sala.max.x - 1f; x += 2f)
            for (float z = sala.min.z + 1f; z <= sala.max.z - 1f; z += 2f)
                foreach (var y in new[] { 0.5f, 1.2f, 2.2f })
                    pts.Add(new Vector3(x, suelo + y, z));
        lpg.probePositions = pts.ToArray();
    }

    internal static void GenerarUVsDeLightmap()
    {
        var dirs = new[] { AnimaticsDir + "/Models", "Assets/FreeLabAssets/Models" };
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", dirs))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is ModelImporter mi && !mi.generateSecondaryUV)
            {
                mi.generateSecondaryUV = true;
                mi.SaveAndReimport();
                n++;
            }
        }
        Debug.Log($"[Montar] UVs de lightmap activadas en {n} modelos.");
    }

    // ---------- Capturas de verificación ----------

    internal static void Capturar(string dir, string nombre, Vector3 pos, Vector3 forward)
    {
        var go = new GameObject("CapturaTemp");
        var cam = go.AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward));
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.05f;
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        File.WriteAllBytes(Path.Combine(dir, nombre + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}
