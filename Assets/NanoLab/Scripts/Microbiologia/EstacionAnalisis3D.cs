using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Proyector holográfico: al encajar un portaobjetos con virus en su ranura
// muestra el modelo 3D grande. Se maneja con botones (poke del SDK de Meta o rayo): rotar, zoom,
// vista superior / lateral, corte y etiquetas de las capas. Además se puede agarrar con el grip:
// con una mano se gira y con las dos se amplía. El holograma responde a la mano (brilla más),
// tiene un anillo de escaneo, partículas alrededor y un zumbido suave de proyector.
public class EstacionAnalisis3D : MonoBehaviour
{
    public Platina platina;
    public Transform holograma;          // punto donde flota el modelo
    public float diametro = 0.7f;
    public GameObject haz;               // cono de luz del proyector
    public Transform plataforma;         // anillos holográficos (giran despacio; más intensos con un modelo)
    public TMP_Text titulo, ficha, estado;
    public Renderer micrografia;
    public BotonFisico rotarIzq, rotarDer, zoomMas, zoomMenos, vistaSuperior, vistaLateral, corte, etiquetas;
    public ParticleSystem particulas;    // motas alrededor del virus
    public Transform escaneo;            // disco que recorre el virus de arriba abajo

    // Profundidad (fracción del radio) de cada capa, en el orden de ModeloVirus.Paleta / MuestraData.componentes.
    static readonly float[] ProfundidadCapa = { 1.0f, 0.8f, 0.86f, 0.9f, 0.35f };

    GameObject modelo;
    readonly List<Material> mats = new List<Material>();
    readonly List<float> rimBase = new List<float>();
    Material holoColor;
    OVRCameraRig rig;
    Transform ojo;
    float escala = 1f, escalaObjetivo = 1f, brillo;
    bool auto = true, conCorte, conEtiquetas, volverARotacion, avisadoAgarre;
    Quaternion inclinacion = Quaternion.identity, inclinacionObjetivo = Quaternion.identity;
    Vector3 basePos;

    // Agarre con las manos (0 izquierda, 1 derecha).
    readonly bool[] agarrando = new bool[2];
    readonly Vector3[] dirAnterior = new Vector3[2];
    bool dosManos;
    float distInicio, escalaInicio;
    Vector3 ejeAnterior;

    // Etiquetas de las capas.
    Transform rotulos;
    readonly List<(Transform etiqueta, LineRenderer linea, float profundidad, int lado, float alto)> capas = new List<(Transform, LineRenderer, float, int, float)>();
    AudioSource zumbido;

    void Start()
    {
        holoColor = Resources.Load<Material>("NanoLab/Holograma_Color");
        rig = FindObjectOfType<OVRCameraRig>();
        ojo = rig != null ? rig.centerEyeAnchor : (Camera.main != null ? Camera.main.transform : null);
        basePos = holograma.localPosition;
        platina.Cambio += AlCambiar;
        if (rotarIzq) rotarIzq.Pulsado += () => auto = false;
        if (rotarDer) rotarDer.Pulsado += () => auto = false;
        if (zoomMas) zoomMas.Pulsado += () => escalaObjetivo = Mathf.Min(2.2f, escalaObjetivo + 0.2f);
        if (zoomMenos) zoomMenos.Pulsado += () => escalaObjetivo = Mathf.Max(0.5f, escalaObjetivo - 0.2f);
        if (vistaSuperior) vistaSuperior.Pulsado += () => { inclinacionObjetivo = Quaternion.Euler(80f, 0f, 0f); auto = false; Marcar(); };
        if (vistaLateral) vistaLateral.Pulsado += () => { inclinacionObjetivo = Quaternion.identity; auto = true; volverARotacion = true; Marcar(); };
        if (corte) corte.Pulsado += () => { conCorte = !conCorte; AplicarCorte(); Marcar(); };
        if (etiquetas) etiquetas.Pulsado += () => { conEtiquetas = !conEtiquetas; Marcar(); };

        zumbido = holograma.gameObject.AddComponent<AudioSource>();
        zumbido.clip = Sonidos.Zumbido();
        zumbido.loop = true;
        zumbido.playOnAwake = false;
        zumbido.spatialBlend = 1f;
        zumbido.minDistance = 0.6f;
        zumbido.maxDistance = 6f;
        zumbido.volume = 0f;
        if (escaneo) escaneo.gameObject.SetActive(false);
        AlCambiar(platina.Actual);
    }

