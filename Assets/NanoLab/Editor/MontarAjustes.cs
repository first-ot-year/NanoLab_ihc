using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using MI = MontarIntroduccion;

// Panel de ajustes ☰ del jugador (RNF-07): volumen de música y efectos, modo sentado y recalibrar la
// altura. Se monta como hijo (inactivo) del rig y lo maneja MenuAjustes en tiempo de ejecución.
// Mismo estilo que las balizas: paneles redondeados del Interaction SDK y Atkinson Hyperlegible.
public static class MontarAjustes
{
    const string Nombre = "PanelAjustes";
    static readonly Color Fondo = new Color(0.075f, 0.09f, 0.115f, 1f);
    static readonly Color Cian = new Color(0.37f, 0.83f, 1f);
    static readonly Color Tenue = new Color(0.62f, 0.7f, 0.78f, 1f);
    static readonly Color FondoBoton = new Color(0.14f, 0.2f, 0.27f, 1f);

    public static void Construir(GameObject rigRoot)
    {
        MI.Recursos();
        foreach (var t in rigRoot.GetComponentsInChildren<Transform>(true))
            if (t != null && t.name == Nombre) Object.DestroyImmediate(t.gameObject);

        var menu = rigRoot.GetComponent<MenuAjustes>();
        if (menu == null) menu = rigRoot.AddComponent<MenuAjustes>();
        var p = new GameObject(Nombre).transform;
        p.SetParent(rigRoot.transform, false);

        MI.Panel(p, "Fondo", 0f, -0.15f, 0f, 0.62f, 0.86f, 0.03f, Fondo, Cian, 0.003f, 0);
        Izq(p, "AJUSTES", -0.27f, 0.225f, 0.3f, 0.4f, Cian, true);
        MI.Texto(p, "Botón menú del mando izq. para cerrar", 0.145f, 0.225f, 0.29f, 0.2f, Tenue, false, TextAlignmentOptions.Right);

        MI.Texto(p, "NanoLab VR · complemento educativo: contenido simplificado", 0f, 0.165f, 0.56f, 0.16f, Tenue, false, TextAlignmentOptions.Center);   // RNF-06

        var botones = new List<BotonMenu>();
        menu.barraMusica = Fila(p, 0.11f, "Música", "musica", botones, out menu.musica);
        menu.barraEfectos = Fila(p, 0.02f, "Efectos", "efectos", botones, out menu.efectos);

        Izq(p, "Modo sentado", -0.27f, -0.07f, 0.2f, 0.27f, Color.white, true);
        botones.Add(Boton(p, "sentado", "CAMBIAR", "cambiar el modo sentado", 0f, -0.07f, 0.13f));
        menu.sentado = Izq(p, "NO", 0.08f, -0.07f, 0.22f, 0.27f, Color.white, false);

        botones.Add(Boton(p, "calibrar", "AJUSTAR ALTURA", "poner la vista a tu altura", -0.13f, -0.165f, 0.3f));
        // Ayuda bajo demanda: qué toca ahora, controles y glosario (AvisoInicio.MostrarAyuda).
        botones.Add(Boton(p, "ayuda", "AYUDA", "ver la ayuda y los controles", 0.17f, -0.165f, 0.24f));
        // Partida guardada en el visor (se guarda sola; aquí se fuerza). Checkpoint: se crea al terminar cada misión.
        botones.Add(Boton(p, "guardar", "GUARDAR PARTIDA", "guardar la partida", -0.13f, -0.255f, 0.3f));
        botones.Add(Boton(p, "checkpoint", "VOLVER AL CHECKPOINT", "volver al último checkpoint", 0.17f, -0.255f, 0.24f));
        // Lo que borra el avance va en otra fila que lo que lo guarda, y en rojo (prevención de errores).
        botones.Add(Boton(p, "nueva", "NUEVA PARTIDA", "empezar una partida nueva", -0.13f, -0.345f, 0.3f, new Color(1f, 0.45f, 0.4f)));
        botones.Add(Boton(p, "cerrar", "CERRAR", "cerrar los ajustes", 0.17f, -0.345f, 0.24f));
        // TEMPORAL (pruebas): reinicia la partida de un toque, sin confirmación. NUEVA PARTIDA hace lo mismo pidiendo confirmar.
        botones.Add(Boton(p, "reiniciar", "REINICIAR PARTIDA", "reiniciar la partida", 0f, -0.435f, 0.4f, new Color(1f, 0.45f, 0.4f)));
        menu.partida = MI.Texto(p, "", 0f, -0.49f, 0.58f, 0.2f, Tenue, false, TextAlignmentOptions.Center);
        MI.Texto(p, "Sentado en una silla real: activa el modo sentado y la vista sube a la de alguien de pie.\nToca los botones o apúntalos con el rayo.",
            0f, -0.54f, 0.58f, 0.18f, Tenue, false, TextAlignmentOptions.Center);

        // Los paneles son transparentes (no escriben profundidad): con un orden de dibujo mayor que el de
        // los rótulos y balizas, nada de lo que hay detrás se cuela por encima del panel.
        foreach (var r in p.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingOrder += r.GetComponent<TMP_Text>() != null ? 40 : 30;
            if (PrefabUtility.IsPartOfPrefabInstance(r)) PrefabUtility.RecordPrefabInstancePropertyModifications(r);   // los bloques poke
        }

        menu.panel = p.gameObject;
        menu.botones = botones.ToArray();
        p.gameObject.SetActive(false);
        EditorUtility.SetDirty(menu);
        PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
        Debug.Log($"[Ajustes] Panel ☰ montado en '{rigRoot.name}' con {botones.Count} botones.");
    }

