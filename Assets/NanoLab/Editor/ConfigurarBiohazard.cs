using UnityEditor;
using UnityEngine;

// Configuración global del evento de bioseguridad (BiohazardEvent):
//  · Etiqueta "Contaminated" (zonas contaminadas) y capa "Biohazard" (en la 9 si está libre; el rocío solo busca
//    en esa capa). Se añaden solas al abrir el proyecto o recompilar, y quedan guardadas en
//    ProjectSettings/TagManager.asset, así el APK también las tiene.
//  · Menú para lanzar el evento a mano en play mode.
[InitializeOnLoad]
public static class ConfigurarBiohazard
{
    const int CapaPreferida = 9;

    static ConfigurarBiohazard() => EditorApplication.delayCall += Asegurar;

    [MenuItem("NanoLab/Bioseguridad: crear etiqueta y capa", false, 40)]
    public static void Asegurar()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        bool cambio = false;

        var tags = so.FindProperty("tags");
        bool hayTag = false;
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == BiohazardEvent.Tag) hayTag = true;
        if (!hayTag)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = BiohazardEvent.Tag;
            cambio = true;
        }

        var capas = so.FindProperty("layers");
        bool hayCapa = false;
        for (int i = 0; i < capas.arraySize; i++) if (capas.GetArrayElementAtIndex(i).stringValue == BiohazardEvent.NombreCapa) hayCapa = true;
        if (!hayCapa)
        {
            int libre = string.IsNullOrEmpty(capas.GetArrayElementAtIndex(CapaPreferida).stringValue) ? CapaPreferida : -1;
            for (int i = 8; libre < 0 && i < 28; i++) if (string.IsNullOrEmpty(capas.GetArrayElementAtIndex(i).stringValue)) libre = i;   // 28-31 los usa el proyecto
            if (libre >= 0) { capas.GetArrayElementAtIndex(libre).stringValue = BiohazardEvent.NombreCapa; cambio = true; }
            else Debug.LogWarning("[Biohazard] No hay capas libres para \"Biohazard\": el rocío buscará en todas las capas.");
        }

        if (cambio)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Biohazard] Etiqueta \"{BiohazardEvent.Tag}\" y capa \"{BiohazardEvent.NombreCapa}\" listas (ProjectSettings/TagManager.asset).");
        }
    }

    [MenuItem("NanoLab/Bioseguridad: lanzar el evento (play mode)", false, 41)]
    static void Lanzar()
    {
        var rm = RetoManager.Instancia;
        var foco = rm != null && rm.camara != null ? rm.camara.transform : null;
        if (BiohazardEvent.Instancia == null || foco == null) { Debug.LogWarning("[Biohazard] Entra en play mode en la escena Microbiologia."); return; }
        BiohazardEvent.Reiniciar();
        BiohazardEvent.Disparar(foco);
    }

    [MenuItem("NanoLab/Bioseguridad: lanzar el evento (play mode)", true)]
    static bool LanzarValido() => Application.isPlaying;
}
