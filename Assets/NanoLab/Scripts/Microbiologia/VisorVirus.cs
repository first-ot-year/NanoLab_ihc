using UnityEngine;

// Renderiza el virus girando a una RenderTexture para la vista circular del HUD
// del microscopio. Vive lejos de la sala, en su propia capa. Detrás del virus hay un
// resplandor y alrededor flotan partículas (aspecto de micrografía electrónica coloreada).
public class VisorVirus : MonoBehaviour
{
    public const int Capa = 30;
    static VisorVirus instancia;

    public RenderTexture RT { get; private set; }
    Camera cam;
    Transform pivote;
    GameObject modelo;
    Transform particulas;
    const float DistanciaBase = 3.6f;

    public static VisorVirus Obtener()
    {
        if (instancia == null)
        {
            var go = new GameObject("VisorVirus");
            go.transform.position = new Vector3(0f, -500f, 0f);
            instancia = go.AddComponent<VisorVirus>();
            instancia.Crear();
        }
        return instancia;
    }

    void Crear()
    {
        RT = new RenderTexture(768, 768, 16) { name = "VisorVirusRT" };
        RT.Create();
        var cgo = new GameObject("Camara");
        cgo.transform.SetParent(transform, false);
        cgo.transform.localPosition = new Vector3(0f, 0f, -DistanciaBase);
        cam = cgo.AddComponent<Camera>();
        cam.cullingMask = 1 << Capa;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.04f, 0.09f, 1f);
        cam.fieldOfView = 35f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 20f;
        cam.targetTexture = RT;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        pivote = new GameObject("Pivote").transform;
        pivote.SetParent(transform, false);
        Ambiente();

        // Las cámaras del jugador no deben ver esta capa.
        foreach (var c in FindObjectsOfType<Camera>()) if (c != cam) c.cullingMask &= ~(1 << Capa);
        Activo(false);
    }

    public void Mostrar(MuestraData m, Material holo)
    {
        if (modelo != null) Destruir(modelo);
        modelo = ModeloVirus.Instanciar(m, holo, pivote, 2f);
        foreach (var t in modelo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Capa;
    }

    public void Zoom(float factor) => cam.transform.localPosition = new Vector3(0f, 0f, -DistanciaBase / Mathf.Max(0.1f, factor));

    public void Activo(bool v) { if (cam != null) cam.enabled = v; }

    public void RenderAhora() => cam.Render();   // para las capturas desde el Editor

    void Update()
    {
        if (pivote != null) pivote.Rotate(8f * Time.deltaTime, 22f * Time.deltaTime, 0f, Space.World);
        if (particulas != null) particulas.Rotate(0f, 0f, 2.5f * Time.deltaTime, Space.Self);
    }

    // Resplandor azul detrás del virus y motas de partículas (material aditivo de Resources).
    void Ambiente()
    {
        var mat = Resources.Load<Material>("NanoLab/Resplandor");
        if (mat == null) return;
        var halo = Quad("Resplandor", transform, mat);
        halo.localPosition = new Vector3(0f, 0f, 1.4f);
        halo.localScale = Vector3.one * 4.6f;

        var tenue = new Material(mat) { name = "Particula" };
        tenue.SetFloat("_Intensidad", mat.GetFloat("_Intensidad") * 0.9f);
        particulas = new GameObject("Particulas").transform;
        particulas.SetParent(transform, false);
        var rnd = new System.Random(5);
        float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
        for (int i = 0; i < 60; i++)
        {
            var q = Quad("Particula", particulas, tenue);
            var dir = new Vector2(R(-1f, 1f), R(-1f, 1f)).normalized * R(1.15f, 2.2f);
            q.localPosition = new Vector3(dir.x, dir.y, R(-0.6f, 1.6f));
            q.localScale = Vector3.one * R(0.025f, 0.09f);
        }
    }

    static Transform Quad(string nombre, Transform padre, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destruir(go.GetComponent<Collider>());
        go.name = nombre;
        go.layer = Capa;
        go.transform.SetParent(padre, false);
        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go.transform;
    }

    static void Destruir(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
}