    // Etiqueta, [−], barra de 10 segmentos, [+] y porcentaje.
    static PanelRedondeado[] Fila(Transform p, float y, string titulo, string id, List<BotonMenu> botones, out TMP_Text porcentaje)
    {
        Izq(p, titulo, -0.27f, y, 0.14f, 0.27f, Color.white, true);
        botones.Add(Boton(p, id + "-", "-", "bajar el volumen de " + titulo.ToLower(), -0.1f, y, 0.06f));
        var barra = new PanelRedondeado[10];
        for (int i = 0; i < barra.Length; i++)
            barra[i] = MI.Panel(p, "Nivel" + i, -0.055f + i * 0.016f, y, -0.002f, 0.011f, 0.04f, 0.003f, Cian, Color.clear, 0f, 1);
        botones.Add(Boton(p, id + "+", "+", "subir el volumen de " + titulo.ToLower(), 0.145f, y, 0.06f));
        porcentaje = Izq(p, "100 %", 0.19f, y, 0.11f, 0.27f, Color.white, false);
        return barra;
    }

    // Bloque "poke" oficial del Interaction SDK de Meta (el mismo prefab que los botones de las estaciones): el botón
    // se pulsa con la punta del dedo o del mando y se hunde. Sin el prefab, el botón queda plano (respaldo).
    static void Poke(GameObject go, BotonMenu b, float ancho, bool peligro)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PokePrefab);
        if (prefab == null) { Debug.LogWarning("[Ajustes] Falta el prefab PokeInteractable del Interaction SDK: botones planos."); return; }
        var pk = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
        pk.name = "Poke";
        // El bloque se pulsa hacia su +z: hacia dentro del panel, que se lee desde -z (igual que AvisoInicio.BotonPoke).
        pk.transform.localPosition = Vector3.zero;
        pk.transform.localRotation = Quaternion.identity;
        pk.transform.localScale = new Vector3(ancho, 0.06f, 0.05f);
        var color = pk.GetComponentInChildren<Oculus.Interaction.InteractableColorVisual>(true);
        if (color != null)
        {
            var so = new SerializedObject(color);
            void Estado(string campo, Color c, float t)
            {
                so.FindProperty(campo + ".Color").colorValue = c;
                so.FindProperty(campo + ".ColorTime").floatValue = t;
            }
            Estado("_normalColorState", peligro ? new Color(0.45f, 0.16f, 0.15f, 1f) : new Color(0.14f, 0.3f, 0.42f, 1f), 0.15f);
            Estado("_hoverColorState", peligro ? new Color(0.8f, 0.3f, 0.26f, 1f) : new Color(0.3f, 0.75f, 0.95f, 1f), 0.08f);
            Estado("_selectColorState", new Color(0.45f, 1f, 0.65f, 1f), 0.03f);
            Estado("_disabledColorState", new Color(0.25f, 0.28f, 0.3f, 1f), 0.1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(color);
        }
        // Mismo orden de dibujo que el fondo plano del botón (el bucle final de Construir le suma el del panel).
        foreach (var r in pk.GetComponentsInChildren<Renderer>(true)) r.sortingOrder = 2;
        // La etiqueta, 1,5 mm por delante de la cara que se hunde (PokeInteractableVisual), en su lado -z (el del
        // usuario). Se mide solo esa cara: el prefab entero es más hondo y la etiqueta quedaría flotando delante.
        // Se mide en el marco del botón (el panel cuelga del rig, que está girado: los bounds del mundo no valen).
        var cara = EstiloNanoLab.CaraPoke(pk);
        float frente = float.MaxValue;
        if (cara != null)
            foreach (var r in cara.GetComponentsInChildren<Renderer>(true))
            {
                var lb = r.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    var esquina = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    frente = Mathf.Min(frente, go.transform.InverseTransformPoint(r.transform.TransformPoint(esquina)).z);
                }
            }
        if (frente < float.MaxValue)
        {
            b.etiqueta.transform.localPosition = new Vector3(0f, 0f, frente - 0.0015f);
        }
        b.fondo.gameObject.SetActive(false);   // el bloque poke es ahora el cuerpo del botón
    }

    // Texto alineado a la izquierda cuyo borde izquierdo está en x.
    static TextMeshPro Izq(Transform p, string s, float x, float y, float ancho, float tam, Color c, bool negrita)
    {
        var t = MI.Texto(p, s, x + ancho / 2f, y, ancho, tam, c, negrita, TextAlignmentOptions.Left);
        return t;
    }

    const string PokePrefab = "Packages/com.meta.xr.sdk.interaction/Runtime/Prefabs/Poke/PokeInteractable.prefab";

    // 'borde': color para los botones que borran avance (NUEVA PARTIDA).
    internal static BotonMenu Boton(Transform p, string id, string texto, string accion, float x, float y, float ancho, Color? borde = null)
    {
        var go = new GameObject("Boton_" + id);
        go.transform.SetParent(p, false);
        go.transform.localPosition = new Vector3(x, y, -0.003f);
        var b = go.AddComponent<BotonMenu>();
        b.id = id;
        b.accion = accion;
        b.alcanceRayo = 2f;
        b.fondo = MI.Panel(go.transform, "Fondo", 0f, 0f, 0f, ancho, 0.06f, 0.012f, FondoBoton, borde ?? Cian, 0.002f, 2);
        b.etiqueta = MI.Texto(go.transform, texto, 0f, 0f, ancho, texto.Length <= 2 ? 0.4f : texto.Length > 17 ? 0.19f : 0.22f, Color.white, true, TextAlignmentOptions.Center);
        Poke(go, b, ancho, borde.HasValue);
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;   // solo para el rayo: no empuja nada
        col.size = new Vector3(ancho, 0.06f, 0.02f);
        return b;
    }
}
