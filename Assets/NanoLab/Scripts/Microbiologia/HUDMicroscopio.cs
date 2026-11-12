using TMPro;
using UnityEngine;
using UnityEngine.UI;

// HUD "Análisis microscópico" que aparece al acercar la cara al ocular.
// Se construye por código, pegado a la cabeza, a 0,6 m delante de los ojos.
public class HUDMicroscopio : MonoBehaviour
{
    public static HUDMicroscopio Instancia { get; private set; }
    public static event System.Action<int> AumentoCambiado;

    // Contenido del HUD cuando la muestra pertenece a un reto del Centro de Experimentación:
    // identidad oculta, imagen según la tinción, cuaderno de evidencias y comparación ANTES | DESPUÉS.
    public struct DatosReto
    {
        public string subtitulo;
        public Texture vista, antes;
        public string antesTitulo;
        public string[] claves, valores;
        public string riesgo;
        public string riesgoClave;      // etiqueta de la fila (por defecto "Riesgo")
        public Color? colorRiesgo;      // color de la caja (por defecto rojo)
        public string cuadernoTitulo, cuaderno;
        public string obsTitulo, obs;
        public string guia;
        public string aviso;
        public Color colorAviso;
    }

    static readonly Color Cian = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color CianSuave = new Color(0.35f, 0.85f, 1f, 0.35f);
    static readonly Color Panel = new Color(0.05f, 0.16f, 0.29f, 0.93f);   // azul profundo: el texto blanco contrasta > 12:1
    static readonly Color TextoClaro = new Color(0.85f, 0.93f, 1f, 1f);
    static readonly Color TextoGris = new Color(0.55f, 0.66f, 0.78f, 1f);

    CanvasGroup grupo;
    RawImage vista, micro;
    TMP_Text subtitulo, zoomTxt, escalaTxt, tabTitulo, tabTexto, obsTxt, creditoTxt, riesgoTxt, microTitulo, obsTit, avisoTxt, guiaTxt;
    GameObject pestanas;
    TMP_Text riesgoEtq;
    Image riesgoFondo;
    Image[] riesgoMarco;
    static readonly Color RiesgoFondoColor = new Color(0.45f, 0.05f, 0.08f, 0.85f);
    static readonly Color RiesgoMarcoColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    System.Func<int, DatosReto> proveedorReto;
    readonly TMP_Text[] claves = new TMP_Text[6], valores = new TMP_Text[6];
    readonly Image[] tabs = new Image[4];
    readonly TMP_Text[] tabsTxt = new TMP_Text[4];
    readonly Image[] leyendaPunto = new Image[5];
    readonly TMP_Text[] leyendaTxt = new TMP_Text[5];
    MuestraData actual;
    int aumento, pestana;
    // Zoom continuo: 'zoomObjetivo' lo fijan A/B (saltos a cada objetivo) o el joystick derecho arriba/abajo
    // (zoom fino); 'zoomActual' lo sigue con suavidad. 'aumento' es el objetivo más cercano (etiquetas y reto).
    float zoomActual = 1f, zoomObjetivo = 1f;
    RectTransform zoomRelleno;
    const float AnchoBarraZoom = 170f;
    Vector2 pan;               // desplazamiento de la muestra (uv), como mover la platina
    bool comparando;           // la micrografía pequeña es el ANTES y debe seguir al desplazamiento
    float alphaObjetivo;
    Material holo;

    // Modo "mover la vista" (botón X): en el visor el HUD no cabe entero en la lente y los paneles
    // laterales quedan cortados; el joystick izquierdo desplaza un poco el HUD para traerlos al centro.
    const float Distancia = 0.6f;
    static readonly Vector2 MaxDesplazamiento = new Vector2(0.4f, 0.12f);   // m: centra los paneles laterales
    const float VelocidadVista = 0.45f;                                    // m/s con el joystick a fondo
    bool modoVista;
    Vector2 desplazamiento;
    TMP_Text modoTxt;
    Image modoFondo;
    public bool ModoVista => modoVista;

    // Examen en fresco (portaobjetos sin teñir): bacterias vivas que se mueven según su ficha.
    // Las teñidas con Gram están fijadas y se ve la micrografía real, quieta.
    const int MaxCelulas = 90;
    const float Campo = 60f;             // lado de la muestra recorrible (µm)
    const float Vista = 440f;            // diámetro de la vista circular (px del HUD)
    struct Grupo { public Vector2 pos; public float ang, velocidad, tRun, tGiro, giro; public bool nada; }
    struct Celula { public int grupo; public Vector2 offset; public float largo, ancho, angulo; }
    RectTransform celulasRaiz;
    readonly Image[] celulas = new Image[MaxCelulas];
    readonly Celula[] celula = new Celula[MaxCelulas];
    Grupo[] grupos = new Grupo[0];
    int nCelulas;
    bool enFresco;

    public static HUDMicroscopio Obtener(Transform ojo, Material holoAzul)
    {
        if (Instancia == null)
        {
            var go = new GameObject("HUDMicroscopio", typeof(RectTransform));
            Instancia = go.AddComponent<HUDMicroscopio>();
            Instancia.holo = holoAzul;
            Instancia.Construir();
        }
        var t = Instancia.transform;
        t.SetParent(ojo, false);
        t.localPosition = new Vector3(0f, 0f, Distancia);
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one * 0.0008f;
        return Instancia;
    }

    public int Aumento => aumento;

    public void MostrarReto(MuestraData m, System.Func<int, DatosReto> proveedor)
    {
        Mostrar(m, false);
        proveedorReto = proveedor;
        Refrescar();
    }

