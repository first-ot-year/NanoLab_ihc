using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using CS = ConvertirSketchfab;

// Convierte "Baby Baymax" (Sketchfab, hlaphoneshwe, CC BY 4.0) en el avatar del jugador:
//  - piezas separadas: cuerpo, cabeza (gira en el cuello), brazos (giran en el hombro), piernas;
//  - el modelo original está sentado: las piernas se giran 90° en la cadera para ponerlo de pie;
//  - se quita el plano negro del suelo, se reducen los polígonos (~22k) y las texturas a 512 px;
//  - materiales Standard (Built-in) y prefab en Assets/NanoLab/Modelos/Baymax/AvatarBaymax.prefab.
// Después se instala en el rig de las dos salas con un espejo en el recibidor (y en el anexo).
public static class ConvertirBaymax
{
    const string Fuente = "Assets/Descargas/baymax_model/baby_baymax.glb";
    const string Dir = "Assets/NanoLab/Modelos/Baymax";
    public const string PrefabPath = Dir + "/AvatarBaymax.prefab";
    const float Altura = 1.7f;
    static readonly string[] Escenas = { "Assets/NanoLab/Scenes/Microbiologia.unity", "Assets/NanoLab/Scenes/Laboratorio.unity" };

    // Piezas del glb (nombres de los MeshRenderer) y triángulos objetivo.
    static readonly (string parte, string[] piezas, int tris)[] Partes =
    {
        ("Cuerpo", new[] { "Object_5" }, 5000),
        ("Cabeza", new[] { "Object_11" }, 4000),
        ("BrazoIzq", new[] { "Object_7", "Object_19", "Object_21", "Object_23" }, 3600),   // -x: izquierda (mira a +z)
        ("BrazoDer", new[] { "Object_9", "Object_13", "Object_15", "Object_17" }, 3600),
        ("PiernaIzq", new[] { "Object_31" }, 1800),
        ("PiernaDer", new[] { "Object_28" }, 1800),
    };

    [MenuItem("NanoLab/Avatar Baymax/Convertir modelo e instalar en las salas", false, 30)]
    public static void ConvertirEInstalar()
    {
        Convertir();
        Instalar();
    }

