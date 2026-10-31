using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ML = MontarLaboratorio;

// Colisiones del jugador con el mobiliario (sillas, armarios, mesas, estanterías). Problemas que corrige:
//  - Las sillas solo tenían un collider trigger (para el rayo): el jugador las atravesaba.
//  - El armario del paquete ("Box", raíz "almari") trae un BoxCollider de 1×1×1 m en el pivote que no
//    cubre el mueble; otras piezas del paquete solo tienen collider en una de sus mallas (cajones,
//    puertas de vidrio de las vitrinas), y por los huecos se podía entrar en el mueble.
//  - Objetos apoyados en el suelo añadidos después del montaje inicial se quedaron sin collider.
// A cada uno se le añade un hijo "BloqueoJugador": una caja ajustada a sus mallas en la capa
// CapasNanoLab.BloqueoJugador. Esa capa solo la encuentra el Locomotor del jugador (su CapsuleCast usa
// todas las capas); no choca con ningún objeto físico (los aparatos de las estanterías no salen
// disparados) y los rayos de interacción la ignoran (no tapa lo que hay dentro de una vitrina).
// Se puede repetir: quita los bloqueos anteriores antes de crear los nuevos.
public static class AsegurarColisiones
{
    const string Quimica = "Assets/NanoLab/Scenes/Laboratorio.unity";
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Pack = "Assets/3D Laboratory Environment with Appratus/";
    static string Nombre => CapasNanoLab.NombreBloqueo;

    // Muebles del paquete (nombre del prefab o del modelo).
    static readonly string[] MueblesPack =
        { "box", "table with drawers", "shelf showcase", "showcase", "water shelf", "rack", "shelf", "shelf (2)", "analytical_lab_instruments", "chair" };
    // Cualquier objeto propio o importado cuyo nombre indique un mueble.
    static readonly string[] PalabrasMueble =
        { "armario", "almari", "cabinet", "cupboard", "locker", "closet", "wardrobe", "estante", "vitrina", "showcase", "mueble", "gabinete", "taquilla", "cajonera" };
    // Nunca bloquean: efectos, señalización, puertas y portales (hay que poder cruzarlos).
    static readonly string[] Excluir =
        { "haz", "holograma", "anillo", "rejilla", "umbral", "franja", "baliza", "rotulo", "cartel", "senal", "aviso", "luz", "puerta", "portal", "espejo", "marco", "cabinadesinfeccion", "area_" };

    const float AltoMax = 2.2f;          // por encima no hace falta bloquear (el jugador no llega)
    const float HuellaMin = 0.2f;        // objetos más estrechos no estorban (patas sueltas, haces de luz)
    const float SillaHuella = 0.5f;      // huella máxima del bloqueo de una silla (m)

    // ------------------------------------------------------------------ menús

    [MenuItem("NanoLab/Asegurar colisiones (las dos salas)", false, 29)]
    public static void AmbasSalas()
    {
        CapaYMatriz();
        SillasPrefab();
        foreach (var ruta in new[] { Microbiologia, Quimica })
        {
            if (!File.Exists(ruta)) continue;
            var s = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            Aplicar(s);
            EditorSceneManager.SaveScene(s);
        }
    }

    [MenuItem("NanoLab/Asegurar colisiones (solo la escena abierta)", false, 30)]
    public static void EscenaAbierta()
    {
        CapaYMatriz();
        SillasPrefab();
        var s = SceneManager.GetActiveScene();
        Aplicar(s);
        EditorSceneManager.MarkSceneDirty(s);
    }

    // ------------------------------------------------------------------ capa y matriz de colisiones

    public static void CapaYMatriz()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var capa = tm.FindProperty("layers").GetArrayElementAtIndex(CapasNanoLab.BloqueoJugador);
        if (string.IsNullOrEmpty(capa.stringValue))
        {
            capa.stringValue = Nombre;
            tm.ApplyModifiedPropertiesWithoutUndo();
        }
        else if (capa.stringValue != Nombre)
            Debug.LogWarning($"[Colisiones] La capa {CapasNanoLab.BloqueoJugador} ya se llama '{capa.stringValue}': se usa igualmente para los bloqueos.");

