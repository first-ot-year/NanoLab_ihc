using System.Collections.Generic;
using TMPro;
using UnityEngine;
using C = ContenidoVacuna;
using K = KitVacuna;
using A = AgarreRuntime;

// Monta, al empezar, el ÚNICO reto del Centro de Experimentación: "LABORATORIO DE VACUNAS" (CASO 01 · Crea la vacuna).
// Antes desactiva el reto anterior de bacterias (sus estaciones quedan como mobiliario; ver OcultarRetoAnterior)
// y pone la estación en el centro de la sala, donde estaba la cámara experimental, de cara a la puerta. Se construye en tiempo de ejecución para no tener
// que volver a montar la escena ni hornear la luz: un mostrador de trabajo con cuatro mesas, de izquierda a derecha
// en el orden del proceso:
//   DISEÑO (molde de 5 huecos + bandeja de bloques; luego, la placa de cultivo) · SÍNTESIS (gradilla de frascos,
//   pipeta, tubo de reacción y termobloque) · NANOPARTÍCULAS (cartuchos de lípidos y mezclador) · CALIDAD Y FRÍO
//   (analizador, ultracongelador, nevera y mesa).
// Encima, la pantalla del caso, la pantalla de visualización y la barra de pasos; a los lados, la GUÍA y los
// CONCEPTOS CLAVE. Lo llama AdaptadorEscena (que añade JugadorNanoLab). Marco local: +z apunta hacia el usuario.
public static class MontajeVacuna
{
    public const string NombreRaiz = "EstacionVacuna";
    const float T = 0.91f;   // altura del tablero del mostrador

    // Huecos del centro (constantes de MontarCentroExperimentacion: sala x 6.87-24.14, z -0.25-17.26) y hacia dónde
    // queda el usuario. Se usa el primero sin obstáculos.
    static readonly (Vector3 pos, float yaw)[] Candidatos =
    {
        (new Vector3(15.5f, 0f, 8.5f), -90f),     // centro de la sala (donde estaba la cámara), de cara a la puerta oeste
        (new Vector3(11.5f, 0f, 13.45f), 180f),   // entre la recepción y el archivo, mirando a la entrada
        (new Vector3(19.0f, 0f, 13.45f), 180f),   // entre la cámara y la terminal
        (new Vector3(11.8f, 0f, 4.3f), 0f),       // frente al banco de muestras
    };

    public static readonly Color Violeta = new Color(0.72f, 0.5f, 1f);
    // Centro (x) de cada mesa del mostrador. El usuario mira hacia -z, así que +x queda a SU IZQUIERDA:
    // las mesas van de izquierda a derecha (para el usuario) en el orden del proceso.
    public static readonly float[] Mesas = { 1.15f, 0.42f, -0.35f, -1.15f };

    public static RetoVacuna Asegurar()
    {
        if (RetoVacuna.Instancia != null) return RetoVacuna.Instancia;
        var rm = RetoManager.Instancia != null ? RetoManager.Instancia : Object.FindObjectOfType<RetoManager>();
        if (rm == null) { Debug.Log("[Vacuna] Esta escena no tiene Centro de Experimentación: no se monta la estación de la vacuna."); return null; }

        float suelo = rm.zona.min.y;
        OcultarRetoAnterior(rm);
        Physics.SyncTransforms();
        int mejor = -1, menos = int.MaxValue;
        for (int i = 0; i < Candidatos.Length; i++)
        {
            var p = Candidatos[i].pos; p.y = suelo;
            if (!rm.zona.Contains(p + Vector3.up)) continue;
            int n = Obstaculos(p, Candidatos[i].yaw, out string que);
            if (n > 0) Debug.Log($"[Vacuna] Hueco {i + 1} ocupado por {n} objeto(s): {que}");
            if (n < menos) { menos = n; mejor = i; }
            if (n == 0) break;
        }
        if (mejor < 0) mejor = 0;
        if (menos > 0) Debug.LogWarning("[Vacuna] No hay ningún hueco totalmente libre en el centro; se usa el que menos choca.");

        var pos = Candidatos[mejor].pos; pos.y = suelo;
        var rot = Quaternion.Euler(0f, Candidatos[mejor].yaw, 0f);
        var raiz = new GameObject(NombreRaiz).transform;
        raiz.SetPositionAndRotation(pos, rot);
        var reto = Construir(raiz);
        var centro = raiz.TransformPoint(new Vector3(0f, 1.2f, 0.6f));
        bool girada = Mathf.Abs(Mathf.Sin(Candidatos[mejor].yaw * Mathf.Deg2Rad)) > 0.5f;
        reto.zona = new Bounds(centro, girada ? new Vector3(3.4f, 2.6f, 5.6f) : new Vector3(5.6f, 2.6f, 3.4f));
        Debug.Log($"[Vacuna] LABORATORIO DE VACUNAS ({C.Codigo}) montado en {pos} (hueco {mejor + 1}).");
        return reto;
    }

    // El reto anterior (bacterias) deja de ser un reto, pero sus estaciones VUELVEN COMO MOBILIARIO del laboratorio
    // (banco de muestras, tinción, microscopía con sus microscopios, biblioteca de fichas, terminal, prensa y archivo).
    // Solo se ocultan: la cámara experimental del centro (su sitio lo ocupa la vacuna), la torre de objetivos que cuelga
    // encima, el tótem "Tu objetivo", la baliza, su prueba y la guía de flechas; y de cada estación, lo que pertenecía al
    // caso viejo: el cartel con PISTA, la placa del caso, los rótulos "CASO 0x", la franja y el número del suelo. Su
    // RetoManager se desactiva. No se borra nada de la escena guardada.
    static readonly string[] GruposRetoAnterior = { "7_CamaraExperimental", "TorreObjetivo", "TuObjetivo", "Baliza", "DebugReto" };

    public static void OcultarRetoAnterior(RetoManager rm)
    {
        var centro = rm.transform;
        while (centro.parent != null && centro.name != "CentroExperimentacion") centro = centro.parent;
        var ocultos = new List<string>();
        Transform estaciones = null;
        foreach (var t in centro.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            if (t.name == "Estaciones") estaciones = t;
            if (System.Array.IndexOf(GruposRetoAnterior, t.name) >= 0 && t.gameObject.activeSelf)
            {
                t.gameObject.SetActive(false);
                ocultos.Add(t.name);
            }
        }

        int piezas = 0;
        if (estaciones != null)
        {
            if (!estaciones.gameObject.activeSelf) estaciones.gameObject.SetActive(true);
            foreach (var g in estaciones.GetComponentsInChildren<EstacionGuia>(true)) { g.gameObject.SetActive(false); piezas++; }   // tótems con PISTA
            foreach (var p in estaciones.GetComponentsInChildren<PlacaCaso>(true)) { p.gameObject.SetActive(false); piezas++; }      // placa del caso
            foreach (var p in estaciones.GetComponentsInChildren<PrensaCapsulas>(true)) p.enabled = false;                          // su botón ya no formula
            foreach (var t in estaciones.GetComponentsInChildren<TerminalHipotesis>(true))
            {
                t.enabled = false;
                if (t.pantalla != null) t.pantalla.text = "TERMINAL DE DATOS DEL LABORATORIO\n<size=75%><color=#A9BED2>Consulta las fichas de la biblioteca y los microscopios de la sala.</color></size>";
            }
            // Franja y número de estación pintados en el suelo (eran los pasos 1-8 del caso viejo).
            foreach (Transform est in estaciones)
                foreach (Transform h in est)
                    if (h.name == "FranjaSuelo" || (h.GetComponent<TMP_Text>() != null && h.localPosition.y < 0.02f)) { h.gameObject.SetActive(false); piezas++; }
            // Rótulos que hablan del caso viejo ("CASO 01 · Herida de piel · ACTIVO", "CASO 02 · Próximamente"…).
            foreach (var tx in estaciones.GetComponentsInChildren<TMP_Text>(true))
                if (tx.text != null && tx.text.Contains("CASO 0")) { tx.gameObject.SetActive(false); piezas++; }
        }

        // El tótem de bienvenida de la entrada contaba el caso de bacterias: ahora presenta el de la vacuna.
        foreach (var tx in centro.GetComponentsInChildren<TMP_Text>(true))
            if (tx.text != null && tx.text.Contains("perdiste tus"))
                tx.text = "<color=#FFD25A><b>BAYMAX:</b></color> perdiste tus <b>recuerdos científicos</b>. Resuelve el caso de este centro para recuperar uno.\n\n" +
                          $"<b>{C.Codigo} · {C.Titulo.ToUpper()} DE ARNm</b>\nUn coronavirus se extiende por la ciudad. Ve al <b>LABORATORIO DE VACUNAS</b>, en el centro de la sala, " +
                          "y fabrica una vacuna de ARN mensajero: del antígeno a las pruebas en personas.\n\n" +
                          "<color=#8FD8FF>Joystick izq.: caminar · joystick der.: girar · grip: agarrar · gatillo: pipeta y rayo · cada paso tiene un botón AYUDA (amarillo).</color>";
        var guia = Object.FindObjectOfType<GuiaReto>(true);
        if (guia != null) Object.Destroy(guia.gameObject);
        rm.enabled = false;   // deja de escuchar el microscopio y las muestras; RetoManager.Instancia pasa a null
        Debug.Log($"[Vacuna] Reto anterior desactivado: ocultos {string.Join(", ", ocultos)}; mobiliario de las estaciones restaurado ({piezas} piezas del caso viejo quitadas). Único reto: {C.Codigo} · {C.Titulo}.");
    }

