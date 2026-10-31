using UnityEngine;

// En VR la cabeza real se sale de la cápsula del jugador al inclinarse: pasando muy cerca de un
// armario se metía la cámara en la madera y se veía su interior. Si la cabeza entra en un mueble o
// pared, la vista se oscurece (como en los juegos de VR); al apartarse, vuelve. No actúa con el
// microscopio (la cara se acerca al ocular a propósito) ni con objetos agarrables.
// Lo instala JugadorNanoLab en la raíz del rig.
public class ProteccionCabeza : MonoBehaviour
{
    [Tooltip("A esta distancia de un collider empieza a oscurecerse (m).")]
    public float radioAviso = 0.14f;
    [Tooltip("A esta distancia (o dentro) queda casi negro (m).")]
    public float radioNegro = 0.06f;

    static readonly Collider[] buffer = new Collider[16];
    Transform cabeza;
    Material mat;
    Renderer velo;
    float alpha;

    void Start()
    {
        var rig = GetComponentInChildren<OVRCameraRig>(true);
        if (rig == null) rig = FindObjectOfType<OVRCameraRig>();
        if (rig == null) { enabled = false; return; }
        cabeza = rig.centerEyeAnchor;
        var cam = cabeza.GetComponent<Camera>();

        // Velo negro pegado a los ojos, sin prueba de profundidad y dibujado al final.
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.name = "VeloCabeza";
        q.transform.SetParent(cabeza, false);
        float d = (cam != null ? cam.nearClipPlane : 0.05f) + 0.01f;
        q.transform.localPosition = new Vector3(0f, 0f, d);
        q.transform.localScale = Vector3.one * d * 6f;
        mat = new Material(Shader.Find("UI/Default")) { name = "VeloCabeza", renderQueue = 5000 };
        mat.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
        mat.color = new Color(0f, 0f, 0f, 0f);
        velo = q.GetComponent<Renderer>();
        velo.sharedMaterial = mat;
        velo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        velo.receiveShadows = false;
        velo.enabled = false;
    }

    void LateUpdate()
    {
        if (cabeza == null) return;
        float objetivo = 0f;
        if (!MicroscopioOptico.AlgunoActivo)
        {
            if (Dentro(radioNegro)) objetivo = 0.92f;
            else if (Dentro(radioAviso)) objetivo = 0.55f;
        }
        alpha = Mathf.MoveTowards(alpha, objetivo, Time.deltaTime * 4f);
        mat.color = new Color(0f, 0f, 0f, alpha);
        velo.enabled = alpha > 0.01f;
    }

    bool Dentro(float radio)
    {
        int n = Physics.OverlapSphereNonAlloc(cabeza.position, radio, buffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = buffer[i];
            if (c.transform.IsChildOf(transform) || c.attachedRigidbody != null || c is CharacterController) continue;
            if (c.GetComponentInParent<MicroscopioOptico>() != null || c.GetComponentInParent<EstacionAnalisis3D>() != null) continue;
            if (c.GetComponentInParent<CabinaDescontaminacion>() != null) continue;   // se espera pegado a la puerta
            return true;
        }
        return false;
    }
}