        // No choca con nada (solo la encuentran las consultas del Locomotor). También se aplica al arrancar.
        CapasNanoLab.ConfigurarMatriz();
        var dm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
        var matriz = dm.FindProperty("m_LayerCollisionMatrix");
        if (matriz != null && matriz.isArray && matriz.arraySize == 32)
        {
            int b = CapasNanoLab.BloqueoJugador;
            for (int i = 0; i < 32; i++)
            {
                var fila = matriz.GetArrayElementAtIndex(i);
                fila.longValue = i == b ? 0 : (fila.longValue & ~(1L << b));
            }
            dm.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ escena

    public static void Aplicar(Scene s)
    {
        float suelo = ML.AlturaSuelo(ML.BoundsEscena());
        var todos = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();

        // 1) Bloqueos anteriores (los que vienen del prefab de la silla se quedan).
        int quitados = 0;
        foreach (var t in todos.Where(t => t != null && t.name == Nombre).ToArray())
        {
            if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject) && !PrefabUtility.IsAddedGameObjectOverride(t.gameObject)) continue;
            Object.DestroyImmediate(t.gameObject);
            quitados++;
        }
        todos = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();

        // 2) Muebles.
        var muebles = new List<GameObject>();
        foreach (var t in todos)
        {
            var go = t.gameObject;
            if (Ignorar(go)) continue;
            if (!EsMueble(go)) continue;
            if (muebles.Any(m => t.IsChildOf(m.transform))) continue;   // ya cubierto por su mueble
            muebles.RemoveAll(m => m.transform.IsChildOf(t));            // el de fuera cubre a los de dentro
            muebles.Add(go);
        }
        var informe = new List<string>();
        int nMuebles = 0;
        foreach (var m in muebles)
            if (Bloquear(m.transform, suelo, out var tam)) { nMuebles++; informe.Add($"{m.name} ({tam.x:F2}×{tam.y:F2}×{tam.z:F2})"); }

        // 3) Sillas: el bloqueo va en el prefab; las que no vengan de él lo reciben aquí.
        int nSillas = 0, sillasConPrefab = 0;
        foreach (var silla in todos.Select(t => t.GetComponent<Silla>()).Where(x => x != null))
        {
            if (silla.transform.Find(Nombre) != null) { sillasConPrefab++; continue; }
            BloqueoSilla(silla.gameObject);
            nSillas++;
        }

