using System.Collections.Generic;
using UnityEngine;

// Las 3 misiones del Centro de Experimentación y los 3 recuerdos de Baymax (orden fijo):
//   MISIÓN 1 · OBSERVAR     ¿Qué hay en las muestras?  Banco (1) > Tinción (2) > Microscopía (3) > Conclusiones >
//                           Biblioteca (4) > Terminal (5, fila 1). Compara 3 muestras: bacteria, hongo y virus.
//   MISIÓN 2 · EXPERIMENTAR ¿Qué la detiene?           Terminal (5, fila 2) > Prensa (6) > Cámara (7) > Microscopía (3)
//                           ANTES | DESPUÉS > Conclusiones > Archivo (8).
//   MISIÓN 3 · CREAR        Crea la vacuna             Laboratorio de vacunas (se desbloquea al terminar la 2).
// Las misiones 1 y 2 las coordina RetoManager (el caso de bacterias de E2-E4, partido en dos); la 3, RetoVacuna.
public static class Misiones
{
    public const string Codigo1 = "MISIÓN 1";
    public const string Titulo1 = "¿Qué hay en las muestras?";
    public const string Codigo2 = "MISIÓN 2";
    public const string Titulo2 = "¿Qué la detiene?";
    public const string Codigo3 = ContenidoVacuna.Codigo;

    // Identificadores de los recuerdos (se guardan en la partida).
    public const string Recuerdo1 = "MISIÓN 1 · OBSERVAR";
    public const string Recuerdo2 = "MISIÓN 2 · EXPERIMENTAR";
    public const string Recuerdo3 = "MISIÓN 3 · CREAR";
    public static readonly string[] Recuerdos = { Recuerdo1, Recuerdo2, Recuerdo3 };

    public const string TextoRecuerdo1 =
        "Baymax recuerda: \"No todos los microbios son iguales: los hongos son grandes y tienen núcleo, las bacterias miden una milésima de milímetro " +
        "y los virus son tan pequeños que el microscopio óptico no los ve.\"";
    public const string TextoRecuerdo2 =
        "Baymax recuerda: \"Para detener a un microbio hay que atacar algo que él tiene y nosotros no, como la pared de las bacterias. " +
        "Por eso los antibióticos no sirven contra los virus.\"";

    // Etiquetas de las muestras del banco (lo que se escribe en el portaobjetos: no revela qué son).
    public const string EtiquetaA = "MUESTRA A";
    public const string EtiquetaB = "MUESTRA B";
    public const string EtiquetaC = "MUESTRA C";

    public const string OrigenA = "Herida de piel que no sana";
    public const string OrigenB = "Placa olvidada en la incubadora: le creció un moho";
    public const string OrigenC = "Hisopado nasal de un paciente del brote";

    // Para las pruebas automáticas de la vacuna (sin pasar por las misiones 1 y 2).
    public static bool ForzarDesbloqueo;

    public static bool VacunaDesbloqueada => ForzarDesbloqueo || Tiene(Recuerdo2);
    public static bool Tiene(string recuerdo) { foreach (var r in RecuerdosBaymax.Lista) if (r == recuerdo) return true; return false; }

    // Recuerdos de partidas anteriores: "CASO 01" era el recuerdo de la vacuna (o del caso de bacterias, que hoy es la
    // misión 2) y los dos intermedios de la vacuna del E5 ya no existen. Lo desconocido se descarta.
    public static string Normalizar(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (System.Array.IndexOf(Recuerdos, id) >= 0) return id;
        if (id == "CASO 01" || id == "CASO 01 · PROBAR") return Recuerdo3;
        return null;
    }

    // ------------------------------------------------------------------ muestras B y C (creadas al empezar)

    static MuestraData hongo, virus;

    public static MuestraData MuestraHongo
    {
        get
        {
            if (hongo != null) return hongo;
            hongo = ScriptableObject.CreateInstance<MuestraData>();
            hongo.name = "MisionHongo";
            hongo.codigo = "#H-01";
            hongo.nombre = "Moho (Penicillium)";
            hongo.nombreCientifico = "Penicillium sp.";
            hongo.tipo = TipoMuestra.Hongo;
            hongo.riesgo = "BAJO";
            hongo.color = new Color(0.35f, 0.65f, 0.55f);
            hongo.micrografia = MicrografiasMisiones.Moho;
            hongo.creditoImagen = "Ilustración generada para NanoLab";
            hongo.caracteristicas = new[]
            {
                new MuestraData.Dato { clave = "Forma", valor = "filamentos ramificados (hifas) y esporas en pincel" },
                new MuestraData.Dato { clave = "Tamaño", valor = "hifas de 3-5 µm de ancho, muy largas" },
            };
            hongo.observaciones = "Hongo filamentoso: forma hifas y esporas. Sus células tienen núcleo, como las nuestras.";
            return hongo;
        }
    }