    static int Obstaculos(Vector3 p, float yaw, out string que)
    {
        var rot = Quaternion.Euler(0f, yaw, 0f);
        var cajas = new[]
        {
            (centro: new Vector3(0f, 1.35f, -0.3f), tam: new Vector3(5.5f, 2.1f, 1.6f)),   // mostrador y tableros
            (centro: new Vector3(0f, 1.0f, 1.0f), tam: new Vector3(2.8f, 1.4f, 1.4f)),     // sitio del usuario
        };
        var vistos = new HashSet<Collider>();
        foreach (var (c, t) in cajas)
            foreach (var col in Physics.OverlapBox(p + rot * c, t / 2f, rot, ~0, QueryTriggerInteraction.Ignore))
                if (col.GetComponentInParent<JugadorNanoLab>() == null) vistos.Add(col);
        var nombres = new List<string>();
        foreach (var v in vistos) if (nombres.Count < 4) nombres.Add(v.name);
        que = string.Join(", ", nombres);
        return vistos.Count;
    }

    // ------------------------------------------------------------------ construcción

    static RetoVacuna Construir(Transform raiz)
    {
        var blanco = K.Escena("Blanco", new Color(0.93f, 0.93f, 0.92f));
        var oscuro = K.Escena("Oscuro", new Color(0.12f, 0.14f, 0.17f));
        var marco = K.Escena("Marco", new Color(0.55f, 0.58f, 0.62f));

        // Mostrador: el mueble frena al jugador (capa BloqueoJugador); el tablero tiene un collider normal para que
        // lo que se suelta quede encima en vez de atravesarlo.
        K.Solido(K.Caja(raiz, "Mueble", new Vector3(0f, 0.435f, -0.37f), new Vector3(3.1f, 0.87f, 0.66f), blanco, true));
        var tablero = K.Caja(raiz, "Tablero", new Vector3(0f, 0.89f, -0.37f), new Vector3(3.2f, 0.04f, 0.76f), oscuro, true);
        tablero.gameObject.AddComponent<BoxCollider>();
        K.Caja(raiz, "Zocalo", new Vector3(0f, 0.04f, -0.035f), new Vector3(3.08f, 0.08f, 0.01f), oscuro);
        // Fondo con las pantallas.
        K.Solido(K.Caja(raiz, "Respaldo", new Vector3(0f, 1.62f, -0.775f), new Vector3(3.14f, 1.42f, 0.03f), oscuro, true));
        foreach (var x in new[] { -1.57f, 1.57f })
            K.Caja(raiz, "Poste", new Vector3(x, 1.62f, -0.75f), new Vector3(0.06f, 1.42f, 0.06f), marco, true);

        var frente = Quaternion.Euler(0f, 180f, 0f);   // marcos de pantalla: su -z mira al usuario
        // Pantallas más pequeñas (82 %) y más bajas: el centro a 1,62 m, cerca de la altura de los ojos.
        const float escalaPantalla = 0.82f;
        var caso = K.Grupo(raiz, "PantallaCaso", new Vector3(0.64f, 1.62f, -0.755f), frente);       // izquierda
        var visual = K.Grupo(raiz, "PantallaVisual", new Vector3(-0.64f, 1.62f, -0.755f), frente);   // derecha
        caso.localScale = visual.localScale = Vector3.one * escalaPantalla;
        var cab = K.Grupo(raiz, "Cabecera", new Vector3(0f, 2.3f, -0.72f), Quaternion.Euler(10f, 0f, 0f) * frente);   // inclinada hacia el usuario
        cab.localScale = Vector3.one * 0.88f;

        var reto = raiz.gameObject.AddComponent<RetoVacuna>();
        PanelCaso(caso, reto);
        PanelVisual(visual, reto);
        Cabecera(cab, reto);
        Botones(raiz, reto);
        Franjas(raiz, reto);
        Holograma(raiz, reto);
        MesaDiseno(raiz, reto);
        MesaSintesis(raiz, reto);
        MesaNanoparticulas(raiz, reto);
        MesaCalidad(raiz, reto);
        PlacaCultivo(raiz, reto);
        Guia(raiz);
        Conceptos(raiz);
        Senales(raiz);
        return reto;
    }

