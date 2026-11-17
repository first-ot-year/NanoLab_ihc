using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Entrada al Centro de Experimentación con luces por zonas (revisión: primera impresión que explique la sala sin leer).
// La primera vez que el jugador cruza la puerta en la sesión, delante de cada estación se enciende en el suelo un
// charco de luz, una tras otra, siguiendo el recorrido en U: verde azulado para la MISIÓN 1, morado para la MISIÓN 2
// y coral para la sala de vacunas (MISIÓN 3). Cada zona suena con un tono y Baymax resume en una frase qué hay que
// hacer. Las luces se apagan solas y empieza la luz guía. Las luces del techo están horneadas: esto son superficies
// emisivas (sprites) en el suelo, sin coste de luces en tiempo real.
public class EntradaCentro : MonoBehaviour
{
    static readonly Color[] ColorMision = { new Color(0.25f, 0.85f, 0.75f), new Color(0.7f, 0.5f, 1f), new Color(1f, 0.55f, 0.42f) };
    const float Intervalo = 0.32f, Encendido = 3.2f, Apagado = 0.8f;

    // (nombre del objeto en el centro, misión 0-2)
    static readonly (string nombre, int mision)[] Orden =
    {
        ("Incubadora", 0), ("1_BancoMuestras", 0), ("CabinaBioseguridad", 0), ("2_Tincion", 0), ("3_Microscopia", 0),
        ("PanelConclusiones", 0), ("4_Biblioteca", 0),
        ("5_Terminal", 1), ("6_Prensa", 1), ("7_CamaraExperimental", 1), ("Autoclave", 1),
        ("EstacionVacuna", 2), ("Ultracongelador", 2), ("IncubadoraCO2", 2),
    };

    public static bool YaVista { get; private set; }   // una vez por sesión
    public bool Reproduciendo => t >= 0f;
    public int Zonas => charcos.Count;

    readonly List<(SpriteRenderer sr, int mision)> charcos = new List<(SpriteRenderer, int)>();
    float t = -1f;
    bool dentroAntes = true;   // si el jugador ya empieza dentro (partida guardada), no se reproduce
    static Sprite disco;

    public static EntradaCentro Montar(Transform centro, float suelo)
    {
        var ya = FindObjectOfType<EntradaCentro>();
        if (ya != null) return ya;
        var e = new GameObject("EntradaCentro").AddComponent<EntradaCentro>();
        if (centro != null) e.transform.SetParent(centro, true);
        e.Construir(centro, suelo);
        return e;
    }

    void Construir(Transform centro, float suelo)
    {
        if (disco == null) disco = Sprite.Create(Radial(128), new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f);
        var todos = centro != null ? centro.GetComponentsInChildren<Transform>(true) : new Transform[0];
        var vacuna = FindObjectsOfType<Transform>(true).Where(x => x.name == "EstacionVacuna");
        foreach (var (nombre, mision) in Orden)
        {
            var t = todos.Concat(vacuna).FirstOrDefault(x => x.name == nombre);
            if (t == null) continue;
            // Delante de la estación (el usuario queda en su +z local), a ras de suelo.
            var p = t.TransformPoint(new Vector3(0f, 0f, nombre.StartsWith("Panel") ? 0.8f : 1.0f));
            var sr = new GameObject("Luz_" + nombre).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.transform.SetPositionAndRotation(new Vector3(p.x, suelo + 0.012f, p.z), Quaternion.Euler(90f, 0f, 0f));
            sr.transform.localScale = Vector3.one * 2.2f;
            sr.sprite = disco;
            sr.sortingOrder = 1;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.color = Color.clear;
            sr.gameObject.SetActive(false);
            charcos.Add((sr, mision));
        }
        var rm = RetoManager.Instancia;
        dentroAntes = rm != null && rm.JugadorDentro;
    }

    // Pública para las pruebas.
    public void Reproducir()
    {
        YaVista = true;
        t = 0f;
        foreach (var c in charcos) c.sr.gameObject.SetActive(true);
        var cab = ManosRig.Cabeza;
        if (cab != null)
        {
            var f = Vector3.ProjectOnPlane(cab.forward, Vector3.up).normalized;
            Aviso.Mostrar("<b>BAYMAX:</b> este es el CENTRO DE EXPERIMENTACIÓN.\n" +
                          "<color=#40D9BF>Verde azulado: MISIÓN 1</color> · <color=#B380FF>Morado: MISIÓN 2</color> · <color=#FF8C6B>Tras el vidrio: MISIÓN 3</color>\n" +
                          "Sigue la LUZ GUÍA: te lleva paso a paso.",
                          cab.position + f * 1.2f, Aviso.Tipo.Info, 8f);
        }
    }

    void Update()
    {
        var rm = RetoManager.Instancia;
        bool dentro = rm != null && rm.JugadorDentro;
        if (dentro && !dentroAntes && !YaVista && !MicroscopioOptico.AlgunoActivo) Reproducir();
        dentroAntes = dentro;
        if (t < 0f) return;
        float antes = t;
        t += Time.deltaTime;
        int misionSonada = -1;
        for (int i = 0; i < charcos.Count; i++)
        {
            var (sr, mision) = charcos[i];
            float inicio = i * Intervalo;
            float fin = charcos.Count * Intervalo + Encendido;
            float a = t < inicio ? 0f : t < inicio + 0.35f ? (t - inicio) / 0.35f : t < fin ? 1f : Mathf.Clamp01(1f - (t - fin) / Apagado);
            var c = ColorMision[mision]; c.a = 0.45f * a * (0.85f + 0.15f * Mathf.Sin(t * 4f + i));
            sr.color = c;
            sr.transform.localScale = Vector3.one * (2.2f * Mathf.Lerp(0.5f, 1f, Mathf.Clamp01(a * 1.5f)));
            // Un tono al empezar cada misión.
            if (antes < inicio && t >= inicio && (i == 0 || charcos[i - 1].mision != mision)) misionSonada = mision;
        }
        if (misionSonada >= 0 && charcos.Count > 0) Sonidos.Etapa(charcos.First(c => c.mision == misionSonada).sr.transform.position + Vector3.up);
        if (t > charcos.Count * Intervalo + Encendido + Apagado)
        {
            t = -1f;
            foreach (var c in charcos) c.sr.gameObject.SetActive(false);
        }
    }

    // Charco de luz: centro brillante y borde que se desvanece.
    static Texture2D Radial(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float r = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f).magnitude * 2f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), 1.6f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
