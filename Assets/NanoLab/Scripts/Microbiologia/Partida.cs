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
// Respaldo: cada guardado deja la versión anterior en partida.json.bak. Si partida.json se daña (la app
// se cerró a mitad de escribir, memoria llena…), se aparta a partida.corrupta.json y se carga el respaldo.
public static class Partida
{
    public const int Version = 2;   // 1: partida de la rama de E3 (sin checkpoints); 2: con checkpoints

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
        public string estrategia;        // DianaAgente de la estrategia (RF-05); vacío = sin elegir
    }

    [System.Serializable]
    public class Datos
    {
        public int version = Version;
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
        public string estrategia;        // DianaAgente de la estrategia de la terminal; vacío = sin elegir (campo aditivo de la v2)
        public List<Checkpoint> checkpoints = new List<Checkpoint>();   // uno por reto terminado, el último al final
        public bool vistoHongo, vistoVirus;   // misión 1: muestras B y C ya observadas (campos aditivos)
        public bool biohazard;               // misión 2: evento de bioseguridad ya resuelto (campo aditivo)
    }

#if UNITY_EDITOR
    // Las pruebas automáticas (PruebaReto, PruebaLocomocion) guardan en un archivo aparte: así no leen ni
    // pisan la partida de quien prueba en el Editor. Lo fija PartidaPruebas (Editor) y dura la sesión.
    public const string ClaveRutaPrueba = "NanoLab.Partida.RutaPrueba";
