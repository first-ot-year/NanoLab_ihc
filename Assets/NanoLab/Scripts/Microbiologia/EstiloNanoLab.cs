using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Estilo visual común de NanoLab VR:
//  - LETRA: Nunito SemiBold (redondeada, amigable y muy legible en el visor). Se crea como fuente dinámica de
//    TextMeshPro a partir del .ttf de Resources/NanoLab/Fuentes; los glifos que no tenga los busca en la fuente
//    anterior de la escena (fallback). AdaptadorEscena la aplica a todos los textos 3D al empezar.
//  - ICONOS del mando (Resources/NanoLab/Iconos): gatillo, grip, botón A/X, rayo y pulsar, para que el estudiante
//    vea qué botón usar en vez de leerlo; y siguiente/repetir para las tapas de CONTINUAR y REPETIR.
public static class EstiloNanoLab
{
    public enum Icono { Ninguno, Gatillo, Grip, Boton, Rayo, Pulsar, Siguiente, Repetir }

    static TMP_FontAsset fuente;
    static bool intentado;
    static readonly Dictionary<string, Material> contornos = new Dictionary<string, Material>();
    static readonly Dictionary<Icono, Sprite> iconos = new Dictionary<Icono, Sprite>();

    // Fuente Nunito (null si no se pudo cargar: entonces se mantiene la de la escena).
    public static TMP_FontAsset Fuente
    {
        get
        {
            if (intentado) return fuente;
            intentado = true;
            var ttf = Resources.Load<Font>("NanoLab/Fuentes/Nunito-SemiBold");
            if (ttf == null) { Debug.LogWarning("[Estilo] No se encontró Resources/NanoLab/Fuentes/Nunito-SemiBold: se mantiene la letra de la escena."); return null; }
            fuente = TMP_FontAsset.CreateFontAsset(ttf);
            if (fuente == null) return null;
            fuente.name = "Nunito SemiBold (dinámica)";
            // Respaldo para glifos que Nunito no tiene (p. ej. letras griegas): la fuente que ya usaba la escena.
            var anterior = new List<TMP_FontAsset>();
            foreach (var t in Object.FindObjectsOfType<TextMeshPro>(true))
                if (t.font != null && t.font != fuente && !anterior.Contains(t.font)) { anterior.Add(t.font); break; }
            if (TMP_Settings.defaultFontAsset != null && !anterior.Contains(TMP_Settings.defaultFontAsset)) anterior.Add(TMP_Settings.defaultFontAsset);
            fuente.fallbackFontAssetTable = anterior;
            return fuente;
        }
    }

    // Cambia la letra de un texto 3D a Nunito conservando su contorno. Devuelve true si la cambió.
    public static bool Aplicar(TMP_Text t)
    {
        var f = Fuente;
        if (f == null || t == null || t.font == f) return false;
        var mat = t.fontSharedMaterial;
        if (mat != null && mat.shader != null && mat.shader.name.Contains("Overlay")) return false;   // textos "siempre encima": se dejan
        float ancho = 0f, dilatar = 0f;
        Color color = Color.black;
        if (mat != null && mat.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            ancho = mat.GetFloat(ShaderUtilities.ID_OutlineWidth);
            color = mat.GetColor(ShaderUtilities.ID_OutlineColor);
            if (mat.HasProperty(ShaderUtilities.ID_FaceDilate)) dilatar = mat.GetFloat(ShaderUtilities.ID_FaceDilate);
        }
        t.font = f;
        if (ancho > 0.01f) t.fontSharedMaterial = Contorno(ancho, color, dilatar);
        return true;
    }

    public static int AplicarATodos()
    {
        int n = 0;
        foreach (var t in Object.FindObjectsOfType<TextMeshPro>(true)) if (Aplicar(t)) n++;
        return n;
    }

