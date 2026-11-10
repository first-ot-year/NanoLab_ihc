using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MM = MontarMicrobiologia;
using MC = MontarCentroExperimentacion;
using ML = MontarLaboratorio;

// Almacenamiento interactivo de Microbiologia.unity (Compartimento + Hueco + Guardable):
//  - Mesas con cajones del pack ("table with drawers"): sus tres cajones de madera (pCube22, 186, 187)
//    se deslizan y la hoja de vidrio frontal (pCube23) se abre como puerta de vitrina. Dentro hay
//    portaobjetos y placas de cultivo que se sacan, y huecos libres para guardar lo que se quiera.
//  - Armarios de madera con puertas de vidrio ("showcase"): las dos hojas giran sobre su bisagra.
//  - Muebles blancos de las islas de microscopios: se les añade un cajón de verdad.
//  - Zona de criopreservación de la sala BSL-3: ultracongelador de -80 °C con puerta de vidrio y los
//    criotubos de virus, y criotanque de nitrógeno líquido (-196 °C) con las cepas bacterianas.
// Todo se apunta con el rayo de la mano vacía (gatillo o A/X) para abrir/cerrar, y se agarra con el SDK.
public static class MontarAlmacenamiento
{
    const string SlidePrefab = "Assets/NanoLab/Prefabs/Portaobjetos.prefab";
    const string Pack = "Assets/3D Laboratory Environment with Appratus/Prefabs/";
    // Mallas de "table with drawers" (medidas en el FBX): cajones de arriba abajo y hoja de vidrio.
    static readonly string[] Cajones = { "pCube22", "pCube186", "pCube187" };
    const string Armazon = "pCube12", HojaVidrio = "pCube23", Bisagra = "BisagraVidrio";
    public const string NombreZona = "ZonaCriopreservacion";

    static Dictionary<string, Material> M => MM.mats;
    static float suelo => MM.suelo;

    static readonly Vector2 PasoPorta = new Vector2(0.095f, 0.036f);
    static readonly Vector2 PasoTubo = new Vector2(0.07f, 0.07f);
    static readonly Vector2 PasoPlaca = new Vector2(0.13f, 0.13f);
    static readonly Vector2 PasoVidrio = new Vector2(0.12f, 0.12f);

    // ================================================================== entrada

