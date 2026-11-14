using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ajustes de la revisión del profesor que se aplican al cargar la escena, sin volver a montarla ni hornearla
// (así no se toca Microbiologia.unity, que el equipo edita a la vez y es muy fácil de romper en un merge):
//  1) Fuera los rótulos flotantes de las áreas (BalizaArea: "4 · CENTRO DE EXPERIMENTACIÓN · Caso 01", "CEPARIO"...).
//  2) Fuera el tótem-letrero del pasillo "CENTRO DE EXPERIMENTACIÓN > ¡Nuevo! Resuelve el CASO 01".
//  4) Un microscopio más en cada mesa de MICROSCOPÍA (3) del centro: se clona el puesto que ya hay (con su platina y su
//     MicroscopioOptico) y se coloca en el lado libre de la misma mesa.
// Es idempotente: si la escena ya viene sin rótulos o con los microscopios extra, no hace nada.
public static class LimpiezaEscena
{
    const string Marca = "PuestoExtra";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AlCargar()
    {
        Aplicar(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    static void AlCargarEscena(Scene s, LoadSceneMode m) => Aplicar(s);

    public static void Aplicar(Scene escena)
    {
        if (!escena.IsValid() || !escena.isLoaded) return;
        var todos = escena.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToList();
        int rotulos = 0, carteles = 0, microscopios = 0;

        // 1) Rótulos de área.
        foreach (var b in todos.Select(t => t != null ? t.GetComponent<BalizaArea>() : null).Where(b => b != null).ToList())
        {
            b.gameObject.SetActive(false);
            Object.Destroy(b.gameObject);
            rotulos++;
        }
        // 2) Tótem del pasillo que mandaba al centro.
        foreach (var t in todos.Where(t => t != null && t.name == "HaciaElCentro").ToList())
        {
            t.gameObject.SetActive(false);
            Object.Destroy(t.gameObject);
            carteles++;
        }
        // 4) Microscopio extra por mesa.
        foreach (var est in todos.Where(t => t != null && t.name == "3_Microscopia").ToList())
            microscopios += MicroscopiosExtra(est);

        if (rotulos + carteles + microscopios > 0)
            Debug.Log($"[Revisión] Quitados {rotulos} rótulos de área y {carteles} letrero(s); añadidos {microscopios} microscopio(s) en MICROSCOPÍA (3).");
    }

    // En cada mesa con UN microscopio, clona su puesto y lo pone a 0,55 m, del lado con más mesa libre.
    static int MicroscopiosExtra(Transform estacion)
    {
        if (estacion.Cast<Transform>().Any(t => t.name == Marca)) return 0;
        var puestos = estacion.Cast<Transform>().Where(t => t.name == "Puesto" && t.GetComponentInChildren<MicroscopioOptico>(true) != null).ToList();
        var mesas = estacion.Cast<Transform>().Where(t => t.name == "Mesa").ToList();
        int n = 0;
        foreach (var p in puestos)
        {
            float x = estacion.InverseTransformPoint(p.position).x;
            // Mesa debajo de este puesto (la más cercana en x) y su ancho en el eje x de la estación.
            float min = x - 0.9f, max = x + 0.9f;
            var mesa = mesas.OrderBy(m => Mathf.Abs(estacion.InverseTransformPoint(Centro(m)).x - x)).FirstOrDefault();
            if (mesa != null && Limites(estacion, mesa, out float m0, out float m1)) { min = m0; max = m1; }
            const float Separacion = 0.55f, Margen = 0.16f;
            float izq = x - Separacion, der = x + Separacion;
            bool cabeIzq = izq > min + Margen, cabeDer = der < max - Margen;
            if (!cabeIzq && !cabeDer) continue;
            float nx = cabeIzq && cabeDer ? (x - min > max - x ? izq : der) : cabeIzq ? izq : der;

            var copia = Object.Instantiate(p.gameObject, p.parent);
            copia.name = Marca;
            var lp = p.localPosition;
            copia.transform.localPosition = new Vector3(lp.x + (nx - x), lp.y, lp.z);
            copia.transform.localRotation = p.localRotation;
            // Sin portaobjetos de la copia (si el puesto tenía uno encima, se quedaría duplicado).
            foreach (var s in copia.GetComponentsInChildren<Portaobjetos>(true)) Object.Destroy(s.gameObject);
            foreach (var r in copia.GetComponentsInChildren<Renderer>(true))
                if (r.isPartOfStaticBatch) Debug.LogWarning($"[Revisión] '{r.name}' del microscopio estaba en un lote estático: la copia puede verse mal.");
            n++;
        }
        return n;
    }

    static Vector3 Centro(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return t.position;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.center;
    }

    static bool Limites(Transform estacion, Transform mesa, out float min, out float max)
    {
        min = float.MaxValue; max = float.MinValue;
        var rs = mesa.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return false;
        foreach (var r in rs)
        {
            var b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                float lx = estacion.InverseTransformPoint(c).x;
                min = Mathf.Min(min, lx); max = Mathf.Max(max, lx);
            }
        }
        return max > min;
    }
}
