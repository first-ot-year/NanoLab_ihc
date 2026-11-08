using TMPro;
using UnityEngine;

// Cabina de descontaminación (esclusa) a la entrada de la sala BSL-3, con DOS puertas correderas de
// vidrio: la exterior (lado del laboratorio) y la interior (lado de la BSL-3). Nunca están abiertas a la
// vez. Al acercarse desde el laboratorio se abre la exterior; al quedarse dentro se cierra, el usuario
// queda encerrado y recibe una desinfección completa de 11 s en cuatro fases (ducha de aire,
// nebulización, luz UV-C y extracción) con una pantalla en cada pared lateral que muestra la fase, qué hace,
// la barra de progreso y los segundos que faltan. Al terminar se abre la interior. Para salir es igual
// al revés: se abre la interior, se cierran las dos, desinfección de salida y se abre la exterior.
// Si sale de la cabina a mitad, se interrumpe. La puerta exterior se crea al empezar (clonando las hojas
// de la interior) si la escena no la tiene. Va en la raíz de la cabina; todo en coordenadas del mundo.
public class CabinaDescontaminacion : MonoBehaviour
{
    public Transform hojaIzq, hojaDer;          // puerta interior: se deslizan hacia -x y +x
    [Tooltip("Puerta exterior (lado del laboratorio). Si falta, se crea al empezar copiando la interior.")]
    public Transform hojaExtIzq, hojaExtDer;
    public float apertura = 0.76f;
    [Tooltip("Centro de la puerta interior, a ras de suelo.")]
    public Vector3 puerta;
    [Tooltip("Interior de la cabina (donde hay que quedarse).")]
    public Bounds zona;
    [Tooltip("Hacia dónde queda la sala BSL-3 desde la puerta.")]
    public Vector3 haciaDentro = Vector3.forward;
    public ParticleSystem[] humo;
    [Tooltip("Duración total de la desinfección (s). La escena antigua guardaba 3.5: se fuerza a 11.")]
    public float duracion = 11f;

    struct Fase { public string nombre, detalle; public float seg; public bool humo, uv; }
    static readonly Fase[] Fases =
    {
        new Fase { nombre = "DUCHA DE AIRE", detalle = "Chorros de aire con filtro HEPA arrastran las partículas de la bata.", seg = 3f, humo = true },
        new Fase { nombre = "NEBULIZACIÓN", detalle = "Niebla de peróxido de hidrógeno: cubre la bata, los guantes y el calzado.", seg = 4f, humo = true },
        new Fase { nombre = "LUZ UV-C", detalle = "La radiación ultravioleta inactiva los microorganismos de las superficies.", seg = 2.5f, uv = true },
        new Fase { nombre = "EXTRACCIÓN", detalle = "Se renueva el aire de la cabina. Quédate quieto un momento más.", seg = 1.5f },
    };

    // Espera: las dos cerradas. AbiertaFuera / AbiertaDentro: una de las dos abierta. Sellando: la que
    // estaba abierta se cierra con el usuario dentro. Rociando: las dos cerradas, desinfectando.
    enum Estado { Espera, AbiertaFuera, AbiertaDentro, Sellando, Rociando }
    Estado estado;
    bool salida, estuvoDentro;
    float t, tEnCabina, tLejos, kInt, kExt, finPanel;
    int fase = -1;
    Vector3 izq0, der0, extIzq0, extDer0, puertaExt;
    Light luzUV;
    PanelProgreso panel;

    bool IntAbierta => estado == Estado.AbiertaDentro;
    bool ExtAbierta => estado == Estado.AbiertaFuera;

    void Awake()
    {
        if (duracion < 11f) duracion = 11f;   // la escena guardada lleva el valor antiguo
        haciaDentro = Vector3.ProjectOnPlane(haciaDentro, Vector3.up).normalized;
        // La puerta exterior va pegada al frente de la cabina (lado del laboratorio).
        float medio = Medio(haciaDentro);
        float dPuerta = Vector3.Dot(puerta - zona.center, haciaDentro);
        puertaExt = puerta - haciaDentro * (dPuerta + medio + 0.02f);
        if (hojaExtIzq == null && hojaIzq != null) hojaExtIzq = Clonar(hojaIzq, "HojaExtIzq");
        if (hojaExtDer == null && hojaDer != null) hojaExtDer = Clonar(hojaDer, "HojaExtDer");
        if (hojaIzq != null) izq0 = hojaIzq.localPosition;
        if (hojaDer != null) der0 = hojaDer.localPosition;
        if (hojaExtIzq != null) extIzq0 = hojaExtIzq.localPosition;
        if (hojaExtDer != null) extDer0 = hojaExtDer.localPosition;
        Jambas();
        Humo(false);
    }

