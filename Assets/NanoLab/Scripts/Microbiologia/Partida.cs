using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// Partida guardada en local (Application.persistentDataPath/partida.json; en Quest, la memoria de la app):
// muestras analizadas, recuerdos de Baymax, avance del caso del Centro de Experimentación, sala y posición.
// Se guarda sola (al avanzar, al quitarse el visor o pausar, al salir y cada minuto) y con el botón
// GUARDAR PARTIDA del panel ☰; al abrir la app se continúa donde se dejó. NUEVA PARTIDA la borra.
public static class Partida
{
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
    }

    public static string Ruta => Path.Combine(Application.persistentDataPath, "partida.json");
    public static bool Existe => File.Exists(Ruta);
    public static System.DateTime? UltimoGuardado { get; private set; }

    public static Datos Leer()
    {
        try
        {
            if (!Existe) return null;
            return JsonUtility.FromJson<Datos>(File.ReadAllText(Ruta));
        }
        catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo leer: " + e.Message); return null; }
    }

    // Reúne el estado actual. Lo que no está en esta sala (el caso, si se está en Química) se conserva del archivo.
    public static bool Guardar()
    {
        var d = Leer() ?? new Datos();
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
        if (j != null && j.Pies(out var pies, out var yaw)) { d.hayPosicion = true; d.pies = pies; d.yaw = yaw; }
        try
        {
            File.WriteAllText(Ruta, JsonUtility.ToJson(d, true));
            UltimoGuardado = System.DateTime.Now;
            return true;
        }
        catch (System.Exception e) { Debug.LogWarning("[Partida] No se pudo guardar: " + e.Message); return false; }
    }

    public static void Borrar()
    {
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
                yield return new WaitForSeconds(0.3f);   // el Locomotor ya está listo
                if (JugadorNanoLab.Instancia != null) JugadorNanoLab.Instancia.Teletransportar(d.pies, Quaternion.Euler(0f, d.yaw, 0f) * Vector3.forward);
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

    // Al cambiar de sala por el portal: lo pendiente se guarda antes de descargar la escena.
    void OnDestroy() { if (sucio && !restaurando) Partida.Guardar(); }
}
