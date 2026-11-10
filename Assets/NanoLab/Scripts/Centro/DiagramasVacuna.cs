using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using C = ContenidoVacuna;
using K = KitVacuna;

// Diagramas de la pantalla derecha para los pasos de FABRICACIÓN del CASO 01 · Crea la vacuna (diseño del ARNm, síntesis,
// nanopartículas y control de calidad). Igual que VisualesVacuna: primitivas creadas en tiempo de ejecución,
// se leen desde -z local y el color siempre va con texto o forma (RNF-03).

// ---------------------------------------------------------------------------------------------- diseño del ARNm

// La hebra de ARNm con sus 5 huecos (5' > 3'): refleja en grande lo que el estudiante coloca en el molde.
public class DiagramaARNm : MonoBehaviour
{
    const int N = 5;
    Renderer[] cajas, bordes;
    TMP_Text[] textos, marcas;
    TMP_Text explica;

    public static DiagramaARNm Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "DiagramaARNm", pos);
        var d = g.gameObject.AddComponent<DiagramaARNm>();
        d.Montar();
        return d;
    }

    static float X(int i) => -0.5f + i * 0.25f;

    void Montar()
    {
        cajas = new Renderer[N]; bordes = new Renderer[N]; textos = new TMP_Text[N]; marcas = new TMP_Text[N];
        K.Caja(transform, "Hebra", new Vector3(0f, 0.25f, -0.004f), new Vector3(1.24f, 0.012f, 0.002f), K.Plano(new Color(1f, 0.6f, 0.15f)));
        K.Texto(transform, "<b>5'</b>", new Vector3(-0.66f, 0.25f, -0.006f), 0.3f, K.Amarillo, 0.1f, 0.08f, TextAlignmentOptions.Center, true);
        K.Texto(transform, "<b>3'</b>", new Vector3(0.66f, 0.25f, -0.006f), 0.3f, K.Amarillo, 0.1f, 0.08f, TextAlignmentOptions.Center, true);
        K.Texto(transform, "el ribosoma lee en este sentido  >>>", new Vector3(0f, 0.38f, -0.006f), 0.17f, K.Gris, 1.2f, 0.05f);
        for (int i = 0; i < N; i++)
        {
            bordes[i] = K.Caja(transform, "Borde" + i, new Vector3(X(i), 0.25f, -0.006f), new Vector3(0.23f, 0.13f, 0.002f), K.Plano(Color.white));
            cajas[i] = K.Caja(transform, "Hueco" + i, new Vector3(X(i), 0.25f, -0.009f), new Vector3(0.21f, 0.11f, 0.002f), K.Plano(Color.white));
            textos[i] = K.Texto(transform, "", new Vector3(X(i), 0.25f, -0.012f), 0.17f, new Color(0.05f, 0.07f, 0.1f), 0.2f, 0.1f, TextAlignmentOptions.Center, true);
            K.AutoTam(textos[i], 0.11f);
            marcas[i] = K.Texto(transform, "", new Vector3(X(i), 0.12f, -0.012f), 0.22f, K.Verde, 0.22f, 0.06f, TextAlignmentOptions.Center, true);
            K.Texto(transform, (i + 1).ToString(), new Vector3(X(i), 0.355f, -0.012f), 0.18f, K.Gris, 0.1f, 0.05f, TextAlignmentOptions.Center, true);
        }
        explica = K.Texto(transform, "", new Vector3(0f, 0.04f, -0.006f), 0.21f, Color.white, 1.3f, 0.42f, TextAlignmentOptions.Top, false, true);
        K.AutoTam(explica, 0.15f);
        Mostrar(new string[N], null, "");
    }

    // ids[i] = bloque del hueco i (null si está vacío); ok[i] = resultado de la verificación (null = sin verificar).
    public void Mostrar(string[] ids, bool?[] ok, string texto)
    {
        for (int i = 0; i < N; i++)
        {
            var b = ids[i] != null ? System.Array.Find(C.Bloques, x => x.id == ids[i]) : null;
            K.Pintar(cajas[i], b != null ? b.color : new Color(0.16f, 0.2f, 0.26f));
            textos[i].text = b != null ? b.nombre : "?";
            textos[i].color = b != null ? new Color(0.05f, 0.07f, 0.1f) : K.Gris;
            bool? r = ok != null ? ok[i] : null;
            K.Pintar(bordes[i], r == true ? K.Verde : r == false ? K.Rojo : new Color(0.45f, 0.55f, 0.65f));
            marcas[i].text = r == true ? "OK" : r == false ? "X" : "";
            marcas[i].color = r == true ? K.Verde : K.Rojo;
        }
        explica.text = texto;
    }
}

