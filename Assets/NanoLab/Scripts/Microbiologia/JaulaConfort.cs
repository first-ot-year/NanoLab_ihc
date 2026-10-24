using UnityEngine;

// Marco de reposo (RNF-01): esfera de rejilla alrededor de la cabeza, orientada con el rig (no con la
// cabeza), que aparece en la periferia solo mientras el joystick desplaza o gira al jugador.
// Al ojo le da una referencia quieta respecto al cuerpo, que coincide con lo que siente el oído
// interno: es lo que reduce el mareo del desplazamiento continuo. La instala JugadorNanoLab.
[DefaultExecutionOrder(10001)]   // después de AlturaEstable: se coloca donde se va a dibujar la cabeza
public class JaulaConfort : MonoBehaviour
{
    [Tooltip("Ángulo total (grados) del centro de la vista que queda siempre libre.")]
    [Range(40f, 120f)] public float campoLibre = 70f;
    [Tooltip("Velocidad (m/s) a la que la rejilla llega a su intensidad máxima.")]
    public float velocidadPlena = 1f;
    [Range(0f, 1f)] public float intensidadMax = 0.85f;

    Transform origen, cabeza;
    Material mat;
    Vector3 posAnterior;
    float yawAnterior, intensidad, pulsoGiro;
    bool pausado;
    static readonly int IdIntensidad = Shader.PropertyToID("_Intensidad");

    public static JaulaConfort Crear(Transform origen, Transform cabeza, int capa)
    {
        var plantilla = Resources.Load<Material>("NanoLab/JaulaConfort");
        if (plantilla == null) { Debug.LogWarning("[Confort] Falta Resources/NanoLab/JaulaConfort."); return null; }
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(go.GetComponent<Collider>());
        go.name = "JaulaConfort";
        go.layer = capa;   // el jugador la ve; los espejos no
        go.transform.SetParent(origen, false);
        go.transform.localScale = Vector3.one * 1.2f;
        var r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        var j = go.AddComponent<JaulaConfort>();
        j.origen = origen;
        j.cabeza = cabeza;
        j.mat = new Material(plantilla);
        r.sharedMaterial = j.mat;
        return j;
    }

    public void Pausar(bool p) => pausado = p;

    void Start()
    {
        posAnterior = origen.position;
        yawAnterior = origen.eulerAngles.y;
        Aplicar(0f);
    }

    void LateUpdate()
    {
        if (origen == null || cabeza == null) return;
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // Solo el desplazamiento artificial (el del rig); andar de verdad por la habitación no cuenta.
        var d = origen.position - posAnterior; d.y = 0f;
        float vel = d.magnitude / dt;
        if (d.magnitude > 1.5f) vel = 0f;   // salto grande (teletransporte, recolocación): no es marcha
        float giro = Mathf.Abs(Mathf.DeltaAngle(yawAnterior, origen.eulerAngles.y));
        if (giro > 1f) pulsoGiro = 1f;      // giro por pasos: un destello breve de la rejilla
        posAnterior = origen.position;
        yawAnterior = origen.eulerAngles.y;
        pulsoGiro = Mathf.MoveTowards(pulsoGiro, 0f, dt / 0.35f);

        float objetivo = pausado ? 0f : Mathf.Max(Mathf.Clamp01(vel / velocidadPlena), pulsoGiro);
        // Entra rápido (antes de que el cerebro note el conflicto) y sale despacio.
        intensidad = Mathf.MoveTowards(intensidad, objetivo, dt / (objetivo > intensidad ? 0.12f : 0.4f));
        Aplicar(intensidad * intensidadMax);

        // Centrada en los ojos y orientada con el rig: quieta respecto al cuerpo del jugador.
        transform.SetPositionAndRotation(cabeza.position, origen.rotation);
    }

    void Aplicar(float i)
    {
        if (mat == null) return;
        mat.SetFloat(IdIntensidad, i);
        float semi = campoLibre * 0.5f;
        mat.SetFloat("_CosLibre", Mathf.Cos(semi * Mathf.Deg2Rad));
        mat.SetFloat("_CosLleno", Mathf.Cos(Mathf.Min(89f, semi + 25f) * Mathf.Deg2Rad));
    }
}
