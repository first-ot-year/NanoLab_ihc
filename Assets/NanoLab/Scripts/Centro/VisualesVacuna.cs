using System.Collections;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// Visuales de la pantalla derecha de la estación de la vacuna (virus, placa, fases y eficacia; los diagramas
// de fabricación están en DiagramasVacuna). Todo se construye con primitivas en tiempo de
// ejecución y se lee desde -z local. El color siempre va acompañado de forma o texto (RNF-03).

// ---------------------------------------------------------------------------------------------- virus

// Modelo del coronavirus del brote: envoltura transparente, espigas S, ARN y proteína N dentro.
// Resalta la parte de cada opción del paso ANTÍGENO y muestra los anticuerpos neutralizantes pegándose a las espigas.
public class VirusVacuna : MonoBehaviour
{
    public enum Parte { Arn = 0, Espiga = 1, Envoltura = 2, ProteinaN = 3, Ninguna = -1 }

    public static readonly Color ColorEspiga = new Color(0.95f, 0.28f, 0.3f);
    public static readonly Color ColorEnvoltura = new Color(0.55f, 0.8f, 1f);
    public static readonly Color ColorArn = new Color(1f, 0.6f, 0.15f);
    public static readonly Color ColorN = new Color(0.98f, 0.9f, 0.35f);
    public static readonly Color ColorAnticuerpo = new Color(0.35f, 1f, 0.55f);

    Transform modelo, etiquetas;
    Renderer envoltura;
    readonly System.Collections.Generic.List<Renderer>[] partes =
    {
        new System.Collections.Generic.List<Renderer>(), new System.Collections.Generic.List<Renderer>(),
        new System.Collections.Generic.List<Renderer>(), new System.Collections.Generic.List<Renderer>(),
    };
    TMP_Text[] rotulos = new TMP_Text[4];
    Color[] colores;
    Transform[] anticuerpos;
    Vector3[] destinos;
    Vector3 posBase;
    float escalaBase = 1f;
    Parte resaltada = Parte.Ninguna;
    Parte? pintada;
    float tAnticuerpos = -1f;
    Vector3 posObjetivo;
    float escalaObjetivo = 1f;

