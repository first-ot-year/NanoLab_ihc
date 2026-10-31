using UnityEditor;

// Cierra una barra de progreso que se quedó abierta (p. ej. si un montaje falló antes de cerrarla).
// Se ejecuta sola cada vez que Unity recompila los scripts, y también desde el menú.
[InitializeOnLoad]
public static class LimpiarProgreso
{
    static LimpiarProgreso() => EditorApplication.delayCall += EditorUtility.ClearProgressBar;

    [MenuItem("NanoLab/Cerrar barra de progreso atascada", false, 2)]
    public static void Cerrar() => EditorUtility.ClearProgressBar();
}