// ---------------------------------------------------------------------------------------------- síntesis

// Transcripción in vitro: los 3 ingredientes (con su marca) y, al incubar, la T7 recorriendo el molde de ADN
// mientras salen hebras de ARNm.
public class DiagramaSintesis : MonoBehaviour
{
    static readonly string[] Ids = { "molde", "t7", "ntp" };
    Renderer[] fichas;
    TMP_Text[] marcas;
    Transform enzima;
    readonly List<Transform> hebras = new List<Transform>();
    TMP_Text estado;
    public bool Animando { get; private set; }

    public static DiagramaSintesis Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "DiagramaSintesis", pos);
        var d = g.gameObject.AddComponent<DiagramaSintesis>();
        d.Montar();
        return d;
    }

    void Montar()
    {
        fichas = new Renderer[3]; marcas = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            var p = System.Array.Find(C.ReactivosSintesis, x => x.id == Ids[i]);
            float x = -0.42f + i * 0.42f;
            fichas[i] = K.Caja(transform, "Ficha" + i, new Vector3(x, 0.4f, -0.006f), new Vector3(0.36f, 0.1f, 0.002f), K.Plano(Color.white));
            K.Texto(transform, $"<b>{p.nombre}</b>\n<size=75%>{p.sub}</size>", new Vector3(x, 0.4f, -0.009f), 0.17f, new Color(0.05f, 0.07f, 0.1f), 0.34f, 0.09f, TextAlignmentOptions.Center);
            marcas[i] = K.Texto(transform, "", new Vector3(x, 0.32f, -0.009f), 0.18f, K.Verde, 0.36f, 0.05f, TextAlignmentOptions.Center, true);
        }
        // Molde de ADN (doble hebra) y la enzima T7.
        var azul = K.Plano(new Color(0.45f, 0.75f, 1f));
        K.Caja(transform, "ADN1", new Vector3(0f, -0.02f, -0.005f), new Vector3(1.1f, 0.012f, 0.002f), azul);
        K.Caja(transform, "ADN2", new Vector3(0f, -0.06f, -0.005f), new Vector3(1.1f, 0.012f, 0.002f), azul);
        for (int i = 0; i < 22; i++) K.Caja(transform, "Par", new Vector3(-0.52f + i * 0.05f, -0.04f, -0.005f), new Vector3(0.006f, 0.04f, 0.002f), azul);
        K.Texto(transform, "molde de ADN", new Vector3(0.62f, -0.04f, -0.006f), 0.15f, new Color(0.45f, 0.75f, 1f), 0.2f, 0.06f, TextAlignmentOptions.Left, true);
        enzima = K.Grupo(transform, "T7", new Vector3(-0.55f, -0.04f, -0.012f));
        K.Esfera(enzima, "Cuerpo", Vector3.zero, 0.1f, K.Plano(new Color(0.75f, 0.45f, 1f)));
        K.Texto(enzima, "T7", new Vector3(0f, 0f, -0.06f), 0.18f, Color.white, 0.1f, 0.05f, TextAlignmentOptions.Center, true);
        estado = K.Texto(transform, "", new Vector3(0f, -0.3f, -0.006f), 0.2f, K.Cian, 1.3f, 0.12f, TextAlignmentOptions.Center, true);
        Reiniciar();
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        Animando = false;
        for (int i = 0; i < 3; i++) Marcar(Ids[i], false);
        foreach (var h in hebras) if (h != null) Destroy(h.gameObject);
        hebras.Clear();
        enzima.gameObject.SetActive(false);
        estado.text = "Tubo de reacción: vacío";
    }

    public void Marcar(string id, bool puesto)
    {
        int i = System.Array.IndexOf(Ids, id);
        if (i < 0) return;
        K.Pintar(fichas[i], puesto ? System.Array.Find(C.ReactivosSintesis, x => x.id == id).color : new Color(0.35f, 0.4f, 0.48f));
        marcas[i].text = puesto ? "EN EL TUBO" : "";
    }

    public void Estado(string s) => estado.text = s;

    public void Incubar(System.Action alTerminar) => StartCoroutine(Animar(alTerminar));

    IEnumerator Animar(System.Action alTerminar)
    {
        Animando = true;
        enzima.gameObject.SetActive(true);
        var naranja = K.Plano(new Color(1f, 0.6f, 0.15f));
        const float dur = 3f;
        float t = 0f;
        Sonidos.Camara(transform.position);
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Repeat(t / 1f, 1f);
            enzima.localPosition = new Vector3(Mathf.Lerp(-0.52f, 0.52f, k), -0.04f, -0.012f);
            // La enzima va dejando hebras de ARNm nuevas, que suben.
            int esperadas = Mathf.Min(6, 1 + (int)(t / dur * 6f));
            while (hebras.Count < esperadas)
            {
                var h = K.Grupo(transform, "ARNm", new Vector3(-0.45f + hebras.Count * 0.18f, 0.05f, -0.01f));
                K.Caja(h, "Hebra", new Vector3(0f, 0.08f, 0f), new Vector3(0.012f, 0.16f, 0.002f), naranja);
                h.localScale = new Vector3(1f, 0.01f, 1f);
                hebras.Add(h);
            }
            foreach (var h in hebras) h.localScale = new Vector3(1f, Mathf.MoveTowards(h.localScale.y, 1f, Time.deltaTime * 1.5f), 1f);
            int barras = Mathf.RoundToInt(t / dur * 12f);
            estado.text = $"TRANSCRIPCIÓN · 37 °C · 2 h\n<color=#39FF88>{new string('|', barras)}</color><color=#4A6278>{new string('|', 12 - barras)}</color>";
            yield return null;
        }
        foreach (var h in hebras) h.localScale = Vector3.one;
        enzima.gameObject.SetActive(false);
        estado.text = "<color=#FFB040>ARNm de la espiga: millones de copias</color> · la DNasa elimina el molde de ADN";
        Animando = false;
        alTerminar?.Invoke();
    }
}

