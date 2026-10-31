using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Partida guardada en local (Application.persistentDataPath/partida.json; en Quest, la memoria de la app):
// muestras analizadas, recuerdos de Baymax, avance del caso del Centro de Experimentación, sala y posición.
// Se guarda sola (al avanzar, al quitarse el visor o pausar, al salir y cada minuto) y con el botón
// GUARDAR PARTIDA del panel ☰; al abrir la app se continúa donde se dejó. NUEVA PARTIDA la borra.
// Checkpoint: cada vez que se termina un reto se guarda una foto del avance (y dónde estaba el jugador);
// el botón CHECKPOINT del panel ☰ vuelve a ella aunque luego se haya repetido o cambiado el caso.
public static class Partida
{
    [System.Serializable]
    public class Checkpoint
    {
        public string reto;              // reto que se acababa de terminar (p. ej. CASO 01)
        public string fecha;
        public string escena;
        public Vector3 pies;
        public float yaw;
        public List<string> muestras = new List<string>();
        public List<string> recuerdos = new List<string>();
        public int etapa;
        public bool[] evidencias;
        public string hipotesis;
    }

    [System.Serializable]
    public class Datos
    {
        public int version = 1;
        public string fecha;
        public string escena;
        public bool hayPosicion;
        public Vector3 pies;
        public float yaw;
        public List<string> muestras = new List<string>();
        public List<string> recuerdos = new List<string>();
        public string reto;              // código del caso (p. ej. CASO 01)
        public int etapa;
        public bool[] evidencias;
        public string hipotesis;         // MuestraData.name
        public List<Checkpoint> checkpoints = new List<Checkpoint>();   // uno por reto terminado, el último al final
    }

    public static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");
    public static bool Existe => File.Exists(Ruta);
    public static System.DateTime? UltimoGuardado { get; private set; }
    // Tras NUEVA PARTIDA no se guarda nada hasta que la escena nueva arranque (si no, el estado viejo volvía).
    internal static bool Suspendido { get; set; }

    public static Checkpoint UltimoCheckpoint
    {
        get
        {
            var d = Leer();
            return d != null && d.checkpoints != null && d.checkpoints.Count > 0 ? d.checkpoints[d.checkpoints.Count - 1] : null;
        }
    }

    public static Datos Leer()
    {
        try
        {
            if (!Existe) return null;
            return JsonUtility.FromJson<Datos>(File.ReadAllText(Ruta));
        }
        catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo leer: " + e.Message); return null; }
    }

    // Reúne el estado actual. Lo que no se toca aquí (los checkpoints) se conserva del archivo.
    public static bool Guardar() => Guardar(null);

    // Guarda la partida y, además, un checkpoint del reto que se acaba de terminar.
    public static bool GuardarCheckpoint(string reto) => Guardar(reto);

