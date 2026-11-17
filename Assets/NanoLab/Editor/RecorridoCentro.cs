using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// NanoLab › Recorrido en U (ordenar el centro): reubica en la escena las estaciones y los equipos del Centro de
// Experimentación para que el estudiante no se pierda y la sala no se sienta cargada (ver RecorridoU.cs y
// Docs/Recorrido_U.md). Se hace en el editor, no al jugar, porque mesas y equipos son geometría ESTÁTICA (lotes
// estáticos y luz horneada): moverlos en tiempo de ejecución deja el dibujo en el sitio viejo.
//
//   z 17 ┌ Ultracong. ── MESA DE VACUNAS ── Incub. CO2 ┐   SALA DE VACUNAS (misión 3)
//   z 11 ├─puerta─┬──── Autoclave ─ Cámara ─ Prensa ─ Terminal ┤ ← pared de vidrio (misión 2)
//        │        Archivo                                   Biblioteca │
//  puerta│                    (centro despejado)           Conclusiones│
//   z  0 └ Incub.37 ─ Banco ─ Cabina ─ Tinción ─ Microscopía ──────────┘   (misión 1)
//        x 6.9                                                     x 24.1
//
// Es idempotente (posiciones absolutas). Después conviene hornear la luz (NanoLab › Recorrido en U: hornear la luz)
// para que las sombras del suelo sigan a los muebles.
public static class RecorridoCentro
{
    // nombre, x, z, yaw (convención de las estaciones: el usuario queda en +z local).
    static readonly (string ruta, float x, float z, float yaw)[] Posiciones =
    {
        // Misión 1: pared sur, en orden de uso.
        ("Equipamiento/Incubadora", 7.25f, 1.8f, 90f),
        ("Estaciones/1_BancoMuestras", 10.5f, 0.55f, 0f),
        ("Equipamiento/CabinaBioseguridad", 12.6f, 0.2f, 0f),
        ("Estaciones/2_Tincion", 15.6f, 1.15f, 0f),
        ("Estaciones/3_Microscopia", 20.5f, 1.15f, 0f),
        ("Estaciones/4_Biblioteca", 23.45f, 7.3f, -90f),
        // Misión 2: a lo largo de la pared de vidrio (z = 11), de este a oeste.
        ("Estaciones/5_Terminal", 21.6f, RecorridoU.ParedZ - 0.76f, 180f),
        ("Estaciones/6_Prensa", 17.8f, RecorridoU.ParedZ - 1.45f, 180f),
        ("Estaciones/7_CamaraExperimental", 14.2f, RecorridoU.ParedZ - 1.0f, 180f),
        ("Equipamiento/Autoclave", 11.3f, RecorridoU.ParedZ - 0.43f, 180f),
        // Entrada: archivo de recuerdos (Baymax y el progreso) junto a la puerta.
        ("Estaciones/8_Archivo", 7.55f, 4.6f, 90f),
        ("Recepcion/Bienvenida", 7.6f, 6.85f, 150f),   // al entrar, a la derecha, fuera del paso
        // Misión 3: sala de vacunas (la mesa se monta al jugar en RecorridoU.PosVacuna).
        ("Equipamiento/Ultracongelador", 11.1f, RecorridoU.Z1 - 0.38f, 180f),
        ("Equipamiento/IncubadoraCO2", 19.9f, RecorridoU.Z1 - 0.38f, 180f),
    };

    [MenuItem("NanoLab/Recorrido en U (ordenar el centro)", false, 30)]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Recorrido en U", "Detén el Play antes de ordenar el centro.", "OK"); return; }
        var raiz = Raiz();
        if (raiz == null) { EditorUtility.DisplayDialog("Recorrido en U", "No encuentro 'CentroExperimentacion' en la escena abierta (abre Microbiologia.unity).", "OK"); return; }
        var banco = Buscar(raiz, "Estaciones/1_BancoMuestras");
        float suelo = banco != null ? banco.position.y : 0.07f;

        int movidos = 0;
        foreach (var (ruta, x, z, yaw) in Posiciones)
        {
            var t = Buscar(raiz, ruta);
            if (t == null) { Debug.LogWarning($"[Recorrido] No está '{ruta}': se deja como está."); continue; }
            Undo.RecordObject(t, "Recorrido en U");
            t.SetPositionAndRotation(new Vector3(x, suelo, z), Quaternion.Euler(0f, yaw, 0f));
            movidos++;
        }

