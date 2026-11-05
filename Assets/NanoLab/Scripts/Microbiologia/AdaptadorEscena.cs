using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Ajustes de la escena que se aplican al empezar, para que funcionen aunque la escena guardada sea anterior
// a ellos (sin tener que volver a montarla ni hornear la luz). Lo añade JugadorNanoLab. Cada paso solo
// actúa si hace falta:
//  1) quita la cristalería de química (vasos, matraces, probetas, tubos de ensayo…) y el portal a Química:
//     NanoLab VR es de microbiología. En los huecos de los cajones y vitrinas pone placas de Petri estériles;
//  2) si la entrada del Centro de Experimentación no tiene puerta, monta la corredera automática de dos hojas;
//  3) si la cámara experimental no tiene compuerta, monta la de guillotina;
//  4) tableros y carteles: fondo de cristal claro (no paneles negros) con el texto contorneado para leerse.
public class AdaptadorEscena : MonoBehaviour
{
    static readonly string[] Quimica =
    {
        "erlenmeyer", "beaker", "florence", "test_tube", "spirit_lamp", "crucible", "funnel", "boiling_liquid",
        "graduated_cylinder", "glass support", "stand glass", "china dish", "analytical", "lab glass",
    };

    // Constantes de MontarCentroExperimentacion (entrada al centro en la pared oeste).
    const float ParedX0 = 6.63f, ParedX1 = 6.87f, PuertaZ0 = 7.3f, PuertaZ1 = 8.9f, AltoPuerta = 2.25f, Grosor = 0.25f;

    // Paneles: azul pizarra suave (ni negro ni cristal blanco): el texto claro se lee bien y se ve el laboratorio detrás.
    // Contraste pensado para la vista: fondo azul profundo casi opaco (luminancia ~0.03) frente a texto
    // blanco (contraste > 12:1, WCAG AAA) y amarillo/cian de los resaltados (> 9:1). Se distingue bien
    // del laboratorio blanco y no es negro. Borde cian brillante y grueso para que el panel destaque.
    public static readonly Color ColorPanel = new Color(0.06f, 0.19f, 0.34f, 0.97f);
    public static readonly Color BordePanel = new Color(0.4f, 0.88f, 1f, 1f);

    readonly Dictionary<string, Material> materiales = new Dictionary<string, Material>();
    void Awake()
    {
        IndexarMateriales();
        int q = QuitarQuimica();
        PuertaCentro();
        foreach (var cam in FindObjectsOfType<CamaraExperimental>(true)) if (cam.compuerta == null) Compuerta(cam);
        int t = Tableros();
        int c = Cajones();
        if (c > 0) Debug.Log($"[NanoLab] Cajones que se pueden abrir y jalar añadidos: {c}.");
        Debug.Log($"[NanoLab] Escena adaptada: {q} objetos de química quitados, {t} tableros de cristal.");
    }

    // 5) Centro de Experimentación: oculta el reto anterior de bacterias y monta el único reto, el LABORATORIO DE
    //    VACUNAS (CASO 01 · Crea la vacuna). En Start, cuando el RetoManager ya existe; si algo falla, el resto del
    //    laboratorio sigue funcionando.
    void Start()
    {
        try { MontajeVacuna.Asegurar(); }
        catch (System.Exception e) { Debug.LogException(e); }
        int t = TextosAlFrente();
        if (t > 0) Debug.Log($"[NanoLab] Textos puestos por delante de sus paneles: {t}.");
    }

    // 6) Los paneles de los tableros son transparentes (RoundedBoxUnlit / cristal, sin ZWrite) igual que el texto
    //    TextMeshPro: Unity los ordena por la distancia a la cámara y, según desde dónde se mire, dibujaba el panel
    //    ENCIMA del texto (letras tenues o que desaparecen al alejarse). Con un sortingOrder mayor, el texto se
    //    dibuja siempre después de su panel. (AvisoInicio y GuiaReto ya lo hacían así.)
    public const int OrdenTexto = 5;

