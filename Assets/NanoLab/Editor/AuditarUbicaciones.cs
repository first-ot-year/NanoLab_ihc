using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// NanoLab › Auditar ubicaciones (play mode): revisa, con la escena ya montada al jugar, dónde están las cosas que el
// estudiante toca y escribe un informe en Docs/Auditoria_Ubicaciones.md. Busca:
//   - FLOTA:    un objeto que se agarra (portaobjetos, piezas de la vacuna, objetos guardables) con más de 2 cm de aire
//               debajo, o sin nada debajo.
//   - HUNDIDO:  su base queda más de 1 cm por dentro de la superficie que lo sostiene.
//   - ALTURA:   algo que se toca (botón, frasco, muestra, hueco donde se encaja) por debajo de 0,75 m o por encima de
//               1,45 m del suelo (rango cómodo de pie para 15 años, sin agacharse ni estirarse).
//   - FONDO:    a más de 0,5 m del borde de la mesa que lo sostiene (el mueble frena el cuerpo: más allá hay que
//               estirar el brazo del todo).
//   - CHOQUE:   dos estaciones o equipos del centro cuyos volúmenes se meten uno en otro más de 5 cm.
// Con "Corregir lo que flota" baja hasta la superficie los objetos agarrables que flotan (solo en esta sesión de Play:
// sirve para comprobar el arreglo antes de llevarlo al código de montaje).
public static class AuditarUbicaciones
{
    const float MaxAire = 0.02f, MaxHundido = 0.01f, AltoMin = 0.75f, AltoMax = 1.45f, MaxFondo = 0.5f, MinChoque = 0.05f;

    [MenuItem("NanoLab/Auditar ubicaciones (play mode)", false, 40)]
    public static void Auditar() => Ejecutar(false);

    [MenuItem("NanoLab/Auditar ubicaciones y corregir lo que flota (play mode)", false, 41)]
    public static void AuditarYCorregir() => Ejecutar(true);

    [MenuItem("NanoLab/Auditar ubicaciones (play mode)", true)]
    [MenuItem("NanoLab/Auditar ubicaciones y corregir lo que flota (play mode)", true)]
    static bool Valido() => EditorApplication.isPlaying;

    static void Ejecutar(bool corregir)
    {
        Physics.SyncTransforms();
        float suelo = RetoManager.Instancia != null ? RetoManager.Instancia.zona.min.y : 0.07f;
        var hallazgos = new List<(string tipo, string zona, string que, string detalle)>();

        // ---- objetos que se agarran: ¿flotan o se hunden?
        var agarrables = new List<Transform>();
        agarrables.AddRange(Encajable.Todos.Where(e => e != null && !e.Agarrado && e.gameObject.activeInHierarchy).Select(e => e.transform));
        agarrables.AddRange(Portaobjetos.Todos.Where(p => p != null && !p.Agarrado && p.gameObject.activeInHierarchy).Select(p => p.transform));
        agarrables.AddRange(Guardable.Todos.Where(g => g != null && !g.Agarrado && g.gameObject.activeInHierarchy).Select(g => g.transform));
        int corregidos = 0;
        foreach (var t in agarrables.Distinct())
        {
            if (!Limites(t, out var b)) continue;
            if (!SuperficieDebajo(t, b, out var hit))
            {
                hallazgos.Add(("FLOTA", Zona(t), Ruta(t), "no hay nada debajo"));
                continue;
            }
            float aire = b.min.y - hit.point.y;
            if (aire > MaxAire)
            {
                hallazgos.Add(("FLOTA", Zona(t), Ruta(t), $"{aire * 100f:0} cm sobre {hit.collider.name}"));
                if (corregir) { t.position -= Vector3.up * aire; corregidos++; }
            }
            else if (aire < -MaxHundido)
                hallazgos.Add(("HUNDIDO", Zona(t), Ruta(t), $"{-aire * 100f:0} cm dentro de {hit.collider.name}"));
        }

        // ---- cosas que se tocan: altura y fondo
        var tocables = new List<Transform>();
        tocables.AddRange(Object.FindObjectsOfType<BotonFisico>().Select(x => x.transform));
        tocables.AddRange(PuntoPipeta.Todos.Where(p => p != null && p.EsFuente).Select(p => p.transform));
        tocables.AddRange(Object.FindObjectsOfType<FuenteMuestra>().Select(x => x.transform));
        tocables.AddRange(Object.FindObjectsOfType<ZonaEncaje>().Select(x => x.transform));
        tocables.AddRange(agarrables);
        foreach (var t in tocables.Where(t => t != null && t.gameObject.activeInHierarchy && Dentro(t)).Distinct())
        {
            if (t.GetComponentInParent<MenuAjustes>() != null || t.GetComponentInParent<JugadorNanoLab>() != null) continue;
            float alto = t.position.y - suelo;
            if (alto < AltoMin || alto > AltoMax)
                hallazgos.Add(("ALTURA", Zona(t), Ruta(t), $"a {alto:0.00} m del suelo (cómodo: {AltoMin}-{AltoMax} m)"));
            if (Physics.Raycast(t.position + Vector3.up * 0.05f, Vector3.down, out var mesa, 0.6f, ~0, QueryTriggerInteraction.Ignore) && mesa.collider.attachedRigidbody == null)
            {
                var mb = mesa.collider.bounds;
                // Distancia al borde más cercano por el que se puede llegar (el más próximo en XZ).
                float fondo = Mathf.Min(Mathf.Min(t.position.x - mb.min.x, mb.max.x - t.position.x), Mathf.Min(t.position.z - mb.min.z, mb.max.z - t.position.z));
                if (fondo > MaxFondo && mb.size.x < 6f && mb.size.z < 6f)
                    hallazgos.Add(("FONDO", Zona(t), Ruta(t), $"a {fondo:0.00} m del borde de {mesa.collider.name}"));
            }
        }

        // ---- estaciones y equipos que se pisan
        var raiz = GameObject.Find("CentroExperimentacion");
        var piezas = new List<(string nombre, Bounds b)>();
        if (raiz != null)
            foreach (var grupo in new[] { "Estaciones", "Equipamiento" })
            {
                var g = raiz.transform.Find(grupo);
                if (g == null) continue;
                foreach (Transform e in g)
                    if (e.gameObject.activeInHierarchy && Limites(e, out var b)) piezas.Add((e.name, b));
            }
        var vac = GameObject.Find(MontajeVacuna.NombreRaiz);
        if (vac != null && Limites(vac.transform, out var bv)) piezas.Add((MontajeVacuna.NombreRaiz, bv));
        var conc = Object.FindObjectOfType<PanelConclusiones>();
        if (conc != null && Limites(conc.transform, out var bc)) piezas.Add(("PanelConclusiones", bc));
        for (int i = 0; i < piezas.Count; i++)
            for (int j = i + 1; j < piezas.Count; j++)
            {
                var a = piezas[i].b; var c = piezas[j].b;
                float dx = Mathf.Min(a.max.x, c.max.x) - Mathf.Max(a.min.x, c.min.x);
                float dz = Mathf.Min(a.max.z, c.max.z) - Mathf.Max(a.min.z, c.min.z);
                float dy = Mathf.Min(a.max.y, c.max.y) - Mathf.Max(a.min.y, c.min.y);
                if (dx > MinChoque && dz > MinChoque && dy > 0.3f)
                    hallazgos.Add(("CHOQUE", "Centro", $"{piezas[i].nombre} ↔ {piezas[j].nombre}", $"se meten {dx * 100f:0} × {dz * 100f:0} cm"));
            }

        Escribir(hallazgos, corregidos);
    }