// ---------------------------------------------------------------------------------------------- nanopartícula

// Corte de una nanopartícula lipídica: cada tipo de lípido aparece al encajar su cartucho y el ARNm al poner el tubo.
public class DiagramaLNP : MonoBehaviour
{
    static readonly string[] Ids = { "ionizable", "fosfo", "colesterol", "peg" };
    readonly GameObject[] grupos = new GameObject[4];
    GameObject arnm;
    Transform particula;
    TMP_Text[] leyenda = new TMP_Text[4];
    TMP_Text estado;
    readonly List<Transform> copias = new List<Transform>();
    public bool Animando { get; private set; }

    public static DiagramaLNP Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "DiagramaLNP", pos);
        var d = g.gameObject.AddComponent<DiagramaLNP>();
        d.Montar();
        return d;
    }

    void Montar()
    {
        particula = K.Grupo(transform, "Particula", new Vector3(-0.25f, 0.07f, -0.02f));
        var col = new Color[4];
        for (int i = 0; i < 4; i++) col[i] = System.Array.Find(C.Lipidos, x => x.id == Ids[i]).color;

        // Contorno tenue para que se vea la forma aunque falten lípidos.
        K.Pieza(particula, "Contorno", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.01f), new Vector3(0.46f, 0.001f, 0.46f), K.Plano(new Color(0.12f, 0.18f, 0.26f)), Quaternion.Euler(90f, 0f, 0f));

        // 0 ionizable: en el núcleo, junto al ARNm.
        var g0 = new List<Renderer>();
        for (int i = 0; i < 18; i++)
        {
            float a = i * 2.4f, r = 0.04f + 0.1f * ((i * 37) % 10) / 10f;
            g0.Add(K.Esfera(particula, "Ionizable", new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f), 0.026f, K.Plano(col[0])));
        }
        // 1 fosfolípido: el anillo exterior (cabeza + cola hacia dentro).
        var g1 = new List<Renderer>();
        for (int i = 0; i < 26; i++)
        {
            float a = i * Mathf.PI * 2f / 26f;
            var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            if (i % 4 == 0) continue;   // huecos para el PEG
            g1.Add(K.Esfera(particula, "Cabeza", d * 0.2f, 0.03f, K.Plano(col[1])));
            g1.Add(K.Pieza(particula, "Cola", PrimitiveType.Cube, d * 0.175f, new Vector3(0.006f, 0.03f, 0.004f), K.Plano(new Color(0.75f, 0.75f, 0.75f)), Quaternion.FromToRotation(Vector3.up, d)));
        }
        // 2 colesterol: entre las colas.
        var g2 = new List<Renderer>();
        for (int i = 0; i < 13; i++)
        {
            float a = (i + 0.5f) * Mathf.PI * 2f / 13f;
            g2.Add(K.Pieza(particula, "Colesterol", PrimitiveType.Cube, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.155f, new Vector3(0.012f, 0.022f, 0.004f), K.Plano(col[2]), Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg)));
        }
        // 3 PEG: cabezas con un "pelo" hacia fuera.
        var g3 = new List<Renderer>();
        for (int i = 0; i < 26; i += 4)
        {
            float a = i * Mathf.PI * 2f / 26f;
            var d = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            g3.Add(K.Esfera(particula, "CabezaPEG", d * 0.2f, 0.03f, K.Plano(col[3])));
            g3.Add(K.Pieza(particula, "PEG", PrimitiveType.Cube, d * 0.245f, new Vector3(0.008f, 0.06f, 0.004f), K.Plano(col[3]), Quaternion.FromToRotation(Vector3.up, d)));
        }
        var listas = new[] { g0, g1, g2, g3 };
        for (int i = 0; i < 4; i++)
        {
            var grupo = K.Grupo(particula, "Lipido_" + Ids[i], Vector3.zero);
            foreach (var r in listas[i]) r.transform.SetParent(grupo, true);
            grupos[i] = grupo.gameObject;
        }

        // ARNm enrollado en el núcleo.
        var a2 = K.Grupo(particula, "ARNm", Vector3.zero);
        for (int i = 0; i < 34; i++)
        {
            float t = i / 34f * Mathf.PI * 5f, r = 0.02f + 0.09f * i / 34f;
            K.Esfera(a2, "Nucleotido", new Vector3(Mathf.Cos(t) * r, Mathf.Sin(t) * r, -0.004f), 0.016f, K.Plano(new Color(1f, 0.6f, 0.15f)));
        }
        arnm = a2.gameObject;

        // Leyenda.
        for (int i = 0; i < 4; i++)
        {
            var p = System.Array.Find(C.Lipidos, x => x.id == Ids[i]);
            K.Caja(transform, "Color", new Vector3(0.12f, 0.3f - i * 0.11f, -0.006f), new Vector3(0.04f, 0.04f, 0.002f), K.Plano(p.color));
            leyenda[i] = K.Texto(transform, "", new Vector3(0.43f, 0.3f - i * 0.11f, -0.006f), 0.17f, Color.white, 0.55f, 0.1f, TextAlignmentOptions.Left);
            K.AutoTam(leyenda[i], 0.12f);
        }
        K.Caja(transform, "ColorARN", new Vector3(0.12f, 0.3f - 4 * 0.11f, -0.006f), new Vector3(0.04f, 0.04f, 0.002f), K.Plano(new Color(1f, 0.6f, 0.15f)));
        leyenda4 = K.Texto(transform, "", new Vector3(0.43f, 0.3f - 4 * 0.11f, -0.006f), 0.17f, Color.white, 0.55f, 0.1f, TextAlignmentOptions.Left);
        estado = K.Texto(transform, "", new Vector3(0f, -0.3f, -0.006f), 0.2f, K.Cian, 1.3f, 0.1f, TextAlignmentOptions.Center, true);
        Reiniciar();
    }

    TMP_Text leyenda4;

    public void Reiniciar()
    {
        StopAllCoroutines();
        Animando = false;
        for (int i = 0; i < 4; i++) Lipido(Ids[i], false);
        Arnm(false);
        foreach (var c in copias) if (c != null) Destroy(c.gameObject);
        copias.Clear();
        particula.localScale = Vector3.one;
        estado.text = "Corte de una nanopartícula lipídica (~80 nm)";
    }

    public void Lipido(string id, bool puesto)
    {
        int i = System.Array.IndexOf(Ids, id);
        if (i < 0) return;
        grupos[i].SetActive(puesto);
        var p = System.Array.Find(C.Lipidos, x => x.id == id);
        leyenda[i].text = puesto ? $"<b>{p.nombre}</b> <color=#39FF88>OK</color>\n<size=80%>{p.explica.Substring(p.explica.IndexOf(':') + 1).Trim()}</size>" : $"<color=#6A7E92>{p.nombre} (falta)</color>";
    }

    public void Arnm(bool puesto)
    {
        arnm.SetActive(puesto);
        leyenda4.text = puesto ? "<b>ARNm de la espiga</b> <color=#39FF88>OK</color>" : "<color=#6A7E92>ARNm (falta el tubo)</color>";
    }

    public void Mezclar(System.Action alTerminar) => StartCoroutine(Animar(alTerminar));

    IEnumerator Animar(System.Action alTerminar)
    {
        Animando = true;
        Sonidos.Camara(transform.position);
        const float dur = 2.6f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            int barras = Mathf.RoundToInt(t / dur * 12f);
            estado.text = $"MEZCLA MICROFLUÍDICA: ARNm (agua, pH 4) + lípidos (etanol)\n<color=#39FF88>{new string('|', barras)}</color><color=#4A6278>{new string('|', 12 - barras)}</color>";
            particula.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 10f));
            yield return null;
        }
        particula.localScale = Vector3.one;
        // Muchas partículas iguales alrededor: no es una, son billones.
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f;
            var c = Instantiate(particula, particula.parent);
            c.localPosition = particula.localPosition + new Vector3(Mathf.Cos(a) * 0.33f, Mathf.Sin(a) * 0.24f, 0.01f);
            c.localScale = Vector3.one * 0.28f;
            copias.Add(c);
            yield return new WaitForSeconds(0.06f);
        }
        estado.text = "<color=#39FF88>Billones de nanopartículas con tu ARNm dentro</color>";
        Animando = false;
        alTerminar?.Invoke();
    }
}

