using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// Prepara al empezar las MISIONES 1 y 2 con el mobiliario del Centro de Experimentación (sin volver a montar la escena
// ni hornear la luz). Lo llama MontajeVacuna.Asegurar:
//   AntesDeLaVacuna: oculta la cámara experimental (su sitio, el centro de la sala, lo ocupa el laboratorio de vacunas),
//     la torre de objetivos que colgaba encima y la baliza antigua.
//   DespuesDeLaVacuna: vuelve a poner la cámara en un hueco libre, convierte las dos ranuras "próximamente" del banco
//     en las MUESTRAS B (placa con moho) y C (hisopado del brote), rotula la A, monta el panel CONCLUSIONES junto a los
//     microscopios y conecta todo con RetoManager. Las estaciones, sus carteles con PISTA, la terminal, la prensa, el
//     archivo y la guía de flechas siguen activos (en la versión anterior se ocultaban).
public static class MontajeMisiones
{
    static readonly string[] Ocultar = { "TorreObjetivo", "Baliza" };

    public static void AntesDeLaVacuna(RetoManager rm)
    {
        var centro = Centro(rm);
        foreach (var t in centro.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            if (System.Array.IndexOf(Ocultar, t.name) >= 0 || t.name == "7_CamaraExperimental") t.gameObject.SetActive(false);
            if (t.name == "Estaciones" && !t.gameObject.activeSelf) t.gameObject.SetActive(true);
        }
    }

    public static void DespuesDeLaVacuna(RetoManager rm)
    {
        var centro = Centro(rm);
        float suelo = rm.zona.min.y;
        Physics.SyncTransforms();
        var camara = centro.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "7_CamaraExperimental");
        if (camara != null) ReubicarCamara(camara, suelo);
        Physics.SyncTransforms();

