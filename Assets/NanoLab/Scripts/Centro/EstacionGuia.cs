using TMPro;
using UnityEngine;

// Cartel de una estación del centro: número, nombre, qué se hace aquí y botón PISTA.
// PISTA es progresiva: 1) una pregunta que hace pensar, 2) una pista concreta, 3) la solución (RetoManager.PedirPista).
// Se ilumina cuando es la estación del objetivo actual (una meta a la vez).
public class EstacionGuia : MonoBehaviour
{
    public int numero;
    public string titulo;
    [TextArea(2, 4)] public string descripcion;
    public TMP_Text tituloTxt, textoTxt;
    public Renderer marcoActivo;            // brillo pulsante cuando es la estación actual
    public BotonFisico pista;
    public Transform puntoBaliza;

    public bool Activa { get; private set; }
    string pistaMostrada;

    // "Estación que despierta" (idea 2 de la revisión del profesor): la estación del paso se enciende como un equipo
    // en uso, sin carteles nuevos: una luz cálida que respira sobre la zona de trabajo, un charco de luz en el suelo,
    // el marco del cartel que late y, si el jugador está lejos y no mira hacia ella, un "ping" espacial que sale de
    // la propia estación (idea 5): el oído lleva la cabeza hacia allí.
    static readonly Color ColorDespierto = new Color(1f, 0.84f, 0.45f);
    const float DistanciaPing = 3f, CadenciaPing = 7f;
    static Sprite spCharco;
    Light luzEstacion;
    SpriteRenderer charco;
    float encendido, tPing;
    Vector3 escalaMarco;

    public float Encendido => encendido;   // para las pruebas

    void OnEnable() { if (pista != null) pista.Pulsado += PedirPista; }
    void OnDisable() { if (pista != null) pista.Pulsado -= PedirPista; }

    void Update()
    {
        if (!Activa && encendido <= 0f) return;
        encendido = Mathf.MoveTowards(encendido, Activa ? 1f : 0f, Time.deltaTime / 0.6f);
        if (luzEstacion == null) MontarDespertar();
        float resp = 0.7f + 0.3f * Mathf.Sin(Time.time * 2.2f);
        luzEstacion.enabled = encendido > 0.01f;
        luzEstacion.intensity = 1.1f * encendido * resp;
        charco.enabled = encendido > 0.01f;
        var c = ColorDespierto; c.a = 0.32f * encendido * resp; charco.color = c;
        if (marcoActivo != null) marcoActivo.transform.localScale = escalaMarco * (1f + 0.035f * encendido * Mathf.Sin(Time.time * 4f));
        if (Activa) Ping();
    }

    void Ping()
    {
        if (Time.time < tPing) return;
        var cab = ManosRig.Cabeza;
        var rm = RetoManager.Instancia;
        if (cab == null || MicroscopioOptico.AlgunoActivo || (rm != null && !rm.JugadorDentro)) return;
        var p = Centro;
        var d = new Vector3(p.x - cab.position.x, 0f, p.z - cab.position.z);
        var f = new Vector3(cab.forward.x, 0f, cab.forward.z);
        bool mirando = f.sqrMagnitude > 1e-4f && Vector3.Angle(f, d) < 30f;
        if (d.magnitude < DistanciaPing || mirando) return;
        tPing = Time.time + CadenciaPing;
        Sonidos.Llamada(p + Vector3.up * 1.2f);
    }

    // Centro de la zona de trabajo (el punto al que lleva la luz guía).
    Vector3 Centro => puntoBaliza != null ? puntoBaliza.position : transform.position;

    void MontarDespertar()
    {
        if (marcoActivo != null) escalaMarco = marcoActivo.transform.localScale;
        var p = Centro;
        float suelo = p.y;
        if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var hit, 3f, ~0, QueryTriggerInteraction.Ignore)) suelo = hit.point.y;
        var go = new GameObject("LuzDespierta");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(p.x, suelo + 2.1f, p.z);
        luzEstacion = go.AddComponent<Light>();
        luzEstacion.type = LightType.Point;
        luzEstacion.color = ColorDespierto;
        luzEstacion.range = 2.6f;
        luzEstacion.shadows = LightShadows.None;
        luzEstacion.renderMode = LightRenderMode.ForcePixel;
        luzEstacion.intensity = 0f;
        if (spCharco == null)
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "CharcoLuz" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float r = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f).magnitude * 2f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f) * 255));
                }
            tex.SetPixels32(px);
            tex.Apply();
            spCharco = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n / 2.4f);   // 2,4 m de diámetro
        }
        charco = new GameObject("CharcoLuz").AddComponent<SpriteRenderer>();
        charco.transform.SetParent(transform, false);
        charco.transform.position = new Vector3(p.x, suelo + 0.012f, p.z);
        charco.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        charco.sprite = spCharco;
        charco.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        charco.receiveShadows = false;
        charco.enabled = false;
    }

    public void Mostrar(bool activa, string objetivo)
    {
        if (activa && !Activa) tPing = Time.time + 4f;   // el paso nuevo ya suena (luz guía): el primer ping espera
        Activa = activa;
        if (marcoActivo != null) marcoActivo.enabled = activa;
        if (tituloTxt != null) tituloTxt.text = $"<color=#{(activa ? "FFD25A" : "5FD4FF")}>{numero}</color>  {titulo}";
        Refrescar(objetivo);
    }

    void Refrescar(string objetivo)
    {
        if (textoTxt == null) return;
        string s = Activa && !string.IsNullOrEmpty(objetivo)
            ? $"<color=#FFD25A><b>AHORA:</b></color> {objetivo}"
            : $"<color=#A9BED2>{descripcion}</color>";
        if (!string.IsNullOrEmpty(pistaMostrada)) s += $"\n<color=#FFD25A>{pistaMostrada}</color>";
        textoTxt.text = s;
    }

    public void LimpiarPista() => pistaMostrada = null;

    public void PedirPista()
    {
        var rm = RetoManager.Instancia;
        if (rm == null) return;
        pistaMostrada = rm.PedirPista();
        Refrescar(rm.ObjetivoActual);
        Sonidos.Pista(transform.position);
        Aviso.Mostrar(pistaMostrada, transform.position + Vector3.up * 0.35f, Aviso.Tipo.Pista, 8f);
    }
}
