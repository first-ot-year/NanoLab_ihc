using System.Linq;
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
        // Criotubo o placa guardado en un compartimento cerrado (ultracongelador, vitrina, cajón).
        var hueco = Hueco.De(transform);
        if (hueco != null && !hueco.Accesible)
        {
            Aviso.Mostrar($"Está cerrado: abre primero {hueco.compartimento.nombre}.", transform.position + Vector3.up * 0.15f, Aviso.Tipo.Falta);
            Sonidos.Pista(transform.position);
            return;
        }
        if (!string.IsNullOrEmpty(bloqueo))
        {
            Aviso.Mostrar(bloqueo, transform.position + Vector3.up * 0.15f, Aviso.Tipo.Falta);
            Sonidos.Pista(transform.position);
            return;
        }
        if (esDesinfectante)
        {
            // Prevención de errores: una tinción válida que aún no se observó no se borra por acercar el portaobjetos.
            var rm = RetoManager.Instancia;
            bool valiosa = p.DeCaso && !p.Despues && p.PasosTincion > 0 && !p.TincionErronea
                           && rm != null && p.muestra == rm.reto.muestra && !rm.Evidencias.All(x => x);
            if (valiosa && !Confirmacion.Pedir("limpiar" + p.GetInstanceID()))
            {
                Aviso.Mostrar("Esta tinción es válida y aún no la has observado. Vuelve a tocar el desinfectante para borrarla.",
                    transform.position + Vector3.up * 0.15f, Aviso.Tipo.Pista, Confirmacion.Segundos);
                Sonidos.Pista(transform.position);
                return;
            }
            p.Limpiar();
        }
        else if (reactivo > 0) p.AplicarReactivo(reactivo);
        else
        {
            // Una muestra de misión ya teñida no se pisa con otra por rozar su placa (se perdería la tinción).
            if (p.DeCaso && !p.Despues && p.muestra != null && p.muestra != muestra && p.PasosTincion > 0
                && !p.TincionErronea && !string.IsNullOrEmpty(etiqueta))
            {
                Aviso.Mostrar($"Este portaobjetos ya tiene la {p.Etiqueta} teñida. Usa otro portaobjetos o límpialo antes con el desinfectante.",
                    transform.position + Vector3.up * 0.15f, Aviso.Tipo.Falta);
                Sonidos.Pista(transform.position);
                return;
            }
            p.Cargar(muestra, etiqueta);
        }
        Usada?.Invoke(this, p);
    }
}
