using TMPro;
using UnityEngine;
using K = KitVacuna;

// Vial con un agente de intervención (índice en RetoData.agentes). Se agarra y se encaja en la prensa.
// Revisión del profesor ("nada indica por qué usamos tal cápsula y no hay sonido"): al cogerlo suena y aparece
// encima una FICHA DEL AGENTE: qué hace, qué estructura ataca y si encaja con la estrategia elegida en la terminal.
// La ficha mira a la cabeza y se apaga al soltar el vial (o al encajarlo: la prensa sigue explicándolo).
[RequireComponent(typeof(Encajable))]
public class VialAgente : MonoBehaviour
{
    public int agente;
    public Encajable Encajable { get; private set; }

    Transform ficha;
    TextMeshPro texto;
    float apagarEn = -1f;

    public bool FichaVisible => ficha != null && ficha.gameObject.activeSelf;
    public string TextoFicha => texto != null ? texto.text : "";

    void Awake() => Encajable = GetComponent<Encajable>();

    void OnEnable()
    {
        if (Encajable == null) Encajable = GetComponent<Encajable>();
        Encajable.Agarrar += AlAgarrar;
        Encajable.Soltar += AlSoltar;
    }

    void OnDisable()
    {
        Encajable.Agarrar -= AlAgarrar;
        Encajable.Soltar -= AlSoltar;
        if (ficha != null) ficha.gameObject.SetActive(false);
    }

    void AlAgarrar(Encajable _) => MostrarFicha();
    void AlSoltar(Encajable _) => apagarEn = Time.time + 1.2f;

    // Pública para las pruebas (sin visor no se puede "agarrar").
    public void MostrarFicha()
    {
        var rm = RetoManager.Instancia;
        if (rm == null || rm.reto == null || rm.reto.agentes == null || agente < 0 || agente >= rm.reto.agentes.Length) return;
        var a = rm.reto.agentes[agente];
        var diana = rm.reto.EstrategiaDe(a.diana);
        if (ficha == null) Construir();
        string encaja = rm.EstrategiaElegida == null ? "<color=#A9BED2>Aún no elegiste estrategia en la TERMINAL (5).</color>"
            : rm.EsCoherente(agente) ? $"<color=#39FF88>COINCIDE con tu estrategia ({rm.EstrategiaElegida.corto}).</color>"
            : $"<color=#FFB347>NO coincide con tu estrategia ({rm.EstrategiaElegida.corto}).</color>";
        texto.text = $"<b><color=#{ColorUtility.ToHtmlStringRGB(a.color)}>{a.sigla}</color> · {a.nombre}</b>\n" +
                     $"<size=80%>{a.accion}</size>\n" +
                     (diana != null ? $"<size=80%><color=#8FD8FF>ATACA:</color> {diana.nombre.ToLower()}</size>\n" : "") +
                     $"<size=75%>{encaja}</size>";
        ficha.gameObject.SetActive(true);
        apagarEn = -1f;
        Sonidos.Scan(transform.position);
        Colocar();
    }

    void LateUpdate()
    {
        if (ficha == null || !ficha.gameObject.activeSelf) return;
        if (apagarEn > 0f && Time.time > apagarEn)
        {
            ficha.gameObject.SetActive(false);
            return;
        }
        Colocar();
    }

    void Colocar()
    {
        var cab = ManosRig.Cabeza;
        ficha.position = transform.position + Vector3.up * 0.16f;
        if (cab == null) return;
        var d = ficha.position - cab.position;
        if (d.sqrMagnitude > 1e-4f) ficha.rotation = Quaternion.LookRotation(d, Vector3.up);
    }

    void Construir()
    {
        ficha = new GameObject("FichaAgente").transform;
        ficha.SetParent(null, false);   // no hereda la escala del vial
        K.Panel(ficha, "Fondo", new Vector3(0f, 0f, 0.002f), 0.3f, 0.15f, new Color(0.04f, 0.09f, 0.14f, 0.96f), new Color(0.37f, 0.83f, 1f), 0.002f, 0.012f);
        texto = K.Texto(ficha, "", Vector3.zero, 0.11f, Color.white, 0.27f, 0.13f, TextAlignmentOptions.Center);
        K.AutoTam(texto, 0.05f);
        foreach (var c in ficha.GetComponentsInChildren<Collider>(true)) Destroy(c);
        ficha.gameObject.SetActive(false);
    }

    void OnDestroy() { if (ficha != null) Destroy(ficha.gameObject); }
}
