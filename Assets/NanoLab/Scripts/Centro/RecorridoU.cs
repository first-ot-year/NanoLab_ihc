using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using K = KitVacuna;

// Distribución "recorrido en U" del Centro de Experimentación (pedida tras la revisión del profesor: que el estudiante
// no se pierda y que la sala no se sienta cargada). La aplica el menú NanoLab › Recorrido en U, que mueve en la escena
// las estaciones y los equipos (son geometría estática horneada: no se pueden mover en tiempo de ejecución) y deja
// la marca "RecorridoU" en el centro. Con la marca:
//   - MISIÓN 1 en la pared sur (incubadora → banco → cabina → tinción → microscopía), biblioteca en la pared este.
//   - MISIÓN 2 a lo largo de una PARED DE VIDRIO (terminal → prensa → cámara → autoclave); conclusiones en la
//     esquina sureste, junto a la microscopía (se usa en las misiones 1 y 2).
//   - MISIÓN 3 en su propia SALA DE VACUNAS, detrás del vidrio (mesa de vacunas, ultracongelador, incubadora CO2).
//     La puerta de vidrio se abre al completar la misión 2 (recuerdo 2 de Baymax).
//   - El centro de la sala queda despejado.
// Este archivo tiene las posiciones que usan las piezas que se crean al jugar (vacuna, conclusiones) y construye
// la pared de vidrio con su puerta.
public static class RecorridoU
{
    public const string Marca = "RecorridoU";

    // Sala: x 6.87–24.14, z -0.25–17.26 (MontarCentroExperimentacion). Pared de vidrio en z = ParedZ.
    public const float X0 = 6.87f, X1 = 24.14f, Z1 = 17.26f;
    public const float ParedZ = 11.0f;
    public const float PuertaX0 = 7.35f, PuertaX1 = 8.85f;

    public static readonly Vector3 PosVacuna = new Vector3(15.5f, 0f, 16.42f);
    public const float YawVacuna = 180f;
    // Conclusiones se usa dos veces: al final de la misión 1 (clasificar las 3 muestras, justo después de la
    // microscopía) y al final de la misión 2 (explicar el resultado). Va en la esquina sureste, entre la
    // microscopía y la biblioteca, para que la misión 1 no obligue a cruzar la sala.
    public static readonly Vector3 PosConclusiones = new Vector3(23.4f, 0f, 2.6f);
    public const float YawConclusiones = -90f;

    static int escenaVista = -1;
    static bool activo;

    // ¿La escena tiene aplicada la distribución? (se mira una vez por escena)
    public static bool Activo
    {
        get
        {
            var s = SceneManager.GetActiveScene();
            if (s.handle != escenaVista)
            {
                escenaVista = s.handle;
                activo = s.IsValid() && s.GetRootGameObjects().Any(r => r.GetComponentsInChildren<Transform>(true).Any(t => t.name == Marca));
            }
            return activo;
        }
    }

    public static bool AlNorte(Vector3 p) => p.z > ParedZ;

    // Punto de paso delante de la puerta de vidrio, del lado indicado.
    public static Vector3 Puerta(bool ladoNorte, float y) => new Vector3((PuertaX0 + PuertaX1) / 2f, y, ParedZ + (ladoNorte ? 0.7f : -0.7f));

    // Monta la pared (una vez). La llama MontajeMisiones al preparar el centro.
    public static ParedVacunas Montar(Transform centro, float suelo)
    {
        if (!Activo) return null;
        var ya = Object.FindObjectOfType<ParedVacunas>();
        if (ya != null) return ya;
        var g = new GameObject("ParedVacunas");
        if (centro != null) g.transform.SetParent(centro, true);
        g.transform.position = new Vector3(0f, suelo, 0f);
        var p = g.AddComponent<ParedVacunas>();
        p.Construir();
        return p;
    }
}
