using UnityEngine;

// Criotubo, placa o vaso: al tocarlo con un portaobjetos agarrado, le pasa su
// muestra (o lo limpia si es el desinfectante). Necesita un collider trigger.
public class FuenteMuestra : MonoBehaviour
{
    public MuestraData muestra;
    public bool esDesinfectante;

    public void Aplicar(Portaobjetos p)
    {
        if (esDesinfectante) p.Limpiar();
        else p.Cargar(muestra);
    }
}