    // Rehace la sala BSL-3 (zona de criopreservación y mesa de criotubos con cajones) y convierte las
    // mesas con cajones de la sala principal, sin regenerar el resto. Incluye el horneado de la luz:
    // los cajones y puertas dejan de ser estáticos.
    [MenuItem("NanoLab/Montar cajones, vitrinas y criopreservación", false, 4)]
    public static void Reconstruir()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Cajones, vitrinas y criopreservación",
                "Se rehace la sala de alto riesgo (con la zona de criopreservación) y se convierten las mesas con cajones " +
                "de Microbiologia.unity. Incluye el horneado de la luz y puede tardar.\n\n¿Continuar?", "Continuar", "Cancelar"))
            return;
        MontarSalaAltoRiesgo.Reconstruir();
    }

    internal static MuestraData[] Muestras(TipoMuestra tipo) =>
        AssetDatabase.FindAssets("t:MuestraData", new[] { "Assets/NanoLab/Muestras" })
            .Select(g => AssetDatabase.LoadAssetAtPath<MuestraData>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(m => m != null && m.tipo == tipo).OrderBy(m => m.codigo).ToArray();

    // ================================================================== mesas con cajones

    internal class Espacio
    {
        public Transform padre;       // lo que se mueve con él (el cajón) o la mesa
        public Compartimento comp;
        public Vector3 centro;        // en el espacio de 'padre', a ras de la superficie
        public Vector2 area;          // ancho (x) y fondo (z) útiles, en metros
    }

    internal class MesaConvertida
    {
        public List<Espacio> vitrina = new List<Espacio>();   // baldas detrás de la hoja de vidrio (de arriba abajo)
        public List<Espacio> cajones = new List<Espacio>();   // de arriba abajo
    }

    // Todas las mesas con cajones de la sala (menos las del Centro de Experimentación, donde
    // las muestras de los retos no deben estar a la vista con su nombre).
    internal static void Mesas()
    {
        var slide = AssetDatabase.LoadAssetAtPath<GameObject>(SlidePrefab);
        Physics.SyncTransforms();
        var mesas = Object.FindObjectsOfType<Transform>(true).Where(t => t.Find(Armazon) != null && t.Find(Cajones[0]) != null).ToList();
        int n = 0;
        foreach (var t in mesas)
        {
            var ancestros = t.GetComponentsInParent<Transform>(true);
            if (ancestros.Any(p => p.name == MC.NombreRaiz)) continue;
            if (ancestros.Any(p => p.name == "MesaCriotubos")) continue;   // la convierte MontarSalaAltoRiesgo
            var m = ConvertirMesa(t);
            if (m == null) continue;
            n++;
            Llenar(m, slide);
        }
        Debug.Log($"[Almacen] Mesas con cajones interactivos en la sala: {n}.");
    }

    // Como en un laboratorio real, los muebles guardan material, no muestras (esas van al ultracongelador
    // y al criotanque): vitrina y cajones con portaobjetos y placas de Petri estériles.
    internal static void Llenar(MesaConvertida m, GameObject slide)
    {
        foreach (var v in m.vitrina) Llenar(Huecos(v, PasoPlaca, 10), Varios(8, PlacaVacia));   // microbiología: placas, no cristalería de química
        string[] rotulos = { "PORTAOBJETOS", "PLACAS DE PETRI", "PLACAS DE RESERVA" };
        for (int i = 0; i < m.cajones.Count; i++)
        {
            var c = m.cajones[i];
            RotuloCajon(c, rotulos[Mathf.Min(i, rotulos.Length - 1)]);
            if (i == 0) Llenar(Huecos(c, PasoPorta, 16), Varios(8, () => Portaobjetos(slide)));
            else if (i == 1) Llenar(Huecos(c, PasoPlaca, 12), Varios(6, PlacaVacia));
            else Llenar(Huecos(c, PasoPlaca, 12), Varios(6, PlacaVacia));
        }
    }

    // Prepara una mesa del pack: cajones correderos y hoja de vidrio con bisagra. Se puede repetir
    // (rehace colliders y huecos). Devuelve null si delante no hay sitio para abrir nada.
    internal static MesaConvertida ConvertirMesa(Transform t)
    {
        var raizPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
        if (raizPrefab != null) PrefabUtility.UnpackPrefabInstance(raizPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var h in t.GetComponentsInChildren<Transform>(true).Where(x => x != null && x.name == "Huecos").ToArray())
            if (h != null) Object.DestroyImmediate(h.gameObject);
        foreach (var r in t.GetComponentsInChildren<Transform>(true).Where(x => x != null && x.name == "RotuloCajon").ToArray())
            if (r != null) Object.DestroyImmediate(r.gameObject);
        Physics.SyncTransforms();

        var res = new MesaConvertida();
        var marco = BoundsMalla(t, t.Find(Armazon));
        float frente = marco.max.z;   // los cajones y el vidrio dan a +z local (FBX)

        // ---- cajones
        var cajones = Cajones.Select(n => t.Find(n)).Where(c => c != null).ToList();
        if (cajones.Count > 0)
        {
            var bc = BoundsMalla(t, cajones[0]);
            foreach (var c in cajones.Skip(1)) bc.Encapsulate(BoundsMalla(t, c));
            if (SitioDelante(t, new Vector3(bc.center.x, bc.center.y, frente + 0.25f), new Vector3(bc.extents.x - 0.02f, bc.extents.y - 0.03f, 0.22f)))
                foreach (var c in cajones) res.cajones.Add(Cajon(t, c));
        }

        // ---- hoja de vidrio: puerta con bisagra en el borde exterior
        var bisagra = t.Find(Bisagra);
        var hoja = t.Find(HojaVidrio) ?? (bisagra != null ? bisagra.Find(HojaVidrio) : null);
        if (hoja != null)
        {
            if (bisagra != null && hoja.parent == bisagra) { hoja.SetParent(t, true); Object.DestroyImmediate(bisagra.gameObject); }
            var hb = BoundsMalla(t, hoja);
            bool izq = Mathf.Abs(hb.min.x - marco.center.x) > Mathf.Abs(hb.max.x - marco.center.x);
            float ancho = hb.size.x;
            if (SitioDelante(t, new Vector3(hb.center.x, hb.center.y, frente + ancho / 2f + 0.03f), new Vector3(ancho / 2f - 0.02f, hb.extents.y - 0.05f, ancho / 2f - 0.02f)))
            {
                bisagra = new GameObject(Bisagra).transform;
                bisagra.SetParent(t, false);
                bisagra.localPosition = new Vector3(izq ? hb.min.x : hb.max.x, hb.center.y, hb.center.z);
                hoja.SetParent(bisagra, true);
                NoEstatico(bisagra.gameObject);
                foreach (var col in hoja.GetComponents<Collider>()) Object.DestroyImmediate(col);
                hoja.gameObject.AddComponent<BoxCollider>();
                var comp = bisagra.gameObject.AddComponent<Compartimento>();
                comp.tipo = Compartimento.Tipo.Bisagra;
                comp.eje = Vector3.up;
                comp.angulo = izq ? -100f : 100f;   // se abre hacia fuera (+z)
                comp.nombre = "la vitrina";
                comp.avisoAlAbrir = "Vitrina de material estéril: placas de Petri.\nSuelta un objeto sobre una silueta azul para guardarlo.";

                // Baldas: superficies horizontales del armazón detrás del vidrio.
                float zc = frente - 0.26f;
                foreach (var y in Baldas(t, new Vector3(hb.center.x, hb.max.y - 0.05f, zc), hb.size.y))
                    res.vitrina.Add(new Espacio { padre = t, comp = comp, centro = new Vector3(hb.center.x, y + 0.001f, zc), area = new Vector2(ancho - 0.12f, 0.42f) });
            }
        }
        Physics.SyncTransforms();
        Debug.Log($"[Almacen] {t.name} ({t.position.x:F1}, {t.position.z:F1}): {res.cajones.Count} cajones, vitrina con {res.vitrina.Count} balda(s).");
        return res.cajones.Count + res.vitrina.Count > 0 ? res : null;
    }

    static Espacio Cajon(Transform t, Transform c)
    {
        var b = c.GetComponent<MeshFilter>().sharedMesh.bounds;   // espacio local del cajón
        NoEstatico(c.gameObject);
        foreach (var col in c.GetComponents<Collider>()) Object.DestroyImmediate(col);
        // Fondo y frente (el rayo encuentra el frente; lo que se suelta dentro se apoya en el fondo).
        var fondo = c.gameObject.AddComponent<BoxCollider>();
        fondo.center = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z); fondo.size = new Vector3(b.size.x, 0.02f, b.size.z);
        var tapa = c.gameObject.AddComponent<BoxCollider>();
        tapa.center = new Vector3(b.center.x, b.center.y, b.max.z - 0.01f); tapa.size = new Vector3(b.size.x, b.size.y, 0.02f);
        var comp = c.GetComponent<Compartimento>();
        if (comp == null) comp = c.gameObject.AddComponent<Compartimento>();
        comp.tipo = Compartimento.Tipo.Corredera;
        comp.recorrido = t.InverseTransformVector(c.TransformVector(new Vector3(0f, 0f, b.size.z * 0.6f)));   // en el espacio de la mesa
        comp.nombre = "el cajón";
        return new Espacio { padre = c, comp = comp, centro = new Vector3(b.center.x, b.min.y + 0.021f, b.center.z - 0.02f), area = new Vector2(b.size.x - 0.08f, b.size.z - 0.1f) };
    }

    // Alturas (espacio de la mesa) de las superficies donde apoyar cosas dentro de la vitrina, de arriba abajo.
    static List<float> Baldas(Transform t, Vector3 desdeLocal, float alto)
    {
        var ys = new List<float>();
        var origen = t.TransformPoint(desdeLocal);
        foreach (var hit in Physics.RaycastAll(origen, -t.up, alto * Mathf.Abs(t.lossyScale.y), ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(t) || Vector3.Dot(hit.normal, t.up) < 0.7f) continue;
            float y = t.InverseTransformPoint(hit.point).y;
            if (ys.All(o => Mathf.Abs(o - y) > 0.12f)) ys.Add(y);
        }
        if (ys.Count == 0) ys.Add(desdeLocal.y + 0.05f - alto + 0.03f);
        return ys.OrderByDescending(y => y).Take(3).ToList();
    }

    // Nada sólido (paredes, muebles) en la caja local indicada: hay sitio para abrir.
    static bool SitioDelante(Transform t, Vector3 centroLocal, Vector3 medio)
    {
        var s = t.lossyScale;
        var he = new Vector3(Mathf.Abs(medio.x * s.x), Mathf.Abs(medio.y * s.y), Mathf.Abs(medio.z * s.z));
        foreach (var c in Physics.OverlapBox(t.TransformPoint(centroLocal), he, t.rotation, ~0, QueryTriggerInteraction.Ignore))
        {
            if (c.transform.IsChildOf(t) || c.attachedRigidbody != null || c.GetComponentInParent<Silla>() != null) continue;
            Debug.Log($"[Almacen] {t.name}: no hay sitio para abrir (choca con {c.name}).");
            return false;
        }
        return true;
    }

    static Bounds BoundsMalla(Transform raiz, Transform hijo)
    {
        var m = hijo.GetComponent<MeshFilter>().sharedMesh.bounds;
        var b = new Bounds(raiz.InverseTransformPoint(hijo.TransformPoint(m.center)), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            var esquina = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            b.Encapsulate(raiz.InverseTransformPoint(hijo.TransformPoint(esquina)));
        }
        return b;
    }

    // Lo que se mueve no puede ser estático (el static batching lo dejaría fijo): se ilumina con sondas.
    static void NoEstatico(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.receiveGI = ReceiveGI.LightProbes;
    }

    static void RotuloCajon(Espacio c, string texto)
    {
        var b = c.padre.GetComponent<MeshFilter>().sharedMesh.bounds;
        var r = new GameObject("RotuloCajon").transform;
        r.SetParent(c.padre, false);
        r.localPosition = new Vector3(b.center.x, b.center.y + b.extents.y * 0.55f, b.max.z + 0.003f);
        r.localRotation = Quaternion.Euler(0f, 180f, 0f);   // legible desde delante (+z)
        var s = c.padre.lossyScale;
        r.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
        var t = MM.Texto3D(r, texto, Vector3.zero, Quaternion.identity, 0.16f, new Color(0.85f, 0.95f, 1f), 0.4f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 220);
    }

    // ================================================================== todos los muebles

    // Mesas con cajones, armarios de puertas de vidrio y cajones de las islas de microscopios.
    internal static void Muebles()
    {
        Mesas();
        Armarios();
        CajonesIslas();
    }

    // ================================================================== armario de puertas de vidrio

    // "showcase" del pack (2,3 × 2,7 m): dos puertas (marco pCube140/173, vidrio showcase_mirror_3/4 y
    // tirador pCube148/149) que giran sobre su borde exterior, y baldas con placas, portaobjetos y huecos.
    static readonly string[] HojasArmario = { "pCube140", "pCube173" };
    static readonly string[] PiezasPuerta = { "showcase_mirror_3", "showcase_mirror_4", "pCube148", "pCube149" };

    internal static void Armarios()
    {
        var slide = AssetDatabase.LoadAssetAtPath<GameObject>(SlidePrefab);
        Physics.SyncTransforms();
        var armarios = Object.FindObjectsOfType<Transform>(true).Where(t => t.Find("pCube201") != null && t.Find("pCube144") != null).ToList();
        int n = 0;
        foreach (var t in armarios)
        {
            if (t.GetComponentsInParent<Transform>(true).Any(p => p.name == MC.NombreRaiz)) continue;
            if (ConvertirArmario(t, slide)) n++;
        }
        Debug.Log($"[Almacen] Armarios de puertas de vidrio interactivos: {n}.");
    }

    static bool ConvertirArmario(Transform t, GameObject slide)
    {
        var raizPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
        if (raizPrefab != null) PrefabUtility.UnpackPrefabInstance(raizPrefab, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        // Repetible: deshace bisagras y huecos de una conversión anterior.
        foreach (var b in t.Cast<Transform>().Where(c => c.name == "BisagraPuerta").ToArray())
        {
            foreach (var hijo in b.Cast<Transform>().ToArray()) hijo.SetParent(t, true);
            Object.DestroyImmediate(b.gameObject);
        }
        foreach (var h in t.GetComponentsInChildren<Transform>(true).Where(x => x != null && x.name == "Huecos").ToArray())
            if (h != null) Object.DestroyImmediate(h.gameObject);
        Physics.SyncTransforms();

        float s = Mathf.Abs(t.lossyScale.x);   // metros -> unidades locales: m / s
        var hojas = HojasArmario.Select(x => t.Find(x)).Where(x => x != null && x.GetComponent<MeshFilter>() != null).ToList();
        if (hojas.Count == 0) return false;
        Bounds? total = null;
        foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
        {
            var b = BoundsMalla(t, mf.transform);
            if (total == null) total = b; else { var x = total.Value; x.Encapsulate(b); total = x; }
        }
        var marco = total.Value;
        var bh0 = BoundsMalla(t, hojas[0]);
        float sz = Mathf.Sign(bh0.center.z - marco.center.z);   // lado de las puertas
        float frenteHojas = sz > 0 ? bh0.min.z : bh0.max.z;

        // Baldas alcanzables (entre 0,25 y 1,75 m del suelo) detrás del vidrio, de arriba abajo.
        float zc = frenteHojas - sz * 0.2f / s;
        var ys = new List<float>();
        var origen = t.TransformPoint(new Vector3((marco.center.x + bh0.center.x) / 2f, marco.max.y - 0.1f / s, zc));
        foreach (var hit in Physics.RaycastAll(origen, -t.up, marco.size.y * s, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(t) || Vector3.Dot(hit.normal, t.up) < 0.7f) continue;
            float alto = hit.point.y - suelo;
            float y = t.InverseTransformPoint(hit.point).y;
            if (alto < 0.25f || alto > 1.75f || ys.Any(o => Mathf.Abs(o - y) < 0.12f / s)) continue;
            ys.Add(y);
        }
        ys = ys.OrderByDescending(y => y).Take(3).ToList();

        int puertas = 0;
        foreach (var hoja in hojas)
        {
            var hb = BoundsMalla(t, hoja);
            bool exteriorEnMin = Mathf.Abs(hb.min.x - marco.center.x) > Mathf.Abs(hb.max.x - marco.center.x);
            float ancho = hb.size.x;
            if (!SitioDelante(t, new Vector3(hb.center.x, hb.center.y, frenteHojas + sz * (ancho / 2f + 0.05f / s)),
                              new Vector3(ancho / 2f - 0.03f / s, Mathf.Min(hb.extents.y, 0.9f / s), ancho / 2f - 0.03f / s)))
                continue;
            var bis = new GameObject("BisagraPuerta").transform;
            bis.SetParent(t, false);
            bis.localPosition = new Vector3(exteriorEnMin ? hb.min.x : hb.max.x, hb.center.y, hb.center.z);
            // Vidrio y tirador que caen dentro de esta hoja.
            var piezas = new List<Transform> { hoja };
            foreach (var nombre in PiezasPuerta)
            {
                var p = t.Find(nombre);
                if (p == null || p.GetComponent<MeshFilter>() == null) continue;
                float cx = BoundsMalla(t, p).center.x;
                if (cx > hb.min.x && cx < hb.max.x) piezas.Add(p);
            }
            foreach (var p in piezas)
            {
                p.SetParent(bis, true);
                foreach (var col in p.GetComponents<Collider>()) Object.DestroyImmediate(col);
                p.gameObject.AddComponent<BoxCollider>();
            }
            NoEstatico(bis.gameObject);
            var comp = bis.gameObject.AddComponent<Compartimento>();
            comp.tipo = Compartimento.Tipo.Bisagra;
            comp.eje = Vector3.up;
            comp.angulo = -100f * (exteriorEnMin ? 1f : -1f) * sz;   // la hoja gira hacia fuera
            comp.nombre = "el armario";
            comp.avisoAlAbrir = "Armario de material: placas de Petri estériles y un microscopio de repuesto.\nSuelta un objeto sobre una silueta azul para guardarlo.";
            puertas++;

            // Baldas de arriba: placas de Petri estériles (sin cristalería de química: NanoLab es de microbiología).
            // Balda de abajo: microscopio de repuesto tras la primera hoja; placas estériles tras la otra.
            bool primera = puertas == 1;
            for (int i = 0; i < ys.Count; i++)
            {
                var e = new Espacio { padre = t, comp = comp, centro = new Vector3(hb.center.x, ys[i] + 0.001f / s, zc), area = new Vector2(ancho * s - 0.12f, 0.3f) };
                bool ultima = i == ys.Count - 1 && ys.Count > 1;
                if (!ultima) Llenar(Huecos(e, PasoPlaca, 12), Varios(primera ? 8 : 6, PlacaVacia));
                else if (primera) MicroscopioGuardado(t, e.centro, Quaternion.Euler(0f, sz > 0 ? 180f : 0f, 0f));
                else Llenar(Huecos(e, PasoPlaca, 8), Varios(6, PlacaVacia));
            }
        }
        Physics.SyncTransforms();
        Debug.Log($"[Almacen] Armario {t.name} ({t.position.x:F1}, {t.position.z:F1}): {puertas} puerta(s), {ys.Count} balda(s).");
        return puertas > 0;
    }

    // ================================================================== cajones de las islas

    // Los muebles blancos bajo la encimera de las islas ("shelf showcase") son una sola caja: el cajón
    // solo estaba dibujado. Se añade un cajón de verdad en la cara del tirador (el tirador del pack pasa
    // al frente del cajón nuevo) con portaobjetos y huecos para guardar.
    const float AltoCajon = 0.18f, FondoCajon = 0.42f, SalidaCajon = 0.32f;

    internal static void CajonesIslas()
    {
        var slide = AssetDatabase.LoadAssetAtPath<GameObject>(SlidePrefab);
        Physics.SyncTransforms();
        var tiradores = Object.FindObjectsOfType<MeshFilter>(true)
            .Where(mf => mf.sharedMesh != null && AssetDatabase.GetAssetPath(mf.sharedMesh).Contains("drawer handle") && mf.GetComponentInParent<Compartimento>() == null)
            .ToList();
        var muebles = Object.FindObjectsOfType<MeshFilter>(true)
            .Where(mf => mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("shelf_showcase"))
            .ToList();
        int n = 0;
        foreach (var mf in muebles)
        {
            if (mf.GetComponentsInParent<Transform>(true).Any(p => p.name == MC.NombreRaiz)) continue;
            foreach (var viejo in mf.transform.Cast<Transform>().Where(c => c.name == "CajonIsla").ToArray())
                Object.DestroyImmediate(viejo.gameObject);
            var r = mf.GetComponent<Renderer>();
            if (r == null) continue;
            var b = r.bounds;
            var cerca = b; cerca.Expand(0.16f);
            var tir = tiradores.Where(x => x != null && cerca.Contains(x.GetComponent<Renderer>().bounds.center))
                .OrderBy(x => b.SqrDistance(x.GetComponent<Renderer>().bounds.center)).FirstOrDefault();
            if (tir == null) { Debug.Log($"[Almacen] {mf.name} ({b.center.x:F1}, {b.center.z:F1}): sin tirador, no se le pone cajón."); continue; }
            if (CajonIsla(mf.transform, b, tir.transform, slide)) n++;
        }
        Debug.Log($"[Almacen] Cajones nuevos en las islas de microscopios: {n} (de {muebles.Count} muebles).");
    }

    static bool CajonIsla(Transform mueble, Bounds b, Transform tirador, GameObject slide)
    {
        var tb = tirador.GetComponent<Renderer>().bounds;
        var c = tb.center;
        // Cara del tirador: la más cercana a su centro.
        var caras = new[]
        {
            (n: Vector3.right, d: Mathf.Abs(c.x - b.max.x)), (n: Vector3.left, d: Mathf.Abs(c.x - b.min.x)),
            (n: Vector3.forward, d: Mathf.Abs(c.z - b.max.z)), (n: Vector3.back, d: Mathf.Abs(c.z - b.min.z)),
        };
        var cara = caras.OrderBy(x => x.d).First().n;
        var u = Vector3.Cross(Vector3.up, cara);
        float anchoCara = Mathf.Abs(Vector3.Dot(b.size, u));
        float ancho = Mathf.Min(anchoCara - 0.08f, 0.52f);
        float y = Mathf.Clamp(c.y, b.min.y + AltoCajon, b.max.y - AltoCajon / 2f - 0.03f);
        float lateral = Mathf.Clamp(Vector3.Dot(c - b.center, u), -(anchoCara - ancho) / 2f, (anchoCara - ancho) / 2f);
        var pos = new Vector3(b.center.x, y, b.center.z) + u * lateral;
        float plano = Vector3.Dot(b.center, cara) + Mathf.Abs(Vector3.Dot(b.extents, cara));
        pos += cara * (plano - Vector3.Dot(pos, cara));
        var rot = Quaternion.LookRotation(cara);

        // Sitio delante para sacarlo.
        foreach (var col in Physics.OverlapBox(pos + cara * (SalidaCajon / 2f + 0.03f), new Vector3(ancho / 2f - 0.02f, AltoCajon / 2f - 0.02f, SalidaCajon / 2f), rot, ~0, QueryTriggerInteraction.Ignore))
        {
            if (col.transform.IsChildOf(mueble) || col.transform.IsChildOf(tirador) || col.attachedRigidbody != null || col.GetComponentInParent<Silla>() != null) continue;
            Debug.Log($"[Almacen] {mueble.name} ({b.center.x:F1}, {b.center.z:F1}): no hay sitio para abrir el cajón (choca con {col.name}).");
            return false;
        }

        var raiz = new GameObject("CajonIsla").transform;
        raiz.SetParent(mueble, false);
        raiz.SetPositionAndRotation(pos, rot);
        var ls = mueble.lossyScale;
        raiz.localScale = new Vector3(1f / ls.x, 1f / ls.y, 1f / ls.z);
        var movil = new GameObject("Cajon").transform;   // +z local: hacia fuera del mueble
        movil.SetParent(raiz, false);
        float w = ancho, h = AltoCajon, f = FondoCajon;
        MM.Caja(movil, "Frente", new Vector3(0f, 0f, 0.009f), new Vector3(w, h, 0.018f), M["Blanco"], true);
        MM.Caja(movil, "Junta", new Vector3(0f, 0f, 0.0005f), new Vector3(w + 0.012f, h + 0.012f, 0.001f), M["Oscuro"], false);
        MM.Caja(movil, "Fondo", new Vector3(0f, -h / 2f + 0.012f, -f / 2f), new Vector3(w - 0.03f, 0.012f, f), M["Blanco"], true);
        MM.Caja(movil, "Base", new Vector3(0f, -h / 2f + 0.0185f, -f / 2f), new Vector3(w - 0.05f, 0.001f, f - 0.02f), M["Oscuro"], false);
        foreach (var sx in new[] { -1f, 1f })
            MM.Caja(movil, "Lateral", new Vector3(sx * (w / 2f - 0.021f), -h / 2f + 0.06f, -f / 2f), new Vector3(0.012f, 0.1f, f), M["Blanco"], true);
        MM.Caja(movil, "Trasera", new Vector3(0f, -h / 2f + 0.06f, -f + 0.006f), new Vector3(w - 0.03f, 0.1f, 0.012f), M["Blanco"], true);
        // El tirador del pack pasa al frente del cajón (una copia que se mueve con él; el original se oculta).
        var copia = Object.Instantiate(tirador.gameObject, tirador.position + cara * 0.02f, tirador.rotation, movil);
        copia.name = "Tirador";
        foreach (var col in copia.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
        foreach (var rr in tirador.GetComponentsInChildren<Renderer>()) rr.enabled = false;
        foreach (var col in tirador.GetComponentsInChildren<Collider>()) col.enabled = false;
        NoEstatico(raiz.gameObject);

        var comp = movil.gameObject.AddComponent<Compartimento>();
        comp.tipo = Compartimento.Tipo.Corredera;
        comp.recorrido = new Vector3(0f, 0f, SalidaCajon);
        comp.nombre = "el cajón";
        var e = new Espacio { padre = movil, comp = comp, centro = new Vector3(0f, -h / 2f + 0.019f, -0.17f), area = new Vector2(w - 0.07f, 0.3f) };
        Llenar(Huecos(e, PasoPorta, 8), Enumerable.Range(0, 3).Select(_ => Portaobjetos(slide)));
        return true;
    }

    // ================================================================== huecos y objetos

    // Rejilla de huecos sobre un espacio, empezando por la fila más cercana al usuario (+z).
    internal static List<Hueco> Huecos(Espacio e, Vector2 paso, int max)
    {
        var g = new GameObject("Huecos").transform;
        g.SetParent(e.padre, false);
        g.localPosition = e.centro;
        var s = e.padre.lossyScale;
        g.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);   // los objetos guardados no se deforman
        int nx = Mathf.Max(1, Mathf.FloorToInt(e.area.x / paso.x)), nz = Mathf.Max(1, Mathf.FloorToInt(e.area.y / paso.y));
        float diam = Mathf.Min(paso.x, paso.y) * 0.8f;
        var lista = new List<Hueco>();
        for (int iz = nz - 1; iz >= 0 && lista.Count < max; iz--)
            for (int ix = 0; ix < nx && lista.Count < max; ix++)
            {
                var h = new GameObject("Hueco").transform;
                h.SetParent(g, false);
                h.localPosition = new Vector3((ix - (nx - 1) / 2f) * paso.x, 0f, (iz - (nz - 1) / 2f) * paso.y);
                var hueco = h.gameObject.AddComponent<Hueco>();
                hueco.compartimento = e.comp;
                hueco.radio = Mathf.Max(0.05f, Mathf.Min(paso.x, paso.y) * 0.6f);
                var f = MM.Cilindro(h, "Fantasma", new Vector3(0f, 0.002f, 0f), new Vector3(diam, 0.001f, diam), Quaternion.identity, M["Fantasma"]);
                hueco.fantasma = f.GetComponent<Renderer>();
                hueco.fantasma.enabled = false;
                lista.Add(hueco);
            }
        return lista;
    }

    static void Llenar(List<Hueco> huecos, IEnumerable<GameObject> objetos)
    {
        int i = 0;
        foreach (var o in objetos)
        {
            if (i >= huecos.Count) { Object.DestroyImmediate(o); continue; }
            var t = o.transform;
            t.SetParent(huecos[i++].transform, false);
            t.localPosition = Vector3.up * (o.GetComponent<Portaobjetos>() != null ? 0.0016f : 0f);
            t.localRotation = Quaternion.identity;
            o.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    static GameObject Portaobjetos(GameObject slidePrefab) => (GameObject)PrefabUtility.InstantiatePrefab(slidePrefab);

    // Criotubo que se agarra: lleva su muestra (tocarlo con un portaobjetos la carga) y se guarda en huecos.
    internal static GameObject Criotubo(MuestraData m, string prefijo)
    {
        var go = new GameObject($"{prefijo}_{m.name}");
        var brillo = Utileria.Criotubo(go.transform, m);
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.034f, 0f); bc.size = new Vector3(0.018f, 0.068f, 0.018f);
        var toque = new GameObject("Toque");
        toque.transform.SetParent(go.transform, false);
        var tc = toque.AddComponent<BoxCollider>();
        tc.isTrigger = true; tc.center = new Vector3(0f, 0.04f, 0f); tc.size = new Vector3(0.05f, 0.09f, 0.05f);
        go.AddComponent<FuenteMuestra>().muestra = m;
        var res = go.AddComponent<Resaltado>();
        res.brillos = new[] { brillo };
        res.radio = 0.08f;
        EtiquetaPequena(go.transform, m, 0.1f);
        ConfigurarMetaISDK.HacerAgarrable(go, 0.02f);
        go.GetComponent<Rigidbody>().isKinematic = true;
        go.AddComponent<Guardable>();
        return go;
    }

    // ---------------- material de laboratorio (sin muestra: se guarda en cajones y armarios)

    // Placa de Petri estéril vacía (Ø 9 cm): base y tapa de vidrio.
    internal static GameObject PlacaVacia()
    {
        var go = new GameObject("PlacaPetriEsteril");
        MM.Cilindro(go.transform, "Base", new Vector3(0f, 0.006f, 0f), new Vector3(0.09f, 0.006f, 0.09f), Quaternion.identity, M["VidrioReal"]);
        MM.Cilindro(go.transform, "Tapa", new Vector3(0f, 0.0085f, 0f), new Vector3(0.094f, 0.0055f, 0.094f), Quaternion.identity, M["VidrioReal"]);
        MM.Cilindro(go.transform, "Canto", new Vector3(0f, 0.0085f, 0f), new Vector3(0.0945f, 0.0045f, 0.0945f), Quaternion.identity, M["CantoVidrio"]);
        return Material(go, 0.05f);
    }

    // Cristalería del pack (vaso, matraz, probeta, tubo de ensayo…) escalada a su altura real.
    internal static GameObject Cristaleria(string prefab, float alto)
    {
        var go = new GameObject(prefab);
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + prefab + ".prefab");
        if (p != null)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(p, go.transform);
            // Sin prefab: ConfigurarMetaISDK no lo toma por un aparato suelto de química.
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var col in inst.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            foreach (var rb in inst.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            inst.transform.localRotation = Quaternion.identity;
            var bb = ML.BoundsDe(inst);
            inst.transform.localScale *= alto / Mathf.Max(0.001f, bb.size.y);
            bb = ML.BoundsDe(inst);
            inst.transform.localPosition -= new Vector3(bb.center.x, bb.min.y, bb.center.z);
        }
        else MM.Cilindro(go.transform, "Vidrio", new Vector3(0f, alto / 2f, 0f), new Vector3(alto * 0.5f, alto / 2f, alto * 0.5f), Quaternion.identity, M["VidrioReal"]);
        return Material(go, 0.15f);
    }

    static GameObject Material(GameObject go, float masa)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.receiveGI = ReceiveGI.LightProbes;
        ConfigurarMetaISDK.HacerAgarrable(go, masa);
        go.GetComponent<Rigidbody>().isKinematic = true;
        go.AddComponent<Guardable>();
        return go;
    }

    static IEnumerable<GameObject> Varios(int n, System.Func<GameObject> crear) => Enumerable.Range(0, n).Select(_ => crear()).ToList();

    // Microscopio de repuesto guardado (solo se ve: no tiene platina ni ocular activos).
    static void MicroscopioGuardado(Transform padre, Vector3 posLocal, Quaternion rotLocal)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NanoLab/Modelos/Microscopio.prefab");
        if (prefab == null) return;
        var g = new GameObject("MicroscopioRepuesto").transform;
        g.SetParent(padre, false);
        g.localPosition = posLocal;
        g.localRotation = rotLocal;
        var s = padre.lossyScale;
        g.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, g);
        var bb = ML.BoundsDe(inst);
        inst.transform.position -= new Vector3(bb.center.x - g.position.x, bb.min.y - g.position.y, bb.center.z - g.position.z);
        foreach (var r in inst.GetComponentsInChildren<MeshRenderer>(true)) r.receiveGI = ReceiveGI.LightProbes;
    }

    static void EtiquetaPequena(Transform padre, MuestraData m, float y)
    {
        var e = new GameObject("Etiqueta").transform;
        e.SetParent(padre, false);
        e.localPosition = new Vector3(0f, y, 0f);
        e.gameObject.AddComponent<Cartel>();
        var hex = ColorUtility.ToHtmlStringRGB(m.color);
        var t = MM.Texto3D(e, $"<b><color=#{hex}>{m.nombre}</color></b>", Vector3.zero, Quaternion.identity, 0.11f, Color.white, 0.14f);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 220);
    }

    // ================================================================== zona de criopreservación

    // 'pos' a ras de suelo; el frente de los equipos mira hacia +z local (rotado 'yaw').
    internal static void ZonaCriopreservacion(Transform padre, Vector3 pos, float yaw)
    {
        var virus = Muestras(TipoMuestra.Virus);
        var bacterias = Muestras(TipoMuestra.Bacteria);
        var z = new GameObject(NombreZona).transform;
        z.SetParent(padre, false);
        z.SetPositionAndRotation(new Vector3(pos.x, suelo, pos.z), Quaternion.Euler(0f, yaw, 0f));

        Ultracongelador(z, virus);
        Criotanque(z, new Vector3(1.0f, 0f, 0.05f), bacterias);

        var r = new GameObject("Rotulo").transform;
        r.SetParent(z, false);
        r.localPosition = new Vector3(0.5f, 2.2f, 0.1f);
        r.localRotation = Quaternion.Euler(0f, 180f, 0f);
        MM.Caja(r, "Fondo", new Vector3(0f, 0f, -0.01f), new Vector3(1.7f, 0.3f, 0.02f), M["PanelHUD"], false);
        MM.Caja(r, "Borde", new Vector3(0f, 0f, -0.012f), new Vector3(1.73f, 0.33f, 0.018f), M["EmisivoAzul"], false);
        var t = MM.Texto3D(r, "ZONA DE CRIOPRESERVACIÓN\n<size=60%>Ultracongelador -80 °C · Nitrógeno líquido -196 °C</size>", Vector3.zero, Quaternion.identity, 0.42f, new Color(0.55f, 0.85f, 1f), 1.6f);
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        MM.Caja(z, "FranjaSuelo", new Vector3(0.5f, 0.003f, 0.75f), new Vector3(2.1f, 0.006f, 0.04f), M["EmisivoAzul"], false);
        MC.FijoPrimitiva(r.gameObject);
    }

    // Ultracongelador vertical de 0,8 × 1,9 × 0,7 m con puerta de vidrio, tres baldas y criotubos de virus.
    static void Ultracongelador(Transform zona, MuestraData[] virus)
    {
        const float w = 0.8f, h = 1.9f, d = 0.7f, base_ = 0.15f, techo = 1.7f;
        var u = new GameObject("Ultracongelador").transform;
        u.SetParent(zona, false);

        var cuerpo = MM.Grupo(u, "Cuerpo");
        MM.Caja(cuerpo, "Zocalo", new Vector3(0f, base_ / 2f, 0f), new Vector3(w, base_, d), M["Blanco"], true);
        MM.Caja(cuerpo, "Motor", new Vector3(0f, (techo + h) / 2f, 0f), new Vector3(w, h - techo, d), M["Blanco"], true);
        MM.Caja(cuerpo, "Fondo", new Vector3(0f, h / 2f, -d / 2f + 0.03f), new Vector3(w, h, 0.06f), M["Blanco"], true);
        MM.Caja(cuerpo, "LadoI", new Vector3(-w / 2f + 0.03f, h / 2f, 0f), new Vector3(0.06f, h, d), M["Blanco"], true);
        MM.Caja(cuerpo, "LadoD", new Vector3(w / 2f - 0.03f, h / 2f, 0f), new Vector3(0.06f, h, d), M["Blanco"], true);
        // Interior escarchado y luz fría.
        float alto = techo - base_;
        MM.Caja(cuerpo, "InteriorFondo", new Vector3(0f, base_ + alto / 2f, -d / 2f + 0.065f), new Vector3(w - 0.12f, alto, 0.01f), Escarcha, false);
        MM.Caja(cuerpo, "InteriorI", new Vector3(-w / 2f + 0.065f, base_ + alto / 2f, 0f), new Vector3(0.01f, alto, d - 0.1f), Escarcha, false);
        MM.Caja(cuerpo, "InteriorD", new Vector3(w / 2f - 0.065f, base_ + alto / 2f, 0f), new Vector3(0.01f, alto, d - 0.1f), Escarcha, false);
        MM.Caja(cuerpo, "LuzFria", new Vector3(0f, techo - 0.015f, d / 2f - 0.08f), new Vector3(w - 0.14f, 0.012f, 0.025f), M["EmisivoAzul"], false);
        // Pantalla y rótulo.
        MM.Caja(cuerpo, "Display", new Vector3(0f, h - 0.1f, d / 2f + 0.005f), new Vector3(0.36f, 0.11f, 0.01f), M["PanelHUD"], false);
        var pt = MM.Texto3D(cuerpo, "-80 °C", new Vector3(0f, h - 0.1f, d / 2f + 0.012f), Quaternion.Euler(0f, 180f, 0f), 0.36f, new Color(0.4f, 0.85f, 1f), 0.34f);
        pt.alignment = TextAlignmentOptions.Center; pt.fontStyle = FontStyles.Bold;
        var rt = MM.Texto3D(cuerpo, "ULTRACONGELADOR", new Vector3(0f, h + 0.08f, d / 2f - 0.1f), Quaternion.Euler(0f, 180f, 0f), 0.3f, new Color(0.4f, 0.9f, 1f), w + 0.4f);
        rt.alignment = TextAlignmentOptions.Center; rt.fontStyle = FontStyles.Bold;
        MM.Senal(cuerpo, "Biohazard", u.TransformPoint(new Vector3(-w / 2f - 0.005f, 1.3f, 0.05f)), u.rotation * Quaternion.Euler(0f, 90f, 0f), "RIESGO\nBIOLÓGICO", 0.18f);

        // Baldas de rejilla cromada (de arriba abajo).
        float[] baldas = { 1.32f, 0.93f, 0.54f };
        foreach (var y in baldas) MM.Caja(cuerpo, "Balda", new Vector3(0f, y - 0.0075f, 0f), new Vector3(w - 0.13f, 0.015f, d - 0.12f), M["Cromo"], true);
        MC.FijoPrimitiva(cuerpo.gameObject);

        // Puerta de vidrio con bisagra a la izquierda (vista desde delante: -x local).
        var bis = new GameObject("BisagraPuerta").transform;
        bis.SetParent(u, false);
        bis.localPosition = new Vector3(-w / 2f, 0f, d / 2f + 0.015f);
        float py = (base_ + techo) / 2f, ph = techo - base_ + 0.04f;
        MM.Caja(bis, "MarcoS", new Vector3(w / 2f, py + ph / 2f - 0.03f, 0f), new Vector3(w, 0.06f, 0.03f), M["Blanco"], true);
        MM.Caja(bis, "MarcoI", new Vector3(w / 2f, py - ph / 2f + 0.03f, 0f), new Vector3(w, 0.06f, 0.03f), M["Blanco"], true);
        MM.Caja(bis, "MarcoIzq", new Vector3(0.025f, py, 0f), new Vector3(0.05f, ph, 0.03f), M["Blanco"], true);
        MM.Caja(bis, "MarcoDer", new Vector3(w - 0.025f, py, 0f), new Vector3(0.05f, ph, 0.03f), M["Blanco"], true);
        MM.Caja(bis, "Vidrio", new Vector3(w / 2f, py, 0f), new Vector3(w - 0.1f, ph - 0.12f, 0.01f), M["Vidrio"], true);
        MM.Caja(bis, "Tirador", new Vector3(w - 0.07f, py, 0.035f), new Vector3(0.025f, 0.4f, 0.025f), M["Cromo"], false);
        var pv = MM.Texto3D(bis, "CRIOPRESERVACIÓN · VIRUS", new Vector3(w / 2f, py + ph / 2f - 0.03f, 0.017f), Quaternion.Euler(0f, 180f, 0f), 0.18f, new Color(0.1f, 0.3f, 0.5f), w - 0.1f);
        pv.alignment = TextAlignmentOptions.Center; pv.fontStyle = FontStyles.Bold;
        var comp = bis.gameObject.AddComponent<Compartimento>();
        comp.tipo = Compartimento.Tipo.Bisagra;
        comp.eje = Vector3.up;
        comp.angulo = -100f;
        comp.nombre = "el ultracongelador";
        comp.avisoAlAbrir = "Ultracongelador a -80 °C: criotubos de virus.\nToca uno con un portaobjetos para tomar la muestra y devuélvelo a su hueco.";

        // Interior: gradilla con los virus (balda de arriba), gradilla vacía para guardar criotubos
        // (balda central) y huecos para placas de cultivo (balda de abajo).
        var interior = MM.Grupo(u, "Interior");
        for (int i = 0; i < 2; i++)
        {
            float y = baldas[i];
            MM.Caja(interior, "Gradilla", new Vector3(0f, y + 0.015f, 0.17f), new Vector3(w - 0.18f, 0.03f, 0.08f), M["PlasticoAzul"], true);
            var hs = Huecos(new Espacio { padre = interior, comp = comp, centro = new Vector3(0f, y + 0.03f, 0.17f), area = new Vector2(w - 0.2f, 0.08f) }, PasoTubo, 8);
            if (i == 0) Llenar(hs, virus.Select(v => Criotubo(v, "Criotubo")));
        }
        Huecos(new Espacio { padre = interior, comp = comp, centro = new Vector3(0f, baldas[2], 0.06f), area = new Vector2(w - 0.2f, 0.27f) }, PasoPlaca, 8);
    }

    // Criotanque de nitrógeno líquido (Ø 56 cm): tapa con bisagra trasera y las cepas bacterianas.
    static void Criotanque(Transform zona, Vector3 pos, MuestraData[] bacterias)
    {
        const float alto = 0.62f;
        var c = new GameObject("Criotanque").transform;
        c.SetParent(zona, false);
        c.localPosition = pos;

        var cuerpo = MM.Grupo(c, "Cuerpo");
        MM.Cilindro(cuerpo, "Tanque", new Vector3(0f, alto / 2f, 0f), new Vector3(0.56f, alto / 2f, 0.56f), Quaternion.identity, M["Cromo"]);
        MM.Cilindro(cuerpo, "Cuello", new Vector3(0f, alto + 0.04f, 0f), new Vector3(0.46f, 0.04f, 0.46f), Quaternion.identity, M["Cromo"]);
        MM.Cilindro(cuerpo, "Plato", new Vector3(0f, alto + 0.001f, 0f), new Vector3(0.42f, 0.001f, 0.42f), Quaternion.identity, Escarcha);
        var col = cuerpo.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, alto / 2f, 0f); col.size = new Vector3(0.5f, alto, 0.5f);
        MM.Caja(cuerpo, "Etiqueta", new Vector3(0f, 0.42f, 0.282f), new Vector3(0.3f, 0.12f, 0.005f), M["BlancoMate"], false);
        var et = MM.Texto3D(cuerpo, "N<sub>2</sub> LÍQUIDO\n-196 °C", new Vector3(0f, 0.42f, 0.286f), Quaternion.Euler(0f, 180f, 0f), 0.2f, new Color(0.1f, 0.25f, 0.6f), 0.28f);
        et.alignment = TextAlignmentOptions.Center; et.fontStyle = FontStyles.Bold;
        var rt = MM.Texto3D(cuerpo, "CRIOTANQUE · CEPAS BACTERIANAS", new Vector3(0f, 1.05f, 0f), Quaternion.identity, 0.26f, new Color(0.4f, 0.9f, 1f), 0.9f);
        rt.alignment = TextAlignmentOptions.Center; rt.fontStyle = FontStyles.Bold;
        rt.gameObject.AddComponent<Cartel>();
        MC.FijoPrimitiva(cuerpo.gameObject);

        // Tapa: bisagra en el borde trasero (-z); al abrirse, el borde delantero sube.
        var bis = new GameObject("BisagraTapa").transform;
        bis.SetParent(c, false);
        bis.localPosition = new Vector3(0f, alto + 0.08f, -0.24f);
        var tapa = MM.Cilindro(bis, "Tapa", new Vector3(0f, 0.015f, 0.24f), new Vector3(0.5f, 0.015f, 0.5f), Quaternion.identity, M["PlasticoAzul"]);
        tapa.AddComponent<BoxCollider>();
        MM.Cilindro(bis, "Pomo", new Vector3(0f, 0.045f, 0.24f), new Vector3(0.08f, 0.02f, 0.08f), Quaternion.identity, M["Negro"]);
        var comp = bis.gameObject.AddComponent<Compartimento>();
        comp.tipo = Compartimento.Tipo.Bisagra;
        comp.eje = Vector3.right;
        comp.angulo = -100f;
        comp.nombre = "el criotanque";
        comp.avisoAlAbrir = "Criotanque de nitrógeno líquido (-196 °C): cepas bacterianas.\nTócalas con un portaobjetos para tomar la muestra.";

        var hs = Huecos(new Espacio { padre = c, comp = comp, centro = new Vector3(0f, alto + 0.002f, 0f), area = new Vector2(0.27f, 0.18f) }, new Vector2(0.09f, 0.09f), 6);
        Llenar(hs, bacterias.Select(b => Criotubo(b, "Cepa")));
    }

    static Material Escarcha => M.TryGetValue("Escarcha", out var m) ? m : MM.Mat("Escarcha", Shader.Find("Standard"), new Color(0.86f, 0.93f, 1f), 0f, 0.35f);
}