        // Menos carga visual: fuera los rótulos numerados del suelo (queda la franja de color) y el tótem "TU OBJETIVO"
        // (el objetivo ya está en el reloj y en la torre).
        int ocultos = 0;
        var estaciones = Buscar(raiz, "Estaciones");
        if (estaciones != null)
            foreach (Transform e in estaciones)
                foreach (Transform c in e)
                    if (c.name == "Texto" && Mathf.Abs(c.localPosition.y) < 0.02f && c.gameObject.activeSelf)
                    {
                        Undo.RecordObject(c.gameObject, "Recorrido en U");
                        c.gameObject.SetActive(false);
                        ocultos++;
                    }
        var tu = Buscar(raiz, "Recepcion/TuObjetivo");
        if (tu != null && tu.gameObject.activeSelf) { Undo.RecordObject(tu.gameObject, "Recorrido en U"); tu.gameObject.SetActive(false); ocultos++; }
        // Torre del objetivo en el centro de la zona de experimentos.
        var torre = Buscar(raiz, "TorreObjetivo");
        if (torre != null) { Undo.RecordObject(torre, "Recorrido en U"); torre.position = new Vector3(15.5f, torre.position.y, 5.6f); }

        // Marca para el juego (RecorridoU.Activo): coloca la vacuna, la cámara, las conclusiones y la pared de vidrio.
        if (raiz.Find(RecorridoU.Marca) == null)
        {
            var marca = new GameObject(RecorridoU.Marca);
            Undo.RegisterCreatedObjectUndo(marca, "Recorrido en U");
            marca.transform.SetParent(raiz, false);
        }

        var escena = raiz.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log($"[Recorrido] Centro ordenado en U: {movidos} estaciones/equipos movidos, {ocultos} rótulos ocultos. Escena guardada. " +
                  "Hornea la luz (NanoLab › Recorrido en U: hornear la luz) para actualizar las sombras.");
        EditorUtility.DisplayDialog("Recorrido en U",
            $"Listo: {movidos} estaciones y equipos reubicados y la escena guardada.\n\n" +
            "La pared de vidrio, la sala de vacunas y las conclusiones se colocan solas al darle Play.\n\n" +
            "Recomendado: NanoLab › Recorrido en U: hornear la luz (tarda unos minutos) para que las sombras sigan a los muebles.", "OK");
    }

    [MenuItem("NanoLab/Recorrido en U: hornear la luz", false, 31)]
    public static void Hornear()
    {
        if (EditorApplication.isPlaying) return;
        if (!Lightmapping.BakeAsync()) Debug.LogWarning("[Recorrido] No se pudo empezar a hornear la luz.");
        else Debug.Log("[Recorrido] Horneando la luz… (ventana Lighting para ver el progreso). Guarda la escena al terminar.");
    }

    // Punto de vista definido para la distribución ORIGINAL (frente y giro viejos de una estación) llevado a donde está
    // ahora esa estación. Lo usan las capturas automáticas; sin el recorrido en U devuelve lo mismo.
    public static (Vector3 desde, Vector3 dir) Vista(string estacion, Vector3 frenteViejo, float yawViejo, Vector3 desde, Vector3 dir)
    {
        var raiz = Raiz();
        var t = raiz != null ? raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == estacion) : null;
        if (t == null) return (desde, dir);
        var q0 = Quaternion.Euler(0f, yawViejo, 0f);
        var local = Quaternion.Inverse(q0) * (desde - new Vector3(frenteViejo.x, t.position.y, frenteViejo.z));
        return (t.position + t.rotation * local, t.rotation * (Quaternion.Inverse(q0) * dir));
    }

    static Transform Raiz()
    {
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            foreach (var r in s.GetRootGameObjects())
            {
                var t = r.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "CentroExperimentacion");
                if (t != null) return t;
            }
        }
        return null;
    }

    static Transform Buscar(Transform raiz, string ruta)
    {
        var t = raiz;
        foreach (var parte in ruta.Split('/'))
        {
            t = t.Cast<Transform>().FirstOrDefault(c => c.name == parte);
            if (t == null) return null;
        }
        return t;
    }
}
