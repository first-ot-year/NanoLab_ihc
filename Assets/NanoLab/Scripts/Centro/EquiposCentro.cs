using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using K = KitVacuna;

// Los equipos del Centro de Experimentación que antes solo decoraban pasan a ser parte de las misiones, cada uno con
// su consola (pantalla + botón que se pulsa con el dedo o con el rayo) y su número en la guía de flechas del suelo:
//   9  INCUBADORA 37 °C       MISIÓN 1 · al empezar: SACAR PLACAS (las placas A y B pasaron la noche dentro).
//   10 CABINA DE BIOSEGURIDAD MISIÓN 1 · antes de tocar el hisopado C (puede llevar un virus): ENCENDER FLUJO.
//   11 AUTOCLAVE 121 °C       MISIÓN 2 · último paso: ESTERILIZAR la placa tratada y el material.
//   12 ULTRACONGELADOR -80 °C MISIÓN 3 · paso 3 (síntesis): SACAR REACTIVOS (molde de ADN y enzima T7).
//   13 INCUBADORA CO2         MISIÓN 3 · paso 6 (preclínica): SACAR CÉLULAS (células Vero para el ensayo).
// Pulsar un equipo cuando no toca no penaliza: explica para qué sirve y en qué misión se usa.
// Lo monta MontajeMisiones al empezar (sin tocar la escena guardada). RetoManager y RetoVacuna preguntan
// EquiposCentro.Listo(...) antes de dejar seguir.
public class EquiposCentro : MonoBehaviour
{
    public static EquipoCentro Incubadora, Cabina, Autoclave, Ultracongelador, IncubadoraCO2;
    public static IEnumerable<EquipoCentro> Todos => new[] { Incubadora, Cabina, Autoclave, Ultracongelador, IncubadoraCO2 }.Where(e => e != null);
    // false: los equipos no bloquean ni suman pasos a las misiones; se pueden usar para aprender (botón, bandeja y
    // explicación), pero la guía no manda a ellos. true: cada uno es un paso obligatorio (versión larga).
    public const bool Obligatorios = false;
    public static bool Listo(EquipoCentro e) => !Obligatorios || e == null || e.Hecho;

    static readonly List<(Renderer r, bool on)> rendOcultos = new List<(Renderer, bool)>();
    static Transform placaA, placaB;
    static FuenteMuestra fuenteA, fuenteB, fuenteC;
    const string BloqueoPlacas = "Esta placa sigue en la INCUBADORA (9) a 37 °C: ve a la incubadora y pulsa SACAR PLACAS.";
    const string BloqueoHisopado = "El hisopado C puede llevar un virus respiratorio: enciende antes el FLUJO de la CABINA DE BIOSEGURIDAD (10).";

    EquipoCentro pendienteVacuna;

    // ------------------------------------------------------------------ montaje

