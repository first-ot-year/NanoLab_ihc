using TMPro;
using UnityEngine;
using UnityEngine.UI;
using K = KitVacuna;

// Monitor físico junto al microscopio (presencia): el ocular solo enseña la muestra, y las características, el
// cuaderno de evidencias, la micrografía, la comparación y las observaciones se leen aquí al levantar la cabeza,
// como en un microscopio con cámara. Lo crea MicroscopioOptico la primera vez que se mira por él, fijo en el mundo
// a un lado del ocular y de cara a donde estaba el jugador; HUDMicroscopio le cuelga sus textos (UsarMonitor).
public class MonitorMicroscopio : MonoBehaviour
{
    const float AnchoPantalla = 0.46f, AltoPantalla = 0.30f;
    const float PxAncho = 1000f, PxAlto = 660f;

    public RectTransform Pantalla { get; private set; }
    TMP_Text espera;

    public static MonitorMicroscopio Crear(Transform ocular, Transform cabeza)
    {
        var go = new GameObject("MonitorMicroscopio");
        var m = go.AddComponent<MonitorMicroscopio>();
        m.Colocar(ocular, cabeza);
        m.Montar();
        return m;
    }

    // A la derecha del ocular (vista desde el jugador), algo por encima, mirando al jugador solo en horizontal.
    // Si choca con algo, a la izquierda. Un pie baja hasta la mesa si la hay debajo.
    void Colocar(Transform ocular, Transform cabeza)
    {
        var hacia = Vector3.ProjectOnPlane(ocular.position - cabeza.position, Vector3.up);
        if (hacia.sqrMagnitude < 1e-4f) hacia = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
        hacia.Normalize();
        var derecha = Vector3.Cross(Vector3.up, hacia).normalized;
        var rot = Quaternion.LookRotation(hacia, Vector3.up);   // +z se aleja del jugador: la pantalla se lee desde -z
        var medio = new Vector3(AnchoPantalla / 2f + 0.02f, AltoPantalla / 2f + 0.02f, 0.03f);
        Vector3 pos = ocular.position + derecha * 0.4f + Vector3.up * 0.1f + hacia * 0.05f;
        if (Physics.CheckBox(pos, medio, rot, ~(1 << CapasNanoLab.BloqueoJugador), QueryTriggerInteraction.Ignore))
        {
            var izq = ocular.position - derecha * 0.4f + Vector3.up * 0.1f + hacia * 0.05f;
            if (!Physics.CheckBox(izq, medio, rot, ~(1 << CapasNanoLab.BloqueoJugador), QueryTriggerInteraction.Ignore)) pos = izq;
        }
        transform.SetPositionAndRotation(pos, rot);
    }

    void Montar()
    {
        var oscuro = K.Plano(new Color(0.09f, 0.1f, 0.12f));
        var gris = K.Escena("Marco", new Color(0.55f, 0.58f, 0.62f));
        K.Caja(transform, "Carcasa", new Vector3(0f, 0f, 0.012f), new Vector3(AnchoPantalla + 0.03f, AltoPantalla + 0.03f, 0.024f), oscuro);
        // Pie hasta la mesa (si hay una debajo); si no, queda como un monitor de brazo.
        var baseP = transform.position - Vector3.up * (AltoPantalla / 2f + 0.015f);
        if (Physics.Raycast(baseP, Vector3.down, out var hit, 0.6f, ~(1 << CapasNanoLab.BloqueoJugador), QueryTriggerInteraction.Ignore))
        {
            float largo = baseP.y - hit.point.y;
            var pie = K.Caja(transform, "Pie", Vector3.zero, new Vector3(0.04f, largo, 0.03f), gris).transform;
            pie.position = baseP - Vector3.up * (largo / 2f) + transform.forward * 0.02f;
            var pata = K.Caja(transform, "Base", Vector3.zero, new Vector3(0.2f, 0.012f, 0.14f), gris).transform;
            pata.position = hit.point + Vector3.up * 0.006f + transform.forward * 0.02f;
        }

        // Pantalla: canvas en el mundo (con prueba de profundidad normal), delante de la carcasa.
        var go = new GameObject("Pantalla", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        Pantalla = (RectTransform)go.transform;
        Pantalla.sizeDelta = new Vector2(PxAncho, PxAlto);
        Pantalla.localPosition = new Vector3(0f, 0f, -0.001f);
        Pantalla.localScale = Vector3.one * (AnchoPantalla / PxAncho);
        var fondo = new GameObject("Fondo", typeof(RectTransform)).AddComponent<Image>();
        fondo.rectTransform.SetParent(Pantalla, false);
        fondo.rectTransform.sizeDelta = new Vector2(PxAncho, PxAlto);
        fondo.color = new Color(0.02f, 0.06f, 0.11f, 1f);
        fondo.raycastTarget = false;
        espera = new GameObject("Espera", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        espera.rectTransform.SetParent(Pantalla, false);
        espera.rectTransform.sizeDelta = new Vector2(PxAncho - 80f, 200f);
        if (EstiloNanoLab.Fuente != null) espera.font = EstiloNanoLab.Fuente;
        espera.fontSize = 34;
        espera.alignment = TextAlignmentOptions.Center;
        espera.color = new Color(0.55f, 0.66f, 0.78f);
        espera.text = "MONITOR DEL MICROSCOPIO\n<size=70%>Pon una muestra en la platina y mira por el ocular.</size>";
        foreach (var c in GetComponentsInChildren<Collider>(true)) Destroy(c);
    }

    // El texto de espera solo se ve mientras el monitor no tiene los datos de una muestra.
    void LateUpdate()
    {
        if (espera == null) return;
        bool vacio = Pantalla.childCount <= 2;   // fondo y espera
        if (espera.gameObject.activeSelf != vacio) espera.gameObject.SetActive(vacio);
    }
}
