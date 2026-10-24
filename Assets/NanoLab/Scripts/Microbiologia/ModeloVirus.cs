using System.Collections.Generic;
using UnityEngine;

// Crea el modelo 3D de un virus con material de holograma. Usa el modelo del NIH
// si la muestra lo tiene; si no, genera uno provisional por código.
// Color por capas: cuerpo, espículas, proteínas de membrana e interior (ver NanoLab/Holograma).
public static class ModeloVirus
{
    // Paleta común al modelo y a la leyenda del HUD: espícula, cuerpo, proteína 1, proteína 2, interior.
    public static Color[] Paleta(MuestraData m) => new[]
    {
        m.color,
        new Color(0.16f, 0.3f, 0.78f),
        new Color(0.62f, 0.32f, 0.95f),
        new Color(1f, 0.62f, 0.22f),
        new Color(0.78f, 0.3f, 0.85f),
    };

    static readonly Dictionary<MuestraData, float> radioCuerpo = new Dictionary<MuestraData, float>();

    public static GameObject Instanciar(MuestraData m, Material baseMat, Transform padre, float diametro)
    {
        GameObject go;
        if (m.modelo3D != null)
        {
            go = Object.Instantiate(m.modelo3D);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = CopiaHolo(baseMat, mats[i]);
                r.sharedMaterials = mats;
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Quitar(c);
        }
        else go = Procedural(m, baseMat);

        go.name = "Virus_" + m.name;
        go.transform.SetParent(padre, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        // Normaliza tamaño y centra en el padre.
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (max > 1e-5f)
            {
                // Los bounds están en mundo: escalar por s deja el diámetro en `diametro` metros
                // y hay que desplazar el centro escalado hasta el origen del padre.
                float s = diametro / max;
                go.transform.localScale = Vector3.one * s;
                go.transform.position -= (b.center - padre.position) * s;
            }
        }
        Capas(go, m, padre.position, diametro / 2f);
        return go;
    }