    public static void Montar(Transform centro, RetoManager rm, Transform banco)
    {
        Incubadora = Cabina = Autoclave = Ultracongelador = IncubadoraCO2 = null;
        rendOcultos.Clear();
        placaA = placaB = null; fuenteA = fuenteB = fuenteC = null;
        var eq = centro.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Equipamiento" && t.parent == centro);
        if (eq == null) { Debug.LogWarning("[Equipos] No hay grupo Equipamiento en el centro: los equipos siguen decorativos."); return; }
        var host = new GameObject("EquiposCentro").AddComponent<EquiposCentro>();
        host.transform.SetParent(centro, false);

        Transform Buscar(string n) => eq.Cast<Transform>().FirstOrDefault(t => t.name == n);

        var inc = Buscar("Incubadora");
        if (inc != null)
        {
            Incubadora = EquipoCentro.Crear(inc, EquipoCentro.Forma.Pared, 9, "INCUBADORA 37 °C", "SACAR PLACAS",
                "Aquí crecen los cultivos a 37 °C, la temperatura del cuerpo. Las placas A y B pasaron la noche dentro.",
                "Las bacterias de la piel crecen mejor a 37 °C, la temperatura del cuerpo: en una noche ya forman colonias que se ven a simple vista. Las placas A y B ya están en el BANCO (1).",
                "Se usa al EMPEZAR la MISIÓN 1 para sacar las placas.", "PLACAS FUERA · 37,0 °C");
            Incubadora.EsSuTurno = () => !Obligatorios || (RetoManager.Instancia != null && RetoManager.Instancia.Actual == EtapaReto.Exploracion);
            Incubadora.Animacion = Incubadora.Bandeja(2, new[] { new Color(0.75f, 0.12f, 0.15f), new Color(0.95f, 0.85f, 0.55f) }, new[] { "A", "B" }, true, "LAS PLACAS A y B");
            Incubadora.AlTerminar = () => { MostrarPlacas(true); AvisarBanco(banco); RetoManager.Instancia?.EquipoUsado(); };
        }

        var cab = Buscar("CabinaBioseguridad");
        if (cab != null)
        {
            Cabina = EquipoCentro.Crear(cab, EquipoCentro.Forma.Cabina, 10, "CABINA DE BIOSEGURIDAD", "ENCENDER FLUJO",
                "Cabina clase II: el aire filtrado (HEPA) aleja de tu cara lo que sale de una muestra. Aquí está la MUESTRA C.",
                "Flujo encendido: una cortina de aire filtrado separa la muestra de tu cara. Así se abre una muestra que puede tener un virus respiratorio. Ya puedes tocar el tubo C con un portaobjetos limpio.",
                "Se usa en la MISIÓN 1, para abrir el hisopado C.", "FLUJO LAMINAR · ENCENDIDO");
            Cabina.EsSuTurno = () => !Obligatorios || (RetoManager.Instancia != null && RetoManager.Instancia.Actual <= EtapaReto.Observacion && Listo(Incubadora));
            Cabina.AlTerminar = () => { if (fuenteC != null) fuenteC.bloqueo = null; Cabina.Flujo(true); RetoManager.Instancia?.EquipoUsado(); };
        }

        var aut = Buscar("Autoclave");
        if (aut != null)
        {
            Autoclave = EquipoCentro.Crear(aut, EquipoCentro.Forma.Encima, 11, "AUTOCLAVE 121 °C", "ESTERILIZAR",
                "Vapor a 121 °C y alta presión: mata bacterias, hongos, virus y esporas.",
                "Vapor a 121 °C durante 15 minutos: mata bacterias, hongos, virus y hasta las esporas. Todo lo que tocó una muestra se esteriliza antes de tirarlo.",
                "Se usa al FINAL de la MISIÓN 2 para esterilizar la placa tratada.", "CICLO TERMINADO · ESTÉRIL");
            Autoclave.EsSuTurno = () => !Obligatorios || (RetoManager.Instancia != null && RetoManager.Instancia.PendienteAutoclave);
            Autoclave.Animacion = Autoclave.Ciclo(new[] { "CALENTANDO · 98 °C", "VAPOR · 121 °C · 1 atm", "ESTERILIZANDO · 15 min", "ENFRIANDO…" }, 0.6f);
            Autoclave.AlTerminar = () => { if (Obligatorios) RetoManager.Instancia?.Esterilizado(); };
            // Como en las estaciones: primero se hace con las manos (dejar el material encima) y luego se pulsa.
            var zona = Autoclave.ZonaCarga("DEJA AQUÍ EL PORTAOBJETOS DESPUÉS");
            Autoclave.Requisito = () =>
            {
                bool hayDespues = Portaobjetos.Todos.Any(x => x != null && x.Despues);
                bool encima = Portaobjetos.Todos.Any(x => x != null && x.Despues && (x.transform.position - zona.position).sqrMagnitude < 0.3f * 0.3f);
                return !Obligatorios || !hayDespues || encima ? null : "Primero deja el portaobjetos DESPUÉS en la bandeja de encima del AUTOCLAVE (11); luego pulsa ESTERILIZAR.";
            };
        }

        var ult = Buscar("Ultracongelador");
        if (ult != null)
        {
            Ultracongelador = EquipoCentro.Crear(ult, EquipoCentro.Forma.Pared, 12, "ULTRACONGELADOR -80 °C", "SACAR REACTIVOS",
                "Aquí se guardan congelados el molde de ADN y la enzima T7 de la vacuna, para que no se estropeen.",
                "El molde de ADN y la enzima T7 se guardan a -80 °C porque a temperatura ambiente se degradan en horas. Ya están en la mesa SÍNTESIS del laboratorio de vacunas.",
                "Se usa en la MISIÓN 3, paso 3 (síntesis del ARNm).", "REACTIVOS RETIRADOS · -80 °C");
            Ultracongelador.EsSuTurno = () => !Obligatorios || (RetoVacuna.Instancia != null && RetoVacuna.Instancia.Actual == EtapaVacuna.Sintesis);
            Ultracongelador.Animacion = Ultracongelador.Bandeja(3, new[] { new Color(0.5f, 0.75f, 1f), new Color(0.95f, 0.95f, 0.6f), new Color(0.6f, 1f, 0.7f) }, new[] { "ADN", "T7", "NTP" }, true, "LOS TUBOS ADN, T7 y NTP");
        }

        var co2 = Buscar("IncubadoraCO2");
        if (co2 != null)
        {
            IncubadoraCO2 = EquipoCentro.Crear(co2, EquipoCentro.Forma.Pared, 13, "INCUBADORA CO2", "SACAR CÉLULAS",
                "Cultivo de células humanas y animales: 37 °C con 5 % de CO2, que mantiene el pH del medio como en el cuerpo.",
                "Las células Vero crecen a 37 °C con 5 % de CO2, que mantiene el pH del medio como en la sangre. Son las células que el virus infectará en el ensayo de la placa.",
                "Se usa en la MISIÓN 3, paso 6 (ensayo preclínico).", "CÉLULAS FUERA · 37 °C · 5 % CO2");
            IncubadoraCO2.EsSuTurno = () => !Obligatorios || (RetoVacuna.Instancia != null && RetoVacuna.Instancia.Actual == EtapaVacuna.Neutralizacion);
            IncubadoraCO2.Animacion = IncubadoraCO2.Bandeja(1, new[] { new Color(0.95f, 0.6f, 0.65f) }, new[] { "VERO" }, true, "EL FRASCO DE CÉLULAS VERO");
        }

        Debug.Log($"[Equipos] Consolas: {string.Join(", ", Todos.Select(e => e.titulo))}" + (Obligatorios ? "" : " (opcionales)"));
        if (!Obligatorios) return;   // las placas A, B y el hisopado C se quedan en el BANCO (1)
        // Las placas A y B empiezan en la incubadora; el hisopado C, dentro de la cabina.
        placaA = rm.placa != null ? rm.placa.transform : null;
        fuenteA = rm.placa != null ? rm.placa.GetComponent<FuenteMuestra>() : null;
        if (banco != null)
        {
            placaB = banco.Cast<Transform>().FirstOrDefault(t => t.name == "MuestraB_PlacaMoho");
            var c = banco.Cast<Transform>().FirstOrDefault(t => t.name == "MuestraC_Hisopado");
            fuenteB = placaB != null ? placaB.GetComponent<FuenteMuestra>() : null;
            if (c != null && cab != null)
            {
                c.SetParent(cab, false);
                c.localPosition = new Vector3(-0.25f, 0.92f, 0.02f);
                c.localRotation = Quaternion.identity;
                fuenteC = c.GetComponent<FuenteMuestra>();
                foreach (var tx in banco.GetComponentsInChildren<TMP_Text>(true))
                    if (tx.text != null && tx.text.Contains("MUESTRA C · Hisopado"))
                        tx.text = "<b>MUESTRA C · Hisopado nasal</b>\n<color=#5FD4FF>EN LA CABINA (10)</color>";
            }
        }
        if (Incubadora != null) MostrarPlacas(false);
        if (fuenteC != null && Cabina != null) fuenteC.bloqueo = BloqueoHisopado;
    }