    public static VirusVacuna Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "VirusDelBrote", pos);
        var v = g.gameObject.AddComponent<VirusVacuna>();
        v.Montar();
        return v;
    }

    void Montar()
    {
        posBase = posObjetivo = transform.localPosition;
        colores = new[] { ColorArn, ColorEspiga, ColorEnvoltura, ColorN };
        modelo = K.Grupo(transform, "Modelo", new Vector3(0f, 0f, -0.28f));
        modelo.localRotation = Quaternion.Euler(15f, 0f, 0f);
        const float r = 0.2f;

        envoltura = K.Esfera(modelo, "Envoltura", Vector3.zero, r * 2f, K.Vidrio(new Color(0.55f, 0.8f, 1f, 0.28f)));
        partes[(int)Parte.Envoltura].Add(envoltura);
        // Proteínas M de la membrana (puntos oscuros en la envoltura).
        var puntos = Fibonacci(40);
        for (int i = 0; i < puntos.Length; i += 2)
            partes[(int)Parte.Envoltura].Add(K.Esfera(modelo, "ProteinaM", puntos[i] * r * 0.99f, 0.018f, K.Plano(new Color(0.35f, 0.55f, 0.8f))));

        // Espigas S (tallo + cabeza): en la superficie, la "llave" del virus.
        var dirs = Fibonacci(22);
        destinos = new Vector3[8];
        for (int i = 0; i < dirs.Length; i++)
        {
            var d = dirs[i];
            var rot = Quaternion.FromToRotation(Vector3.up, d);
            partes[(int)Parte.Espiga].Add(K.Pieza(modelo, "Tallo", PrimitiveType.Cylinder, d * (r + 0.018f), new Vector3(0.014f, 0.022f, 0.014f), K.Plano(ColorEspiga), rot));
            partes[(int)Parte.Espiga].Add(K.Pieza(modelo, "Cabeza", PrimitiveType.Sphere, d * (r + 0.05f), new Vector3(0.05f, 0.036f, 0.05f), K.Plano(ColorEspiga), rot));
            if (i < destinos.Length) destinos[i] = d * (r + 0.085f);
        }

        // ARN enrollado y proteína N pegada a él (nucleocápside).
        const int n = 70;
        for (int i = 0; i < n; i++)
        {
            float th = Mathf.PI * (i + 0.5f) / n, ph = i * 0.75f;
            var p = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.11f;
            partes[(int)Parte.Arn].Add(K.Esfera(modelo, "ARN", p, 0.016f, K.Plano(ColorArn)));
            if (i % 4 == 0) partes[(int)Parte.ProteinaN].Add(K.Esfera(modelo, "ProteinaN", p * 1.12f, 0.03f, K.Plano(ColorN)));
        }

        // Una malla por parte (de ~180 piezas a 5 llamadas de dibujo); la envoltura transparente va aparte.
        string[] nombresParte = { "ARN", "Espigas", "ProteinasM", "ProteinaN" };
        for (int i = 0; i < partes.Length; i++)
        {
            var piezas = partes[i].FindAll(x => x != envoltura);
            var unida = K.Combinar(modelo, nombresParte[i], piezas);
            partes[i].RemoveAll(x => x != envoltura);
            if (unida != null) partes[i].Add(unida);
        }

        // Anticuerpos (forma de Y): aparecen cuando el antígeno correcto se ha elegido.
        anticuerpos = new Transform[destinos.Length];
        for (int i = 0; i < anticuerpos.Length; i++)
        {
            var a = K.Grupo(modelo, "Anticuerpo", destinos[i] * 3f, Quaternion.FromToRotation(Vector3.down, destinos[i].normalized));
            var m = K.Plano(ColorAnticuerpo);
            K.Caja(a, "Tronco", new Vector3(0f, -0.018f, 0f), new Vector3(0.009f, 0.03f, 0.009f), m);
            K.Pieza(a, "BrazoI", PrimitiveType.Cube, new Vector3(-0.009f, 0.008f, 0f), new Vector3(0.008f, 0.026f, 0.008f), m, Quaternion.Euler(0f, 0f, 35f));
            K.Pieza(a, "BrazoD", PrimitiveType.Cube, new Vector3(0.009f, 0.008f, 0f), new Vector3(0.008f, 0.026f, 0.008f), m, Quaternion.Euler(0f, 0f, -35f));
            a.gameObject.SetActive(false);
            anticuerpos[i] = a;
        }

        // Rótulos con el color de cada parte (y un cuadrado del mismo color, para no depender solo del texto).
        etiquetas = K.Grupo(transform, "Etiquetas", Vector3.zero);
        string[] nombres = { "ARN (genoma)", "Espiga S", "Envoltura lipídica", "Proteína N" };
        Vector3[] pos = { new Vector3(-0.47f, 0.14f, -0.01f), new Vector3(0.47f, 0.22f, -0.01f), new Vector3(0.47f, -0.16f, -0.01f), new Vector3(-0.47f, -0.08f, -0.01f) };
        for (int i = 0; i < 4; i++)
        {
            K.Caja(etiquetas, "Color", pos[i] + new Vector3(i == 0 || i == 3 ? 0.17f : -0.17f, 0f, 0f), new Vector3(0.03f, 0.03f, 0.004f), K.Plano(colores[i]));
            rotulos[i] = K.Texto(etiquetas, nombres[i], pos[i], 0.24f, colores[i], 0.3f, 0.06f, i == 0 || i == 3 ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, true);
            rotulos[i].rectTransform.localPosition = pos[i] + new Vector3(i == 0 || i == 3 ? -0.03f : 0.03f, 0f, 0f);
        }
    }

    static Vector3[] Fibonacci(int n)
    {
        var r = new Vector3[n];
        float oro = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int i = 0; i < n; i++)
        {
            float y = 1f - (i + 0.5f) * 2f / n, rad = Mathf.Sqrt(1f - y * y), th = oro * i;
            r[i] = new Vector3(Mathf.Cos(th) * rad, y, Mathf.Sin(th) * rad);
        }
        return r;
    }

    public void Colocar(Vector3 desplazamiento, float escala, bool conEtiquetas)
    {
        posObjetivo = posBase + desplazamiento;
        escalaObjetivo = escala;
        etiquetas.gameObject.SetActive(conEtiquetas);
    }

    public void Resaltar(Parte p) => resaltada = p;

    // Rótulo de una parte (para ponerle el selector del paso ANTÍGENO).
    public Transform Rotulo(int i) => rotulos[i].transform;

    public void Anticuerpos(bool si)
    {
        if (si && tAnticuerpos < 0f) tAnticuerpos = 0f;
        if (!si) tAnticuerpos = -1f;
        foreach (var a in anticuerpos) a.gameObject.SetActive(si);
    }

    void Update()
    {
        modelo.Rotate(0f, 18f * Time.deltaTime, 0f, Space.Self);
        transform.localPosition = Vector3.Lerp(transform.localPosition, posObjetivo, Time.deltaTime * 4f);
        escalaBase = Mathf.Lerp(escalaBase, escalaObjetivo, Time.deltaTime * 4f);
        transform.localScale = Vector3.one * escalaBase;

        // Parte resaltada: parpadea más clara; el resto se atenúa. Solo se repinta lo que cambia.
        float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
        bool cambio = resaltada != pintada;
        for (int i = 0; i < partes.Length; i++)
        {
            bool si = (int)resaltada == i;
            if (!cambio && !si) continue;
            bool otra = resaltada != Parte.Ninguna && !si;
            var c = colores[i];
            var col = si ? Color.Lerp(c, Color.white, 0.25f + 0.45f * pulso) : otra ? Color.Lerp(c, new Color(0.2f, 0.25f, 0.3f), 0.55f) : c;
            foreach (var r in partes[i])
                K.Pintar(r, r == envoltura ? new Color(col.r, col.g, col.b, si ? 0.45f : 0.28f) : new Color(col.r, col.g, col.b, 1f));
            rotulos[i].fontSize = si ? 0.3f : 0.24f;
            rotulos[i].color = otra ? new Color(c.r, c.g, c.b, 0.5f) : c;
        }
        pintada = resaltada;

        if (tAnticuerpos >= 0f)
        {
            tAnticuerpos += Time.deltaTime;
            for (int i = 0; i < anticuerpos.Length; i++)
            {
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((tAnticuerpos - i * 0.15f) / 1.2f));
                anticuerpos[i].localPosition = Vector3.Lerp(destinos[i] * 3f, destinos[i], k);
            }
        }
    }
}

