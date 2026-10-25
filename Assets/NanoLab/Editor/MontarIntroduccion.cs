using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MM = MontarMicrobiologia;
using ML = MontarLaboratorio;

// Introducción narrativa (RF-01): tótem estático frente al punto de aparición. El estudiante es Baymax,
// ha perdido sus recuerdos científicos y debe recuperarlos investigando. Lo invita a mirarse en el espejo
// del recibidor, lo orienta hacia las áreas (hasta el banco de muestras del Centro) y muestra el
// indicador de recuerdos recuperados. Incluye el aviso de contenido simplificado (RNF-06).
public static class MontarIntroduccion
{
    const string Microbiologia = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string Quimica = "Assets/NanoLab/Scenes/Laboratorio.unity";
    static readonly Vector3 Spawn = new Vector3(-3.3f, 0f, 2.24f);
    static readonly Vector3 PosIntro = new Vector3(-2.2f, 0f, 3.35f);
    public static readonly Vector3 PosTutorial = new Vector3(-0.6f, 0f, 3.6f);

    [MenuItem("NanoLab/Montar introducción Baymax", false, 26)]
    public static void AplicarEscenaMicrobiologia()
    {
        var s = EditorSceneManager.OpenScene(Microbiologia, OpenSceneMode.Single);
        MM.CrearMateriales();
        Aplicar(s);
        EditorSceneManager.SaveScene(s);
        OrdenarEscenas();
    }

    public static void Aplicar(Scene s)
    {
        if (MM.mats == null) MM.CrearMateriales();
        MM.suelo = ML.AlturaSuelo(ML.BoundsEscena());

        var tutorial = s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Tutorial");
        var padre = tutorial != null ? tutorial.parent : new GameObject("Bienvenida").transform;
        var vieja = padre.Find("Introduccion");
        if (vieja != null) Object.DestroyImmediate(vieja.gameObject);

        // El tutorial de portaobjetos se aparta a la derecha para dejar la introducción delante.
        if (tutorial != null)
        {
            tutorial.position = new Vector3(PosTutorial.x, MM.suelo, PosTutorial.z);
            var d = Spawn - PosTutorial; d.y = 0f;
            tutorial.rotation = Quaternion.LookRotation(-d.normalized);
        }

        Construir(padre);
        Debug.Log($"[Intro] {s.name}: introducción de Baymax montada en {PosIntro}.");
    }

