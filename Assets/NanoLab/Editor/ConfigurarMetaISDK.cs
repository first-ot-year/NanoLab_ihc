#pragma warning disable 618   // PhysicsGrabbable está marcado como obsoleto, pero es el que usa la guía del curso
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ML = MontarLaboratorio;

// Configura las salas para el Meta Interaction SDK, siguiendo la guía del curso:
//  - Jugador: solo OVRCameraRigInteraction (se quita el OVRPlayerController antiguo).
//  - Objetos agarrables: Rigidbody + Grabbable + GrabInteractable + PhysicsGrabbable +
//    HandGrabInteractable + DistanceGrabInteractable + DistanceHandGrabInteractable.
//  - Teletransporte: el suelo es TeleportInteractable; paredes y muebles bloquean el arco.
public static class ConfigurarMetaISDK
{
    const string RigPrefab = "Packages/com.meta.xr.sdk.interaction.ovr/Runtime/Prefabs/Legacy/OVRCameraRigInteraction.prefab";
    const string SlidePrefab = "Assets/NanoLab/Prefabs/Portaobjetos.prefab";
    static readonly string[] Escenas = { "Assets/NanoLab/Scenes/Laboratorio.unity", "Assets/NanoLab/Scenes/Microbiologia.unity" };
    static readonly Vector3 Spawn = new Vector3(-3.3f, 0f, 2.24f);
    const float Yaw = 24f;
    const float TamanoMaxAgarrable = 0.35f;

    [MenuItem("NanoLab/Configurar interacción Meta (las dos salas)", false, 22)]
    public static void Ambas()
    {
        ActualizarPrefabPortaobjetos();
        foreach (var path in Escenas)
        {
            if (!File.Exists(path)) continue;
            var s = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            Configurar(s);
            AgruparLab.Agrupar(s);
            EditorSceneManager.SaveScene(s);
        }
    }

    [MenuItem("NanoLab/Configurar interacción Meta (escena abierta)", false, 23)]
    public static void Abierta()
    {
        ActualizarPrefabPortaobjetos();
        var s = SceneManager.GetActiveScene();
        Configurar(s);
        EditorSceneManager.MarkSceneDirty(s);
    }

