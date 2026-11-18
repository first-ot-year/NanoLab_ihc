using System.Linq;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// Demostración animada del SIGUIENTE GESTO en la misión 1 (pruebas con usuarios: "no es tan sencillo de entender").
// Un portaobjetos FANTASMA (holograma cian) sale del portaobjetos real, o de la bandeja si aún no tienes ninguno,
// viaja en arco hasta donde hay que tocar, "toca" con un destello y un anillo, y se desvanece. Se repite mientras
// el paso siga pendiente y estés cerca. Encima del destino, una etiqueta corta dice el gesto ("TOCA LA MUESTRA A",
// "GOTERO 2 DE 4", "PONLO EN LA PLATINA").
//  - FROTIS: portaobjetos → placa de la MUESTRA A.
//  - TINCIÓN: portaobjetos → el gotero que toca ahora (1 > 2 > 3 > 4); con la tinción no válida, al desinfectante.
//  - OBSERVAR: portaobjetos de la A (luego la B y la C) → la platina del microscopio más cercano.
// Nada sigue a la cabeza: la etiqueta se orienta una sola vez, al cambiar de destino. Se apaga con la cara en el
// ocular, fuera del centro y lejos del destino. La crea RetoManager.
public class DemoGesto : MonoBehaviour
{
    const float Ciclo = 2.4f, Pausa = 0.9f, DistanciaMax = 4.5f;
    static readonly Color Cian = new Color(0.4f, 0.9f, 1f);

    Transform fantasma, anillo, etiquetaT;
    Renderer fantasmaR;
    SpriteRenderer anilloR;
    TextMeshPro etiqueta;
    Material matFantasma;
    float t;
    Transform destinoMostrado;
    FuenteMuestra[] fuentes = new FuenteMuestra[0];
    Platina[] platinas = new Platina[0];
    float tCache;

    public static DemoGesto Instancia { get; private set; }
    public bool Visible => fantasma != null && fantasma.gameObject.activeSelf;
    public string Gesto => etiqueta != null && etiquetaT.gameObject.activeSelf ? etiqueta.text : null;

    public static DemoGesto Crear()
    {
        var d = new GameObject("DemoGesto").AddComponent<DemoGesto>();
        d.Montar();
        return d;
    }

    void Awake() => Instancia = this;
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    void Update()
    {
        if (Time.time >= tCache)
        {
            tCache = Time.time + 2f;
            fuentes = FindObjectsOfType<FuenteMuestra>();
            platinas = FindObjectsOfType<Platina>();
        }
        var cab = ManosRig.Cabeza;
        bool hay = Calcular(out var desde, out var hacia, out var texto);
        if (hay && cab != null)
        {
            var d = hacia.position - cab.position; d.y = 0f;
            if (d.magnitude > DistanciaMax) hay = false;
        }
        if (!hay || cab == null)
        {
            Mostrar(false);
            destinoMostrado = null;
            return;
        }
        Mostrar(true);
        var meta = hacia.position;
        if (destinoMostrado != hacia)
        {
            // Destino nuevo: la etiqueta se coloca encima, mirando al jugador (una vez, solo en horizontal).
            destinoMostrado = hacia;
            t = 0f;
            etiquetaT.position = meta + Vector3.up * 0.26f;
            var m = etiquetaT.position - cab.position; m.y = 0f;
            if (m.sqrMagnitude > 1e-4f) etiquetaT.rotation = Quaternion.LookRotation(m, Vector3.up);
        }
        if (etiqueta.text != texto) etiqueta.text = texto;
        etiqueta.alpha = 0.85f + 0.15f * Mathf.Sin(Time.time * 3f);

        // Ciclo: aparece en el origen → arco hasta el destino → toca (destello) → se desvanece → pausa.
        t += Time.deltaTime;
        float total = Ciclo + Pausa;
        if (t > total) t -= total;
        float u = t / Ciclo;
        var a = desde + Vector3.up * 0.06f;
        var b = meta + Vector3.up * 0.04f;
        float alfa;
        Vector3 p;
        if (u < 0.15f) { p = a; alfa = u / 0.15f; }
        else if (u < 0.7f)
        {
            float k = Mathf.SmoothStep(0f, 1f, (u - 0.15f) / 0.55f);
            p = Vector3.Lerp(a, b, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.18f);
            alfa = 1f;
        }
        else if (u < 0.85f) { p = b + Vector3.down * 0.03f * Mathf.Sin((u - 0.7f) / 0.15f * Mathf.PI); alfa = 1f; }
        else if (u <= 1f) { p = b; alfa = 1f - (u - 0.85f) / 0.15f; }
        else { p = b; alfa = 0f; }
        fantasma.position = p;
        var dir = b - a; dir.y = 0f;
        if (dir.sqrMagnitude > 1e-4f) fantasma.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        var c = Cian; c.a = 0.55f * alfa;
        matFantasma.color = c;
        fantasmaR.enabled = alfa > 0.01f;

        // Anillo en el destino: late siempre y se abre al "tocar".
        float toque = u >= 0.7f && u < 1f ? (u - 0.7f) / 0.3f : 0f;
        anillo.position = meta + Vector3.up * 0.005f;
        anillo.localScale = Vector3.one * (0.16f * (1f + 0.1f * Mathf.Sin(Time.time * 4f)) + 0.12f * toque);
        var ca = Cian; ca.a = toque > 0f ? 0.8f * (1f - toque) + 0.3f : 0.35f;
        anilloR.color = ca;
    }

