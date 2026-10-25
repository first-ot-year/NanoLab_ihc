using UnityEngine;

// Diana de un agente de intervención (modelo educativo simplificado, no un tratamiento real).
public enum DianaAgente { ParedPeptidoglucano, MembranaExterna, Virus, Ninguna }

// Definición de un reto del Centro de Experimentación: qué muestra es, qué opciones hay,
// qué texturas se ven en el microscopio y qué pistas da el centro. Se configura sin tocar código.
[CreateAssetMenu(menuName = "NanoLab/Reto", fileName = "Reto")]
public class RetoData : ScriptableObject
{
    [System.Serializable]
    public class Agente
    {
        public string sigla;
        public string nombre;
        [TextArea(1, 3)] public string accion;
        public Color color = Color.white;
        public DianaAgente diana;
        [TextArea(2, 4)] public string pistaSiFalla;
    }

    [System.Serializable]
    public class Evidencia
    {
        public string clave;            // "Forma"
        public string valor;            // "cocos (células esféricas)"
        public int aumentoMinimo;       // índice de aumento necesario (0 = 100x, 1 = 400x, 2 = 1000x)
        public bool requiereGram;       // solo con la tinción de Gram completa
        public string comoObtenerla;    // "sube a 1000x"
    }

    [Header("Caso")]
    public string codigo = "CASO 01";
    public string titulo = "La herida que no sana";
    [TextArea(3, 8)] public string historia;
    public string origen = "Herida de piel";
    public MuestraData muestra;
    public MuestraData[] opciones;      // identificaciones posibles en la terminal (incluye la correcta)

    [Header("Microscopio")]
    public Texture2D vistaSinTenir;
    public Texture2D vistaTincionFallida;
    public Texture2D vistaDespuesEficaz;
    public Texture2D vistaDespuesCrecimiento;
    public Evidencia[] evidencias;

    [Header("Intervención")]
    public Agente[] agentes;
    [Range(0, 1)] public float umbralExito = 0.7f;

    [Header("Pistas por etapa (se dan en orden al pulsar PISTA)")]
    [TextArea(2, 4)] public string[] pistasExploracion;
    [TextArea(2, 4)] public string[] pistasTincion;
    [TextArea(2, 4)] public string[] pistasObservacion;
    [TextArea(2, 4)] public string[] pistasInvestigacion;
    [TextArea(2, 4)] public string[] pistasHipotesis;
    [TextArea(2, 4)] public string[] pistasPreparacion;
    [TextArea(2, 4)] public string[] pistasExperimento;
    [TextArea(2, 4)] public string[] pistasAnalisis;

    [Header("Cierre")]
    [TextArea(3, 8)] public string explicacion;
    public string fragmento = "Recuerdo científico 1/3";
    [TextArea(2, 4)] public string textoFragmento;

    // Eficacia (0..1) de un agente sobre la muestra del caso: depende de su estructura, no de su nombre.
    public float Eficacia(Agente a) => Eficacia(a, muestra);

    public static float Eficacia(Agente a, MuestraData m)
    {
        if (a == null || m == null) return 0f;
        bool bacteria = m.tipo == TipoMuestra.Bacteria;
        switch (a.diana)
        {
            case DianaAgente.ParedPeptidoglucano: return bacteria ? (m.gramPositiva ? 0.95f : 0.2f) : 0f;
            case DianaAgente.MembranaExterna: return bacteria ? (m.gramPositiva ? 0.1f : 0.9f) : 0f;
            case DianaAgente.Virus: return bacteria ? 0f : 0.85f;
            default: return 0f;
        }
    }
}
