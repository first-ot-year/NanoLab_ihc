using UnityEngine;

// Capas físicas propias de NanoLab.
//  - BloqueoJugador (8): cajas que solo frenan al jugador (sillas, armarios, muebles). El Locomotor de
//    Meta las encuentra con su CapsuleCast (máscara -1), pero no chocan con ningún objeto físico: los
//    aparatos de las estanterías no salen disparados ni se quedan atrapados, y los rayos de
//    PunteroMuestras las ignoran (MascaraRayos), así que no tapan lo que hay dentro de un armario.
// Las monta AsegurarColisiones (menú NanoLab/Asegurar colisiones).
public static class CapasNanoLab
{
    public const int BloqueoJugador = 8;
    public const string NombreBloqueo = "BloqueoJugador";

    // Para los rayos de interacción: todo menos los bloqueos del jugador.
    public static int MascaraRayos => ~(1 << BloqueoJugador);

    // Por si la matriz de colisiones del proyecto no se guardó: se aplica también al arrancar.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ConfigurarMatriz()
    {
        for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(BloqueoJugador, i, true);
    }
}