    // Qué gesto toca ahora: desde dónde (el portaobjetos o la bandeja), hasta dónde y con qué palabras.
    bool Calcular(out Vector3 desde, out Transform hacia, out string texto)
    {
        desde = Vector3.zero; hacia = null; texto = null;
        var rm = RetoManager.Instancia;
        if (rm == null || !rm.JugadorDentro || rm.Mision != 1 || MicroscopioOptico.AlgunoActivo || BiohazardEvent.Activo) return false;
        if (rm.GuiaExternaActiva) return false;
        switch (rm.Actual)
        {
            case EtapaReto.Exploracion:
                if (!EquiposCentro.Listo(EquiposCentro.Incubadora)) return false;
                return AMuestraA(rm, Agarrado(), out desde, out hacia, out texto);
            case EtapaReto.Tincion:
            {
                var pa = rm.PortaMuestraA;
                if (pa == null) return AMuestraA(rm, Agarrado(), out desde, out hacia, out texto);
                desde = pa.transform.position;
                if (pa.TincionErronea)
                {
                    var des = fuentes.FirstOrDefault(f => f != null && f.esDesinfectante && f.isActiveAndEnabled);
                    if (des == null) return false;
                    hacia = des.transform;
                    texto = "<b>LÍMPIALO AQUÍ</b>\n<size=70%>y vuelve a empezar</size>";
                    return true;
                }
                int n = Mathf.Clamp(pa.PasosTincion + 1, 1, 4);
                var g = fuentes.FirstOrDefault(f => f != null && !f.esDesinfectante && f.reactivo == n && f.isActiveAndEnabled);
                if (g == null) return false;
                hacia = g.transform;
                texto = $"<b>GOTERO {n} DE 4</b>\n<size=70%>{Portaobjetos.Reactivos[n]}</size>";
                return true;
            }
            case EtapaReto.Observacion:
            {
                var pa = rm.ObservandoA ? rm.PortaMuestraA : rm.PortaPendiente;
                if (pa == null || pa.Encajada != null) return false;
                var pl = Cercana(pa.transform.position);
                if (pl == null) return false;
                desde = pa.transform.position;
                hacia = pl.anclaje != null ? pl.anclaje : pl.transform;
                texto = $"<b>PONLO EN LA PLATINA</b>\n<size=70%>{pa.Etiqueta} · luego acerca la cara al ocular</size>";
                return true;
            }
        }
        return false;
    }

    bool AMuestraA(RetoManager rm, Portaobjetos enMano, out Vector3 desde, out Transform hacia, out string texto)
    {
        desde = Vector3.zero; hacia = null; texto = null;
        if (rm.placa == null) return false;
        Vector3? origen = enMano != null ? enMano.transform.position
                        : MontajeMisiones.BandejaBanco != null ? MontajeMisiones.BandejaBanco.position : (Vector3?)null;
        if (origen == null) return false;
        desde = origen.Value;
        hacia = rm.placa.transform;
        texto = enMano != null ? "<b>TOCA LA MUESTRA A</b>\n<size=70%>con el portaobjetos</size>"
                               : "<b>COGE UN PORTAOBJETOS</b>\n<size=70%>y toca la MUESTRA A</size>";
        return true;
    }

    static Portaobjetos Agarrado() => Portaobjetos.Todos.FirstOrDefault(p => p != null && p.Agarrado && !p.Despues);

    Platina Cercana(Vector3 p)
    {
        Platina mejor = null;
        float dm = float.MaxValue;
        foreach (var pl in platinas)
        {
            if (pl == null || !pl.isActiveAndEnabled || !pl.Libre) continue;
            float d = (pl.transform.position - p).sqrMagnitude;
            if (d < dm) { dm = d; mejor = pl; }
        }
        return mejor;
    }

    void Mostrar(bool si)
    {
        if (fantasma.gameObject.activeSelf != si) fantasma.gameObject.SetActive(si);
        if (anillo.gameObject.activeSelf != si) anillo.gameObject.SetActive(si);
        if (etiquetaT.gameObject.activeSelf != si) etiquetaT.gameObject.SetActive(si);
    }

    // ------------------------------------------------------------------ construcción

    void Montar()
    {
        matFantasma = new Material(Shader.Find("Sprites/Default")) { name = "DemoFantasma" };
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "PortaobjetosFantasma";
        Destroy(go.GetComponent<Collider>());
        fantasma = go.transform;
        fantasma.SetParent(transform, false);
        fantasma.localScale = new Vector3(0.026f, 0.004f, 0.076f);
        fantasmaR = go.GetComponent<Renderer>();
        fantasmaR.sharedMaterial = matFantasma;
        fantasmaR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fantasmaR.receiveShadows = false;

        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "AnilloDemo" };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f).magnitude * 2f;
                float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.85f) / 0.12f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        anilloR = new GameObject("AnilloDemo").AddComponent<SpriteRenderer>();
        anilloR.sprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        anillo = anilloR.transform;
        anillo.SetParent(transform, false);
        anillo.rotation = Quaternion.Euler(90f, 0f, 0f);
        anilloR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        etiquetaT = K.Grupo(transform, "EtiquetaDemo", Vector3.zero);
        etiqueta = K.Texto(etiquetaT, "", Vector3.zero, 0.42f, Cian, 0.8f, 0.16f, TextAlignmentOptions.Center, true);
        etiqueta.outlineWidth = 0.25f;
        etiqueta.outlineColor = new Color32(0, 0, 0, 230);
        foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
        Mostrar(false);
    }
}