    [MenuItem("NanoLab/Avatar Baymax/Convertir modelo", false, 31)]
    public static void Convertir()
    {
        Directory.CreateDirectory(Dir + "/Mallas");
        Directory.CreateDirectory(Dir + "/Texturas");
        AssetDatabase.Refresh();
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(Fuente);
        if (src == null) throw new System.Exception("No se pudo cargar " + Fuente);
        var inst = (GameObject)Object.Instantiate(src);
        inst.transform.position = Vector3.zero;   // se conserva la rotación de la raíz (corrección Y-arriba del glTF)
        var porNombre = inst.GetComponentsInChildren<MeshRenderer>(true).ToDictionary(r => r.name, r => r);
        Renderer[] Rs(string[] ns) => ns.Select(n => porNombre.TryGetValue(n, out var r) ? r : throw new System.Exception("Falta la pieza " + n)).ToArray();
        Bounds B(IEnumerable<Renderer> rs) { var l = rs.ToArray(); var b = l[0].bounds; foreach (var r in l) b.Encapsulate(r.bounds); return b; }

        var cuerpo = B(Rs(Partes[0].piezas));
        var cabeza = B(Rs(Partes[1].piezas));
        var brazoI = B(Rs(Partes[2].piezas));
        var brazoD = B(Rs(Partes[3].piezas));

        // Piernas: el original está sentado con las piernas hacia delante (+z). Se giran 90° en X
        // alrededor de la cadera (al fondo de la pierna, a la altura de su eje) para que cuelguen.
        Matrix4x4 Pierna(Renderer r)
        {
            var b = r.bounds;
            var cadera = new Vector3(b.center.x, b.center.y, cuerpo.center.z);
            return Matrix4x4.Translate(cadera) * Matrix4x4.Rotate(Quaternion.Euler(90f, 0f, 0f)) * Matrix4x4.Translate(-cadera);
        }
        var pose = new Dictionary<string, Matrix4x4>();
        foreach (var n in Partes[4].piezas.Concat(Partes[5].piezas)) pose[n] = Pierna(porNombre[n]);

        // Altura total de pie (pies a coronilla) para normalizar a 1,7 m con el pivote entre los pies.
        float pies = float.MaxValue;
        foreach (var n in pose.Keys)
        {
            var mesh = CS.GetMesh(porNombre[n]);
            var m = pose[n] * porNombre[n].transform.localToWorldMatrix;
            foreach (var v in mesh.vertices) pies = Mathf.Min(pies, m.MultiplyPoint3x4(v).y);
        }
        float s = Altura / (cabeza.max.y - pies);
        var G = Matrix4x4.Scale(Vector3.one * s) * Matrix4x4.Translate(new Vector3(-cuerpo.center.x, -pies, -cuerpo.center.z));
        Vector3 N(Vector3 p) => G.MultiplyPoint3x4(p);

        var pivotes = new Dictionary<string, Vector3>
        {
            { "Cuerpo", Vector3.zero }, { "PiernaIzq", Vector3.zero }, { "PiernaDer", Vector3.zero },
            { "Cabeza", N(new Vector3(cabeza.center.x, cabeza.min.y + cabeza.size.y * 0.12f, cabeza.center.z)) },
            { "BrazoIzq", N(new Vector3(brazoI.center.x, brazoI.max.y - brazoI.size.x * 0.25f, brazoI.center.z)) },
            { "BrazoDer", N(new Vector3(brazoD.center.x, brazoD.max.y - brazoD.size.x * 0.25f, brazoD.center.z)) },
        };

        var materiales = new Dictionary<Material, Material>();
        var raiz = new GameObject("AvatarBaymax");
        var av = raiz.AddComponent<AvatarBaymax>();
        int totalAntes = 0, totalDespues = 0;
        foreach (var (parte, piezas, tris) in Partes)
        {
            var rs = Rs(piezas);
            int antes = rs.Sum(r => CS.Triangles(CS.GetMesh(r)));
            float q = Mathf.Clamp01((float)tris / antes);
            var aPivote = Matrix4x4.Translate(-pivotes[parte]) * G;
            var porMat = new Dictionary<Material, List<CombineInstance>>();
            foreach (var r in rs)
            {
                var red = CS.Reducir(CS.GetMesh(r), q);
                var m = aPivote * (pose.TryGetValue(r.name, out var p) ? p : Matrix4x4.identity) * r.transform.localToWorldMatrix;
                var mat = Material(r.sharedMaterial, materiales);
                if (!porMat.TryGetValue(mat, out var l)) porMat[mat] = l = new List<CombineInstance>();
                for (int i = 0; i < red.subMeshCount; i++) l.Add(new CombineInstance { mesh = red, subMeshIndex = i, transform = m });
            }
            var subs = new List<CombineInstance>();
            foreach (var kv in porMat)
            {
                var sm = new Mesh { indexFormat = IndexFormat.UInt32 };
                sm.CombineMeshes(kv.Value.ToArray(), true, true);
                subs.Add(new CombineInstance { mesh = sm, transform = Matrix4x4.identity });
            }
            var mesh = new Mesh { name = "Baymax_" + parte, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(subs.ToArray(), false, true);
            mesh.RecalculateBounds();
            mesh.Optimize();
            string mp = $"{Dir}/Mallas/Baymax_{parte}.asset";
            AssetDatabase.DeleteAsset(mp);
            AssetDatabase.CreateAsset(mesh, mp);

            var pivote = new GameObject(parte == "Cabeza" ? "Cuello" : parte.StartsWith("Brazo") ? "Hombro" + parte.Substring(5) : parte).transform;
            pivote.SetParent(raiz.transform, false);
            pivote.localPosition = pivotes[parte];
            Transform t = pivote;
            if (pivote.name != parte)
            {
                t = new GameObject(parte).transform;
                t.SetParent(pivote, false);
            }
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = t.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterials = porMat.Keys.ToArray();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.lightProbeUsage = LightProbeUsage.BlendProbes;

            if (parte == "Cabeza") { av.cuello = pivote; av.cabeza = new Renderer[] { mr }; t.gameObject.layer = AvatarBaymax.CapaCabeza; }
            if (parte == "BrazoIzq") av.hombroIzq = pivote;
            if (parte == "BrazoDer") av.hombroDer = pivote;
            totalAntes += antes; totalDespues += CS.Triangles(mesh);
        }
        // Largo del brazo: del hombro a la punta de la mano (en reposo cuelga hacia -Y).
        var mallaBrazo = av.hombroIzq.GetComponentInChildren<MeshFilter>().sharedMesh;
        av.largoBrazo = -mallaBrazo.bounds.min.y;
        // Ojos: centro de la cabeza, un poco adelantados (la cámara queda dentro de la cabeza oculta).
        var cab = N(cabeza.center);
        av.ojos = new Vector3(cab.x, cab.y, cab.z + cabeza.extents.z * s * 0.35f);
        Object.DestroyImmediate(inst);

        AssetDatabase.DeleteAsset(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPath);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Baymax] Avatar convertido: {totalAntes} -> {totalDespues} triángulos, escala x{s:F3}, ojos {av.ojos:F2}, brazo {av.largoBrazo:F2} m, {materiales.Count} materiales.");
    }