    static void PanelCaso(Transform t, RetoVacuna r)
    {
        K.Panel(t, "Fondo", Vector3.zero, 1.46f, 1.3f, AdaptadorEscena.ColorPanel, AdaptadorEscena.BordePanel, 0.008f, 0.04f);
        var linea = K.Plano(new Color(0.3f, 0.45f, 0.6f));
        K.Caja(t, "Separador", new Vector3(0f, 0.232f, -0.002f), new Vector3(1.36f, 0.003f, 0.001f), linea);
        K.Caja(t, "Separador", new Vector3(0f, -0.068f, -0.002f), new Vector3(1.36f, 0.003f, 0.001f), linea);
        r.titulo = K.Texto(t, "", new Vector3(0f, 0.625f, -0.004f), 0.38f, K.Amarillo, 1.36f, 0.07f, TextAlignmentOptions.TopLeft, true, true);
        K.AutoTam(r.titulo, 0.28f);
        r.enunciado = K.Texto(t, "", new Vector3(0f, 0.54f, -0.004f), 0.27f, Color.white, 1.36f, 0.3f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(r.enunciado, 0.19f);
        r.opcionesTxt = K.Texto(t, "", new Vector3(0f, 0.218f, -0.004f), 0.27f, Color.white, 1.36f, 0.275f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(r.opcionesTxt, 0.19f);
        r.respuesta = K.Texto(t, "", new Vector3(0f, -0.082f, -0.004f), 0.27f, Color.white, 1.36f, 0.46f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(r.respuesta, 0.19f);
        r.estado = K.Texto(t, "", new Vector3(0f, -0.565f, -0.004f), 0.23f, new Color(0.8f, 0.87f, 0.95f), 1.36f, 0.06f, TextAlignmentOptions.TopLeft, false, true);
        // Modo PASO A PASO: una sola acción, grande, con el icono del botón del mando.
        r.iconoPaso = EstiloNanoLab.CrearIcono(t, "IconoPaso", new Vector3(-0.56f, 0.39f, -0.006f), 0.22f);
        r.iconoPaso.enabled = false;
        r.accionGrande = K.Texto(t, "", new Vector3(0.12f, 0.39f, -0.006f), 0.46f, Color.white, 1.08f, 0.29f, TextAlignmentOptions.Left, true);
        K.AutoTam(r.accionGrande, 0.3f);
    }

    static void PanelVisual(Transform t, RetoVacuna r)
    {
        K.Panel(t, "Fondo", Vector3.zero, 1.46f, 1.3f, new Color(0.03f, 0.08f, 0.16f, 0.98f), AdaptadorEscena.BordePanel, 0.008f, 0.04f);
        r.tituloVisual = K.Texto(t, "", new Vector3(0f, 0.625f, -0.004f), 0.34f, K.Cian, 1.36f, 0.07f, TextAlignmentOptions.Top, true, true);
        K.AutoTam(r.tituloVisual, 0.26f);
        r.pieVisual = K.Texto(t, "", new Vector3(0f, -0.5f, -0.004f), 0.24f, new Color(0.8f, 0.87f, 0.95f), 1.36f, 0.12f, TextAlignmentOptions.Top, false, true);
        K.AutoTam(r.pieVisual, 0.18f);

        // Franja libre bajo el título (entre el título y la gráfica o la placa) para los rótulos de "mira aquí".
        r.anclaMirar = K.Grupo(t, "AnclaMirar", new Vector3(0f, 0.47f, -0.02f));
        r.virus = VirusVacuna.Crear(t, new Vector3(0f, 0.04f, 0f));
        r.diagramaArnm = DiagramaARNm.Crear(t, new Vector3(0f, 0.05f, 0f));
        r.sintesis = DiagramaSintesis.Crear(t, new Vector3(0f, 0.05f, 0f));
        r.lnp = DiagramaLNP.Crear(t, new Vector3(0f, 0.05f, 0f));
        r.calidad = PanelCalidad.Crear(t, new Vector3(0f, 0.05f, 0f));
        r.placa = PlacaNeutralizacion.Crear(t, new Vector3(0f, 0.05f, 0f));
        r.fases = LineaFases.Crear(t, new Vector3(0f, 0.05f, 0f));
        r.grafica = GraficaEficacia.Crear(t, new Vector3(0f, 0.02f, 0f));
        r.siguiente = PanelSiguiente.Crear(t, new Vector3(0f, 0f, 0f));
        // Paso ANTÍGENO: el virus está en el holograma del mostrador; aquí, la clave de colores (sin la respuesta).
        r.leyendaAntigeno = K.Texto(t, "<color=#F2474D><b>ROJO</b></color> · espiga S\n<color=#8CCCFF><b>AZUL</b></color> · envoltura lipídica\n<color=#FF9926><b>NARANJA</b></color> · ARN (genoma)\n<color=#FAE65A><b>AMARILLO</b></color> · proteína N\n\n<size=80%><color=#A9BED2>Toca una parte del virus holográfico (o apúntala con el rayo).\nGrip junto al virus para girarlo.</color></size>",
            new Vector3(0f, 0.32f, -0.006f), 0.34f, Color.white, 1.2f, 0.8f, TextAlignmentOptions.Top, true, true);
        r.leyendaAntigeno.gameObject.SetActive(false);

        // Rótulos del virus que se eligen con el rayo (paso ANTÍGENO).
        r.selectoresVirus = new SelectorVacuna[4];
        for (int i = 0; i < 4; i++)
        {
            var rot = r.virus.Rotulo(i);
            r.selectoresVirus[i] = Selector(rot, $"elegir {C.Antigeno.opciones[i].corto}", new Vector3(0.36f, 0.09f, 0.03f), false);
        }

        // Diluciones de la placa que se eligen con el rayo (paso TÍTULO).
        r.selectoresColumna = new SelectorVacuna[PlacaNeutralizacion.Columnas];
        for (int c = 0; c < PlacaNeutralizacion.Columnas; c++)
        {
            var g = K.Grupo(r.placa.transform, "SelectorColumna" + c, r.placa.Cabecera(c));
            r.selectoresColumna[c] = Selector(g, $"elegir {PlacaNeutralizacion.Diluciones[c]}", new Vector3(0.12f, 0.6f, 0.03f), false, new Vector3(0f, -0.22f, 0f));
        }

        // Cierre: el recuerdo recuperado (el virus con anticuerpos se muestra encima).
        var c2 = K.Grupo(t, "Cierre", Vector3.zero);
        r.cierreTxt = K.Texto(c2, "", new Vector3(0f, -0.22f, -0.01f), 0.22f, K.Amarillo, 1.3f, 0.24f, TextAlignmentOptions.Center);
        K.AutoTam(r.cierreTxt, 0.15f);
        r.cierre = c2.gameObject;
    }

    static SelectorVacuna Selector(Transform padre, string accion, Vector3 tam, bool tactil, Vector3? centro = null)
    {
        var go = new GameObject("Selector");
        go.transform.SetParent(padre, false);
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = tam;
        bc.center = centro ?? Vector3.zero;
        var s = go.AddComponent<SelectorVacuna>();
        s.accion = accion;
        s.tactil = tactil;
        s.alcanceRayo = 2.6f;
        return s;
    }

    static void Cabecera(Transform t, RetoVacuna r)
    {
        K.Panel(t, "Fondo", Vector3.zero, 3.0f, 0.36f, new Color(0.04f, 0.1f, 0.2f, 0.98f), Violeta, 0.012f, 0.05f);
        r.cabecera = K.Texto(t, "", new Vector3(0f, 0.105f, -0.004f), 0.4f, Color.white, 2.9f, 0.09f, TextAlignmentOptions.Center, true);
        int n = C.Pasos.Length;
        float w = 2.88f / n;
        r.segmentos = new Renderer[n];
        r.etiquetasSegmento = new TMP_Text[n];
        for (int i = 0; i < n; i++)
        {
            float x = -1.44f + (i + 0.5f) * w;
            r.segmentos[i] = K.Caja(t, "Paso" + i, new Vector3(x, -0.115f, -0.003f), new Vector3(w - 0.03f, 0.05f, 0.002f), K.Plano(Color.white));
            // Mismo tamaño en todas (con AutoTam las cortas salían más grandes y parecían resaltadas).
            r.etiquetasSegmento[i] = K.Texto(t, $"{i + 1} {C.Pasos[i]}", new Vector3(x, -0.04f, -0.004f), 0.21f, K.Gris, w, 0.06f, TextAlignmentOptions.Center, true);
            r.etiquetasSegmento[i].enableWordWrapping = false;
        }
    }

    // ------------------------------------------------------------------ botones

    static void Botones(Transform raiz, RetoVacuna r)
    {
        const float z = -0.11f;
        int n = 5;
        r.opciones = new BotonFisico[n];
        r.cartelesOpcion = new GameObject[n];
        r.textosCartel = new TMP_Text[n];
        var azul = new Color(0.15f, 0.42f, 0.78f);
        for (int i = 0; i < n; i++)
        {
            float x = 0.6f - i * 0.24f;   // A a la izquierda del usuario
            r.opciones[i] = Boton(raiz, "ABCDE"[i].ToString(), new Vector3(x, T, z), azul);
            r.cartelesOpcion[i] = Cartel(raiz, new Vector3(x, T, z), "", K.Cian, out r.textosCartel[i]);
        }
        r.pista = Boton(raiz, "?", new Vector3(-0.95f, T, z), new Color(0.78f, 0.58f, 0.05f), EstiloNanoLab.Icono.Ninguno, "AYUDA");
        r.cartelPista = Cartel(raiz, new Vector3(-0.95f, T, z), "<b>AYUDA</b>", K.Amarillo, out _);
        // PASO A PASO: modo especial con una sola instrucción grande (el LED queda encendido mientras está activo).
        r.grupoPasoAPaso = BotonMaquina(raiz, "PasoAPaso", "PASO A PASO", new Vector3(1.4f, T, -0.1f), new Color(0.48f, 0.3f, 0.75f), out r.pasoAPaso);
        // CONTINUAR: gris mientras el paso no está hecho; verde (con LED y halo) cuando ya se puede avanzar.
        r.continuar = Boton(raiz, "", new Vector3(-1.3f, T, z), new Color(0.2f, 0.3f, 0.27f), EstiloNanoLab.Icono.Siguiente, "CONTINUAR", false);
        r.cartelContinuar = Cartel(raiz, new Vector3(-1.3f, T, z), "<b>CONTINUAR</b>", K.Verde, out _);
        r.repetir = Boton(raiz, "", new Vector3(-1.3f, T, z), new Color(0.48f, 0.3f, 0.75f), EstiloNanoLab.Icono.Repetir, "REPETIR", false);
        r.cartelRepetir = Cartel(raiz, new Vector3(-1.3f, T, z), "<b>REPETIR CASO</b>", Violeta, out _);
    }

    // Botón "poke" del Interaction SDK de Meta: se reutiliza el bloque oficial PokeInteractable de los botones de la
    // escena (tapa redondeada que se hunde con la punta del dedo o del mando y cambia de color al acercarse y al
    // pulsar). En la tapa va la letra o el icono, que se hunden con ella. Si no hay plantilla en la escena, botón de
    // respaldo (se pulsa al acercar la mano). Siempre se puede pulsar también con el rayo de la mano vacía.
    static BotonFisico Boton(Transform padre, string letra, Vector3 pos, Color color, EstiloNanoLab.Icono icono = EstiloNanoLab.Icono.Ninguno, string nombre = null, bool impreso = true)
    {
        // Las piezas (y sobre todo el texto: TextMeshPro necesita su Awake) se crean con el objeto ACTIVO; luego se
        // desactiva para que el Awake de BotonFisico se ejecute cuando ya tiene sus referencias.
        var go = new GameObject("Boton_" + (nombre ?? letra));
        var b = go.transform;
        b.SetParent(padre, false);
        b.localPosition = pos;
        b.localRotation = Quaternion.Euler(0f, 180f, 0f);
        K.Caja(b, "Base", new Vector3(0f, 0.005f, 0f), new Vector3(0.085f, 0.01f, 0.085f), K.Escena("Marco", Color.gray));
        K.Caja(b, "Bisel", new Vector3(0f, 0.0104f, 0f), new Vector3(0.075f, 0.0008f, 0.075f), K.Plano(new Color(0.08f, 0.1f, 0.13f)));
        var halo = K.Pieza(b, "Halo", PrimitiveType.Cylinder, new Vector3(0f, 0.0106f, 0f), new Vector3(0.1f, 0.0004f, 0.1f), K.Plano(Color.white));
        var led = K.Pieza(b, "Led", PrimitiveType.Cylinder, new Vector3(0.034f, 0.0112f, 0.034f), new Vector3(0.008f, 0.0006f, 0.008f), K.Plano(Color.white));

        Transform tapa = null, cara = b;
        float alto = 0.0235f;
        // Igual que en los botones de la escena: el bloque se pulsa hacia su +z, girado para pulsar hacia abajo.
        var p = EstiloNanoLab.CrearPoke(b, new Vector3(0f, 0.0105f, 0f), Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.064f, color);
        if (p != null)
        {
            var vis = p.GetComponentInChildren<Oculus.Interaction.PokeInteractableVisual>(true);
            if (vis != null)
            {
                cara = vis.transform;
                float arriba = float.MinValue;
                foreach (var rr in vis.GetComponentsInChildren<Renderer>(true)) arriba = Mathf.Max(arriba, rr.bounds.max.y);
                if (arriba > float.MinValue) alto = (arriba - b.position.y) / Mathf.Max(1e-4f, b.lossyScale.y) + 0.0012f;
            }
        }
        else
        {
            K.Pieza(b, "BiselRespaldo", PrimitiveType.Cylinder, new Vector3(0f, 0.0104f, 0f), new Vector3(0.068f, 0.0005f, 0.068f), K.Plano(new Color(0.08f, 0.1f, 0.13f)));
            tapa = K.Pieza(b, "Tapa", PrimitiveType.Cylinder, new Vector3(0f, 0.017f, 0f), new Vector3(0.058f, 0.006f, 0.058f), K.Plano(color)).transform;
        }

        // Carcasa: el botón va metido en su pedestal apoyado en el mostrador (no flota). Solo asoman 5 mm de tapa y
        // al pulsar se hunde dentro, como un botón de verdad. El LED y el halo van encima de la carcasa.
        float hc = Mathf.Max(0.012f, alto - 0.0012f - 0.005f);
        const float fuera = 0.085f, pared = 0.009f;
        var carcasa = K.Escena("Marco", Color.gray);
        K.Caja(b, "CarcasaFrente", new Vector3(0f, hc / 2f, (fuera - pared) / 2f), new Vector3(fuera, hc, pared), carcasa);
        K.Caja(b, "CarcasaFondo", new Vector3(0f, hc / 2f, -(fuera - pared) / 2f), new Vector3(fuera, hc, pared), carcasa);
        K.Caja(b, "CarcasaDer", new Vector3((fuera - pared) / 2f, hc / 2f, 0f), new Vector3(pared, hc, fuera - 2f * pared), carcasa);
        K.Caja(b, "CarcasaIzq", new Vector3(-(fuera - pared) / 2f, hc / 2f, 0f), new Vector3(pared, hc, fuera - 2f * pared), carcasa);
        halo.transform.localPosition = new Vector3(0f, hc + 0.0004f, 0f);
        led.transform.localPosition = new Vector3(0.038f, hc + 0.0006f, 0.038f);   // sobre la esquina de la carcasa

        // Letra o icono sobre la tapa (se hunde con ella). Los botones con icono llevan su nombre impreso delante.
        TMP_Text etiqueta;
        if (icono != EstiloNanoLab.Icono.Ninguno)
        {
            var sr = EstiloNanoLab.CrearIcono(b, "Icono", new Vector3(0f, alto, 0f), 0.042f, Quaternion.Euler(90f, 0f, 0f));
            EstiloNanoLab.Poner(sr, icono);
            sr.transform.SetParent(cara, true);
        }
        if (!string.IsNullOrEmpty(letra))
        {
            etiqueta = K.Texto(b, letra, new Vector3(0f, alto, 0f), 0.42f, Color.white, 0.06f, 0.055f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 0f, 0f));
            etiqueta.transform.SetParent(cara, true);
        }
        else
        {
            etiqueta = K.Texto(b, nombre ?? "", new Vector3(0f, 0.002f, -0.07f), 0.15f, Color.white, 0.22f, 0.04f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 0f, 0f));
            K.AutoTam(etiqueta, 0.09f);
            etiqueta.enabled = impreso;   // CONTINUAR y REPETIR ya tienen su cartel detrás
        }
        go.SetActive(false);
        var bc = go.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = new Vector3(0f, 0.03f, 0f);
        bc.size = new Vector3(0.085f, 0.06f, 0.085f);
        var bf = go.AddComponent<BotonFisico>();
        bf.tapa = tapa;
        bf.halo = halo;
        bf.led = led;
        bf.radio = 0.05f;
        bf.etiqueta = etiqueta;
        bf.colorReposo = color;
        go.SetActive(true);
        return bf;
    }

    // Cartelito inclinado detrás de cada botón con lo que significa.
    static GameObject Cartel(Transform raiz, Vector3 boton, string texto, Color borde, out TMP_Text txt)
    {
        const float inclinacion = 40f;
        float s = Mathf.Sin(inclinacion * Mathf.Deg2Rad), c = Mathf.Cos(inclinacion * Mathf.Deg2Rad);
        var g = K.Grupo(raiz, "Cartel", boton + new Vector3(0f, 0.06f, -0.15f), K.MirarDesde(new Vector3(0f, s, c), new Vector3(0f, c, -s)));
        // Atril: placa trasera inclinada que baja hasta el mostrador y una base apoyada en él (el cartel no flota).
        var marcoAtril = K.Escena("Marco", Color.gray);
        K.Caja(g, "Atril", new Vector3(0f, -0.022f, 0.01f), new Vector3(0.235f, 0.122f, 0.012f), marcoAtril);
        var b = K.Grupo(raiz, "BaseCartel", boton + new Vector3(0f, 0f, -0.12f));
        b.gameObject.transform.SetParent(g, true);   // se muestra y se oculta con el cartel
        K.Caja(b, "Base", new Vector3(0f, 0.004f, 0f), new Vector3(0.235f, 0.008f, 0.075f), marcoAtril);
        K.Caja(b, "Pata", new Vector3(0f, 0.03f, -0.035f), new Vector3(0.18f, 0.06f, 0.008f), marcoAtril);
        K.Panel(g, "Fondo", Vector3.zero, 0.225f, 0.075f, AdaptadorEscena.ColorPanel, borde, 0.004f, 0.012f);
        txt = K.Texto(g, texto, new Vector3(0f, 0f, -0.003f), 0.2f, Color.white, 0.21f, 0.065f, TextAlignmentOptions.Center);
        K.AutoTam(txt, 0.12f);
        return g.gameObject;
    }

    // Botón de una máquina con su rótulo impreso en el mostrador (delante). Devuelve el grupo para mostrarlo u ocultarlo.
    static GameObject BotonMaquina(Transform raiz, string nombre, string rotulo, Vector3 pos, Color color, out BotonFisico boton)
    {
        var g = K.Grupo(raiz, "Maquina_" + nombre, pos);
        // Tapa lisa de color (como los botones de Meta) y el nombre de la máquina impreso delante.
        boton = Boton(g, "", Vector3.zero, color, EstiloNanoLab.Icono.Ninguno, rotulo);
        boton.name = "Boton_" + nombre;
        return g.gameObject;
    }

    // Proyector holográfico en el centro del mostrador con el virus grande del paso ANTÍGENO.
    static void Holograma(Transform raiz, RetoVacuna r)
    {
        var g = K.Grupo(raiz, "Holograma", new Vector3(0f, T, -0.22f));
        var cian = new Color(0.45f, 0.9f, 1f);
        K.Pieza(g, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.012f, 0f), new Vector3(0.2f, 0.012f, 0.2f), K.Escena("Oscuro", Color.black));
        K.Pieza(g, "Lente", PrimitiveType.Cylinder, new Vector3(0f, 0.026f, 0f), new Vector3(0.13f, 0.003f, 0.13f), K.Plano(cian));
        K.Pieza(g, "Haz", PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.14f, 0.065f, 0.14f), K.Vidrio(new Color(cian.r, cian.g, cian.b, 0.08f)));
        K.Texto(g, "HOLOGRAMA · tócalo o gíralo con el grip", new Vector3(0f, 0.002f, 0.14f), 0.11f, K.Cian, 0.5f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        r.holograma = VirusVacuna.CrearHolograma(g, new Vector3(0f, 0.3f, 0f), Quaternion.Euler(0f, 180f, 0f));
        r.holograma.transform.localScale = Vector3.one * 0.62f;   // grande para tocarlo, sin tapar las pantallas
        r.holograma.Colocar(Vector3.zero, 0.62f, false);
        var vh = g.gameObject.AddComponent<VirusHolograma>();
        vh.virus = r.holograma;
        vh.radio = 0.22f;
        r.grupoHolograma = g.gameObject;
    }

