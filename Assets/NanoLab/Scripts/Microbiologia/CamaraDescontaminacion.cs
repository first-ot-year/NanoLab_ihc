using TMPro;
using UnityEngine;

// Cámara de descontaminación (esclusa) a la entrada del laboratorio de alto riesgo BSL-3.
// Dos puertas correderas herméticas: la exterior da a la sala principal y la interior al BSL-3.
// Nunca están abiertas las dos a la vez:
//  1. Al acercarte, se abre la puerta de tu lado.
//  2. En cuanto estás dentro, se cierran las dos puertas (la cámara queda sellada).
//  3. Ciclo de descontaminación: ducha de aire, niebla desinfectante (peróxido de hidrógeno
//     vaporizado), luz UV-C y extracción por filtros HEPA, con sonido, vibración y pantallas.
//  4. Se abre la puerta del otro lado. Al cruzarla se cierra detrás de ti.
// Al salir del BSL-3, los portaobjetos con virus que lleves se descontaminan (las muestras virales
// no salen del área). La monta MontarSalaAltoRiesgo. Raíz en el suelo, en el centro de la cámara,
// con +Z hacia el BSL-3.
public class CamaraDescontaminacion : MonoBehaviour
{
    public PuertaCorrediza exterior, interior;
    [Tooltip("Ancho (x) y fondo (z) interiores de la cámara (m).")]
    public float ancho = 1.6f, fondo = 1.4f;
    [Tooltip("Distancia (m) a la que se abre la puerta al acercarse.")]
    public float distanciaAcercarse = 1.4f;

    [Header("Efectos")]
    public ParticleSystem[] niebla;
    public Light luzCiclo, luzUV;
    public AudioSource aire;
    public TMP_Text pantallaInterior, pantallaExterior, pantallaBSL;
    public Transform barra;   // barra de progreso (se escala en x de 0 a 1)

    [System.Serializable]
    public struct Fase
    {
        public string nombre;
        public string detalle;
        public float duracion;
        public float emision, velocidad, volumen, vibracion;
        public bool uv;
    }

    public Fase[] fases =
    {
        new Fase { nombre = "DUCHA DE AIRE", detalle = "Chorros de aire a presión arrastran las partículas de tu traje", duracion = 3f, emision = 90f, velocidad = 2.2f, volumen = 0.55f, vibracion = 0.35f },
        new Fase { nombre = "NEBULIZACIÓN DESINFECTANTE", detalle = "Niebla de peróxido de hidrógeno vaporizado", duracion = 3.5f, emision = 60f, velocidad = 0.6f, volumen = 0.3f, vibracion = 0.12f },
        new Fase { nombre = "LUZ ULTRAVIOLETA UV-C", detalle = "La luz UV-C inactiva los microorganismos de las superficies", duracion = 2.5f, emision = 0f, velocidad = 0.3f, volumen = 0.1f, uv = true },
        new Fase { nombre = "EXTRACCIÓN DE AIRE", detalle = "Los filtros HEPA renuevan el aire de la cámara", duracion = 2f, emision = 0f, velocidad = 0.3f, volumen = 0.45f },
    };

    enum Estado { Espera, Sellando, Ciclo, Saliendo }
    enum Lado { Ninguno, Laboratorio, BSL }

    Estado estado = Estado.Espera;
    Lado origen = Lado.Laboratorio, bloqueado = Lado.Ninguno;
    float tCiclo, tFuera, tVolver;
    int fase = -1;
    bool vibrando;

    const float MargenSellado = 0.3f;   // la cabeza debe estar a esta distancia de las dos puertas para sellar

    float Total { get { float s = 0f; foreach (var f in fases) s += f.duracion; return s; } }

    void Start()
    {
        Efectos(-1);
        if (aire != null)
        {
            aire.clip = Ruido();
            aire.loop = true;
            aire.volume = 0f;
            aire.Play();
        }
        Pantallas("LISTO", "Acércate a la puerta", "LIBRE");
    }

    void OnDisable() => Vibrar(0f);

