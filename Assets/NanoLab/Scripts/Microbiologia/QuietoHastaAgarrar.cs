using System.Collections;
using Oculus.Interaction;
using UnityEngine;

// El objeto se queda quieto donde está colocado (kinemático) hasta que se agarra por
// primera vez con el Interaction SDK; al soltarlo pasa a tener física normal.
// Evita que los aparatos de las estanterías se caigan al empezar la escena.
[RequireComponent(typeof(Rigidbody))]
public class QuietoHastaAgarrar : MonoBehaviour
{
    Grabbable grabbable;
    Rigidbody rb;
    bool liberado, estabaAgarrado;

    void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void Update()
    {
        if (liberado || grabbable == null) return;
        bool agarrado = grabbable.SelectingPointsCount > 0;
        if (!agarrado && estabaAgarrado) StartCoroutine(Liberar());
        estabaAgarrado = agarrado;
    }

    // Un frame después de soltarlo (cuando el SDK ya restauró su estado) se activa la física.
    IEnumerator Liberar()
    {
        yield return null;
        rb.isKinematic = false;
        rb.useGravity = true;
        liberado = true;
    }
}
