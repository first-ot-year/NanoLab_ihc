using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MM = MontarMicrobiologia;

// Sillas en las que el jugador se puede sentar (RNF-07). Se usa la silla de laboratorio modelada en
// Blender (MontarSillaLaboratorio, prefab en Assets/NanoLab/Silla_Laboratorio_Interactuable/Prefabs);
// si faltara su FBX, el taburete de primitivas Assets/NanoLab/Prefabs/Silla.prefab. Las sillas del
// paquete de Animatics (chair.prefab) eran solo decorativas: en Microbiología se quitan y se pone una
// frente a cada microscopio de puesto; en Química se cambian por la propia en el mismo sitio, mirando
// hacia la mesa más cercana. Las sillas del prefab que no se usa también se cambian.
public static class MontarSillas
{
    public static string PrefabRuta => File.Exists(MontarSillaLaboratorio.Fbx) ? MontarSillaLaboratorio.PrefabRuta : PrefabTaburete;
    const string PrefabTaburete = "Assets/NanoLab/Prefabs/Silla.prefab";
    static readonly string[] Originales =
    {
        "Assets/3D Laboratory Environment with Appratus/Prefabs/chair.prefab",
        "Assets/3D Laboratory Environment with Appratus/Models/chair.fbx",
    };
    static string[] Reemplazables => Originales.Concat(new[] { PrefabTaburete, MontarSillaLaboratorio.PrefabRuta }.Where(r => r != PrefabRuta)).ToArray();
    const string MatDir = "Assets/NanoLab/Materiales/Micro";
    const string Quimica = "Assets/NanoLab/Scenes/Laboratorio.unity";
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";