// ---------------------------------------------------------------------------------------------- calidad

// Tabla del análisis del lote y estado de conservación.
public class PanelCalidad : MonoBehaviour
{
    TMP_Text tabla, estado;
    public bool Animando { get; private set; }

    public static PanelCalidad Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "PanelCalidad", pos);
        var p = g.gameObject.AddComponent<PanelCalidad>();
        p.tabla = K.Texto(g, "", new Vector3(0f, 0.42f, -0.006f), 0.22f, Color.white, 1.3f, 0.5f, TextAlignmentOptions.TopLeft, false, true);
        p.estado = K.Texto(g, "", new Vector3(0f, -0.2f, -0.006f), 0.24f, K.Cian, 1.3f, 0.2f, TextAlignmentOptions.Center, true);
        p.Reiniciar();
        return p;
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        Animando = false;
        tabla.text = Tabla(0);
        estado.text = "LOTE 001 · SIN ANALIZAR\n<size=80%><color=#A9BED2>Pon el vial en el analizador</color></size>";
    }

    static string Tabla(int filas)
    {
        var sb = new System.Text.StringBuilder("<b><color=#8FD8FF>PARÁMETRO                          RESULTADO     ESPECIFICACIÓN</color></b>\n");
        for (int i = 0; i < C.Analisis.Length; i++)
        {
            var c = C.Analisis[i].Split('|');
            sb.Append(i < filas
                ? $"{c[0]}<pos=55%>{c[1]}<pos=75%>{c[2]}  <color=#39FF88><b>OK</b></color>\n"
                : $"<color=#6A7E92>{c[0]}<pos=55%>…<pos=75%>{c[2]}</color>\n");
        }
        return sb.ToString();
    }

    public void Analizar(System.Action alTerminar) => StartCoroutine(Animar(alTerminar));

    IEnumerator Animar(System.Action alTerminar)
    {
        Animando = true;
        Sonidos.Scan(transform.position);
        for (int i = 1; i <= C.Analisis.Length; i++)
        {
            estado.text = "ANALIZANDO LOTE 001…";
            yield return new WaitForSeconds(0.45f);
            tabla.text = Tabla(i);
            Sonidos.Clic(transform.position);
        }
        estado.text = "<color=#39FF88>LOTE 001 · APROBADO</color>\n<size=80%><color=#A9BED2>Ahora guárdalo donde el ARNm no se estropee</color></size>";
        Animando = false;
        alTerminar?.Invoke();
    }

    public void Estado(string s) => estado.text = s;
}

