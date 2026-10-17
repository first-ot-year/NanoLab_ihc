using UnityEngine;

// Espejo plano para verse con el avatar: una cámara colocada en el reflejo de los ojos del
// jugador renderiza (con un frustum que pasa justo por los bordes del espejo) a una textura
// que se muestra en el cristal. Solo se renderiza cuando el jugador está cerca y delante.
[DefaultExecutionOrder(200)]
public class EspejoAvatar : MonoBehaviour
{
    public Renderer superficie;           // quad del cristal (su cara visible mira a -Z local)
    public int resolucion = 768;         // alto de la textura en píxeles
    public float distanciaActiva = 7f;

    Camera cam;
    RenderTexture rt;
    bool cada2;

    void Start() => Preparar();

    public void Preparar()
    {
        if (cam != null) return;
        var s = superficie.transform.lossyScale;
        int alto = resolucion, ancho = Mathf.Max(64, Mathf.RoundToInt(resolucion * s.x / s.y));
        rt = new RenderTexture(ancho, alto, 24) { name = "Espejo", antiAliasing = 2 };
        var go = new GameObject("CamaraEspejo");
        go.transform.SetParent(transform, false);
        cam = go.AddComponent<Camera>();
        cam.enabled = false;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.Skybox;
        var mat = superficie.material;   // instancia propia
        mat.mainTexture = rt;
        mat.mainTextureScale = new Vector2(-1f, 1f);   // el reflejo se ve invertido de izquierda a derecha
        mat.mainTextureOffset = new Vector2(1f, 0f);
    }

    void OnDestroy() { if (rt != null) rt.Release(); }

    void LateUpdate()
    {
        var ojo = ManosRig.Cabeza;
        if (ojo == null || cam == null) return;
        var t = superficie.transform;
        var n = -t.forward;                                   // normal del lado de la sala
        var centro = t.position;
        float lado = Vector3.Dot(ojo.position - centro, n);
        if (lado <= 0.05f || (ojo.position - centro).sqrMagnitude > distanciaActiva * distanciaActiva) return;
        cada2 = !cada2;
        if (cada2) return;                                    // 36 Hz bastan para un espejo
        RenderizarDesde(ojo.position);
    }

    // Visto desde el reflejo del ojo, la izquierda y la derecha del cristal se intercambian.
    public void RenderizarDesde(Vector3 ojo)
    {
        var t = superficie.transform;
        var n = -t.forward;
        var pe = ojo - 2f * Vector3.Dot(ojo - t.position, n) * n;
        var pa = t.TransformPoint(new Vector3(0.5f, -0.5f, 0f));    // abajo-izquierda vista desde el reflejo
        var pb = t.TransformPoint(new Vector3(-0.5f, -0.5f, 0f));   // abajo-derecha
        var pc = t.TransformPoint(new Vector3(0.5f, 0.5f, 0f));     // arriba-izquierda

        // Proyección generalizada (Kooima): el plano cercano coincide con el espejo,
        // así no se ve lo que hay detrás de él.
        var vr = (pb - pa).normalized;
        var vu = (pc - pa).normalized;
        var vn = -Vector3.Cross(vr, vu).normalized;           // hacia el ojo reflejado
        var va = pa - pe; var vb = pb - pe; var vc = pc - pe;
        float d = -Vector3.Dot(va, vn);
        if (d < 0.05f) return;
        float cerca = d * 0.995f, lejos = 40f;
        float l = Vector3.Dot(vr, va) * cerca / d, r = Vector3.Dot(vr, vb) * cerca / d;
        float b = Vector3.Dot(vu, va) * cerca / d, tp = Vector3.Dot(vu, vc) * cerca / d;
        cam.transform.SetPositionAndRotation(pe, Quaternion.LookRotation(-vn, vu));
        cam.nearClipPlane = cerca; cam.farClipPlane = lejos;
        cam.projectionMatrix = Matrix4x4.Frustum(l, r, b, tp, cerca, lejos);
        cam.Render();
    }
}
