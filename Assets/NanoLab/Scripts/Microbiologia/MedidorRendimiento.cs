using System.IO;
using UnityEngine;

// Medidor de rendimiento siempre activo (RNF-08: 72 FPS estables; RNF-02: respuesta < 100 ms).
// Guarda un histograma del tiempo de cada fotograma durante toda la sesión, sin coste apreciable (un contador por
// fotograma). Las métricas del caso (MetricsLogger) toman una marca al empezar y guardan el resumen de ese tramo en
// el JSON de la sesión; además, al pausar o salir de la app se escribe rendimiento_<fecha>.json con el recorrido
// completo (aunque no se termine el caso). Sirve para la evaluación junto con OVR Metrics Tool.
// La lógica del juego reacciona en el mismo fotograma que la entrada, así que el peor tiempo de fotograma (p99 y
// máximo) es la cota de la latencia de respuesta de la interfaz (RNF-02).
public class MedidorRendimiento : MonoBehaviour
{
    public static MedidorRendimiento Instancia { get; private set; }

    const float AnchoCubeta = 0.0005f;   // 0,5 ms
    const int Cubetas = 400;             // hasta 200 ms; lo más lento cae en la última
    const float Calentamiento = 3f;      // los primeros segundos (carga de la escena) no cuentan

    [System.Serializable]
    public class Resumen
    {
        public float segundos;
        public long fotogramas;
        public float objetivoHz;
        public float fpsMedio;
        public float fps1PorCientoPeor;      // FPS del 1 % de fotogramas más lentos (p99 del tiempo de fotograma)
        public float msMedio, msP95, msP99, msPeor;
        public float porcentajeLentos;       // fotogramas que tardaron más de 1,25 veces el presupuesto (p. ej. > 17,4 ms a 72 Hz)
        public bool cumple72;                // fpsMedio >= 0,95 · objetivo y lentos < 5 %
        public bool cumple100ms;             // ningún fotograma (p99) supera 100 ms
        public bool enEditor;
        public string dispositivo;
    }

    // Copia de los contadores en un momento dado (para resumir un tramo).
    public class Marca
    {
        public int[] cubetas;
        public long fotogramas;
        public double suma;
        public float tiempo;
        public int lentos;
        public float peor;
    }

    readonly int[] cubetas = new int[Cubetas];
    long fotogramas;
    double suma;
    int lentos;
    float peor;
    float inicio;
    bool escrito;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (Instancia != null) return;
        var go = new GameObject("MedidorRendimiento");
        DontDestroyOnLoad(go);
        Instancia = go.AddComponent<MedidorRendimiento>();
    }

    void Awake() { inicio = Time.realtimeSinceStartup; }
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    // Frecuencia del visor (72, 90 o 120 Hz en Quest). En el Editor o si no se puede leer, 72 (el objetivo de RNF-08).
    public static float ObjetivoHz
    {
        get
        {
#if !UNITY_EDITOR
            try
            {
                float hz = OVRPlugin.systemDisplayFrequency;
                if (hz > 30f && hz < 200f) return hz;
            }
            catch (System.Exception) { }
#endif
            return 72f;
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f || Time.realtimeSinceStartup - inicio < Calentamiento) return;
        fotogramas++;
        suma += dt;
        if (dt > peor) peor = dt;
        if (dt > 1.25f / ObjetivoHz) lentos++;
        int i = Mathf.Min(Cubetas - 1, Mathf.FloorToInt(dt / AnchoCubeta));
        cubetas[i]++;
    }

    public Marca Marcar() => new Marca
    {
        cubetas = (int[])cubetas.Clone(), fotogramas = fotogramas, suma = suma, tiempo = Time.realtimeSinceStartup, lentos = lentos, peor = peor,
    };

    // Resumen desde una marca (null = desde que se abrió la app).
    public Resumen ResumenDesde(Marca m)
    {
        var c = new int[Cubetas];
        for (int i = 0; i < Cubetas; i++) c[i] = cubetas[i] - (m != null ? m.cubetas[i] : 0);
        long n = fotogramas - (m != null ? m.fotogramas : 0);
        double s = suma - (m != null ? m.suma : 0.0);
        int len = lentos - (m != null ? m.lentos : 0);
        float hz = ObjetivoHz;
        var r = new Resumen
        {
            segundos = Time.realtimeSinceStartup - (m != null ? m.tiempo : inicio),
            fotogramas = n,
            objetivoHz = hz,
            enEditor = Application.isEditor,
            dispositivo = SystemInfo.deviceModel,
        };
        if (n <= 0) return r;
        float medio = (float)(s / n);
        r.msMedio = medio * 1000f;
        r.fpsMedio = 1f / medio;
        r.msP95 = Percentil(c, n, 0.95f) * 1000f;
        r.msP99 = Percentil(c, n, 0.99f) * 1000f;
        r.msPeor = Peor(c) * 1000f;
        r.fps1PorCientoPeor = r.msP99 > 0f ? 1000f / r.msP99 : 0f;
        r.porcentajeLentos = 100f * len / n;
        r.cumple72 = r.fpsMedio >= 0.95f * Mathf.Min(hz, 72f) && r.porcentajeLentos < 5f;
        r.cumple100ms = r.msP99 < 100f;
        return r;
    }

    // Borde superior de la cubeta más lenta con algún fotograma (exacto a 0,5 ms; la última cubeta es "200 ms o más").
    static float Peor(int[] c)
    {
        for (int i = c.Length - 1; i >= 0; i--) if (c[i] > 0) return (i + 1) * AnchoCubeta;
        return 0f;
    }

    static float Percentil(int[] c, long n, float p)
    {
        long objetivo = (long)Mathf.Ceil(p * n), acum = 0;
        for (int i = 0; i < c.Length; i++)
        {
            acum += c[i];
            if (acum >= objetivo) return (i + 1) * AnchoCubeta;   // borde superior de la cubeta (conservador)
        }
        return c.Length * AnchoCubeta;
    }

    // Recorrido completo: al quitarse el visor / pausar y al salir.
    void OnApplicationPause(bool pausa) { if (pausa) Escribir(); }
    void OnApplicationQuit() => Escribir();

    public string Escribir()
    {
        if (fotogramas < 60) return null;
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "NanoLab");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"rendimiento_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, JsonUtility.ToJson(ResumenDesde(null), true));
            if (!escrito) Debug.Log($"[Rendimiento] Resumen guardado en {path}");
            escrito = true;
            return path;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Rendimiento] No se pudo guardar: " + e.Message);
            return null;
        }
    }
}
