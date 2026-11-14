using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Mensajes cortos que flotan junto a la acción (feedback textual inmediato) y se desvanecen.
// Color + prefijo de texto, para no depender solo del color. Cada tipo dice qué pasó:
//   Ok = bien hecho · Error = respuesta o acción equivocada ("AÚN NO") · Falta = todavía no se puede, falta un paso
//   Pista = ayuda o confirmación · Info = información sin prefijo.
// Duran según su longitud (un texto largo no se puede leer en 5 s), llevan un fondo oscuro para leerse sobre el
// laboratorio claro y los importantes quedan en un historial que la AYUDA del menú enseña (reconocer, no recordar).
// Presencia: un aviso es una ETIQUETA FIJA EN EL MUNDO junto al objeto que lo provoca (con un rabillo y una línea
// hacia él); se orienta una vez hacia el jugador y no le persigue. Lo que no tiene objeto (misión completa, pistas,
// partida cargada) va a la muñeca con EnMuneca: vibra y espera a que se mire, sin texto delante de la cara.
public static class Aviso
{
    public enum Tipo { Info, Ok, Error, Pista, Falta }

    public struct Registro { public string texto; public Tipo tipo; public float tiempo; }

    const int MaxHistorial = 6;
    static readonly List<Registro> historial = new List<Registro>();
    public static IReadOnlyList<Registro> Ultimos => historial;

    static readonly List<Item> pool = new List<Item>();
    static Transform raiz;
    const int MaxVisibles = 2;   // más de dos etiquetas a la vez ya es ruido

    // Novedades sin objeto al que pegarse: al brazalete de la muñeca. Sin brazalete, el respaldo de siempre.
    public static void EnMuneca(string texto, Tipo tipo = Tipo.Info)
    {
        if (string.IsNullOrEmpty(texto)) return;
        var b = BrazaleteBaymax.Instancia;
        if (b != null && b.isActiveAndEnabled)
        {
            Registrar(texto, tipo);
            b.Notificar(texto, tipo);
            return;
        }
        var cab = ManosRig.Cabeza;
        if (cab == null) { Registrar(texto, tipo); return; }
        var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
        Mostrar(texto, cab.position + f * 0.9f, tipo);
    }

    // duracion <= 0: según la longitud del texto. Los errores y los "falta un paso" nunca duran menos que eso.
    public static void Mostrar(string texto, Vector3 pos, Tipo tipo = Tipo.Info, float duracion = -1f)
    {
        if (string.IsNullOrEmpty(texto)) return;
        float auto = Mathf.Clamp(2.5f + texto.Length / 14f, 4f, 12f);
        if (duracion <= 0f) duracion = auto;
        else if (tipo == Tipo.Error || tipo == Tipo.Falta) duracion = Mathf.Max(duracion, auto);
        Registrar(texto, tipo);
        // Con el menú ☰ o la ayuda abiertos no se pinta nada encima (queda en el historial de la AYUDA).
        if (HayPanelAbierto) return;

        if (raiz == null)
        {
            raiz = new GameObject("Avisos").transform;
            pool.Clear();
        }
        // Reutiliza el aviso que ya está en ese sitio (no se apilan) o el más antiguo.
        Item elegido = null;
        foreach (var it in pool) if (it != null && it.Activo && (it.Ancla - pos).sqrMagnitude < 0.09f) { elegido = it; break; }
        int activos = 0;
        foreach (var it in pool) if (it != null && it.Activo) activos++;
        if (elegido == null && activos < MaxVisibles)
        {
            foreach (var it in pool) if (it != null && !it.Activo) { elegido = it; break; }
            if (elegido == null) { elegido = Crear(); pool.Add(elegido); }
        }
        if (elegido == null) foreach (var it in pool) if (it != null && it.Activo && (elegido == null || it.Inicio < elegido.Inicio)) elegido = it;
        elegido.Mostrar(texto, pos, tipo, duracion);
    }