// ---------------------------------------------------------------------------------------------- neutralización

// Placa de cultivo del ensayo de neutralización (vista ampliada de 3 filas × 8 diluciones).
// Fila 0: suero de ratones VACUNADOS · fila 1: suero PLACEBO · fila 2: CONTROL de células (sin virus).
public class PlacaNeutralizacion : MonoBehaviour
{
    public const int Filas = 3, Columnas = 8, ColumnaTitulo = 5;   // protege hasta 1:320
    public static readonly string[] Diluciones = { "1:10", "1:20", "1:40", "1:80", "1:160", "1:320", "1:640", "1:1280" };

    static readonly Color Vacio = new Color(0.16f, 0.2f, 0.26f);
    static readonly Color Suero = new Color(1f, 0.9f, 0.55f);
    static readonly Color Medio = new Color(0.95f, 0.72f, 0.75f);
    static readonly Color ConCelulas = new Color(0.95f, 0.5f, 0.58f);
    static readonly Color Violeta = new Color(0.5f, 0.22f, 0.82f);
    static readonly Color Transparente = new Color(0.84f, 0.88f, 0.92f);
    static readonly Color VirusLibre = new Color(1f, 0.15f, 0.15f);
    static readonly Color VirusNeutralizado = new Color(0.35f, 0.75f, 0.45f);

    Renderer[] relleno;
    Renderer[] virus;
    GameObject[] cruces;
    GameObject marco;
    TMP_Text estado;
    public int Pasos { get; private set; }
    public bool Animando { get; private set; }

    public static bool Vive(int f, int c) => f == 2 || (f == 0 && c <= ColumnaTitulo);
    public static bool Neutraliza(int f, int c) => f == 0 && c <= ColumnaTitulo;