    public void RefrescarReto() { if (proveedorReto != null) Refrescar(); }

    public void Mostrar(MuestraData m) => Mostrar(m, false);

    // 'fresco': portaobjetos sin teñir de una bacteria -> células vivas en movimiento.
    public void Mostrar(MuestraData m, bool fresco)
    {
        proveedorReto = null;
        enFresco = fresco && m.tipo == TipoMuestra.Bacteria;
        actual = m;
        aumento = m.AumentoInicial;
        zoomActual = zoomObjetivo = m.FactoresZoom[aumento];
        pestana = 0;
        pan = Vector2.zero;
        modoVista = false;
        desplazamiento = Vector2.zero;
        transform.localPosition = new Vector3(0f, 0f, Distancia);
        RefrescarModo();
        gameObject.SetActive(true);
        var visor = VisorVirus.Obtener();
        if (m.Vista3D)
        {
            visor.Mostrar(m, holo);
            visor.Activo(true);
            vista.texture = visor.RT;
        }
        else
        {
            visor.Activo(false);
            vista.texture = enFresco ? FondoFresco() : m.micrografia;
        }
        if (enFresco) IniciarCelulas(m);
        celulasRaiz.gameObject.SetActive(enFresco);
        Refrescar();
        alphaObjetivo = 1f;
    }

    public void MostrarInmediato(MuestraData m)
    {
        Mostrar(m);
        grupo.alpha = 1f;
    }

    public void Ocultar() => alphaObjetivo = 0f;

    // A / B: salta al objetivo siguiente o anterior (el cambio de imagen es una transición suave).
    // Devuelve false si ya estaba en el máximo o el mínimo.
    public bool CambiarAumento(int d)
    {
        if (actual == null) return false;
        var f = actual.FactoresZoom;
        int i = Mathf.Clamp(aumento + d, 0, f.Length - 1);
        // Si el zoom fino quedó entre dos objetivos, A/B va al siguiente en esa dirección.
        if (d > 0 && zoomObjetivo < f[aumento] - 1e-3f) i = aumento;
        if (d < 0 && zoomObjetivo > f[aumento] + 1e-3f) i = aumento;
        bool cambia = !Mathf.Approximately(f[i], zoomObjetivo);
        zoomObjetivo = f[i];
        PonerAumento(i);
        return cambia;
    }

    // Joystick derecho arriba (zoom +) / abajo (zoom -): zoom fino continuo entre el mínimo y el máximo.
    public void ZoomContinuo(float eje)
    {
        if (actual == null || Mathf.Abs(eje) < 0.25f) return;
        var f = actual.FactoresZoom;
        float v = (Mathf.Abs(eje) - 0.25f) / 0.75f * Mathf.Sign(eje);
        zoomObjetivo = Mathf.Clamp(zoomObjetivo * Mathf.Exp(v * 1.4f * Time.deltaTime), f[0], f[f.Length - 1]);
        PonerAumento(ObjetivoCercano(zoomObjetivo));
    }

    int ObjetivoCercano(float z)
    {
        var f = actual.FactoresZoom;
        int mejor = 0;
        for (int i = 1; i < f.Length; i++)
            if (Mathf.Abs(Mathf.Log(z / f[i])) < Mathf.Abs(Mathf.Log(z / f[mejor]))) mejor = i;
        return mejor;
    }

    void PonerAumento(int i)
    {
        if (i == aumento) return;
        aumento = i;
        Refrescar();
        AumentoCambiado?.Invoke(aumento);
    }

    // Aplica 'zoomActual' a la vista: recorte de la micrografía (bacterias) o cámara del virus.
    void AplicarZoom()
    {
        if (actual == null) return;
        if (actual.Vista3D) VisorVirus.Obtener().Zoom(zoomActual);
        else
        {
            AplicarRecorte(1f / zoomActual);
            if (comparando)
            {
                var r = vista.uvRect;
                float h = r.width * 170f / 380f;
                micro.uvRect = new Rect(r.x, r.y + (r.height - h) / 2f, r.width, h);
            }
        }
        zoomTxt.text = TextoAumento(zoomActual);
        if (zoomRelleno != null)
        {
            var f = actual.FactoresZoom;
            float t = Mathf.InverseLerp(Mathf.Log(f[0]), Mathf.Log(f[f.Length - 1]), Mathf.Log(zoomActual));
            float w = Mathf.Max(4f, AnchoBarraZoom * t);
            zoomRelleno.sizeDelta = new Vector2(w, zoomRelleno.sizeDelta.y);
            zoomRelleno.anchoredPosition = new Vector2(330f - AnchoBarraZoom / 2f + w / 2f, zoomRelleno.anchoredPosition.y);
        }
    }

    // Aumento intermedio (escala logarítmica entre los objetivos): "650x" mientras se hace zoom fino.
    string TextoAumento(float z)
    {
        var f = actual.FactoresZoom;
        var et = actual.Aumentos;
        int i = 0;
        while (i < f.Length - 2 && z > f[i + 1]) i++;
        float a = Numero(et[i]), b = Numero(et[Mathf.Min(i + 1, et.Length - 1)]);
        if (a <= 0f || b <= 0f || f.Length < 2) return et[aumento];
        if (Mathf.Abs(Mathf.Log(z / f[aumento])) < 0.02f) return et[aumento];
        float t = Mathf.Clamp01(Mathf.InverseLerp(Mathf.Log(f[i]), Mathf.Log(f[i + 1]), Mathf.Log(z)));
        float m = Mathf.Exp(Mathf.Lerp(Mathf.Log(a), Mathf.Log(b), t));
        float paso = m >= 10000f ? 1000f : m >= 1000f ? 50f : 10f;
        m = Mathf.Round(m / paso) * paso;
        return m >= 10000f ? $"{m / 1000f:0} 000x" : $"{m:0}x";
    }