        // 4) Lo que queda apoyado en el suelo sin ningún collider sólido.
        int nSueltos = 0;
        var sueltos = new List<string>();
        foreach (var r in todos.Select(t => t.GetComponent<MeshRenderer>()).Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy))
        {
            var go = r.gameObject;
            if (Ignorar(go) || muebles.Any(m => go.transform.IsChildOf(m.transform))) continue;
            var b = r.bounds;
            if (b.min.y > suelo + 0.4f || b.size.y < 0.5f) continue;                // no está en el suelo o es bajo
            float huella = Mathf.Max(b.size.x, b.size.z);
            if (huella < 0.25f || huella > 4f) continue;                             // estrecho, o paredes/suelos
            var raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
            if (TieneColliderSolido(go) || (raiz != go && TieneColliderSolido(raiz))) continue;
            if (Bloquear(go.transform, suelo, out _)) { nSueltos++; sueltos.Add(Ruta(go.transform)); }
        }

        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(s);
        Debug.Log($"[Colisiones] {s.name}: {nMuebles} muebles, {nSillas + sillasConPrefab} sillas ({sillasConPrefab} con el bloqueo del prefab) y " +
                  $"{nSueltos} objetos sin collider bloquean ahora al jugador ({quitados} bloqueos anteriores sustituidos).\n" +
                  "Muebles: " + string.Join(", ", informe.Take(60)) +
                  (sueltos.Count > 0 ? "\nSin collider: " + string.Join(", ", sueltos.Take(40)) : ""));
    }

    static bool EsMueble(GameObject go)
    {
        string n = Limpio(go.name);
        if (PalabrasMueble.Any(n.Contains)) return true;
        if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) return false;
        var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
        var ruta = src != null ? AssetDatabase.GetAssetPath(src) : "";
        if (!ruta.StartsWith(Pack)) return false;
        string archivo = Path.GetFileNameWithoutExtension(ruta).ToLowerInvariant();
        return MueblesPack.Contains(archivo) || MueblesPack.Contains(n);
    }

    static bool Ignorar(GameObject go)
    {
        if (go.GetComponentInParent<OVRCameraRig>(true) != null) return true;     // jugador
        if (go.GetComponentInParent<Silla>(true) != null) return true;            // aparte (prefab)
        if (go.GetComponentInParent<Rigidbody>(true) != null) return true;        // agarrables
        if (go.GetComponentInParent<IAvatarJugador>(true) != null || go.GetComponentInParent<EspejoAvatar>(true) != null) return true;   // Baymax
        if (go.GetComponentInParent<BotonMenu>(true) != null || go.GetComponentInParent<VentanaIntro>(true) != null) return true;        // paneles
        if (go.layer >= 28) return true;                                           // capas de avatar y visor
        for (var t = go.transform; t != null; t = t.parent)
        {
            string n = Limpio(t.name);
            if (Excluir.Any(n.Contains)) return true;
        }
        return false;
    }

    static string Limpio(string s) => s.ToLowerInvariant().Replace("ñ", "n").Replace("ó", "o").Replace("í", "i").Replace("é", "e").Replace("á", "a");

    static bool TieneColliderSolido(GameObject go) =>
        go.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && !c.isTrigger && c.gameObject.layer != CapasNanoLab.BloqueoJugador);

    // Caja hija ajustada a las mallas del objeto (en su espacio local); si se apoya en el suelo, baja
    // hasta él (sin huecos bajo las patas) y sube como mucho hasta AltoMax.
    static bool Bloquear(Transform t, float suelo, out Vector3 tam)
    {
        tam = Vector3.zero;
        if (Vector3.Dot(t.up, Vector3.up) < 0.95f && Mathf.Abs(Vector3.Dot(t.up, Vector3.up)) > 0.05f)
        {
            // Inclinado: la caja local no se alinea con el suelo; se usa la caja de mundo.
            return BloquearMundo(t, suelo, out tam);
        }
        if (!LimitesLocales(t, t, out var b, out var mundo)) return false;
        if (Mathf.Max(mundo.size.x, mundo.size.z) < HuellaMin) return false;
        if (mundo.min.y > suelo + AltoMax) return false;
        float abajo = mundo.min.y < suelo + 0.5f ? suelo + 0.02f : mundo.min.y;
        float arriba = Mathf.Min(mundo.max.y, suelo + AltoMax);
        if (arriba - abajo < 0.1f) return false;
        // Altura en mundo -> local (el objeto está derecho o volcado 90°/180°: se usa el eje que apunta arriba).
        var c = b.center; var e = b.extents;
        var pAbajo = t.InverseTransformPoint(new Vector3(0f, abajo, 0f) + Proyectar(t.TransformPoint(c)));
        var pArriba = t.InverseTransformPoint(new Vector3(0f, arriba, 0f) + Proyectar(t.TransformPoint(c)));
        var min = Vector3.Min(Vector3.Min(b.min, pAbajo), pArriba);
        var max = Vector3.Max(Vector3.Max(b.max, pAbajo), pArriba);
        // El eje local que es vertical se recorta exactamente a [abajo, arriba].
        int eje = EjeVertical(t);
        min[eje] = Mathf.Min(pAbajo[eje], pArriba[eje]);
        max[eje] = Mathf.Max(pAbajo[eje], pArriba[eje]);
        var col = Crear(t);
        col.center = (min + max) / 2f;
        col.size = max - min;
        tam = Vector3.Scale(col.size, Abs(t.lossyScale));
        return true;
    }

    static bool BloquearMundo(Transform t, float suelo, out Vector3 tam)
    {
        tam = Vector3.zero;
        var rs = t.GetComponentsInChildren<MeshRenderer>(true);
        if (rs.Length == 0) return false;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        if (Mathf.Max(b.size.x, b.size.z) < HuellaMin || b.min.y > suelo + AltoMax) return false;
        float abajo = b.min.y < suelo + 0.5f ? suelo + 0.02f : b.min.y;
        float arriba = Mathf.Min(b.max.y, suelo + AltoMax);
        var go = new GameObject(Nombre) { layer = CapasNanoLab.BloqueoJugador };
        go.transform.SetParent(t, false);
        go.transform.SetPositionAndRotation(new Vector3(b.center.x, (abajo + arriba) / 2f, b.center.z), Quaternion.identity);
        var s = t.lossyScale;
        var col = go.AddComponent<BoxCollider>();
        tam = new Vector3(b.size.x, arriba - abajo, b.size.z);
        col.size = new Vector3(tam.x / Mathf.Abs(s.x), tam.y / Mathf.Abs(s.y), tam.z / Mathf.Abs(s.z));
        GameObjectUtility.SetStaticEditorFlags(go, 0);
        return true;
    }

    static Vector3 Proyectar(Vector3 p) => new Vector3(p.x, 0f, p.z);
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    static int EjeVertical(Transform t)
    {
        var up = new[] { Vector3.Dot(t.right, Vector3.up), Vector3.Dot(t.up, Vector3.up), Vector3.Dot(t.forward, Vector3.up) };
        int eje = 0;
        for (int i = 1; i < 3; i++) if (Mathf.Abs(up[i]) > Mathf.Abs(up[eje])) eje = i;
        return eje;
    }

    static BoxCollider Crear(Transform padre)
    {
        var go = new GameObject(Nombre) { layer = CapasNanoLab.BloqueoJugador };
        go.transform.SetParent(padre, false);
        GameObjectUtility.SetStaticEditorFlags(go, 0);   // sin GI ni batching: solo física
        return go.AddComponent<BoxCollider>();
    }

    // Límites de las mallas de 'raiz' en el espacio local de 'espacio' (y en mundo).
    static bool LimitesLocales(Transform raiz, Transform espacio, out Bounds local, out Bounds mundo)
    {
        local = mundo = default;
        bool hay = false;
        foreach (var mf in raiz.GetComponentsInChildren<MeshFilter>(true))
        {
            var r = mf.GetComponent<MeshRenderer>();
            if (mf.sharedMesh == null || r == null || !r.enabled) continue;
            if (Excluir.Any(Limpio(mf.name).Contains)) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var esquina = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var w = mf.transform.TransformPoint(esquina);
                var l = espacio.InverseTransformPoint(w);
                if (!hay) { local = new Bounds(l, Vector3.zero); mundo = new Bounds(w, Vector3.zero); hay = true; }
                else { local.Encapsulate(l); mundo.Encapsulate(w); }
            }
        }
        return hay;
    }

    // ------------------------------------------------------------------ sillas

    // Caja sólida en la capa de bloqueo: el jugador ya no atraviesa la silla. Silla.Colisiones la
    // desactiva mientras está sentado en ella (y JugadorNanoLab la reactiva al levantarse).
    public static void BloqueoSilla(GameObject raiz)
    {
        var vieja = raiz.transform.Find(Nombre);
        if (vieja != null) Object.DestroyImmediate(vieja.gameObject);
        if (!LimitesLocales(raiz.transform, raiz.transform, out var b, out _)) return;
        float ancho = Mathf.Min(b.size.x, SillaHuella), fondo = Mathf.Min(b.size.z, SillaHuella);
        var col = Crear(raiz.transform);
        float abajo = Mathf.Max(b.min.y, 0.02f);
        col.center = new Vector3(b.center.x, (abajo + b.max.y) / 2f, b.center.z);
        col.size = new Vector3(ancho, b.max.y - abajo, fondo);
    }

    // Añade el bloqueo a los prefabs de silla (las instancias de las escenas lo heredan).
    public static void SillasPrefab()
    {
        foreach (var ruta in new[] { MontarSillaLaboratorio.PrefabRuta, "Assets/NanoLab/Prefabs/Silla.prefab" })
        {
            if (!File.Exists(ruta)) continue;
            var raiz = PrefabUtility.LoadPrefabContents(ruta);
            try
            {
                if (raiz.GetComponent<Silla>() == null) continue;
                BloqueoSilla(raiz);
                PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
                Debug.Log($"[Colisiones] Bloqueo sólido añadido al prefab {ruta}.");
            }
            finally { PrefabUtility.UnloadPrefabContents(raiz); }
        }
    }

    static string Ruta(Transform t)
    {
        var partes = new List<string>();
        for (int i = 0; t != null && i < 3; t = t.parent, i++) partes.Insert(0, t.name);
        return string.Join("/", partes);
    }
}