    // Franja de luz en el borde de cada mesa: la de la mesa del paso actual se enciende (una meta visible a la vez).
    static void Franjas(Transform raiz, RetoVacuna r)
    {
        string[] nombres = { "DISEÑO", "SÍNTESIS", "NANOPARTÍCULAS", "CALIDAD Y FRÍO" };
        r.franjas = new Renderer[4];
        r.rotulosMesa = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            r.franjas[i] = K.Caja(raiz, "Franja_" + nombres[i], new Vector3(Mesas[i], 0.872f, 0.013f), new Vector3(0.72f, 0.014f, 0.008f), K.Plano(Color.white));
            r.rotulosMesa[i] = K.Texto(raiz, nombres[i], new Vector3(Mesas[i], T + 0.002f, -0.03f), 0.18f, K.Gris, 0.7f, 0.04f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        }
    }

    // ------------------------------------------------------------------ piezas

    static Renderer Fantasma(PrimitiveType tipo, Vector3 pos, Vector3 escala) =>
        K.Pieza(null, "Silueta", tipo, pos, escala, K.Vidrio(new Color(0.4f, 0.9f, 1f, 0.35f)));

    static TextMeshPro EtiquetaFrontal(Transform t, string s, Vector3 pos, float tam, float ancho, Color c)
    {
        var x = K.Texto(t, s, pos, tam, c, ancho, 0.05f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        K.AutoTam(x, tam * 0.6f);
        return x;
    }

    static PiezaVacuna Agarrable(Transform raiz, string nombre, string tipo, ZonaEncaje casa, C.Pieza datos, Vector3 centro, Vector3 tam, out Transform t)
    {
        var go = new GameObject(nombre);
        t = go.transform;
        t.SetParent(raiz, false);
        if (casa != null) t.SetPositionAndRotation(casa.anclaje.position, casa.anclaje.rotation);
        return Completar(go, tipo, casa, datos, centro, tam);
    }

    static PiezaVacuna Completar(GameObject go, string tipo, ZonaEncaje casa, C.Pieza datos, Vector3 centro, Vector3 tam)
    {
        A.Hacer(go, tipo, casa, centro, tam);
        var p = go.AddComponent<PiezaVacuna>();
        p.datos = datos;
        return p;
    }

    // ------------------------------------------------------------------ mesa 1 · DISEÑO

    static void MesaDiseno(Transform raiz, RetoVacuna r)
    {
        var g = K.Grupo(raiz, "MesaDiseno", new Vector3(Mesas[0], T, 0f));
        r.grupoDiseno = g.gameObject;
        var oscuro = K.Plano(new Color(0.16f, 0.2f, 0.27f));
        var cromo = K.Escena("Cromo", new Color(0.7f, 0.72f, 0.75f));

        // Molde: riel de 5 huecos, de 5' (izquierda) a 3' (derecha).
        var molde = K.Grupo(g, "Molde", new Vector3(0f, 0f, -0.33f));
        K.Caja(molde, "Base", new Vector3(0f, 0.01f, 0f), new Vector3(0.74f, 0.02f, 0.12f), oscuro);
        K.Caja(molde, "Hebra", new Vector3(0f, 0.021f, -0.045f), new Vector3(0.7f, 0.003f, 0.008f), K.Plano(new Color(1f, 0.6f, 0.15f)));
        K.Texto(molde, "<b>5'</b>", new Vector3(0.355f, 0.022f, 0.02f), 0.22f, K.Amarillo, 0.06f, 0.05f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        K.Texto(molde, "<b>3'</b>", new Vector3(-0.355f, 0.022f, 0.02f), 0.22f, K.Amarillo, 0.06f, 0.05f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        K.Texto(molde, "MOLDE DEL ARNm", new Vector3(0f, 0.022f, 0.075f), 0.15f, K.Cian, 0.5f, 0.04f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        r.huecosMolde = new ZonaEncaje[5];
        for (int i = 0; i < 5; i++)
        {
            float x = 0.26f - i * 0.13f;   // hueco 1 (5') a la izquierda del usuario
            K.Caja(molde, "Marca", new Vector3(x, 0.0205f, 0f), new Vector3(0.115f, 0.002f, 0.075f), K.Plano(new Color(0.3f, 0.4f, 0.5f)));
            K.Texto(molde, (i + 1).ToString(), new Vector3(x, 0.023f, 0.05f), 0.16f, K.Gris, 0.05f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
            r.huecosMolde[i] = A.Zona(molde, "Hueco" + (i + 1), "bloque", new Vector3(x, 0.022f, 0f), Quaternion.identity,
                Fantasma(PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.11f, 0.05f, 0.07f)));
        }

        // Bandeja con los 6 bloques, desordenados.
        var bandeja = K.Grupo(g, "Bandeja", new Vector3(0f, 0f, -0.58f));
        K.Caja(bandeja, "Base", new Vector3(0f, 0.0075f, 0f), new Vector3(0.8f, 0.015f, 0.12f), cromo);
        int[] orden = { 2, 4, 0, 5, 3, 1 };
        r.bloques = new PiezaVacuna[orden.Length];
        for (int i = 0; i < orden.Length; i++)
        {
            var b = C.Bloques[orden[i]];
            var casa = A.Zona(bandeja, "Sitio" + i, "bloque", new Vector3(0.325f - i * 0.13f, 0.016f, 0f), Quaternion.identity,
                Fantasma(PrimitiveType.Cube, new Vector3(0f, 0.025f, 0f), new Vector3(0.11f, 0.05f, 0.07f)));
            var p = Agarrable(raiz, "Bloque_" + b.id, "bloque", casa, b, new Vector3(0f, 0.025f, 0f), new Vector3(0.11f, 0.05f, 0.07f), out var t);
            K.Caja(t, "Cuerpo", new Vector3(0f, 0.025f, 0f), new Vector3(0.106f, 0.048f, 0.066f), K.Plano(b.color));
            K.Caja(t, "Muesca", new Vector3(0.053f, 0.025f, 0f), new Vector3(0.006f, 0.02f, 0.02f), K.Plano(Color.Lerp(b.color, Color.black, 0.35f)));
            var n1 = EtiquetaFrontal(t, $"<b>{b.nombre}</b>", new Vector3(0f, 0.032f, 0.0345f), 0.16f, 0.1f, new Color(0.04f, 0.05f, 0.08f));
            n1.rectTransform.sizeDelta = new Vector2(0.1f, 0.03f);
            var n2 = EtiquetaFrontal(t, b.sub, new Vector3(0f, 0.012f, 0.0345f), 0.11f, 0.1f, new Color(0.1f, 0.12f, 0.16f));
            n2.rectTransform.sizeDelta = new Vector2(0.1f, 0.02f);
            K.Texto(t, $"<b>{b.nombre}</b>", new Vector3(0f, 0.0505f, 0f), 0.15f, new Color(0.04f, 0.05f, 0.08f), 0.1f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
            r.bloques[i] = p;
        }
    }

    // ------------------------------------------------------------------ mesa 2 · SÍNTESIS

    static void MesaSintesis(Transform raiz, RetoVacuna r)
    {
        float cx = Mesas[1];
        var g = K.Grupo(raiz, "MesaSintesis", new Vector3(cx, T, 0f));
        var metal = K.Escena("Cromo", new Color(0.7f, 0.72f, 0.75f));
        var oscuro = K.Plano(new Color(0.16f, 0.2f, 0.27f));
        var vidrio = K.Vidrio(new Color(0.85f, 0.95f, 1f, 0.3f));

        // Gradilla con 5 frascos (su contenido cambia según el paso: síntesis o ensayo de neutralización).
        var gr = K.Grupo(g, "Gradilla", new Vector3(0f, 0f, -0.63f));
        K.Caja(gr, "Base", new Vector3(0f, 0.02f, 0f), new Vector3(0.58f, 0.04f, 0.1f), K.Escena("PlasticoAzul", new Color(0.2f, 0.4f, 0.8f)));
        r.frascos = new PuntoPipeta[5];
        for (int i = 0; i < 5; i++)
        {
            float x = 0.22f - i * 0.11f;
            var f = K.Grupo(gr, "Frasco" + i, new Vector3(x, 0.04f, 0f));
            K.Pieza(f, "Vidrio", PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f), new Vector3(0.045f, 0.045f, 0.045f), vidrio);
            var liq = K.Pieza(f, "Liquido", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.039f, 0.028f, 0.039f), K.Plano(Color.white));
            K.Pieza(f, "Boca", PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.05f, 0.003f, 0.05f), metal);
            var punto = K.Grupo(f, "Boca", new Vector3(0f, 0.09f, 0f)).gameObject.AddComponent<PuntoPipeta>();
            punto.radio = 0.05f;
            punto.liquido = liq;
            punto.rotulo = K.Texto(gr, "", new Vector3(x, 0.022f, 0.051f), 0.13f, Color.white, 0.105f, 0.04f, TextAlignmentOptions.Center, false, false, Quaternion.Euler(0f, 180f, 0f));
            K.AutoTam(punto.rotulo, 0.08f);
            r.frascos[i] = punto;
        }

        // Pipeta en su soporte (a la izquierda).
        var soporte = K.Grupo(g, "SoportePipeta", new Vector3(-0.33f, 0f, -0.42f));
        K.Pieza(soporte, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.005f, 0f), new Vector3(0.08f, 0.005f, 0.08f), metal);
        K.Pieza(soporte, "Aro", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.045f, 0.004f, 0.045f), oscuro);
        K.Texto(soporte, "PIPETA", new Vector3(0f, 0.002f, 0.06f), 0.14f, K.Cian, 0.15f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        var casaPipeta = A.Zona(soporte, "Sitio", "pipeta", new Vector3(0f, 0.012f, 0f), Quaternion.identity,
            Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(0.035f, 0.11f, 0.035f)));
        var pgo = new GameObject("Pipeta");
        pgo.transform.SetParent(raiz, false);
        pgo.transform.SetPositionAndRotation(casaPipeta.anclaje.position, casaPipeta.anclaje.rotation);
        var pt = pgo.transform;
        K.Pieza(pt, "Punta", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.008f, 0.03f, 0.008f), K.Vidrio(new Color(0.95f, 0.95f, 0.85f, 0.5f)));
        var liqP = K.Pieza(pt, "LiquidoPunta", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.006f, 0.016f, 0.006f), K.Plano(Color.white));
        K.Pieza(pt, "Cuerpo", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f), new Vector3(0.028f, 0.07f, 0.028f), K.Escena("PlasticoBlanco", new Color(0.95f, 0.95f, 0.95f)));
        K.Pieza(pt, "Anillo", PrimitiveType.Cylinder, new Vector3(0f, 0.065f, 0f), new Vector3(0.02f, 0.006f, 0.02f), K.Plano(new Color(0.2f, 0.5f, 0.9f)));
        K.Pieza(pt, "Embolo", PrimitiveType.Cylinder, new Vector3(0f, 0.215f, 0f), new Vector3(0.01f, 0.018f, 0.01f), metal);
        K.Pieza(pt, "Pulsador", PrimitiveType.Cylinder, new Vector3(0f, 0.235f, 0f), new Vector3(0.02f, 0.004f, 0.02f), K.Plano(new Color(0.2f, 0.5f, 0.9f)));
        var etiq = K.Texto(pt, "vacía", new Vector3(0f, 0.13f, 0.0145f), 0.1f, K.Gris, 0.1f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 90f));
        K.AutoTam(etiq, 0.06f);
        A.Hacer(pgo, "pipeta", casaPipeta, new Vector3(0f, 0.12f, 0f), new Vector3(0.035f, 0.24f, 0.035f), 0.03f);
        var punta = K.Grupo(pt, "Extremo", new Vector3(0f, 0.002f, 0f));
        var pip = pgo.AddComponent<PipetaVacuna>();
        pip.punta = punta;
        pip.liquido = liqP;
        pip.etiqueta = etiq;
        pip.Vaciar();
        r.pipeta = pip;

        // Termobloque (37 °C) con su botón INCUBAR.
        var termo = K.Grupo(g, "Termobloque", new Vector3(-0.05f, 0f, -0.42f));
        K.Caja(termo, "Cuerpo", new Vector3(0f, 0.035f, 0f), new Vector3(0.2f, 0.07f, 0.15f), metal, true);
        K.Caja(termo, "Bloque", new Vector3(0f, 0.071f, 0f), new Vector3(0.14f, 0.004f, 0.1f), K.Plano(new Color(0.55f, 0.58f, 0.62f)));
        K.Pieza(termo, "Pocillo", PrimitiveType.Cylinder, new Vector3(0f, 0.073f, 0f), new Vector3(0.042f, 0.001f, 0.042f), K.Plano(new Color(0.1f, 0.1f, 0.12f)));
        K.Caja(termo, "Pantalla", new Vector3(0f, 0.035f, 0.0755f), new Vector3(0.15f, 0.045f, 0.002f), K.Plano(new Color(0.03f, 0.08f, 0.12f)));
        r.pantallaTermo = K.Texto(termo, "TERMOBLOQUE\n37 °C", new Vector3(0f, 0.035f, 0.078f), 0.12f, K.Cian, 0.14f, 0.04f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        K.AutoTam(r.pantallaTermo, 0.07f);
        r.huecoTermo = A.Zona(termo, "Pocillo", "tubo", new Vector3(0f, 0.04f, 0f), Quaternion.identity,
            Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.036f, 0.05f, 0.036f)));
        r.grupoIncubarTubo = BotonMaquina(raiz, "IncubarTubo", "INCUBAR 37 °C", new Vector3(cx - 0.05f, T, -0.18f), new Color(0.85f, 0.35f, 0.15f), out r.incubarTubo);

        // Tubo de reacción en su gradilla pequeña.
        var porta = K.Grupo(g, "PortaTubo", new Vector3(0.24f, 0f, -0.42f));
        K.Caja(porta, "Base", new Vector3(0f, 0.015f, 0f), new Vector3(0.07f, 0.03f, 0.07f), K.Escena("PlasticoAzul", new Color(0.2f, 0.4f, 0.8f)));
        K.Texto(porta, "TUBO DE\nREACCIÓN", new Vector3(0f, 0.002f, 0.07f), 0.12f, K.Cian, 0.15f, 0.05f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        var casaTubo = A.Zona(porta, "Sitio", "tubo", new Vector3(0f, 0.02f, 0f), Quaternion.identity,
            Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.036f, 0.05f, 0.036f)));
        var tubo = Agarrable(raiz, "TuboReaccion", "tubo", casaTubo, null, new Vector3(0f, 0.05f, 0f), new Vector3(0.04f, 0.1f, 0.04f), out var tt);
        K.Pieza(tt, "Vidrio", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.034f, 0.05f, 0.034f), vidrio);
        K.Pieza(tt, "Fondo", PrimitiveType.Sphere, new Vector3(0f, 0.002f, 0f), new Vector3(0.034f, 0.01f, 0.034f), vidrio);
        K.Pieza(tt, "Tapa", PrimitiveType.Cylinder, new Vector3(0.026f, 0.092f, 0f), new Vector3(0.036f, 0.003f, 0.036f), K.Plano(new Color(0.95f, 0.6f, 0.2f)), Quaternion.Euler(0f, 0f, 80f));   // abierta
        var liqT = K.Pieza(tt, "Liquido", PrimitiveType.Cylinder, new Vector3(0f, 0.005f, 0f), new Vector3(0.028f, 0.001f, 0.028f), K.Plano(Color.white));
        tubo.liquido = liqT;
        tubo.baseLiquido = 0.005f;
        tubo.altoLiquido = 0.075f;
        tubo.etiqueta = EtiquetaFrontal(tt, "VACÍO", new Vector3(0f, 0.065f, 0.0185f), 0.1f, 0.06f, Color.white);
        var pTubo = K.Grupo(tt, "Boca", new Vector3(0f, 0.11f, 0f)).gameObject.AddComponent<PuntoPipeta>();
        pTubo.nombre = "el tubo de reacción";
        pTubo.radio = 0.06f;
        r.tubo = tubo;
        r.puntoTubo = pTubo;
    }

    // ------------------------------------------------------------------ mesa 3 · NANOPARTÍCULAS

    static void MesaNanoparticulas(Transform raiz, RetoVacuna r)
    {
        float cx = Mesas[2];
        var g = K.Grupo(raiz, "MesaNanoparticulas", new Vector3(cx, T, 0f));
        var metal = K.Escena("Cromo", new Color(0.7f, 0.72f, 0.75f));
        var blanco = K.Escena("PlasticoBlanco", new Color(0.95f, 0.95f, 0.95f));
        var vidrio = K.Vidrio(new Color(0.85f, 0.95f, 1f, 0.3f));
        var hueco = K.Plano(new Color(0.1f, 0.1f, 0.12f));

        // Mezclador microfluídico: entrada de fase acuosa (tubo) + 4 entradas de lípidos; chip de vidrio encima.
        var mez = K.Grupo(g, "Mezclador", new Vector3(0f, 0f, -0.5f));
        K.Caja(mez, "Cuerpo", new Vector3(0f, 0.06f, 0f), new Vector3(0.6f, 0.12f, 0.24f), blanco, true);
        K.Caja(mez, "Frente", new Vector3(0f, 0.06f, 0.1205f), new Vector3(0.5f, 0.07f, 0.002f), K.Plano(new Color(0.03f, 0.08f, 0.12f)));
        r.pantallaMezclador = K.Texto(mez, "", new Vector3(0f, 0.06f, 0.123f), 0.13f, K.Cian, 0.48f, 0.065f, TextAlignmentOptions.Center, false, false, Quaternion.Euler(0f, 180f, 0f));
        K.AutoTam(r.pantallaMezclador, 0.07f);
        // Chip con el canal en Y (dos entradas que se juntan).
        var chip = K.Grupo(mez, "Chip", new Vector3(0f, 0.121f, -0.075f));
        K.Caja(chip, "Vidrio", Vector3.zero, new Vector3(0.36f, 0.004f, 0.07f), vidrio);
        var canal = K.Plano(new Color(0.4f, 0.9f, 1f));
        K.Pieza(chip, "CanalA", PrimitiveType.Cube, new Vector3(0.12f, 0.003f, 0.012f), new Vector3(0.12f, 0.002f, 0.006f), canal, Quaternion.Euler(0f, 12f, 0f));
        K.Pieza(chip, "CanalB", PrimitiveType.Cube, new Vector3(0.12f, 0.003f, -0.012f), new Vector3(0.12f, 0.002f, 0.006f), canal, Quaternion.Euler(0f, -12f, 0f));
        r.canalMezcla = K.Caja(chip, "CanalMezcla", new Vector3(-0.07f, 0.003f, 0f), new Vector3(0.26f, 0.002f, 0.008f), canal);
        K.Texto(chip, "chip microfluídico", new Vector3(-0.05f, 0.004f, 0.03f), 0.09f, K.Gris, 0.2f, 0.02f, TextAlignmentOptions.Center, false, false, Quaternion.Euler(90f, 180f, 0f));

        // Entrada acuosa (tubo con el ARNm), a la izquierda.
        K.Pieza(mez, "EntradaAcuosa", PrimitiveType.Cylinder, new Vector3(0.24f, 0.121f, 0.04f), new Vector3(0.046f, 0.002f, 0.046f), hueco);
        K.Texto(mez, "ARNm\n(fase acuosa)", new Vector3(0.24f, 0.123f, 0.095f), 0.09f, K.Amarillo, 0.12f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        r.huecoMezclaTubo = A.Zona(mez, "EntradaTubo", "tubo", new Vector3(0.24f, 0.09f, 0.04f), Quaternion.identity,
            Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.036f, 0.05f, 0.036f)));
        // 4 entradas de lípidos.
        r.huecosLipido = new ZonaEncaje[4];
        for (int i = 0; i < 4; i++)
        {
            float x = 0.1f - i * 0.1f;
            K.Pieza(mez, "EntradaLipido", PrimitiveType.Cylinder, new Vector3(x, 0.121f, 0.04f), new Vector3(0.044f, 0.002f, 0.044f), hueco);
            r.huecosLipido[i] = A.Zona(mez, "EntradaLipido" + (i + 1), "lipido", new Vector3(x, 0.095f, 0.04f), Quaternion.identity,
                Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.034f, 0.04f, 0.034f)));
        }
        K.Texto(mez, "LÍPIDOS (en etanol)", new Vector3(-0.05f, 0.123f, 0.095f), 0.09f, K.Cian, 0.4f, 0.02f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));

        // Salida: bandeja donde aparece el vial de la vacuna.
        var salida = K.Grupo(g, "Salida", new Vector3(-0.36f, 0f, -0.28f));
        K.Caja(salida, "Bandeja", new Vector3(0f, 0.005f, 0f), new Vector3(0.09f, 0.01f, 0.09f), metal);
        K.Texto(salida, "SALIDA", new Vector3(0f, 0.002f, 0.07f), 0.12f, K.Verde, 0.12f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        r.salidaVial = A.Zona(salida, "Sitio", "vacuna", new Vector3(0f, 0.011f, 0f), Quaternion.identity,
            Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.036f, 0.035f, 0.036f)));

        r.grupoMezclar = BotonMaquina(raiz, "Mezclar", "MEZCLAR", new Vector3(cx + 0.36f, T, -0.3f), new Color(0.15f, 0.55f, 0.8f), out r.mezclar);

        // Gradilla de cartuchos de lípidos (delante, desordenados).
        var gr = K.Grupo(g, "Cartuchos", new Vector3(0f, 0f, -0.15f));
        r.grupoCartuchos = gr.gameObject;
        K.Caja(gr, "Base", new Vector3(0f, 0.01f, 0f), new Vector3(0.56f, 0.02f, 0.07f), K.Escena("PlasticoAzul", new Color(0.2f, 0.4f, 0.8f)));
        int[] orden = { 2, 4, 0, 3, 1 };
        r.cartuchos = new PiezaVacuna[orden.Length];
        for (int i = 0; i < orden.Length; i++)
        {
            var l = C.Lipidos[orden[i]];
            var casa = A.Zona(gr, "Sitio" + i, "lipido", new Vector3(0.22f - i * 0.11f, 0.02f, 0f), Quaternion.identity,
                Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.04f, 0f), new Vector3(0.034f, 0.04f, 0.034f)));
            var p = Agarrable(raiz, "Cartucho_" + l.id, "lipido", casa, l, new Vector3(0f, 0.045f, 0f), new Vector3(0.04f, 0.09f, 0.04f), out var t);
            K.Pieza(t, "Cuerpo", PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.032f, 0.035f, 0.032f), vidrio);
            K.Pieza(t, "Contenido", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.027f, 0.026f, 0.027f), K.Plano(l.color));
            K.Pieza(t, "Tapon", PrimitiveType.Cylinder, new Vector3(0f, 0.077f, 0f), new Vector3(0.036f, 0.008f, 0.036f), K.Plano(Color.Lerp(l.color, Color.black, 0.3f)));
            var e = EtiquetaFrontal(t, $"<b>{l.nombre}</b>", new Vector3(0f, 0.035f, 0.019f), 0.1f, 0.1f, Color.white);
            e.rectTransform.sizeDelta = new Vector2(0.1f, 0.04f);
            e.outlineWidth = 0.25f;
            e.outlineColor = new Color32(0, 0, 0, 220);
            r.cartuchos[i] = p;
        }

        // Vial de la vacuna (aparece en la salida al mezclar).
        var vial = Agarrable(raiz, "VialVacuna", "vacuna", r.salidaVial, null, new Vector3(0f, 0.035f, 0f), new Vector3(0.04f, 0.075f, 0.04f), out var vt);
        K.Pieza(vt, "Vidrio", PrimitiveType.Cylinder, new Vector3(0f, 0.03f, 0f), new Vector3(0.034f, 0.03f, 0.034f), vidrio);
        K.Pieza(vt, "Vacuna", PrimitiveType.Cylinder, new Vector3(0f, 0.022f, 0f), new Vector3(0.029f, 0.02f, 0.029f), K.Plano(new Color(0.96f, 0.95f, 0.9f)));
        K.Pieza(vt, "Cuello", PrimitiveType.Cylinder, new Vector3(0f, 0.064f, 0f), new Vector3(0.018f, 0.006f, 0.018f), vidrio);
        K.Pieza(vt, "Tapa", PrimitiveType.Cylinder, new Vector3(0f, 0.073f, 0f), new Vector3(0.03f, 0.005f, 0.03f), K.Plano(new Color(0.55f, 0.35f, 0.95f)));
        var ve = EtiquetaFrontal(vt, "<b>VACUNA ARNm</b>\nLOTE 001", new Vector3(0f, 0.03f, 0.0175f), 0.07f, 0.06f, new Color(0.05f, 0.05f, 0.1f));
        ve.rectTransform.sizeDelta = new Vector2(0.06f, 0.035f);
        K.Caja(vt, "Etiqueta", new Vector3(0f, 0.03f, 0.0172f), new Vector3(0.03f, 0.03f, 0.001f), K.Plano(Color.white));
        r.vial = vial;
    }

    // ------------------------------------------------------------------ mesa 4 · CALIDAD Y FRÍO

    static void MesaCalidad(Transform raiz, RetoVacuna r)
    {
        float cx = Mesas[3];
        var g = K.Grupo(raiz, "MesaCalidad", new Vector3(cx, T, 0f));
        var blanco = K.Escena("PlasticoBlanco", new Color(0.95f, 0.95f, 0.95f));
        var pantalla = K.Plano(new Color(0.03f, 0.08f, 0.12f));

        // Analizador (tamaño de partícula, encapsulación, integridad, esterilidad).
        var an = K.Grupo(g, "Analizador", new Vector3(0.22f, 0f, -0.5f));
        K.Caja(an, "Cuerpo", new Vector3(0f, 0.08f, 0f), new Vector3(0.24f, 0.16f, 0.22f), blanco, true);
        K.Caja(an, "Pantalla", new Vector3(0f, 0.1f, 0.1105f), new Vector3(0.2f, 0.08f, 0.002f), pantalla);
        r.pantallaAnalizador = K.Texto(an, "ANALIZADOR\nlisto", new Vector3(0f, 0.1f, 0.113f), 0.11f, K.Cian, 0.19f, 0.075f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        K.AutoTam(r.pantallaAnalizador, 0.06f);
        K.Pieza(an, "Puerto", PrimitiveType.Cylinder, new Vector3(0f, 0.161f, 0.03f), new Vector3(0.05f, 0.002f, 0.05f), K.Plano(new Color(0.1f, 0.1f, 0.12f)));
        K.Texto(an, "VIAL AQUÍ", new Vector3(0f, 0.163f, 0.085f), 0.09f, K.Cian, 0.15f, 0.02f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        r.huecoAnalizador = A.Zona(an, "Puerto", "vacuna", new Vector3(0f, 0.162f, 0.03f), Quaternion.identity,
            Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.036f, 0.035f, 0.036f)));

        // Ultracongelador, nevera y mesa, cada uno con un sitio para el vial. El ultracongelador y la nevera son armarios
        // con puerta (AlmacenFrio): se abre al acercar el vial, el vial entra y la puerta se cierra. Van juntos en el
        // grupo "Almacenes" (la guía señala el grupo, no uno concreto, para no dar la respuesta).
        var grupo = K.Grupo(g, "Almacenes", new Vector3(-0.21f, 0f, -0.48f));
        r.huecosAlmacen = new ZonaEncaje[C.Almacenes.Length];
        for (int i = 0; i < C.Almacenes.Length; i++)
        {
            var a = C.Almacenes[i];
            var u = K.Grupo(grupo, "Almacen_" + a.id, new Vector3(0.17f - i * 0.17f, 0f, 0f));
            bool armario = a.id != "mesa";
            float alto = armario ? 0.2f : 0.02f;
            var cuerpo = a.id == "ultra" ? K.Plano(new Color(0.2f, 0.32f, 0.5f)) : a.id == "nevera" ? blanco : K.Escena("Madera", new Color(0.6f, 0.45f, 0.3f));
            K.Caja(u, "Display", new Vector3(0f, alto + 0.045f, 0.06f), new Vector3(0.14f, 0.07f, 0.004f), pantalla);
            var txt = K.Texto(u, $"<b>{a.nombre}</b>\n<size=140%>{a.sub}</size>", new Vector3(0f, alto + 0.045f, 0.057f), 0.09f, a.color, 0.13f, 0.065f, TextAlignmentOptions.Center, false, false, Quaternion.Euler(0f, 180f, 0f));
            K.AutoTam(txt, 0.05f);
            var fantasma = Fantasma(PrimitiveType.Cylinder, new Vector3(0f, 0.035f, 0f), new Vector3(0.036f, 0.035f, 0.036f));
            if (!armario)
            {
                K.Caja(u, "Cuerpo", new Vector3(0f, alto / 2f, 0f), new Vector3(0.15f, alto, 0.2f), cuerpo, true);
                K.Pieza(u, "Sitio", PrimitiveType.Cylinder, new Vector3(0f, alto + 0.001f, -0.03f), new Vector3(0.05f, 0.001f, 0.05f), K.Plano(Color.Lerp(a.color, Color.black, 0.5f)));
                r.huecosAlmacen[i] = A.Zona(u, "Sitio", "vacuna", new Vector3(0f, alto + 0.002f, -0.03f), Quaternion.identity, fantasma);
                continue;
            }
            r.huecosAlmacen[i] = Armario(u, a, cuerpo, fantasma);
        }
    }

    // Armario frigorífico hueco (frente abierto hacia el usuario, +z) con puerta de ventana y el sitio del vial dentro.
    static ZonaEncaje Armario(Transform u, ContenidoVacuna.Pieza a, Material cuerpo, Renderer fantasma)
    {
        const float w = 0.15f, h = 0.2f, d = 0.2f, e = 0.01f;
        var interior = K.Plano(Color.Lerp(a.color, Color.white, 0.75f));
        K.Caja(u, "Suelo", new Vector3(0f, e / 2f, 0f), new Vector3(w, e, d), cuerpo, true);
        K.Caja(u, "Techo", new Vector3(0f, h - e / 2f, 0f), new Vector3(w, e, d), cuerpo, true);
        K.Caja(u, "Fondo", new Vector3(0f, h / 2f, -d / 2f + e / 2f), new Vector3(w, h, e), cuerpo, true);
        K.Caja(u, "LadoI", new Vector3(-w / 2f + e / 2f, h / 2f, 0f), new Vector3(e, h, d), cuerpo, true);
        K.Caja(u, "LadoD", new Vector3(w / 2f - e / 2f, h / 2f, 0f), new Vector3(e, h, d), cuerpo, true);
        // Interior claro (escarcha) y una luz arriba, para que el vial se vea dentro.
        K.Caja(u, "Interior", new Vector3(0f, h / 2f, -d / 2f + e + 0.001f), new Vector3(w - 2f * e, h - 2f * e, 0.002f), interior);
        K.Caja(u, "Luz", new Vector3(0f, h - e - 0.003f, 0.01f), new Vector3(w - 0.04f, 0.004f, 0.03f), K.Plano(new Color(0.9f, 0.97f, 1f)));
        K.Pieza(u, "Sitio", PrimitiveType.Cylinder, new Vector3(0f, e + 0.0005f, -0.015f), new Vector3(0.05f, 0.001f, 0.05f), K.Plano(Color.Lerp(a.color, Color.black, 0.5f)));

        // Puerta con bisagra a la izquierda del armario (-x): se abre hacia el usuario.
        var bisagra = K.Grupo(u, "Bisagra", new Vector3(-w / 2f, h / 2f, d / 2f + 0.004f));
        var marco = K.Plano(Color.Lerp(a.color, Color.white, 0.4f));
        float vAlto = h - 0.09f, vY = -h / 2f + 0.03f + vAlto / 2f;   // ventana: de 3 cm sobre el suelo hasta 6 cm bajo el techo
        K.Caja(bisagra, "Arriba", new Vector3(w / 2f, h / 2f - 0.03f, 0f), new Vector3(w, 0.06f, 0.008f), marco);
        K.Caja(bisagra, "Abajo", new Vector3(w / 2f, -h / 2f + 0.015f, 0f), new Vector3(w, 0.03f, 0.008f), marco);
        K.Caja(bisagra, "MarcoI", new Vector3(0.01f, vY, 0f), new Vector3(0.02f, vAlto, 0.008f), marco);
        K.Caja(bisagra, "MarcoD", new Vector3(w - 0.01f, vY, 0f), new Vector3(0.02f, vAlto, 0.008f), marco);
        K.Caja(bisagra, "Ventana", new Vector3(w / 2f, vY, 0f), new Vector3(w - 0.04f, vAlto, 0.003f), K.Vidrio(new Color(0.8f, 0.92f, 1f, 0.22f)));
        K.Caja(bisagra, "Tirador", new Vector3(w - 0.012f, 0.01f, 0.008f), new Vector3(0.008f, 0.07f, 0.008f), K.Escena("Cromo", Color.gray));

        // La zona está en la boca (al soltar el vial ahí, encaja) y su anclaje se desliza hacia el fondo.
        var zona = A.Zona(u, "Boca", "vacuna", new Vector3(0f, e + 0.001f, 0.06f), Quaternion.identity, fantasma);
        var ancla = new GameObject("Ancla").transform;
        ancla.SetParent(zona.transform, false);
        zona.anclaje = ancla;
        var af = u.gameObject.AddComponent<AlmacenFrio>();
        af.puerta = bisagra;
        af.ancla = ancla;
        af.zona = zona;
        af.dentro = new Vector3(0f, 0f, -0.075f);
        return zona;
    }

    // ------------------------------------------------------------------ placa de cultivo (paso PRECLÍNICA, en la mesa de diseño)

    static void PlacaCultivo(Transform raiz, RetoVacuna r)
    {
        var g = K.Grupo(raiz, "PlacaCultivo", new Vector3(Mesas[0], T, -0.36f));
        r.grupoPlaca = g.gameObject;
        K.Caja(g, "Cuerpo", new Vector3(0f, 0.0125f, 0f), new Vector3(0.3f, 0.025f, 0.17f), K.Escena("PlasticoBlanco", new Color(0.95f, 0.95f, 0.95f)));
        K.Caja(g, "Tapa", new Vector3(0f, 0.026f, 0f), new Vector3(0.3f, 0.002f, 0.17f), K.Vidrio(new Color(0.9f, 0.95f, 1f, 0.15f)));
        string[] filas = { "VACUNADO", "PLACEBO", "CONTROL" };
        r.pocillos = new Renderer[PlacaNeutralizacion.Filas * PlacaNeutralizacion.Columnas];
        for (int f = 0; f < PlacaNeutralizacion.Filas; f++)
        {
            K.Texto(g, filas[f], new Vector3(0.21f, 0.026f, -0.045f + f * 0.045f), 0.09f, K.Gris, 0.11f, 0.03f, TextAlignmentOptions.Left, true, false, Quaternion.Euler(90f, 180f, 0f));
            for (int c = 0; c < PlacaNeutralizacion.Columnas; c++)
                r.pocillos[f * PlacaNeutralizacion.Columnas + c] = K.Pieza(g, "Pocillo", PrimitiveType.Cylinder,
                    new Vector3(0.122f - c * 0.035f, 0.0255f, -0.045f + f * 0.045f), new Vector3(0.028f, 0.001f, 0.028f), K.Plano(Color.white));
        }
        K.Texto(g, "PLACA DE CULTIVO · ENSAYO DE NEUTRALIZACIÓN", new Vector3(0f, 0.002f, 0.11f), 0.12f, K.Cian, 0.6f, 0.03f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        var p = K.Grupo(g, "Boca", new Vector3(0f, 0.04f, 0f)).gameObject.AddComponent<PuntoPipeta>();
        p.nombre = "la placa";
        p.radio = 0.13f;
        r.puntoPlaca = p;
        r.grupoIncubarPlaca = BotonMaquina(raiz, "IncubarPlaca", "INCUBAR 1 h · 37 °C", new Vector3(Mesas[0] - 0.3f, T, -0.17f), new Color(0.85f, 0.35f, 0.15f), out r.incubarPlaca);
    }

    // ------------------------------------------------------------------ tableros y señales

    static Transform Tablero(Transform raiz, string nombre, Vector3 pos, float ancho, float alto, Color borde)
    {
        var haciaUsuario = new Vector3(-pos.x, 0f, 1.4f - pos.z);
        var t = K.Grupo(raiz, nombre, pos, K.MirarDesde(haciaUsuario, Vector3.up));
        var marco = K.Escena("Marco", Color.gray);
        foreach (var x in new[] { -ancho / 2f + 0.04f, ancho / 2f - 0.04f })
            K.Solido(K.Caja(t, "Pata", new Vector3(x, (0.45f + alto) / 2f, 0.03f), new Vector3(0.05f, 0.45f + alto, 0.05f), marco, true));
        K.Caja(t, "Respaldo", new Vector3(0f, 0.45f + alto / 2f, 0.018f), new Vector3(ancho + 0.04f, alto + 0.04f, 0.02f), K.Escena("Oscuro", Color.black), true);
        K.Panel(t, "Fondo", new Vector3(0f, 0.45f + alto / 2f, 0f), ancho, alto, AdaptadorEscena.ColorPanel, borde, 0.008f, 0.04f);
        return t;
    }

    static void Guia(Transform raiz)
    {
        const float ancho = 1.2f, alto = 2.0f;
        var t = Tablero(raiz, "TableroGuia", new Vector3(2.15f, 0f, -0.25f), ancho, alto, Violeta);
        float top = 0.45f + alto - 0.05f;
        var tt = K.Texto(t, C.GuiaTitulo, new Vector3(0f, top, -0.004f), 0.3f, Violeta, ancho - 0.08f, 0.12f, TextAlignmentOptions.Top, true, true);
        K.AutoTam(tt, 0.2f);
        Color[] colores = { K.Cian, K.Cian, K.Cian, K.Cian, K.Cian, K.Amarillo, K.Amarillo, K.Verde };
        string[] grupo = { "FABRICACIÓN (lo que haces en este mostrador)", null, null, null, null, "PRUEBAS", null, null };
        float y = top - 0.15f;
        for (int i = 0; i < C.Guia.Length; i++)
        {
            if (grupo[i] != null)
            {
                K.Texto(t, grupo[i], new Vector3(0.05f, y, -0.004f), 0.17f, colores[i], ancho - 0.2f, 0.03f, TextAlignmentOptions.TopLeft, true, true);
                y -= 0.035f;
            }
            K.Pieza(t, "Numero", PrimitiveType.Cylinder, new Vector3(-ancho / 2f + 0.08f, y - 0.04f, -0.004f), new Vector3(0.075f, 0.001f, 0.075f), K.Plano(colores[i]), Quaternion.Euler(90f, 0f, 0f));
            K.Texto(t, (i + 1).ToString(), new Vector3(-ancho / 2f + 0.08f, y - 0.04f, -0.008f), 0.32f, new Color(0.05f, 0.08f, 0.12f), 0.07f, 0.06f, TextAlignmentOptions.Center, true);
            var paso = K.Texto(t, $"<b><color=#{ColorUtility.ToHtmlStringRGB(colores[i])}>{C.Guia[i].titulo}</color></b>\n{C.Guia[i].texto}",
                               new Vector3(0.06f, y, -0.004f), 0.2f, Color.white, ancho - 0.2f, 0.17f, TextAlignmentOptions.TopLeft, false, true);
            K.AutoTam(paso, 0.14f);
            y -= 0.18f;
        }
        var pie = K.Texto(t, C.GuiaPie, new Vector3(0f, y - 0.01f, -0.004f), 0.17f, K.Gris, ancho - 0.1f, 0.14f, TextAlignmentOptions.TopLeft, false, true);
        pie.fontStyle = FontStyles.Italic;
        K.AutoTam(pie, 0.12f);
    }

    static void Conceptos(Transform raiz)
    {
        const float ancho = 1.2f, alto = 2.0f;
        var t = Tablero(raiz, "TableroConceptos", new Vector3(-2.15f, 0f, -0.25f), ancho, alto, AdaptadorEscena.BordePanel);
        float top = 0.45f + alto - 0.05f;
        K.Texto(t, C.ConceptosTitulo, new Vector3(0f, top, -0.004f), 0.34f, K.Cian, ancho - 0.08f, 0.08f, TextAlignmentOptions.Top, true, true);
        var sb = new System.Text.StringBuilder();
        foreach (var (termino, def) in C.Conceptos) sb.AppendLine($"<b><color=#8FD8FF>{termino}:</color></b> {def}\n<size=35%> </size>");
        var cuerpo = K.Texto(t, sb.ToString(), new Vector3(0f, top - 0.11f, -0.004f), 0.22f, Color.white, ancho - 0.1f, 1.55f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(cuerpo, 0.15f);
        var aviso = K.Texto(t, C.Aviso, new Vector3(0f, 0.57f, -0.004f), 0.16f, K.Gris, ancho - 0.1f, 0.1f, TextAlignmentOptions.Center);
        aviso.fontStyle = FontStyles.Italic;
    }

    static void Senales(Transform raiz)
    {
        // Franja y rótulo en el suelo (forma + texto, como las demás estaciones).
        K.Caja(raiz, "FranjaSuelo", new Vector3(0f, 0.003f, 0.85f), new Vector3(2.6f, 0.004f, 0.06f), K.Plano(Violeta));
        var r = K.Texto(raiz, $"LABORATORIO DE VACUNAS  ·  {C.Codigo}", new Vector3(0f, 0.005f, 1.12f), 0.7f, Violeta, 3.2f, 0.2f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(90f, 180f, 0f));
        r.name = "RotuloSuelo";

        // Cartel colgado, visible desde la entrada del centro.
        var s = K.Grupo(raiz, "CartelColgado", new Vector3(0f, 3.05f, -0.77f), Quaternion.Euler(0f, 180f, 0f));
        K.Panel(s, "Fondo", Vector3.zero, 2.4f, 0.44f, AdaptadorEscena.ColorPanel, Violeta, 0.012f, 0.06f);
        K.Texto(s, "<b>LABORATORIO DE VACUNAS</b>\n<size=55%>" + C.Codigo + " · " + C.Titulo + " de ARNm · ¡empieza aquí!</size>", new Vector3(0f, 0f, -0.005f), 0.55f, Color.white, 2.3f, 0.4f, TextAlignmentOptions.Center);
        foreach (var x in new[] { -1.0f, 1.0f })
            K.Caja(s, "Cable", new Vector3(x, 0.72f, 0f), new Vector3(0.012f, 1.0f, 0.012f), K.Escena("Cromo", Color.gray));
    }
}
