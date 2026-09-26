using UnityEngine;

// Sonidos de interfaz sintetizados por código (no necesitan archivos de audio).
public static class Sonidos
{
    static AudioClip clic, carga, encaje, scan, logro, error;

    public static void Clic(Vector3 p) => Reproducir(ref clic, "clic", p, 0.5f, 0.05f, 1800f);
    public static void Carga(Vector3 p) => Reproducir(ref carga, "carga", p, 0.6f, 0.22f, 520f, 780f, 1040f);
    public static void Encaje(Vector3 p) => Reproducir(ref encaje, "encaje", p, 0.7f, 0.12f, 300f, 600f);
    public static void Scan(Vector3 p) => Reproducir(ref scan, "scan", p, 0.45f, 0.45f, 400f, 600f, 800f, 1000f, 1200f);
    public static void Logro(Vector3 p) => Reproducir(ref logro, "logro", p, 0.7f, 0.6f, 523f, 659f, 784f, 1047f);
    public static void Error(Vector3 p) => Reproducir(ref error, "error", p, 0.5f, 0.2f, 220f, 180f);

    static void Reproducir(ref AudioClip clip, string nombre, Vector3 p, float volumen, float duracion, params float[] notas)
    {
        if (clip == null) clip = Tono(nombre, duracion, notas);
        AudioSource.PlayClipAtPoint(clip, p, volumen);
    }

    // Secuencia de notas con envolvente suave (ataque corto, caída exponencial).
    static AudioClip Tono(string nombre, float duracion, float[] notas)
    {
        const int sr = 44100;
        int len = Mathf.Max(1, (int)(sr * duracion));
        var data = new float[len];
        int porNota = Mathf.Max(1, len / notas.Length);
        for (int i = 0; i < len; i++)
        {
            int n = Mathf.Min(notas.Length - 1, i / porNota);
            float t = i / (float)sr;
            float local = (i % porNota) / (float)porNota;
            float env = Mathf.Min(1f, local * 20f) * Mathf.Exp(-local * 4f);
            data[i] = (Mathf.Sin(2f * Mathf.PI * notas[n] * t) * 0.8f + Mathf.Sin(4f * Mathf.PI * notas[n] * t) * 0.2f) * env * 0.5f;
        }
        var c = AudioClip.Create(nombre, len, 1, sr, false);
        c.SetData(data, 0);
        return c;
    }
}