#endif

    public static string Ruta
    {
        get
        {
#if UNITY_EDITOR
            string prueba = UnityEditor.SessionState.GetString(ClaveRutaPrueba, "");
            if (!string.IsNullOrEmpty(prueba)) return prueba;
#endif
            return Path.Combine(Application.persistentDataPath, "partida.json");
        }
    }
    public static string RutaRespaldo => Ruta + ".bak";
    public static string RutaCorrupta => Path.ChangeExtension(Ruta, ".corrupta.json");
    static string RutaTemporal => Ruta + ".tmp";

    public static bool Existe => File.Exists(Ruta) || File.Exists(RutaRespaldo);
    public static System.DateTime? UltimoGuardado { get; private set; }
    public static bool UltimoFueManual { get; private set; }
    // La partida en memoria salió de partida.json.bak (el archivo principal faltaba o estaba dañado).
    public static bool DesdeRespaldo { get; private set; }
    // Tras NUEVA PARTIDA no se guarda nada hasta que la escena nueva arranque (si no, el estado viejo volvía).
    internal static bool Suspendido { get; set; }

    // Copia en memoria: el autoguardado (cada minuto) y el panel ☰ no releen el JSON del disco.
    static Datos cache;
    static bool leida;
    static bool soloLectura;   // archivo de una versión más nueva de la app: se lee, pero no se sobrescribe

    public static Checkpoint UltimoCheckpoint
    {
        get
        {
            var d = Leer();
            return d != null && d.checkpoints != null && d.checkpoints.Count > 0 ? d.checkpoints[d.checkpoints.Count - 1] : null;
        }
    }

    // Vuelve a leer del disco en la próxima consulta (pruebas y menús del Editor).
    public static void Olvidar()
    {
        cache = null;
        leida = false;
        soloLectura = false;
        DesdeRespaldo = false;
    }

    public static Datos Leer()
    {
        if (leida) return cache;
        leida = true;
        DesdeRespaldo = false;
        cache = LeerArchivo(Ruta, out bool danado);
        if (danado)
        {
            // Se aparta para que el próximo guardado no mande el archivo dañado al respaldo.
            try
            {
                File.Copy(Ruta, RutaCorrupta, true);
                File.Delete(Ruta);
                Debug.LogWarning($"[Partida] {Ruta} estaba dañada: se apartó a {RutaCorrupta}.");
            }
            catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo apartar la partida dañada: " + e.Message); }
        }
        if (cache == null)
        {
            cache = LeerArchivo(RutaRespaldo, out _);
            DesdeRespaldo = cache != null;
            if (DesdeRespaldo) Debug.LogWarning("[Partida] Se cargó la copia de respaldo (partida.json.bak).");
        }
        if (cache != null)
        {
            if (cache.checkpoints == null) cache.checkpoints = new List<Checkpoint>();
            if (cache.muestras == null) cache.muestras = new List<string>();
            if (cache.recuerdos == null) cache.recuerdos = new List<string>();
            soloLectura = cache.version > Version;
            if (soloLectura) Debug.LogWarning($"[Partida] La partida es de una versión más nueva ({cache.version} > {Version}): se carga, pero no se sobrescribe.");
        }
        return cache;
    }

    // null si no existe; 'danado' si existe pero no se puede interpretar.
    static Datos LeerArchivo(string ruta, out bool danado)
    {
        danado = false;
        try
        {
            if (!File.Exists(ruta)) return null;
            string texto = File.ReadAllText(ruta);
            var d = string.IsNullOrWhiteSpace(texto) ? null : JsonUtility.FromJson<Datos>(texto);
            danado = d == null;
            return d;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Partida] No se pudo leer {Path.GetFileName(ruta)}: {e.Message}");
            danado = File.Exists(ruta);
            return null;
        }
    }

    // Autoguardado. Lo que no se toca aquí (los checkpoints) se conserva.
    public static bool Guardar() => Guardar(null, false);

    // Botón GUARDAR PARTIDA del panel ☰.
    public static bool GuardarManual() => Guardar(null, true);

    // Guarda la partida y, además, un checkpoint del reto que se acaba de terminar.
    public static bool GuardarCheckpoint(string reto) => Guardar(reto, false);

    static bool Guardar(string checkpointDe, bool manual)
    {
        if (Suspendido) return false;
        var d = Leer() ?? new Datos();
        if (soloLectura) return false;
        d.version = Version;
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
            d.estrategia = rm.EstrategiaElegida != null ? rm.EstrategiaElegida.diana.ToString() : null;
            d.vistoHongo = rm.VistoHongo;
            d.vistoVirus = rm.VistoVirus;
            d.biohazard = BiohazardEvent.YaOcurrio;
        }
        var j = JugadorNanoLab.Instancia;
        if (j != null && j.Pies(out var pies, out var yaw))
        {
            // Sentado se guarda el punto por donde se levanta (al cargar se aparece de pie, junto a la silla).
            if (JugadorNanoLab.Sentado(out var silla)) pies = new Vector3(silla.PuntoSalida.x, pies.y, silla.PuntoSalida.z);
            // Solo se guarda una posición con suelo del laboratorio debajo (si no, se aparecería cayendo al vacío).
            d.hayPosicion = JugadorNanoLab.PosicionValida(pies);
            if (d.hayPosicion) { d.pies = pies; d.yaw = yaw; }
        }
        if (!string.IsNullOrEmpty(checkpointDe))
        {
            d.checkpoints.RemoveAll(c => c.reto == checkpointDe);   // el del mismo reto se actualiza y pasa a ser el último
            d.checkpoints.Add(new Checkpoint
            {
                reto = checkpointDe, fecha = d.fecha, escena = d.escena, pies = d.pies, yaw = d.yaw,
                muestras = new List<string>(d.muestras), recuerdos = new List<string>(d.recuerdos),
                etapa = d.etapa, evidencias = d.evidencias != null ? (bool[])d.evidencias.Clone() : null, hipotesis = d.hipotesis, estrategia = d.estrategia,
            });
        }
        try
        {
            Escribir(JsonUtility.ToJson(d, true));
            cache = d;
            leida = true;
            DesdeRespaldo = false;
            UltimoGuardado = System.DateTime.Now;
            UltimoFueManual = manual;
            return true;
        }
        catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo guardar: " + e.Message); return false; }
    }

    // Se escribe aparte y se reemplaza: si la app se cierra a mitad, la partida anterior sigue intacta.
    // La que había pasa a ser el respaldo (.bak).
    static void Escribir(string json)
    {
        string tmp = RutaTemporal;
        Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
        File.WriteAllText(tmp, json);
        if (!File.Exists(Ruta)) { File.Move(tmp, Ruta); return; }
        try { File.Replace(tmp, Ruta, RutaRespaldo); }
        catch (System.Exception)
        {
            // Sin File.Replace en la plataforma: el respaldo se copia antes de cambiar el archivo.
            File.Copy(Ruta, RutaRespaldo, true);
            File.Delete(Ruta);
            File.Move(tmp, Ruta);
        }
    }

    // NUEVA PARTIDA: se borra todo, también el respaldo (si no, se cargaría al volver a abrir la app).
    public static void Borrar()
    {
        Suspendido = true;
        foreach (var ruta in new[] { Ruta, RutaRespaldo, RutaTemporal })
        {
            try { if (File.Exists(ruta)) File.Delete(ruta); }
            catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo borrar: " + e.Message); }
        }
        cache = null;
        leida = true;
        soloLectura = false;
        DesdeRespaldo = false;
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
            if (rm != null && rm.reto != null && rm.EsSuyo(d.reto) && d.evidencias != null)
            {
                rm.Restaurar((EtapaReto)d.etapa, d.evidencias, d.hipotesis, d.estrategia);
                rm.RestaurarComparacion(d.vistoHongo, d.vistoVirus);
                BiohazardEvent.Restaurar(d.biohazard);
            }

            if (!restauradaEnSesion)
            {
                restauradaEnSesion = true;
                // Al abrir la app se vuelve a la sala donde se dejó (la app arranca en Microbiología).
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
                    string origen = Partida.DesdeRespaldo ? "\n<size=80%>(desde la copia de respaldo: la última se había dañado)</size>" : "";
                    // Reconocer, no recordar: al volver se dice por dónde se iba.
                    string sigue = rm != null && rm.Actual != EtapaReto.Cierre
                        ? $"\n<size=85%>{rm.CodigoMision} · paso {rm.PasoMision} de {rm.PasosMision}: {rm.ObjetivoCorto}</size>" +
                          (rm.RetomadaAntes ? "\n<size=80%>Lo que llevabas en las manos no se guarda: repite el último paso.</size>" : "")
                        : "";
                    Aviso.Mostrar($"Partida cargada ({d.fecha}).\nMuestras: {d.muestras.Count} · Recuerdos: {d.recuerdos.Count}/{RecuerdosBaymax.Total}{origen}{sigue}", cab.position + f * 1f, Aviso.Tipo.Ok, 8f);
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
        if (rm != null && rm.reto != null && rm.EsSuyo(c.reto) && c.evidencias != null)
        {
            if (rm.Actual != EtapaReto.Exploracion) rm.Reiniciar();   // estaciones, placa y cámara como al empezar
            rm.Restaurar((EtapaReto)c.etapa, c.evidencias, c.hipotesis, c.estrategia);
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