    // ------------------------------------------------------------------ placas A y B

    static void MostrarPlacas(bool ver)
    {
        if (!ver)
        {
            rendOcultos.Clear();
            foreach (var t in new[] { placaA, placaB })
            {
                if (t == null) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { rendOcultos.Add((r, r.enabled)); r.enabled = false; }
                foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                foreach (var tx in t.GetComponentsInChildren<TMP_Text>(true)) tx.enabled = false;
            }
            if (fuenteA != null) fuenteA.bloqueo = BloqueoPlacas;
            if (fuenteB != null) fuenteB.bloqueo = BloqueoPlacas;
            return;
        }
        foreach (var (r, on) in rendOcultos) if (r != null) r.enabled = on;
        foreach (var t in new[] { placaA, placaB })
        {
            if (t == null) continue;
            foreach (var c in t.GetComponentsInChildren<Collider>(true)) c.enabled = true;
            foreach (var tx in t.GetComponentsInChildren<TMP_Text>(true)) tx.enabled = true;
        }
        rendOcultos.Clear();
        if (fuenteA != null && fuenteA.bloqueo == BloqueoPlacas) fuenteA.bloqueo = null;
        if (fuenteB != null && fuenteB.bloqueo == BloqueoPlacas) fuenteB.bloqueo = null;
    }