    void Update()
    {
        var cab = ManosRig.Cabeza;
        if (cab == null || exterior == null || interior == null) return;
        var p = transform.InverseTransformPoint(cab.position);
        float medio = fondo / 2f;
        bool enAncho = Mathf.Abs(p.x) < ancho / 2f + 0.05f;
        bool dentro = enAncho && p.z > -medio + MargenSellado && p.z < medio - MargenSellado;
        bool enCamara = enAncho && p.z > -medio && p.z < medio;
        bool cercaAncho = Mathf.Abs(p.x) < ancho / 2f + 0.7f;
        bool cercaLab = cercaAncho && p.z <= -medio && p.z > -medio - distanciaAcercarse;
        bool cercaBSL = cercaAncho && p.z >= medio && p.z < medio + distanciaAcercarse;
        // Tras cruzar una puerta no se vuelve a abrir hasta que te alejas de ella.
        if (bloqueado == Lado.Laboratorio && !cercaLab) bloqueado = Lado.Ninguno;
        if (bloqueado == Lado.BSL && !cercaBSL) bloqueado = Lado.Ninguno;

        switch (estado)
        {
            case Estado.Espera:
                if (dentro)
                {
                    origen = interior.Abriendo ? Lado.BSL : Lado.Laboratorio;
                    Sellar();
                }
                else if (cercaLab && bloqueado != Lado.Laboratorio && interior.Cerrada)
                {
                    exterior.Abrir(true); tFuera = 0f;
                    Pantallas("PUERTA ABIERTA", "Entra en la cámara", "EN USO");
                }
                else if (cercaBSL && bloqueado != Lado.BSL && exterior.Cerrada)
                {
                    interior.Abrir(true); tFuera = 0f;
                    Pantallas("PUERTA ABIERTA", "Entra en la cámara para salir del BSL-3", "EN USO");
                }
                else if (!enCamara && (exterior.Abriendo || interior.Abriendo))
                {
                    // Se acercó pero no entró: se cierra al poco.
                    tFuera += Time.deltaTime;
                    if (tFuera > 1.5f) { exterior.Abrir(false); interior.Abrir(false); Pantallas("LISTO", "Acércate a la puerta", "LIBRE"); }
                }
                break;

            case Estado.Sellando:
                if (exterior.Cerrada && interior.Cerrada) EmpezarCiclo();
                break;

            case Estado.Ciclo:
                if (!enCamara) { Abortar(); break; }   // salió físicamente (escala de habitación)
                Ciclo();
                break;

            case Estado.Saliendo:
                var destino = origen == Lado.Laboratorio ? Lado.BSL : Lado.Laboratorio;
                bool cruzo = destino == Lado.BSL ? p.z > medio + 0.35f : p.z < -medio - 0.35f;
                if (cruzo)
                {
                    (destino == Lado.BSL ? interior : exterior).Abrir(false);
                    bloqueado = destino;
                    estado = Estado.Espera;
                    Pantallas("LISTO", "Acércate a la puerta", "LIBRE");
                    break;
                }
                // Si se queda junto a la puerta por la que entró, pide un ciclo de vuelta.
                bool junto = enAncho && (origen == Lado.Laboratorio ? p.z < -medio + 0.45f : p.z > medio - 0.45f);
                tVolver = junto ? tVolver + Time.deltaTime : 0f;
                if (tVolver > 1.5f) { origen = destino; Sellar(); }
                break;
        }
    }

    void Sellar()
    {
        exterior.Abrir(false);
        interior.Abrir(false);
        estado = Estado.Sellando;
        tVolver = 0f;
        Pantallas("SELLANDO PUERTAS", "Quédate dentro de la cámara", "EN USO");
        Aviso.Mostrar("Cámara de descontaminación: quédate dentro hasta que se abra la puerta",
            transform.position + Vector3.up * 1.75f, Aviso.Tipo.Info, 4f);
    }

    void EmpezarCiclo()
    {
        estado = Estado.Ciclo;
        tCiclo = 0f;
        fase = -1;
    }

    void Ciclo()
    {
        tCiclo += Time.deltaTime;
        float acum = 0f;
        int f = fases.Length;
        for (int i = 0; i < fases.Length; i++)
        {
            if (tCiclo < acum + fases[i].duracion) { f = i; break; }
            acum += fases[i].duracion;
        }
        if (f >= fases.Length) { Terminar(); return; }
        if (f != fase) { fase = f; Efectos(f); Sonidos.Clic(transform.position + Vector3.up * 2f); }

        var fa = fases[f];
        float restante = Total - tCiclo;
        float progreso = Mathf.Clamp01(tCiclo / Total);
        string paso = $"PASO {f + 1}/{fases.Length} · {fa.nombre}";
        Pantallas($"DESCONTAMINANDO · {Mathf.CeilToInt(restante)} s", $"{paso}\n<size=75%><color=#A9BED2>{fa.detalle}</color></size>", "EN USO");
        if (barra != null) barra.localScale = new Vector3(Mathf.Max(0.001f, progreso), 1f, 1f);

        // Luz del ciclo que pulsa y UV que parpadea al encenderse.
        if (luzCiclo != null) luzCiclo.intensity = fa.uv ? 0.2f : 0.8f + Mathf.Sin(Time.time * 6f) * 0.4f;
        if (luzUV != null && fa.uv) luzUV.intensity = tCiclo - acum < 0.4f ? Random.Range(0.3f, 2.2f) : 2.2f;
        if (aire != null) aire.volume = Mathf.Lerp(aire.volume, fa.volumen * AjustesNanoLab.VolumenEfectos, Time.deltaTime * 4f);
        if (fa.vibracion > 0f) Vibrar(fa.vibracion);
        else if (vibrando) Vibrar(0f);
    }