    public static PlacaNeutralizacion Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "PlacaNeutralizacion", pos);
        var p = g.gameObject.AddComponent<PlacaNeutralizacion>();
        p.Montar();
        return p;
    }

    static Vector3 Centro(int f, int c) => new Vector3(-0.33f + c * 0.13f, 0.17f - f * 0.15f, 0f);

    void Montar()
    {
        int n = Filas * Columnas;
        relleno = new Renderer[n]; virus = new Renderer[n]; cruces = new GameObject[n];
        K.Panel(transform, "Placa", new Vector3(0.12f, 0.06f, 0f), 1.18f, 0.6f, new Color(0.85f, 0.9f, 0.95f, 0.15f), new Color(0.75f, 0.85f, 0.95f, 0.6f), 0.004f, 0.04f);
        string[] filas = { "VACUNADO", "PLACEBO", "CONTROL\n<size=75%>(sin virus)</size>" };
        for (int f = 0; f < Filas; f++)
        {
            var t = K.Texto(transform, filas[f], new Vector3(-0.58f, Centro(f, 0).y, -0.005f), 0.2f, f == 0 ? new Color(0.45f, 0.75f, 1f) : f == 1 ? new Color(1f, 0.7f, 0.35f) : K.Gris, 0.24f, 0.1f, TextAlignmentOptions.Right, true);
            t.rectTransform.localPosition = new Vector3(-0.53f, Centro(f, 0).y, -0.005f);
            for (int c = 0; c < Columnas; c++)
            {
                int i = f * Columnas + c;
                var p = Centro(f, c);
                var giro = Quaternion.Euler(90f, 0f, 0f);
                K.Pieza(transform, "Borde", PrimitiveType.Cylinder, p + new Vector3(0f, 0f, -0.003f), new Vector3(0.112f, 0.002f, 0.112f), K.Plano(new Color(0.55f, 0.62f, 0.7f)), giro);
                relleno[i] = K.Pieza(transform, "Pocillo", PrimitiveType.Cylinder, p + new Vector3(0f, 0f, -0.006f), new Vector3(0.098f, 0.002f, 0.098f), K.Plano(Color.white), giro);
                virus[i] = K.Esfera(transform, "Virus", p + new Vector3(0f, 0f, -0.02f), 0.032f, K.Plano(Color.white));
                var cruz = K.Grupo(transform, "Cruz", p + new Vector3(0f, 0f, -0.012f));
                foreach (var s in new[] { 1f, -1f })
                    K.Pieza(cruz, "Trazo", PrimitiveType.Cube, Vector3.zero, new Vector3(0.085f, 0.012f, 0.003f), K.Plano(new Color(0.85f, 0.15f, 0.15f)), Quaternion.Euler(0f, 0f, 45f * s));
                cruces[i] = cruz.gameObject;
            }
        }
        for (int c = 0; c < Columnas; c++)
            K.Texto(transform, Diluciones[c], new Vector3(Centro(0, c).x, 0.27f, -0.005f), 0.19f, Color.white, 0.13f, 0.05f, TextAlignmentOptions.Center, true);
        K.Texto(transform, "dilución del suero  >  cada vez MENOS anticuerpos", new Vector3(0.12f, 0.32f, -0.005f), 0.17f, K.Gris, 1.1f, 0.05f);

        // Marco amarillo en la columna del título (se muestra al resolver la lectura).
        var m = K.Grupo(transform, "MarcoTitulo", new Vector3(Centro(0, ColumnaTitulo).x, Centro(1, 0).y, -0.008f));
        var amarillo = K.Plano(K.Amarillo);
        K.Caja(m, "Arriba", new Vector3(0f, 0.235f, 0f), new Vector3(0.13f, 0.008f, 0.002f), amarillo);
        K.Caja(m, "Abajo", new Vector3(0f, -0.235f, 0f), new Vector3(0.13f, 0.008f, 0.002f), amarillo);
        K.Caja(m, "Izq", new Vector3(-0.065f, 0f, 0f), new Vector3(0.008f, 0.47f, 0.002f), amarillo);
        K.Caja(m, "Der", new Vector3(0.065f, 0f, 0f), new Vector3(0.008f, 0.47f, 0.002f), amarillo);
        K.Texto(m, "TÍTULO", new Vector3(0f, -0.27f, 0f), 0.17f, K.Amarillo, 0.2f, 0.04f, TextAlignmentOptions.Center, true);
        marco = m.gameObject;

        estado = K.Texto(transform, "", new Vector3(0.12f, -0.36f, -0.005f), 0.22f, K.Cian, 1.2f, 0.06f, TextAlignmentOptions.Center, true);
        Reiniciar();
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        Animando = false;
        Pasos = 0;
        for (int i = 0; i < relleno.Length; i++)
        {
            K.Pintar(relleno[i], Vacio);
            virus[i].gameObject.SetActive(false);
            virus[i].transform.localScale = Vector3.one * 0.032f;
            cruces[i].SetActive(false);
        }
        marco.SetActive(false);
        estado.text = "Placa nueva: 24 pocillos vacíos";
    }

    public void MarcarTitulo(bool si) => marco.SetActive(si);

    // Posición (local) del rótulo de la dilución de una columna (allí va su selector en el paso TÍTULO).
    public Vector3 Cabecera(int c) => new Vector3(Centro(0, c).x, 0.27f, -0.01f);

    // Aplica el paso 'k' del ensayo (0 suero, 1 virus, 2 incubar, 3 células, 4 teñir y leer) con su animación.
    public void Aplicar(int k)
    {
        StopAllCoroutines();
        StartCoroutine(Animar(k));
    }

    IEnumerator Animar(int k)
    {
        Animando = true;
        switch (k)
        {
            case 0:
                estado.text = "Diluciones seriadas del suero (cada pocillo, la mitad que el anterior)";
                for (int c = 0; c < Columnas; c++)
                {
                    for (int f = 0; f < Filas; f++)
                        K.Pintar(relleno[f * Columnas + c], f == 2 ? Medio : Color.Lerp(Suero, Medio, c / (Columnas - 1f) * 0.8f));
                    Sonidos.Gota(transform.position);
                    yield return new WaitForSeconds(0.1f);
                }
                break;
            case 1:
                estado.text = "Misma cantidad de virus en las filas VACUNADO y PLACEBO";
                for (int c = 0; c < Columnas; c++)
                {
                    for (int f = 0; f < 2; f++)
                    {
                        var v = virus[f * Columnas + c];
                        v.gameObject.SetActive(true);
                        K.Pintar(v, VirusLibre);
                    }
                    yield return new WaitForSeconds(0.08f);
                }
                break;
            case 2:
                yield return Incubar("INCUBANDO suero + virus · 37 °C · 1 h", 1.6f);
                for (int c = 0; c <= ColumnaTitulo; c++)
                {
                    var v = virus[c];
                    K.Pintar(v, VirusNeutralizado);
                    v.transform.localScale = Vector3.one * 0.022f;
                    yield return new WaitForSeconds(0.08f);
                }
                estado.text = "Puntos verdes: virus cubiertos de anticuerpos (neutralizados) · rojos: virus libres";
                break;
            case 3:
                estado.text = "Células Vero en todos los pocillos";
                for (int c = 0; c < Columnas; c++)
                {
                    for (int f = 0; f < Filas; f++) K.Pintar(relleno[f * Columnas + c], ConCelulas);
                    Sonidos.Gota(transform.position);
                    yield return new WaitForSeconds(0.08f);
                }
                break;
            case 4:
                yield return Incubar("INCUBANDO 3 días · 37 °C · el virus libre destruye las células", 2.2f);
                estado.text = "Tinción con CRISTAL VIOLETA…";
                yield return new WaitForSeconds(0.5f);
                for (int c = 0; c < Columnas; c++)
                {
                    for (int f = 0; f < Filas; f++)
                    {
                        int i = f * Columnas + c;
                        virus[i].gameObject.SetActive(false);
                        bool vive = Vive(f, c);
                        K.Pintar(relleno[i], vive ? Violeta : Transparente);
                        cruces[i].SetActive(!vive);
                    }
                    yield return new WaitForSeconds(0.1f);
                }
                estado.text = "LECTURA LISTA · violeta = células vivas · transparente con X = destruidas";
                break;
        }
        Pasos = Mathf.Max(Pasos, k + 1);
        Animando = false;
    }

    IEnumerator Incubar(string texto, float segundos)
    {
        float t = 0f;
        Sonidos.Camara(transform.position);
        while (t < segundos)
        {
            t += Time.deltaTime;
            int barras = Mathf.RoundToInt(t / segundos * 12f);
            estado.text = $"{texto}\n<color=#39FF88>{new string('|', barras)}</color><color=#4A6278>{new string('|', 12 - barras)}</color>";
            yield return null;
        }
    }
}