    static bool Guardar(string checkpointDe)
    {
        if (Suspendido) return false;
        var d = Leer() ?? new Datos();
        if (d.checkpoints == null) d.checkpoints = new List<Checkpoint>();
        d.fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        d.escena = SceneManager.GetActiveScene().name;
        d.muestras = new List<string>(Progreso.Vistas);
        d.recuerdos = new List<string>(RecuerdosBaymax.Lista);
        var rm = RetoManager.Instancia;
        if (rm != null && rm.reto != null)
        {
            d.reto = rm.reto.codigo;
            d.etapa = (int)rm.Actual;
            d.evidencias = (bool[])rm.Evidencias.Clone();
            d.hipotesis = rm.Hipotesis != null ? rm.Hipotesis.name : null;
        }
        var j = JugadorNanoLab.Instancia;
        // Solo se guarda una posición con suelo del laboratorio debajo (si no, se aparecería cayendo al vacío).
        if (j != null && j.Pies(out var pies, out var yaw) && JugadorNanoLab.PosicionValida(pies)) { d.hayPosicion = true; d.pies = pies; d.yaw = yaw; }
        else if (j != null) d.hayPosicion = false;
        if (!string.IsNullOrEmpty(checkpointDe))
        {
            d.checkpoints.RemoveAll(c => c.reto == checkpointDe);   // el del mismo reto se actualiza y pasa a ser el último
            d.checkpoints.Add(new Checkpoint
            {
                reto = checkpointDe, fecha = d.fecha, escena = d.escena, pies = d.pies, yaw = d.yaw,
                muestras = new List<string>(d.muestras), recuerdos = new List<string>(d.recuerdos),
                etapa = d.etapa, evidencias = d.evidencias != null ? (bool[])d.evidencias.Clone() : null, hipotesis = d.hipotesis,
            });
        }
        try
        {
            // Se escribe aparte y se reemplaza: si la app se cierra a mitad, la partida anterior sigue intacta.
            string tmp = Ruta + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(d, true));
            if (!File.Exists(Ruta)) File.Move(tmp, Ruta);
            else
            {
                try { File.Replace(tmp, Ruta, null); }
                catch (System.Exception) { File.Delete(Ruta); File.Move(tmp, Ruta); }   // sin File.Replace en la plataforma
            }
            UltimoGuardado = System.DateTime.Now;
            return true;
        }
        catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo guardar: " + e.Message); return false; }
    }

    public static void Borrar()
    {
        Suspendido = true;
        try { if (Existe) File.Delete(Ruta); } catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo borrar: " + e.Message); }
        UltimoGuardado = null;
        Progreso.Reiniciar();
        RecuerdosBaymax.Reiniciar();
    }
}

// Lo instala JugadorNanoLab en el rig: restaura la partida al empezar y la guarda cuando cambia algo.
public class GestorPartida : MonoBehaviour
{
    const float Intervalo = 60f;     // autoguardado periódico (s)
    const float Espera = 2f;         // agrupa varios cambios seguidos en un solo guardado

    static bool restauradaEnSesion;  // la posición y la sala se restauran solo al abrir la app
    static bool posicionPendiente;
    bool sucio, restaurando;
    float tSucio, tPeriodico;

    void OnEnable()
    {
        Progreso.Cambiado += Marcar;
        RecuerdosBaymax.Cambiado += Marcar;
        RetoManager.Cambio += Marcar;
    }

    void OnDisable()
    {
        Progreso.Cambiado -= Marcar;
        RecuerdosBaymax.Cambiado -= Marcar;
        RetoManager.Cambio -= Marcar;
    }

    void Marcar() { if (!restaurando) { sucio = true; tSucio = Time.unscaledTime; } }

    IEnumerator Start()
    {
        Partida.Suspendido = false;   // escena nueva (también después de NUEVA PARTIDA)
        restaurando = true;
        yield return null;   // después de los Start de RetoManager, Progreso y el jugador
        var d = Partida.Leer();
        if (d != null)
        {
            Progreso.Restaurar(d.muestras);
            RecuerdosBaymax.Restaurar(d.recuerdos);
            var rm = RetoManager.Instancia;
            if (rm != null && rm.reto != null && d.reto == rm.reto.codigo && d.evidencias != null)
                rm.Restaurar((EtapaReto)d.etapa, d.evidencias, d.hipotesis);

            if (!restauradaEnSesion)
            {
                restauradaEnSesion = true;
                // Al abrir la app se vuelve a la sala donde se dejó (hoy solo existe Microbiología).
                if (!string.IsNullOrEmpty(d.escena) && d.escena != SceneManager.GetActiveScene().name && Application.CanStreamedLevelBeLoaded(d.escena))
                {
                    posicionPendiente = d.hayPosicion;
                    restaurando = false;
                    SceneManager.LoadScene(d.escena);
                    yield break;
                }
                posicionPendiente = d.hayPosicion;
            }
            if (posicionPendiente && d.escena == SceneManager.GetActiveScene().name)
            {
                posicionPendiente = false;
                yield return new WaitForSeconds(0.6f);   // el Locomotor ya está listo y el jugador ya pasó por el punto de aparición
                if (JugadorNanoLab.Instancia != null)
                {
                    if (JugadorNanoLab.PosicionValida(d.pies))
                        JugadorNanoLab.Instancia.Teletransportar(d.pies, Quaternion.Euler(0f, d.yaw, 0f) * Vector3.forward);
                    else
                    {
                        // Partida guardada fuera del laboratorio (versiones anteriores): se empieza en la entrada.
                        Debug.LogWarning($"[Partida] Posición guardada sin suelo {d.pies}: se aparece en la entrada del laboratorio.");
                        var spawn = JugadorNanoLab.Instancia.PuntoAparicion(out var adelanteSpawn);
                        JugadorNanoLab.Instancia.Teletransportar(spawn, adelanteSpawn);
                    }
                }
                var cab = ManosRig.Cabeza;
                if (cab != null)
                {
                    var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
                    Aviso.Mostrar($"Partida cargada ({d.fecha}).\nMuestras: {d.muestras.Count} · Recuerdos: {d.recuerdos.Count}/{RecuerdosBaymax.Total}", cab.position + f * 1f, Aviso.Tipo.Ok, 4f);
                }
            }
        }
        else restauradaEnSesion = true;
        restaurando = false;
        tPeriodico = Time.unscaledTime;
    }