    // Menú ☰ o tarjetas de ayuda abiertos: lo demás se aparta para no taparlos.
    public static bool HayPanelAbierto =>
        (MenuAjustes.Instancia != null && MenuAjustes.Instancia.Abierto) || (AvisoInicio.Instancia != null && AvisoInicio.Instancia.Abierto);

    // Al abrir el menú o la ayuda se quitan los avisos que hubiera flotando.
    public static void OcultarTodos()
    {
        foreach (var it in pool) if (it != null && it.Activo) it.gameObject.SetActive(false);
    }

    // Los avisos importantes (todo menos la información de paso) se guardan para poder releerlos.
    static void Registrar(string texto, Tipo tipo)
    {
        if (tipo == Tipo.Info) return;
        if (historial.Count > 0 && historial[historial.Count - 1].texto == texto) return;
        historial.Add(new Registro { texto = texto, tipo = tipo, tiempo = Time.time });
        if (historial.Count > MaxHistorial) historial.RemoveAt(0);
    }

    static Item Crear()
    {
        var go = new GameObject("Aviso");
        go.transform.SetParent(raiz, false);
        var t = go.AddComponent<TextMeshPro>();
        var f = EstiloNanoLab.Fuente;   // Nunito; si no carga, la letra por defecto de TMP
        if (f != null) t.font = f;
        t.sortingOrder = AdaptadorEscena.OrdenTexto + 2;   // por encima de paneles y textos
        t.fontSize = 0.22f;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.rectTransform.sizeDelta = new Vector2(0.5f, 0.15f);
        t.overflowMode = TextOverflowModes.Overflow;
        t.fontStyle = FontStyles.Bold;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 230);
        var item = go.AddComponent<Item>();
        item.texto = t;
        item.fondo = CrearFondo(go.transform);
        if (item.fondo != null)
        {
            // Rabillo: un rombo del mismo panel bajo el borde inferior, apuntando al objeto.
            var r = Object.Instantiate(item.fondo.gameObject, go.transform);
            r.name = "Rabillo";
            item.rabillo = r.GetComponent<PanelRedondeado>();
            item.rabillo.radio = 0.002f;
            item.rabillo.borde = 0f;
            item.rabillo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
        // Línea fina hasta el objeto que provoca el aviso.
        var lgo = new GameObject("Linea");
        lgo.transform.SetParent(go.transform, false);
        var l = lgo.AddComponent<LineRenderer>();
        l.useWorldSpace = true;
        l.positionCount = 2;
        l.widthMultiplier = 0.0015f;
        l.numCapVertices = 2;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        l.receiveShadows = false;
        var sh = Shader.Find("Sprites/Default");
        if (sh != null) l.sharedMaterial = new Material(sh);
        item.linea = l;
        go.SetActive(false);
        return item;
    }

    // Fondo oscuro redondeado detrás del texto (mismo material que los demás paneles). null si no hay material.
    static PanelRedondeado CrearFondo(Transform padre)
    {
        Material mat = null;
        var ejemplo = Object.FindObjectOfType<PanelRedondeado>(true);
        if (ejemplo != null) mat = ejemplo.GetComponent<MeshRenderer>().sharedMaterial;
        if (mat == null) { var sh = Shader.Find("Interaction/RoundedBoxUnlit"); if (sh != null) mat = new Material(sh); }
        if (mat == null) return null;
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "Fondo";
        Object.Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(padre, false);
        var mr = q.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = AdaptadorEscena.OrdenTexto + 1;   // detrás del texto del aviso, delante del resto
        mr.sharedMaterial = mat;
        var p = q.AddComponent<PanelRedondeado>();
        p.radio = 0.02f;
        p.borde = 0.003f;
        p.color = new Color(0.03f, 0.07f, 0.12f, 0.82f);
        return p;
    }