    public static void Configurar(Scene s)
    {
        var bounds = ML.BoundsEscena();
        float suelo = ML.AlturaSuelo(bounds);

        // 1) Un solo jugador: el rig de interacción de Meta.
        int quitados = 0;
        foreach (var pc in Object.FindObjectsOfType<OVRPlayerController>(true))
        {
            if (pc == null) continue;
            Object.DestroyImmediate(PrefabUtility.GetOutermostPrefabInstanceRoot(pc.gameObject) ?? pc.gameObject);
            quitados++;
        }
        var rig = Object.FindObjectsOfType<OVRCameraRig>(true).FirstOrDefault();
        if (rig == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefab);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, s);
            rig = inst.GetComponentInChildren<OVRCameraRig>(true);
        }
        var rigRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(rig.gameObject) ?? rig.gameObject;
        rigRoot.transform.SetPositionAndRotation(new Vector3(Spawn.x, suelo, Spawn.z), Quaternion.Euler(0f, Yaw, 0f));
        var mgr = rig.GetComponent<OVRManager>();
        if (mgr != null)
        {
            var so = new SerializedObject(mgr);
            var p = so.FindProperty("_trackingOriginType");
            if (p != null) { p.intValue = (int)OVRManager.TrackingOrigin.FloorLevel; so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        Debug.Log($"[MetaISDK] {s.name}: OVRPlayerController quitados: {quitados}. Rig '{rigRoot.name}' en la entrada, seguimiento a nivel de suelo.");

        // 2) Sin teletransporte por ahora (decisión de Carlo): se quita el que hubiera.
        int quitadosTp = QuitarTeletransporte();

        // 3) Objetos agarrables.
        var agarrables = BuscarAgarrables();
        foreach (var go in agarrables)
        {
            HacerAgarrable(go, 0.25f);
            // Los aparatos de las estanterías se quedan quietos hasta el primer agarre
            // (el pack los coloca flotando o solapados y se caerían al empezar).
            if (go.GetComponent<Portaobjetos>() == null)
            {
                go.GetComponent<Rigidbody>().isKinematic = true;
                Obtener<QuietoHastaAgarrar>(go);
            }
        }
        Debug.Log($"[MetaISDK] {s.name}: {agarrables.Count} objetos agarrables ({string.Join(", ", agarrables.Take(12).Select(g => g.name))}{(agarrables.Count > 12 ? "…" : "")}). Componentes de teletransporte quitados: {quitadosTp}.");
    }

    // ------------------------------------------------------------------ agarrables

    static List<GameObject> BuscarAgarrables()
    {
        var res = new HashSet<GameObject>();
        foreach (var t in Object.FindObjectsOfType<Transform>(true))
        {
            var go = t.gameObject;
            var raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            // Aparatos de química del pack de Animatics (los mismos que se quitan en Microbiología).
            if (raiz != null && raiz == go)
            {
                var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
                string nombre = ((src != null ? Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(src)) : "") + " " + go.name).ToLowerInvariant().Replace('_', ' ');
                if (MontarMicrobiologia.Aparatos.Any(a => nombre.Contains(a.Replace('_', ' '))) && Pequeno(go)) res.Add(go);
                // Portaobjetos (ya vienen configurados desde su prefab, pero se asegura).
                if (go.GetComponent<Portaobjetos>() != null) res.Add(go);
            }
            // Props descargados (matraces, pipetas, placas) de la sala de Química.
            if (t.parent != null && t.parent.parent != null && t.parent.parent.name == "Props_Descargados" && Pequeno(go)) res.Add(go);
        }
        return res.ToList();
    }

    static bool Pequeno(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return false;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return Mathf.Max(b.size.x, b.size.y, b.size.z) <= TamanoMaxAgarrable;
    }

    // Componentes de la guía del curso, enlazados entre sí.
    public static void HacerAgarrable(GameObject go, float masa)
    {
        // Un objeto estático no se mueve: se quitan los flags de estático y se ilumina con sondas.
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.receiveGI = ReceiveGI.LightProbes;

        // Colliders: los MeshCollider deben ser convexos con un Rigidbody dinámico.
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true)) mc.convex = true;
        if (!go.GetComponentsInChildren<Collider>(true).Any(c => !c.isTrigger)) AjustarBoxCollider(go);
        foreach (var hijo in go.GetComponentsInChildren<Rigidbody>(true)) if (hijo.gameObject != go) Object.DestroyImmediate(hijo);

        var rb = Obtener<Rigidbody>(go);
        rb.mass = masa;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var grab = Obtener<Grabbable>(go);
        var fisica = Obtener<PhysicsGrabbable>(go);
        Enlazar(fisica, ("_pointable", grab), ("_rigidbody", rb));
        Enlazar(Obtener<GrabInteractable>(go), ("_rigidbody", rb), ("_pointableElement", grab), ("_physicsGrabbable", fisica));
        Enlazar(Obtener<HandGrabInteractable>(go), ("_rigidbody", rb), ("_pointableElement", grab), ("_physicsGrabbable", fisica));
        Enlazar(Obtener<DistanceGrabInteractable>(go), ("_rigidbody", rb), ("_pointableElement", grab), ("_physicsGrabbable", fisica));
        Enlazar(Obtener<DistanceHandGrabInteractable>(go), ("_rigidbody", rb), ("_pointableElement", grab), ("_physicsGrabbable", fisica));

        if (PrefabUtility.IsPartOfPrefabInstance(go))
            foreach (var c in go.GetComponents<Component>()) PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }

    static T Obtener<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    static void Enlazar(Object comp, params (string prop, Object valor)[] campos)
    {
        var so = new SerializedObject(comp);
        foreach (var (prop, valor) in campos)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[MetaISDK] {comp.GetType().Name} no tiene el campo {prop}"); continue; }
            p.objectReferenceValue = valor;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AjustarBoxCollider(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var bc = go.AddComponent<BoxCollider>();
        if (rs.Length == 0) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        var t = go.transform;
        bc.center = t.InverseTransformPoint(b.center);
        var s = t.lossyScale;
        bc.size = new Vector3(b.size.x / Mathf.Max(1e-4f, Mathf.Abs(s.x)), b.size.y / Mathf.Max(1e-4f, Mathf.Abs(s.y)), b.size.z / Mathf.Max(1e-4f, Mathf.Abs(s.z)));
    }

    // ------------------------------------------------------------------ teletransporte

    // Quita las superficies/bloqueos de teletransporte de la escena (el rig de Meta no puede teletransportarse sin ellas).
    static int QuitarTeletransporte()
    {
        int n = 0;
        foreach (var tp in Object.FindObjectsOfType<TeleportInteractable>(true)) { Object.DestroyImmediate(tp); n++; }
        foreach (var cs in Object.FindObjectsOfType<ColliderSurface>(true))
            if (cs.GetComponentInParent<OVRCameraRig>() == null) { Object.DestroyImmediate(cs); n++; }
        return n;
    }

    // (No se usa por ahora.) Suelo teletransportable + bloqueos en paredes y muebles.
    static int ConfigurarTeletransporte(float suelo)
    {
        // Suelo teletransportable: el suelo de seguridad cubre toda la sala a la altura del piso.
        var sueloGo = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "SueloSeguridad")?.gameObject;
        if (sueloGo != null) Teleport(sueloGo, sueloGo.GetComponent<BoxCollider>(), true);
        else Debug.LogWarning("[MetaISDK] No hay SueloSeguridad: no se configuró el suelo de teletransporte.");

        // Bloqueos: paredes, vidrios, mesas y armarios (colliders grandes que no son el suelo).
        int n = 0;
        foreach (var c in Object.FindObjectsOfType<Collider>(true))
        {
            if (c.isTrigger || c.gameObject == sueloGo) continue;
            if (c.attachedRigidbody != null) continue;                 // objetos agarrables
            if (c.GetComponentInParent<OVRCameraRig>() != null) continue;
            var b = c.bounds;
            if (b.size.y < 0.5f || Mathf.Max(b.size.x, b.size.z) < 0.3f) continue;
            if (b.max.y < suelo + 0.3f) continue;                       // piso
            if (Teleport(c.gameObject, c, false)) n++;
        }
        return n;
    }

    static bool Teleport(GameObject go, Collider col, bool permitir)
    {
        if (col == null) return false;
        var sup = go.GetComponents<ColliderSurface>().FirstOrDefault(x => new SerializedObject(x).FindProperty("_collider").objectReferenceValue == col);
        if (sup == null)
        {
            sup = go.AddComponent<ColliderSurface>();
            Enlazar(sup, ("_collider", col));
        }
        var tp = go.GetComponents<TeleportInteractable>().FirstOrDefault(x => new SerializedObject(x).FindProperty("_surface").objectReferenceValue == sup);
        if (tp == null) tp = go.AddComponent<TeleportInteractable>();
        var so = new SerializedObject(tp);
        so.FindProperty("_surface").objectReferenceValue = sup;
        so.FindProperty("_allowTeleport").boolValue = permitir;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // ------------------------------------------------------------------ prefab del portaobjetos

    public static void ActualizarPrefabPortaobjetos()
    {
        if (!File.Exists(SlidePrefab)) return;
        var raiz = PrefabUtility.LoadPrefabContents(SlidePrefab);
        try
        {
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach (var r in raiz.GetComponents<Resaltado>()) Object.DestroyImmediate(r);
            var bc = raiz.GetComponent<BoxCollider>();
            if (bc != null) bc.size = new Vector3(0.075f, 0.006f, 0.026f);
            var p = raiz.GetComponent<Portaobjetos>();
            var brillo = raiz.transform.Find("Brillo");
            if (p != null && brillo != null) p.brillo = brillo.GetComponent<Renderer>();
            HacerAgarrable(raiz, 0.05f);
            PrefabUtility.SaveAsPrefabAsset(raiz, SlidePrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(raiz); }
        Debug.Log("[MetaISDK] Prefab Portaobjetos actualizado con el Interaction SDK.");
    }

    // ------------------------------------------------------------------ verificación

    // Simula 2 s de física en el Editor y avisa de los agarrables que se caen o se mueven
    // (por ejemplo, porque empiezan metidos en la mesa).
    [MenuItem("NanoLab/Probar física de los objetos agarrables", false, 24)]
    public static void ProbarFisica()
    {
        foreach (var path in Escenas)
        {
            if (!File.Exists(path)) continue;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var rbs = Object.FindObjectsOfType<Grabbable>(true).Select(g => g.GetComponent<Rigidbody>()).Where(r => r != null).ToList();
            var inicio = rbs.ToDictionary(r => r, r => r.position);
            var modo = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            for (int i = 0; i < 100; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = modo;
            var movidos = rbs.Where(r => (r.position - inicio[r]).magnitude > 0.03f).ToList();
            Debug.Log($"[Fisica] {Path.GetFileNameWithoutExtension(path)}: {rbs.Count} agarrables, {movidos.Count} se movieron más de 3 cm" +
                      (movidos.Count > 0 ? ": " + string.Join(", ", movidos.Take(15).Select(r => $"{r.name} ({(r.position - inicio[r]).magnitude:F2} m)")) : ""));
        }
    }
}