    void Update()
    {
        if (restaurando) return;
        if (sucio && Time.unscaledTime - tSucio > Espera) GuardarAhora();
        else if (Time.unscaledTime - tPeriodico > Intervalo) GuardarAhora();
    }

    void GuardarAhora()
    {
        sucio = false;
        tPeriodico = Time.unscaledTime;
        Partida.Guardar();
    }

    // En Quest: al quitarse el visor o pulsar el botón de Meta la app se pausa (y puede cerrarse sin avisar).
    void OnApplicationPause(bool pausa) { if (pausa && !restaurando) GuardarAhora(); }
    void OnApplicationQuit() { if (!restaurando) GuardarAhora(); }

    // Al descargar la escena: lo pendiente se guarda (salvo tras NUEVA PARTIDA, ver Partida.Suspendido).
    void OnDestroy() { if (sucio && !restaurando) Partida.Guardar(); }

    // Botón CHECKPOINT del panel ☰: vuelve al avance y al sitio del último reto terminado.
    public static bool VolverAlCheckpoint(out string mensaje)
    {
        var c = Partida.UltimoCheckpoint;
        if (c == null) { mensaje = "Aún no hay checkpoint: termina un reto para crear uno."; return false; }
        if (!string.IsNullOrEmpty(c.escena) && c.escena != SceneManager.GetActiveScene().name)
        {
            mensaje = "El checkpoint es de otra sala.";
            return false;
        }
        Progreso.Restaurar(c.muestras);
        RecuerdosBaymax.Restaurar(c.recuerdos);
        var rm = RetoManager.Instancia;
        if (rm != null && rm.reto != null && rm.reto.codigo == c.reto && c.evidencias != null)
        {
            if (rm.Actual != EtapaReto.Exploracion) rm.Reiniciar();   // estaciones, placa y cámara como al empezar
            rm.Restaurar((EtapaReto)c.etapa, c.evidencias, c.hipotesis);
        }
        var adelante = Quaternion.Euler(0f, c.yaw, 0f) * Vector3.forward;
        if (JugadorNanoLab.Instancia != null)
        {
            if (JugadorNanoLab.PosicionValida(c.pies)) JugadorNanoLab.Instancia.Teletransportar(c.pies, adelante);
            else { var sp = JugadorNanoLab.Instancia.PuntoAparicion(out adelante); JugadorNanoLab.Instancia.Teletransportar(sp, adelante); }
        }
        Partida.Guardar();
        mensaje = $"De vuelta al checkpoint: {c.reto} ({c.fecha}).";
        Aviso.Mostrar(mensaje, c.pies + Vector3.up * 1.35f + adelante * 0.9f, Aviso.Tipo.Ok, 4f);
        return true;
    }
}
