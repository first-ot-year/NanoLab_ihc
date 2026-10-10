using UnityEngine;

// Vial con un agente de intervención (índice en RetoData.agentes). Se agarra y se encaja en la prensa.
[RequireComponent(typeof(Encajable))]
public class VialAgente : MonoBehaviour
{
    public int agente;
    public Encajable Encajable { get; private set; }

    void Awake() => Encajable = GetComponent<Encajable>();
}