    // Mitad de la cabina a lo largo de una dirección horizontal.
    float Medio(Vector3 dir) => Mathf.Abs(zona.extents.x * dir.x) + Mathf.Abs(zona.extents.z * dir.z);

    // Tapa la rendija que queda entre el final de las paredes laterales y la puerta interior (por ahí se
    // veía el laboratorio con la cabina cerrada).
    void Jambas()
    {
        if (hojaIzq == null || transform.Find("Jamba") != null) return;
        float hueco = Vector3.Dot(hojaIzq.position - zona.center, haciaDentro) - Medio(haciaDentro);
        if (hueco < 0.03f) return;
        Material blanco = null;
        foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            if (r.name == "Lateral") { blanco = r.sharedMaterial; break; }
        var derecha = Vector3.Cross(Vector3.up, haciaDentro).normalized;
        float suelo = zona.center.y - zona.extents.y;
        foreach (float lado in new[] { -1f, 1f })
        {
            var j = GameObject.CreatePrimitive(PrimitiveType.Cube);
            j.name = "Jamba";
            j.transform.SetParent(transform, true);
            float largo = hueco - 0.025f;                    // termina justo antes de la hoja (que pasa por delante)
            float a = Medio(haciaDentro) + largo / 2f;
            j.transform.position = zona.center + haciaDentro * a + derecha * (lado * (Medio(derecha) + 0.04f)) + Vector3.up * (suelo + 1.16f - zona.center.y);
            j.transform.rotation = Quaternion.LookRotation(haciaDentro, Vector3.up);
            j.transform.localScale = new Vector3(0.08f, 2.32f, largo);
            if (blanco != null) j.GetComponent<MeshRenderer>().sharedMaterial = blanco;
        }
    }

    // Copia una hoja de la puerta interior al lado exterior (girada para que la franja mire al laboratorio)
    // y, la primera vez, un dintel que tapa el hueco entre la puerta y el techo de la cabina.
    Transform Clonar(Transform hoja, string nombre)
    {
        var c = Instantiate(hoja.gameObject, hoja.parent).transform;
        c.name = nombre;
        float d = Vector3.Dot(hoja.position - zona.center, haciaDentro);
        c.position = hoja.position - haciaDentro * (d + Medio(haciaDentro) + 0.025f);   // justo delante del frente
        c.rotation = Quaternion.AngleAxis(180f, Vector3.up) * hoja.rotation;
        if (transform.Find("DintelExterior") == null)
        {
            Material blanco = null;
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
                if (r.name == "Lateral" || r.name == "Techo") { blanco = r.sharedMaterial; break; }
            var dintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dintel.name = "DintelExterior";
            dintel.transform.SetParent(transform, true);
            float y0 = puerta.y + 2.25f, y1 = zona.center.y - zona.extents.y + 2.47f;
            dintel.transform.position = new Vector3(puertaExt.x, (y0 + y1) / 2f, c.position.z);
            dintel.transform.rotation = Quaternion.LookRotation(haciaDentro, Vector3.up);
            dintel.transform.localScale = new Vector3(zona.size.x + 0.24f, Mathf.Max(0.05f, y1 - y0), 0.06f);
            if (blanco != null) dintel.GetComponent<MeshRenderer>().sharedMaterial = blanco;
        }
        return c;
    }