    public static int TextosAlFrente()
    {
        int n = 0;
        foreach (var t in FindObjectsOfType<TextMeshPro>(true))
            if (t.sortingOrder < OrdenTexto) { t.sortingOrder = OrdenTexto; n++; }
        return n;
    }

    // ------------------------------------------------------------------ materiales de la escena

    void IndexarMateriales()
    {
        foreach (var r in FindObjectsOfType<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                string n = m.name.Replace(" (Instance)", "");
                if (!materiales.ContainsKey(n)) materiales[n] = m;
            }
    }

    Material Mat(string nombre, Color respaldo)
    {
        if (materiales.TryGetValue(nombre, out var m)) return m;
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        m = new Material(sh) { name = nombre, color = respaldo };
        materiales[nombre] = m;
        return m;
    }

    // ------------------------------------------------------------------ 1) química

    int QuitarQuimica()
    {
        GameObject plantilla = null;
        foreach (var t in FindObjectsOfType<Transform>(true))
            if (t.name == "PlacaPetriEsteril") { plantilla = t.gameObject; break; }

        var quitar = new List<GameObject>();
        foreach (var t in FindObjectsOfType<Transform>(true))
        {
            string n = t.name.ToLowerInvariant();
            if (n == "portal" || n.StartsWith("portal_") || (n == "area_5" && t.parent != null && t.parent.name == "AreasLaboratorio"))
            { quitar.Add(t.gameObject); continue; }
            if (!EsQuimica(n) || (t.parent != null && EsQuimica(t.parent.name.ToLowerInvariant()))) continue;
            // Nunca piezas de NanoLab (muestras, portaobjetos, microscopio) ni objetos grandes (muebles).
            if (t.GetComponentInChildren<FuenteMuestra>(true) != null || t.GetComponentInChildren<Portaobjetos>(true) != null) continue;
            if (Tamano(t) > 0.6f) continue;
            quitar.Add(t.gameObject);
        }
        foreach (var go in quitar)
        {
            if (go == null) continue;
            var hueco = go.transform.parent;
            go.SetActive(false);
            Destroy(go);
            if (plantilla != null && hueco != null && hueco.name.StartsWith("Hueco"))
            {
                var placa = Instantiate(plantilla, hueco, false);
                placa.name = "PlacaPetriEsteril";
                placa.transform.localPosition = Vector3.zero;
                placa.transform.localRotation = Quaternion.identity;
                placa.SetActive(true);
            }
        }
        return quitar.Count;
    }

    static bool EsQuimica(string n)
    {
        n = n.Replace('_', ' ');
        foreach (var a in Quimica) if (n.Contains(a.Replace('_', ' '))) return true;
        return false;
    }

    static float Tamano(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return 0f;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.size.magnitude;
    }

    // ------------------------------------------------------------------ 2) puerta del centro

