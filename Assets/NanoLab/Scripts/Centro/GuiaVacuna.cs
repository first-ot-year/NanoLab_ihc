using System.Collections;
using TMPro;
using UnityEngine;
using K = KitVacuna;
using I = EstiloNanoLab.Icono;

// Ayudas del CASO 01 para que el estudiante NO tenga que leer todo:
//  - VentanaGuia: apartado FIJO en la estación ("AHORA"), bajo la pantalla del caso: un solo texto corto (la acción
//    actual) con el icono del botón del mando que hay que usar. No sigue la vista.
//  - DemoAyuda: animación de la acción (de dónde a dónde) con una copia fantasma del objeto y rótulos de niño
//    ("COGE ESTO" > "LLÉVALO" > "PONLO AQUÍ"). AYUDA la enseña 3 veces; en PASO A PASO se repite sin parar.
//  - VirusHolograma: el virus del paso ANTÍGENO se toca con la mano (o se apunta con el rayo) y se gira con el grip.

// ---------------------------------------------------------------------------------------------- apartado "AHORA"

public class VentanaGuia : MonoBehaviour
{
    const float Ancho = 1.12f, Alto = 0.13f;
    TMP_Text paso, texto;
    SpriteRenderer icono;
    bool ver;
    float escala, pop;
    string ultimo;

    public static VentanaGuia Crear(Transform padre, Vector3 pos, Quaternion rot)
    {
        var g = new GameObject("ApartadoAhora").AddComponent<VentanaGuia>();
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = rot;
        g.Montar();
        return g;
    }

    void Montar()
    {
        K.Panel(transform, "Fondo", Vector3.zero, Ancho, Alto, new Color(0.04f, 0.1f, 0.2f, 0.96f), new Color(1f, 0.82f, 0.35f, 1f), 0.003f);
        K.Texto(transform, "<b>AHORA</b>", new Vector3(-Ancho / 2f + 0.07f, 0.035f, -0.004f), 0.15f, K.Amarillo, 0.12f, 0.03f, TextAlignmentOptions.Center, true);
        icono = EstiloNanoLab.CrearIcono(transform, "Icono", new Vector3(-Ancho / 2f + 0.07f, -0.015f, -0.004f), 0.075f);
        paso = K.Texto(transform, "", new Vector3(0.07f, 0.042f, -0.004f), 0.13f, K.Gris, Ancho - 0.18f, 0.03f, TextAlignmentOptions.Left, true);
        texto = K.Texto(transform, "", new Vector3(0.07f, -0.012f, -0.004f), 0.26f, Color.white, Ancho - 0.18f, 0.075f, TextAlignmentOptions.Left, true);
        K.AutoTam(texto, 0.15f);
        transform.localScale = Vector3.zero;
    }

    public void Mostrar(bool mostrar, string pasoTxt, string accion, I ic)
    {
        ver = mostrar;
        if (!mostrar) return;
        paso.text = pasoTxt;
        texto.text = accion;
        EstiloNanoLab.Poner(icono, ic);
        if (accion != ultimo) { ultimo = accion; pop = 1f; }   // pequeño "pop" al cambiar de acción
    }

    void Update()
    {
        escala = Mathf.MoveTowards(escala, ver ? 1f : 0f, Time.deltaTime * 4f);
        pop = Mathf.MoveTowards(pop, 0f, Time.deltaTime * 3f);
        transform.localScale = Vector3.one * (Mathf.SmoothStep(0f, 1f, escala) * (1f + 0.06f * Mathf.Sin(pop * Mathf.PI)));
    }
}

// ---------------------------------------------------------------------------------------------- demostración

public class DemoAyuda : MonoBehaviour
{
    Transform fantasma, copia, destino, origenCopia;
    SpriteRenderer icono;
    TMP_Text rotulo, rotuloDestino;
    Renderer bola, anillo;
    public bool Reproduciendo { get; private set; }
    // Ancla dentro de la pantalla derecha (arriba, bajo el título): si la animación empieza ahí, es un rótulo
    // plano dentro de la pantalla ("MIRA LAS BARRAS"), sin bola, que no se sale del marco ni tapa la gráfica.
    public Transform pantalla;

    public static DemoAyuda Crear(Transform padre)
    {
        var d = new GameObject("DemoAyuda").AddComponent<DemoAyuda>();
        d.transform.SetParent(padre, false);
        d.Montar();
        return d;
    }