    void Update()
    {
        var cabeza = ManosRig.Cabeza;
        if (cabeza != null) Logica(cabeza.position);
        kInt = Mathf.MoveTowards(kInt, IntAbierta ? 1f : 0f, Time.deltaTime * 1.4f);
        kExt = Mathf.MoveTowards(kExt, ExtAbierta ? 1f : 0f, Time.deltaTime * 1.4f);
        float si = Mathf.SmoothStep(0f, 1f, kInt) * apertura, se = Mathf.SmoothStep(0f, 1f, kExt) * apertura;
        if (hojaIzq != null) hojaIzq.localPosition = izq0 + Vector3.left * si;
        if (hojaDer != null) hojaDer.localPosition = der0 + Vector3.right * si;
        if (hojaExtIzq != null) hojaExtIzq.localPosition = extIzq0 + Vector3.left * se;
        if (hojaExtDer != null) hojaExtDer.localPosition = extDer0 + Vector3.right * se;
        if (panel != null && estado != Estado.Rociando && estado != Estado.Sellando && Time.time > finPanel) panel.Mostrar(false);
    }

    void Logica(Vector3 h)
    {
        var plano = new Vector3(h.x, zona.center.y, h.z);
        bool enCabina = zona.Contains(plano);
        float lado = Vector3.Dot(h - puerta, haciaDentro);               // > 0: dentro de la BSL-3
        float ladoExt = Vector3.Dot(h - puertaExt, haciaDentro);         // < 0: en el laboratorio
        float lateral = Vector3.Dot(h - puerta, Vector3.Cross(Vector3.up, haciaDentro));
        bool dentro = lado > 0.1f;
        bool fuera = ladoExt < -0.1f && !enCabina;
        bool cercaDentro = dentro && lado < 0.9f && Mathf.Abs(lateral) < 1.1f;          // se abre
        bool cercaFuera = fuera && ladoExt > -0.9f && Mathf.Abs(lateral) < 1.1f;
        bool umbralDentro = dentro && lado < 1.2f && Mathf.Abs(lateral) < 1.3f;         // sigue abierta
        bool umbralFuera = fuera && ladoExt > -1.2f && Mathf.Abs(lateral) < 1.3f;
        // Con margen: al balancear la cabeza no se "sale" de la cabina.
        var holgada = new Bounds(zona.center, zona.size + new Vector3(0.3f, 0f, 0.3f));
        bool enCabinaHolgada = holgada.Contains(plano);
        if (dentro) estuvoDentro = true;
        else if (fuera && !umbralFuera) estuvoDentro = false;

        switch (estado)
        {
            case Estado.Espera:
                if (cercaDentro) Abrir(Estado.AbiertaDentro);
                else if (cercaFuera) Abrir(Estado.AbiertaFuera);
                else if (enCabina)
                {
                    // Dentro con las dos cerradas (p. ej. tras interrumpir): vuelve a empezar.
                    tEnCabina += Time.deltaTime;
                    if (tEnCabina > 0.8f) Sellar(estuvoDentro);
                }
                else tEnCabina = 0f;
                break;

            case Estado.AbiertaFuera:
                // Entrando desde el laboratorio: ya dentro de la cabina, se cierra la exterior y se desinfecta.
                bool pasado = enCabina && ladoExt > 0.25f;
                tEnCabina = pasado ? tEnCabina + Time.deltaTime : 0f;
                if (tEnCabina > 0.6f && !salida) { Sellar(false); break; }
                if (salida && fuera) salida = false;                       // ya salió tras la desinfección de salida
                tLejos = umbralFuera || enCabina ? 0f : tLejos + Time.deltaTime;
                if (tLejos > 0.6f) Cerrar();
                break;

            case Estado.AbiertaDentro:
                // Saliendo de la BSL-3: ya dentro de la cabina, se cierra la interior y se desinfecta.
                bool entro = estuvoDentro && enCabina && lado < -0.35f;
                tEnCabina = entro ? tEnCabina + Time.deltaTime : 0f;
                if (tEnCabina > 0.6f) { Sellar(true); break; }
                tLejos = umbralDentro || enCabina ? 0f : tLejos + Time.deltaTime;
                if (tLejos > 0.6f) Cerrar();
                break;

            case Estado.Sellando:
                // Espera a que la puerta abierta termine de cerrarse antes de rociar.
                if (!enCabinaHolgada) { Interrumpir(); break; }
                if (kInt <= 0f && kExt <= 0f) Rociar();
                break;

            case Estado.Rociando:
                t += Time.deltaTime;
                if (!enCabinaHolgada) { Interrumpir(); break; }
                Progreso();
                if (t >= duracion)
                {
                    Parar();
                    Sonidos.Etapa(puerta);
                    if (salida)
                    {
                        Abrir(Estado.AbiertaFuera);
                        Final("DESINFECCIÓN DE SALIDA COMPLETA", "Puerta exterior abierta: ya puedes volver al laboratorio.");
                    }
                    else
                    {
                        Abrir(Estado.AbiertaDentro);
                        Final("DESCONTAMINACIÓN COMPLETA", "Puerta interior abierta: puedes pasar a la sala BSL-3.");
                    }
                }
                break;
        }
    }