    void PuertaCentro()
    {
        if (FindObjectOfType<PuertaAutomatica>(true) != null) return;
        Transform grupo = null;
        foreach (var t in FindObjectsOfType<Transform>(true))
            if (t.name == "Puerta" && t.parent != null && t.parent.name == "CentroExperimentacion") { grupo = t; break; }
        if (grupo == null) return;
        var umbral = grupo.Find("Umbral");
        float suelo = umbral != null ? umbral.position.y - 0.004f : 0f;

        var raiz = new GameObject("PuertaAutomatica");
        raiz.SetActive(false);   // Awake de los componentes cuando ya tienen sus referencias
        raiz.transform.SetParent(grupo, false);
        raiz.transform.SetPositionAndRotation(new Vector3((ParedX0 + ParedX1) / 2f, suelo, (PuertaZ0 + PuertaZ1) / 2f), Quaternion.identity);
        var auto = raiz.AddComponent<PuertaAutomatica>();

        var vidrio = Mat("VidrioPared", new Color(0.8f, 0.92f, 1f, 0.12f));
        var marco = Mat("Marco", new Color(0.75f, 0.78f, 0.8f));
        var oscuro = Mat("Oscuro", new Color(0.05f, 0.05f, 0.06f));
        var cian = Mat("EmisivoCian", new Color(0.3f, 0.9f, 1f));
        var blanco = Mat("Blanco", Color.white);
        if (!materiales.ContainsKey("EmisivoVerde"))
        {
            // No existía en la escena: se hace a partir del cian (mismo shader emisivo).
            var v = new Material(cian) { name = "EmisivoVerde", color = new Color(0.25f, 1f, 0.45f) };
            if (v.HasProperty("_EmissionColor")) v.SetColor("_EmissionColor", new Color(0.25f, 1f, 0.45f) * 1.8f);
            materiales["EmisivoVerde"] = v;
        }
        var verde = materiales["EmisivoVerde"];

        float w = (PuertaZ1 - PuertaZ0) / 2f + 0.02f;
        float h = AltoPuerta - 0.02f;
        var hojas = new List<PuertaCorrediza>();
        foreach (int lado in new[] { -1, 1 })
        {
            var pivote = new GameObject(lado < 0 ? "Hoja_Sur" : "Hoja_Norte").transform;
            pivote.SetParent(raiz.transform, false);
            var pc = pivote.gameObject.AddComponent<PuertaCorrediza>();
            var hoja = new GameObject("Hoja").transform;
            hoja.SetParent(pivote, false);
            Caja(hoja, "Vidrio", new Vector3(0f, h / 2f, 0f), new Vector3(0.025f, h, w), vidrio, true);
            Caja(hoja, "MarcoSup", new Vector3(0f, h - 0.03f, 0f), new Vector3(0.045f, 0.06f, w), marco);
            Caja(hoja, "MarcoInf", new Vector3(0f, 0.07f, 0f), new Vector3(0.045f, 0.14f, w), marco);
            Caja(hoja, "MarcoExt", new Vector3(0f, h / 2f, lado * (w / 2f - 0.03f)), new Vector3(0.045f, h, 0.06f), marco);
            Caja(hoja, "MarcoInt", new Vector3(0f, h / 2f, -lado * (w / 2f - 0.02f)), new Vector3(0.045f, h, 0.04f), oscuro);
            Caja(hoja, "Franja", new Vector3(0f, 1.05f, 0f), new Vector3(0.047f, 0.05f, w - 0.1f), cian);
            Caja(hoja, "Esmerilado", new Vector3(0f, 1.45f, 0f), new Vector3(0.027f, 0.5f, w - 0.1f), blanco);
            var rb = hoja.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            pc.hoja = hoja;
            pc.cerrada = new Vector3(0f, 0f, lado * w / 2f);
            pc.abierta = new Vector3(0f, 0f, lado * (w / 2f + w + 0.02f));   // se esconde dentro del muro
            pc.duracion = 0.9f;
            pc.sonido = lado < 0;
            hoja.localPosition = pc.cerrada;
            hojas.Add(pc);
        }
        auto.hojas = hojas.ToArray();
        auto.zona = new Vector3(1.7f, 1.5f, 1.3f);

        var abierta = new List<GameObject>();
        var cerrada = new List<GameObject>();
        foreach (int cara in new[] { -1, 1 })
        {
            float x = cara * (Grosor / 2f + 0.03f);
            var tam = new Vector3(0.02f, 0.04f, PuertaZ1 - PuertaZ0 - 0.2f);
            abierta.Add(Caja(raiz.transform, "LuzAbierta", new Vector3(x, AltoPuerta + 0.06f, 0f), tam, verde));
            cerrada.Add(Caja(raiz.transform, "LuzCerrada", new Vector3(x, AltoPuerta + 0.06f, 0f), tam, oscuro));
        }
        auto.luzAbierta = abierta.ToArray();
        auto.luzCerrada = cerrada.ToArray();
        foreach (var l in abierta) l.SetActive(false);
        raiz.SetActive(true);
        Debug.Log("[NanoLab] Puerta corredera automática montada en la entrada del Centro de Experimentación.");
    }