    void Montar()
    {
        var cian = new Color(0.45f, 0.95f, 1f);
        fantasma = K.Grupo(transform, "Fantasma", Vector3.zero);
        bola = K.Esfera(fantasma, "Bola", Vector3.zero, 0.05f, K.Vidrio(new Color(cian.r, cian.g, cian.b, 0.45f)));
        anillo = K.Pieza(fantasma, "Anillo", PrimitiveType.Cylinder, new Vector3(0f, -0.03f, 0f), new Vector3(0.1f, 0.001f, 0.1f), K.Plano(cian));
        icono = EstiloNanoLab.CrearIcono(fantasma, "Icono", new Vector3(0f, 0.09f, 0f), 0.1f);
        rotulo = Rotulo(fantasma, new Vector3(0f, 0.19f, 0f));
        copia = K.Grupo(fantasma, "Copia", Vector3.zero);
        fantasma.gameObject.SetActive(false);

        // Marca del destino: anillo amarillo que late y su rótulo ("PONLO AQUÍ").
        destino = K.Grupo(transform, "Destino", Vector3.zero);
        K.Pieza(destino, "Anillo", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.11f, 0.001f, 0.11f), K.Plano(K.Amarillo));
        rotuloDestino = Rotulo(destino, new Vector3(0f, 0.12f, 0f));
        rotuloDestino.color = K.Amarillo;
        destino.gameObject.SetActive(false);
    }

    static TMP_Text Rotulo(Transform padre, Vector3 pos)
    {
        var t = K.Texto(padre, "", pos, 0.3f, Color.white, 0.6f, 0.08f, TextAlignmentOptions.Bottom, true);
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 235);
        t.sortingOrder = AdaptadorEscena.OrdenTexto + 2;
        return t;
    }

    // Rótulos por defecto según el botón del mando.
    public static string TextoDesde(I ic) =>
        ic == I.Grip ? "COGE ESTO" : ic == I.Gatillo ? "PUNTA AQUÍ + GATILLO" : ic == I.Pulsar ? "PULSA AQUÍ" : ic == I.Rayo ? "APUNTA + GATILLO" : "MIRA AQUÍ";
    public static string TextoHasta(I ic) =>
        ic == I.Grip ? "PONLO AQUÍ" : ic == I.Gatillo ? "AQUÍ + GATILLO" : "AQUÍ";

    // Muestra el gesto: de 'desde' a 'hasta' (arco) o, sin destino, un pulso sobre 'desde'. veces < 0 = sin parar.
    public void Reproducir(Transform desde, Transform hasta, I ic, int veces = 3, string txtDesde = null, string txtHasta = null)
    {
        if (desde == null) return;
        StopAllCoroutines();
        StartCoroutine(Animar(desde, hasta, ic, veces, txtDesde ?? TextoDesde(ic), txtHasta ?? TextoHasta(ic)));
    }

    public void Detener()
    {
        StopAllCoroutines();
        if (fantasma != null) fantasma.gameObject.SetActive(false);
        if (destino != null) destino.gameObject.SetActive(false);
        Reproduciendo = false;
    }

    // Copia transparente del objeto que se agarra (solo piezas pequeñas: bloque, tubo, vial, cartucho, pipeta).
    void PrepararCopia(Transform desde, I ic)
    {
        if (origenCopia == desde) { copia.gameObject.SetActive(origenCopia != null && copia.childCount > 0); return; }
        for (int i = copia.childCount - 1; i >= 0; i--) Destroy(copia.GetChild(i).gameObject);
        origenCopia = desde;
        copia.gameObject.SetActive(false);
        if (desde == null || (ic != I.Grip && ic != I.Gatillo)) return;
        var rs = desde.GetComponentsInChildren<MeshRenderer>(false);
        if (rs.Length == 0 || rs.Length > 40) return;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        if (b.size.magnitude > 0.4f) return;   // máquinas y muebles: solo la bola
        var mat = K.Vidrio(new Color(0.45f, 0.95f, 1f, 0.35f));
        int n = 0;
        foreach (var r in rs)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || r.GetComponent<TMP_Text>() != null) continue;
            var go = new GameObject("Pieza");
            go.transform.SetParent(copia, false);
            var m = desde.worldToLocalMatrix * r.transform.localToWorldMatrix;
            go.transform.localPosition = m.GetColumn(3);
            go.transform.localRotation = m.rotation;
            go.transform.localScale = m.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var nr = go.AddComponent<MeshRenderer>();
            nr.sharedMaterial = mat;
            nr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            n++;
        }
        // La copia cuelga del fantasma (con la orientación del objeto): queda bajo la bola, como si la mano lo llevara.
        copia.localRotation = Quaternion.Inverse(fantasma.rotation) * desde.rotation;
        copia.localPosition = new Vector3(0f, -0.04f, 0f) - Vector3.up * b.extents.y - copia.localRotation * desde.InverseTransformVector(b.center - desde.position);
        copia.gameObject.SetActive(n > 0);
    }

    IEnumerator Animar(Transform desde, Transform hasta, I ic, int veces, string txtDesde, string txtHasta)
    {
        Reproduciendo = true;
        bool enPantalla = pantalla != null && desde == pantalla;
        fantasma.gameObject.SetActive(true);
        fantasma.localRotation = Quaternion.identity;
        EstiloNanoLab.Poner(icono, ic);
        bola.enabled = anillo.enabled = !enPantalla;
        // En pantalla: icono a la izquierda y rótulo amarillo centrado, en el plano de la pantalla.
        icono.transform.localPosition = enPantalla ? new Vector3(-0.3f, 0f, -0.002f) : new Vector3(0f, 0.09f, 0f);
        icono.transform.localRotation = Quaternion.identity;
        rotulo.rectTransform.localPosition = enPantalla ? new Vector3(icono.enabled ? 0.04f : 0f, 0f, -0.002f) : new Vector3(0f, 0.19f, 0f);
        rotulo.rectTransform.localRotation = Quaternion.identity;
        rotulo.alignment = enPantalla ? TextAlignmentOptions.Center : TextAlignmentOptions.Bottom;
        rotulo.color = enPantalla ? K.Amarillo : Color.white;
        PrepararCopia(enPantalla ? null : desde, ic);
        bool viaje = hasta != null && !enPantalla;
        destino.gameObject.SetActive(viaje);
        rotuloDestino.text = txtHasta;
        float dur = viaje ? 2.2f : 1.4f;
        for (int v = 0; veces < 0 || v < veces; v++)
        {
            if (desde == null) break;
            Sonidos.Clic(desde.position);
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                if (desde == null) break;
                if (enPantalla)
                {
                    // Late suavemente dentro de la pantalla, sin moverse ni girar hacia la cabeza.
                    fantasma.SetPositionAndRotation(desde.position, desde.rotation);
                    fantasma.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(t * Mathf.PI * 4f));
                    rotulo.text = txtDesde;
                    yield return null;
                    continue;
                }
                var a = desde.position + Vector3.up * 0.06f;
                var b = viaje && hasta != null ? hasta.position + Vector3.up * 0.06f : a;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.25f) / 0.5f));   // coge, viaja, suelta
                fantasma.position = Vector3.Lerp(a, b, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 0.18f);
                float pulso = 1f + 0.25f * Mathf.Sin(t * Mathf.PI * 6f) * (!viaje ? 1f : (t < 0.25f || t > 0.75f ? 1f : 0f));
                fantasma.localScale = Vector3.one * pulso;
                rotulo.text = !viaje || t < 0.25f ? txtDesde : t < 0.75f ? "LLÉVALO…" : txtHasta;
                if (viaje && hasta != null)
                {
                    destino.position = hasta.position + Vector3.up * 0.005f;
                    destino.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(Time.time * 6f));
                }
                Mirar(rotulo.transform);
                Mirar(rotuloDestino.transform);
                Mirar(icono.transform);
                yield return null;
            }
            yield return new WaitForSeconds(veces < 0 ? 0.6f : 0.25f);
        }
        fantasma.gameObject.SetActive(false);
        destino.gameObject.SetActive(false);
        Reproduciendo = false;
    }

    static void Mirar(Transform t)
    {
        var cab = ManosRig.Cabeza;
        if (cab == null || t == null) return;
        var d = t.position - cab.position;
        d.y = 0f;
        if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
    }
}