// ---------------------------------------------------------------------------------------------- siguiente paso

// Presentación del paso al pulsar CONTINUAR: ocupa la pantalla derecha unos segundos y muestra qué viene, dónde se
// hace y el plan de acciones, que se va AGREGANDO línea a línea (con un clic). Después vuelve el diagrama del paso;
// también se cierra en cuanto el estudiante empieza a trabajar.
public class PanelSiguiente : MonoBehaviour
{
    const int Max = 6;
    TMP_Text paso, titulo, donde, pie;
    Transform[] filas;
    TMP_Text[] textos;
    float[] pop;
    public bool Completo { get; private set; }

    public static PanelSiguiente Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "SiguientePaso", pos);
        var p = g.gameObject.AddComponent<PanelSiguiente>();
        p.Montar();
        g.gameObject.SetActive(false);
        return p;
    }

    void Montar()
    {
        paso = K.Texto(transform, "", new Vector3(0f, 0.5f, -0.01f), 0.26f, K.Amarillo, 1.3f, 0.06f, TextAlignmentOptions.Center, true);
        titulo = K.Texto(transform, "", new Vector3(0f, 0.415f, -0.01f), 0.4f, Color.white, 1.36f, 0.1f, TextAlignmentOptions.Center, true);
        K.AutoTam(titulo, 0.28f);
        donde = K.Texto(transform, "", new Vector3(0f, 0.33f, -0.01f), 0.24f, K.Cian, 1.3f, 0.06f, TextAlignmentOptions.Center);
        K.Caja(transform, "Linea", new Vector3(0f, 0.285f, -0.006f), new Vector3(1.2f, 0.004f, 0.002f), K.Plano(new Color(0.3f, 0.45f, 0.6f)));
        filas = new Transform[Max]; textos = new TMP_Text[Max]; pop = new float[Max];
        for (int i = 0; i < Max; i++)
        {
            float y = 0.215f - i * 0.11f;
            var f = K.Grupo(transform, "Fila" + i, new Vector3(0f, y, -0.01f));
            K.Pieza(f, "Circulo", PrimitiveType.Cylinder, new Vector3(-0.6f, 0f, 0f), new Vector3(0.08f, 0.001f, 0.08f), K.Plano(K.Cian), Quaternion.Euler(90f, 0f, 0f));
            K.Texto(f, (i + 1).ToString(), new Vector3(-0.6f, 0f, -0.004f), 0.3f, new Color(0.04f, 0.08f, 0.14f), 0.08f, 0.07f, TextAlignmentOptions.Center, true);
            textos[i] = K.Texto(f, "", new Vector3(0.04f, 0f, -0.002f), 0.26f, Color.white, 1.17f, 0.1f, TextAlignmentOptions.Left);
            K.AutoTam(textos[i], 0.19f);
            filas[i] = f;
        }
        pie = K.Texto(transform, "", new Vector3(0f, -0.46f, -0.01f), 0.24f, K.Verde, 1.3f, 0.06f, TextAlignmentOptions.Center, true);
    }

    public void Mostrar(string encabezado, string tituloPaso, string dondeTxt, string[] lineas)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(Animar(encabezado, tituloPaso, dondeTxt, lineas));
    }

    public void Ocultar()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    IEnumerator Animar(string encabezado, string tituloPaso, string dondeTxt, string[] lineas)
    {
        Completo = false;
        paso.text = encabezado;
        titulo.text = tituloPaso;
        donde.text = dondeTxt;
        pie.text = "";
        for (int i = 0; i < Max; i++) filas[i].gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        for (int i = 0; i < lineas.Length && i < Max; i++)
        {
            textos[i].text = lineas[i];
            filas[i].gameObject.SetActive(true);
            pop[i] = 1f;
            Sonidos.Clic(transform.position);
            yield return new WaitForSeconds(0.45f);
        }
        pie.text = "¡Manos a la obra! La flecha amarilla de la mesa te marca cada acción.";
        Completo = true;
    }

    void Update()
    {
        for (int i = 0; i < Max; i++)
        {
            if (pop[i] <= 0f) continue;
            pop[i] = Mathf.MoveTowards(pop[i], 0f, Time.deltaTime * 3f);
            filas[i].localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(pop[i] * Mathf.PI));
        }
    }
}