// ---------------------------------------------------------------------------------------------- fases

// Línea temporal de las fases clínicas: cada tarjeta se rellena al pulsar la fase en el orden correcto.
public class LineaFases : MonoBehaviour
{
    static readonly (string titulo, string quien, string pregunta)[] Tarjetas =
    {
        ("FASE I", "20-100 voluntarios sanos", "¿Es segura?\n¿Qué dosis?"),
        ("FASE II", "cientos de personas", "¿Respuesta inmune?\nDosis y pauta"),
        ("FASE III", "miles de personas", "¿Protege?\nSorteo, placebo y doble ciego"),
        ("APROBACIÓN", "agencia reguladora", "Revisa todos los datos:\nbeneficio frente a riesgo"),
        ("FASE IV", "millones de personas", "Vigilancia de\nefectos muy raros"),
    };

    TMP_Text[] titulos, textos;
    Renderer[] puntos;
    Transform[] tarjetas;
    float[] pop;

    public static LineaFases Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "LineaFases", pos);
        var l = g.gameObject.AddComponent<LineaFases>();
        l.Montar();
        return l;
    }

    void Montar()
    {
        int n = Tarjetas.Length;
        titulos = new TMP_Text[n]; textos = new TMP_Text[n]; puntos = new Renderer[n]; tarjetas = new Transform[n]; pop = new float[n];
        K.Caja(transform, "Linea", new Vector3(0f, -0.3f, -0.004f), new Vector3(1.32f, 0.01f, 0.002f), K.Plano(K.Gris));
        K.Texto(transform, "pocas personas  >>>  millones de personas", new Vector3(0f, -0.36f, -0.005f), 0.18f, K.Gris, 1.3f, 0.05f);
        for (int i = 0; i < n; i++)
        {
            float x = -0.54f + i * 0.27f;
            var t = K.Grupo(transform, "Tarjeta" + i, new Vector3(x, 0.02f, -0.006f));
            K.Panel(t, "Fondo", Vector3.zero, 0.25f, 0.56f, new Color(0.1f, 0.16f, 0.24f, 0.95f), new Color(0.4f, 0.5f, 0.6f, 1f), 0.004f, 0.02f);
            titulos[i] = K.Texto(t, "?", new Vector3(0f, 0.19f, -0.003f), 0.26f, K.Gris, 0.24f, 0.08f, TextAlignmentOptions.Center, true);
            textos[i] = K.Texto(t, "", new Vector3(0f, 0.12f, -0.003f), 0.16f, Color.white, 0.22f, 0.4f, TextAlignmentOptions.Top, false, true);
            puntos[i] = K.Pieza(transform, "Punto", PrimitiveType.Cylinder, new Vector3(x, -0.3f, -0.008f), new Vector3(0.04f, 0.002f, 0.04f), K.Plano(Color.white), Quaternion.Euler(90f, 0f, 0f));
            tarjetas[i] = t;
        }
        Reiniciar();
    }

    public void Reiniciar()
    {
        for (int i = 0; i < tarjetas.Length; i++)
        {
            titulos[i].text = "?";
            titulos[i].color = K.Gris;
            textos[i].text = "";
            K.Pintar(puntos[i], new Color(0.3f, 0.36f, 0.44f));
            pop[i] = 0f;
        }
    }

    public void Rellenar(int i)
    {
        var d = Tarjetas[i];
        titulos[i].text = d.titulo;
        titulos[i].color = K.Verde;
        textos[i].text = $"<color=#8FD8FF>{d.quien}</color>\n\n{d.pregunta}";
        K.Pintar(puntos[i], K.Verde);
        pop[i] = 1f;
    }

    void Update()
    {
        for (int i = 0; i < tarjetas.Length; i++)
        {
            pop[i] = Mathf.MoveTowards(pop[i], 0f, Time.deltaTime * 2.5f);
            tarjetas[i].localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(pop[i] * Mathf.PI));
        }
    }
}

