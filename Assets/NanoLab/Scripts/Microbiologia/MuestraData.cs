using UnityEngine;

public enum TipoMuestra { Virus, Bacteria }

// Ficha de una muestra: lo que se ve en el HUD del microscopio y en la estación 3D.
[CreateAssetMenu(menuName = "NanoLab/Muestra", fileName = "Muestra")]
public class MuestraData : ScriptableObject
{
    [System.Serializable]
    public struct Dato { public string clave, valor; }

    public string codigo = "#A7-23";
    public string nombre;
    public string nombreCientifico;
    public TipoMuestra tipo;
    public Dato[] caracteristicas;
    public string riesgo = "ALTO CONTAGIO";
    public Color color = Color.cyan;

    [Header("Imagen y modelo")]
    public Texture2D micrografia;
    public string creditoImagen;
    public GameObject modelo3D;          // modelo del NIH (solo virus); si falta se genera uno provisional

    [Header("Pestañas")]
    [TextArea(2, 6)] public string morfologia;
    [TextArea(2, 6)] public string estructura;
    [TextArea(2, 6)] public string composicion;
    [TextArea(2, 6)] public string comportamiento;
    [TextArea(2, 6)] public string observaciones;

    public static readonly string[] NombresPestanas = { "MORFOLOGÍA", "ESTRUCTURA", "COMPOSICIÓN", "COMPORTAMIENTO" };

    public string Pestana(int i)
    {
        switch (i)
        {
            case 0: return morfologia;
            case 1: return estructura;
            case 2: return composicion;
            default: return comportamiento;
        }
    }

    // Aumentos del microscopio (etiqueta, factor de zoom de la vista, barra de escala).
    public string[] Aumentos => tipo == TipoMuestra.Virus
        ? new[] { "25 000x", "50 000x", "100 000x", "200 000x" }
        : new[] { "100x", "400x", "1000x" };

    public float[] FactoresZoom => tipo == TipoMuestra.Virus
        ? new[] { 0.55f, 0.8f, 1.15f, 1.7f }
        : new[] { 1f, 2f, 3.5f };

    public string[] Escalas => tipo == TipoMuestra.Virus
        ? new[] { "400 nm", "200 nm", "100 nm", "50 nm" }
        : new[] { "50 µm", "10 µm", "5 µm" };

    public int AumentoInicial => tipo == TipoMuestra.Virus ? 2 : 1;
}