    public static MuestraData MuestraVirus
    {
        get
        {
            if (virus != null) return virus;
            virus = ScriptableObject.CreateInstance<MuestraData>();
            virus.name = "MisionVirus";
            virus.codigo = "#V-99";
            virus.nombre = "Virus del brote";
            virus.nombreCientifico = "coronavirus respiratorio (modelo)";
            virus.tipo = TipoMuestra.Virus;
            virus.vistaOptica = true;   // al microscopio óptico: solo se ven las células de la nariz
            virus.riesgo = "ALTO CONTAGIO";
            virus.color = new Color(0.9f, 0.45f, 0.5f);
            virus.micrografia = MicrografiasMisiones.Hisopado;
            virus.creditoImagen = "Ilustración generada para NanoLab";
            virus.observaciones = "Células de la mucosa nasal. El virus (unos 100 nm) no se ve con luz visible.";
            return virus;
        }
    }
}

// Micrografías ilustrativas de las muestras B y C, generadas al vuelo (una vez). 256 px, campo circular.
public static class MicrografiasMisiones
{
    const int Lado = 256;
    static Texture2D moho, hisopado;

    public static Texture2D Moho => moho != null ? moho : (moho = GenerarMoho());
    public static Texture2D Hisopado => hisopado != null ? hisopado : (hisopado = GenerarHisopado());

    // Penicillium teñido con azul de lactofenol: hifas largas, ramificadas y tabicadas, y conidióforos que acaban en
    // un "pincel" de fiálides con cadenas de esporas redondas (Penicillium = "pincelito" en latín).
    static Texture2D GenerarMoho()
    {
        var c = Lienzo(new Color(0.86f, 0.9f, 0.96f));
        var rnd = new System.Random(5);
        var hifa = new Color(0.22f, 0.36f, 0.72f);
        var espora = new Color(0.3f, 0.45f, 0.82f);
        float R() => (float)rnd.NextDouble();
        for (int h = 0; h < 7; h++)
        {
            Vector2 p = new Vector2(R() * Lado, R() * Lado);
            float ang = R() * Mathf.PI * 2f;
            int tramos = 6 + rnd.Next(6);
            for (int t = 0; t < tramos; t++)
            {
                ang += (R() - 0.5f) * 0.6f;
                var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (18f + R() * 14f);
                Linea(c, p, q, 2.6f, hifa);
                Disco(c, q, 2.2f, hifa * 0.8f);                       // tabique (septo)
                if (R() < 0.35f)                                      // rama lateral
                {
                    float a2 = ang + (R() < 0.5f ? 1f : -1f) * (0.6f + R() * 0.5f);
                    var r = q + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * (20f + R() * 15f);
                    Linea(c, q, r, 2.2f, hifa);
                }
                if (R() < 0.3f) Pincel(c, q, ang + Mathf.PI / 2f * (R() < 0.5f ? 1f : -1f), rnd, hifa, espora);
                p = q;
            }
        }
        // Esporas sueltas por el campo.
        for (int i = 0; i < 40; i++) Disco(c, new Vector2(R() * Lado, R() * Lado), 1.8f, espora);
        return Terminar(c, "Mision_Moho");
    }

    static void Pincel(Color[] c, Vector2 b, float ang, System.Random rnd, Color hifa, Color espora)
    {
        var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        var punta = b + dir * 22f;
        Linea(c, b, punta, 2f, hifa);
        for (int k = -2; k <= 2; k++)
        {
            float a = ang + k * 0.28f;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var f = punta + d * 8f;
            Linea(c, punta, f, 1.5f, hifa);
            for (int s = 1; s <= 4; s++) Disco(c, f + d * (s * 3.6f), 1.7f, espora);   // cadena de esporas
        }
    }