// ---------------------------------------------------------------------------------------------- virus holográfico

public class VirusHolograma : MonoBehaviour
{
    public VirusVacuna virus;
    public float radio = 0.3f;
    // Revisión del playtest: "quiero poder girarlo como un globo terráqueo, no sostenerlo en la mano".
    //  - El virus NO se mueve de su sitio: gira sobre su centro. Grip sobre el virus (mano o mando) y mover la mano lo
    //    hace rodar como una bola de ratón (trackball); al soltar sigue girando un poco y se frena (inercia).
    //  - Al empezar el paso hay unos segundos en los que tocar no elige (el rayo + gatillo sí).
    //  - Para analizar o elegir con la mano hay que dejarla sobre la parte un momento y sin el grip.
    public const float Retener = 0.45f;
    public const float Frenado = 1.6f;   // 1/s: cuánto tarda en pararse la inercia
    public static float TactilDesde;   // RetoVacuna lo pone al empezar el paso ANTÍGENO
    public Oculus.Interaction.Grabbable asa;   // agarre del SDK de Meta (mano y mando, de cerca); lo pone MontajeVacuna
    public bool Girando => agarre >= 0 || AsaAgarrada;
    public bool YaAgarrado { get; private set; }   // se giró al menos una vez en este paso (lo reinicia RetoVacuna)
    public void ReiniciarAgarre() { YaAgarrado = false; GradosGirados = 0f; }
    public float GradosGirados { get; private set; }   // giro acumulado en este paso (explorar girando descubre partes)
    public Vector3 VelocidadAngular => velocidad;   // grados/s (eje * velocidad)
    bool AsaAgarrada => asa != null && asa.isActiveAndEnabled && asa.SelectingPointsCount > 0;
    readonly Collider[] buffer = new Collider[16];
    readonly SelectorVacuna[] tocando = new SelectorVacuna[2];
    readonly float[] desde = new float[2];
    readonly bool[] elegido = new bool[2];
    int agarre = -1;            // giro con el grip propio (sin asa del SDK)
    Vector3? previo;            // del centro al punto de agarre, el frame anterior
    Vector3 velocidad;
    Vector3 casaPos;
    bool casaGuardada;

