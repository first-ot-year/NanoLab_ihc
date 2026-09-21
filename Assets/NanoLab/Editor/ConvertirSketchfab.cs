using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

// Convierte los modelos glTF de Sketchfab en prefabs ligeros para Quest:
// reduce polígonos, junta todo en una sola malla (una submalla por material),
// pasa los materiales a Standard (Built-in), ajusta la escala real y deja el
// pivote abajo en el centro.
public static class ConvertirSketchfab
{
    const string OutDir = "Assets/NanoLab/Modelos";

    class Modelo
    {
        public string gltf, nombre;
        public int trianglesObjetivo;
        public Medida medida;
        public float metros;
        // Descarta piezas cuyo ancho sea menor que esta fracción de la pieza más ancha
        // (el cultivo trae ADN y bacterias a otra escala dentro de la escena).
        public float soloPiezasGrandes;
        // Coloca cada pieza (placa) en fila en lugar de todas en el mismo punto.
        public bool separarEnFila;
    }
    enum Medida { Alto, AnchoPiezaMayor }

    static readonly Modelo[] Modelos =
    {
        new Modelo { gltf = "Assets/stereo_microscope/scene.gltf", nombre = "Estereomicroscopio",
                     trianglesObjetivo = 30000, medida = Medida.Alto, metros = 0.45f },
        new Modelo { gltf = "Assets/microbial_culture_petri_dish_-_agar_plate/scene.gltf", nombre = "PlacaPetri_Cultivo",
                     trianglesObjetivo = 30000, medida = Medida.AnchoPiezaMayor, metros = 0.10f, soloPiezasGrandes = 0.5f },
        new Modelo { gltf = "Assets/petri_dish_collection__by_3dlabware/scene.gltf", nombre = "PlacasPetri_Coleccion",
                     trianglesObjetivo = 25000, medida = Medida.AnchoPiezaMayor, metros = 0.15f, separarEnFila = true },
    };