    static void AvisarBanco(Transform banco)
    {
        if (banco == null) return;
        var p = banco.TransformPoint(new Vector3(-0.3f, 1.3f, -0.2f));
        Aviso.Mostrar("Placas A y B en el BANCO (1)", p, Aviso.Tipo.Ok, 5f);
    }

    // ------------------------------------------------------------------ reinicios y partidas

    // REINICIAR en el ARCHIVO (8): las misiones 1 y 2 vuelven a empezar con las placas en la incubadora.
    public static void ReiniciarMisiones()
    {
        foreach (var e in new[] { Incubadora, Cabina, Autoclave }) if (e != null) e.Marcar(false);
        if (Incubadora != null && rendOcultos.Count == 0) MostrarPlacas(false);
        if (Cabina != null) { Cabina.Flujo(false); if (fuenteC != null) fuenteC.bloqueo = BloqueoHisopado; }
    }

    public static void ReiniciarVacuna()
    {
        foreach (var e in new[] { Ultracongelador, IncubadoraCO2 }) if (e != null) e.Marcar(false);
    }

    // Partida guardada: lo que ya pasó no se repite.
    public static void Sincronizar(RetoManager rm)
    {
        if (rm == null) return;
        bool empezado = rm.Actual > EtapaReto.Exploracion || (rm.Evidencias != null && rm.Evidencias.Any(x => x));
        if (empezado && Incubadora != null && !Incubadora.Hecho) { Incubadora.Marcar(true); MostrarPlacas(true); }
        if ((rm.VistoVirus || rm.Actual >= EtapaReto.Investigacion) && Cabina != null && !Cabina.Hecho)
        {
            Cabina.Marcar(true); Cabina.Flujo(true);
            if (fuenteC != null) fuenteC.bloqueo = null;
        }
        if (rm.Actual == EtapaReto.Cierre && Autoclave != null) Autoclave.Marcar(true);
    }

    // Pruebas sin visor: pulsa el botón del equipo y espera a que termine.
    public static IEnumerator Usar(EquipoCentro e)
    {
        if (e == null || e.Hecho) yield break;
        EquipoCentro.Pruebas = true;
        e.boton.Activar();
        for (float t = 0f; t < 8f && !e.Hecho; t += Time.deltaTime) yield return null;
        EquipoCentro.Pruebas = false;
    }

    // ------------------------------------------------------------------ misión 3: la guía va al equipo que toca

    void Update()
    {
        var rv = RetoVacuna.Instancia;
        var rm = RetoManager.Instancia;
        EquipoCentro p = null;
        if (rv != null)
        {
            if (rv.Actual == EtapaVacuna.Sintesis && !Listo(Ultracongelador)) p = Ultracongelador;
            else if (rv.Actual == EtapaVacuna.Neutralizacion && !Listo(IncubadoraCO2)) p = IncubadoraCO2;
        }
        if (p == pendienteVacuna || rm == null) return;
        pendienteVacuna = p;
        if (p != null) rm.GuiaExterna(p.guia, $"{ContenidoVacuna.Codigo} · {ContenidoVacuna.Titulo.ToUpper()}",
            $"Antes de seguir con la vacuna: ve al {p.titulo} ({p.guia.numero}) y pulsa {p.accion}.");
        else rm.GuiaExterna(null, null, null);
    }
}

