using TMPro;
using UnityEngine;

// Pequeño panel sobre el mando izquierdo con el objetivo actual: siempre a mano, sin menús.
// Solo se ve dentro del Centro de Experimentación.
public class ObjetivoMuneca : MonoBehaviour
{
    TextMeshPro texto;
    GameObject fondo;
    Transform mano;

    public static ObjetivoMuneca Crear(Material materialFondo)
    {
        var go = new GameObject("ObjetivoMuneca");
        var o = go.AddComponent<ObjetivoMuneca>();
        o.fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(o.fondo.GetComponent<Collider>());
        o.fondo.transform.SetParent(go.transform, false);
        o.fondo.transform.localPosition = new Vector3(0f, 0f, 0.002f);
        o.fondo.transform.localScale = new Vector3(0.24f, 0.1f, 1f);
        if (materialFondo != null) o.fondo.GetComponent<Renderer>().sharedMaterial = materialFondo;
        var t = new GameObject("Texto").AddComponent<TextMeshPro>();
        t.transform.SetParent(go.transform, false);
        t.fontSize = 0.11f;
        t.rectTransform.sizeDelta = new Vector2(0.22f, 0.09f);
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = true;
        t.color = Color.white;
        o.texto = t;
        go.SetActive(false);
        return o;
    }

    public void Poner(string objetivo) { if (texto != null) texto.text = "<color=#FFD25A><b>OBJETIVO</b></color>\n" + objetivo; }

    public void Visible(bool v) { if (gameObject.activeSelf != v) gameObject.SetActive(v); }

    void LateUpdate()
    {
        if (mano == null)
        {
            var rig = FindObjectOfType<OVRCameraRig>();
            if (rig == null || rig.leftHandAnchor == null) return;
            mano = rig.leftHandAnchor;
        }
        var cab = ManosRig.Cabeza;
        transform.position = mano.position + Vector3.up * 0.09f;
        if (cab != null)
        {
            var d = transform.position - cab.position;
            if (d.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(d);
        }
    }
}