    // Cierra la puerta que estaba abierta con el usuario dentro: las dos quedan cerradas.
    void Sellar(bool deSalida)
    {
        salida = deSalida;
        bool habiaAbierta = estado == Estado.AbiertaFuera || estado == Estado.AbiertaDentro;
        estado = Estado.Sellando;
        tEnCabina = 0f;
        if (habiaAbierta) Sonidos.Puerta(zona.center, false);
        if (panel == null) panel = CrearPantallas();
        panel.Mostrar(true);
        panel.Poner(deSalida ? "DESINFECCIÓN DE SALIDA" : "DESCONTAMINACIÓN", -1, "", "Cerrando las dos puertas… quédate en el centro de la cabina.", 0f, duracion, false);
    }

    // Pantalla de estado fija en cada pared lateral de la cabina, a la altura de los ojos y mirando hacia
    // dentro: se ve al entrar y al salir sin que nada siga a la cabeza. Las dos muestran lo mismo.
    PanelProgreso CrearPantallas()
    {
        var derecha = Vector3.Cross(Vector3.up, haciaDentro).normalized;
        float suelo = zona.center.y - zona.extents.y;
        float sep = Mathf.Abs(Vector3.Dot(zona.extents, new Vector3(Mathf.Abs(derecha.x), Mathf.Abs(derecha.y), Mathf.Abs(derecha.z)))) + 0.03f;
        PanelProgreso primero = null, anterior = null;
        foreach (float lado in new[] { 1f, -1f })
        {
            var pos = new Vector3(zona.center.x, suelo + 1.5f, zona.center.z) + derecha * (lado * sep);
            var rot = Quaternion.LookRotation(derecha * lado, Vector3.up);   // la cara visible (-z) mira al centro de la cabina
            var pp = PanelProgreso.Crear(transform, pos, rot, 1.25f);
            if (primero == null) primero = pp; else anterior.gemelo = pp;
            anterior = pp;
        }
        return primero;
    }

    void Rociar()
    {
        estado = Estado.Rociando;
        t = 0f;
        fase = -1;
        ManosRig.Vibrar(zona.center, 0.25f, 0.3f);
        Progreso();
    }

    void Interrumpir()
    {
        Parar();
        estado = Estado.Espera;
        tEnCabina = 0f;
        Avisar("Descontaminación interrumpida.\nVuelve al centro de la cabina y quédate quieto.", Aviso.Tipo.Error);
        Sonidos.Error(puerta);
    }

    // Cambia de fase (humo, luz UV, sonido) y actualiza la pancarta.
    void Progreso()
    {
        float acumulado = 0f, escala = duracion / 11f;
        int f = Fases.Length - 1;
        for (int i = 0; i < Fases.Length; i++)
        {
            float d = Fases[i].seg * escala;
            if (t < acumulado + d) { f = i; break; }
            acumulado += d;
        }
        if (f != fase)
        {
            fase = f;
            Humo(Fases[f].humo);
            UV(Fases[f].uv);
            if (Fases[f].humo) Sonidos.Rocio(zona.center, Fases[f].seg * escala);
            Sonidos.Clic(zona.center);
        }
        float p = Mathf.Clamp01(t / duracion);
        string titulo = salida ? "DESINFECCIÓN DE SALIDA" : "DESCONTAMINACIÓN EN CURSO";
        panel.Poner(titulo, f, Fases[f].nombre, Fases[f].detalle, p, Mathf.Max(0f, duracion - t), false);
    }

