using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Registro de la sesión para la evaluación HEART (tiempo, decisiones, intentos, ayudas).
// Se guarda como JSON en Application.persistentDataPath/NanoLab/ (en Quest:
// /sdcard/Android/data/com.utec.NanoLab/files/NanoLab/). Herramientas/analizar_metricas.py resume varias sesiones.
//
// Qué alimenta cada dimensión de HEART:
//   H (Happiness)  → cuestionario Likert fuera del visor (Docs/Piloto_HEART.md); aquí no se mide.
//   E (Engagement) → accionesIndagacion, porcentajeInactivo.
//   A (Adoption)   → segundosHastaEmpezar (de llegar a la estación a pulsar CONTINUAR, o al empezar el paso 1 si se
//                    repite) y segundosPrimerPaso (hasta el primer acierto).
//   R (Retention)  → preAciertos / postAciertos de las preguntas de comprensión.
//   T (Task)       → completado, duracionSegundos, pistasPedidas, comparacionAntesDespues, errores.
// Y el rendimiento del tramo (RNF-08, RNF-02) en 'rendimiento'.
public class MetricsLogger
{
    [System.Serializable] public class TiempoEtapa { public string etapa; public float segundos; }
    [System.Serializable] public class Intento { public int numero; public string hipotesis; public string estrategia; public string agente; public bool coherente; public float eficacia; public bool exito; public float minuto; }
    [System.Serializable] public class Conteo { public string etapa; public int veces; }
    // Evento sorpresa de bioseguridad (misión 2): tiempos hasta tomar el spray y hasta descontaminar.
    [System.Serializable] public class Bioseguridad { public bool ocurrido; public float segundosHastaSpray; public float segundosLimpieza; public int zonas; public float minuto; }

    [System.Serializable]
    public class Sesion
    {
        public string fecha;
        public string caso;
        public string version;
        public string dispositivo;
        public bool completado;
        public float duracionSegundos;
        public List<TiempoEtapa> tiempos = new List<TiempoEtapa>();
        public List<Intento> intentos = new List<Intento>();
        public List<string> hipotesis = new List<string>();
        public List<string> estrategias = new List<string>();   // RF-05: estructura que se decidió atacar
        public List<string> fichasConsultadas = new List<string>();
        public List<string> evidencias = new List<string>();
        public int pistasPedidas;
        public List<string> pistas = new List<string>();
        public int tincionesErroneas;
        public int observacionesMicroscopio;
        public int maxAumentoUsado;
        public bool comparacionAntesDespues;

        // HEART (añadido en E5; campos nuevos al final para no romper los JSON anteriores)
        public int errores;
        public List<Conteo> erroresPorEtapa = new List<Conteo>();
        public bool pasoAPasoUsado;
        public int accionesIndagacion;
        public List<string> indagacion = new List<string>();
        public float segundosDesdeAbrirApp;        // al llegar a la estación (orientación en el laboratorio)
        public float segundosHastaEmpezar = -1f;   // de llegar a la estación a empezar el caso (primer CONTINUAR)
        public float segundosPrimerPaso = -1f;     // de llegar a la estación al primer acierto (normalmente, el paso 1)
        public float segundosMuestreados;          // tiempo del caso en que se midió la actividad
        public float segundosInactivo;             // ventanas de 1 s sin moverse ni interactuar
        public float porcentajeInactivo;
        public int totalComprension;
        public int preAciertos = -1;               // -1 = no se hizo (p. ej. al REPETIR el caso)
        public int postAciertos = -1;
        public List<string> pretest = new List<string>();
        public List<string> postest = new List<string>();
        public MedidorRendimiento.Resumen rendimiento;
        public Bioseguridad bioseguridad;
    }

    public Sesion Datos { get; private set; }
    float inicio, inicioEtapa;
    string etapaActual;
    readonly MedidorRendimiento.Marca marca;