    // LEDs de los botones que alternan.
    void Marcar()
    {
        if (corte) corte.MarcarActivo(conCorte);
        if (etiquetas) etiquetas.MarcarActivo(conEtiquetas);
        if (vistaSuperior) vistaSuperior.MarcarActivo(inclinacionObjetivo != Quaternion.identity);
        if (vistaLateral) vistaLateral.MarcarActivo(inclinacionObjetivo == Quaternion.identity && auto);
    }

    public void AlCambiar(Portaobjetos p)
    {
        if (modelo != null) { Destroy(modelo); modelo = null; }
        if (rotulos != null) { Destroy(rotulos.gameObject); rotulos = null; }
        capas.Clear();
        mats.Clear();
        rimBase.Clear();
        var m = p != null ? p.muestra : null;
        if (haz) haz.SetActive(false);
        if (particulas) particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (escaneo) escaneo.gameObject.SetActive(false);
        if (zumbido) zumbido.Stop();

        if (m == null)
        {
            titulo.text = "ESTACIÓN DE ANÁLISIS 3D";
            ficha.text = "";
            estado.text = "Coloca un portaobjetos con una <b>muestra viral</b> en la ranura iluminada";
            if (micrografia) micrografia.enabled = false;
            return;
        }

        titulo.text = m.nombre.ToUpper();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=#8FA6BC>{m.nombreCientifico}</color>");
        if (m.caracteristicas != null)
            foreach (var d in m.caracteristicas) sb.AppendLine($"<color=#5FD3FF>{d.clave}:</color> {d.valor}");
        sb.AppendLine($"<color=#FF7070><b>Riesgo: {m.riesgo}</b></color>");
        ficha.text = sb.ToString();
        if (micrografia) { micrografia.enabled = m.micrografia != null; micrografia.material.mainTexture = m.micrografia; }

        if (m.tipo != TipoMuestra.Virus)
        {
            estado.text = "El modelo 3D está disponible para las <b>muestras virales</b>. Observa esta muestra en un microscopio.";
            return;
        }

        estado.text = "<b>Agarra el virus con el grip</b> para girarlo · con <b>las dos manos</b>, amplíalo\nO usa los botones: rotar, zoom, vistas, <b>corte</b> y <b>etiquetas</b>";
        escala = escalaObjetivo = 1f;
        inclinacion = inclinacionObjetivo = Quaternion.identity;
        auto = true; conCorte = false; conEtiquetas = false; avisadoAgarre = false;
        holograma.localRotation = Quaternion.identity;
        // Pivote intermedio en el centro del virus: el zoom y la inclinación giran/escalan sobre él.
        modelo = new GameObject("Centro");
        modelo.transform.SetParent(holograma, false);
        ModeloVirus.Instanciar(m, holoColor, modelo.transform, diametro);
        foreach (var r in modelo.GetComponentsInChildren<Renderer>()) mats.AddRange(r.materials);
        foreach (var mat in mats) rimBase.Add(mat.HasProperty("_RimIntensity") ? mat.GetFloat("_RimIntensity") : 0f);
        CrearEtiquetas(m);
        AplicarCorte();
        Marcar();
        if (haz) haz.SetActive(true);
        if (particulas) particulas.Play();
        if (escaneo) escaneo.gameObject.SetActive(true);
        if (zumbido && zumbido.clip) zumbido.Play();
        Progreso.Marcar(m);
        Sonidos.Scan(holograma.position);
    }

    void Update()
    {
        if (plataforma != null)
        {
            plataforma.Rotate(0f, (modelo != null ? -14f : -5f) * Time.deltaTime, 0f, Space.Self);
            float s = Mathf.Lerp(plataforma.localScale.x, modelo != null ? 1f + 0.08f * brillo : 0.85f, Time.deltaTime * 3f);
            plataforma.localScale = Vector3.one * s;
        }
        if (modelo == null) return;
        float dt = Time.deltaTime;

        bool cerca = Manos(dt);
        if (auto) holograma.Rotate(0f, 18f * dt, 0f, Space.World);
        if (rotarIzq && rotarIzq.Presionado) { holograma.Rotate(0f, 90f * dt, 0f, Space.World); volverARotacion = false; }
        if (rotarDer && rotarDer.Presionado) { holograma.Rotate(0f, -90f * dt, 0f, Space.World); volverARotacion = false; }
        if (volverARotacion)
        {
            holograma.localRotation = Quaternion.Slerp(holograma.localRotation, Quaternion.identity, dt * 4f);
            if (Quaternion.Angle(holograma.localRotation, Quaternion.identity) < 1f) volverARotacion = false;
        }

        escala = Mathf.Lerp(escala, escalaObjetivo, dt * 6f);
        inclinacion = Quaternion.Slerp(inclinacion, inclinacionObjetivo, dt * 5f);
        modelo.transform.localScale = Vector3.one * escala;
        modelo.transform.localRotation = inclinacion;
        holograma.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 1.2f) * 0.02f;