    // Material de Nunito con contorno (compartido por todos los textos con el mismo contorno).
    public static Material Contorno(float ancho, Color color, float dilatar)
    {
        string clave = $"{ancho:F2}_{ColorUtility.ToHtmlStringRGBA(color)}_{dilatar:F2}";
        if (contornos.TryGetValue(clave, out var m) && m != null) return m;
        m = new Material(Fuente.material) { name = "Nunito Contorno " + clave };
        m.SetFloat(ShaderUtilities.ID_OutlineWidth, ancho);
        m.SetColor(ShaderUtilities.ID_OutlineColor, color);
        if (m.HasProperty(ShaderUtilities.ID_FaceDilate)) m.SetFloat(ShaderUtilities.ID_FaceDilate, dilatar);
        m.EnableKeyword(ShaderUtilities.Keyword_Outline);
        contornos[clave] = m;
        return m;
    }

    public static Sprite Sprite(Icono i)
    {
        if (i == Icono.Ninguno) return null;
        if (iconos.TryGetValue(i, out var s) && s != null) return s;
        var tex = Resources.Load<Texture2D>("NanoLab/Iconos/" + i.ToString().ToLowerInvariant());
        s = tex != null ? UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width) : null;
        iconos[i] = s;
        return s;
    }

    // Icono del mando en un SpriteRenderer (1 unidad = 'tam' metros), por delante de los paneles transparentes.
    public static SpriteRenderer CrearIcono(Transform padre, string nombre, Vector3 pos, float tam, Quaternion? rot = null)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale = Vector3.one * tam;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = AdaptadorEscena.OrdenTexto + 1;
        return sr;
    }

    // Plantilla del botón "poke" oficial del Interaction SDK de Meta (bloque PokeInteractable) tomada de un botón de la
    // escena, para crear botones poke por código (el prefab del paquete solo se puede cargar en el Editor).
    static GameObject plantillaPoke;
    static bool plantillaBuscada;

    public static GameObject PlantillaPoke()
    {
        if (plantillaBuscada && plantillaPoke != null) return plantillaPoke;
        plantillaBuscada = true;
        foreach (var p in Object.FindObjectsOfType<Oculus.Interaction.PokeInteractable>(true))
        {
            var bf = p.GetComponentInParent<BotonFisico>(true);
            if (bf == null) continue;
            var t = p.transform;
            while (t.parent != null && t.parent != bf.transform) t = t.parent;
            if (t.GetComponentInChildren<Oculus.Interaction.PokeInteractableVisual>(true) == null) continue;
            plantillaPoke = t.gameObject;
            break;
        }
        if (plantillaPoke == null) Debug.LogWarning("[Estilo] No hay botones poke en la escena: se usan botones de respaldo.");
        return plantillaPoke;
    }

    // Copia del bloque poke con los colores dados (reposo, encima y pulsado). Devuelve null si no hay plantilla.
    public static GameObject CrearPoke(Transform padre, Vector3 pos, Quaternion rot, Vector3 escala, Color reposo, Color? encima = null)
    {
        var plantilla = PlantillaPoke();
        if (plantilla == null) return null;
        var p = Object.Instantiate(plantilla, padre, false);
        p.name = "Poke";
        p.SetActive(true);
        p.transform.localPosition = pos;
        p.transform.localRotation = rot;
        p.transform.localScale = escala;
        var cv = p.GetComponentInChildren<Oculus.Interaction.InteractableColorVisual>(true);
        if (cv != null)
        {
            var e = encima ?? Color.Lerp(reposo, Color.white, 0.35f); e.a = 1f; reposo.a = 1f;
            cv.InjectOptionalNormalColorState(new Oculus.Interaction.InteractableColorVisual.ColorState { Color = reposo, ColorTime = 0.15f });
            cv.InjectOptionalHoverColorState(new Oculus.Interaction.InteractableColorVisual.ColorState { Color = e, ColorTime = 0.08f });
            cv.InjectOptionalSelectColorState(new Oculus.Interaction.InteractableColorVisual.ColorState { Color = new Color(0.45f, 1f, 0.65f, 1f), ColorTime = 0.03f });
        }
        return p;
    }

    // Parte del poke que se hunde al pulsar (para poner encima la letra o el icono).
    public static Transform CaraPoke(GameObject poke) =>
        poke != null ? poke.GetComponentInChildren<Oculus.Interaction.PokeInteractableVisual>(true)?.transform : null;

    public static void Poner(SpriteRenderer sr, Icono i)
    {
        if (sr == null) return;
        sr.sprite = Sprite(i);
        sr.enabled = sr.sprite != null;
    }
}