// ---------------------------------------------------------------------------------------------- eficacia

// Barras de casos por grupo, tabla de seguridad y la fórmula de la eficacia (tras responder).
public class GraficaEficacia : MonoBehaviour
{
    Transform barraV, barraP;
    TMP_Text numV, numP, formula;
    float t = -1f;
    const float Alto = 0.5f, Base = -0.28f;

    public static GraficaEficacia Crear(Transform padre, Vector3 pos)
    {
        var g = K.Grupo(padre, "GraficaEficacia", pos);
        var e = g.gameObject.AddComponent<GraficaEficacia>();
        e.Montar();
        return e;
    }

    void Montar()
    {
        K.Caja(transform, "Eje", new Vector3(-0.3f, Base, -0.004f), new Vector3(0.62f, 0.006f, 0.002f), K.Plano(K.Gris));
        barraV = Barra("Vacuna", -0.46f, new Color(0.35f, 0.62f, 1f), "VACUNA\n<size=80%>15 000 personas</size>", out numV);
        barraP = Barra("Placebo", -0.14f, new Color(1f, 0.6f, 0.2f), "PLACEBO\n<size=80%>15 000 personas</size>", out numP);
        K.Texto(transform, "Casos de COVID-19 sintomática", new Vector3(-0.3f, 0.31f, -0.005f), 0.2f, K.Gris, 0.62f, 0.05f, TextAlignmentOptions.Center, true);
        var seg = K.Texto(transform, ContenidoVacuna.Seguridad, new Vector3(0.36f, 0.3f, -0.005f), 0.19f, Color.white, 0.6f, 0.4f, TextAlignmentOptions.TopLeft, false, true);
        seg.lineSpacing = 8f;
        formula = K.Texto(transform, "", new Vector3(0.36f, -0.06f, -0.005f), 0.22f, K.Verde, 0.62f, 0.3f, TextAlignmentOptions.TopLeft, true, true);
        Reiniciar();
    }

