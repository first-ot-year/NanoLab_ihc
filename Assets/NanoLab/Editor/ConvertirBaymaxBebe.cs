using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using MM = MontarMicrobiologia;

// Convierte el Baymax Bebé v4.2 (FBX con esqueleto hecho en Blender por un compañero; fuente en
// Fuentes_Blender/BaymaxBebe_v4_2_Integracion) en el avatar del jugador:
//  - importador: rig Generic, ejes horneados, clips Idle/Walk/Walk_Giro en bucle y sin el prefijo
//    "Baymax_Rig|", materiales del FBX cambiados por dos Standard propios (marfil y negro);
//  - solo el LOD1 (12 500 triángulos; el LOD0 de 50 000 es demasiado para el Quest, RNF-08);
//  - la malla del cuerpo se parte por pesos de los huesos en "Manos" (con la muñeca tapada, capa 0: se ven en primera
//    persona) y "Cuerpo" (capa 29, solo los espejos), compartiendo esqueleto;
//  - Animator Idle <-> Walk y la pose de puño sacada del clip Gestos_Manos;
//  - prefab Assets/NanoLab/Modelos/BaymaxBebe/AvatarBaymaxBebe.prefab con AvatarBaymaxBebe.
// ConvertirBaymax.InstalarEn lo pone en el rig de las salas (o el AvatarBaymax viejo si falta).
public static class ConvertirBaymaxBebe
{
    const string Dir = "Assets/NanoLab/Modelos/BaymaxBebe";
    const string Fbx = Dir + "/BaymaxBebe_v4_2.fbx";
    public const string PrefabPath = Dir + "/AvatarBaymaxBebe.prefab";
    const string Controlador = Dir + "/BaymaxBebe.controller";
    static readonly string[] Bucle = { "Idle", "Walk", "Walk_Giro" };
    static readonly string[] Dedos = { "Indice", "Medio", "Menique", "Pulgar" };
    // Las manos se cortan con un plano perpendicular al antebrazo, esto antes de la articulación de la muñeca
    // (m, a escala del modelo); el corte se tapa (PartirManos). Con los antebrazos, tapaban la mesa.
    const float CorteMuneca = 0.03f;

    public static bool Disponible => File.Exists(Fbx);

    [MenuItem("NanoLab/Avatar Baymax/Baymax Bebé v4.2: convertir e instalar", false, 29)]
    public static void ConvertirEInstalar()
    {
        if (Convertir() != null) ConvertirBaymax.Instalar();
    }

    public static GameObject Convertir()
    {
        if (!Disponible) { Debug.LogError("[BaymaxBebe] Falta " + Fbx); return null; }
        Directory.CreateDirectory(Dir + "/Mallas");
        var marfil = MM.MatEn(Dir, "BaymaxMarfil", Shader.Find("Standard"));
        marfil.color = new Color(0.86f, 0.84f, 0.80f).gamma;   // valores de Blender, en lineal
        marfil.SetFloat("_Glossiness", 0.58f);
        marfil.SetFloat("_Metallic", 0f);
        var negro = MM.MatEn(Dir, "BaymaxOjos", Shader.Find("Standard"));
        negro.color = Color.black;
        negro.SetFloat("_Glossiness", 0f);
        negro.SetFloat("_Metallic", 0f);
        AssetDatabase.SaveAssets();

        var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        imp.globalScale = 1f;
        imp.useFileScale = true;
        imp.bakeAxisConversion = true;
        imp.importBlendShapes = false;
        imp.importCameras = false;
        imp.importLights = false;
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.skinWeights = ModelImporterSkinWeights.Standard;
        imp.importAnimation = true;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.SaveAndReimport();
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.name = c.name.Substring(c.name.IndexOf('|') + 1);
            c.loopTime = Bucle.Contains(c.name);
        }
        imp.clipAnimations = clips;
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Baymax_Marfil_Satinado"), marfil);
        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Baymax_Ojos_Negro"), negro);
        imp.SaveAndReimport();

        var animClips = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToDictionary(c => c.name);
        AnimationClip Clip(string n) => animClips.TryGetValue(n, out var c) ? c : throw new System.Exception($"[BaymaxBebe] Falta el clip {n}");

