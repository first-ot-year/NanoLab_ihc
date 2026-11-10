using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Utilidades para montar la estación de la vacuna EN TIEMPO DE EJECUCIÓN (sin volver a montar la escena ni
// hornear la luz): piezas primitivas sin collider, materiales copiados de los que ya usa la escena (así sus
// shaders están incluidos en la build), textos TextMeshPro con la fuente de la escena y paneles redondeados.
public static class KitVacuna
{
    static readonly Dictionary<string, Material> escena = new Dictionary<string, Material>();
    static readonly Dictionary<Color, Material> planos = new Dictionary<Color, Material>();
    static readonly Dictionary<Color, Material> vidrios = new Dictionary<Color, Material>();
    static readonly Dictionary<PrimitiveType, Mesh> mallas = new Dictionary<PrimitiveType, Mesh>();
    static Material matPanel;
    static TMP_FontAsset fuente;
    static bool indexado;
    static MaterialPropertyBlock mpb;

    public static readonly Color Cian = new Color(0.4f, 0.88f, 1f);
    public static readonly Color Verde = new Color(0.35f, 1f, 0.55f);
    public static readonly Color Rojo = new Color(1f, 0.45f, 0.38f);
    public static readonly Color Amarillo = new Color(1f, 0.82f, 0.35f);
    public static readonly Color Gris = new Color(0.66f, 0.75f, 0.85f);

    public static void Indexar()
    {
        if (indexado) return;
        indexado = true;
        foreach (var r in Object.FindObjectsOfType<Renderer>(true))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                string n = m.name.Replace(" (Instance)", "");
                if (!escena.ContainsKey(n)) escena[n] = m;
            }
        var ejemplo = Object.FindObjectOfType<PanelRedondeado>(true);
        if (ejemplo != null) matPanel = ejemplo.GetComponent<MeshRenderer>().sharedMaterial;
        if (matPanel == null)
        {
            var sh = Shader.Find("Interaction/RoundedBoxUnlit");
            if (sh != null) matPanel = new Material(sh);
        }
        foreach (var tx in Object.FindObjectsOfType<TextMeshPro>(true)) if (tx.font != null) { fuente = tx.font; break; }
    }

    public static Material Escena(string nombre, Color respaldo)
    {
        Indexar();
        if (escena.TryGetValue(nombre, out var m)) return m;
        var sh = Shader.Find("Standard");
        m = new Material(sh) { name = nombre, color = respaldo };
        escena[nombre] = m;
        return m;
    }

    // Color plano sin iluminación (pantallas, iconos, diagramas): se ve igual con cualquier luz.
    public static Material Plano(Color c)
    {
        if (planos.TryGetValue(c, out var m)) return m;
        Indexar();
        Material b = escena.TryGetValue("BlancoMate", out var bm) ? bm : null;
        m = b != null ? new Material(b) : new Material(Shader.Find("Unlit/Color"));
        m.name = "VacunaPlano";
        m.color = c;
        planos[c] = m;
        return m;
    }

    // Material transparente (envolturas del virus, nanopartículas). Sin vidrio en la escena, color plano.
    public static Material Vidrio(Color c)
    {
        if (vidrios.TryGetValue(c, out var m)) return m;
        Indexar();
        Material b = escena.TryGetValue("Vidrio", out var v) ? v : escena.TryGetValue("VidrioReal", out var v2) ? v2 : null;
        m = b != null ? new Material(b) : Plano(new Color(c.r, c.g, c.b, 1f));
        if (b != null) { m.name = "VacunaVidrio"; m.color = c; }
        vidrios[c] = m;
        return m;
    }

    static Mesh Malla(PrimitiveType t)
    {
        if (mallas.TryGetValue(t, out var m) && m != null) return m;
        var go = GameObject.CreatePrimitive(t);
        m = go.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
        mallas[t] = m;
        return m;
    }

    public static Transform Grupo(Transform padre, string nombre, Vector3 pos, Quaternion rot)
    {
        var t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = pos;
        t.localRotation = rot;
        return t;
    }

    public static Transform Grupo(Transform padre, string nombre, Vector3 pos) => Grupo(padre, nombre, pos, Quaternion.identity);

    public static Renderer Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Material m, Quaternion? rot = null, bool sombras = false)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = escala;
        go.AddComponent<MeshFilter>().sharedMesh = Malla(tipo);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = sombras ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = sombras;
        return r;
    }

    // Une muchas piezas con el mismo material en una sola malla (una llamada de dibujo; RNF-08) y borra las originales.
    public static Renderer Combinar(Transform padre, string nombre, IList<Renderer> piezas)
    {
        if (piezas == null || piezas.Count == 0) return null;
        var partes = new CombineInstance[piezas.Count];
        var aLocal = padre.worldToLocalMatrix;
        for (int i = 0; i < piezas.Count; i++)
            partes[i] = new CombineInstance { mesh = piezas[i].GetComponent<MeshFilter>().sharedMesh, transform = aLocal * piezas[i].transform.localToWorldMatrix };
        var malla = new Mesh { name = nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        malla.CombineMeshes(partes, true, true);
        var r = Pieza(padre, nombre, PrimitiveType.Cube, Vector3.zero, Vector3.one, piezas[0].sharedMaterial);
        r.GetComponent<MeshFilter>().sharedMesh = malla;
        foreach (var p in piezas) { if (Application.isPlaying) Object.Destroy(p.gameObject); else Object.DestroyImmediate(p.gameObject); }
        return r;
    }

    public static Renderer Caja(Transform p, string n, Vector3 pos, Vector3 tam, Material m, bool sombras = false) => Pieza(p, n, PrimitiveType.Cube, pos, tam, m, null, sombras);

    public static Renderer Esfera(Transform p, string n, Vector3 pos, float diametro, Material m) => Pieza(p, n, PrimitiveType.Sphere, pos, Vector3.one * diametro, m);

    // Mueble macizo: frena al jugador (capa BloqueoJugador) pero no tapa los rayos de interacción.
    public static void Solido(Renderer r)
    {
        r.gameObject.AddComponent<BoxCollider>();
        r.gameObject.layer = CapasNanoLab.BloqueoJugador;
    }

    public static void Pintar(Renderer r, Color c)
    {
        if (r == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", c);
        r.SetPropertyBlock(mpb);
    }

    // Texto 3D. Se lee desde -z local (como los paneles). 'arriba' = la posición es el borde superior.
    public static TextMeshPro Texto(Transform padre, string s, Vector3 pos, float tam, Color c, float ancho, float alto,
                                    TextAlignmentOptions al = TextAlignmentOptions.Center, bool negrita = false, bool arriba = false, Quaternion? rot = null)
    {
        Indexar();
        var go = new GameObject("Texto");
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<TextMeshPro>();
        if (fuente != null) t.font = fuente;
        t.richText = true;
        t.text = s;
        t.fontSize = tam;
        t.color = c;
        t.alignment = al;
        t.enableWordWrapping = true;
        t.overflowMode = TextOverflowModes.Overflow;
        t.sortingOrder = AdaptadorEscena.OrdenTexto;   // siempre por delante de los paneles transparentes
        if (negrita) t.fontStyle = FontStyles.Bold;
        var rt = t.rectTransform;
        rt.sizeDelta = new Vector2(ancho, alto);
        if (arriba) rt.pivot = new Vector2(0.5f, 1f);
        rt.localPosition = pos;
        rt.localRotation = rot ?? Quaternion.identity;
        return t;
    }

    // Reduce el tamaño si el texto no cabe en su caja (los textos del reto cambian de longitud).
    public static void AutoTam(TMP_Text t, float min)
    {
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = min;
    }

    // Panel redondeado con borde (estilo de los paneles de Quest). Se ve desde -z local.
    public static Renderer Panel(Transform padre, string nombre, Vector3 pos, float ancho, float alto, Color color, Color borde, float grosor = 0.006f, float radio = 0.03f)
    {
        Indexar();
        if (matPanel != null)
        {
            var r = Pieza(padre, nombre, PrimitiveType.Quad, pos, Vector3.one, matPanel);
            var p = r.gameObject.AddComponent<PanelRedondeado>();
            p.ancho = ancho; p.alto = alto; p.radio = radio; p.color = color; p.colorBorde = borde; p.borde = grosor;
            p.Aplicar();
            return r;
        }
        // Respaldo: dos placas planas (borde detrás).
        if (grosor > 0f) Pieza(padre, nombre + "_Borde", PrimitiveType.Quad, pos + new Vector3(0f, 0f, 0.001f), new Vector3(ancho + grosor * 2f, alto + grosor * 2f, 1f), Plano(new Color(borde.r, borde.g, borde.b, 1f)));
        return Pieza(padre, nombre, PrimitiveType.Quad, pos, new Vector3(ancho, alto, 1f), Plano(new Color(color.r, color.g, color.b, 1f)));
    }

    public static Quaternion MirarDesde(Vector3 haciaElUsuario, Vector3 arriba)
    {
        // Rotación de un marco cuyo -z mira al usuario (así se leen sus textos y paneles).
        return Quaternion.LookRotation(-haciaElUsuario.normalized, arriba);
    }
}
