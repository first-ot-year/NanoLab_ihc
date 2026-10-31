using UnityEngine;

// Taburete en el que el jugador se sienta apuntándolo con el rayo (gatillo o A/X): mira hacia donde
// mira el taburete (la mesa) y los ojos quedan a la altura del ocular. Se levanta moviendo el joystick
// izquierdo (sentado, la mano queda dentro de su collider y el rayo ya no lo encuentra). El aspecto está
// en el hijo "Modelo" del prefab (silla de Blender, MontarSillaLaboratorio) y se puede reemplazar sin tocar los anclas.
public class Silla : Apuntable
{
    [Tooltip("Ancla a la altura del asiento, en su centro.")]
    public Transform asiento;
    [Tooltip("Dónde queda el jugador al levantarse (detrás de la silla).")]
    public Transform salida;
    [Tooltip("Altura de los ojos sobre el asiento (m). Con el taburete de 0.65 m quedan a ~1.4 m, a la altura del ocular.")]
    public float ojosSobreAsiento = 0.72f;

    Collider[] colliders;

    public bool Ocupada => JugadorNanoLab.Sentado(out var s) && s == this;

    public override string Accion => Ocupada ? "levantarte" : "sentarte";

    // Sentado, el rayo solo encuentra la propia silla (para levantarse): no se salta de una silla a otra.
    public override bool Disponible => base.Disponible && (!JugadorNanoLab.Sentado(out var s) || s == this);

    public override void Activar()
    {
        var j = JugadorNanoLab.Instancia;
        if (j == null) return;
        if (Ocupada) j.Levantarse();
        else j.Sentarse(this);
    }

    public Vector3 PuntoAsiento => asiento != null ? asiento.position : transform.position + Vector3.up * 0.47f;
    public Vector3 PuntoSalida => salida != null ? salida.position : transform.position - transform.forward * 0.6f;

    // El jugador ocupa el sitio de la silla mientras está sentado. El collider trigger de la raíz
    // (el que encuentra el rayo) no se toca.
    public void Colisiones(bool activas)
    {
        if (colliders == null) colliders = System.Array.FindAll(GetComponentsInChildren<Collider>(true), c => !c.isTrigger);
        foreach (var c in colliders) if (c != null) c.enabled = activas;
    }
}