// Consola de un equipo: pantalla, botón, cartel de guía (EstacionGuia) y su pequeña animación.
public class EquipoCentro : MonoBehaviour
{
    public enum Forma { Pared, Encima, Cabina }

    public string titulo, accion, explicacion, cuando, textoHecho;
    public EstacionGuia guia;
    public BotonFisico boton;
    public TMP_Text estado;
    public System.Func<bool> EsSuTurno;
    public System.Func<IEnumerator> Animacion;
    public System.Action AlTerminar;
    public System.Func<string> Requisito;   // lo que falta hacer con las manos antes de pulsar (null = nada)
    public static bool Pruebas;             // pruebas sin visor: no se esperan las manos
    public bool Hecho { get; private set; }
    public bool Ocupado { get; private set; }

    string descripcionInicial;
    Renderer flujo;
    Transform bandeja;
    Vector3 bandejaDentro, bandejaFuera;
    bool recoger;
    string queRecoger;
    readonly List<Transform> piezas = new List<Transform>();
    Transform zonaCarga;
    float frente = 0.35f;

    void Start() { if (boton != null) boton.Pulsado += Pulsar; Refrescar(); }

    void Pulsar()
    {
        if (Ocupado) return;
        var pos = boton.transform.position + Vector3.up * 0.25f;
        if (Hecho) { Aviso.Mostrar($"{titulo}: ya está hecho.\n{explicacion}", pos, Aviso.Tipo.Info, 5f); Sonidos.Clic(pos); return; }
        if (EsSuTurno == null || !EsSuTurno())
        {
            Aviso.Mostrar($"<b>{titulo}</b>\n{guia.descripcion}\n<color=#FFD25A>{cuando}</color>", pos, Aviso.Tipo.Info, 6f);
            Sonidos.Clic(pos);
            return;
        }
        // "FALTA UN PASO" (no cuenta como error): primero lo que se hace con las manos.
        string falta = Pruebas ? null : Requisito?.Invoke();
        if (falta != null)
        {
            Aviso.Mostrar($"<b>FALTA UN PASO</b>\n{falta}", pos, Aviso.Tipo.Info, 5f);
            Sonidos.Pista(pos);
            if (zonaCarga != null) StartCoroutine(Resaltar(zonaCarga));
            return;
        }
        StartCoroutine(Usar());
    }

    IEnumerator Usar()
    {
        Ocupado = true;
        var pos = boton.transform.position + Vector3.up * 0.25f;
        Sonidos.Carga(pos);
        ManosRig.Vibrar(pos, 0.4f, 0.08f);
        if (Animacion != null) yield return StartCoroutine(Animacion());
        else { estado.text = $"<color=#FFD25A>{accion}…</color>"; yield return new WaitForSeconds(0.8f); }
        Ocupado = false;
        Marcar(true);
        Sonidos.Logro(pos);
        Aviso.Mostrar($"<b>{titulo}</b> · {explicacion}", pos + Vector3.up * 0.1f, Aviso.Tipo.Ok, 7f);
        AlTerminar?.Invoke();
    }

    public void Marcar(bool hecho)
    {
        StopAllCoroutines();
        Ocupado = false;
        Hecho = hecho;
        guia.descripcion = hecho ? $"<color=#7CFF9E>OK · {textoHecho}</color>" : descripcionInicial;
        if (bandeja != null) { bandeja.localPosition = bandejaDentro; bandeja.gameObject.SetActive(false); }
        if (guia != null && !guia.Activa) guia.Mostrar(false, null);
        Refrescar();
    }

    // El cartel: AHORA cuando es la meta de la guía; si no, qué es el equipo o que ya se usó.
    public void Refrescar()
    {
        if (estado == null) return;
        bool turno = !Hecho && EsSuTurno != null && EsSuTurno();
        string falta = turno ? Requisito?.Invoke() : null;
        if (zonaCarga != null) zonaCarga.gameObject.SetActive(turno);
        estado.text = Hecho ? $"<color=#7CFF9E>OK · {textoHecho}</color>"
                    : turno && falta != null ? "<color=#FFD25A>▼ DEJA EL PORTAOBJETOS EN LA BANDEJA</color>"
                    : turno ? $"<color=#FFD25A>▼ PULSA EL BOTÓN: {accion}</color>"
                    : $"<color=#A9BED2>BOTÓN: {accion} · {cuando}</color>";
    }