    // Impulso de giro (lo usan las pruebas; también sirve para un "empujón" desde fuera).
    public void Impulsar(Vector3 gradosPorSegundo) => velocidad = gradosPorSegundo;

    void Update()
    {
        if (virus == null || virus.Modelo == null) return;
        var m = virus.Modelo;
        if (!casaGuardada) { casaPos = m.localPosition; casaGuardada = true; }
        m.localPosition = casaPos;   // el globo no se mueve de su pie
        var centro = m.position;

        Mano(0, ManosRig.ManoIzq, OVRInput.Controller.LTouch);
        Mano(1, ManosRig.ManoDer, OVRInput.Controller.RTouch);

        // Punto que "arrastra" la superficie: el agarre del SDK o la mano con el grip propio.
        Vector3? punto = null;
        if (AsaAgarrada && asa.SelectingPoints.Count > 0) punto = asa.SelectingPoints[0].position;
        else if (agarre >= 0) { var mano = agarre == 0 ? ManosRig.ManoIzq : ManosRig.ManoDer; if (mano != null) punto = mano.position; }

        if (punto.HasValue)
        {
            var d = punto.Value - centro;
            if (previo.HasValue && d.sqrMagnitude > 1e-5f && previo.Value.sqrMagnitude > 1e-5f)
            {
                var q = Quaternion.FromToRotation(previo.Value, d);
                m.rotation = q * m.rotation;
                q.ToAngleAxis(out float ang, out Vector3 eje);
                if (ang > 180f) ang -= 360f;
                GradosGirados += Mathf.Abs(ang);
                var v = Time.deltaTime > 1e-4f ? eje * (ang / Time.deltaTime) : Vector3.zero;
                velocidad = Vector3.Lerp(velocidad, v, 0.35f);   // se suaviza para la inercia al soltar
            }
            previo = d;
            YaAgarrado = true;
        }
        else
        {
            previo = null;
            float w = velocidad.magnitude;
            if (w > 2f) { m.rotation = Quaternion.AngleAxis(w * Time.deltaTime, velocidad / w) * m.rotation; GradosGirados += w * Time.deltaTime; }
            velocidad *= Mathf.Exp(-Frenado * Time.deltaTime);
        }
        // El asa vuelve siempre al centro del virus cuando no se está agarrando.
        if (asa != null && !AsaAgarrada) asa.transform.SetPositionAndRotation(centro, Quaternion.identity);
    }

    void Mano(int i, Transform mano, OVRInput.Controller mando)
    {
        if (mano == null) return;
        bool grip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, mando) > 0.55f;

        // Sin asa del SDK: grip cerca del virus = girarlo (no cogerlo).
        if (asa == null)
        {
            if (agarre == i && !grip) { agarre = -1; Sonidos.Clic(virus.Centro); }
            else if (agarre < 0 && grip && (mano.position - virus.Centro).magnitude < radio * 1.4f)
            {
                agarre = i;
                previo = null;
                ManosRig.Vibrar(virus.Centro, 0.3f, 0.05f);
                Sonidos.Clic(virus.Centro);
            }
        }

        // Tocar para analizar o elegir: la parte que toca la mano (con su forma real), dejándola un momento y sin grip.
        SelectorVacuna s = null;
        int n = Physics.OverlapSphereNonAlloc(mano.position, 0.03f, buffer, ~0, QueryTriggerInteraction.Collide);
        for (int k = 0; k < n && s == null; k++)
        {
            var x = buffer[k].GetComponent<SelectorVacuna>();
            if (x != null && x.transform.IsChildOf(virus.transform)) s = x;
        }
        if (s != tocando[i]) { tocando[i] = s; desde[i] = Time.time; elegido[i] = false; }
        if (s == null || grip || Girando || elegido[i] || Time.time < TactilDesde) return;
        if (Time.time - desde[i] >= Retener) { elegido[i] = true; s.Activar(); }
    }
}
