using TMPro;
using UnityEngine;
using K = KitVacuna;

// Pared de vidrio entre la zona de experimentos y la sala de vacunas, con su puerta corredera.
public class ParedVacunas : MonoBehaviour
{
    const float Alto = 2.7f, AltoTecho = 4.25f, Grosor = 0.05f;

    Transform hojaIzq, hojaDer;
    Vector3 izqCerrada, derCerrada;
    Renderer luz;
    TextMeshPro estado;
    float apertura;
    bool desbloqueada, avisada;

    public bool Desbloqueada => desbloqueada;
    public bool Abierta => apertura > 0.95f;

    public void Construir()
    {
        var t = transform;
        float z = RecorridoU.ParedZ;
        var marco = K.Escena("Marco", new Color(0.55f, 0.58f, 0.62f));
        var blanco = K.Escena("Blanco", new Color(0.93f, 0.93f, 0.92f));
        var vidrio = K.Vidrio(new Color(0.75f, 0.9f, 1f, 0.18f));

        // Tramos de vidrio a ambos lados de la puerta, con montantes cada ~2,4 m, zócalo y dintel opaco hasta el techo.
        Tramo(t, RecorridoU.X0, RecorridoU.PuertaX0, z, marco, blanco, vidrio);
        Tramo(t, RecorridoU.PuertaX1, RecorridoU.X1, z, marco, blanco, vidrio);
        float xm = (RecorridoU.PuertaX0 + RecorridoU.PuertaX1) / 2f, ancho = RecorridoU.PuertaX1 - RecorridoU.PuertaX0;
        // Dintel sobre la puerta y marco.
        K.Solido(K.Caja(t, "DintelPuerta", new Vector3(xm, (Alto + AltoTecho) / 2f, z), new Vector3(ancho, AltoTecho - Alto, 0.12f), blanco, true));
        K.Caja(t, "MarcoPuertaSup", new Vector3(xm, Alto - 0.04f, z), new Vector3(ancho, 0.08f, 0.1f), marco);

        // Hojas correderas (se meten por delante del vidrio, del lado de experimentos).
        float hoja = ancho / 2f;
        hojaIzq = K.Grupo(t, "HojaIzq", new Vector3(xm - hoja / 2f, 0f, z - 0.06f));
        hojaDer = K.Grupo(t, "HojaDer", new Vector3(xm + hoja / 2f, 0f, z - 0.06f));
        foreach (var h in new[] { hojaIzq, hojaDer })
        {
            K.Solido(K.Caja(h, "Vidrio", new Vector3(0f, Alto / 2f - 0.05f, 0f), new Vector3(hoja - 0.02f, Alto - 0.12f, 0.03f), vidrio));
            K.Caja(h, "Canto", new Vector3(0f, 0.05f, 0f), new Vector3(hoja - 0.02f, 0.1f, 0.04f), marco);
            K.Caja(h, "Tirador", new Vector3(h == hojaIzq ? hoja / 2f - 0.08f : -hoja / 2f + 0.08f, 1.05f, -0.03f), new Vector3(0.025f, 0.4f, 0.025f), marco);
        }
        izqCerrada = hojaIzq.localPosition;
        derCerrada = hojaDer.localPosition;

        // Rótulo sobre la puerta (mirando a la zona de experimentos) y luz de estado.
        var rot = Quaternion.identity;   // el texto se lee desde -z (lado de experimentos)
        var cartel = K.Grupo(t, "CartelVacunas", new Vector3(xm + 1.6f, 2.35f, z - 0.08f), rot);
        K.Panel(cartel, "Fondo", Vector3.zero, 1.9f, 0.42f, new Color(0.06f, 0.19f, 0.34f, 1f), new Color(1f, 0.6f, 0.45f, 1f), 0.004f, 0.04f);
        K.Texto(cartel, "<b>LABORATORIO DE VACUNAS</b>", new Vector3(0f, 0.09f, -0.004f), 0.3f, new Color(1f, 0.75f, 0.6f), 1.8f, 0.12f, TextAlignmentOptions.Center);
        estado = K.Texto(cartel, "", new Vector3(0f, -0.08f, -0.004f), 0.2f, Color.white, 1.8f, 0.16f, TextAlignmentOptions.Center);
        luz = K.Esfera(t, "LuzPuerta", new Vector3(xm, Alto + 0.12f, z - 0.08f), 0.09f, K.Plano(Color.white));
        foreach (var c in cartel.GetComponentsInChildren<Collider>(true)) Destroy(c);
        Estado(Misiones.VacunaDesbloqueada, false);
    }

