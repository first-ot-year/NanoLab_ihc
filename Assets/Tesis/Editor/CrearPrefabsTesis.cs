using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Crea prefabs (variantes de los .fbx de la tesis) con los materiales que tenían
// asignados en las escenas originales. Solo usa MeshRenderer/MeshFilter.
// Se puede ejecutar varias veces: sobrescribe los prefabs de Assets/Tesis/Prefabs.
public static class CrearPrefabsTesis
{
    const string JsonPath = "Assets/Tesis/Editor/thesis_materials.json";
    const string OutDir = "Assets/Tesis/Prefabs";

    [System.Serializable] class Slot { public long id; public bool isMesh; public int index; public string material; }
    [System.Serializable] class Variant { public string name; public string model; public Slot[] slots; }
    [System.Serializable] class Data { public Variant[] variants; }

    [MenuItem("NanoLab/Crear prefabs de la tesis")]
    public static void Crear()
    {
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(JsonPath));
        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/Tesis", "Prefabs");

        int created = 0, warnings = 0;
        foreach (var v in data.variants)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(v.model);
            if (model == null)
            {
                Debug.LogWarning($"[CrearPrefabsTesis] No se encontró el modelo {v.model}");
                warnings++;
                continue;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                var renderers = inst.GetComponentsInChildren<Renderer>(true);
                foreach (var s in v.slots)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(s.material);
                    var target = FindRenderer(renderers, s);
                    if (mat == null || target == null)
                    {
                        Debug.LogWarning($"[CrearPrefabsTesis] {v.name}: no se pudo asignar {s.material} (renderer {s.id}, slot {s.index})");
                        warnings++;
                        continue;
                    }
                    var mats = target.sharedMaterials;
                    if (s.index >= mats.Length)
                        System.Array.Resize(ref mats, s.index + 1);
                    mats[s.index] = mat;
                    target.sharedMaterials = mats;
                }
                PrefabUtility.SaveAsPrefabAsset(inst, $"{OutDir}/{v.name}.prefab");
                created++;
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CrearPrefabsTesis] Prefabs creados: {created}. Avisos: {warnings}.");
    }

    const string LabSource = "Assets/Tesis/Editor/Origen/LabScene_tesis_NO_ABRIR.unity";
    const string LabPrefab = "Assets/Tesis/Prefabs/Laboratorio.prefab";

    // Reconstruye el laboratorio de la tesis (LabScene) como un solo prefab:
    // copia solo la parte visible (MeshFilter/MeshRenderer) y las luces, en la
    // misma posición que tenían. Nada de scripts ni componentes de XR.
    [MenuItem("NanoLab/Crear laboratorio completo")]
    public static void CrearLaboratorio()
    {
        if (!Application.isBatchMode && AssetDatabase.LoadAssetAtPath<GameObject>(LabPrefab) != null &&
            !EditorUtility.DisplayDialog("Laboratorio ya existe",
                "Se va a reemplazar el prefab Laboratorio. Perderás los colliders y los cambios que le hayas hecho " +
                "(también los que hiciste en la escena sobre él). ¿Continuar?", "Reemplazar", "Cancelar"))
            return;

        var source = EditorSceneManager.OpenScene(LabSource, OpenSceneMode.Additive);
        var root = new GameObject("Laboratorio");
        int objects = 0, lights = 0;
        try
        {
            foreach (var top in source.GetRootGameObjects())
            {
                if (!top.activeInHierarchy) continue;
                Transform group = null;

                foreach (var r in top.GetComponentsInChildren<Renderer>(false))
                {
                    Mesh mesh;
                    if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
                    else if (r is MeshRenderer && r.TryGetComponent(out MeshFilter mf)) mesh = mf.sharedMesh;
                    else continue;
                    if (mesh == null || !r.enabled) continue;

                    var meshPath = AssetDatabase.GetAssetPath(mesh);
                    if (!meshPath.StartsWith("Assets/Tesis/") && meshPath != "Library/unity default resources") continue;

                    if (group == null)
                    {
                        group = new GameObject(top.name).transform;
                        group.SetParent(root.transform, false);
                    }
                    var copy = new GameObject(r.name);
                    copy.transform.SetParent(group, false);
                    copy.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
                    copy.transform.localScale = r.transform.lossyScale;
                    copy.AddComponent<MeshFilter>().sharedMesh = mesh;

                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] == null) mats[i] = DefaultMaterial(meshPath, mesh, i);
                    copy.AddComponent<MeshRenderer>().sharedMaterials = mats;
                    objects++;
                }

                foreach (var l in top.GetComponentsInChildren<Light>(false))
                {
                    var luces = root.transform.Find("Luces");
                    if (luces == null)
                    {
                        luces = new GameObject("Luces").transform;
                        luces.SetParent(root.transform, false);
                    }
                    var copy = new GameObject(l.name);
                    copy.transform.SetParent(luces, false);
                    copy.transform.SetPositionAndRotation(l.transform.position, l.transform.rotation);
                    EditorUtility.CopySerialized(l, copy.AddComponent<Light>());
                    lights++;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, LabPrefab);
            Debug.Log($"[CrearPrefabsTesis] Laboratorio creado en {LabPrefab}: {objects} objetos, {lights} luces.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            EditorSceneManager.CloseScene(source, true);
        }
    }

    // Añade MeshCollider (no convexo, estático) a las piezas de la sala:
    // paredes, puerta, ventanas y marcos (hijos de "Environment").
    // Se salta Cube.001, la losa del suelo, que ya tiene collider en la escena.
    [MenuItem("NanoLab/Añadir colliders a paredes")]
    public static void AnadirCollidersParedes()
    {
        var contents = PrefabUtility.LoadPrefabContents(LabPrefab);
        int added = 0;
        try
        {
            var env = contents.transform.Find("Environment");
            if (env == null)
            {
                Debug.LogError("[CrearPrefabsTesis] No se encontró 'Environment' en el prefab Laboratorio.");
                return;
            }
            foreach (Transform t in env)
            {
                if (t.name == "Cube.001") continue;
                if (t.GetComponent<Collider>() != null) continue;
                if (!t.TryGetComponent(out MeshFilter mf) || mf.sharedMesh == null) continue;

                t.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                t.gameObject.isStatic = true;
                added++;
            }
            PrefabUtility.SaveAsPrefabAsset(contents, LabPrefab);
            Debug.Log($"[CrearPrefabsTesis] Colliders añadidos a {added} piezas de la sala.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    // Material por defecto del .fbx (o el de Unity) cuando el original no se copió.
    static Material DefaultMaterial(string meshPath, Mesh mesh, int index)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
        if (model != null)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                Mesh m = r is SkinnedMeshRenderer smr ? smr.sharedMesh
                       : r.TryGetComponent(out MeshFilter mf) ? mf.sharedMesh : null;
                if (m == mesh && index < r.sharedMaterials.Length && r.sharedMaterials[index] != null)
                    return r.sharedMaterials[index];
            }
        }
        return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
    }

    static Renderer FindRenderer(Renderer[] renderers, Slot s)
    {
        foreach (var r in renderers)
        {
            Object key;
            if (s.isMesh)
            {
                if (r is SkinnedMeshRenderer smr) key = smr.sharedMesh;
                else key = r.TryGetComponent(out MeshFilter mf) ? mf.sharedMesh : null;
            }
            else
            {
                key = PrefabUtility.GetCorrespondingObjectFromSource(r);
            }
            if (key != null && LocalId(key) == s.id)
                return r;
        }
        return null;
    }

    static long LocalId(Object o)
    {
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string _, out long id) ? id : 0;
    }
}