        var raiz = new GameObject("AvatarBaymaxBebe");
        var av = raiz.AddComponent<AvatarBaymaxBebe>();
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        modelo.name = "Modelo";
        modelo.transform.SetParent(raiz.transform, false);
        Object.DestroyImmediate(modelo.GetComponent<LODGroup>());
        foreach (var n in new[] { "Cuerpo_LOD0", "Cara_LOD0" })
        {
            var t = modelo.transform.Find(n);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        var cuerpo = modelo.transform.Find("Cuerpo_LOD1").GetComponent<SkinnedMeshRenderer>();
        var cara = modelo.transform.Find("Cara_LOD1").GetComponent<SkinnedMeshRenderer>();
        cuerpo.name = "Cuerpo";
        cara.name = "Cara";
        float alto = cuerpo.bounds.size.y;
        int trisCara = cara.sharedMesh.triangles.Length / 3;

        // El FBX mira a -Z (la cara está delante del centro en -Z): se gira para que mire a +Z como el rig.
        if (cara.bounds.center.z < 0f) modelo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        var caraB = cara.bounds;
        if (caraB.center.z <= 0f) Debug.LogError("[BaymaxBebe] La cara no queda hacia +Z: revisa los ejes del FBX.");

        Transform H(string n) => modelo.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == n)
                                 ?? throw new System.Exception($"[BaymaxBebe] Falta el hueso {n}");
        av.animator = modelo.GetComponent<Animator>();
        av.cuello = H("Cuello");
        av.cabeza = H("Cabeza");
        av.brazoIzq = H("Brazo.L"); av.antebrazoIzq = H("Antebrazo.L"); av.manoIzq = H("Mano.L");
        av.brazoDer = H("Brazo.R"); av.antebrazoDer = H("Antebrazo.R"); av.manoDer = H("Mano.R");
        av.musloIzq = H("Muslo.L"); av.musloDer = H("Muslo.R");
        av.dedosIzq = av.manoIzq.GetComponentsInChildren<Transform>(true).Where(t => t != av.manoIzq).ToArray();
        av.dedosDer = av.manoDer.GetComponentsInChildren<Transform>(true).Where(t => t != av.manoDer).ToArray();
        // Ojos dentro de la cabeza, un poco por detrás de la cara (la cabeza no se ve en primera persona).
        var ojosLocal = raiz.transform.InverseTransformPoint(caraB.center);
        av.ojos = new Vector3(0f, ojosLocal.y, ojosLocal.z * 0.45f);

        PunoDesdeClip(modelo, Clip("Gestos_Manos"), av);

        // Animator: Idle <-> Walk según la velocidad; Walk con su ritmo.
        AssetDatabase.DeleteAsset(Controlador);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(Controlador);
        ac.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ac.AddParameter("Ritmo", AnimatorControllerParameterType.Float);
        var ps = ac.parameters;
        ps[1].defaultFloat = 1f;
        ac.parameters = ps;
        var sm = ac.layers[0].stateMachine;
        var idle = sm.AddState("Idle");
        idle.motion = Clip("Idle");
        var walk = sm.AddState("Walk");
        walk.motion = Clip("Walk");
        walk.speedParameterActive = true;
        walk.speedParameter = "Ritmo";
        sm.defaultState = idle;
        var aWalk = idle.AddTransition(walk);
        aWalk.hasExitTime = false; aWalk.duration = 0.15f;
        aWalk.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Velocidad");
        var aIdle = walk.AddTransition(idle);
        aIdle.hasExitTime = false; aIdle.duration = 0.2f;
        aIdle.AddCondition(AnimatorConditionMode.Less, 0.15f, "Velocidad");
        av.animator.runtimeAnimatorController = ac;
        av.animator.applyRootMotion = false;
        av.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Manos y cuerpo: dos mallas con el mismo esqueleto (ver PartirManos).
        var src = cuerpo.sharedMesh;
        var resto = new List<int>();
        var mallaManos = PartirManos(src, cuerpo.bones, resto, out int trisManos);
        var mallaCuerpo = Copia(src, "BaymaxBebe_Cuerpo", resto);
        cuerpo.sharedMesh = mallaCuerpo;
        var goManos = new GameObject("Manos");
        goManos.transform.SetParent(modelo.transform, false);
        goManos.transform.localPosition = cuerpo.transform.localPosition;
        goManos.transform.localRotation = cuerpo.transform.localRotation;
        goManos.transform.localScale = cuerpo.transform.localScale;
        var smrManos = goManos.AddComponent<SkinnedMeshRenderer>();
        smrManos.sharedMesh = mallaManos;
        smrManos.bones = cuerpo.bones;
        smrManos.rootBone = cuerpo.rootBone;
        smrManos.sharedMaterials = cuerpo.sharedMaterials;