    // ------------------------------------------------------------------ 3) compuerta de la cámara

    void Compuerta(CamaraExperimental cam)
    {
        const float ancho = 0.4f, alto = 0.26f, sube = 0.3f;
        var vieja = cam.transform.Find("Compuerta");
        if (vieja != null) Destroy(vieja.gameObject);
        var c = new GameObject("Compuerta").transform;
        c.SetParent(cam.transform, false);
        c.localPosition = new Vector3(0f, 0.925f, 0.68f);
        var marco = Mat("Marco", new Color(0.75f, 0.78f, 0.8f));
        foreach (float x in new[] { -ancho / 2f - 0.02f, ancho / 2f + 0.02f })
            Caja(c, "Guia", new Vector3(x, (alto + sube) / 2f, 0f), new Vector3(0.03f, alto + sube, 0.05f), marco);
        Caja(c, "Carcasa", new Vector3(0f, alto + sube / 2f + 0.01f, 0.012f), new Vector3(ancho + 0.07f, sube + 0.02f, 0.028f), Mat("Blanco", Color.white));
        var luz = Caja(c, "LuzCompuerta", new Vector3(0f, alto + 0.025f, 0.028f), new Vector3(ancho + 0.02f, 0.012f, 0.006f), Mat("EmisivoCian", new Color(0.3f, 0.9f, 1f)));
        var hoja = new GameObject("Hoja").transform;
        hoja.SetParent(c, false);
        Caja(hoja, "Vidrio", new Vector3(0f, alto / 2f, -0.006f), new Vector3(ancho, alto, 0.012f), Mat("VidrioPared", new Color(0.8f, 0.92f, 1f, 0.12f)));
        Caja(hoja, "Canto", new Vector3(0f, 0.012f, -0.006f), new Vector3(ancho, 0.024f, 0.02f), marco);
        Caja(hoja, "Junta", new Vector3(0f, 0.002f, -0.006f), new Vector3(ancho, 0.004f, 0.022f), Mat("Oscuro", Color.black));
        Caja(hoja, "Franja", new Vector3(0f, alto - 0.03f, -0.006f), new Vector3(ancho, 0.015f, 0.014f), Mat("EmisivoCian", new Color(0.3f, 0.9f, 1f)));
        cam.compuerta = hoja;
        cam.compuertaCerrada = Vector3.zero;
        cam.compuertaAbierta = new Vector3(0f, sube, 0f);
        cam.luzCompuerta = luz.GetComponent<Renderer>();
    }