        // Respuesta a la mano: el borde del holograma brilla más al acercarla y aún más al agarrarlo.
        float objetivoBrillo = agarrando[0] || agarrando[1] ? 1f : cerca ? 0.5f : 0f;
        brillo = Mathf.MoveTowards(brillo, objetivoBrillo, dt * 3f);
        for (int i = 0; i < mats.Count; i++)
            if (rimBase[i] > 0f) mats[i].SetFloat("_RimIntensity", rimBase[i] + brillo * 1.4f + Mathf.Sin(Time.time * 6f) * 0.15f * brillo);

        Escaneo();
        if (particulas)
        {
            var sh = particulas.shape;
            sh.radius = diametro * 0.55f * escala;
        }
        if (zumbido) zumbido.volume = Mathf.MoveTowards(zumbido.volume, (0.12f + 0.08f * brillo) * AjustesNanoLab.VolumenEfectos, dt);
        ActualizarEtiquetas();
        if (conCorte) AplicarCorte();
    }

    // ------------------------------------------------------------------ agarre con las manos

    // Grip cerca del virus: una mano lo gira (como una bola), dos manos lo amplían o reducen.
    // Devuelve si alguna mano está cerca (para el brillo).
    // Grip del mando o, con seguimiento de manos (sin mandos), pellizco índice-pulgar.
    readonly OVRHand[] manosHT = new OVRHand[2];
    bool manosBuscadas;
    bool Aprieta(int i, OVRInput.Controller mando)
    {
        if (!manosBuscadas)
        {
            manosBuscadas = true;
            foreach (var h in FindObjectsOfType<OVRHand>(true))
            {
                if ((int)h.GetHand() == (int)OVRHand.Hand.HandLeft) manosHT[0] = h;
                else if ((int)h.GetHand() == (int)OVRHand.Hand.HandRight) manosHT[1] = h;
            }
        }
        var mano = manosHT[i];
        if (mano != null && mano.IsTracked) return mano.GetFingerIsPinching(OVRHand.HandFinger.Index);
        return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, mando) > 0.55f;
    }

    bool Manos(float dt)
    {
        if (rig == null) return false;
        var centro = holograma.position;
        float alcance = diametro * 0.5f * escala + 0.22f;
        Transform[] anclas = { rig.leftHandAnchor, rig.rightHandAnchor };
        OVRInput.Controller[] mandos = { OVRInput.Controller.LTouch, OVRInput.Controller.RTouch };
        bool cerca = false;
        int n = 0;
        for (int i = 0; i < 2; i++)
        {
            if (anclas[i] == null) continue;
            var dir = anclas[i].position - centro;
            bool dentro = dir.magnitude < alcance;
            cerca |= dentro;
            bool grip = Aprieta(i, mandos[i]);
            if (grip && !agarrando[i] && dentro)
            {
                agarrando[i] = true;
                dirAnterior[i] = dir;
                auto = false; volverARotacion = false;
                Sonidos.Clic(anclas[i].position);
                ManosRig.Vibrar(anclas[i].position, 0.5f, 0.06f);
                if (!avisadoAgarre)
                {
                    avisadoAgarre = true;
                    Aviso.Mostrar("Gira la mano para rotarlo · agárralo con las dos manos y sepáralas para ampliarlo", centro + Vector3.up * (alcance + 0.1f), Aviso.Tipo.Info, 3f);
                }
            }
            else if (!grip && agarrando[i]) agarrando[i] = false;
            if (agarrando[i]) n++;
        }

        if (n == 2)
        {
            var eje = anclas[1].position - anclas[0].position;
            float d = eje.magnitude;
            if (!dosManos) { dosManos = true; distInicio = Mathf.Max(0.05f, d); escalaInicio = escalaObjetivo; ejeAnterior = eje; }
            float nueva = Mathf.Clamp(escalaInicio * d / distInicio, 0.5f, 2.2f);
            if (Mathf.Abs(nueva - escalaObjetivo) > 0.1f) ManosRig.Vibrar(centro, 0.15f, 0.02f);   // "muescas" al ampliar
            escalaObjetivo = nueva;
            holograma.rotation = Quaternion.FromToRotation(ejeAnterior, eje) * holograma.rotation;
            ejeAnterior = eje;
        }
        else
        {
            dosManos = false;
            for (int i = 0; i < 2; i++)
            {
                if (!agarrando[i]) continue;
                var dir = anclas[i].position - centro;
                if (dirAnterior[i].sqrMagnitude > 1e-4f && dir.sqrMagnitude > 1e-4f)
                    holograma.rotation = Quaternion.FromToRotation(dirAnterior[i], dir) * holograma.rotation;
                dirAnterior[i] = dir;
            }
        }
        return cerca;
    }

    // ------------------------------------------------------------------ efectos

    // Disco de escaneo que sube y baja por el virus con el tamaño de su sección.
    void Escaneo()
    {
        if (escaneo == null) return;
        float r = diametro * 0.5f * escala;
        float y = Mathf.Sin(Time.time * 1.6f) * r * 0.92f;
        float seccion = 2f * Mathf.Sqrt(Mathf.Max(0f, r * r - y * y)) * 1.08f + 0.02f;
        escaneo.position = holograma.position + Vector3.up * y;
        escaneo.localScale = new Vector3(seccion, seccion, 1f);
    }

    // Rótulo por capa (espícula, envoltura, proteínas, interior) con una línea hasta su profundidad.
    void CrearEtiquetas(MuestraData m)
    {
        if (m.componentes == null || m.componentes.Length == 0) return;
        rotulos = new GameObject("Etiquetas").transform;
        rotulos.SetParent(transform, false);
        var pal = ModeloVirus.Paleta(m);
        var lineaMat = Resources.Load<Material>("NanoLab/LineaPuntero");
        int k = Mathf.Min(m.componentes.Length, ProfundidadCapa.Length);
        for (int i = 0; i < k; i++)
        {
            var e = new GameObject("Capa" + i).transform;
            e.SetParent(rotulos, false);
            e.gameObject.AddComponent<Cartel>();
            var t = e.gameObject.AddComponent<TextMeshPro>();
            t.text = $"<color=#{ColorUtility.ToHtmlStringRGB(pal[i])}>●</color> {m.componentes[i]}";
            t.fontSize = 0.32f;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 220);
            t.rectTransform.sizeDelta = new Vector2(0.6f, 0.08f);
            var l = new GameObject("Linea").AddComponent<LineRenderer>();
            l.transform.SetParent(rotulos, false);
            l.positionCount = 2;
            l.useWorldSpace = true;
            l.widthMultiplier = 0.003f;
            l.sharedMaterial = lineaMat;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Izquierda / derecha del usuario, repartidas en altura.
            int lado = i % 2 == 0 ? -1 : 1;
            float alto = (i / 2 - (k - 1) / 4f) * 0.32f;
            capas.Add((e, l, ProfundidadCapa[i], lado, alto));
        }
        rotulos.gameObject.SetActive(false);
    }

    void ActualizarEtiquetas()
    {
        if (rotulos == null) return;
        rotulos.gameObject.SetActive(conEtiquetas);
        if (!conEtiquetas || ojo == null) return;
        var centro = holograma.position;
        float r = diametro * 0.5f * escala;
        var haciaUsuario = ojo.position - centro; haciaUsuario.y = 0f;
        haciaUsuario = haciaUsuario.sqrMagnitude > 1e-4f ? haciaUsuario.normalized : Vector3.back;
        var derecha = Vector3.Cross(Vector3.up, haciaUsuario);
        foreach (var c in capas)
        {
            var pos = centro + derecha * c.lado * (r + 0.32f) + Vector3.up * c.alto + haciaUsuario * 0.1f;
            c.etiqueta.position = pos;
            var dir = (pos - centro).normalized;
            c.linea.SetPosition(0, pos - derecha * c.lado * 0.12f);
            c.linea.SetPosition(1, centro + dir * r * c.profundidad);
        }
    }

    // Plano que pasa por el centro y mira al usuario: quita la mitad cercana.
    void AplicarCorte()
    {
        Vector3 n = Vector3.forward;
        if (ojo != null) { n = holograma.position - ojo.position; n.y = 0f; n = n.sqrMagnitude > 1e-4f ? n.normalized : Vector3.forward; }
        var plano = new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, holograma.position));
        foreach (var m in mats)
        {
            m.SetFloat("_ClipOn", conCorte ? 1f : 0f);
            m.SetVector("_ClipPlane", plano);
        }
    }
}
