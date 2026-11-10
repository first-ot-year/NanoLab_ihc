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
    bool holo;

    // Modo HOLOGRAMA (mostrador del paso ANTÍGENO): envoltura cortada por delante para ver el interior, cada parte
    // con su collider y su selector (orden ARN, Espiga, Envoltura, Proteína N), sin rótulos y sin giro automático.
    public SelectorVacuna[] selectores = new SelectorVacuna[4];
    public bool Holograma => holo;
    public Vector3 Centro => modelo != null ? modelo.position : transform.position;
    public void Girar(Quaternion delta) { if (modelo != null) modelo.rotation = delta * modelo.rotation; }

    public static VirusVacuna CrearHolograma(Transform padre, Vector3 pos, Quaternion rot)
    {
        var g = K.Grupo(padre, "VirusHolograma", pos, rot);
        var v = g.gameObject.AddComponent<VirusVacuna>();
        v.holo = true;
        v.Montar();
        return v;
    }

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
        modelo = K.Grupo(transform, "Modelo", holo ? Vector3.zero : new Vector3(0f, 0f, -0.28f));
        modelo.localRotation = Quaternion.Euler(15f, 0f, 0f);
        const float r = 0.2f;

        if (holo)
        {
            // Media envoltura (la mitad de -z, la que mira al usuario, se corta) con las dos caras visibles.
            envoltura = K.Pieza(modelo, "Envoltura", PrimitiveType.Cube, Vector3.zero, Vector3.one * r, K.Vidrio(new Color(0.55f, 0.8f, 1f, 0.28f)));
            envoltura.GetComponent<MeshFilter>().sharedMesh = MediaEsfera();
        }
        else envoltura = K.Esfera(modelo, "Envoltura", Vector3.zero, r * 2f, K.Vidrio(new Color(0.55f, 0.8f, 1f, 0.28f)));
        partes[(int)Parte.Envoltura].Add(envoltura);
        // Proteínas M de la membrana (puntos oscuros en la envoltura).
        var puntos = Fibonacci(40);
        for (int i = 0; i < puntos.Length; i += 2)
            if (!holo || puntos[i].z > 0f) partes[(int)Parte.Envoltura].Add(K.Esfera(modelo, "ProteinaM", puntos[i] * r * 0.99f, 0.018f, K.Plano(new Color(0.35f, 0.55f, 0.8f))));

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

        if (holo)
        {
            // Cada parte se toca con su forma real (mano) o se apunta con el rayo + gatillo.
            var rb = modelo.gameObject.AddComponent<Rigidbody>();   // cinemático: el modelo gira sin mover colliders estáticos
            rb.isKinematic = true;
            rb.useGravity = false;
            string[] acciones = { "elegir ARN", "elegir espiga S", "elegir envoltura", "elegir proteína N" };
            for (int i = 0; i < partes.Length; i++)
            {
                Renderer pieza = i == (int)Parte.Envoltura ? envoltura : partes[i].Find(x => x != envoltura);
                if (pieza == null) continue;
                pieza.gameObject.AddComponent<MeshCollider>().sharedMesh = pieza.GetComponent<MeshFilter>().sharedMesh;
                var s = pieza.gameObject.AddComponent<SelectorVacuna>();
                s.accion = acciones[i];
                s.tactil = false;   // el toque lo detecta VirusHolograma con la forma de cada parte
                s.alcanceRayo = 2.6f;
                selectores[i] = s;
            }
            return;   // sin rótulos: la clave de colores va en la pantalla derecha
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

    // Semiesfera de radio 1 (z >= 0) con caras por fuera y por dentro (se ve el interior por el corte).
    static Mesh mediaEsfera;
    static Mesh MediaEsfera()
    {
        if (mediaEsfera != null) return mediaEsfera;
        const int lat = 12, lon = 24;
        var v = new System.Collections.Generic.List<Vector3>();
        var nor = new System.Collections.Generic.List<Vector3>();
        var tri = new System.Collections.Generic.List<int>();
        for (int cara = 0; cara < 2; cara++)
        {
            int b = v.Count;
            for (int i = 0; i <= lat; i++)
            {
                float a = Mathf.PI * 0.5f * i / lat;   // 0 = polo (+z), PI/2 = borde del corte
                for (int j = 0; j <= lon; j++)
                {
                    float ph = 2f * Mathf.PI * j / lon;
                    var p = new Vector3(Mathf.Sin(a) * Mathf.Cos(ph), Mathf.Sin(a) * Mathf.Sin(ph), Mathf.Cos(a));
                    v.Add(p);
                    nor.Add(cara == 0 ? p : -p);
                }
            }
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int a0 = b + i * (lon + 1) + j, a1 = a0 + 1, b0 = a0 + lon + 1, b1 = b0 + 1;
                    if (cara == 0) tri.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                    else tri.AddRange(new[] { a0, b0, a1, a1, b0, b1 });
                }
        }
        mediaEsfera = new Mesh { name = "MediaEsfera" };
        mediaEsfera.SetVertices(v);
        mediaEsfera.SetNormals(nor);
        mediaEsfera.SetTriangles(tri, 0);
        mediaEsfera.RecalculateBounds();
        return mediaEsfera;
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
        if (holo) escalaBase = escala;   // el holograma no se anima al colocarlo
        if (etiquetas != null) etiquetas.gameObject.SetActive(conEtiquetas);
    }

    public void Resaltar(Parte p) => resaltada = p;

    // Rótulo de una parte (para ponerle el selector del paso ANTÍGENO).
    public Transform Rotulo(int i) => rotulos[i] != null ? rotulos[i].transform : transform;

    public void Anticuerpos(bool si)
    {
        if (si && tAnticuerpos < 0f) tAnticuerpos = 0f;
        if (!si) tAnticuerpos = -1f;
        foreach (var a in anticuerpos) a.gameObject.SetActive(si);
    }

    void Update()
    {
        if (!holo) modelo.Rotate(0f, 18f * Time.deltaTime, 0f, Space.Self);   // el holograma lo gira el usuario
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
            if (rotulos[i] == null) continue;
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

// Barras de casos por grupo y tabla de seguridad. Cada respuesta correcta del paso EFICACIA revela algo más:
// 1 "10 veces menos" entre las barras · 2 diez personas (9 se salvan, 1 enferma) · 3 el 90 % · 4 el sello APROBADA.
public class GraficaEficacia : MonoBehaviour
{
    static readonly Color Naranja = new Color(1f, 0.6f, 0.2f);
    Transform barraV, barraP, comparacion, personas, sello;
    TMP_Text numV, numP, formula, rotuloPersonas;
    Renderer[] cuerpos;
    float t = -1f, tPersonas = -1f, tSello = -1f;
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
        barraP = Barra("Placebo", -0.14f, Naranja, "PLACEBO\n<size=80%>15 000 personas</size>", out numP);
        K.Texto(transform, "Enfermos de COVID-19 en cada grupo", new Vector3(-0.3f, 0.31f, -0.005f), 0.2f, K.Gris, 0.62f, 0.05f, TextAlignmentOptions.Center, true);
        var seg = K.Texto(transform, ContenidoVacuna.Seguridad, new Vector3(0.36f, 0.3f, -0.005f), 0.19f, Color.white, 0.6f, 0.4f, TextAlignmentOptions.TopLeft, false, true);
        seg.lineSpacing = 8f;

        // 1 · Comparación entre las barras.
        // Encima de la barra corta (VACUNA), sin tapar la naranja.
        comparacion = K.Grupo(transform, "Comparacion", new Vector3(-0.46f, -0.04f, -0.008f));
        K.Texto(comparacion, "<b>10 veces\nMENOS</b>", Vector3.zero, 0.24f, K.Verde, 0.2f, 0.1f, TextAlignmentOptions.Center, true);

        // 2 · Diez personas: las que se salvan se ponen verdes.
        personas = K.Grupo(transform, "Personas", new Vector3(0.36f, 0.06f, -0.006f));
        cuerpos = new Renderer[10];
        for (int i = 0; i < 10; i++)
        {
            var f = K.Grupo(personas, "Persona", new Vector3(-0.25f + i * 0.055f, 0f, 0f));
            cuerpos[i] = K.Pieza(f, "Cuerpo", PrimitiveType.Capsule, new Vector3(0f, -0.012f, 0f), new Vector3(0.026f, 0.022f, 0.01f), K.Plano(Color.white));
            K.Esfera(f, "Cabeza", new Vector3(0f, 0.027f, 0f), 0.02f, K.Plano(new Color(0.95f, 0.85f, 0.75f)));
        }
        rotuloPersonas = K.Texto(personas, "", new Vector3(0f, -0.06f, 0f), 0.17f, Color.white, 0.62f, 0.04f, TextAlignmentOptions.Center, true);

        // 3 · El porcentaje.
        formula = K.Texto(transform, "", new Vector3(0.36f, -0.06f, -0.005f), 0.24f, K.Verde, 0.62f, 0.14f, TextAlignmentOptions.Top, true, true);

        // 4 · Sello de la agencia.
        sello = K.Grupo(transform, "Sello", new Vector3(0.36f, -0.27f, -0.01f), Quaternion.Euler(0f, 0f, 8f));
        K.Panel(sello, "Fondo", Vector3.zero, 0.42f, 0.1f, new Color(0.05f, 0.25f, 0.12f, 0.95f), K.Verde, 0.003f);
        K.Texto(sello, "<b>APROBADA</b>  <size=60%>· fase IV la vigila</size>", new Vector3(0f, 0f, -0.003f), 0.3f, K.Verde, 0.4f, 0.08f, TextAlignmentOptions.Center, true);
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
        t = tPersonas = tSello = -1f;
        barraV.localScale = barraP.localScale = new Vector3(1f, 0.001f, 1f);
        numV.text = numP.text = "";
        formula.text = "";
        comparacion.gameObject.SetActive(false);
        personas.gameObject.SetActive(false);
        sello.gameObject.SetActive(false);
        foreach (var c in cuerpos) K.Pintar(c, Naranja);
        rotuloPersonas.text = "";
    }

    public void Mostrar() { if (t < 0f) t = 0f; }

    // Revela lo que corresponde a la pregunta k (1..4) recién acertada.
    public void Revelar(int k)
    {
        switch (k)
        {
            case 1: comparacion.gameObject.SetActive(true); comparacion.localScale = Vector3.one * 0.2f; break;
            case 2: personas.gameObject.SetActive(true); tPersonas = 0f; break;
            case 3: MostrarFormula(); break;
            case 4: sello.gameObject.SetActive(true); tSello = 0f; break;
        }
    }

    public void MostrarFormula()
    {
        formula.text = "<size=130%>Eficacia = 9 de cada 10 = 90 %</size>\n<size=75%><color=#A9BED2>es lo mismo que (1 - 15/150) x 100</color></size>";
    }

    void Update()
    {
        if (comparacion.gameObject.activeSelf && comparacion.localScale.x < 1f)
            comparacion.localScale = Vector3.one * Mathf.MoveTowards(comparacion.localScale.x, 1f, Time.deltaTime * 3f);
        if (tPersonas >= 0f)
        {
            // De una en una, 9 personas pasan de naranja (habría enfermado) a verde (se salvó); la última sigue roja.
            tPersonas += Time.deltaTime;
            int verdes = Mathf.Min(9, Mathf.FloorToInt(tPersonas / 0.15f));
            for (int i = 0; i < 10; i++) K.Pintar(cuerpos[i], i < verdes ? K.Verde : i == 9 && tPersonas > 1.5f ? K.Rojo : Naranja);
            if (tPersonas > 1.5f) { rotuloPersonas.text = "De cada 10: <color=#39FF88>9 se salvan</color> · <color=#FF7A6B>1 enferma</color>"; tPersonas = -1f; }
        }
        if (tSello >= 0f)
        {
            tSello += Time.deltaTime;
            float k = Mathf.Clamp01(tSello / 0.35f);
            sello.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, k);   // cae como un sello
            if (k >= 1f) tSello = -1f;
        }
        if (t < 0f) return;
        t += Time.deltaTime;
        float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 1.5f));
        float hv = Alto * ContenidoVacuna.CasosVacuna / ContenidoVacuna.CasosPlacebo, hp = Alto;
        barraV.localScale = new Vector3(1f, Mathf.Max(0.001f, hv * s), 1f);
        barraP.localScale = new Vector3(1f, Mathf.Max(0.001f, hp * s), 1f);
        numV.text = $"{Mathf.RoundToInt(ContenidoVacuna.CasosVacuna * s)} enfermos";
        numP.text = $"{Mathf.RoundToInt(ContenidoVacuna.CasosPlacebo * s)} enfermos";
        numV.rectTransform.localPosition = new Vector3(-0.46f, Base + hv * s + 0.04f, -0.008f);
        numP.rectTransform.localPosition = new Vector3(-0.14f, Base + hp * s + 0.04f, -0.008f);
        if (s >= 1f) t = -2f;   // terminada: deja de recalcular
    }
}
