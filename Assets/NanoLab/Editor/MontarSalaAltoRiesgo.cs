using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MM = MontarMicrobiologia;
using MC = MontarCentroExperimentacion;
using ML = MontarLaboratorio;

// Sala de alto riesgo (BSL-3): habitación aparte, toda de vidrio, detrás de la pared norte de
// Microbiologia.unity. Sustituye a la antigua zona acristalada de la esquina noroeste (≈ 17 m²).
//
// Planta (x hacia la derecha, z hacia arriba). La pared norte de la sala principal se sustituye por
// vidrio entre X0 y X1; alrededor del cubo de vidrio queda un hueco técnico y una envolvente opaca
// (wall01 del pack) para que a través del vidrio no se vea el cielo.
//
//   z ≈ 26 ┌──────────────── envolvente ────────────────┐
//          │ ┌────────── vidrio · MESA CRIOTUBOS ──────┐ │
//          │ │ incubadora                     cabina   │ │
//          │ │ autoclave      (estación 3D)    BSC     │ │
//          │ │                   consola       ultra-  │ │
//          │ │                                congelad.│ │
//   z ≈ 16,5 ══ puerta ═══════════ vidrio ═════════════╧═╧═══  pared norte de la sala principal
//            [cabina de desinfección]   (sala de microbiología)
//          x -10,4                                   x -2,7
public static class MontarSalaAltoRiesgo
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Pack = "Assets/3D Laboratory Environment with Appratus/Prefabs/";
    public const string NombreRaiz = "SalaAltoRiesgo";

    // Interior del cubo de vidrio (en z empieza en el centro de la pared norte y mide Fondo).
    public const float X0 = -10.4f, X1 = -2.7f, Fondo = 9.0f;
    const float Hueco = 0.55f;                       // entre el vidrio y la envolvente
    // Puerta de 1,6 m (el jugador, radio 0,25 m, pasa con holgura).
    const float PuertaX0 = -10.0f, PuertaX1 = -8.4f, AltoPuerta = 2.25f;
    // Valores de la pared norte si no se encuentra en la escena (vista cenital).
    const float ParedZ0Defecto = 16.4f, ParedZ1Defecto = 16.65f;

    static Dictionary<string, Material> M => MM.mats;
    static float suelo => MM.suelo;
    static float paredZ0, paredZ1;   // caras sur (sala principal) y norte de la pared
    static float ZV => (paredZ0 + paredZ1) / 2f;   // plano del vidrio sur
    static float ZF => paredZ1 + Fondo;            // vidrio norte

    // ================================================================== entrada

    [MenuItem("NanoLab/Reconstruir solo la sala de alto riesgo", false, 3)]
    public static void Reconstruir()
    {
        ML.GenerarUVsDeLightmap();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var muestras = CrearMuestras.Todas();
        AssetDatabase.SaveAssets();
        MM.CrearMateriales();
        var slidePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NanoLab/Prefabs/Portaobjetos.prefab");
        var sondas = Object.FindObjectsOfType<LightProbeGroup>(true).FirstOrDefault(l => l.name == "SondasDeLuz");
        if (sondas != null) Object.DestroyImmediate(sondas.gameObject);

        var padre = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Microbiologia" && t.parent != null && t.parent.name.StartsWith("LAB_"));
        if (padre == null) padre = new GameObject("Microbiologia").transform;
        Construir(padre, muestras.Where(m => m.tipo == TipoMuestra.Virus).ToArray(), slidePrefab);
        ConfigurarMetaISDK.ActualizarPrefabPortaobjetos();
        ConfigurarMetaISDK.Configurar(scene);
        ML.ConfigurarLuces("Microbiologia_Lighting");
        MC.LucesTiempoReal();
        ML.CrearSondas(ML.BoundsEscena(), suelo);
        AsegurarColisiones.Aplicar(scene);
        AgruparLab.Agrupar(scene);
        EditorSceneManager.SaveScene(scene);
        if (!Application.isBatchMode || !System.Environment.GetCommandLineArgs().Contains("-sinBake"))
        {
            Debug.Log("[BSL] Horneando iluminación...");
            Debug.Log(Lightmapping.Bake() ? "[BSL] Bake completado." : "[BSL] El bake falló.");
            EditorSceneManager.SaveScene(scene);
        }
        MM.Capturas();
    }

    public static void Construir(Transform padre, MuestraData[] virus, GameObject slidePrefab)
    {
        MM.suelo = ML.AlturaSuelo(ML.BoundsEscena());

        // Antes de borrar nada: medir la pared norte (incluye los tramos de una reconstrucción anterior).
        var pared = MedirParedNorte(out var piezas);
        foreach (var go in piezas) if (go != null) Object.DestroyImmediate(go);
        foreach (var t in Object.FindObjectsOfType<Transform>(true)
                     .Where(t => t != null && (t.name == NombreRaiz || t.name == "ZonaAltoRiesgo" || t.name == "CabinaDesinfeccion")).ToArray())
            if (t != null) Object.DestroyImmediate(t.gameObject);
        QuitarColgadosEnPared(padre);

        var raiz = MM.Grupo(padre, NombreRaiz);
        ReconstruirParedNorte(MM.Grupo(raiz, "ParedNorte"), pared);
        Envolvente(MM.Grupo(raiz, "Envolvente"));
        CuboDeVidrio(MM.Grupo(raiz, "Vidrio"));
        Physics.SyncTransforms();

        var dentro = MM.Grupo(raiz, "Interior");
        MM.CrearEstacion3D(dentro, new Vector3(-6.6f, 0f, paredZ1 + 4.9f), new Vector3(-6.6f, 0f, paredZ1 + 3.9f));
        Criotubos(dentro, virus, slidePrefab);
        Equipamiento(MM.Grupo(raiz, "Equipamiento"));
        CabinaDesinfeccion(MM.Grupo(raiz, "CabinaDesinfeccion"));
        Senales(MM.Grupo(raiz, "Senales"));
        Luces(MM.Grupo(raiz, "Luces"));
        Debug.Log($"[BSL] Sala de alto riesgo: vidrio x {X0}..{X1}, z {ZV:F2}..{ZF:F2} ({(X1 - X0) * (ZF - ZV):F0} m²).");
    }

    // ================================================================== pared norte

    // Tramos de wall01 de la pared norte (a lo largo de x) que caen en el ancho de la sala.
    static Bounds MedirParedNorte(out List<GameObject> piezas)
    {
        piezas = new List<GameObject>();
        Bounds? total = null;
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>(true))
        {
            if (r == null) continue;
            var go = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject) ?? r.gameObject;
            if (!go.name.ToLowerInvariant().StartsWith("wall01")) continue;
            var b = r.bounds;
            if (b.size.x <= b.size.z || b.center.z < 15.5f || b.center.z > 18f) continue;
            if (b.max.x <= X0 - 1.5f || b.min.x > X1 + 0.01f) continue;
            if (!piezas.Contains(go)) piezas.Add(go);
            if (total == null) total = b; else { var t = total.Value; t.Encapsulate(b); total = t; }
        }
        if (total == null)
        {
            Debug.LogWarning("[BSL] No se encontró la pared norte (wall01): se usan las medidas por defecto.");
            paredZ0 = ParedZ0Defecto; paredZ1 = ParedZ1Defecto;
            return new Bounds(new Vector3((X0 - Hueco - MC.Grosor + X1) / 2f, MC.AltoSala / 2f, ZV), new Vector3(X1 - X0 + Hueco + MC.Grosor, MC.AltoSala, paredZ1 - paredZ0));
        }
        paredZ0 = total.Value.min.z; paredZ1 = total.Value.max.z;
        Debug.Log($"[BSL] Pared norte: {piezas.Count} tramo(s), x {total.Value.min.x:F2}..{total.Value.max.x:F2}, z {paredZ0:F2}..{paredZ1:F2}");
        return total.Value;
    }

    // Estanterías y armarios colgados en el tramo que pasa a ser de vidrio (quedarían flotando).
    static void QuitarColgadosEnPared(Transform padre)
    {
        string[] fijos = { "wall", "roof", "floor", "pillar", "light" };
        var quitar = new HashSet<GameObject>();
        foreach (var r in Object.FindObjectsOfType<MeshRenderer>(true))
        {
            if (r == null || r.transform.IsChildOf(padre)) continue;
            var go = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
            if (go == null) continue;
            string n = go.name.ToLowerInvariant();
            if (fijos.Any(n.Contains)) continue;
            var b = r.bounds;
            if (b.max.z < paredZ0 - 0.7f || b.min.z > paredZ1 || b.max.x < X0 || b.min.x > X1 || b.min.y < suelo + 1.0f) continue;
            quitar.Add(go);
        }
        foreach (var go in quitar)
        {
            Debug.Log($"[BSL] Quitado de la pared norte: {go.name} ({ML.BoundsDe(go).min:F2}..{ML.BoundsDe(go).max:F2})");
            Object.DestroyImmediate(go);
        }
    }

    // La pared norte queda opaca fuera del ancho de la sala.
    static void ReconstruirParedNorte(Transform p, Bounds pared)
    {
        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "wall01.prefab");
        if (pared.min.x < X0 - 0.05f) MC.Muro(p, wall, V(pared.min.x, 0f, paredZ0), V(X0, 0f, paredZ1), true);
        if (pared.max.x > X1 + 0.05f) MC.Muro(p, wall, V(X1, 0f, paredZ0), V(pared.max.x, 0f, paredZ1), true);
    }

    // ================================================================== sala

    static float SX0 => X0 - Hueco - MC.Grosor;
    static float SX1 => X1 + Hueco + MC.Grosor;
    static float SZ1 => ZF + Hueco + MC.Grosor;

    // Suelo, techo y paredes opacas alrededor del cubo de vidrio.
    static void Envolvente(Transform s)
    {
        var floor = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "floor.prefab");
        MC.Pieza(s, floor, new Bounds(V((SX0 + SX1) / 2f, suelo - 0.07f, (paredZ0 + SZ1) / 2f), V(SX1 - SX0, 0.14f, SZ1 - paredZ0)), 0f);

        var wall = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "wall01.prefab");
        MC.Muro(s, wall, V(SX0, 0f, SZ1 - MC.Grosor), V(SX1, 0f, SZ1), true);           // norte
        MC.Muro(s, wall, V(SX0, 0f, paredZ1), V(SX0 + MC.Grosor, 0f, SZ1), false);      // oeste
        MC.Muro(s, wall, V(SX1 - MC.Grosor, 0f, paredZ1), V(SX1, 0f, SZ1), false);      // este

        var roof = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "roof.prefab");
        var techo = MM.Grupo(s, "Techo");
        int nx = 3, nz = 3;
        float w = (SX1 - SX0) / nx, d = (SZ1 - paredZ1) / nz;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
                MC.Pieza(techo, roof, new Bounds(V(SX0 + (i + 0.5f) * w, MC.AltoSala + 0.03f, paredZ1 + (j + 0.5f) * d), V(w, 0.06f, d)), 0f, colliders: false);

        // Franjas rosas en la envolvente: se ven a través del vidrio y marcan el área de riesgo.
        foreach (float y in new[] { suelo + 0.12f, MC.AltoSala - 0.25f })
        {
            MM.Caja(s, "FranjaRosa", V((SX0 + SX1) / 2f, y, SZ1 - MC.Grosor - 0.02f), V(SX1 - SX0 - 2f * MC.Grosor, 0.04f, 0.02f), M["EmisivoRosa"], false);
            MM.Caja(s, "FranjaRosa", V(SX0 + MC.Grosor + 0.02f, y, (paredZ1 + SZ1) / 2f), V(0.02f, 0.04f, SZ1 - paredZ1), M["EmisivoRosa"], false);
            MM.Caja(s, "FranjaRosa", V(SX1 - MC.Grosor - 0.02f, y, (paredZ1 + SZ1) / 2f), V(0.02f, 0.04f, SZ1 - paredZ1), M["EmisivoRosa"], false);
        }

        // Rótulo grande en el fondo, legible desde la sala principal a través del vidrio.
        var t = MM.Texto3D(s, "LABORATORIO DE ALTO RIESGO · BSL-3", V((X0 + X1) / 2f, MC.AltoSala - 0.7f, SZ1 - MC.Grosor - 0.03f), Quaternion.identity, 3.2f, new Color(1f, 0.55f, 0.65f), X1 - X0);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 200);

        var seg = new GameObject("SueloSeguridadBSL");
        seg.transform.SetParent(s, false);
        seg.transform.position = V((SX0 + SX1) / 2f, suelo - 0.1f, (paredZ0 + SZ1) / 2f);
        seg.AddComponent<BoxCollider>().size = V(SX1 - SX0 + 2f, 0.2f, SZ1 - paredZ0 + 2f);
    }

    // Las cuatro paredes de vidrio (la sur, con la puerta, ocupa el hueco de la pared norte).
    static void CuboDeVidrio(Transform g)
    {
        float alto = MC.AltoSala - suelo - 0.01f;
        MM.Pared(g, V(X0, 0f, ZV), V(PuertaX0, 0f, ZV), alto);
        MM.Pared(g, V(PuertaX1, 0f, ZV), V(X1, 0f, ZV), alto);
        MM.Pared(g, V(X0, 0f, ZV), V(X0, 0f, ZF), alto);
        MM.Pared(g, V(X0, 0f, ZF), V(X1, 0f, ZF), alto);
        MM.Pared(g, V(X1, 0f, ZF), V(X1, 0f, ZV), alto);
        float yd = suelo + AltoPuerta;
        MM.Caja(g, "Dintel", V((PuertaX0 + PuertaX1) / 2f, (yd + suelo + alto) / 2f, ZV), V(PuertaX1 - PuertaX0, suelo + alto - yd, 0.06f), M["Marco"], true);
        MM.Caja(g, "Umbral", V((PuertaX0 + PuertaX1) / 2f, suelo + 0.004f, ZV), V(PuertaX1 - PuertaX0, 0.008f, paredZ1 - paredZ0 + 0.1f), M["Cromo"], false);
        // Marco luminoso de la puerta.
        foreach (float x in new[] { PuertaX0 + 0.04f, PuertaX1 - 0.04f })
            MM.Caja(g, "MarcoPuerta", V(x, suelo + AltoPuerta / 2f, ZV), V(0.03f, AltoPuerta, 0.08f), M["EmisivoRosa"], false);
        MM.Caja(g, "MarcoPuerta", V((PuertaX0 + PuertaX1) / 2f, yd - 0.02f, ZV), V(PuertaX1 - PuertaX0, 0.03f, 0.08f), M["EmisivoRosa"], false);
    }

    // ================================================================== contenido

    // Mesa del pack contra el vidrio norte con el rack de criotubos de virus.
    static void Criotubos(Transform padre, MuestraData[] virus, GameObject slidePrefab)
    {
        const float cx = -8.6f;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "table with drawers.prefab");
        var cont = new GameObject("MesaCriotubos").transform;
        cont.SetParent(padre, false);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cont);
        var b = ML.BoundsDe(inst);
        if (b.size.z > b.size.x) inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        b = ML.BoundsDe(inst);
        cont.position += V(cx - b.center.x, suelo - b.min.y, ZF - 0.08f - b.max.z);
        MC.Fijo(cont.gameObject, true);
        b = ML.BoundsDe(inst);
        Physics.SyncTransforms();

        float zRack = b.min.z + 0.2f;   // cerca del borde, al alcance del brazo
        float top = MM.AlturaEn(cx, zRack, b.max.y + 0.3f);
        Debug.Log($"[BSL] Mesa de criotubos: {b.size.x:F2} × {b.size.z:F2} m, altura {top - suelo:F2} m");
        var rack = new GameObject("RackCriotubos").transform;
        rack.SetParent(padre, false);
        rack.position = V(cx, top, zRack);
        rack.rotation = Quaternion.Euler(0f, 180f, 0f);
        Utileria.Gradilla(rack);
        for (int i = 0; i < virus.Length; i++)
        {
            var v = virus[i];
            var tubo = new GameObject("Criotubo_" + v.name).transform;
            tubo.SetParent(rack, false);
            tubo.localPosition = V(-0.24f + i * 0.12f, 0.026f, 0f);   // metido 1,4 cm en la gradilla
            var brillo = Utileria.Criotubo(tubo, v);
            var bc = tubo.gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = V(0f, 0.06f, 0f); bc.size = V(0.07f, 0.12f, 0.07f);
            tubo.gameObject.AddComponent<FuenteMuestra>().muestra = v;
            tubo.gameObject.AddComponent<Resaltado>().brillos = new[] { brillo };
            MM.Etiqueta(tubo, v.nombre, v.riesgo, V(0f, 0.22f, 0f), v.color);
        }
        Utileria.Bandeja(rack, V(0.45f, 0f, 0f), new[] { -0.035f, 0f, 0.035f });
        for (int i = 0; i < 3; i++)
        {
            var s = (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab, rack);
            s.transform.localPosition = V(0.45f, 0.015f, -0.035f + i * 0.035f);
        }
        MM.CrearDesinfectante(rack, V(-0.45f, 0f, 0f));
        var titulo = new GameObject("TituloVirus").transform;
        titulo.SetParent(rack, false);
        titulo.localPosition = V(0f, 0.5f, 0f);
        titulo.gameObject.AddComponent<Cartel>();
        var t = MM.Texto3D(titulo, "MUESTRAS VIRALES · BSL-3", Vector3.zero, Quaternion.identity, 0.45f, new Color(1f, 0.55f, 0.65f), 1.6f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.2f; t.outlineColor = new Color32(0, 0, 0, 200);
    }

    // Equipos de un laboratorio BSL-3 (ambientación, no interactivos), pegados al vidrio.
    static void Equipamiento(Transform g)
    {
        MC.Equipo(g, "Autoclave", V(X0 + 0.42f, 0f, paredZ1 + 4.6f), 90f, V(0.8f, 1.05f, 0.7f), "AUTOCLAVE", "121 °C · LISTO", true);
        MC.Equipo(g, "Incubadora", V(X0 + 0.42f, 0f, paredZ1 + 5.7f), 90f, V(0.8f, 1.6f, 0.7f), "INCUBADORA", "37.0 °C", false);
        MC.Equipo(g, "Ultracongelador", V(X1 - 0.42f, 0f, paredZ1 + 2.0f), -90f, V(0.8f, 1.9f, 0.7f), "ULTRACONGELADOR", "-80 °C", false);
        MC.CabinaBioseguridad(g, V(X1 - 0.45f, 0f, paredZ1 + 6.2f), -90f);
    }

    // ================================================================== cámara de descontaminación

    // Esclusa delante de la puerta (CamaraDescontaminacion): dos puertas correderas herméticas
    // (exterior hacia la sala principal, interior en el vidrio del BSL-3), boquillas de niebla, luz
    // UV-C, pantallas de estado y semáforos. Fondo desde la cara sur de la pared hacia la sala.
    const float FondoCamara = 1.3f;
    const float AltoCamara = 2.3f;

    [MenuItem("NanoLab/Montar cámara de descontaminación (BSL-3)", false, 4)]
    public static void MontarCamara()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MM.CrearMateriales();
        MM.suelo = ML.AlturaSuelo(ML.BoundsEscena());
        var raiz = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == NombreRaiz);
        if (raiz == null) { Debug.LogError("[BSL] No está la sala de alto riesgo: ejecuta 'NanoLab/Reconstruir solo la sala de alto riesgo'."); return; }
        MedirPuerta(raiz);
        var vieja = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "CabinaDesinfeccion");
        var padre = vieja != null ? vieja.parent : raiz;
        if (vieja != null) Object.DestroyImmediate(vieja.gameObject);
        CabinaDesinfeccion(MM.Grupo(padre, "CabinaDesinfeccion"));
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[BSL] Cámara de descontaminación montada delante de la puerta (vidrio en z {ZV:F2}, pared {paredZ0:F2}..{paredZ1:F2}).");
    }

    // Plano del vidrio y grosor de la pared a partir del umbral de la puerta (sin volver a medir la pared).
    static void MedirPuerta(Transform raiz)
    {
        var umbral = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Umbral");
        if (umbral == null)
        {
            Debug.LogWarning("[BSL] No se encontró el umbral de la puerta: se usan las medidas por defecto.");
            paredZ0 = ParedZ0Defecto; paredZ1 = ParedZ1Defecto;
            return;
        }
        float zv = umbral.position.z, grosor = Mathf.Max(0.05f, Mathf.Abs(umbral.lossyScale.z) - 0.1f);
        paredZ0 = zv - grosor / 2f;
        paredZ1 = zv + grosor / 2f;
    }

    static void CabinaDesinfeccion(Transform padre)
    {
        float cx = (PuertaX0 + PuertaX1) / 2f, ancho = PuertaX1 - PuertaX0;
        float z0 = paredZ0 - FondoCamara, fondo = ZV - z0, zc = (z0 + ZV) / 2f;
        const float alto = AltoCamara;
        MM.Emisivo("EmisivoVerde", new Color(0.3f, 1f, 0.45f), 2f);
        MM.Emisivo("EmisivoRojo", new Color(1f, 0.22f, 0.18f), 2f);

        // Raíz en el suelo, en el centro de la cámara; +z hacia el BSL-3.
        var c = new GameObject("Camara").transform;
        c.SetParent(padre, false);
        c.position = V(cx, suelo, zc);
        var cam = c.gameObject.AddComponent<CamaraDescontaminacion>();
        cam.ancho = ancho;
        cam.fondo = fondo;

        // Paredes laterales hasta el vidrio (cámara estanca), techo, difusor y suelo de rejilla.
        foreach (var lado in new[] { -1f, 1f })
        {
            float x = lado * (ancho / 2f + 0.03f);
            MM.Caja(c, "Lateral", V(x, alto / 2f, 0f), V(0.06f, alto, fondo), M["Blanco"], true);
            for (int i = 0; i < 3; i++)
                MM.Caja(c, "Boquilla", V(x - lado * 0.035f, 0.6f + i * 0.55f, 0f), V(0.012f, 0.04f, fondo - 0.25f), M["EmisivoAzul"], false);
        }
        MM.Caja(c, "Techo", V(0f, alto + 0.08f, 0f), V(ancho + 0.14f, 0.16f, fondo), M["Blanco"], true);
        MM.Caja(c, "Difusor", V(0f, alto - 0.005f, 0f), V(0.5f, 0.01f, 0.5f), M["EmisivoAzul"], false);
        MM.Caja(c, "Rejilla", V(0f, 0.006f, 0f), V(ancho - 0.05f, 0.012f, fondo - 0.05f), M["Oscuro"], false);
        MM.Caja(c, "BordeRejilla", V(0f, 0.004f, 0f), V(ancho, 0.006f, fondo), M["EmisivoAzul"], false);
        // Dintel de la puerta exterior (la interior usa el dintel del vidrio).
        MM.Caja(c, "DintelExterior", V(0f, (AltoPuerta + alto) / 2f, -fondo / 2f), V(ancho + 0.12f, alto - AltoPuerta + 0.02f, 0.06f), M["Blanco"], true);

        // Puertas: las dos hojas se deslizan hacia el este (al oeste el vidrio del BSL-3 está muy cerca).
        cam.exterior = Puerta(c, "PuertaExterior", -fondo / 2f - 0.06f, ancho);
        cam.interior = Puerta(c, "PuertaInterior", fondo / 2f + 0.06f, ancho);

        // Niebla desinfectante: dos cortinas laterales y una del techo.
        var matNiebla = MaterialNiebla();
        cam.niebla = new[]
        {
            Niebla(c, "NieblaOeste", V(-ancho / 2f + 0.04f, 1.15f, 0f), Quaternion.LookRotation(Vector3.right), V(fondo - 0.3f, 1.3f, 0.02f), matNiebla),
            Niebla(c, "NieblaEste", V(ancho / 2f - 0.04f, 1.15f, 0f), Quaternion.LookRotation(Vector3.left), V(fondo - 0.3f, 1.3f, 0.02f), matNiebla),
            Niebla(c, "NieblaTecho", V(0f, alto - 0.05f, 0f), Quaternion.LookRotation(Vector3.down, Vector3.forward), V(ancho - 0.3f, fondo - 0.3f, 0.02f), matNiebla),
        };

        // Luces: azul tenue fija (horneada) y, solo durante el ciclo, la del ciclo y la UV-C (tiempo real).
        var azul = new GameObject("LuzAzul").AddComponent<Light>();
        azul.transform.SetParent(c, false);
        azul.transform.localPosition = V(0f, 2.1f, 0f);
        azul.type = LightType.Point; azul.color = new Color(0.35f, 0.65f, 1f); azul.range = 2.6f; azul.intensity = 1.8f;
        azul.lightmapBakeType = LightmapBakeType.Baked;
        cam.luzCiclo = LuzCamara(c, "LuzCiclo", new Color(0.65f, 0.88f, 1f));
        cam.luzUV = LuzCamara(c, "LuzUV", new Color(0.55f, 0.2f, 1f));

        var audio = c.gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = 1f; audio.maxDistance = 8f;
        cam.aire = audio;

        // Pantalla interior (pared este, mirando hacia dentro) con barra de progreso.
        float w = Mathf.Min(0.9f, fondo - 0.3f);
        var pi = new GameObject("PantallaInterior").transform;
        pi.SetParent(c, false);
        pi.localPosition = V(ancho / 2f - 0.005f, 1.5f, 0f);
        pi.localRotation = Quaternion.LookRotation(Vector3.right);
        MM.Caja(pi, "Fondo", V(0f, 0f, 0.005f), V(w, 0.36f, 0.01f), M["PanelHUD"], false);
        MM.Caja(pi, "Borde", V(0f, 0f, 0.008f), V(w + 0.02f, 0.38f, 0.008f), M["EmisivoCian"], false);
        cam.pantallaInterior = MM.Texto3D(pi, "<b>LISTO</b>", V(0f, 0.04f, -0.003f), Quaternion.identity, 0.32f, Color.white, w - 0.06f);
        cam.pantallaInterior.alignment = TextAlignmentOptions.Center;
        cam.pantallaInterior.rectTransform.sizeDelta = new Vector2(w - 0.06f, 0.24f);
        float lb = w - 0.08f;
        MM.Caja(pi, "BarraFondo", V(0f, -0.135f, -0.002f), V(lb, 0.025f, 0.004f), M["Oscuro"], false);
        var barra = new GameObject("Barra").transform;
        barra.SetParent(pi, false);
        barra.localPosition = V(-lb / 2f, -0.135f, -0.004f);
        MM.Caja(barra, "Relleno", V(lb / 2f, 0f, 0f), V(lb, 0.025f, 0.004f), M["EmisivoCian"], false);
        barra.localScale = V(0.001f, 1f, 1f);
        cam.barra = barra;

        // Estado visible desde fuera (sobre la puerta exterior) y desde el BSL-3 (sobre la interior).
        cam.pantallaExterior = Rotulo(c, "EstadoExterior", V(0f, alto + 0.07f, -fondo / 2f - 0.045f), 0f, ancho + 0.1f);
        cam.pantallaBSL = Rotulo(c, "EstadoBSL", V(0f, AltoPuerta + 0.08f, fondo / 2f + 0.05f), 180f, ancho + 0.1f);

        // Carteles: fuera, encima de la cámara; dentro del BSL-3, sobre la puerta.
        foreach (var (pos, rotY, texto, fondoCartel) in new[]
                 {
                     (V(0f, alto + 0.4f, -fondo / 2f - 0.02f), 0f, "ENTRADA\nÁREA BSL-3", "Amarillo"),
                     (V(0f, AltoPuerta + 0.42f, fondo / 2f + 0.07f), 180f, "DESCONTAMINACIÓN\nANTES DE SALIR", "BlancoMate"),
                 })
        {
            var cartel = new GameObject("Cartel").transform;
            cartel.SetParent(c, false);
            cartel.localPosition = pos;
            cartel.localRotation = Quaternion.Euler(0f, rotY, 0f);
            MM.Caja(cartel, "Placa", V(0f, 0f, 0.012f), V(ancho + 0.2f, 0.42f, 0.02f), M[fondoCartel], false);
            var t = MM.Texto3D(cartel, texto, V(0f, 0f, -0.002f), Quaternion.identity, 0.8f, Color.black, ancho + 0.15f);
            t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        }
    }

    // Puerta corredera: hoja de vidrio con marco (Rigidbody cinemático + collider sólido) y semáforo al oeste.
    static PuertaCorrediza Puerta(Transform c, string nombre, float z, float ancho)
    {
        var p = new GameObject(nombre).transform;
        p.SetParent(c, false);
        p.localPosition = V(0f, 0f, z);
        var pc = p.gameObject.AddComponent<PuertaCorrediza>();
        var hoja = new GameObject("Hoja").transform;
        hoja.SetParent(p, false);
        float w = ancho + 0.1f, h = AltoPuerta - 0.01f;
        MM.Caja(hoja, "Vidrio", V(0f, h / 2f, 0f), V(w, h, 0.03f), M["VidrioPared"], true);
        MM.Caja(hoja, "MarcoSup", V(0f, h - 0.03f, 0f), V(w, 0.06f, 0.045f), M["Marco"], false);
        MM.Caja(hoja, "MarcoInf", V(0f, 0.06f, 0f), V(w, 0.12f, 0.045f), M["Marco"], false);
        foreach (var x in new[] { -w / 2f + 0.03f, w / 2f - 0.03f })
            MM.Caja(hoja, "MarcoLado", V(x, h / 2f, 0f), V(0.06f, h, 0.045f), M["Marco"], false);
        MM.Caja(hoja, "Franja", V(0f, 1.05f, 0f), V(w - 0.12f, 0.05f, 0.046f), M["EmisivoRosa"], false);
        MM.Caja(hoja, "Junta", V(-w / 2f + 0.065f, h / 2f, 0f), V(0.012f, h - 0.2f, 0.05f), M["Oscuro"], false);
        var rb = hoja.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        pc.hoja = hoja;
        pc.cerrada = Vector3.zero;
        pc.abierta = V(w + 0.04f, 0f, 0f);

        var sem = new GameObject("Semaforo").transform;
        sem.SetParent(p, false);
        sem.localPosition = V(-ancho / 2f - 0.14f, 1.9f, 0f);
        MM.Caja(sem, "Caja", Vector3.zero, V(0.08f, 0.2f, 0.05f), M["Oscuro"], false);
        var rojo = MM.Caja(sem, "Rojo", V(0f, 0.05f, 0f), V(0.05f, 0.05f, 0.07f), M["EmisivoRojo"], false);
        var verde = MM.Caja(sem, "Verde", V(0f, -0.05f, 0f), V(0.05f, 0.05f, 0.07f), M["EmisivoVerde"], false);
        pc.luzRoja = new[] { rojo };
        pc.luzVerde = new[] { verde };
        verde.SetActive(false);
        return pc;
    }

    static TMP_Text Rotulo(Transform c, string nombre, Vector3 pos, float rotY, float ancho)
    {
        var r = new GameObject(nombre).transform;
        r.SetParent(c, false);
        r.localPosition = pos;
        r.localRotation = Quaternion.Euler(0f, rotY, 0f);
        MM.Caja(r, "Fondo", V(0f, 0f, 0.008f), V(ancho, 0.14f, 0.01f), M["PanelHUD"], false);
        var t = MM.Texto3D(r, "<color=#7CF29A><b>CÁMARA LIBRE</b></color> · acércate a la puerta", V(0f, 0f, -0.002f), Quaternion.identity, 0.42f, Color.white, ancho - 0.06f);
        t.alignment = TextAlignmentOptions.Center;
        return t;
    }

    static Light LuzCamara(Transform c, string nombre, Color color)
    {
        var l = new GameObject(nombre).AddComponent<Light>();
        l.transform.SetParent(c, false);
        l.transform.localPosition = V(0f, AltoCamara - 0.25f, 0f);
        l.type = LightType.Point; l.color = color; l.range = 2.8f; l.intensity = 1f;
        l.shadows = LightShadows.None;
        l.lightmapBakeType = LightmapBakeType.Realtime;
        l.enabled = false;
        return l;
    }

    // Material de partículas por defecto de Unity (se incluye en la build al usarlo en la escena).
    static Material MaterialNiebla()
    {
        var m = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        if (m != null) return m;
        m = MM.MatEn("Assets/NanoLab/Materiales/Micro", "NieblaDescontaminacion", Shader.Find("Particles/Standard Unlit"));
        m.mainTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        return m;
    }

    // Cortina de niebla: caja emisora que lanza partículas a lo largo de su z local; se frenan
    // enseguida y quedan flotando, crecen y se desvanecen. CamaraDescontaminacion cambia la emisión.
    static ParticleSystem Niebla(Transform padre, string nombre, Vector3 pos, Quaternion rot, Vector3 caja, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.96f, 1f, 0.22f), new Color(0.8f, 0.9f, 1f, 0.32f));
        main.gravityModifier = -0.02f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 140;
        var em = ps.emission;
        em.rateOverTime = 0f;
        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Box;
        sh.scale = caja;
        var lim = ps.limitVelocityOverLifetime;
        lim.enabled = true; lim.limit = 0.25f; lim.dampen = 0.12f;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.7f));
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                   new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = gr;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortMode = ParticleSystemSortMode.Distance;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    // Carteles de riesgo en la cara del vidrio que da a la sala principal.
    static void Senales(Transform padre)
    {
        float z = paredZ0 - 0.04f;
        MM.Senal(padre, "SenalRiesgo", V(-5.8f, suelo + 1.75f, z), Quaternion.identity, "ÁREA DE ALTO RIESGO\nAGENTES BIOLÓGICOS", 0.75f);
        MM.Senal(padre, "SenalAcceso", V(-3.6f, suelo + 1.75f, z), Quaternion.identity, "ACCESO\nRESTRINGIDO", 0.42f);
        MM.CartelEPP(padre, V(-7.3f, suelo + 1.95f, z));
    }

    static void Luces(Transform g)
    {
        foreach (var x in new[] { X0 + 1.9f, X1 - 1.9f })
            foreach (var z in new[] { paredZ1 + 2.2f, ZF - 2.2f })
                Luz(g, "LuzTecho", V(x, MC.AltoSala - 0.2f, z), new Color(1f, 0.97f, 0.95f), 6.5f, 1.15f);
        Luz(g, "LuzRosada", V((X0 + X1) / 2f, MC.AltoSala - 0.4f, (paredZ1 + ZF) / 2f), new Color(1f, 0.5f, 0.62f), 8f, 1.0f);
        // Hueco técnico: un tono rosado tenue detrás del vidrio.
        Luz(g, "LuzHuecoO", V(X0 - Hueco / 2f, suelo + 2f, (paredZ1 + ZF) / 2f), new Color(1f, 0.5f, 0.62f), 4f, 0.6f);
        Luz(g, "LuzHuecoE", V(X1 + Hueco / 2f, suelo + 2f, (paredZ1 + ZF) / 2f), new Color(1f, 0.5f, 0.62f), 4f, 0.6f);
        Luz(g, "LuzHuecoN", V((X0 + X1) / 2f, suelo + 2f, ZF + Hueco / 2f), new Color(1f, 0.5f, 0.62f), 4f, 0.6f);
    }

    static void Luz(Transform g, string nombre, Vector3 pos, Color color, float rango, float intensidad)
    {
        var l = new GameObject(nombre).AddComponent<Light>();
        l.transform.SetParent(g, false);
        l.transform.position = pos;
        l.type = LightType.Point; l.color = color; l.range = rango; l.intensity = intensidad;
        l.lightmapBakeType = LightmapBakeType.Baked;
    }

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
}