    // Actividad: ventanas de 1 s. Una ventana es inactiva si la cabeza apenas se movió y giró, las manos apenas se
    // movieron y no hubo ninguna interacción (Actividad()).
    const float Ventana = 1f, UmbralCabeza = 0.03f, UmbralGiro = 4f, UmbralManos = 0.04f;
    float tVentana, movCabeza, giro, movManos;
    bool interaccion, hayPrevio;
    Vector3 pCab, pIzq, pDer;
    Quaternion rCab;

    public MetricsLogger(string caso)
    {
        Datos = new Sesion
        {
            fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), caso = caso,
            version = Application.version, dispositivo = SystemInfo.deviceModel,
            segundosDesdeAbrirApp = Time.realtimeSinceStartup,
        };
        inicio = inicioEtapa = Time.time;
        if (MedidorRendimiento.Instancia != null) marca = MedidorRendimiento.Instancia.Marcar();
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

    // ---------------------------------------------------------------- HEART

    public void Error(string etapa)
    {
        Datos.errores++;
        var c = Datos.erroresPorEtapa.Find(x => x.etapa == etapa);
        if (c == null) Datos.erroresPorEtapa.Add(new Conteo { etapa = etapa, veces = 1 });
        else c.veces++;
    }

    // Acción deliberada de indagación (tocar el virus para explorarlo, observar al microscopio, consultar…).
    public void Indagacion(string que)
    {
        Datos.accionesIndagacion++;
        if (Datos.indagacion.Count < 60) Datos.indagacion.Add(que);
        Actividad();
    }

    public void Empezar() { if (Datos.segundosHastaEmpezar < 0f) Datos.segundosHastaEmpezar = Segundos; }
    public void PrimerExito() { if (Datos.segundosPrimerPaso < 0f) Datos.segundosPrimerPaso = Segundos; }

    // Cualquier interacción (botón, pieza encajada, respuesta) marca la ventana actual como activa.
    public void Actividad() => interaccion = true;

    // Se llama en cada fotograma mientras el caso está en marcha.
    public void Muestrear(Transform cabeza, Transform izq, Transform der, float dt)
    {
        if (cabeza == null || dt <= 0f || dt > 0.5f) { hayPrevio = false; return; }
        Vector3 i = izq != null ? izq.position : cabeza.position, d = der != null ? der.position : cabeza.position;
        if (hayPrevio)
        {
            movCabeza += (cabeza.position - pCab).magnitude;
            giro += Quaternion.Angle(cabeza.rotation, rCab);
            movManos += (i - pIzq).magnitude + (d - pDer).magnitude;
        }
        pCab = cabeza.position; rCab = cabeza.rotation; pIzq = i; pDer = d; hayPrevio = true;
        tVentana += dt;
        Datos.segundosMuestreados += dt;
        if (tVentana < Ventana) return;
        bool quieto = !interaccion && movCabeza < UmbralCabeza && giro < UmbralGiro && movManos < UmbralManos;
        if (quieto) Datos.segundosInactivo += tVentana;
        tVentana = movCabeza = giro = movManos = 0f;
        interaccion = false;
    }

    public void Comprension(bool antes, int pregunta, string respuesta, bool correcta, int total)
    {
        Datos.totalComprension = total;
        var lista = antes ? Datos.pretest : Datos.postest;
        lista.Add($"P{pregunta + 1}: {respuesta} ({(correcta ? "correcta" : "incorrecta")})");
        if (antes) Datos.preAciertos = Mathf.Max(0, Datos.preAciertos) + (correcta ? 1 : 0);
        else Datos.postAciertos = Mathf.Max(0, Datos.postAciertos) + (correcta ? 1 : 0);
        Actividad();
    }

    public string Guardar(bool completado)
    {
        Etapa(null);
        Datos.completado = completado;
        Datos.duracionSegundos = Segundos;
        Datos.porcentajeInactivo = Datos.segundosMuestreados > 0f ? 100f * Datos.segundosInactivo / Datos.segundosMuestreados : 0f;
        if (MedidorRendimiento.Instancia != null) Datos.rendimiento = MedidorRendimiento.Instancia.ResumenDesde(marca);
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
