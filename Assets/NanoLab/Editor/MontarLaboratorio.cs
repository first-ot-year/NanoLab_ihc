using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Monta la escena Laboratorio.unity a partir del laboratorio de Animatics:
// colliders, props descargados sobre las mesas, OVRPlayerController e
// iluminación horneada para Quest.
public static class MontarLaboratorio
{
    const string AnimaticsDir = "Assets/3D Laboratory Environment with Appratus";
    const string SourceScene = AnimaticsDir + "/Scenes/Laboratory Scene.unity";
    const string SceneDir = "Assets/NanoLab/Scenes";
    const string ScenePath = SceneDir + "/Laboratorio.unity";
    const string PlayerPrefab = "Packages/com.meta.xr.sdk.core/Prefabs/OVRPlayerController.prefab";
    const string PropsRoot = "Props_Descargados";

    static readonly string[] Microbiologia =
    {
        "Assets/NanoLab/Modelos/Estereomicroscopio.prefab",
        "Assets/NanoLab/Modelos/PlacasPetri_Coleccion.prefab",
        "Assets/NanoLab/Modelos/PlacaPetri_Cultivo.prefab",
    };

    [MenuItem("NanoLab/Montar laboratorio")]
    public static void Montar()
    {
        GenerarUVsDeLightmap();

        if (!AssetDatabase.IsValidFolder("Assets/NanoLab")) AssetDatabase.CreateFolder("Assets", "NanoLab");
        if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder("Assets/NanoLab", "Scenes");
        AssetDatabase.DeleteAsset(ScenePath);
        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
            throw new System.Exception("No se pudo copiar " + SourceScene);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Punto de aparición = cámara original de la escena.
        var cam = Object.FindObjectsOfType<Camera>().FirstOrDefault();
        Vector3 spawn = cam ? cam.transform.position : Vector3.zero;
        float yaw = cam ? cam.transform.eulerAngles.y : 0f;
        if (cam) Object.DestroyImmediate(cam.gameObject);

        int colliders = AnadirColliders();
        MarcarEstatico();
        Bounds sala = BoundsEscena();
        float suelo = AlturaSuelo(sala);
        CrearSueloSeguridad(sala, suelo);

        var mesas = Object.FindObjectsOfType<Transform>()
            .Where(t => t.name.StartsWith("table with drawers") && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
            .ToList();
        Debug.Log($"[Montar] Mesas encontradas: {mesas.Count} ({string.Join(" | ", mesas.Select(m => m.position.ToString("F2")))})");

        var props = new GameObject(PropsRoot).transform;
        var mesaMicro = MesaMasCercana(mesas, 4.9f);
        var mesaQuim = MesaMasCercana(mesas, 0.81f, mesaMicro);
        ColocarEnMesa(mesaMicro, Microbiologia, NuevoGrupo(props, "Microbiologia"));
        var freeLab = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/FreeLabAssets/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
        ColocarEnMesa(mesaQuim, freeLab, NuevoGrupo(props, "Quimica"), normalizar: true);

        ColocarJugador(spawn, yaw, suelo, sala);
        ConfigurarLuces();
        CrearSondas(sala, suelo);

        AgruparLab.Agrupar(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Montar] Colliders añadidos: {colliders}. Horneando iluminación...");
        bool ok = Lightmapping.Bake();
        Debug.Log(ok ? "[Montar] Bake completado." : "[Montar] El bake falló o se canceló.");
        EditorSceneManager.SaveScene(scene);

        var escenas = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
        escenas.AddRange(EditorBuildSettings.scenes.Where(s => s.path != ScenePath));
        EditorBuildSettings.scenes = escenas.ToArray();

        Capturas();
    }

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

    // ---------- Props ----------

    static Transform NuevoGrupo(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static Transform MesaMasCercana(List<Transform> mesas, float z, Transform excluir = null) =>
        mesas.Where(m => m != excluir).OrderBy(m => Mathf.Abs(m.position.z - z)).FirstOrDefault();

    internal static Bounds BoundsDe(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void ColocarEnMesa(Transform mesa, string[] prefabs, Transform grupo, bool normalizar = false)
    {
        if (mesa == null) { Debug.LogError($"[Montar] No hay mesa para {grupo.name}"); return; }
        var tb = BoundsDe(mesa.gameObject);
        float top = tb.max.y;
        int largo = tb.size.x >= tb.size.z ? 0 : 2;
        int corto = 2 - largo;
        const float margen = 0.08f, paso = 0.05f, hueco = 0.03f;

        // Lo que ya está encima de la mesa.
        var region = new Bounds(new Vector3(tb.center.x, top + 0.4f, tb.center.z), new Vector3(tb.size.x, 0.8f, tb.size.z));
        var ocupado = Object.FindObjectsOfType<Renderer>()
            .Where(r => !r.transform.IsChildOf(mesa) && !r.transform.IsChildOf(grupo.root) && r.bounds.Intersects(region)
                        && Mathf.Max(r.bounds.size.x, r.bounds.size.z) < 1.2f)   // ignora paredes/techo/suelo
            .Select(r => r.bounds).ToList();

        foreach (var path in prefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogError($"[Montar] Falta {path}"); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, grupo);
            go.transform.position = Vector3.zero;

            var b = BoundsDe(go);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (normalizar && (max > 0.6f || max < 0.03f))
            {
                go.transform.localScale *= 0.2f / max;
                Debug.Log($"[Montar] {go.name}: escala corregida (medía {max:F3} m)");
                b = BoundsDe(go);
            }
            Vector3 pivoteAlCentro = go.transform.position - b.center;
            float medioLargo = b.extents[largo], medioCorto = b.extents[corto];

            bool colocado = false;
            var laterales = new[] { 0f, -tb.extents[corto] * 0.5f, tb.extents[corto] * 0.5f };
            for (float p = tb.min[largo] + margen + medioLargo; p <= tb.max[largo] - margen - medioLargo && !colocado; p += paso)
            {
                foreach (var lat in laterales)
                {
                    var c = tb.center;
                    c[largo] = p;
                    c[corto] = tb.center[corto] + lat;
                    if (Mathf.Abs(c[corto] - tb.center[corto]) + medioCorto > tb.extents[corto] - 0.02f) continue;
                    c.y = top + b.extents.y + 0.002f;
                    var candidato = new Bounds(c, b.size + Vector3.one * hueco);
                    if (ocupado.Any(o => o.Intersects(candidato))) continue;

                    go.transform.position = c + pivoteAlCentro;
                    ocupado.Add(new Bounds(c, b.size));
                    colocado = true;
                    break;
                }
            }
            if (!colocado)
            {
                var c = tb.center; c.y = top + b.extents.y + 0.002f;
                go.transform.position = c + pivoteAlCentro;
                Debug.LogWarning($"[Montar] {go.name}: no había hueco libre en la mesa, se dejó en el centro.");
            }
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.receiveGI = ReceiveGI.LightProbes;
            if (go.GetComponentInChildren<Collider>() == null) go.AddComponent<BoxCollider>();
            Debug.Log($"[Montar] {grupo.name}: {go.name} en {go.transform.position.ToString("F2")}");
        }
    }

    // ---------- Jugador ----------

    internal static GameObject ColocarJugador(Vector3 spawn, float yaw, float suelo, Bounds sala)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
        if (prefab == null) { Debug.LogError("[Montar] No se encontró OVRPlayerController.prefab"); return null; }
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var cc = player.GetComponent<CharacterController>();
        float h = cc ? cc.height : 2f, r = cc ? cc.radius : 0.3f, cy = cc ? cc.center.y : 0f;

        Physics.SyncTransforms();
        var pos = new Vector3(spawn.x, suelo, spawn.z);
        if (Bloqueado(pos, r))
        {
            pos = BuscarLibre(pos, suelo, r, sala);
            Debug.Log($"[Montar] El punto de la cámara original estaba ocupado; jugador movido a {pos.ToString("F2")}");
        }
        player.transform.position = new Vector3(pos.x, suelo + h / 2f - cy + 0.05f, pos.z);
        player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        Debug.Log($"[Montar] OVRPlayerController en {player.transform.position.ToString("F2")}, mirando a {yaw:F0}°");
        return player;
    }

    internal static bool Bloqueado(Vector3 pies, float r) =>
        Physics.CheckCapsule(pies + Vector3.up * (r + 0.1f), pies + Vector3.up * 1.7f, r + 0.1f);

    static Vector3 BuscarLibre(Vector3 desde, float suelo, float r, Bounds sala)
    {
        Vector3 mejor = desde; float dist = float.MaxValue;
        for (float x = sala.min.x + 0.5f; x < sala.max.x - 0.5f; x += 0.25f)
            for (float z = sala.min.z + 0.5f; z < sala.max.z - 0.5f; z += 0.25f)
            {
                var p = new Vector3(x, suelo, z);
                float d = (p - desde).sqrMagnitude;
                if (d < dist && !Bloqueado(p, r)) { dist = d; mejor = p; }
            }
        return mejor;
    }

    // ---------- Luz ----------

    internal static void ConfigurarLuces(string nombre = "Laboratorio_Lighting")
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
        string path = SceneDir + "/" + nombre + ".lighting";
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

    [MenuItem("NanoLab/Capturas de verificación")]
    public static void Capturas()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);

        var player = Object.FindObjectsOfType<CharacterController>().FirstOrDefault();
        var sala = BoundsEscena();
        if (player != null)
        {
            var eye = player.transform.position + Vector3.up * 0.7f;
            Capturar(dir, "1_vista_jugador", eye, player.transform.forward);
        }
        foreach (var nombre in new[] { "Microbiologia", "Quimica" })
        {
            var g = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name == nombre && t.parent != null && t.parent.name == PropsRoot)?.gameObject;
            if (g == null || g.transform.childCount == 0) continue;
            var b = BoundsDe(g);
            var haciaSala = new Vector3(sala.center.x - b.center.x, 0, sala.center.z - b.center.z).normalized;
            if (haciaSala == Vector3.zero) haciaSala = Vector3.right;
            var pos = b.center + haciaSala * 1.1f + Vector3.up * 0.55f;
            Capturar(dir, "2_" + nombre, pos, b.center - pos);
        }
        Capturar(dir, "3_vista_general", new Vector3(sala.center.x, sala.max.y - 0.3f, sala.min.z + 0.5f),
                 new Vector3(0, -0.45f, 1));
        Debug.Log($"[Montar] Capturas guardadas en {dir}");
    }

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