        var banco = centro.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "1_BancoMuestras");
        if (banco != null) MuestrasBanco(rm, banco);

        // Incubadora, cabina, autoclave, ultracongelador e incubadora CO2: consolas y pasos de las misiones.
        try { EquiposCentro.Montar(centro, rm, banco); }
        catch (System.Exception e) { Debug.LogWarning("[Equipos] No se pudieron montar las consolas: " + e); }
        // Evento sorpresa de bioseguridad (misión 2): kit de derrames con el pulverizador y la iluminación de alarma.
        try { BiohazardEvent.Montar(centro); }
        catch (System.Exception e) { Debug.LogWarning("[Biohazard] No se pudo montar el evento: " + e); }

        var micro = centro.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "3_Microscopia");
        bandejaMicro = micro != null ? micro.Cast<Transform>().FirstOrDefault(t => t.name == "Bandeja") : null;
        PrepararComparacion(rm);
        var panel = CrearConclusiones(centro, micro, suelo);

        // Tótem de bienvenida de la entrada: presenta las 3 misiones.
        foreach (var tx in centro.GetComponentsInChildren<TMP_Text>(true))
            if (tx.text != null && tx.text.Contains("perdiste tus"))
                tx.text = "<color=#FFD25A><b>BAYMAX:</b></color> perdiste tus <b>recuerdos científicos</b>. Hay un brote en la ciudad: completa las 3 misiones de este centro para recuperarlos.\n\n" +
                          $"<b>1 · {Misiones.Titulo1}</b> Compara 3 muestras al microscopio.\n" +
                          $"<b>2 · {Misiones.Titulo2}</b> Prueba qué detiene a la bacteria.\n" +
                          $"<b>3 · {ContenidoVacuna.Titulo}</b> En el LABORATORIO DE VACUNAS (se desbloquea con la 2).\n\n" +
                          "Los equipos con número 9-13 (incubadoras, cabina, autoclave, ultracongelador) son opcionales: púlsalos para ver qué hacen.\n\n" +
                          "<color=#8FD8FF>Sigue las flechas del suelo · cada estación tiene un botón PISTA · Y: ver tu objetivo.</color>";
        // Carteles de las estaciones: qué se hace en cada una en las misiones.
        foreach (var g in rm.estaciones)
        {
            if (g == null || g.numero < 1 || g.numero > Descripciones.Length) continue;
            g.descripcion = Descripciones[g.numero - 1];
        }
        if (rm.resumen != null) rm.resumen.indiceFragmento = 1;
        rm.Conectar(panel, Misiones.MuestraHongo, Misiones.MuestraVirus);
        Debug.Log($"[Misiones] Misiones 1 y 2 listas con el mobiliario del centro (cámara {(camara != null ? camara.position.ToString() : "no encontrada")}, panel CONCLUSIONES {panel.transform.position}).");
    }

    static readonly string[] Descripciones =
    {
        "MISIÓN 1 · Aquí está la MUESTRA A (herida de piel): tócala con un portaobjetos. La B (moho) y la C (hisopado) ya están listas junto a los microscopios.",
        "MISIÓN 1 · Tinción de Gram de la MUESTRA A: goteros 1 > 2 > 3 > 4. Aquí están los portaobjetos y el desinfectante.",
        "MISIONES 1 y 2 · Compara las muestras A, B y C y, en la misión 2, el ANTES | DESPUÉS. Al lado, el panel CONCLUSIONES.",
        "AYUDA OPCIONAL · Fichas de microorganismos: si dudas, busca cuál coincide con la MUESTRA A.",
        "MISIÓN 1: identifica la MUESTRA A (fila 1) · MISIÓN 2: elige qué estructura atacar (fila 2).",
        "MISIÓN 2 · Encaja el vial del agente que sigue tu estrategia y pulsa FORMULAR.",
        "MISIÓN 2 · Placa de la MUESTRA A + cápsula y pulsa ACTIVAR.",
        "Informe de las misiones 1 y 2 y recuerdos de Baymax. REINICIAR las repite.",
    };

    static Transform Centro(RetoManager rm)
    {
        var c = rm.transform;
        while (c.parent != null && c.name != "CentroExperimentacion") c = c.parent;
        return c;
    }

    // ------------------------------------------------------------------ cámara experimental

    // Huecos del centro (sala x 6.87-24.14, z -0.25-17.26) y hacia dónde mira la consola (el usuario queda delante).
    static readonly (float x, float z, float yaw)[] HuecosCamara =
    {
        (19.8f, 8.5f, -90f),    // detrás del laboratorio de vacunas, de cara a él
        (19.8f, 12.6f, -90f),
        (19.8f, 4.5f, -90f),
        (12.2f, 12.8f, 90f),
        (12.2f, 4.6f, 90f),
        (17.6f, 13.6f, 180f),
    };

    static void ReubicarCamara(Transform camara, float suelo)
    {
        var cajas = new[]
        {
            (centro: new Vector3(0f, 1.15f, 0.3f), tam: new Vector3(1.7f, 2.1f, 2.3f)),   // cuerpo y consola
            (centro: new Vector3(0f, 1.0f, 1.95f), tam: new Vector3(1.4f, 1.4f, 1.1f)),   // sitio del usuario
        };
        int i = Elegir(HuecosCamara.Select(h => (new Vector3(h.x, suelo, h.z), h.yaw)).ToArray(), cajas, out int choques);
        var h0 = HuecosCamara[i];
        camara.SetPositionAndRotation(new Vector3(h0.x, camara.position.y, h0.z), Quaternion.Euler(0f, h0.yaw, 0f));
        camara.gameObject.SetActive(true);
        if (choques > 0) Debug.LogWarning($"[Misiones] Ningún hueco libre para la cámara experimental: se usa el que menos choca ({choques}).");
    }

    // Primer candidato sin obstáculos (o el que menos tiene).
    static int Elegir((Vector3 pos, float yaw)[] candidatos, (Vector3 centro, Vector3 tam)[] cajas, out int choques)
    {
        int mejor = 0; choques = int.MaxValue;
        for (int i = 0; i < candidatos.Length; i++)
        {
            int n = Obstaculos(candidatos[i].pos, candidatos[i].yaw, cajas, out string que);
            if (n > 0) Debug.Log($"[Misiones] Hueco {candidatos[i].pos} ocupado por {n}: {que}");
            if (n < choques) { choques = n; mejor = i; }
            if (n == 0) break;
        }
        return mejor;
    }

    static int Obstaculos(Vector3 p, float yaw, (Vector3 centro, Vector3 tam)[] cajas, out string que)
    {
        var rot = Quaternion.Euler(0f, yaw, 0f);
        var vistos = new HashSet<Collider>();
        foreach (var (c, t) in cajas)
            foreach (var col in Physics.OverlapBox(p + rot * c, t / 2f, rot, ~0, QueryTriggerInteraction.Ignore))
                if (col.GetComponentInParent<JugadorNanoLab>() == null) vistos.Add(col);
        que = string.Join(", ", vistos.Take(4).Select(v => v.name));
        return vistos.Count;
    }

    // ------------------------------------------------------------------ banco de muestras

    static void MuestrasBanco(RetoManager rm, Transform banco)
    {
        const float h = 0.95f;
        // La placa del caso pasa a ser la MUESTRA A.
        if (rm.placa != null)
        {
            var f = rm.placa.GetComponent<FuenteMuestra>();
            if (f != null) f.etiqueta = Misiones.EtiquetaA;
            foreach (var t in rm.placa.GetComponentsInChildren<TMP_Text>(true))
                if (t.text != null && t.text.Contains("CASO")) t.text = "<b>MUESTRA A</b>";
        }
        // Las dos ranuras "próximamente" (x 0.1 y 0.7): se quita su placa decorativa y su campana de vidrio.
        foreach (Transform t in banco)
        {
            var lp = t.localPosition;
            bool ranura = (Mathf.Abs(lp.x - 0.1f) < 0.06f || Mathf.Abs(lp.x - 0.7f) < 0.06f) && Mathf.Abs(lp.z + 0.25f) < 0.06f && lp.y > h - 0.05f;
            if (ranura && t.name != "Ranura") t.gameObject.SetActive(false);
        }
        // Rótulos de las ranuras (antes "CASO 01 · ACTIVO", "CASO 0x · Próximamente").
        foreach (var tx in banco.GetComponentsInChildren<TMP_Text>(true))
        {
            if (tx.text == null || !tx.text.StartsWith("<b>CASO 0")) continue;
            float x = banco.InverseTransformPoint(tx.transform.position).x;
            string titulo = x < -0.3f ? "MUESTRA A · Herida de piel" : x < 0.4f ? "MUESTRA B · Placa con moho" : "MUESTRA C · Hisopado nasal";
            string estado = x < -0.3f ? "PACIENTE 1" : x < 0.4f ? "OLVIDADA EN LA INCUBADORA" : "PACIENTE DEL BROTE";
            tx.text = $"<b>{titulo}</b>\n<color=#5FD4FF>{estado}</color>";
            tx.gameObject.SetActive(true);
        }
        PlacaMoho(banco, new Vector3(0.1f, h + 0.02f, -0.25f));
        TuboHisopado(banco, new Vector3(0.7f, h + 0.02f, -0.25f));
    }

    // MUESTRA B: placa de agar con colonias doradas y un moho verde-azulado con un halo sin colonias alrededor
    // (lo que vio Fleming en 1928).
    static void PlacaMoho(Transform banco, Vector3 pos)
    {
        var g = K.Grupo(banco, "MuestraB_PlacaMoho", pos);
        var vidrio = K.Vidrio(new Color(0.85f, 0.95f, 1f, 0.3f));
        K.Pieza(g, "Base", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0f), new Vector3(0.15f, 0.008f, 0.15f), vidrio);
        K.Pieza(g, "Agar", PrimitiveType.Cylinder, new Vector3(0f, 0.008f, 0f), new Vector3(0.14f, 0.004f, 0.14f), K.Plano(new Color(0.95f, 0.85f, 0.55f)));
        var centroMoho = new Vector3(0.022f, 0f, -0.012f);
        K.Pieza(g, "Halo", PrimitiveType.Cylinder, centroMoho + new Vector3(0f, 0.0123f, 0f), new Vector3(0.075f, 0.0004f, 0.075f), K.Plano(new Color(0.98f, 0.94f, 0.78f)));
        K.Pieza(g, "MohoBorde", PrimitiveType.Cylinder, centroMoho + new Vector3(0f, 0.0128f, 0f), new Vector3(0.036f, 0.0016f, 0.036f), K.Plano(new Color(0.95f, 0.97f, 0.95f)));
        K.Pieza(g, "Moho", PrimitiveType.Sphere, centroMoho + new Vector3(0f, 0.0135f, 0f), new Vector3(0.028f, 0.008f, 0.028f), K.Plano(new Color(0.3f, 0.6f, 0.5f)));
        var rnd = new System.Random(3);
        var dorado = K.Plano(new Color(0.95f, 0.75f, 0.25f));
        for (int i = 0; i < 16; i++)
        {
            float a = (float)rnd.NextDouble() * Mathf.PI * 2f, r = 0.02f + (float)rnd.NextDouble() * 0.045f;
            var p = new Vector3(Mathf.Cos(a) * r, 0.0125f, Mathf.Sin(a) * r);
            if ((p - centroMoho - new Vector3(0f, 0.0125f, 0f)).magnitude < 0.042f || p.magnitude > 0.064f) continue;   // dentro del halo no crecen
            float d = 0.007f + (float)rnd.NextDouble() * 0.007f;
            K.Pieza(g, "Colonia", PrimitiveType.Sphere, p, new Vector3(d, d * 0.35f, d), dorado);
        }
        K.Pieza(g, "Tapa", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.155f, 0.002f, 0.155f), vidrio);
        Fuente(g, Misiones.MuestraHongo, Misiones.EtiquetaB, new Vector3(0f, 0.035f, 0f), new Vector3(0.17f, 0.07f, 0.17f));
        Rotulo(g, "B", new Vector3(0f, 0.15f, 0f));
    }

    // MUESTRA C: tubo con el hisopo nasal en medio de transporte, en su gradilla.
    static void TuboHisopado(Transform banco, Vector3 pos)
    {
        var g = K.Grupo(banco, "MuestraC_Hisopado", pos);
        K.Caja(g, "Gradilla", new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.04f, 0.08f), K.Escena("PlasticoAzul", new Color(0.2f, 0.4f, 0.8f)));
        K.Pieza(g, "Tubo", PrimitiveType.Cylinder, new Vector3(0f, 0.075f, 0f), new Vector3(0.026f, 0.06f, 0.026f), K.Vidrio(new Color(0.85f, 0.95f, 1f, 0.35f)));
        K.Pieza(g, "Medio", PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f), new Vector3(0.022f, 0.028f, 0.022f), K.Plano(new Color(0.95f, 0.55f, 0.6f)));
        K.Pieza(g, "Tapon", PrimitiveType.Cylinder, new Vector3(0f, 0.14f, 0f), new Vector3(0.03f, 0.008f, 0.03f), K.Plano(new Color(0.85f, 0.2f, 0.25f)));
        K.Pieza(g, "Hisopo", PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0f), new Vector3(0.004f, 0.05f, 0.004f), K.Plano(Color.white));
        Fuente(g, Misiones.MuestraVirus, Misiones.EtiquetaC, new Vector3(0f, 0.08f, 0f), new Vector3(0.1f, 0.16f, 0.1f));
        Rotulo(g, "C", new Vector3(0f, 0.22f, 0f));
    }

    static void Fuente(Transform g, MuestraData m, string etiqueta, Vector3 centro, Vector3 tam)
    {
        var f = g.gameObject.AddComponent<FuenteMuestra>();
        f.muestra = m;
        f.etiqueta = etiqueta;
        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.center = centro;
        bc.size = tam;
    }

    static void Rotulo(Transform g, string letra, Vector3 pos)
    {
        var t = K.Texto(g, $"<b>MUESTRA {letra}</b>", pos, 0.32f, new Color(0.37f, 0.83f, 1f), 0.4f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        t.outlineWidth = 0.2f;
        t.outlineColor = new Color32(0, 0, 0, 220);
    }

    // ------------------------------------------------------------------ panel CONCLUSIONES

    static Transform bandejaMicro;

    // MISIÓN 1, menos idas y vueltas: los 2 portaobjetos de la bandeja de microscopía ya traen la MUESTRA B y la C
    // (preparaciones listas). Solo hay que ponerlas en la platina; también se pueden cargar a mano en el BANCO (1).
    public static void PrepararComparacion(RetoManager rm)
    {
        if (bandejaMicro == null || rm == null) return;
        var portas = bandejaMicro.GetComponentsInChildren<Portaobjetos>(true);
        var muestras = new[] { (Misiones.MuestraHongo, Misiones.EtiquetaB), (Misiones.MuestraVirus, Misiones.EtiquetaC) };
        for (int i = 0; i < portas.Length && i < muestras.Length; i++)
        {
            var (m, etiqueta) = muestras[i];
            if (m == null || portas[i].Agarrado) continue;
            if (portas[i].muestra != m) portas[i].Cargar(m, etiqueta);
        }
    }

    static PanelConclusiones CrearConclusiones(Transform centro, Transform micro, float suelo)
    {
        var cand = new List<(Vector3 pos, float yaw)>();
        if (micro != null)
        {
            float y = micro.eulerAngles.y;
            cand.Add((micro.TransformPoint(new Vector3(-2.65f, 0f, -0.1f)), y));   // entre la tinción y los microscopios
            cand.Add((micro.TransformPoint(new Vector3(2.6f, 0f, -0.1f)), y));
            cand.Add((micro.TransformPoint(new Vector3(-2.6f, 0f, 2.6f)), y + 90f));
        }
        cand.Add((new Vector3(21.3f, suelo, 4.6f), -90f));
        cand.Add((new Vector3(18.2f, suelo, 4.4f), 0f));
        for (int i = 0; i < cand.Count; i++) cand[i] = (new Vector3(cand[i].pos.x, suelo, cand[i].pos.z), cand[i].yaw);
        var cajas = new[]
        {
            (centro: new Vector3(0f, 1.0f, -0.05f), tam: new Vector3(1.05f, 1.9f, 0.6f)),   // atril y pantalla
            (centro: new Vector3(0f, 1.0f, 0.8f), tam: new Vector3(0.9f, 1.4f, 0.8f)),      // sitio del usuario
        };
        int i0 = Elegir(cand.ToArray(), cajas, out int choques);
        if (choques > 0) Debug.LogWarning($"[Misiones] Ningún hueco libre para el panel CONCLUSIONES: se usa el que menos choca ({choques}).");
        return PanelConclusiones.Crear(centro, cand[i0].pos, cand[i0].yaw);
    }
}