        // Los brazos se estiran hacia los mandos: límites amplios para que no se recorten.
        foreach (var r in new[] { cuerpo, cara, smrManos })
        {
            r.localBounds = new Bounds(new Vector3(0f, 0.6f, 0f), Vector3.one * 3f);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
            r.lightProbeUsage = LightProbeUsage.BlendProbes;
            r.skinnedMotionVectors = false;
            r.forceMatrixRecalculationPerRender = true;   // el espejo renderiza en LateUpdate, antes del skinning del frame
            r.gameObject.layer = r == smrManos ? 0 : AvatarBaymax.CapaCabeza;
        }


        AssetDatabase.DeleteAsset(PrefabPath);
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabPath);
        Object.DestroyImmediate(raiz);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BaymaxBebe] Avatar en {PrefabPath}: alto {alto:F2} m, cara hacia +Z ({caraB.center.z:F2}), ojos {av.ojos:F2}, " +
                  $"manos {trisManos} triángulos, cuerpo {resto.Count / 3}, cara {trisCara}, clips {string.Join(", ", animClips.Keys)}.");
        return prefab;
    }

    static Mesh Copia(Mesh src, string nombre, List<int> tris)
    {
        var m = Object.Instantiate(src);
        m.name = nombre;
        m.subMeshCount = 1;
        m.SetTriangles(tris, 0);
        Guardar(m);
        return m;
    }

    static void Guardar(Mesh m)
    {
        string path = $"{Dir}/Mallas/{m.name}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
    }

    // Manos: los triángulos del antebrazo, la mano y los dedos recortados por un plano perpendicular al
    // antebrazo justo antes de la muñeca (los que lo cruzan se parten), y el corte tapado con un disco:
    // un anillo liso, sin borde dentado. El cuerpo ('resto') conserva todo lo demás, también los
    // triángulos que cruzan el plano, para que en el espejo no quede rendija entre las dos mallas.
    static Mesh PartirManos(Mesh src, Transform[] huesos, List<int> resto, out int trisManos)
    {
        var esBrazo = huesos.Select(b => b != null && (b.name.StartsWith("Antebrazo") || b.name.StartsWith("Mano") || Dedos.Any(d => b.name.StartsWith(d)))).ToArray();
        var v = new List<Vector3>(); src.GetVertices(v);
        var nor = new List<Vector3>(); src.GetNormals(nor);
        var tan = new List<Vector4>(); src.GetTangents(tan);
        var uv = new List<Vector2>(); src.GetUVs(0, uv);
        var col = new List<Color>(); src.GetColors(col);
        var bw = src.boneWeights.ToList();
        int nOriginal = v.Count;

        Vector3 EnMalla(string h) => src.bindposes[System.Array.FindIndex(huesos, b => b != null && b.name == h)].inverse.MultiplyPoint3x4(Vector3.zero);
        var planos = new[] { ".L", ".R" }.Select(l =>
        {
            var muneca = EnMalla("Mano" + l);
            var n = (muneca - EnMalla("Antebrazo" + l)).normalized;
            return (punto: muneca - n * CorteMuneca, normal: n);
        }).ToArray();
        int Lado(int i) => huesos[bw[i].boneIndex0] != null && huesos[bw[i].boneIndex0].name.EndsWith(".L") ? 0 : 1;
        float Dist(int i, int lado) => Vector3.Dot(v[i] - planos[lado].punto, planos[lado].normal);
        bool Brazo(int i)
        {
            var w = bw[i];
            return (esBrazo[w.boneIndex0] ? w.weight0 : 0f) + (esBrazo[w.boneIndex1] ? w.weight1 : 0f)
                 + (esBrazo[w.boneIndex2] ? w.weight2 : 0f) + (esBrazo[w.boneIndex3] ? w.weight3 : 0f) >= 0.5f;
        }
        int Nuevo(Vector3 p, Vector3 n, BoneWeight w)
        {
            v.Add(p);
            if (nor.Count > 0) nor.Add(n);
            if (tan.Count > 0) tan.Add(new Vector4(1f, 0f, 0f, 1f));
            if (uv.Count > 0) uv.Add(Vector2.zero);
            if (col.Count > 0) col.Add(Color.white);
            bw.Add(w);
            return v.Count - 1;
        }
        var cortes = new Dictionary<(int, int), int>();
        int Cortar(int a, int b, int lado)
        {
            var clave = a < b ? (a, b) : (b, a);
            if (cortes.TryGetValue(clave, out int c)) return c;
            float da = Dist(a, lado), db = Dist(b, lado), t = da / (da - db);
            var n = nor.Count > 0 ? Vector3.Lerp(nor[a], nor[b], t).normalized : Vector3.up;
            return cortes[clave] = Nuevo(Vector3.Lerp(v[a], v[b], t), n, Mezclar(bw[a], bw[b], t));
        }

        var tris = src.triangles;
        var manos = new List<int>();
        var borde = new List<(int salida, int entrada, int lado)>();
        var idx = new int[3];
        var d = new float[3];
        for (int i = 0; i < tris.Length; i += 3)
        {
            idx[0] = tris[i]; idx[1] = tris[i + 1]; idx[2] = tris[i + 2];
            int lado = Lado(idx[0]);
            bool brazo = Brazo(idx[0]) && Brazo(idx[1]) && Brazo(idx[2]);
            for (int k = 0; k < 3; k++) d[k] = Dist(idx[k], lado);
            int dentro = (d[0] > 0f ? 1 : 0) + (d[1] > 0f ? 1 : 0) + (d[2] > 0f ? 1 : 0);
            if (!brazo || dentro < 3) resto.AddRange(idx);
            if (!brazo || dentro == 0) continue;
            if (dentro == 3) { manos.AddRange(idx); continue; }
            // Sutherland-Hodgman con el mismo sentido de giro que el triángulo.
            var poli = new List<int>(4);
            int salida = -1, entrada = -1;
            for (int k = 0; k < 3; k++)
            {
                int a = idx[k], b = idx[(k + 1) % 3];
                bool aDentro = d[k] > 0f, bDentro = d[(k + 1) % 3] > 0f;
                if (aDentro) poli.Add(a);
                if (aDentro != bDentro)
                {
                    int c = Cortar(a, b, lado);
                    poli.Add(c);
                    if (aDentro) salida = c; else entrada = c;
                }
            }
            for (int j = 1; j + 1 < poli.Count; j++) { manos.Add(poli[0]); manos.Add(poli[j]); manos.Add(poli[j + 1]); }
            if (salida >= 0 && entrada >= 0) borde.Add((salida, entrada, lado));
        }

        // Tapa: abanico desde el centro del anillo, con vértices propios de normal hacia el codo.
        for (int lado = 0; lado < 2; lado++)
        {
            var es = borde.Where(e => e.lado == lado).ToList();
            if (es.Count == 0) continue;
            var anillo = es.SelectMany(e => new[] { e.salida, e.entrada }).Distinct().ToArray();
            var centro = Vector3.zero;
            foreach (var p in anillo) centro += v[p];
            centro /= anillo.Length;
            var hacia = -planos[lado].normal;
            var copia = anillo.ToDictionary(p => p, p => Nuevo(v[p], hacia, bw[p]));
            int c = Nuevo(centro, hacia, bw[anillo[0]]);
            foreach (var e in es) { manos.Add(copia[e.entrada]); manos.Add(copia[e.salida]); manos.Add(c); }
        }

        var m = Object.Instantiate(src);
        m.name = "BaymaxBebe_Manos";
        m.subMeshCount = 1;
        m.SetVertices(v);
        if (nor.Count == v.Count) m.SetNormals(nor);
        if (tan.Count == v.Count) m.SetTangents(tan);
        if (uv.Count == v.Count) m.SetUVs(0, uv);
        if (col.Count == v.Count) m.SetColors(col);
        m.boneWeights = bw.ToArray();
        m.SetTriangles(manos, 0);
        m.RecalculateBounds();
        Guardar(m);
        trisManos = manos.Count / 3;
        Debug.Log($"[BaymaxBebe] Manos recortadas en la muñeca: {borde.Count} aristas de corte, {v.Count - nOriginal} vértices nuevos.");
        return m;
    }

    // Pesos de un punto entre dos vértices: los 4 huesos que más pesan, normalizados.
    static BoneWeight Mezclar(BoneWeight a, BoneWeight b, float t)
    {
        var suma = new Dictionary<int, float>();
        void Sumar(int i, float w) { if (w > 0f) suma[i] = (suma.TryGetValue(i, out var x) ? x : 0f) + w; }
        Sumar(a.boneIndex0, a.weight0 * (1f - t)); Sumar(a.boneIndex1, a.weight1 * (1f - t));
        Sumar(a.boneIndex2, a.weight2 * (1f - t)); Sumar(a.boneIndex3, a.weight3 * (1f - t));
        Sumar(b.boneIndex0, b.weight0 * t); Sumar(b.boneIndex1, b.weight1 * t);
        Sumar(b.boneIndex2, b.weight2 * t); Sumar(b.boneIndex3, b.weight3 * t);
        var top = suma.OrderByDescending(kv => kv.Value).Take(4).ToArray();
        float total = top.Sum(kv => kv.Value);
        var r = new BoneWeight();
        if (top.Length > 0) { r.boneIndex0 = top[0].Key; r.weight0 = top[0].Value / total; }
        if (top.Length > 1) { r.boneIndex1 = top[1].Key; r.weight1 = top[1].Value / total; }
        if (top.Length > 2) { r.boneIndex2 = top[2].Key; r.weight2 = top[2].Value / total; }
        if (top.Length > 3) { r.boneIndex3 = top[3].Key; r.weight3 = top[3].Value / total; }
        return r;
    }

    // La pose de puño es el fotograma de Gestos_Manos con los dedos más doblados respecto al reposo.
    static void PunoDesdeClip(GameObject modelo, AnimationClip clip, AvatarBaymaxBebe av)
    {
        var huesos = modelo.GetComponentsInChildren<Transform>(true);
        var guardado = huesos.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();
        var dedos = av.dedosIzq.Concat(av.dedosDer).ToArray();
        var reposo = dedos.Select(t => t.localRotation).ToArray();
        float mejorT = 0f, mejor = -1f;
        for (float t = 0f; t <= clip.length + 1e-4f; t += 1f / 30f)
        {
            clip.SampleAnimation(modelo, t);
            float s = 0f;
            for (int i = 0; i < dedos.Length; i++)
                if (!dedos[i].name.StartsWith("Pulgar")) s += Quaternion.Angle(dedos[i].localRotation, reposo[i]);
            if (s > mejor) { mejor = s; mejorT = t; }
        }
        clip.SampleAnimation(modelo, mejorT);
        av.punoIzq = av.dedosIzq.Select(t => t.localRotation).ToArray();
        av.punoDer = av.dedosDer.Select(t => t.localRotation).ToArray();
        for (int i = 0; i < huesos.Length; i++)
        {
            huesos[i].localPosition = guardado[i].localPosition;
            huesos[i].localRotation = guardado[i].localRotation;
            huesos[i].localScale = guardado[i].localScale;
        }
        Debug.Log($"[BaymaxBebe] Puño de {clip.name} en t={mejorT:F2} s ({mejor / Mathf.Max(1, dedos.Length - 2):F0}° de media por falange).");
    }
}