    static GameObject Caja(Transform padre, string nombre, Vector3 pos, Vector3 tam, Material m, bool collider = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        if (!collider) Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localScale = tam;
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    // ------------------------------------------------------------------ 5) todos los cajones accesibles

    // Mallas de "table with drawers": cajones de arriba abajo y armazón (como MontarAlmacenamiento).
    static readonly string[] NombresCajon = { "pCube22", "pCube186", "pCube187" };
    const string ArmazonMesa = "pCube12";

    // Las mesas con cajones que el montaje no convirtió (las del Centro y las que tenían algo delante)
    // reciben cajones que se abren con el rayo y se jalan con la mano. Si la pieza forma parte del lote
    // estático (no se puede mover), se oculta y se pone en su sitio una copia móvil con la misma malla
    // (tomada de un cajón ya convertido) y la misma luz horneada.
    int Cajones()
    {
        var mallas = new Dictionary<string, Mesh>();
        foreach (var comp in FindObjectsOfType<Compartimento>(true))
        {
            var mf = comp.GetComponent<MeshFilter>();
            var r = comp.GetComponent<MeshRenderer>();
            if (mf == null || mf.sharedMesh == null || (r != null && r.isPartOfStaticBatch)) continue;
            string n = comp.name.Replace("_Movil", "");
            if (System.Array.IndexOf(NombresCajon, n) >= 0 && !mallas.ContainsKey(n)) mallas[n] = mf.sharedMesh;
        }

        int hechos = 0;
        foreach (var mesa in FindObjectsOfType<Transform>(true))
        {
            if (mesa.Find(ArmazonMesa) == null || mesa.Find(NombresCajon[0]) == null) continue;
            foreach (var n in NombresCajon)
            {
                var c = mesa.Find(n);
                if (c == null || c.GetComponent<Compartimento>() != null || c.parent.Find(n + "_Movil") != null) continue;
                var r = c.GetComponent<MeshRenderer>();
                var mf = c.GetComponent<MeshFilter>();
                if (r == null || mf == null) continue;

                Transform movil;
                Mesh malla;
                if (!r.isPartOfStaticBatch && mf.sharedMesh != null) { movil = c; malla = mf.sharedMesh; }
                else
                {
                    if (!mallas.TryGetValue(n, out malla)) continue;   // sin malla original: no se puede copiar
                    movil = new GameObject(n + "_Movil").transform;
                    movil.SetParent(c.parent, false);
                    movil.localPosition = c.localPosition;
                    movil.localRotation = c.localRotation;
                    movil.localScale = c.localScale;
                    movil.gameObject.layer = c.gameObject.layer;
                    movil.gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
                    var nr = movil.gameObject.AddComponent<MeshRenderer>();
                    nr.sharedMaterials = r.sharedMaterials;
                    nr.lightmapIndex = r.lightmapIndex;
                    nr.lightmapScaleOffset = r.lightmapScaleOffset;
                    nr.shadowCastingMode = r.shadowCastingMode;
                    nr.receiveShadows = r.receiveShadows;
                    r.enabled = false;
                    // Los objetos que estaban dentro del cajón se mueven con la copia.
                    for (int i = c.childCount - 1; i >= 0; i--) c.GetChild(i).SetParent(movil, true);
                }
                foreach (var col in c.GetComponents<Collider>()) Destroy(col);
                if (movil != c) foreach (var col in movil.GetComponents<Collider>()) Destroy(col);

                // Fondo (donde se apoya lo que se guarda) y frente (lo que encuentra el rayo), como en el montaje.
                var b = malla.bounds;
                var fondo = movil.gameObject.AddComponent<BoxCollider>();
                fondo.center = new Vector3(b.center.x, b.min.y + 0.01f, b.center.z);
                fondo.size = new Vector3(b.size.x, 0.02f, b.size.z);
                var tapa = movil.gameObject.AddComponent<BoxCollider>();
                tapa.center = new Vector3(b.center.x, b.center.y, b.max.z - 0.01f);
                tapa.size = new Vector3(b.size.x, b.size.y, 0.02f);

                // Recorrido: el 60 % del fondo del cajón, o menos si hay algo delante (otra mesa, una pared).
                var dirMundo = movil.TransformVector(new Vector3(0f, 0f, b.size.z)).normalized;
                float profundidad = movil.TransformVector(new Vector3(0f, 0f, b.size.z)).magnitude;
                float largo = Mathf.Min(profundidad * 0.6f, Libre(mesa, movil, b, dirMundo) - 0.03f);
                largo = Mathf.Max(largo, Mathf.Min(0.15f, profundidad * 0.6f));
                var comp = movil.gameObject.AddComponent<Compartimento>();
                comp.tipo = Compartimento.Tipo.Corredera;
                comp.recorrido = movil.parent.InverseTransformVector(dirMundo * largo);
                comp.nombre = "el cajón";
                hechos++;
            }
        }
        return hechos;
    }

    // Espacio libre (m) delante del frente del cajón hasta el primer obstáculo que no sea la propia mesa.
    static float Libre(Transform mesa, Transform cajon, Bounds b, Vector3 dir)
    {
        var frente = cajon.TransformPoint(new Vector3(b.center.x, b.center.y, b.max.z));
        var medio = Vector3.Scale(new Vector3(b.extents.x - 0.02f, b.extents.y - 0.02f, 0.01f), Abs(cajon.lossyScale));
        float libre = 1f;
        foreach (var h in Physics.BoxCastAll(frente - dir * 0.02f, medio, dir, cajon.rotation, 1f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(mesa) || h.collider.attachedRigidbody != null) continue;
            if (h.collider.GetComponentInParent<Silla>() != null) continue;   // las sillas se apartan
            if (h.distance > 0f) libre = Mathf.Min(libre, h.distance);
        }
        return libre;
    }

    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    // ------------------------------------------------------------------ 4) tableros de cristal

    // Fondos oscuros de tableros, carteles y ventanas → cristal claro con borde cian; los textos claros que
    // tienen encima llevan contorno oscuro para leerse con el laboratorio detrás. El visor del microscopio y
    // los avisos no se tocan (ya tienen su propio fondo / contorno).
    int Tableros()
    {
        int n = 0;
        var fondos = new List<Transform>();
        foreach (var p in FindObjectsOfType<PanelRedondeado>(true))
        {
            var c = p.color;
            bool oscuro = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) < 0.2f && c.a > 0.5f;
            bool pequeno = p.ancho * p.alto < 0.01f;   // ojos de Baymax, separadores, puntos…
            if (!oscuro || pequeno || EsBoton(p.transform)) continue;
            p.color = ColorPanel;
            p.colorBorde = BordePanel;
            p.borde = Mathf.Max(p.borde, 0.006f);
            p.Aplicar();
            fondos.Add(p.transform.parent != null ? p.transform.parent : p.transform);
            n++;
        }
        Material cristal = null;
        foreach (var r in FindObjectsOfType<MeshRenderer>(true))
        {
            var m = r.sharedMaterial;
            if (m == null || !m.name.StartsWith("PanelHUD")) continue;
            if (cristal == null) cristal = CristalTablero(m);
            if (cristal == null) break;
            r.sharedMaterial = cristal;
            fondos.Add(r.transform.parent != null ? r.transform.parent : r.transform);
            n++;
        }
        var hechos = new HashSet<TMP_Text>();
        foreach (var f in fondos)
            foreach (var t in f.GetComponentsInChildren<TMP_Text>(true))
                if (hechos.Add(t)) Contorno(t);
        return n;
    }

