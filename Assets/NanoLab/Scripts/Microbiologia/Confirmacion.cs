using UnityEngine;

// Doble pulsación para las acciones que borran avance (heurísticas 3 y 5 de Nielsen): la primera avisa, la segunda
// (antes de 'Segundos') confirma. Un único patrón para REINICIAR, CHECKPOINT, NUEVA PARTIDA y el desinfectante.
public static class Confirmacion
{
    public const float Segundos = 5f;
    static string clave;
    static float hasta;

    // true si es la segunda pulsación de 'k' dentro del plazo.
    public static bool Pedir(string k)
    {
        if (clave == k && Time.unscaledTime < hasta) { clave = null; return true; }
        clave = k;
        hasta = Time.unscaledTime + Segundos;
        return false;
    }

    public static bool Pendiente(string k) => clave == k && Time.unscaledTime < hasta;
}