    void Terminar()
    {
        Efectos(-1);
        if (barra != null) barra.localScale = Vector3.one;
        var destino = origen == Lado.Laboratorio ? interior : exterior;
        destino.Abrir(true);
        estado = Estado.Saliendo;
        tVolver = 0f;
        Sonidos.Etapa(transform.position + Vector3.up * 1.6f);
        if (origen == Lado.BSL) DescontaminarMuestras();
        Pantallas("DESCONTAMINACIÓN COMPLETA", origen == Lado.Laboratorio ? "Puedes entrar al laboratorio BSL-3" : "Puedes salir a la sala principal", "EN USO");
        Aviso.Mostrar(origen == Lado.Laboratorio ? "Descontaminación completa: puedes entrar al BSL-3" : "Descontaminación completa: puedes salir",
            transform.position + Vector3.up * 1.75f, Aviso.Tipo.Ok, 3f);
    }

    void Abortar()
    {
        Efectos(-1);
        estado = Estado.Espera;
        Pantallas("LISTO", "Acércate a la puerta", "LIBRE");
    }

    // Las muestras virales no salen del área BSL-3: los portaobjetos que lleves o dejes en la cámara se limpian.
    void DescontaminarMuestras()
    {
        int n = 0;
        foreach (var p in Portaobjetos.Todos)
        {
            if (p == null || p.muestra == null || p.muestra.tipo != TipoMuestra.Virus) continue;
            var l = transform.InverseTransformPoint(p.transform.position);
            if (!p.Agarrado && (Mathf.Abs(l.x) > ancho / 2f + 0.3f || Mathf.Abs(l.z) > fondo / 2f + 0.3f)) continue;
            p.Limpiar();
            n++;
        }
        if (n > 0)
            Aviso.Mostrar("Las muestras virales no pueden salir del BSL-3: tu portaobjetos se ha descontaminado",
                transform.position + Vector3.up * 1.45f, Aviso.Tipo.Pista, 5f);
    }

    // -1 = todo apagado.
    void Efectos(int f)
    {
        bool activo = f >= 0;
        var fa = activo ? fases[f] : default;
        if (niebla != null)
            foreach (var ps in niebla)
            {
                if (ps == null) continue;
                var em = ps.emission;
                em.rateOverTime = activo ? fa.emision : 0f;
                var main = ps.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(fa.velocidad * 0.6f, Mathf.Max(0.05f, fa.velocidad));
                if (activo && fa.emision > 0f && !ps.isPlaying) ps.Play();
                if (!activo) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        if (luzCiclo != null) { luzCiclo.enabled = activo; luzCiclo.intensity = 0.8f; }
        if (luzUV != null) luzUV.enabled = activo && fa.uv;
        if (!activo)
        {
            if (aire != null) aire.volume = 0f;
            Vibrar(0f);
        }
    }

    void Vibrar(float amplitud)
    {
        vibrando = amplitud > 0f;
        // El mando corta la vibración a los 2 s: se renueva cada frame mientras dura la fase.
        OVRInput.SetControllerVibration(vibrando ? 0.3f : 0f, amplitud, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(vibrando ? 0.3f : 0f, amplitud, OVRInput.Controller.RTouch);
    }

    void Pantallas(string titulo, string detalle, string exteriorTexto)
    {
        string interiorTexto = $"<b>{titulo}</b>\n{detalle}";
        if (pantallaInterior != null) pantallaInterior.text = interiorTexto;
        string fuera = exteriorTexto == "LIBRE"
            ? "<color=#7CF29A><b>CÁMARA LIBRE</b></color> · acércate a la puerta"
            : $"<color=#FF8A8A><b>CÁMARA EN USO</b></color> · {titulo.ToLower()}";
        if (pantallaExterior != null) pantallaExterior.text = fuera;
        if (pantallaBSL != null) pantallaBSL.text = fuera;
    }

    // Ruido de aire (rosa suavizado) generado por código, en bucle.
    static AudioClip ruido;
    static AudioClip Ruido()
    {
        if (ruido != null) return ruido;
        const int sr = 22050;
        int len = sr * 2;
        var d = new float[len];
        var rnd = new System.Random(7);
        float b0 = 0f, b1 = 0f, b2 = 0f;
        for (int i = 0; i < len; i++)
        {
            float w = (float)rnd.NextDouble() * 2f - 1f;
            b0 = 0.997f * b0 + w * 0.029591f;
            b1 = 0.985f * b1 + w * 0.032534f;
            b2 = 0.95f * b2 + w * 0.048056f;
            d[i] = (b0 + b1 + b2 + w * 0.05f) * 0.9f;
        }
        // Fundido en los extremos para que el bucle no haga clic.
        int fade = sr / 20;
        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            d[i] *= k;
            d[len - 1 - i] *= k;
        }
        ruido = AudioClip.Create("AireDescontaminacion", len, 1, sr, false);
        ruido.SetData(d, 0);
        return ruido;
    }
}