    float siguienteRefresco;
    void Update()
    {
        if (Time.time < siguienteRefresco || Ocupado) return;
        siguienteRefresco = Time.time + 0.5f;
        // Menos carga visual: la pantalla del equipo solo se enciende al acercarse (o si es el paso de la guía).
        var cab = ManosRig.Cabeza;
        var pant = transform.Find("Consola/Pantalla");
        if (pant != null && cab != null)
        {
            var d = cab.position - transform.position; d.y = 0f;
            bool ver = d.sqrMagnitude < 2.5f * 2.5f || (guia != null && guia.Activa);
            if (pant.gameObject.activeSelf != ver) pant.gameObject.SetActive(ver);
        }
        Refrescar();
        if (guia != null && !guia.Activa && RetoManager.Instancia != null && RetoManager.Instancia.Actual == EtapaReto.Cierre) guia.Mostrar(false, null);
    }

    // ------------------------------------------------------------------ animaciones

    // Bandeja que sale del equipo con placas o tubos y vuelve a entrar.
    // 'recoger': la bandeja sale y se queda fuera hasta que el usuario toca con la mano cada placa o tubo para sacarlo.
    public System.Func<IEnumerator> Bandeja(int n, Color[] colores, string[] rotulos, bool recoger = false, string queRecoger = null)
    {
        this.recoger = recoger;
        this.queRecoger = queRecoger;
        piezas.Clear();
        bandeja = K.Grupo(transform.Find("Consola"), "Bandeja", Vector3.zero);
        float y = 0.62f;
        bandejaDentro = new Vector3(0f, y, frente - 0.2f);
        bandejaFuera = new Vector3(0f, y, frente + 0.18f);
        bandeja.localPosition = bandejaDentro;
        K.Caja(bandeja, "Fondo", Vector3.zero, new Vector3(0.5f, 0.015f, 0.28f), K.Escena("Cromo", new Color(0.8f, 0.82f, 0.85f)));
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) / 2f) * 0.14f;
            var pz = K.Grupo(bandeja, "Pieza" + i, new Vector3(x, 0f, 0f));
            piezas.Add(pz);
            K.Pieza(pz, "Envase", PrimitiveType.Cylinder, new Vector3(0f, 0.017f, 0f), new Vector3(0.1f, 0.009f, 0.1f), K.Vidrio(new Color(0.85f, 0.95f, 1f, 0.35f)));
            K.Pieza(pz, "Contenido", PrimitiveType.Cylinder, new Vector3(0f, 0.014f, 0f), new Vector3(0.09f, 0.005f, 0.09f), K.Plano(colores[i % colores.Length]));
            var t = K.Texto(pz, $"<b>{rotulos[i % rotulos.Length]}</b>", new Vector3(0f, 0.08f, 0f), 0.22f, Color.white, 0.12f, 0.05f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
            t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 220);
        }
        bandeja.gameObject.SetActive(false);
        return BandejaFuera;
    }

    IEnumerator BandejaFuera()
    {
        bandeja.gameObject.SetActive(true);
        estado.text = $"<color=#FFD25A>PUERTA ABIERTA · {accion}…</color>";
        Sonidos.Puerta(bandeja.position, true);
        yield return Mover(bandejaDentro, bandejaFuera, 0.9f);
        if (recoger) yield return Recoger();
        else yield return new WaitForSeconds(1.2f);
        yield return Mover(bandejaFuera, bandejaDentro, 0.7f);
        bandeja.gameObject.SetActive(false);
        Sonidos.Puerta(bandeja.position, false);
    }

    // Espera a que la mano toque cada pieza de la bandeja (las que faltan laten). Se hace con la mano, no con el rayo.
    IEnumerator Recoger()
    {
        foreach (var pz in piezas) { pz.gameObject.SetActive(true); pz.localScale = Vector3.one; }
        int n = piezas.Count, cogidas = 0;
        string que = queRecoger ?? "LO DE LA BANDEJA";
        Aviso.Mostrar($"Saca {que.ToLower()}: tócalos con la mano.", bandeja.position + Vector3.up * 0.3f, Aviso.Tipo.Info, 4f);
        while (cogidas < n)
        {
            estado.text = $"<color=#FFD25A>▼ TOCA CON LA MANO {que} ({cogidas}/{n})</color>";
            float pulso = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
            foreach (var pz in piezas)
            {
                if (!pz.gameObject.activeSelf) continue;
                pz.localScale = Vector3.one * pulso;
                if (Pruebas || ManosRig.MasCercana(pz.position, 0.14f, out _))
                {
                    pz.gameObject.SetActive(false);
                    cogidas++;
                    Sonidos.Encaje(pz.position);
                    ManosRig.Vibrar(pz.position, 0.5f, 0.06f);
                    break;
                }
            }
            yield return null;
        }
        estado.text = "<color=#7CFF9E>¡LISTO! YA ESTÁ FUERA</color>";
        yield return new WaitForSeconds(0.5f);
        foreach (var pz in piezas) { pz.gameObject.SetActive(true); pz.localScale = Vector3.one; }
    }

    // Bandeja de carga marcada encima del equipo (autoclave): ahí se deja el material antes de pulsar.
    public Transform ZonaCarga(string rotulo)
    {
        var cuerpo = transform.Find("Cuerpo");
        float alto = cuerpo != null ? cuerpo.localScale.y : 1.05f;
        float ancho = cuerpo != null ? cuerpo.localScale.x : 0.6f;
        var g = K.Grupo(transform.Find("Consola"), "ZonaCarga", new Vector3(-Mathf.Min(0.2f, ancho * 0.3f), alto + 0.002f, frente - 0.15f));
        K.Caja(g, "Bandeja", Vector3.zero, new Vector3(0.24f, 0.004f, 0.16f), K.Plano(new Color(0.2f, 0.55f, 0.75f)));
        K.Caja(g, "Borde", new Vector3(0f, -0.001f, 0f), new Vector3(0.27f, 0.003f, 0.19f), K.Plano(K.Amarillo));
        var t = K.Texto(g, $"<b>{rotulo}</b>", new Vector3(0f, 0.09f, -0.05f), 0.16f, Color.white, 0.3f, 0.06f, TextAlignmentOptions.Center, true, false, Quaternion.Euler(0f, 180f, 0f));
        t.outlineWidth = 0.25f; t.outlineColor = new Color32(0, 0, 0, 220);
        zonaCarga = g;
        g.gameObject.SetActive(false);
        return g;
    }

    IEnumerator Resaltar(Transform t)
    {
        for (float k = 0f; k < 1.2f; k += Time.deltaTime)
        {
            t.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(k * 12f));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator Mover(Vector3 a, Vector3 b, float s)
    {
        for (float t = 0f; t < s; t += Time.deltaTime)
        {
            bandeja.localPosition = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t / s));
            yield return null;
        }
        bandeja.localPosition = b;
    }

    public System.Func<IEnumerator> Ciclo(string[] fases, float porFase) => () => CicloFases(fases, porFase);

    IEnumerator CicloFases(string[] fases, float porFase)
    {
        foreach (var f in fases)
        {
            estado.text = $"<color=#FFD25A>{f}</color>";
            Sonidos.Scan(transform.position + Vector3.up);
            yield return new WaitForSeconds(porFase);
        }
    }

    public void Flujo(bool on) { if (flujo != null) flujo.enabled = on; }

    // ------------------------------------------------------------------ montaje

    // 'eq' es la raíz del equipo (a ras de suelo, su frente mira a +z local, donde se pone el usuario).
    public static EquipoCentro Crear(Transform eq, Forma forma, int numero, string titulo, string accion, string descripcion,
                                     string explicacion, string cuando, string textoHecho)
    {
        var e = eq.gameObject.AddComponent<EquipoCentro>();
        e.titulo = titulo; e.accion = accion; e.explicacion = explicacion; e.cuando = cuando; e.textoHecho = textoHecho;
        e.descripcionInicial = descripcion;
        var g = K.Grupo(eq, "Consola", Vector3.zero);
        float frente = 0.35f;
        var cuerpo = eq.Find("Cuerpo");
        if (cuerpo != null) frente = cuerpo.localScale.z / 2f;
        e.frente = frente;

        Vector3 pPant, pBoton; Quaternion rPant;
        switch (forma)
        {
            case Forma.Encima:
            {
                float alto = cuerpo != null ? cuerpo.localScale.y : 1.05f;
                pBoton = new Vector3(0f, alto + 0.001f, frente - 0.12f);
                pPant = new Vector3(0f, alto + 0.3f, -0.08f);
                rPant = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-12f, 0f, 0f);
                K.Caja(g, "Soporte", new Vector3(0f, alto + 0.08f, -0.1f), new Vector3(0.05f, 0.16f, 0.03f), K.Escena("Marco", Color.gray));
                break;
            }
            case Forma.Cabina:
                frente = 0.375f;
                e.frente = frente;
                pBoton = new Vector3(0.52f, 0.921f, 0.2f);
                // Pantalla en la pared del fondo, dentro de la cabina (se ve por el frente abierto).
                pPant = new Vector3(0.22f, 1.36f, -0.285f);
                rPant = Quaternion.Euler(0f, 180f, 0f);
                // Indicador del flujo: franja verde bajo la luz UV, apagada hasta encenderlo.
                e.flujo = K.Caja(g, "Flujo", new Vector3(0f, 1.84f, 0.05f), new Vector3(1.38f, 0.02f, 0.03f), K.Plano(new Color(0.35f, 1f, 0.55f)));
                e.flujo.enabled = false;
                break;
            default:
                pBoton = new Vector3(0f, 0.951f, frente + 0.08f);
                pPant = new Vector3(0f, 1.2f, frente + 0.015f);
                rPant = Quaternion.Euler(0f, 180f, 0f);
                K.Solido(K.Caja(g, "Repisa", new Vector3(0f, 0.94f, frente + 0.075f), new Vector3(0.26f, 0.02f, 0.15f), K.Escena("Marco", Color.gray)));
                break;
        }

        e.boton = MontajeVacuna.Boton(g, "", pBoton, new Color(1f, 0.75f, 0.25f), EstiloNanoLab.Icono.Pulsar, "Equipo" + numero);

        // Pantalla (se lee desde -z local; girada 180° mira al usuario).
        var pant = K.Grupo(g, "Pantalla", pPant, rPant);
        const float w = 0.56f, h = 0.36f;
        var marco = K.Caja(pant, "MarcoActivo", new Vector3(0f, 0f, 0.004f), new Vector3(w + 0.03f, h + 0.03f, 0.004f), K.Plano(K.Amarillo));
        marco.enabled = false;
        K.Panel(pant, "Fondo", Vector3.zero, w, h, AdaptadorEscena.ColorPanel, AdaptadorEscena.BordePanel, 0.003f, 0.03f);
        var tit = K.Texto(pant, "", new Vector3(0f, h / 2f - 0.02f, -0.004f), 0.24f, K.Cian, w - 0.04f, 0.06f, TextAlignmentOptions.Top, true, true);
        K.AutoTam(tit, 0.14f);
        var cuerpoTxt = K.Texto(pant, "", new Vector3(0f, h / 2f - 0.085f, -0.004f), 0.17f, Color.white, w - 0.04f, 0.19f, TextAlignmentOptions.TopLeft, false, true);
        K.AutoTam(cuerpoTxt, 0.1f);
        e.estado = K.Texto(pant, "", new Vector3(0f, -h / 2f + 0.035f, -0.004f), 0.17f, K.Amarillo, w - 0.04f, 0.05f, TextAlignmentOptions.Center, true, false);
        K.AutoTam(e.estado, 0.1f);

        e.guia = eq.gameObject.AddComponent<EstacionGuia>();
        e.guia.numero = numero;
        e.guia.titulo = titulo;
        e.guia.descripcion = descripcion;
        e.guia.tituloTxt = tit;
        e.guia.textoTxt = cuerpoTxt;
        e.guia.marcoActivo = marco;
        e.guia.puntoBaliza = K.Grupo(eq, "PuntoBaliza", new Vector3(0f, 0f, frente + 0.9f));
        e.guia.Mostrar(false, null);
        return e;
    }
}