    static float Numero(string s)
    {
        float v = 0f; bool hay = false;
        foreach (char c in s) if (char.IsDigit(c)) { v = v * 10f + (c - '0'); hay = true; }
        return hay ? v : -1f;
    }

    // Joystick: mueve la muestra (bacterias: recorre la imagen ampliada; virus: gira el modelo).
    public void Mover(Vector2 eje)
    {
        if (actual == null || eje.sqrMagnitude < 0.04f) return;
        if (actual.Vista3D && proveedorReto == null)
        {
            VisorVirus.Obtener().Orbitar(eje * Time.deltaTime);
            return;
        }
        float s = vista.uvRect.width;
        if (s >= 0.999f) return;   // a poco aumento se ve la muestra entera
        pan += eje * (Time.deltaTime * s * 0.6f);   // más aumento, movimiento más fino
        AplicarRecorte(s);
        if (comparando)
        {
            var r = vista.uvRect;
            float h = r.width * 170f / 380f;
            micro.uvRect = new Rect(r.x, r.y + (r.height - h) / 2f, r.width, h);
        }
    }

    void AplicarRecorte(float s)
    {
        float lim = Mathf.Max(0f, (1f - s) / 2f);
        pan = new Vector2(Mathf.Clamp(pan.x, -lim, lim), Mathf.Clamp(pan.y, -lim, lim));
        vista.uvRect = new Rect(0.5f - s / 2f + pan.x, 0.5f - s / 2f + pan.y, s, s);
    }

    // X: alterna entre mover la muestra y mover la vista. Al volver a la muestra, el HUD se recentra.
    public void AlternarModo()
    {
        modoVista = !modoVista;
        RefrescarModo();
    }

    // Joystick en modo vista: a la derecha se ve lo de la derecha (el HUD se desplaza al lado contrario).
    public void MoverVista(Vector2 eje)
    {
        if (eje.sqrMagnitude < 0.04f) return;
        desplazamiento += eje * (VelocidadVista * Time.deltaTime);
        desplazamiento = new Vector2(Mathf.Clamp(desplazamiento.x, -MaxDesplazamiento.x, MaxDesplazamiento.x),
                                     Mathf.Clamp(desplazamiento.y, -MaxDesplazamiento.y, MaxDesplazamiento.y));
    }

    void RefrescarModo()
    {
        if (modoTxt == null) return;
        modoTxt.text = modoVista ? "MOVIENDO LA VISTA  ·  Joystick izq.  ·  X: volver a la muestra" : "X: mover la vista";
        modoTxt.color = modoVista ? Color.black : TextoGris;
        modoFondo.enabled = modoVista;
    }

    public void CambiarPestana(int d)
    {
        pestana = (pestana + d + 4) % 4;
        Refrescar();
    }