    // Material Standard con la textura difusa del glTF reducida a 512 px.
    static Material Material(Material src, Dictionary<Material, Material> cache)
    {
        if (cache.TryGetValue(src, out var hecho)) return hecho;
        var tex = src.HasProperty("baseColorTexture") ? src.GetTexture("baseColorTexture") : src.mainTexture;
        string nombre = tex != null ? tex.name : src.name;
        string path = $"{Dir}/Baymax_{nombre}.mat";
        var dst = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (dst == null) { dst = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(dst, path); }
        dst.color = Color.white;
        dst.SetFloat("_Glossiness", 0.55f);
        dst.SetFloat("_Metallic", 0f);
        if (tex != null) dst.mainTexture = Extraer(tex, nombre);
        EditorUtility.SetDirty(dst);
        cache[src] = dst;
        return dst;
    }

    static Texture2D Extraer(Texture tex, string nombre)
    {
        string path = $"{Dir}/Texturas/Baymax_{nombre}.png";
        const int n = 512;
        var rt = RenderTexture.GetTemporary(n, n, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(tex, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var t = new Texture2D(n, n, TextureFormat.RGB24, true);
        t.ReadPixels(new Rect(0, 0, n, n), 0, 0);
        t.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter ti) { ti.maxTextureSize = 512; ti.mipmapEnabled = true; ti.SaveAndReimport(); }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ------------------------------------------------------------------ instalación

    [MenuItem("NanoLab/Avatar Baymax/Instalar en las dos salas", false, 32)]
    public static void Instalar()
    {
        foreach (var path in Escenas)
        {
            if (!File.Exists(path)) continue;
            var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            InstalarEn(s);
            EditorSceneManager.SaveScene(s);
        }
    }

    // Avatar en el rig del jugador + espejo en el recibidor (y en la recepción del anexo, si existe).
    public static void InstalarEn(Scene s)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogWarning("[Baymax] Falta el prefab del avatar: ejecuta 'Convertir modelo'."); return; }
        var rig = Object.FindObjectsOfType<OVRCameraRig>(true).FirstOrDefault();
        if (rig == null) { Debug.LogWarning($"[Baymax] {s.name}: no hay rig del jugador."); return; }
        var rigRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(rig.gameObject) ?? rig.gameObject;

        foreach (var viejo in Object.FindObjectsOfType<AvatarBaymax>(true)) Object.DestroyImmediate(PrefabUtility.GetOutermostPrefabInstanceRoot(viejo.gameObject) ?? viejo.gameObject);
        foreach (var viejo in Object.FindObjectsOfType<EspejoAvatar>(true)) Object.DestroyImmediate(viejo.gameObject);

        var av = (GameObject)PrefabUtility.InstantiatePrefab(prefab, rigRoot.transform);
        av.transform.localPosition = Vector3.zero;
        av.transform.localRotation = Quaternion.identity;

        // Espejo del recibidor: en la primera pared a la derecha del punto de aparición.
        var spawn = rigRoot.transform.position;
        Physics.SyncTransforms();
        var desde = new Vector3(spawn.x, spawn.y + 1.4f, spawn.z);
        if (Physics.Raycast(desde, Vector3.right, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            Espejo(rigRoot.scene, "EspejoRecibidor", new Vector3(hit.point.x - 0.03f, spawn.y, Mathf.Clamp(spawn.z + 0.3f, 0.9f, 3.2f)), -90f);
        else Debug.LogWarning($"[Baymax] {s.name}: no se encontró pared para el espejo del recibidor.");

        // Espejo en la recepción del Centro de Experimentación (pared oeste del anexo).
        if (Object.FindObjectOfType<RetoManager>(true) != null)
            Espejo(rigRoot.scene, "EspejoCentro", new Vector3(MontarCentroExperimentacion.X0 + 0.03f, spawn.y, 5.6f), 90f);
        Debug.Log($"[Baymax] {s.name}: avatar instalado en '{rigRoot.name}'.");
    }

    // Espejo de pie con marco; 'yaw' orienta su cara hacia la sala (la cara visible es -Z local).
    static void Espejo(Scene s, string nombre, Vector3 pie, float yaw)
    {
        var mats = MontarMicrobiologia.mats;
        if (mats == null || !mats.ContainsKey("Marco")) { MontarMicrobiologia.CrearMateriales(); mats = MontarMicrobiologia.mats; }
        var e = new GameObject(nombre).transform;
        SceneManager.MoveGameObjectToScene(e.gameObject, s);
        e.position = pie;
        e.rotation = Quaternion.Euler(0f, yaw + 180f, 0f);   // -Z local mira hacia 'yaw'
        float ancho = 1.0f, alto = 1.8f, base0 = 0.12f;
        MontarMicrobiologia.Caja(e, "Marco", new Vector3(0f, base0 + alto / 2f, 0.03f), new Vector3(ancho + 0.1f, alto + 0.1f, 0.04f), mats["Marco"], true);
        var cristal = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(cristal.GetComponent<Collider>());
        cristal.name = "Cristal";
        cristal.transform.SetParent(e, false);
        cristal.transform.localPosition = new Vector3(0f, base0 + alto / 2f, 0f);
        cristal.transform.localScale = new Vector3(ancho, alto, 1f);
        var mat = MontarMicrobiologia.MatEn("Assets/NanoLab/Materiales/Micro", "Espejo", Shader.Find("Unlit/Texture"));
        cristal.GetComponent<Renderer>().sharedMaterial = mat;
        var rotulo = MontarMicrobiologia.Texto3D(e, "<b>ESPEJO</b>\n<size=70%>Así te ves: eres Baby Baymax</size>", new Vector3(0f, base0 + alto + 0.15f, -0.01f), Quaternion.identity, 0.32f, new Color(0.4f, 0.9f, 1f), 1.2f);
        rotulo.alignment = TMPro.TextAlignmentOptions.Center;
        var esp = e.gameObject.AddComponent<EspejoAvatar>();
        esp.superficie = cristal.GetComponent<Renderer>();
        Debug.Log($"[Baymax] {nombre} en {pie:F2}");
    }

    // ------------------------------------------------------------------ verificación

    // Renderiza el avatar en varias poses y su reflejo en el espejo (Logs/Capturas/avatar_*.png).
    [MenuItem("NanoLab/Avatar Baymax/Capturas de prueba", false, 33)]
    public static void Capturas()
    {
        var s = EditorSceneManager.OpenScene(Escenas[0], OpenSceneMode.Single);
        var av = Object.FindObjectOfType<AvatarBaymax>(true);
        if (av == null) { Debug.LogError("[Baymax] No hay avatar en la escena."); return; }
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        float suelo = MontarLaboratorio.AlturaSuelo(MontarLaboratorio.BoundsEscena());
        var espejo = Object.FindObjectsOfType<EspejoAvatar>(true).FirstOrDefault(x => x.name == "EspejoRecibidor");

        // Jugador de 1,65 m delante del espejo, con los brazos en tres poses.
        var frente = espejo != null ? -espejo.superficie.transform.forward : Vector3.forward;
        var cabeza = (espejo != null ? espejo.transform.position + frente * 1.6f : new Vector3(-3.3f, suelo, 2.24f)) + Vector3.up * 1.65f;
        cabeza.y = suelo + 1.65f;
        var mira = Quaternion.LookRotation(-frente);
        var derecha = mira * Vector3.right;
        var poses = new (string, Vector3?, Vector3?)[]
        {
            ("reposo", null, null),
            ("manos_delante", cabeza + mira * new Vector3(-0.25f, -0.45f, 0.4f), cabeza + mira * new Vector3(0.25f, -0.45f, 0.4f)),
            ("saludo", cabeza + mira * new Vector3(-0.35f, -0.7f, 0.1f), cabeza + mira * new Vector3(0.45f, 0.15f, 0.25f)),
        };
        foreach (var (nombre, izq, der) in poses)
        {
            for (int i = 0; i < 3; i++) av.Posar(cabeza, mira, izq, der, suelo);
            // Vista en tercera persona (desde un lado).
            var lado = cabeza + derecha * 1.8f + frente * 0.6f - Vector3.up * 0.5f;
            MontarLaboratorio.Capturar(dir, "avatar_" + nombre, lado, (cabeza - Vector3.up * 0.6f) - lado);
            // Primera persona (mirando hacia abajo): la cabeza está en una capa que el jugador no ve.
            CapturarPrimeraPersona(dir, "avatar_" + nombre + "_1p", cabeza, mira * Quaternion.Euler(50f, 0f, 0f));
            if (espejo != null)
            {
                espejo.Preparar();
                espejo.RenderizarDesde(cabeza);
                MontarLaboratorio.Capturar(dir, "avatar_" + nombre + "_espejo", cabeza + frente * 0.2f, -frente);
            }
        }
        Debug.Log("[Baymax] Capturas del avatar guardadas.");
    }

    static void CapturarPrimeraPersona(string dir, string nombre, Vector3 pos, Quaternion rot)
    {
        var go = new GameObject("Cam1P");
        var cam = go.AddComponent<Camera>();
        cam.transform.SetPositionAndRotation(pos, rot);
        cam.fieldOfView = 90f; cam.nearClipPlane = 0.05f;
        cam.cullingMask &= ~(1 << AvatarBaymax.CapaCabeza);
        var rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
        File.WriteAllBytes(Path.Combine(dir, nombre + ".png"), tex.EncodeToPNG());
        RenderTexture.active = null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }
}