    void Tramo(Transform t, float xa, float xb, float z, Material marco, Material blanco, Material vidrio)
    {
        float largo = xb - xa;
        if (largo < 0.05f) return;
        int n = Mathf.Max(1, Mathf.RoundToInt(largo / 2.4f));
        float paso = largo / n;
        for (int i = 0; i < n; i++)
        {
            float x0 = xa + i * paso, xc = x0 + paso / 2f;
            K.Solido(K.Caja(t, "Vidrio", new Vector3(xc, Alto / 2f, z), new Vector3(paso - 0.06f, Alto - 0.2f, Grosor), vidrio));
            K.Caja(t, "Montante", new Vector3(x0 + 0.03f, Alto / 2f, z), new Vector3(0.06f, Alto, 0.09f), marco);
        }
        K.Caja(t, "Montante", new Vector3(xb - 0.03f, Alto / 2f, z), new Vector3(0.06f, Alto, 0.09f), marco);
        K.Caja(t, "Zocalo", new Vector3((xa + xb) / 2f, 0.05f, z), new Vector3(largo, 0.1f, 0.1f), blanco);
        K.Caja(t, "Travesano", new Vector3((xa + xb) / 2f, Alto - 0.05f, z), new Vector3(largo, 0.1f, 0.1f), marco);
        K.Solido(K.Caja(t, "Dintel", new Vector3((xa + xb) / 2f, (Alto + AltoTecho) / 2f, z), new Vector3(largo, AltoTecho - Alto, 0.12f), blanco, true));
    }

    void Estado(bool abierta, bool anunciar)
    {
        desbloqueada = abierta;
        if (luz != null) K.Pintar(luz, abierta ? new Color(0.25f, 1f, 0.45f) : new Color(1f, 0.3f, 0.25f));
        if (estado != null)
            estado.text = abierta ? "<color=#39FF88>ABIERTO · MISIÓN 3</color>" : "Se abre al completar la <b>MISIÓN 2</b>";
        if (anunciar && !avisada)
        {
            avisada = true;
            var p = Puerta();
            Sonidos.Logro(p);
            Aviso.Mostrar("Se abrió el LABORATORIO DE VACUNAS: cruza la puerta de vidrio.", p + Vector3.up * 1.8f, Aviso.Tipo.Ok, 6f);
        }
    }

    Vector3 Puerta() => transform.position + new Vector3((RecorridoU.PuertaX0 + RecorridoU.PuertaX1) / 2f, 0f, RecorridoU.ParedZ);

    void Update()
    {
        bool ahora = Misiones.VacunaDesbloqueada;
        if (ahora != desbloqueada) Estado(ahora, ahora);
        var cab = ManosRig.Cabeza;
        bool cerca = false;
        if (cab != null)
        {
            var d = cab.position - Puerta();
            cerca = Mathf.Abs(d.x) < 1.4f && Mathf.Abs(d.z) < 1.8f;
        }
        float objetivo = desbloqueada && cerca ? 1f : 0f;
        float antes = apertura;
        apertura = Mathf.MoveTowards(apertura, objetivo, Time.deltaTime / 0.9f);
        if ((antes == 0f && apertura > 0f) || (antes == 1f && apertura < 1f)) Sonidos.Clic(Puerta() + Vector3.up);
        float s = Mathf.SmoothStep(0f, 1f, apertura) * (RecorridoU.PuertaX1 - RecorridoU.PuertaX0) / 2f;
        hojaIzq.localPosition = izqCerrada + Vector3.left * s;
        hojaDer.localPosition = derCerrada + Vector3.right * s;
    }
}