    void Update()
    {
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, Time.deltaTime * 4f);
        // Transición suave del zoom (en escala logarítmica: cada objetivo tarda lo mismo).
        if (actual != null && !Mathf.Approximately(zoomActual, zoomObjetivo))
        {
            float l = Mathf.Lerp(Mathf.Log(zoomActual), Mathf.Log(zoomObjetivo), 1f - Mathf.Exp(-9f * Time.deltaTime));
            zoomActual = Mathf.Abs(l - Mathf.Log(zoomObjetivo)) < 0.002f ? zoomObjetivo : Mathf.Exp(l);
            AplicarZoom();
        }
        if (enFresco && proveedorReto == null && grupo.alpha > 0f) AnimarCelulas(Time.deltaTime);
        else if (celulasRaiz != null && celulasRaiz.gameObject.activeSelf) celulasRaiz.gameObject.SetActive(false);
        if (!modoVista) desplazamiento = Vector2.MoveTowards(desplazamiento, Vector2.zero, Time.deltaTime * 1.5f);
        var objetivo = new Vector3(-desplazamiento.x, -desplazamiento.y, Distancia);
        transform.localPosition = Vector3.Lerp(transform.localPosition, objetivo, 1f - Mathf.Exp(-12f * Time.deltaTime));
        if (alphaObjetivo == 0f && grupo.alpha == 0f)
        {
            VisorVirus.Obtener().Activo(false);
            gameObject.SetActive(false);
        }
    }

    void Refrescar()
    {
        var m = actual;
        if (m == null) return;
        subtitulo.text = $"MUESTRA {m.codigo}  ·  {m.nombre.ToUpper()}" + (enFresco && proveedorReto == null ? "  ·  <color=#7CF29A>EN FRESCO: vivas, sin teñir</color>" : "");
        escalaTxt.text = m.Escalas[aumento];
        if (m.Vista3D) vista.uvRect = new Rect(0, 0, 1, 1);
        // Recorte: más aumento = trozo más pequeño de la micrografía, desplazado por 'pan' (con el zoom actual).
        AplicarZoom();

        for (int i = 0; i < claves.Length; i++)
        {
            bool hay = m.caracteristicas != null && i < m.caracteristicas.Length;
            claves[i].text = hay ? m.caracteristicas[i].clave + ":" : "";
            valores[i].text = hay ? m.caracteristicas[i].valor : "";
        }
        riesgoTxt.text = m.riesgo;

        tabTitulo.text = MuestraData.NombresPestanas[pestana];
        tabTexto.text = m.Pestana(pestana);
        // COMPOSICIÓN de un virus: leyenda con el mismo color que cada parte del modelo 3D.
        bool leyenda = proveedorReto == null && pestana == 2 && m.tipo == TipoMuestra.Virus && m.componentes != null && m.componentes.Length > 0;
        tabTexto.gameObject.SetActive(!leyenda);
        var pal = ModeloVirus.Paleta(m);
        for (int i = 0; i < leyendaTxt.Length; i++)
        {
            bool hay = leyenda && i < m.componentes.Length;
            leyendaPunto[i].gameObject.SetActive(hay);
            leyendaTxt[i].gameObject.SetActive(hay);
            if (!hay) continue;
            leyendaPunto[i].color = pal[i];
            leyendaTxt[i].text = m.componentes[i];
        }
        for (int i = 0; i < 4; i++)
        {
            bool sel = i == pestana;
            tabs[i].color = sel ? new Color(0.2f, 0.6f, 0.85f, 0.55f) : new Color(0.03f, 0.1f, 0.18f, 0.85f);
            tabsTxt[i].color = sel ? Color.white : TextoGris;
        }

        obsTxt.text = m.observaciones;
        micro.texture = m.micrografia;
        micro.uvRect = RecorteAspecto(m.micrografia, 380f / 190f);
        microTitulo.text = m.Vista3D ? "MICROGRAFÍA ELECTRÓNICA REAL" : "MICROGRAFÍA ÓPTICA REAL";
        creditoTxt.text = m.creditoImagen;

        bool reto = proveedorReto != null;
        pestanas.SetActive(!reto);
        PonerRiesgo("Riesgo", null);
        micro.enabled = true;
        obsTit.text = "OBSERVACIONES";
        avisoTxt.text = "";
        guiaTxt.text = "";
        if (reto) RefrescarReto(proveedorReto(aumento));
    }

    void RefrescarReto(DatosReto d)
    {
        tabTexto.gameObject.SetActive(true);
        foreach (var i in leyendaPunto) i.gameObject.SetActive(false);
        foreach (var t in leyendaTxt) t.gameObject.SetActive(false);
        subtitulo.text = d.subtitulo;
        if (d.vista != null) vista.texture = d.vista;
        for (int i = 0; i < claves.Length; i++)
        {
            bool hay = d.claves != null && i < d.claves.Length;
            claves[i].text = hay ? d.claves[i] + ":" : "";
            valores[i].text = hay ? d.valores[i] : "";
        }
        riesgoTxt.text = d.riesgo;
        PonerRiesgo(d.riesgoClave ?? "Riesgo", d.colorRiesgo);
        tabTitulo.text = d.cuadernoTitulo;
        tabTexto.text = d.cuaderno;
        obsTit.text = d.obsTitulo;
        obsTxt.text = d.obs;
        avisoTxt.text = d.aviso;
        avisoTxt.color = d.colorAviso;
        creditoTxt.text = "";
        comparando = d.antes != null;
        if (d.antes != null)
        {
            // Mismo recorte que la vista principal para comparar a igual aumento.
            micro.texture = d.antes;
            var r = vista.uvRect;
            float h = r.width * 170f / 380f;
            micro.uvRect = new Rect(r.x, r.y + (r.height - h) / 2f, r.width, h);
            microTitulo.text = d.antesTitulo;
        }
        else
        {
            micro.enabled = false;
            microTitulo.text = "GUÍA DE OBSERVACIÓN";
            guiaTxt.text = d.guia;
        }
    }

    // ---------------- Examen en fresco ----------------

    static string Dato(MuestraData m, string clave)
    {
        if (m.caracteristicas != null)
            foreach (var d in m.caracteristicas) if (d.clave == clave) return d.valor ?? "";
        return "";
    }

    // Reparte grupos (células sueltas, cadenas o racimos) por la muestra según la forma y la movilidad de la ficha.
    void IniciarCelulas(MuestraData m)
    {
        string forma = Dato(m, "Forma").ToLowerInvariant();
        string movilidad = Dato(m, "Movilidad").ToLowerInvariant();
        bool bacilo = forma.Contains("bacilo");
        bool racimos = forma.Contains("racimo");
        bool cadenas = forma.Contains("cadena");
        bool movil = !(movilidad.Contains("inmóvil") || movilidad.Contains("inmovil") || movilidad.StartsWith("no"));
        float largo = bacilo ? (Dato(m, "Tamaño").Contains("3") ? 2.8f : 2.0f) : 1.0f;
        float ancho = bacilo ? 0.8f : 1.0f;
        var sprite = bacilo ? Capsula() : CelulaRedonda();

        var lista = new System.Collections.Generic.List<Grupo>();
        nCelulas = 0;
        var rnd = new System.Random(m.name.GetHashCode());
        float R() => (float)rnd.NextDouble();
        while (nCelulas < MaxCelulas - 8)
        {
            int g = lista.Count;
            int n = racimos ? 4 + rnd.Next(5) : cadenas ? 1 + rnd.Next(3) : 1;
            var gr = new Grupo { pos = new Vector2(R() * Campo, R() * Campo), ang = R() * 360f, nada = movil, tRun = R() * 1.5f };
            // Velocidades didácticas (algo menores que las reales, ~20-30 µm/s) para poder seguirlas.
            gr.velocidad = movil ? (n == 1 ? 9f + R() * 6f : 4f + R() * 2f) : 0f;
            lista.Add(gr);
            for (int k = 0; k < n && nCelulas < MaxCelulas; k++)
            {
                Vector2 off;
                if (racimos) off = k == 0 ? Vector2.zero : new Vector2(R() - 0.5f, R() - 0.5f).normalized * (0.85f + R() * 0.6f) + new Vector2(R() - 0.5f, R() - 0.5f) * 0.3f;
                else off = new Vector2((k - (n - 1) / 2f) * (largo + 0.15f), 0f);   // cadena alineada con el grupo
                celula[nCelulas] = new Celula { grupo = g, offset = off, largo = largo * (0.9f + R() * 0.25f), ancho = ancho * (0.9f + R() * 0.2f), angulo = racimos ? R() * 360f : 0f };
                celulas[nCelulas].sprite = sprite;
                celulas[nCelulas].color = new Color(0.78f, 0.8f, 0.8f, 0.92f);
                nCelulas++;
            }
        }
        grupos = lista.ToArray();
        for (int i = nCelulas; i < MaxCelulas; i++) celulas[i].gameObject.SetActive(false);
    }

    void AnimarCelulas(float dt)
    {
        if (!celulasRaiz.gameObject.activeSelf) celulasRaiz.gameObject.SetActive(true);
        // Grupos: nadar en tramos rectos y cambiar de rumbo (run & tumble), más movimiento browniano.
        for (int g = 0; g < grupos.Length; g++)
        {
            var gr = grupos[g];
            if (gr.nada)
            {
                gr.tRun -= dt;
                if (gr.tRun <= 0f)
                {
                    if (gr.tGiro <= 0f) { gr.tGiro = Random.Range(0.12f, 0.3f); gr.giro = Random.Range(-110f, 110f) / gr.tGiro; }
                    gr.tGiro -= dt;
                    gr.ang += gr.giro * dt;
                    if (gr.tGiro <= 0f) gr.tRun = Random.Range(0.8f, 2f);
                }
                else
                {
                    gr.ang += Random.Range(-25f, 25f) * dt;
                    float a = gr.ang * Mathf.Deg2Rad;
                    gr.pos += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * gr.velocidad * dt;
                }
            }
            gr.pos += Random.insideUnitCircle * (0.9f * Mathf.Sqrt(dt));   // browniano (~1 µm)
            if (!gr.nada) gr.ang += Random.Range(-8f, 8f) * dt;
            gr.pos = new Vector2(Mathf.Repeat(gr.pos.x, Campo), Mathf.Repeat(gr.pos.y, Campo));
            grupos[g] = gr;
        }

        // A píxeles de la vista, con el aumento y el desplazamiento actuales (uvRect).
        var uv = vista.uvRect;
        float pxPorUm = Vista / (Campo * Mathf.Max(0.01f, uv.width));
        for (int i = 0; i < nCelulas; i++)
        {
            var c = celula[i];
            var gr = grupos[c.grupo];
            float a = gr.ang * Mathf.Deg2Rad;
            var rot = new Vector2(c.offset.x * Mathf.Cos(a) - c.offset.y * Mathf.Sin(a), c.offset.x * Mathf.Sin(a) + c.offset.y * Mathf.Cos(a));
            var s = (gr.pos + rot) / Campo;
            float x = ((s.x - uv.x) / uv.width - 0.5f) * Vista;
            float y = ((s.y - uv.y) / uv.height - 0.5f) * Vista;
            var img = celulas[i];
            bool visible = x * x + y * y < 235f * 235f;
            if (img.gameObject.activeSelf != visible) img.gameObject.SetActive(visible);
            if (!visible) continue;
            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(Mathf.Max(2f, c.largo * pxPorUm), Mathf.Max(2f, c.ancho * pxPorUm));
            rt.localRotation = Quaternion.Euler(0f, 0f, gr.ang + c.angulo);
        }
    }

    // Fondo claro del campo en fresco (luz transmitida, sin colorante) con algo de grano.
    static Texture2D fondoFresco;
    static Texture2D FondoFresco()
    {
        if (fondoFresco != null) return fondoFresco;
        const int n = 256;
        fondoFresco = new Texture2D(n, n, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Repeat };
        var rnd = new System.Random(3);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float g = (float)rnd.NextDouble() * 0.03f + Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.04f;
                px[y * n + x] = new Color(0.84f + g, 0.85f + g, 0.81f + g);
            }
        fondoFresco.SetPixels32(px);
        fondoFresco.Apply();
        return fondoFresco;
    }

    // Célula sin teñir: centro claro y borde oscuro (como se ve con el diafragma cerrado).
    static Sprite celulaRedonda, capsula;
    static Sprite CelulaRedonda()
    {
        if (celulaRedonda == null) celulaRedonda = SpriteCelula(64, 64, (x, y) => Mathf.Sqrt(x * x + y * y));
        return celulaRedonda;
    }

    static Sprite Capsula()
    {
        // Distancia a un segmento horizontal: bastón con extremos redondeados (proporción 3:1).
        if (capsula == null) capsula = SpriteCelula(192, 64, (x, y) => { float dx = Mathf.Max(0f, Mathf.Abs(x) * 3f - 2f); return Mathf.Sqrt(dx * dx + y * y); });
        return capsula;
    }

    // 'dist' recibe coordenadas normalizadas (-1..1) y devuelve 1 en el borde de la célula.
    static Sprite SpriteCelula(int w, int h, System.Func<float, float, float> dist)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = dist(((x + 0.5f) / w - 0.5f) * 2f, ((y + 0.5f) / h - 0.5f) * 2f);
                float alfa = Mathf.Clamp01((1f - d) * 12f);
                float borde = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.95f, d));
                float v = Mathf.Lerp(0.95f, 0.22f, borde);
                t.SetPixel(x, y, new Color(v, v, v * 1.02f, alfa));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    void PonerRiesgo(string clave, Color? color)
    {
        riesgoEtq.text = clave + ":";
        var c = color ?? new Color(1f, 0.3f, 0.3f);
        riesgoFondo.color = color.HasValue ? new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 0.85f) : RiesgoFondoColor;
        foreach (var i in riesgoMarco) i.color = color.HasValue ? new Color(c.r, c.g, c.b, 0.9f) : RiesgoMarcoColor;
        riesgoTxt.color = color.HasValue ? Color.Lerp(c, Color.white, 0.4f) : new Color(1f, 0.6f, 0.6f);
    }

    static Rect RecorteAspecto(Texture t, float aspecto)
    {
        if (t == null) return new Rect(0, 0, 1, 1);
        float a = (float)t.width / t.height;
        if (a > aspecto) { float w = aspecto / a; return new Rect((1 - w) / 2, 0, w, 1); }
        float h = a / aspecto; return new Rect(0, (1 - h) / 2, 1, h);
    }

    // ---------------- Construcción ----------------

    void Construir()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        var rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(1400, 640);
        var raiz = transform;

        Img(raiz, "Fondo", 0, 0, 1400, 640, new Color(0.01f, 0.025f, 0.05f, 1f));   // opaco: no se transparenta el microscopio
        var vin = Rect(raiz, "Vineta", 0, 0, 1400, 640).gameObject.AddComponent<RawImage>();
        vin.texture = Vineta(); vin.raycastTarget = false;
        // Borde de la lente del ocular (anillo tenue sobre la viñeta).
        var lente = Rect(raiz, "Lente", 0, 0, 1400, 640).gameObject.AddComponent<RawImage>();
        lente.texture = Lente(); lente.color = new Color(0.45f, 0.85f, 1f, 0.35f); lente.raycastTarget = false;

        // Cabecera
        Txt(raiz, "Titulo", -420, 285, 560, 46, "ANÁLISIS MICROSCÓPICO", 34, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        subtitulo = Txt(raiz, "Subtitulo", -420, 250, 560, 30, "", 20, TextoClaro, TextAlignmentOptions.Left);
        Img(raiz, "LineaTitulo", -420, 230, 560, 2, CianSuave);

        // Zoom
        Img(raiz, "ZoomFondo", 330, 262, 190, 74, Panel);
        Marco(raiz, 330, 262, 190, 74, CianSuave);
        Esquinas(raiz, 330, 262, 190, 74);
        Txt(raiz, "ZoomEtq", 330, 285, 170, 20, "ZOOM   <color=#9FB4C4>A: +   B: -</color>", 15, Cian, TextAlignmentOptions.Left);
        zoomTxt = Txt(raiz, "Zoom", 330, 257, 170, 38, "", 30, Color.white, TextAlignmentOptions.Left, FontStyles.Bold);
        // Barra de zoom: del objetivo menor al mayor.
        Img(raiz, "ZoomBarraFondo", 330, 236, AnchoBarraZoom, 6, new Color(1f, 1f, 1f, 0.15f));
        zoomRelleno = Img(raiz, "ZoomBarra", 330, 236, AnchoBarraZoom, 6, Cian).rectTransform;

        // Vista circular
        vista = Rect(raiz, "Vista", 0, 25, 440, 440).gameObject.AddComponent<RawImage>();
        vista.raycastTarget = false;
        // Células del examen en fresco: encima de la muestra y debajo del recorte circular.
        celulasRaiz = Rect(raiz, "Celulas", 0, 25, 440, 440);
        for (int i = 0; i < MaxCelulas; i++)
        {
            celulas[i] = Img(celulasRaiz, "Celula" + i, 0, 0, 10, 10, Color.white);
            celulas[i].gameObject.SetActive(false);
        }
        celulasRaiz.gameObject.SetActive(false);
        // Recorte circular con una imagen opaca fuera del círculo (no con Mask: el stencil
        // de la máscara pierde el "dibujar siempre encima" y el microscopio tapaba la vista).
        Img(raiz, "RecorteCircular", 0, 25, 446, 446, new Color(0.01f, 0.025f, 0.05f, 1f), CirculoInverso());
        Img(raiz, "Anillo", 0, 25, 474, 474, new Color(0.35f, 0.85f, 1f, 0.7f), Anillo());
        Img(raiz, "Cruz1", 0, 25 + 250, 2, 34, Cian); Img(raiz, "Cruz2", 0, 25 - 250, 2, 34, Cian);
        Img(raiz, "Cruz3", 250, 25, 34, 2, Cian); Img(raiz, "Cruz4", -250, 25, 34, 2, Cian);
        for (int i = 0; i < 9; i++) Img(raiz, "Regla" + i, -290, -125 + i * 37.5f, i % 4 == 0 ? 22 : 12, 2, CianSuave);
        Img(raiz, "Escala", 0, -168, 150, 3, Color.white);
        Img(raiz, "EscalaI", -75, -168, 3, 14, Color.white); Img(raiz, "EscalaD", 75, -168, 3, 14, Color.white);
        escalaTxt = Txt(raiz, "EscalaTxt", 0, -148, 200, 26, "", 20, Color.white, TextAlignmentOptions.Center);

        // Panel izquierdo: características
        Txt(raiz, "CaractTit", -480, 188, 400, 30, "CARACTERÍSTICAS PRINCIPALES", 21, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        Img(raiz, "CaractFondo", -480, 70, 420, 210, Panel);
        Marco(raiz, -480, 70, 420, 210, CianSuave);
        Esquinas(raiz, -480, 70, 420, 210);
        for (int i = 0; i < 6; i++)
        {
            float y = 155 - i * 28;
            claves[i] = Txt(raiz, "Clave" + i, -585, y, 190, 28, "", 17, TextoGris, TextAlignmentOptions.Left);
            valores[i] = Txt(raiz, "Valor" + i, -395, y, 230, 28, "", 17, TextoClaro, TextAlignmentOptions.Left);
        }
        riesgoEtq = Txt(raiz, "RiesgoEtq", -585, -18, 190, 30, "Riesgo:", 17, TextoGris, TextAlignmentOptions.Left);
        riesgoFondo = Img(raiz, "RiesgoFondo", -395, -18, 220, 28, RiesgoFondoColor);
        riesgoMarco = Marco(raiz, -395, -18, 220, 28, RiesgoMarcoColor);
        riesgoTxt = Txt(raiz, "Riesgo", -395, -18, 210, 28, "", 16, new Color(1f, 0.6f, 0.6f), TextAlignmentOptions.Center, FontStyles.Bold);

        // Panel izquierdo: pestaña actual
        Img(raiz, "TabFondo", -480, -140, 420, 170, Panel);
        Marco(raiz, -480, -140, 420, 170, CianSuave);
        Esquinas(raiz, -480, -140, 420, 170);
        tabTitulo = Txt(raiz, "TabTitulo", -480, -73, 390, 28, "", 20, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        tabTexto = Txt(raiz, "TabTexto", -480, -152, 390, 130, "", 16, TextoClaro, TextAlignmentOptions.TopLeft);
        for (int i = 0; i < leyendaTxt.Length; i++)
        {
            float y = -103 - i * 25;
            leyendaPunto[i] = Img(raiz, "LeyendaPunto" + i, -672, y, 14, 14, Color.white, Circulo());
            leyendaTxt[i] = Txt(raiz, "Leyenda" + i, -462, y, 390, 24, "", 17, TextoClaro, TextAlignmentOptions.Left);
            leyendaPunto[i].gameObject.SetActive(false);
            leyendaTxt[i].gameObject.SetActive(false);
        }

        // Panel derecho: micrografía real y observaciones
        microTitulo = Txt(raiz, "MicroTit", 480, 188, 400, 30, "", 19, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        Img(raiz, "MicroFondo", 480, 70, 420, 210, Panel);
        Marco(raiz, 480, 70, 420, 210, CianSuave);
        Esquinas(raiz, 480, 70, 420, 210);
        micro = Rect(raiz, "Micro", 480, 80, 380, 170).gameObject.AddComponent<RawImage>();
        micro.raycastTarget = false;
        creditoTxt = Txt(raiz, "Credito", 480, -22, 390, 18, "", 10, TextoGris, TextAlignmentOptions.Left);
        guiaTxt = Txt(raiz, "Guia", 480, 70, 390, 190, "", 17, TextoClaro, TextAlignmentOptions.TopLeft);
        Img(raiz, "ObsFondo", 480, -140, 420, 170, Panel);
        Marco(raiz, 480, -140, 420, 170, CianSuave);
        Esquinas(raiz, 480, -140, 420, 170);
        obsTit = Txt(raiz, "ObsTit", 480, -73, 390, 28, "OBSERVACIONES", 20, Cian, TextAlignmentOptions.Left, FontStyles.Bold);
        obsTxt = Txt(raiz, "Obs", 480, -152, 390, 130, "", 16, TextoClaro, TextAlignmentOptions.TopLeft);

        // Pestañas (debajo de los paneles, entre ellos)
        var pest = Rect(raiz, "Pestanas", 0, 0, 1400, 640);
        pestanas = pest.gameObject;
        for (int i = 0; i < 4; i++)
        {
            float x = -255f + i * 170f;
            tabs[i] = Img(pest, "Tab" + i, x, -262, 162, 40, Panel);
            Marco(pest, x, -262, 162, 40, CianSuave);
            tabsTxt[i] = Txt(pest, "TabTxt" + i, x, -262, 158, 38, MuestraData.NombresPestanas[i], 15, TextoGris, TextAlignmentOptions.Center, FontStyles.Bold);
        }
        // Aviso del reto: ocupa el hueco de las pestañas, que en modo reto se ocultan.
        avisoTxt = Txt(raiz, "Aviso", 0, -258, 720, 56, "", 19, Color.white, TextAlignmentOptions.Center, FontStyles.Bold);

        // Pie
        Txt(raiz, "Pie", -520, -305, 360, 20, "LAB. DE BIOCIENCIAS  |  NANOLAB", 13, TextoGris, TextAlignmentOptions.Left);
        Txt(raiz, "Ayuda", 0, -305, 820, 20, "Joystick izq.: mover la muestra     X: mover la vista     Joystick der.: arriba/abajo zoom +/-, lados pestañas     A: más aumento     B / Y: menos     Aléjate del ocular", 13, TextoGris, TextAlignmentOptions.Center);
        // Indicador del modo (arriba, centrado): se resalta mientras se mueve la vista.
        modoFondo = Img(raiz, "ModoFondo", 0, 302, 450, 26, Cian);
        modoTxt = Txt(raiz, "Modo", 0, 302, 444, 24, "", 14, TextoGris, TextAlignmentOptions.Center, FontStyles.Bold);
        RefrescarModo();
        Img(raiz, "Led", 468, -305, 12, 12, new Color(0.25f, 1f, 0.5f), Circulo());
        Txt(raiz, "Captura", 575, -305, 200, 20, "IMAGEN CAPTURADA", 13, new Color(0.25f, 1f, 0.5f), TextAlignmentOptions.Left);

        DibujarSiempreEncima();
        gameObject.SetActive(false);
    }

    // El HUD está a 0,6 m de los ojos, pero el microscopio, la mesa o las manos quedan más cerca
    // al acercar la cara al ocular y lo tapaban. Se dibuja sin prueba de profundidad y al final
    // (cola Overlay), como los visores pegados a la cara.
    void DibujarSiempreEncima()
    {
        const int Always = (int)UnityEngine.Rendering.CompareFunction.Always;
        var ui = new Material(Canvas.GetDefaultCanvasMaterial()) { name = "HUD_SiempreEncima" };
        ui.SetInt("unity_GUIZTestMode", Always);
        ui.renderQueue = 4000;
        var textos = new System.Collections.Generic.Dictionary<Material, Material>();
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            if (g is TMP_Text t)
            {
                var src = t.fontSharedMaterial;
                if (src == null) continue;
                if (!textos.TryGetValue(src, out var m))
                {
                    m = new Material(src) { name = src.name + "_SiempreEncima" };
                    m.SetInt("unity_GUIZTestMode", Always);
                    m.renderQueue = 4000;
                    textos[src] = m;
                }
                t.fontSharedMaterial = m;
            }
            else g.material = ui;
        }
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static Image Img(Transform parent, string name, float x, float y, float w, float h, Color c, Sprite s = null)
    {
        var img = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
        img.color = c; img.sprite = s; img.raycastTarget = false;
        return img;
    }

    static Image[] Marco(Transform parent, float x, float y, float w, float h, Color c)
    {
        return new[]
        {
            Img(parent, "MarcoS", x, y + h / 2, w, 2, c),
            Img(parent, "MarcoI", x, y - h / 2, w, 2, c),
            Img(parent, "MarcoIz", x - w / 2, y, 2, h, c),
            Img(parent, "MarcoD", x + w / 2, y, 2, h, c),
        };
    }

    // Esquinas brillantes (escuadras) sobre el marco de un panel, estilo visor científico.
    static void Esquinas(Transform parent, float x, float y, float w, float h, float largo = 18f)
    {
        foreach (var sx in new[] { -1f, 1f })
            foreach (var sy in new[] { -1f, 1f })
            {
                float cx = x + sx * w / 2f, cy = y + sy * h / 2f;
                Img(parent, "EsquinaH", cx - sx * largo / 2f, cy, largo, 3, Cian);
                Img(parent, "EsquinaV", cx, cy - sy * largo / 2f, 3, largo, Cian);
            }
    }

    static TMP_Text Txt(Transform parent, string name, float x, float y, float w, float h, string texto, float tam, Color c,
                        TextAlignmentOptions al, FontStyles estilo = FontStyles.Normal)
    {
        var t = Rect(parent, name, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
        t.text = texto; t.fontSize = tam; t.color = c; t.alignment = al; t.fontStyle = estilo;
        t.enableWordWrapping = true; t.raycastTarget = false;
        return t;
    }

    // ---------------- Texturas procedurales ----------------

    static Sprite circulo, anillo;
    static Texture2D vineta;

    public static Sprite Circulo()
    {
        if (circulo == null) circulo = SpriteRadial(256, (r) => Mathf.Clamp01((0.5f - r) * 256f / 2f));
        return circulo;
    }

    static Sprite circuloInverso;

    static Sprite CirculoInverso()
    {
        if (circuloInverso == null) circuloInverso = SpriteRadial(256, (r) => Mathf.Clamp01((r - 0.5f) * 256f / 2f));
        return circuloInverso;
    }

    static Sprite Anillo()
    {
        if (anillo == null) anillo = SpriteRadial(256, (r) => Mathf.Clamp01(1f - Mathf.Abs(r - 0.485f) * 256f / 3f));
        return anillo;
    }

    // Distancia elíptica normalizada del ocular (1 = borde de la lente); los paneles quedan dentro.
    static float DistOcular(int x, int y, int w, int h)
    {
        float dx = ((x + 0.5f) / w - 0.5f) * 2f, dy = ((y + 0.5f) / h - 0.5f) * 2f;
        return Mathf.Sqrt(dx * dx * 0.55f + dy * dy * 0.8f);
    }

    // Viñeta del ocular: oscurece suavemente hacia el borde y cierra en negro fuera de la lente.
    static Texture2D Vineta()
    {
        if (vineta != null) return vineta;
        const int w = 512, h = 256;
        vineta = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = DistOcular(x, y, w, h);
                float a = Mathf.Clamp01(Mathf.SmoothStep(0f, 1f, (d - 0.98f) / 0.18f) * 0.95f + Mathf.SmoothStep(0f, 1f, (d - 0.6f) / 0.4f) * 0.15f);
                px[y * w + x] = new Color32(0, 0, 0, (byte)(a * 255));
            }
        vineta.SetPixels32(px);
        vineta.Apply();
        return vineta;
    }

    static Texture2D lente;

    // Anillo fino en el borde de la lente (lo tiñe el RawImage).
    static Texture2D Lente()
    {
        if (lente != null) return lente;
        const int w = 1024, h = 512;
        lente = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = DistOcular(x, y, w, h) - 1.0f;
                float a = Mathf.Exp(-(d * d) / (0.006f * 0.006f)) + Mathf.Exp(-((d + 0.02f) * (d + 0.02f)) / (0.012f * 0.012f)) * 0.25f;
                px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255));
            }
        lente.SetPixels32(px);
        lente.Apply();
        return lente;
    }

    static Sprite SpriteRadial(int n, System.Func<float, float> alpha)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n - 0.5f, dy = (y + 0.5f) / n - 0.5f;
                t.SetPixel(x, y, new Color(1, 1, 1, alpha(Mathf.Sqrt(dx * dx + dy * dy))));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }
}