    static bool EsBoton(Transform t)
    {
        for (var x = t; x != null; x = x.parent)
        {
            string n = x.name.ToLowerInvariant();
            if (n.Contains("boton") || n.Contains("botón") || n.Contains("button")) return true;
            if (x.GetComponent<Collider>() != null && x != t) return false;
        }
        return false;
    }

    // Tableros de caja: el mismo material sin iluminación (Unlit/Color) con el azul de los paneles, para que
    // se vean igual de claros en toda la sala, sin reflejos ni sombras que apaguen las letras.
    static Material CristalTablero(Material original)
    {
        var m = new Material(original) { name = "TableroAzul" };
        var c = ColorPanel; c.a = 1f;
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        return m;
    }

    // Un material contorneado compartido por fuente (no una instancia por texto).
    static readonly Dictionary<Material, Material> contorneados = new Dictionary<Material, Material>();

    static void Contorno(TMP_Text t)
    {
        var c = t.color;
        if (c.r * 0.3f + c.g * 0.59f + c.b * 0.11f < 0.45f) return;   // texto oscuro: ya contrasta con el cristal
        var baseMat = t.fontSharedMaterial;
        if (baseMat == null || !baseMat.HasProperty(ShaderUtilities.ID_OutlineWidth)) return;
        if (baseMat.GetFloat(ShaderUtilities.ID_OutlineWidth) > 0.05f) return;
        if (!contorneados.TryGetValue(baseMat, out var m))
        {
            m = new Material(baseMat) { name = baseMat.name + " Contorno" };
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
            m.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0.06f, 0.12f, 0.9f));
            m.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
            contorneados[baseMat] = m;
        }
        t.fontSharedMaterial = m;
    }
}
