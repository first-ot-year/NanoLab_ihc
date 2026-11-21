using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Letras nítidas en TODOS los carteles y paneles (revisión: "las letras se ven opacadas" y "ajusta las letras").
//  1) Orden de dibujo: los paneles redondeados (Interaction/RoundedBoxUnlit) son transparentes y no escriben
//     profundidad, igual que el texto TextMeshPro. Si la cola de render del panel es mayor o igual que la del texto,
//     el panel se dibuja ENCIMA de sus letras y las deja como detrás de un velo (sortingOrder no lo arregla siempre).
//     Aquí los textos 3D pasan a una cola posterior a la de los paneles: primero todos los paneles, luego las letras.
//     Se usa una copia del material de cada fuente (no se toca el asset).
//  2) Tamaño: el profesor vio las letras pequeñas. Cada texto 3D se agranda una vez un 25 % (Escala).
//  3) Ajuste: si un texto no cabe en su recuadro, se activa el auto-tamaño (encoge hasta el 50 %) en vez de salirse.
// Revisa los textos activos cada 1,5 s (los que se crean después también: avisos, fichas, terminal...).
public class LegibilidadTextos : MonoBehaviour
{
    public const int ColaMinima = 3100;
    public const float Escala = 1.25f;
    // Revisión de la demo ("las letras de los paneles azules de la sala de experimentación, un poquito más grandes"):
    // los carteles de estación y los tableros informativos del centro crecen un 15 % más.
    public const float EscalaSala = 1.15f;
    static readonly HashSet<string> Tableros = new HashSet<string> { "Procedimiento", "Comparar", "Agentes" };

    static readonly Dictionary<Material, Material> copias = new Dictionary<Material, Material>();
    readonly Dictionary<TextMeshPro, string> revisado = new Dictionary<TextMeshPro, string>();
    readonly HashSet<int> agrandados = new HashSet<int>();
    int cola = -1;
    float siguiente;

    public static int Copias => copias.Count;
    public int Cola => cola;

    public static LegibilidadTextos Asegurar()
    {
        var l = FindObjectOfType<LegibilidadTextos>();
        if (l == null) l = new GameObject("LegibilidadTextos").AddComponent<LegibilidadTextos>();
        return l;
    }

    void Update()
    {
        if (Time.unscaledTime < siguiente) return;
        siguiente = Time.unscaledTime + 1.5f;
        Pasada();
    }

    // Pública para las pruebas.
    public int Pasada()
    {
        if (cola < 0) cola = CalcularCola();
        int n = 0;
        foreach (var tx in FindObjectsOfType<TextMeshPro>())
        {
            if (tx == null) continue;
            var m = tx.fontSharedMaterial;
            if (m != null && m.renderQueue < cola && m.renderQueue < 3500)   // el HUD del microscopio ya va en 4000
            {
                if (!copias.TryGetValue(m, out var c) || c == null)
                {
                    c = new Material(m) { name = m.name + " (delante)" };
                    c.renderQueue = cola;
                    copias[m] = c;
                    copias[c] = c;
                }
                tx.fontSharedMaterial = c;
                n++;
            }
            if (agrandados.Add(tx.GetInstanceID()))
            {
                float e = Escala * (EnPanelDeSala(tx.transform) ? EscalaSala : 1f);
                if (tx.enableAutoSizing) tx.fontSizeMax *= e;
                tx.fontSize *= e;
            }
            Ajustar(tx);
        }
        return n;
    }

    // Textos de los carteles y tableros del centro de experimentación (no las etiquetas de los objetos que se agarran).
    public static bool EnPanelDeSala(Transform t)
    {
        bool objeto = false;
        for (var p = t; p != null; p = p.parent)
        {
            if (p.GetComponent<Rigidbody>() != null || p.GetComponent<Oculus.Interaction.Grabbable>() != null) objeto = true;
            if (p.GetComponent<EstacionGuia>() != null || Tableros.Contains(p.name)) return true;
            if (p.name == "CentroExperimentacion") return !objeto;
        }
        return false;
    }

    // Cola de los paneles redondeados de la escena + 1 (como mínimo 3100).
    static int CalcularCola()
    {
        int q = 3000;
        foreach (var p in FindObjectsOfType<PanelRedondeado>(true))
        {
            var r = p.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null) q = Mathf.Max(q, r.sharedMaterial.renderQueue);
        }
        return Mathf.Max(ColaMinima, q + 1);
    }

    // Si el texto se sale de su recuadro, encoge (solo cuando cambia el texto).
    void Ajustar(TextMeshPro tx)
    {
        if (!tx.enableWordWrapping || tx.enableAutoSizing) return;
        string s = tx.text;
        if (string.IsNullOrEmpty(s)) return;
        if (revisado.TryGetValue(tx, out var antes) && antes == s) return;
        revisado[tx] = s;
        var r = tx.rectTransform.rect;
        if (r.width < 0.01f || r.height < 0.01f) return;
        tx.ForceMeshUpdate();
        // Solo textos de varias líneas que de verdad no caben (las etiquetas de una línea no se tocan).
        if (tx.textInfo == null || tx.textInfo.lineCount < 2 || tx.preferredHeight <= r.height * 1.15f) return;
        tx.fontSizeMax = tx.fontSize;
        tx.fontSizeMin = tx.fontSize * 0.5f;
        tx.enableAutoSizing = true;
    }
}
