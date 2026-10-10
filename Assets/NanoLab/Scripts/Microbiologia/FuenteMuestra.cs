using UnityEngine;

// Criotubo, placa, vaso o gotero: al tocarlo con un portaobjetos agarrado, le pasa su
// muestra, lo limpia (desinfectante) o le aplica un reactivo de tinción. Necesita un collider trigger.
public class FuenteMuestra : MonoBehaviour
{
    public MuestraData muestra;
    public bool esDesinfectante;
    [Tooltip("Reactivo de la tinción de Gram (1 cristal violeta, 2 lugol, 3 alcohol-acetona, 4 safranina). 0 = no es un reactivo.")]
    public int reactivo;
    [Tooltip("Texto que se escribe en el portaobjetos en lugar del nombre (p. ej. 'CASO 01' para no revelar la muestra).")]
    public string etiqueta;
    [Tooltip("Si no está vacío, la fuente no se puede usar y se muestra este aviso.")]
    public string bloqueo;

    // Lo escucha el Centro de Experimentación (RetoManager).
    public static event System.Action<FuenteMuestra, Portaobjetos> Usada;

    public void Aplicar(Portaobjetos p)
    {
        if (!string.IsNullOrEmpty(bloqueo))
        {
            Aviso.Mostrar(bloqueo, transform.position + Vector3.up * 0.15f, Aviso.Tipo.Error);
            Sonidos.Error(transform.position);
            return;
        }
        if (esDesinfectante) p.Limpiar();
        else if (reactivo > 0) p.AplicarReactivo(reactivo);
        else p.Cargar(muestra, etiqueta);
        Usada?.Invoke(this, p);
    }
}