    [MenuItem("NanoLab/Inspeccionar modelos Sketchfab")]
    public static void Inspeccionar()
    {
        foreach (var m in Modelos)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(m.gltf);
            if (root == null) { Debug.LogError($"[Sketchfab] No se pudo cargar {m.gltf}"); continue; }
            Debug.Log($"[Sketchfab] === {m.nombre}");
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = GetMesh(r);
                int tris = mesh != null ? Triangles(mesh) : 0;
                Debug.Log($"[Sketchfab]   {RutaDe(r.transform, root.transform)}  tris={tris}  bounds={r.bounds.size}  mats={string.Join(",", r.sharedMaterials.Select(x => x ? x.name + "(" + x.shader.name + ")" : "null"))}");
            }
        }
    }

    [MenuItem("NanoLab/Convertir modelos Sketchfab")]
    public static void Convertir()
    {
        Directory.CreateDirectory(OutDir + "/Mallas");
        Directory.CreateDirectory(OutDir + "/Materiales");
        AssetDatabase.Refresh();
        var vidrio = MaterialVidrio();

        foreach (var m in Modelos)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(m.gltf);
            if (root == null) { Debug.LogError($"[Sketchfab] No se pudo cargar {m.gltf}"); continue; }

            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(r => GetMesh(r) != null).ToArray();
            if (m.soloPiezasGrandes > 0)
            {
                float mayor = renderers.Max(Ancho);
                var descartadas = renderers.Where(r => Ancho(r) < mayor * m.soloPiezasGrandes).ToArray();
                renderers = renderers.Except(descartadas).ToArray();
                Debug.Log($"[Sketchfab] {m.nombre}: se descartan {descartadas.Length} piezas pequeñas ({string.Join(", ", descartadas.Select(d => d.name))})");
            }
            int total = renderers.Sum(r => Triangles(GetMesh(r)));
            float quality = Mathf.Clamp((float)m.trianglesObjetivo / total, 0.02f, 1f);

            // Escala: se mide antes de reducir, sobre las piezas originales.
            float medidaActual = m.medida == Medida.Alto
                ? Encapsular(renderers).size.y
                : renderers.Max(Ancho);
            float escala = medidaActual > 0 ? m.metros / medidaActual : 1f;

            // Espacio mundo: la raíz del glTF trae una rotación que tumba algunos modelos.
            var rootInv = Matrix4x4.identity;
            // Desplazamiento por pieza cuando se colocan en fila (de menor a mayor).
            var desplazamiento = new Dictionary<Renderer, Vector3>();
            if (m.separarEnFila)
            {
                float x = 0f;
                foreach (var r in renderers.OrderBy(Ancho))
                {
                    float w = Ancho(r) * escala;
                    var c = rootInv.MultiplyPoint3x4(r.bounds.center) * escala;
                    float baseY = rootInv.MultiplyPoint3x4(r.bounds.min).y * escala;
                    desplazamiento[r] = new Vector3(x + w / 2f - c.x, -baseY, -c.z);   // todas apoyadas en y=0
                    x += w + 0.02f;
                }
            }
            var porMaterial = new Dictionary<Material, List<CombineInstance>>();
            var convertidos = new Dictionary<Material, Material>();

            foreach (var r in renderers)
            {
                var src = GetMesh(r);
                var reducida = Reducir(src, quality);
                var mover = desplazamiento.TryGetValue(r, out var d) ? Matrix4x4.Translate(d) : Matrix4x4.identity;
                var matrix = mover * Matrix4x4.Scale(Vector3.one * escala) * rootInv * r.transform.localToWorldMatrix;

                var mats = r.sharedMaterials;
                for (int i = 0; i < reducida.subMeshCount; i++)
                {
                    var origen = i < mats.Length ? mats[i] : null;
                    if (origen == null) continue;
                    if (!convertidos.TryGetValue(origen, out var dst))
                    {
                        dst = EsVidrio(origen) ? vidrio : AStandard(origen, m.nombre);
                        convertidos[origen] = dst;
                    }
                    if (!porMaterial.TryGetValue(dst, out var lista))
                        porMaterial[dst] = lista = new List<CombineInstance>();
                    lista.Add(new CombineInstance { mesh = reducida, subMeshIndex = i, transform = matrix });
                }
            }

            // Una submalla por material.
            var partes = new List<CombineInstance>();
            var materialesFinales = new List<Material>();
            foreach (var kv in porMaterial)
            {
                var parte = new Mesh { indexFormat = IndexFormat.UInt32 };
                parte.CombineMeshes(kv.Value.ToArray(), true, true);
                partes.Add(new CombineInstance { mesh = parte, transform = Matrix4x4.identity });
                materialesFinales.Add(kv.Key);
            }
            var final = new Mesh { name = m.nombre, indexFormat = IndexFormat.UInt32 };
            final.CombineMeshes(partes.ToArray(), false, true);

            // Pivote abajo en el centro.
            final.RecalculateBounds();
            var b = final.bounds;
            var offset = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            // Ajuste fino de la altura sobre la malla ya combinada (los bounds del asset glTF no son fiables).
            float ajuste = m.medida == Medida.Alto && b.size.y > 0 ? m.metros / b.size.y : 1f;
            var verts = final.vertices;
            for (int i = 0; i < verts.Length; i++) verts[i] = (verts[i] + offset) * ajuste;
            final.vertices = verts;
            final.RecalculateBounds();
            Unwrapping.GenerateSecondaryUVSet(final);
            final.Optimize();

            string meshPath = $"{OutDir}/Mallas/{m.nombre}.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(final, meshPath);

            var go = new GameObject(m.nombre);
            go.AddComponent<MeshFilter>().sharedMesh = final;
            go.AddComponent<MeshRenderer>().sharedMaterials = materialesFinales.ToArray();
            go.AddComponent<BoxCollider>();
            PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{m.nombre}.prefab");
            Object.DestroyImmediate(go);

            Debug.Log($"[Sketchfab] {m.nombre}: {total} -> {Triangles(final)} triángulos, " +
                      $"escala x{escala:0.####}, tamaño final {final.bounds.size}, materiales {materialesFinales.Count}");
        }
        AssetDatabase.SaveAssets();
    }

    // Renderiza cada prefab convertido de frente, de lado y desde arriba
    // (Logs/Capturas/modelo_*.png) para comprobar orientación y escala.
    [MenuItem("NanoLab/Previsualizar modelos convertidos")]
    public static void Previsualizar()
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        string dir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        foreach (var m in Modelos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutDir}/{m.nombre}.prefab");
            if (prefab == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var b = go.GetComponent<Renderer>().bounds;
            float d = b.size.magnitude * 1.3f;
            var vistas = new (string, Vector3)[] { ("frente", Vector3.back), ("lado", Vector3.right), ("arriba", Vector3.up + Vector3.back * 0.01f) };
            foreach (var (nombre, dirVista) in vistas)
            {
                var camGo = new GameObject("cam");
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.25f, 0.25f, 0.3f);
                cam.nearClipPlane = 0.01f;
                cam.transform.position = b.center + dirVista.normalized * d;
                cam.transform.LookAt(b.center);
                var rt = new RenderTexture(640, 480, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(640, 480, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
                File.WriteAllBytes(System.IO.Path.Combine(dir, $"modelo_{m.nombre}_{nombre}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
                cam.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(camGo);
            }
            Object.DestroyImmediate(go);
        }
        Debug.Log($"[Sketchfab] Previsualizaciones en {dir}");
    }

    static Mesh GetMesh(Renderer r)
    {
        if (r is SkinnedMeshRenderer smr) return smr.sharedMesh;
        return r.TryGetComponent(out MeshFilter mf) ? mf.sharedMesh : null;
    }

    static int Triangles(Mesh m)
    {
        long n = 0;
        for (int i = 0; i < m.subMeshCount; i++) n += m.GetIndexCount(i);
        return (int)(n / 3);
    }

    // Reduce hasta ~quality del original; si el simplificador se queda corto,
    // repite sobre el resultado (hasta 4 pasadas).
    static Mesh Reducir(Mesh src, float quality)
    {
        if (quality >= 1f) return Object.Instantiate(src);
        int objetivo = Mathf.Max(64, Mathf.RoundToInt(Triangles(src) * quality));
        var actual = src;
        for (int pasada = 0; pasada < 4; pasada++)
        {
            var simp = new MeshSimplifier();
            var opciones = SimplificationOptions.Default;
            opciones.EnableSmartLink = true;   // une vértices duplicados para poder reducir más
            opciones.VertexLinkDistance = actual.bounds.size.magnitude * 1e-3;
            opciones.MaxIterationCount = 300;
            opciones.Agressiveness = 10;
            simp.SimplificationOptions = opciones;
            simp.Initialize(actual);
            simp.SimplifyMesh(Mathf.Clamp01((float)objetivo / Triangles(actual)));
            actual = simp.ToMesh();
            if (Triangles(actual) <= objetivo * 1.2f) break;
        }
        return actual;
    }

    static float Ancho(Renderer r) => Mathf.Max(r.bounds.size.x, r.bounds.size.z);

    static Bounds Encapsular(Renderer[] rs)
    {
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static string RutaDe(Transform t, Transform root) =>
        t == root ? t.name : RutaDe(t.parent, root) + "/" + t.name;

    static bool EsVidrio(Material m)
    {
        if (m.renderQueue >= 3000) return true;   // ya era transparente en glTF
        foreach (var p in new[] { "transmissionFactor", "_TransmissionFactor" })
            if (m.HasProperty(p) && m.GetFloat(p) > 0.01f) return true;
        if (m.shaderKeywords.Any(k => k.ToUpperInvariant().Contains("TRANSMISSION"))) return true;
        var n = m.name.ToLowerInvariant();
        return n.Contains("glass") || n.Contains("vidrio") || n.Contains("lens");
    }

    static Texture Tex(Material m, params string[] names)
    {
        foreach (var n in names) if (m.HasProperty(n) && m.GetTexture(n) != null) return m.GetTexture(n);
        return null;
    }

    static float Num(Material m, float def, params string[] names)
    {
        foreach (var n in names) if (m.HasProperty(n)) return m.GetFloat(n);
        return def;
    }

    static Material AStandard(Material src, string prefijo)
    {
        var dst = new Material(Shader.Find("Standard")) { name = $"{prefijo}_{src.name}" };
        var color = src.HasProperty("baseColorFactor") ? src.GetColor("baseColorFactor")
                  : src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor")
                  : src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
        dst.SetColor("_Color", color);

        var albedo = Tex(src, "baseColorTexture", "_BaseMap", "_MainTex");
        if (albedo != null) { dst.SetTexture("_MainTex", albedo); LimitarTextura(albedo, false); }

        dst.SetFloat("_Metallic", Num(src, 0f, "metallicFactor", "_Metallic"));
        dst.SetFloat("_Glossiness", 1f - Num(src, 0.5f, "roughnessFactor", "_Roughness"));

        var normal = Tex(src, "normalTexture", "_BumpMap");
        if (normal != null && LimitarTextura(normal, true))
        {
            dst.SetTexture("_BumpMap", normal);
            dst.EnableKeyword("_NORMALMAP");
        }

        if (color.a < 0.99f)
        {
            // Transparente (Fade)
            dst.SetFloat("_Mode", 2);
            dst.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            dst.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            dst.SetInt("_ZWrite", 0);
            dst.EnableKeyword("_ALPHABLEND_ON");
            dst.renderQueue = 3000;
        }

        string path = $"{OutDir}/Materiales/{Sanear(dst.name)}.mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(dst, path);
        return dst;
    }

    static Material MaterialVidrio()
    {
        string path = $"{OutDir}/Materiales/Vidrio.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        m = new Material(Shader.Find("Standard")) { name = "Vidrio" };
        m.SetColor("_Color", new Color(0.85f, 0.95f, 1f, 0.25f));
        m.SetFloat("_Glossiness", 0.9f);
        m.SetFloat("_Mode", 2);
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_ALPHABLEND_ON");
        m.renderQueue = 3000;
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // Texturas a 1024 como máximo (Quest). Devuelve false si no se puede
    // marcar como normal map (textura embebida en el glTF).
    static bool LimitarTextura(Texture t, bool esNormal)
    {
        var path = AssetDatabase.GetAssetPath(t);
        if (AssetImporter.GetAtPath(path) is TextureImporter ti)
        {
            bool cambio = false;
            if (ti.maxTextureSize > 1024) { ti.maxTextureSize = 1024; cambio = true; }
            if (esNormal && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; cambio = true; }
            if (cambio) ti.SaveAndReimport();
            return true;
        }
        return !esNormal;
    }

    static string Sanear(string s)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
}