    static void Construir(Transform padre)
    {
        var panel = MM.Totem(padre, "Introduccion", PosIntro, Spawn, 1.4f, 1.62f);
        var raiz = panel.parent;
        var cian = new Color(0.4f, 0.9f, 1f);
        var amarillo = new Color(1f, 0.82f, 0.35f);

        // Cara de Baymax (dos ojos unidos por una línea) junto al título.
        var cara = MM.Grupo(panel, "CaraBaymax");
        cara.localPosition = new Vector3(-0.52f, 0.64f, -0.02f);
        MM.Cilindro(cara, "Cara", Vector3.zero, new Vector3(0.24f, 0.004f, 0.2f), Quaternion.Euler(90f, 0f, 0f), MM.mats["BlancoMate"]);
        foreach (var x in new[] { -0.055f, 0.055f })
            MM.Cilindro(cara, "Ojo", new Vector3(x, 0f, -0.005f), new Vector3(0.04f, 0.002f, 0.04f), Quaternion.Euler(90f, 0f, 0f), MM.mats["NegroMate"]);
        MM.Caja(cara, "Linea", new Vector3(0f, 0f, -0.005f), new Vector3(0.11f, 0.006f, 0.002f), MM.mats["NegroMate"], false);

        var titulo = MM.Texto3D(panel, "ERES BAYMAX", new Vector3(0.12f, 0.64f, -0.02f), Quaternion.identity, 0.85f, cian, 1.1f);
        titulo.alignment = TextAlignmentOptions.Center; titulo.fontStyle = FontStyles.Bold;

        var historia = MM.Texto3D(panel,
            "Has perdido tus <b>recuerdos científicos</b>. Para recuperarlos tendrás que investigar muestras como un microbiólogo: observar, consultar, decidir y comprobar.",
            new Vector3(0f, 0.41f, -0.02f), Quaternion.identity, 0.4f, Color.white, 1.3f);
        historia.alignment = TextAlignmentOptions.Center;

        var espejo = MM.Texto3D(panel, "<b>Mírate en el ESPEJO de tu derecha  >></b>", new Vector3(0f, 0.2f, -0.02f), Quaternion.identity, 0.45f, amarillo, 1.3f);
        espejo.alignment = TextAlignmentOptions.Center;

        var dondeT = MM.Texto3D(panel, "DÓNDE IR", new Vector3(0f, 0.07f, -0.02f), Quaternion.identity, 0.42f, cian, 1.3f);
        dondeT.alignment = TextAlignmentOptions.Center; dondeT.fontStyle = FontStyles.Bold;
        var donde = MM.Texto3D(panel,
            "<b>1.</b> Puestos de microscopio: observa muestras libremente.\n" +
            "<b>2.</b> Cepario (bacterias) y zona de alto riesgo (virus, al fondo).\n" +
            "<b>3.</b> <color=#FFD25A><b>CENTRO DE EXPERIMENTACIÓN</b></color> (puerta de la pared derecha): en el <b>BANCO DE MUESTRAS</b> elige el CASO 01 y recupera tu primer recuerdo.",
            new Vector3(0f, -0.16f, -0.02f), Quaternion.identity, 0.33f, Color.white, 1.3f);
        donde.alignment = TextAlignmentOptions.Left;

        // Indicador de recuerdos recuperados (se actualiza en tiempo de ejecución).
        var indicador = panel.gameObject.AddComponent<IndicadorRecuerdos>();
        indicador.texto = MM.Texto3D(panel, "", new Vector3(-0.12f, -0.46f, -0.02f), Quaternion.identity, 0.4f, Color.white, 1.0f);
        indicador.texto.alignment = TextAlignmentOptions.Left; indicador.texto.fontStyle = FontStyles.Bold;
        indicador.ranuras = new Renderer[RecuerdosBaymax.Total];
        for (int i = 0; i < indicador.ranuras.Length; i++)
        {
            var r = MM.Caja(panel, "Recuerdo" + i, new Vector3(0.38f + i * 0.11f, -0.46f, -0.015f), new Vector3(0.08f, 0.08f, 0.01f), MM.mats["BlancoMate"], false);
            indicador.ranuras[i] = r.GetComponent<Renderer>();
        }
        indicador.texto.text = $"RECUERDOS CIENTÍFICOS: <color=#FFD25A>0/{RecuerdosBaymax.Total}</color>";

        var controles = MM.Texto3D(panel, "Joystick izq.: caminar  ·  joystick der.: girar  ·  gatillo o grip: agarrar",
            new Vector3(0f, -0.6f, -0.02f), Quaternion.identity, 0.28f, new Color(0.56f, 0.85f, 1f), 1.3f);
        controles.alignment = TextAlignmentOptions.Center;
        var aviso = MM.Texto3D(panel, "Complemento educativo: las muestras, intervenciones y resultados están simplificados; no son protocolos clínicos ni de laboratorio reales.",
            new Vector3(0f, -0.72f, -0.02f), Quaternion.identity, 0.24f, new Color(0.66f, 0.75f, 0.82f), 1.3f);
        aviso.alignment = TextAlignmentOptions.Center; aviso.fontStyle = FontStyles.Italic;

        GameObjectUtility.SetStaticEditorFlags(raiz.Find("Pie").gameObject, StaticEditorFlags.BatchingStatic);
    }

    // La experiencia empieza en Microbiología; Química queda como sala secundaria y sin escenas de ejemplo.
    public static void OrdenarEscenas()
    {
        var lista = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(Microbiologia, true) };
        if (System.IO.File.Exists(Quimica)) lista.Add(new EditorBuildSettingsScene(Quimica, true));
        EditorBuildSettings.scenes = lista.ToArray();
        Debug.Log("[Intro] Escenas de build: " + string.Join(", ", lista.Select(e => e.path)));
    }
}