    Transform Barra(string nombre, float x, Color c, string rotulo, out TMP_Text numero)
    {
        var pivote = K.Grupo(transform, nombre, new Vector3(x, Base, -0.006f));
        K.Caja(pivote, "Barra", new Vector3(0f, 0.5f, 0f), new Vector3(0.18f, 1f, 0.004f), K.Plano(c));
        K.Texto(transform, rotulo, new Vector3(x, Base - 0.06f, -0.005f), 0.18f, c, 0.3f, 0.08f, TextAlignmentOptions.Center, true);
        numero = K.Texto(transform, "", new Vector3(x, Base + 0.05f, -0.008f), 0.22f, Color.white, 0.3f, 0.05f, TextAlignmentOptions.Center, true);
        return pivote;
    }

    public void Reiniciar()
    {
        t = -1f;
        barraV.localScale = barraP.localScale = new Vector3(1f, 0.001f, 1f);
        numV.text = numP.text = "";
        formula.text = "";
    }

    public void Mostrar() { if (t < 0f) t = 0f; }

    public void MostrarFormula()
    {
        formula.text = "Riesgo vacuna = 15 / 15 000 = 0,1 %\nRiesgo placebo = 150 / 15 000 = 1 %\n<size=125%>Eficacia = (1 - 0,1/1) x 100 = 90 %</size>";
    }

    void Update()
    {
        if (t < 0f) return;
        t += Time.deltaTime;
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.5f));
        float hv = Alto * ContenidoVacuna.CasosVacuna / ContenidoVacuna.CasosPlacebo, hp = Alto;
        barraV.localScale = new Vector3(1f, Mathf.Max(0.001f, hv * k), 1f);
        barraP.localScale = new Vector3(1f, Mathf.Max(0.001f, hp * k), 1f);
        numV.text = $"{Mathf.RoundToInt(ContenidoVacuna.CasosVacuna * k)} casos";
        numP.text = $"{Mathf.RoundToInt(ContenidoVacuna.CasosPlacebo * k)} casos";
        numV.rectTransform.localPosition = new Vector3(-0.46f, Base + hv * k + 0.04f, -0.008f);
        numP.rectTransform.localPosition = new Vector3(-0.14f, Base + hp * k + 0.04f, -0.008f);
    }
}