    // ------------------------------------------------------------------ utilidades

    static bool Dentro(Transform t)
    {
        var rm = RetoManager.Instancia;
        if (rm != null && rm.zona.Contains(t.position)) return true;
        var rv = RetoVacuna.Instancia;
        return rv != null && t.IsChildOf(rv.transform);
    }

    static bool Limites(Transform t, out Bounds b)
    {
        b = default;
        bool hay = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
            if (r.GetComponent<TMPro.TMP_Text>() != null) continue;
            if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    // Primera superficie sólida debajo del objeto que no es parte de él.
    static bool SuperficieDebajo(Transform t, Bounds b, out RaycastHit hit)
    {
        var hits = Physics.RaycastAll(new Vector3(b.center.x, b.max.y + 0.01f, b.center.z), Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore)
                          .OrderBy(h => h.distance);
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(t) || h.collider.GetComponentInParent<JugadorNanoLab>() != null) continue;
            if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
            hit = h;
            return true;
        }
        hit = default;
        return false;
    }

    static string Zona(Transform t)
    {
        if (RetoVacuna.Instancia != null && t.IsChildOf(RetoVacuna.Instancia.transform)) return "Misión 3 · vacunas";
        for (var p = t; p != null; p = p.parent)
            if (p.parent != null && (p.parent.name == "Estaciones" || p.parent.name == "Equipamiento")) return p.name;
        return "Centro";
    }

    static string Ruta(Transform t)
    {
        var partes = new List<string>();
        for (var p = t; p != null && partes.Count < 4; p = p.parent) partes.Insert(0, p.name);
        return string.Join("/", partes);
    }

    static void Escribir(List<(string tipo, string zona, string que, string detalle)> h, int corregidos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Auditoría de ubicaciones");
        sb.AppendLine();
        sb.AppendLine($"Generado con `NanoLab › Auditar ubicaciones (play mode)` el {System.DateTime.Now:dd/MM/yyyy HH:mm}. Etapa del reto: " +
                      $"{(RetoVacuna.Instancia != null ? RetoVacuna.Instancia.Actual.ToString() : "-")}.");
        sb.AppendLine();
        sb.AppendLine($"Criterios: flota > {MaxAire * 100f:0} cm · hundido > {MaxHundido * 100f:0} cm · altura cómoda {AltoMin}-{AltoMax} m · fondo máximo {MaxFondo} m desde el borde · choque > {MinChoque * 100f:0} cm.");
        if (corregidos > 0) sb.AppendLine($"\nSe bajaron {corregidos} objetos que flotaban (solo en esta sesión de Play).");
        sb.AppendLine();
        if (h.Count == 0) sb.AppendLine("Sin hallazgos.");
        foreach (var grupo in h.GroupBy(x => x.tipo).OrderBy(g => g.Key))
        {
            sb.AppendLine($"## {grupo.Key} ({grupo.Count()})");
            sb.AppendLine();
            sb.AppendLine("| Zona | Objeto | Detalle |");
            sb.AppendLine("|---|---|---|");
            foreach (var x in grupo.OrderBy(x => x.zona).ThenBy(x => x.que)) sb.AppendLine($"| {x.zona} | `{x.que}` | {x.detalle} |");
            sb.AppendLine();
        }
        var ruta = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Docs", "Auditoria_Ubicaciones.md");
        File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(false));
        Debug.Log($"[Auditoría] {h.Count} hallazgos ({string.Join(", ", h.GroupBy(x => x.tipo).Select(g => $"{g.Key} {g.Count()}"))}). Informe: {ruta}");
    }
}
