using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Registro de la sesión para la evaluación HEART (tiempo, decisiones, intentos, ayudas).
// Se guarda como JSON en Application.persistentDataPath/NanoLab/.
public class MetricsLogger
{
    [System.Serializable] public class TiempoEtapa { public string etapa; public float segundos; }
    [System.Serializable] public class Intento { public int numero; public string hipotesis; public string agente; public float eficacia; public bool exito; public float minuto; }

    [System.Serializable]
    public class Sesion
    {
        public string fecha;
        public string caso;
        public bool completado;
        public float duracionSegundos;
        public List<TiempoEtapa> tiempos = new List<TiempoEtapa>();
        public List<Intento> intentos = new List<Intento>();
        public List<string> hipotesis = new List<string>();
        public List<string> fichasConsultadas = new List<string>();
        public List<string> evidencias = new List<string>();
        public int pistasPedidas;
        public List<string> pistas = new List<string>();
        public int tincionesErroneas;
        public int observacionesMicroscopio;
        public int maxAumentoUsado;
        public bool comparacionAntesDespues;
    }

    public Sesion Datos { get; private set; }
    float inicio, inicioEtapa;
    string etapaActual;

    public MetricsLogger(string caso)
    {
        Datos = new Sesion { fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), caso = caso };
        inicio = inicioEtapa = Time.time;
    }

    public float Minutos => (Time.time - inicio) / 60f;
    public float Segundos => Time.time - inicio;

    public void Etapa(string nueva)
    {
        if (etapaActual != null) Datos.tiempos.Add(new TiempoEtapa { etapa = etapaActual, segundos = Time.time - inicioEtapa });
        etapaActual = nueva;
        inicioEtapa = Time.time;
    }

    public void Agregar(List<string> lista, string valor) { if (!lista.Contains(valor)) lista.Add(valor); }

    public string Guardar(bool completado)
    {
        Etapa(null);
        Datos.completado = completado;
        Datos.duracionSegundos = Segundos;
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "NanoLab");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"sesion_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, JsonUtility.ToJson(Datos, true));
            Debug.Log($"[Métricas] Sesión guardada en {path}");
            return path;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Métricas] No se pudo guardar la sesión: {e.Message}");
            return null;
        }
    }
}
