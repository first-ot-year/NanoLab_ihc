using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Terminal de hipótesis y estrategia (RF-05). Dos decisiones a partir de las evidencias y las fichas:
//  1) qué microorganismo cree el estudiante que es la muestra;
//  2) qué estructura de ese microorganismo atacará (la estrategia que luego prepara en la prensa).
// Los botones se tocan o se apuntan con el rayo (BotonFisico es Apuntable). Ambas se pueden cambiar antes de experimentar.
// Revisión del profesor: dos filas de botones (3 + 4) confundían. Ahora es UNA pregunta a la vez con TRES botones
// (los de la fila 1) cuyas respuestas cambian al pasar de la pregunta 1 (¿qué bacteria es?) a la 2 (¿qué atacarás?).
// La fila 2 de la consola y sus rótulos fijos se ocultan al empezar.
public class TerminalHipotesis : MonoBehaviour
{
    public BotonFisico[] botones;
    public MuestraData[] opciones;
    public Renderer[] marcos;               // se encienden en la opción elegida
    public BotonFisico[] botonesEstrategia;
    public Renderer[] marcosEstrategia;
    public TMP_Text pantalla;

    public const string AvisoSimplificado = "Modelo educativo simplificado: no es un protocolo clínico ni un tratamiento real.";

    void Awake()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            int k = i;
            botones[i].Pulsado += () => Pulsar(k);
        }
        if (botonesEstrategia != null)
            for (int i = 0; i < botonesEstrategia.Length; i++)
            {
                int k = i;
                if (botonesEstrategia[i] != null) botonesEstrategia[i].Pulsado += () => ElegirEstrategia(k);
            }
    }

    void OnEnable() => RetoManager.Cambio += Refrescar;
    void OnDisable() => RetoManager.Cambio -= Refrescar;
    void Start() => Refrescar();

    // ------------------------------------------------------------------ una pregunta, tres botones

    public const int NumBotones = 3;
    TMP_Text cabecera;
    TMP_Text[] rotulos;
    int[] estrategiaDeBoton = new int[0];   // botón -> índice en reto.estrategias (pregunta 2)
    int preguntaMostrada;
    float animarHasta;

    // 1 = ¿qué bacteria es la MUESTRA A?  ·  2 = ¿qué estructura atacarás?
    public int Pregunta => RetoManager.Instancia != null && RetoManager.Instancia.Hipotesis != null ? 2 : 1;

    // Qué botón (0..2) muestra la estrategia 'i' de reto.estrategias en la pregunta 2 (-1 si no está entre las tres).
    public int BotonDeEstrategia(int i) => System.Array.IndexOf(estrategiaDeBoton, i);

    // Las tres respuestas de la pregunta 2: todas las estrategias menos el "control" (no atacar nada), salvo que sea
    // la correcta; si sobran, se quitan de las incorrectas.
    void PrepararEstrategias()
    {
        var rm = RetoManager.Instancia;
        var est = rm != null && rm.reto != null ? rm.reto.estrategias : null;
        if (est == null) return;
        var lista = Enumerable.Range(0, est.Length).Where(i => est[i] != null).ToList();
        var correcta = rm.reto.DianaCorrecta;
        lista.RemoveAll(i => lista.Count > NumBotones && est[i].diana == DianaAgente.Ninguna && est[i].diana != correcta);
        while (lista.Count > NumBotones)
        {
            int quitar = lista.FindLastIndex(i => est[i].diana != correcta);
            if (quitar < 0) break;
            lista.RemoveAt(quitar);
        }
        estrategiaDeBoton = lista.ToArray();
    }

    // Oculta la fila 2 y los rótulos fijos de la consola y pone rótulos que cambian con la pregunta.
    void TresBotones()
    {
        if (rotulos != null || botones == null) return;
        PrepararEstrategias();
        var nombresFijos = new System.Collections.Generic.HashSet<string>();
        foreach (var o in opciones) if (o != null) nombresFijos.Add($"<b>{o.nombre}</b>");
        var rm = RetoManager.Instancia;
        if (rm != null && rm.reto != null && rm.reto.estrategias != null)
            foreach (var e in rm.reto.estrategias) if (e != null) nombresFijos.Add($"<b>{e.corto}</b>");
        TMP_Text molde = null;
        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
        {
            if (t == pantalla || t.text == null) continue;
            bool fijo = nombresFijos.Contains(t.text) || t.text.Contains("1) HIPÓTESIS") || t.text.Contains("2) ESTRATEGIA");
            if (!fijo) continue;
            if (molde == null && nombresFijos.Contains(t.text)) molde = t;
            t.gameObject.SetActive(false);
        }
        if (botonesEstrategia != null) foreach (var b in botonesEstrategia) if (b != null) b.gameObject.SetActive(false);
        if (marcosEstrategia != null) foreach (var m in marcosEstrategia) if (m != null) m.gameObject.SetActive(false);
        for (int i = NumBotones; i < botones.Length; i++) if (botones[i] != null) botones[i].gameObject.SetActive(false);

        // Rótulos nuevos: encima de la tapa, delante de cada botón (donde estaba el nombre) y una cabecera con la pregunta.
        var rot = Quaternion.Euler(90f, 180f, 0f);
        rotulos = new TMP_Text[Mathf.Min(NumBotones, botones.Length)];
        for (int i = 0; i < rotulos.Length; i++)
        {
            if (botones[i] == null) continue;
            var lp = transform.InverseTransformPoint(botones[i].transform.position);
            rotulos[i] = Rotulo($"Respuesta{i}", new Vector3(lp.x, 0.923f, lp.z - 0.2f), 0.24f, Color.white, 0.42f, 0.16f, rot, molde);
        }
        cabecera = Rotulo("Pregunta", new Vector3(0f, 0.923f, -0.5f), 0.27f, new Color(0.37f, 0.83f, 1f), 1.36f, 0.12f, rot, molde);
        preguntaMostrada = 0;
    }

    TMP_Text Rotulo(string nombre, Vector3 pos, float tam, Color c, float ancho, float alto, Quaternion rot, TMP_Text molde)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var t = go.AddComponent<TextMeshPro>();
        if (molde != null) { t.font = molde.font; t.fontSharedMaterial = molde.fontSharedMaterial; t.sortingOrder = ((TextMeshPro)molde).sortingOrder; }
        t.richText = true;
        t.fontSize = molde != null ? molde.fontSize * tam / 0.24f : tam * 10f;
        t.color = c;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = t.fontSize * 0.55f;
        t.rectTransform.sizeDelta = new Vector2(ancho, alto);
        t.rectTransform.localPosition = pos;
        t.rectTransform.localRotation = rot;
        return t;
    }

    // Botón 0..2: responde a la pregunta que esté en pantalla.
    public void Pulsar(int boton)
    {
        if (Pregunta == 1) { Elegir(boton); return; }
        if (boton >= 0 && boton < estrategiaDeBoton.Length) ElegirEstrategia(estrategiaDeBoton[boton]);
    }

    void Update()
    {
        // Al cambiar de pregunta, las respuestas "saltan" (los botones crecen y vuelven) para que se note el cambio.
        if (Time.time >= animarHasta || botones == null) return;
        float t = 1f - (animarHasta - Time.time) / 0.45f;
        float s = 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        if (rotulos != null) foreach (var r in rotulos) if (r != null) r.transform.localScale = Vector3.one * s;
    }

    // Sobre el borde de la consola, a la altura del pecho (el origen de la estación está en el suelo).
    public Vector3 Encima => transform.TransformPoint(new Vector3(0f, 1.2f, 0.25f));

    public void Elegir(int i)
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        if (!rm.PuedeHipotesis(out var motivo)) { Rechazar(motivo); return; }
        // Con la A ya identificada, la pregunta 1 no vuelve a contar errores (pulsarla sin querer no ensucia las métricas).
        if (rm.Hipotesis != null)
        {
            Aviso.Mostrar($"La MUESTRA A ya está identificada: {rm.Hipotesis.nombre}. Ahora responde la PREGUNTA 2: qué estructura atacarás.", Encima, Aviso.Tipo.Info);
            Sonidos.Clic(transform.position);
            return;
        }
        // Misión 1: la identificación se comprueba; si no encaja con las evidencias, RetoManager explica por qué.
        if (rm.RegistrarHipotesis(opciones[i])) Sonidos.Encaje(transform.position);
        else Sonidos.Error(transform.position);
        Refrescar();
    }

    public void ElegirEstrategia(int i)
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        if (!rm.PuedeEstrategia(out var motivo)) { Rechazar(motivo); return; }
        rm.RegistrarEstrategia(i);
        Sonidos.Encaje(transform.position);
        Refrescar();
    }

    void Rechazar(string motivo)
    {
        // No es un error: todavía falta un paso (se dice cuál).
        Aviso.Mostrar(motivo, Encima, Aviso.Tipo.Falta);
        Sonidos.Pista(transform.position);
    }

    public void Refrescar()
    {
        var rm = RetoManager.Instancia;
        if (rotulos == null && rm != null && rm.reto != null) TresBotones();
        var h = rm != null ? rm.Hipotesis : null;
        var s = rm != null ? rm.EstrategiaElegida : null;
        // Los botones que aún no se pueden usar se ven grises (al pulsarlos explican qué falta).
        bool puedeH = rm != null && rm.Hipotesis == null && rm.PuedeHipotesis(out _);
        bool puedeE = rm != null && rm.PuedeEstrategia(out _);
        int pregunta = Pregunta;
        if (rotulos != null && estrategiaDeBoton.Length == 0) PrepararEstrategias();
        for (int i = 0; i < botones.Length && i < NumBotones; i++) if (botones[i] != null) botones[i].Habilitar(pregunta == 1 ? puedeH : puedeE && i < estrategiaDeBoton.Length);
        if (botonesEstrategia != null) foreach (var b in botonesEstrategia) if (b != null) b.Habilitar(puedeE);
        for (int i = 0; i < marcos.Length; i++)
            if (marcos[i] != null) marcos[i].enabled = pregunta == 1 ? false : i < estrategiaDeBoton.Length && rm.Estrategia == estrategiaDeBoton[i];
        if (rotulos != null)
        {
            for (int i = 0; i < rotulos.Length; i++)
            {
                if (rotulos[i] == null) continue;
                char letra = (char)('A' + i);
                rotulos[i].text = pregunta == 1
                    ? (i < opciones.Length && opciones[i] != null ? $"<b>{letra} · {opciones[i].nombre}</b>" : "")
                    : (i < estrategiaDeBoton.Length ? $"<b>{letra} · {rm.reto.estrategias[estrategiaDeBoton[i]].corto}</b>" : "");
            }
            if (cabecera != null)
                cabecera.text = pregunta == 1 ? "<b>PREGUNTA 1 DE 2:</b> ¿QUÉ BACTERIA ES LA MUESTRA A?" : "<b>PREGUNTA 2 DE 2:</b> ¿QUÉ ESTRUCTURA ATACARÁS?";
            if (pregunta != preguntaMostrada)
            {
                if (preguntaMostrada != 0) { animarHasta = Time.time + 0.45f; Sonidos.Etapa(transform.position + Vector3.up); }
                preguntaMostrada = pregunta;
            }
        }
        if (pantalla == null) return;
        if (rotulos != null) { pantalla.text = TextoPantalla(rm, h, s, pregunta); return; }

        var sb = new StringBuilder();
        // Lo observado, para decidir a partir de las evidencias y no de memoria.
        if (rm != null && rm.Evidencias.Any(x => x))
        {
            var ev = rm.reto.evidencias.Select((e, k) => rm.Evidencias[k] ? e.valor : "???");
            sb.AppendLine($"<size=72%><color=#8FD8FF>TUS EVIDENCIAS (MUESTRA A):</color> {string.Join("  ·  ", ev)}</size>");
        }
        else sb.AppendLine($"<size=72%><color=#8FD8FF>TUS EVIDENCIAS (MUESTRA A):</color> <color=#FFD25A>aún ninguna: observa la muestra al microscopio (3)</color></size>");

        sb.AppendLine(h == null
            ? $"<b>MISIÓN 1 · ¿Qué bacteria es la MUESTRA A?</b>  <color=#FFD25A>[ PENDIENTE ]</color>"
            : $"MISIÓN 1 · La MUESTRA A es <b><color=#FFD25A>{h.nombre}</color></b>  <color=#39FF88>[ IDENTIFICADA ]</color>");
        // Reconocer, no recordar: lo que decía cada ficha ya consultada, junto a su botón.
        if (h == null && rm != null)
        {
            sb.Append("<size=62%>");
            for (int i = 0; i < opciones.Length; i++)
            {
                if (opciones[i] == null) continue;
                char letra = (char)('A' + i);
                sb.AppendLine(rm.FichaVistaDe(opciones[i])
                    ? $"<b>{letra}</b> · {opciones[i].nombre}: {rm.RasgosFicha(opciones[i])}"
                    : $"<color=#8095AA><b>{letra}</b> · {opciones[i].nombre}: ficha sin consultar (BIBLIOTECA 4)</color>");
            }
            sb.Append("</size>");
        }
        sb.AppendLine(s == null
            ? (h == null ? "<color=#8FA6BC>MISIÓN 2 · ¿Qué estructura atacarás?  (después de identificarla)</color>" : "<b>MISIÓN 2 · ¿Qué estructura atacarás?</b>  <color=#FFD25A>[ PENDIENTE ]</color>")
            : $"MISIÓN 2 · Estrategia: <b><color=#FFD25A>{s.nombre}</color></b>  <color=#39FF88>[ ELEGIDA ]</color>");
        sb.Append("<size=70%><color=#A9BED2>");
        sb.Append(h != null && s != null
            ? "Ahora busca en la PRENSA (6) el vial que hace lo que decidiste y formula la cápsula. Puedes cambiar la estrategia antes de experimentar."
            : "Elige según tus evidencias y las fichas de la BIBLIOTECA (4). La estrategia la puedes cambiar antes de experimentar.");
        sb.Append($"</color>\n<color=#7F93A8><i>{AvisoSimplificado}</i></color></size>");
        pantalla.text = sb.ToString();
    }

    // Pantalla de la pregunta actual: qué se pregunta, en qué apoyarse y qué dice cada una de las tres respuestas.
    string TextoPantalla(RetoManager rm, MuestraData h, RetoData.Estrategia s, int pregunta)
    {
        var sb = new StringBuilder();
        if (rm == null) return "";
        if (rm.Evidencias.Any(x => x))
        {
            var ev = rm.reto.evidencias.Select((e, k) => rm.Evidencias[k] ? e.valor : "???");
            sb.AppendLine($"<size=70%><color=#8FD8FF>TUS EVIDENCIAS (MUESTRA A):</color> {string.Join("  ·  ", ev)}</size>");
        }
        else sb.AppendLine("<size=70%><color=#8FD8FF>TUS EVIDENCIAS (MUESTRA A):</color> <color=#FFD25A>aún ninguna: observa la muestra al microscopio (3)</color></size>");
        if (pregunta == 1)
        {
            sb.AppendLine("<b>PREGUNTA 1 DE 2 · ¿Qué bacteria es la MUESTRA A?</b>");
            sb.Append("<size=66%>");
            for (int i = 0; i < opciones.Length && i < NumBotones; i++)
            {
                if (opciones[i] == null) continue;
                char letra = (char)('A' + i);
                sb.AppendLine(rm.FichaVistaDe(opciones[i])
                    ? $"<b>{letra}</b> · {opciones[i].nombre}: {rm.RasgosFicha(opciones[i])}"
                    : $"<b>{letra}</b> · {opciones[i].nombre} <color=#8095AA>(su ficha está en la BIBLIOTECA 4)</color>");
            }
            sb.Append("</size><size=66%><color=#A9BED2>Pulsa el botón de la respuesta que encaje con tus evidencias.</color></size>");
        }
        else
        {
            sb.AppendLine($"<size=78%>La MUESTRA A es <b><color=#FFD25A>{h.nombre}</color></b> <color=#39FF88>[IDENTIFICADA]</color></size>");
            sb.AppendLine("<b>PREGUNTA 2 DE 2 · ¿Qué estructura atacarás?</b>");
            sb.Append("<size=62%>");
            for (int i = 0; i < estrategiaDeBoton.Length; i++)
            {
                var e = rm.reto.estrategias[estrategiaDeBoton[i]];
                char letra = (char)('A' + i);
                string marca = rm.Estrategia == estrategiaDeBoton[i] ? " <color=#39FF88>[ELEGIDA]</color>" : "";
                sb.AppendLine($"<b>{letra} · {e.nombre}</b>{marca}{(string.IsNullOrEmpty(e.descripcion) ? "" : $": {e.descripcion}")}");
            }
            sb.Append("</size><size=66%><color=#A9BED2>");
            sb.Append(s != null ? "Ahora ve a la PRENSA (6) y elige el vial que hace lo que decidiste. Puedes cambiar de respuesta antes de experimentar."
                                : "Piensa en la pared y la tinción de Gram que viste: ¿qué estructura tiene esta bacteria que se pueda atacar?");
            sb.Append("</color></size>");
        }
        sb.Append($"\n<size=50%><color=#7F93A8><i>{AvisoSimplificado}</i></color></size>");
        return sb.ToString();
    }
}