    // Pasa a cada material el centro y el radio del virus en su espacio de objeto y la paleta.
    static void Capas(GameObject go, MuestraData m, Vector3 centro, float radio)
    {
        if (!radioCuerpo.TryGetValue(m, out float rc)) radioCuerpo[m] = rc = RadioCuerpo(go, centro, radio);
        var pal = Paleta(m);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var c = r.transform.InverseTransformPoint(centro);
            float ro = radio / Mathf.Max(1e-5f, r.transform.lossyScale.x);
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null || !mat.HasProperty("_Radial")) continue;
                mat.SetFloat("_Radial", 1f);
                mat.SetVector("_CentroObj", c);
                mat.SetFloat("_RadioObj", ro);
                mat.SetFloat("_RCuerpo", rc);
                mat.SetColor("_ColEspicula", pal[0]);
                mat.SetColor("_ColCuerpo", pal[1]);
                mat.SetColor("_ColProt1", pal[2]);
                mat.SetColor("_ColProt2", pal[3]);
                mat.SetColor("_ColInterior", pal[4]);
            }
        }
    }

    // Radio de la membrana (0..1 del radio total): la distancia al centro donde más vértices hay
    // (la superficie del cuerpo); lo que sobresale son espículas.
    static float RadioCuerpo(GameObject go, Vector3 centro, float radio)
    {
        const int Bins = 60;
        var hist = new int[Bins];
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = mf.sharedMesh;
            if (mesh == null || !mesh.isReadable) continue;
            var verts = mesh.vertices;
            var tr = mf.transform;
            int paso = Mathf.Max(1, verts.Length / 40000);
            for (int i = 0; i < verts.Length; i += paso)
            {
                float d = (tr.TransformPoint(verts[i]) - centro).magnitude / radio;
                int b = Mathf.Clamp((int)(d * Bins), 0, Bins - 1);
                hist[b]++;
            }
        }
        int mejor = -1;
        for (int b = (int)(Bins * 0.45f); b < (int)(Bins * 0.97f); b++) if (mejor < 0 || hist[b] > hist[mejor]) mejor = b;
        if (mejor < 0 || hist[mejor] == 0) return 0.75f;
        return Mathf.Clamp((mejor + 0.5f) / Bins, 0.5f, 0.92f);
    }

    static Material CopiaHolo(Material baseMat, Material original)
    {
        var m = new Material(baseMat);
        if (original != null)
        {
            if (original.HasProperty("_MainTex") && original.GetTexture("_MainTex") != null) m.SetTexture("_MainTex", original.GetTexture("_MainTex"));
            if (original.HasProperty("_Color")) m.SetColor("_Color", original.GetColor("_Color"));
            else if (original.HasProperty("_BaseColor")) m.SetColor("_Color", original.GetColor("_BaseColor"));
        }
        return m;
    }

    static void Quitar(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
    }

    // ---------- Virus provisional ----------

    static Mesh esfera, cilindro;

    static Mesh MallaPrimitiva(PrimitiveType t)
    {
        var g = GameObject.CreatePrimitive(t);
        var mesh = g.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(g);
        return mesh;
    }

    struct Pieza { public Mesh mesh; public Matrix4x4 m; public int color; }

    static GameObject Procedural(MuestraData muestra, Material baseMat)
    {
        if (esfera == null) esfera = MallaPrimitiva(PrimitiveType.Sphere);
        if (cilindro == null) cilindro = MallaPrimitiva(PrimitiveType.Cylinder);

        string n = (muestra.nombre + " " + muestra.nombreCientifico).ToLowerInvariant();
        Color cuerpo = Color.Lerp(muestra.color, new Color(0.15f, 0.25f, 0.6f), 0.55f);
        Color espiga = muestra.color;
        Color interior = new Color(0.75f, 0.3f, 0.9f);
        var colores = new[] { cuerpo, espiga, interior };
        var piezas = new List<Pieza>();

        void Add(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 esc, int color) =>
            piezas.Add(new Pieza { mesh = mesh, m = Matrix4x4.TRS(pos, rot, esc), color = color });

        // Núcleo (se ve en la vista en corte) y cápsula exterior.
        Add(esfera, Vector3.zero, Quaternion.identity, Vector3.one * 1.1f, 2);
        Add(esfera, Vector3.zero, Quaternion.identity, Vector3.one * 2f, 0);

        int cantidad; float largo, cabeza; bool bola;
        if (n.Contains("papiloma") || n.Contains("vph")) { cantidad = 72; largo = 0f; cabeza = 0.32f; bola = true; }
        else if (n.Contains("hepatitis")) { cantidad = 90; largo = 0f; cabeza = 0.22f; bola = true; }
        else if (n.Contains("vih") || n.Contains("hiv")) { cantidad = 28; largo = 0.12f; cabeza = 0.2f; bola = true; }
        else if (n.Contains("influenza")) { cantidad = 80; largo = 0.28f; cabeza = 0.09f; bola = false; }
        else { cantidad = 42; largo = 0.32f; cabeza = 0.17f; bola = true; }   // SARS-CoV-2 y resto

        float phi = Mathf.PI * (3f - Mathf.Sqrt(5f));
        for (int i = 0; i < cantidad; i++)
        {
            float y = 1f - (i / (float)(cantidad - 1)) * 2f;
            float r = Mathf.Sqrt(1f - y * y);
            float th = phi * i;
            var dir = new Vector3(Mathf.Cos(th) * r, y, Mathf.Sin(th) * r);
            var rot = Quaternion.FromToRotation(Vector3.up, dir);
            if (largo > 0f)
                Add(cilindro, dir * (1f + largo * 0.5f), rot, new Vector3(0.06f, largo * 0.5f, 0.06f), 1);
            var pos = dir * (1f + largo + (bola ? cabeza * 0.35f : 0f));
            if (bola) Add(esfera, pos, rot, Vector3.one * cabeza, 1);
            else Add(cilindro, pos, rot, new Vector3(cabeza * 1.6f, cabeza * 0.4f, cabeza * 1.6f), 1);
        }

        // Una malla con una submalla por color.
        var sub = new List<CombineInstance>[3] { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
        foreach (var p in piezas) sub[p.color].Add(new CombineInstance { mesh = p.mesh, transform = p.m });
        var partes = new List<CombineInstance>();
        var mats = new List<Material>();
        for (int c = 0; c < 3; c++)
        {
            if (sub[c].Count == 0) continue;
            var parte = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            parte.CombineMeshes(sub[c].ToArray(), true, true);
            partes.Add(new CombineInstance { mesh = parte, transform = Matrix4x4.identity });
            var mat = new Material(baseMat);
            mat.SetColor("_Color", colores[c]);
            mats.Add(mat);
        }
        var final = new Mesh { name = "VirusProvisional", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        final.CombineMeshes(partes.ToArray(), false, true);
        final.RecalculateBounds();

        var go = new GameObject("VirusProvisional");
        go.AddComponent<MeshFilter>().sharedMesh = final;
        go.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
        return go;
    }
}