    // Sin volver a hornear: las sillas nuevas se iluminan con las sondas de luz.
    [MenuItem("NanoLab/Reemplazar sillas (las dos salas)", false, 27)]
    public static void AmbasSalas()
    {
        AsegurarColisiones.CapaYMatriz();
        foreach (var ruta in new[] { Quimica, Microbiologia })
        {
            if (!File.Exists(ruta)) continue;
            var s = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Single);
            if (ruta == Microbiologia)
            {
                QuitarOriginales(s);
                foreach (var vieja in Object.FindObjectsOfType<Silla>(true)) Object.DestroyImmediate(vieja.gameObject);
                EnPuestos(s, MontarLaboratorio.AlturaSuelo(MontarLaboratorio.BoundsEscena()));
            }
            else Aplicar(s);
            AsegurarColisiones.Aplicar(s);
            AgruparLab.Agrupar(s);
            EditorSceneManager.SaveScene(s);
        }
    }

    // Química sin rehacerla: sillas nuevas y el panel ☰ del rig.
    public static void ActualizarQuimica()
    {
        if (!File.Exists(Quimica)) return;
        var s = EditorSceneManager.OpenScene(Quimica, OpenSceneMode.Single);
        Aplicar(s);
        ConfigurarMetaISDK.Configurar(s);
        AsegurarColisiones.Aplicar(s);
        AgruparLab.Agrupar(s);
        EditorSceneManager.SaveScene(s);
    }

    // Microbiología: las sillas del paquete estaban repartidas por el suelo (algunas en el pasillo de la
    // entrada). Se quitan y se pone una silla frente a cada microscopio de puesto, mirándolo, con el
    // asiento a 0.3 m del borde de la mesa: sentado (taburete), los ojos quedan a la altura del ocular.
    public static int QuitarOriginales(Scene s)
    {
        var viejas = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject).Where(EsSillaOriginal).ToArray();
        foreach (var v in viejas) if (v != null) Object.DestroyImmediate(v);
        Debug.Log($"[Sillas] {s.name}: {viejas.Length} sillas decorativas del paquete quitadas.");
        return viejas.Length;
    }

    public const float AsientoDesdeBorde = 0.3f;

    public static int EnPuestos(Scene s, float suelo)
    {
        var prefab = Prefab();
        var micros = Object.FindObjectsOfType<MicroscopioOptico>(true)
            .Where(m => m.gameObject.scene == s && !m.modoReto && m.platina != null && m.platina.anclaje != null
                        && m.GetComponentsInParent<Transform>(true).Any(p => p.name.StartsWith("Puesto_")))
            .OrderBy(m => m.GetComponentsInParent<Transform>(true).First(p => p.name.StartsWith("Puesto_")).name)
            .ToArray();
        Transform grupo = null;
        int n = 0;
        foreach (var m in micros)
        {
            var puesto = m.GetComponentsInParent<Transform>(true).First(p => p.name.StartsWith("Puesto_"));
            if (grupo == null)
            {
                grupo = new GameObject("Sillas").transform;
                grupo.SetParent(puesto.parent != null ? puesto.parent.parent : null, false);
            }
            var haciaUsuario = Vector3.ProjectOnPlane(m.transform.forward, Vector3.up).normalized;
            var borde = m.platina.anclaje.position + haciaUsuario * AcercarMicroscopios.DistanciaBorde;
            var pos = new Vector3(borde.x, suelo, borde.z) + haciaUsuario * AsientoDesdeBorde;
            var silla = (GameObject)PrefabUtility.InstantiatePrefab(prefab, s);
            silla.name = "Silla_" + puesto.name;
            silla.transform.SetParent(grupo, false);
            silla.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-haciaUsuario, Vector3.up));
            foreach (var r in silla.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.receiveGI = ReceiveGI.LightProbes;
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
            n++;
        }
        Debug.Log($"[Sillas] {s.name}: {n} sillas frente a los microscopios de los puestos.");
        return n;
    }

    // Química: cada silla del paquete se cambia por una propia en el mismo sitio, mirando a la mesa.
    public static int Aplicar(Scene s)
    {
        var prefab = Prefab();
        Physics.SyncTransforms();
        var viejas = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => t.gameObject)
            .Where(EsSillaOriginal)
            .ToArray();
        int n = 0;
        Transform padre = null;
        foreach (var vieja in viejas)
        {
            if (vieja == null) continue;
            var rs = vieja.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) { Object.DestroyImmediate(vieja); continue; }
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var pos = new Vector3(b.center.x, b.min.y, b.center.z);
            var adelante = HaciaLaMesa(pos, vieja);
            if (padre == null) padre = vieja.transform.parent;
            Object.DestroyImmediate(vieja);

            var nueva = (GameObject)PrefabUtility.InstantiatePrefab(prefab, s);
            nueva.name = "Silla_" + (++n);
            if (padre != null) nueva.transform.SetParent(padre, true);
            nueva.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(adelante, Vector3.up));
            foreach (var r in nueva.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.receiveGI = ReceiveGI.LightProbes;   // si la sala no se vuelve a hornear
                r.lightProbeUsage = LightProbeUsage.BlendProbes;
            }
        }
        // Las sillas ya propias se conservan; se cuentan todas.
        int total = Object.FindObjectsOfType<Silla>(true).Length;
        Debug.Log($"[Sillas] {s.name}: {n} sillas del paquete reemplazadas; {total} sillas en las que sentarse.");
        return n;
    }

    static bool EsSillaOriginal(GameObject go)
    {
        if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go)) return false;
        var src = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
        if (src == null) return false;
        var ruta = AssetDatabase.GetAssetPath(src);
        return Reemplazables.Contains(ruta);
    }

    // La silla mira hacia el obstáculo más cercano a la altura de una mesa (la mesa en la que está).
    static Vector3 HaciaLaMesa(Vector3 pos, GameObject ignorar)
    {
        var f = Vector3.ProjectOnPlane(ignorar.transform.forward, Vector3.up).normalized;
        if (f.sqrMagnitude < 0.5f) f = Vector3.forward;
        var dirs = new[] { f, -f, Vector3.Cross(Vector3.up, f), -Vector3.Cross(Vector3.up, f) };
        float mejor = 1.6f;
        var res = f;
        foreach (var d in dirs)
            foreach (var alto in new[] { 0.6f, 0.8f })
            {
                var hits = Physics.RaycastAll(pos + Vector3.up * alto, d, 1.6f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var h in hits)
                {
                    if (h.collider.transform.IsChildOf(ignorar.transform) || h.collider.GetComponentInParent<Silla>() != null) continue;
                    if (h.distance < mejor) { mejor = h.distance; res = d; }
                }
            }
        return res;
    }

    public static GameObject Prefab()
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRuta);
        if (p != null) return p;
        if (PrefabRuta == MontarSillaLaboratorio.PrefabRuta && (p = MontarSillaLaboratorio.CrearPrefab()) != null) return p;
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabTaburete));
        var tela = Material("SillaAsiento", new Color(0.18f, 0.32f, 0.45f), 0f, 0.35f);
        var metal = Material("SillaMetal", new Color(0.62f, 0.65f, 0.7f), 0.85f, 0.6f);

        var raiz = new GameObject("Silla");
        var silla = raiz.AddComponent<Silla>();
        var modelo = new GameObject("Modelo").transform;
        modelo.SetParent(raiz.transform, false);
        // Taburete de laboratorio: las mesas son bancos de 0.9 m, así que el asiento va a 0.65 m y,
        // sentado, los ojos quedan a la altura del ocular (~1.4 m). Mira hacia +Z (la mesa): el
        // respaldo bajo queda detrás. Piezas sin collider.
        MM.Caja(modelo, "Asiento", new Vector3(0f, 0.625f, 0f), new Vector3(0.4f, 0.05f, 0.38f), tela, false);
        MM.Caja(modelo, "Respaldo", new Vector3(0f, 0.86f, -0.18f), new Vector3(0.38f, 0.2f, 0.04f), tela, false);
        foreach (var x in new[] { -0.15f, 0.15f })
            MM.Caja(modelo, "Soporte", new Vector3(x, 0.73f, -0.18f), new Vector3(0.02f, 0.16f, 0.02f), metal, false);
        foreach (var x in new[] { -0.17f, 0.17f })
            foreach (var z in new[] { -0.16f, 0.16f })
                MM.Caja(modelo, "Pata", new Vector3(x, 0.3f, z), new Vector3(0.03f, 0.6f, 0.03f), metal, false);
        // Reposapiés.
        foreach (var z in new[] { -0.16f, 0.16f })
            MM.Caja(modelo, "Reposapies", new Vector3(0f, 0.24f, z), new Vector3(0.34f, 0.02f, 0.02f), metal, false);
        foreach (var x in new[] { -0.17f, 0.17f })
            MM.Caja(modelo, "Reposapies", new Vector3(x, 0.24f, 0f), new Vector3(0.02f, 0.02f, 0.3f), metal, false);

        silla.asiento = new GameObject("Asiento").transform;
        silla.asiento.SetParent(raiz.transform, false);
        silla.asiento.localPosition = new Vector3(0f, 0.65f, 0f);
        silla.salida = new GameObject("Salida").transform;
        silla.salida.SetParent(raiz.transform, false);
        silla.salida.localPosition = new Vector3(0f, 0f, -0.6f);

        // Collider trigger para el rayo (sentarse) y bloqueo sólido para que el jugador no la atraviese
        // (Silla.Colisiones lo desactiva mientras está sentado en ella).
        var t = raiz.AddComponent<BoxCollider>();
        t.isTrigger = true;
        t.center = new Vector3(0f, 0.55f, 0f);
        t.size = new Vector3(0.46f, 1.1f, 0.44f);
        AsegurarColisiones.BloqueoSilla(raiz);

        p = PrefabUtility.SaveAsPrefabAsset(raiz, PrefabTaburete);
        Object.DestroyImmediate(raiz);
        Debug.Log("[Sillas] Prefab creado en " + PrefabTaburete);
        return p;
    }

    static Material Material(string nombre, Color c, float metal, float liso)
    {
        var m = MM.MatEn(MatDir, nombre, Shader.Find("Standard"));
        m.color = c;
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Glossiness", liso);
        EditorUtility.SetDirty(m);
        return m;
    }
}