    void Final(string titulo, string texto)
    {
        if (panel == null) return;
        panel.Poner(titulo, Fases.Length, "", texto, 1f, 0f, true);
        finPanel = Time.time + 2.5f;
    }

    void Parar()
    {
        Humo(false);
        UV(false);
        fase = -1;
        finPanel = Time.time + 0.1f;
    }

    void Abrir(Estado cual)
    {
        estado = cual;
        tLejos = 0f;
        tEnCabina = 0f;
        Sonidos.Puerta(cual == Estado.AbiertaFuera ? puertaExt : puerta, true);
    }

    void Cerrar()
    {
        var p = estado == Estado.AbiertaFuera ? puertaExt : puerta;
        estado = Estado.Espera;
        tEnCabina = 0f;
        salida = false;
        Sonidos.Puerta(p, false);
    }

    void Humo(bool si)
    {
        if (humo == null) return;
        foreach (var p in humo)
        {
            if (p == null) continue;
            if (si) { if (!p.isPlaying) p.Play(); }
            else p.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    // Luz violeta en tiempo real durante la fase UV-C.
    void UV(bool si)
    {
        if (luzUV == null)
        {
            if (!si) return;
            var go = new GameObject("LuzUVC");
            go.transform.SetParent(transform, false);
            go.transform.position = zona.center + Vector3.up * 0.8f;
            luzUV = go.AddComponent<Light>();
            luzUV.type = LightType.Point;
            luzUV.color = new Color(0.55f, 0.3f, 1f);
            luzUV.range = 3f;
            luzUV.intensity = 2.2f;
            luzUV.shadows = LightShadows.None;
        }
        luzUV.enabled = si;
    }

    // El aviso aparece delante de la cara (dentro de la cabina no se ve bien un cartel fijo).
    static void Avisar(string texto, Aviso.Tipo tipo)
    {
        var c = ManosRig.Cabeza;
        if (c == null) return;
        var f = Vector3.ProjectOnPlane(c.forward, Vector3.up).normalized;
        Aviso.Mostrar(texto, c.position + f * 0.9f, tipo, 3f);
    }

    // Pancarta de estado que acompaña a la cabeza: título, pasos 1-4, fase actual con su explicación,
    // barra de progreso con porcentaje y segundos restantes. Cristal translúcido con texto contorneado.
    class PanelProgreso : MonoBehaviour
    {
        const float W = 0.5f, H = 0.27f, AnchoBarra = 0.42f;
        TextMeshPro titulo, pasos, nombre, detalle, cifras;
        PanelRedondeado relleno;
        bool visible;

        public PanelProgreso gemelo;   // la pantalla de la otra pared

        public static PanelProgreso Crear(Transform padre, Vector3 pos, Quaternion rot, float escala)
        {
            var go = new GameObject("PantallaDescontaminacion");
            // Sin padre: la escala de la cabina no deforma la pantalla.
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = Vector3.one * escala;
            var p = go.AddComponent<PanelProgreso>();
            p.Montar();
            go.SetActive(false);
            return p;
        }

        void Montar()
        {
            Material mat = null;
            var ejemplo = FindObjectOfType<PanelRedondeado>(true);
            if (ejemplo != null) mat = ejemplo.GetComponent<MeshRenderer>().sharedMaterial;
            if (mat == null)
            {
                var sh = Shader.Find("Interaction/RoundedBoxUnlit");
                if (sh != null) mat = new Material(sh);
            }
            TMP_FontAsset fuente = EstiloNanoLab.Fuente;   // Nunito; respaldo: la letra de la escena
            if (fuente == null)
                foreach (var tx in FindObjectsOfType<TextMeshPro>(true)) if (tx.font != null) { fuente = tx.font; break; }

            Quad("Fondo", Vector3.zero, W, H, 0.025f, AdaptadorEscena.ColorPanel, AdaptadorEscena.BordePanel, 0.003f, mat);
            Quad("Barra", new Vector3(0f, -0.075f, -0.002f), AnchoBarra, 0.026f, 0.013f, new Color(1f, 1f, 1f, 0.18f), Color.clear, 0f, mat);
            relleno = Quad("Relleno", new Vector3(0f, -0.075f, -0.003f), 0.001f, 0.026f, 0.013f, new Color(0.3f, 0.95f, 1f, 0.95f), Color.clear, 0f, mat);

            titulo = Texto("Titulo", new Vector3(0f, 0.105f, -0.004f), 0.24f, true, fuente);
            pasos = Texto("Pasos", new Vector3(0f, 0.065f, -0.004f), 0.16f, false, fuente);
            nombre = Texto("Fase", new Vector3(0f, 0.025f, -0.004f), 0.3f, true, fuente);
            detalle = Texto("Detalle", new Vector3(0f, -0.022f, -0.004f), 0.17f, false, fuente);
            cifras = Texto("Cifras", new Vector3(0f, -0.11f, -0.004f), 0.18f, true, fuente);
        }

        PanelRedondeado Quad(string n, Vector3 pos, float ancho, float alto, float radio, Color c, Color borde, float grosor, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = n;
            Destroy(q.GetComponent<Collider>());
            q.transform.SetParent(transform, false);
            q.transform.localPosition = pos;
            var mr = q.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (mat != null) mr.sharedMaterial = mat;
            var p = q.AddComponent<PanelRedondeado>();
            p.ancho = ancho; p.alto = alto; p.radio = radio; p.color = c; p.colorBorde = borde; p.borde = grosor;
            p.Aplicar();
            return p;
        }

        TextMeshPro Texto(string n, Vector3 pos, float tam, bool negrita, TMP_FontAsset fuente)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var t = go.AddComponent<TextMeshPro>();
            if (fuente != null) t.font = fuente;
            t.fontSize = tam;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = true;
            t.rectTransform.sizeDelta = new Vector2(W - 0.04f, 0.05f);
            t.overflowMode = TextOverflowModes.Overflow;
            if (negrita) t.fontStyle = FontStyles.Bold;
            t.outlineWidth = 0.12f;
            t.outlineColor = new Color32(0, 15, 30, 220);
            t.color = Color.white;
            return t;
        }

        public void Mostrar(bool si)
        {
            if (gemelo != null) gemelo.Mostrar(si);
            if (si == visible && gameObject.activeSelf == si) return;
            visible = si;
            gameObject.SetActive(si);
        }

        public void Poner(string tit, int fase, string fn, string det, float p, float quedan, bool hecho)
        {
            if (gemelo != null) gemelo.Poner(tit, fase, fn, det, p, quedan, hecho);
            titulo.text = tit;
            titulo.color = hecho ? new Color(0.45f, 1f, 0.6f) : new Color(0.6f, 0.92f, 1f);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Fases.Length; i++)
            {
                if (i > 0) sb.Append("   ");
                string etiqueta = $"{i + 1}. {Fases[i].nombre}";
                if (i < fase) sb.Append("<color=#6BFF9A>").Append(etiqueta).Append(" OK</color>");
                else if (i == fase) sb.Append("<color=#FFD25A><b>").Append(etiqueta).Append("</b></color>");
                else sb.Append("<color=#9FB4C4>").Append(etiqueta).Append("</color>");
            }
            pasos.text = sb.ToString();
            nombre.text = hecho ? "<color=#6BFF9A>LISTO</color>" : fase < 0 ? "<color=#FFD25A>SELLANDO LA CABINA</color>" : $"Fase {fase + 1} de {Fases.Length}: <color=#FFD25A>{fn}</color>";
            detalle.text = det;
            cifras.text = hecho ? "100 %" : $"{Mathf.FloorToInt(p * 100f)} %   ·   quedan {Mathf.CeilToInt(quedan)} s   ·   no salgas de la cabina";
            float a = Mathf.Max(0.001f, AnchoBarra * p);
            relleno.ancho = a;
            relleno.color = hecho ? new Color(0.4f, 1f, 0.55f, 0.95f) : new Color(0.3f, 0.95f, 1f, 0.95f);
            relleno.transform.localPosition = new Vector3(-AnchoBarra / 2f + a / 2f, -0.075f, -0.003f);
            relleno.Aplicar();
        }

    }
}
