using TMPro;
using UnityEngine;
using K = KitVacuna;

// Panel de CONCLUSIONES del Centro de Experimentación, junto a los microscopios (lo monta MontajeMisiones).
// Es un atril con una pantalla y 3 botones (se tocan o se apuntan con el rayo):
//   MISIÓN 1: cuadro comparativo de las muestras A, B y C y la clasificación de cada una (bacteria / hongo / virus).
//   MISIÓN 2: después del ANTES | DESPUÉS, dos preguntas para explicar el resultado.
// No decide nada: RetoManager le dice qué mostrar y escucha 'Elegido'.
public class PanelConclusiones : MonoBehaviour
{
    public TMP_Text titulo, cuerpo, pregunta, respuesta;
    public BotonFisico[] botones;
    public TMP_Text[] rotulos;
    public EstacionGuia guia;          // destino de las flechas del suelo (GuiaReto)
    public Renderer marco;

    public event System.Action<int> Elegido;

    void Start()
    {
        for (int i = 0; i < botones.Length; i++)
        {
            int k = i;
            if (botones[i] != null) botones[i].Pulsado += () => Elegido?.Invoke(k);
        }
    }

    // 'opciones' null o vacío = sin pregunta (los botones se ocultan).
    public void Mostrar(string tituloTxt, string cuerpoTxt, string preguntaTxt, string[] opciones, string respuestaTxt, bool activo)
    {
        titulo.text = tituloTxt;
        cuerpo.text = cuerpoTxt;
        pregunta.text = preguntaTxt ?? "";
        respuesta.text = respuestaTxt ?? "";
        for (int i = 0; i < botones.Length; i++)
        {
            bool ver = opciones != null && i < opciones.Length;
            if (botones[i] != null) botones[i].gameObject.SetActive(ver);
            rotulos[i].gameObject.SetActive(ver);
            if (ver) rotulos[i].text = $"<b>{"ABC"[i]}</b> · {opciones[i]}";
        }
        if (marco != null) K.Pintar(marco, activo ? K.Amarillo : new Color(0.3f, 0.45f, 0.6f));
    }

    public Vector3 Encima => transform.TransformPoint(new Vector3(0f, 1.35f, 0.25f));

    // ------------------------------------------------------------------ montaje

    // 'pos' y 'yaw' en el mundo; el usuario queda en el +z local (delante del atril).
    public static PanelConclusiones Crear(Transform padre, Vector3 pos, float yaw)
    {
        var g = new GameObject("PanelConclusiones").transform;
        g.SetParent(padre, true);
        g.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        var p = g.gameObject.AddComponent<PanelConclusiones>();
        var blanco = K.Escena("Blanco", new Color(0.93f, 0.93f, 0.92f));
        var oscuro = K.Escena("Oscuro", new Color(0.12f, 0.14f, 0.17f));
        const float h = 0.93f;

        // Atril: mueble que frena al jugador y tablero oscuro con los botones.
        K.Solido(K.Caja(g, "Mueble", new Vector3(0f, h / 2f, 0f), new Vector3(0.95f, h, 0.45f), blanco, true));
        var tablero = K.Caja(g, "Tablero", new Vector3(0f, h + 0.01f, 0f), new Vector3(1.0f, 0.02f, 0.5f), oscuro, true);
        tablero.gameObject.AddComponent<BoxCollider>();
        p.marco = K.Caja(g, "Franja", new Vector3(0f, h - 0.05f, 0.227f), new Vector3(0.95f, 0.02f, 0.006f), K.Plano(Color.white));
        K.Caja(g, "Poste", new Vector3(0f, 1.3f, -0.2f), new Vector3(0.05f, 0.75f, 0.04f), K.Escena("Marco", Color.gray), true);

        p.botones = new BotonFisico[3];
        p.rotulos = new TMP_Text[3];
        var colores = new[] { new Color(0.2f, 0.55f, 0.95f), new Color(0.3f, 0.75f, 0.45f), new Color(0.85f, 0.4f, 0.55f) };
        for (int i = 0; i < 3; i++)
        {
            float x = 0.3f - i * 0.3f;   // A a la izquierda del usuario (que mira hacia -z)
            p.botones[i] = MontajeVacuna.Boton(g, "ABC"[i].ToString(), new Vector3(x, h + 0.02f, 0.02f), colores[i], EstiloNanoLab.Icono.Ninguno, "Conclusion" + i);
            p.rotulos[i] = K.Texto(g, "", new Vector3(x, h + 0.021f, 0.15f), 0.13f, Color.white, 0.28f, 0.07f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
            K.AutoTam(p.rotulos[i], 0.08f);
        }

        // Pantalla inclinada hacia el usuario (su -z mira al usuario tras girarla 180°).
        var pant = K.Grupo(g, "Pantalla", new Vector3(0f, 1.52f, -0.17f), Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-10f, 0f, 0f));
        K.Panel(pant, "Fondo", Vector3.zero, 1.0f, 0.82f, AdaptadorEscena.ColorPanel, AdaptadorEscena.BordePanel, 0.003f, 0.03f);
        p.titulo = K.Texto(pant, "", new Vector3(0f, 0.385f, -0.004f), 0.3f, K.Amarillo, 0.94f, 0.06f, TextAlignmentOptions.Top, true, true);
        K.AutoTam(p.titulo, 0.2f);
        p.cuerpo = K.Texto(pant, "", new Vector3(0f, 0.315f, -0.004f), 0.2f, Color.white, 0.94f, 0.34f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(p.cuerpo, 0.13f);
        p.pregunta = K.Texto(pant, "", new Vector3(0f, -0.04f, -0.004f), 0.24f, K.Amarillo, 0.94f, 0.12f, TextAlignmentOptions.TopLeft, true, true);
        K.AutoTam(p.pregunta, 0.15f);
        p.respuesta = K.Texto(pant, "", new Vector3(0f, -0.17f, -0.004f), 0.19f, Color.white, 0.94f, 0.22f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(p.respuesta, 0.12f);
        var cab = K.Texto(g, "CONCLUSIONES", new Vector3(0f, 1.98f, -0.17f), 0.36f, K.Cian, 0.9f, 0.08f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        cab.outlineWidth = 0.2f;

        // Destino de la guía de flechas (como el cartel de una estación).
        p.guia = g.gameObject.AddComponent<EstacionGuia>();
        p.guia.numero = 0;
        p.guia.titulo = "CONCLUSIONES";
        p.guia.descripcion = "Compara las muestras y explica tus resultados.";
        p.guia.puntoBaliza = K.Grupo(g, "PuntoBaliza", new Vector3(0f, 0f, 0.9f));
        return p;
    }
}