    public class Item : MonoBehaviour
    {
        public TextMeshPro texto;
        public PanelRedondeado fondo, rabillo;
        public LineRenderer linea;
        public float Inicio { get; private set; }
        public bool Activo => gameObject.activeSelf;
        public Vector3 Ancla { get; private set; }   // punto del objeto al que se refiere (el aviso queda encima)
        float fin;
        Color color;
        Vector3 basePos;

        public void Mostrar(string s, Vector3 pos, Tipo tipo, float duracion)
        {
            string prefijo;
            switch (tipo)
            {
                case Tipo.Ok: color = new Color(0.35f, 1f, 0.55f); prefijo = "BIEN · "; break;
                case Tipo.Error: color = new Color(1f, 0.5f, 0.4f); prefijo = "AÚN NO · "; break;
                case Tipo.Pista: color = new Color(1f, 0.85f, 0.35f); prefijo = "PISTA · "; break;
                case Tipo.Falta: color = new Color(1f, 0.75f, 0.3f); prefijo = "FALTA UN PASO · "; break;
                default: color = new Color(0.6f, 0.9f, 1f); prefijo = ""; break;
            }
            texto.text = prefijo + s;
            texto.color = color;
            Ancla = pos;
            Inicio = Time.time;
            fin = Time.time + duracion;
            gameObject.SetActive(true);
            // Etiqueta fija en el mundo: algo por encima del punto, girada solo en horizontal hacia el jugador (una vez).
            texto.ForceMeshUpdate();
            float alto = Mathf.Max(0.03f, texto.textBounds.size.y);
            basePos = pos + Vector3.up * (0.06f + alto / 2f);
            transform.position = basePos;
            var cab = ManosRig.Cabeza;
            if (cab != null)
            {
                var d = transform.position - cab.position; d.y = 0f;
                if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
            }
            AjustarFondo();
        }

        // El fondo abraza al texto (que cambia de tamaño con cada mensaje).
        void AjustarFondo()
        {
            if (fondo == null) return;
            texto.ForceMeshUpdate();
            var b = texto.textBounds;
            bool hay = b.size.x > 0.001f && b.size.y > 0.001f;
            fondo.gameObject.SetActive(hay);
            if (!hay) return;
            fondo.ancho = b.size.x + 0.05f;
            fondo.alto = b.size.y + 0.035f;
            fondo.colorBorde = color;
            fondo.opacidad = 0f;
            fondo.transform.localPosition = new Vector3(b.center.x, b.center.y, 0.004f);   // detrás del texto, visto desde la cabeza
            fondo.Aplicar();
            if (rabillo != null)
            {
                rabillo.ancho = rabillo.alto = 0.022f;
                rabillo.color = fondo.color;
                rabillo.opacidad = 0f;
                rabillo.transform.localPosition = new Vector3(b.center.x, b.center.y - fondo.alto / 2f, 0.0045f);
                rabillo.Aplicar();
            }
        }

        void LateUpdate()
        {
            if (Time.time > fin) { gameObject.SetActive(false); return; }
            float a = Mathf.Clamp01((fin - Time.time) / 0.6f) * Mathf.Clamp01((Time.time - Inicio) / 0.15f);
            var c = color; c.a = a;
            texto.color = c;
            if (fondo != null && fondo.gameObject.activeSelf) { fondo.opacidad = a; fondo.Aplicar(); }
            if (rabillo != null && fondo != null && fondo.gameObject.activeSelf) { rabillo.opacidad = a; rabillo.Aplicar(); }
            if (linea != null)
            {
                // Del pico del rabillo al objeto (si el objeto está justo debajo, la línea casi no se ve).
                var punta = transform.TransformPoint(new Vector3(0f, fondo != null ? fondo.transform.localPosition.y - fondo.alto / 2f - 0.012f : -0.03f, 0f));
                linea.SetPosition(0, punta);
                linea.SetPosition(1, Ancla);
                var lc = color; lc.a = a * 0.8f;
                linea.startColor = linea.endColor = lc;
            }
        }
    }
}