    // Hisopado nasal con tinción de Gram: células de la mucosa grandes (rosas, con núcleo) y moco; ningún microbio.
    static Texture2D GenerarHisopado()
    {
        var c = Lienzo(new Color(0.95f, 0.92f, 0.94f));
        var rnd = new System.Random(9);
        float R() => (float)rnd.NextDouble();
        // Moco: estrías tenues.
        for (int i = 0; i < 6; i++)
        {
            var p = new Vector2(R() * Lado, R() * Lado);
            float a = R() * Mathf.PI;
            Linea(c, p - new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 80f, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 80f, 5f, new Color(0.9f, 0.82f, 0.88f));
        }
        // Células epiteliales: polígonos irregulares grandes con un núcleo oscuro.
        for (int i = 0; i < 9; i++)
        {
            var centro = new Vector2(20f + R() * (Lado - 40f), 20f + R() * (Lado - 40f));
            float radio = 18f + R() * 12f;
            int lados = 5 + rnd.Next(3);
            var vert = new Vector2[lados];
            float giro = R() * Mathf.PI;
            for (int k = 0; k < lados; k++)
            {
                float a = giro + k * Mathf.PI * 2f / lados;
                vert[k] = centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radio * (0.8f + R() * 0.35f);
            }
            Poligono(c, vert, new Color(0.93f, 0.62f, 0.72f));
            for (int k = 0; k < lados; k++) Linea(c, vert[k], vert[(k + 1) % lados], 1.2f, new Color(0.78f, 0.4f, 0.55f));
            Disco(c, centro + new Vector2(R() - 0.5f, R() - 0.5f) * 6f, 4.5f, new Color(0.45f, 0.15f, 0.35f));
        }
        return Terminar(c, "Mision_Hisopado");
    }

    // ------------------------------------------------------------------ dibujo

    static Color[] Lienzo(Color fondo)
    {
        var c = new Color[Lado * Lado];
        for (int i = 0; i < c.Length; i++) c[i] = fondo;
        return c;
    }

    static void Pixel(Color[] c, int x, int y, Color col)
    {
        if (x < 0 || y < 0 || x >= Lado || y >= Lado) return;
        c[y * Lado + x] = col;
    }

    static void Disco(Color[] c, Vector2 p, float r, Color col)
    {
        int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.CeilToInt(p.x + r), y0 = Mathf.FloorToInt(p.y - r), y1 = Mathf.CeilToInt(p.y + r);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if ((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y) <= r * r) Pixel(c, x, y, col);
    }

    static void Linea(Color[] c, Vector2 a, Vector2 b, float grosor, Color col)
    {
        float largo = Vector2.Distance(a, b);
        int pasos = Mathf.Max(1, Mathf.CeilToInt(largo));
        for (int i = 0; i <= pasos; i++) Disco(c, Vector2.Lerp(a, b, i / (float)pasos), grosor * 0.5f, col);
    }

    static void Poligono(Color[] c, Vector2[] v, Color col)
    {
        float yMin = float.MaxValue, yMax = float.MinValue;
        foreach (var p in v) { yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y); }
        var cortes = new List<float>();
        for (int y = Mathf.FloorToInt(yMin); y <= Mathf.CeilToInt(yMax); y++)
        {
            cortes.Clear();
            for (int i = 0; i < v.Length; i++)
            {
                var a = v[i]; var b = v[(i + 1) % v.Length];
                if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y)) cortes.Add(a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x));
            }
            cortes.Sort();
            for (int k = 0; k + 1 < cortes.Count; k += 2)
                for (int x = Mathf.CeilToInt(cortes[k]); x <= Mathf.FloorToInt(cortes[k + 1]); x++) Pixel(c, x, y, col);
        }
    }

    static Texture2D Terminar(Color[] c, string nombre)
    {
        float centro = Lado / 2f, rCampo = Lado / 2f - 2f;
        var negro = new Color(0.01f, 0.02f, 0.04f);
        for (int y = 0; y < Lado; y++)
            for (int x = 0; x < Lado; x++)
            {
                float r = Mathf.Sqrt((x - centro) * (x - centro) + (y - centro) * (y - centro));
                int i = y * Lado + x;
                if (r > rCampo) c[i] = negro;
                else if (r > rCampo - 10f) c[i] = Color.Lerp(c[i], negro, (r - (rCampo - 10f)) / 10f);
                c[i].a = 1f;
            }
        var t = new Texture2D(Lado, Lado, TextureFormat.RGB24, false) { name = nombre, wrapMode = TextureWrapMode.Clamp };
        t.SetPixels(c);
        t.Apply(false, true);
        return t;
    }
}
