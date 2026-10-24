using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MM = MontarMicrobiologia;
using ML = MontarLaboratorio;

// Utilería realista de los objetos con los que se interactúa en E2 (elegir una muestra):
// portaobjetos de vidrio, bandejas con ranuras, criotubos con tapa de rosca en su gradilla y
// frasco lavador de etanol. Las piezas redondas son mallas de torno (un perfil revolucionado),
// con paredes con grosor, fondos cónicos y estrías, en lugar de cilindros.
// Medidas reales con un ligero aumento donde hace falta para verlas y tocarlas en VR (RNF-04).
public static class Utileria
{
    const string ScenePath = "Assets/NanoLab/Scenes/Microbiologia.unity";
    const string MallaDir = "Assets/NanoLab/Modelos/Mallas/Utileria";
    const string MatDir = "Assets/NanoLab/Materiales/Micro";

    static Dictionary<string, Material> M => MM.mats;

    // ================================================================== materiales

    internal static void Materiales()
    {
        var std = Shader.Find("Standard");
        // Vidrio de borosilicato: casi incoloro, muy liso (refleja las sondas de la sala).
        MM.Transparente("VidrioReal", new Color(0.9f, 0.97f, 0.96f, 0.14f), 0.97f);
        // El canto del vidrio sódico se ve verdoso.
        MM.Transparente("CantoVidrio", new Color(0.55f, 0.82f, 0.72f, 0.6f), 0.95f);
        MM.Mat("Esmerilado", std, new Color(0.96f, 0.96f, 0.95f), 0f, 0.15f);
        MM.Transparente("Polipropileno", new Color(0.94f, 0.95f, 0.93f, 0.45f), 0.55f);
        MM.Transparente("PEBlanco", new Color(0.95f, 0.97f, 1f, 0.5f), 0.6f);
        MM.Transparente("Etanol", new Color(0.86f, 0.94f, 1f, 0.3f), 0.95f);
        MM.Mat("PlasticoBlanco", std, new Color(0.93f, 0.93f, 0.91f), 0f, 0.55f);
        MM.Mat("PlasticoAzul", std, new Color(0.13f, 0.33f, 0.72f), 0f, 0.45f);
        MM.Mat("PlasticoRojo", std, new Color(0.78f, 0.1f, 0.1f), 0f, 0.5f);
        MM.Mat("Hueco", std, new Color(0.03f, 0.05f, 0.1f), 0f, 0.2f);
        MM.Mat("Papel", std, new Color(0.97f, 0.97f, 0.95f), 0f, 0.1f);
    }

    static PhysicMaterial FisicaVidrio()
    {
        string ruta = MatDir + "/VidrioFisico.physicMaterial";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(ruta);
        if (pm == null) { pm = new PhysicMaterial("VidrioFisico"); AssetDatabase.CreateAsset(pm, ruta); }
        pm.dynamicFriction = 0.35f; pm.staticFriction = 0.5f; pm.bounciness = 0.05f;
        pm.frictionCombine = PhysicMaterialCombine.Average; pm.bounceCombine = PhysicMaterialCombine.Minimum;
        EditorUtility.SetDirty(pm);
        return pm;
    }

    // ================================================================== portaobjetos

    // Portaobjetos de 75 × 25 × 1 mm: vidrio, canto verdoso, zona esmerilada para rotular y frotis ovalado.
    internal static (Renderer frotis, Renderer brillo, TextMeshPro etiqueta) Portaobjetos(Transform go)
    {
        MM.Caja(go, "Vidrio", Vector3.zero, new Vector3(0.075f, 0.001f, 0.025f), M["VidrioReal"], false);
        MM.Caja(go, "CantoLargoA", new Vector3(0f, 0f, 0.0122f), new Vector3(0.075f, 0.001f, 0.0006f), M["CantoVidrio"], false);
        MM.Caja(go, "CantoLargoB", new Vector3(0f, 0f, -0.0122f), new Vector3(0.075f, 0.001f, 0.0006f), M["CantoVidrio"], false);
        MM.Caja(go, "CantoCortoA", new Vector3(0.0372f, 0f, 0f), new Vector3(0.0006f, 0.001f, 0.025f), M["CantoVidrio"], false);
        MM.Caja(go, "CantoCortoB", new Vector3(-0.0372f, 0f, 0f), new Vector3(0.0006f, 0.001f, 0.025f), M["CantoVidrio"], false);
        MM.Caja(go, "Esmerilado", new Vector3(0.0275f, 0.00003f, 0f), new Vector3(0.02f, 0.00106f, 0.0244f), M["Esmerilado"], false);
        var frotis = MM.Cilindro(go, "Frotis", new Vector3(-0.009f, 0.00055f, 0f), new Vector3(0.03f, 0.0001f, 0.017f), Quaternion.identity, M["Frotis"]);
        var brillo = MM.Caja(go, "Brillo", Vector3.zero, new Vector3(0.078f, 0.004f, 0.028f), M["Brillo"], false);
        var et = MM.Texto3D(go, "LIMPIO", new Vector3(0.0275f, 0.00065f, 0f), Quaternion.Euler(90f, 90f, 0f), 0.028f, new Color(0.1f, 0.12f, 0.2f), 0.022f);
        et.alignment = TextAlignmentOptions.Center;
        et.fontStyle = FontStyles.Bold;
        et.enableAutoSizing = true; et.fontSizeMin = 0.01f; et.fontSizeMax = 0.028f;
        var bc = go.GetComponent<BoxCollider>();
        if (bc != null) bc.sharedMaterial = FisicaVidrio();
        return (frotis.GetComponent<Renderer>(), brillo.GetComponent<Renderer>(), et);
    }

    // ================================================================== bandeja

    // Bandeja de plástico con una ranura por portaobjetos. 'ranuras' son las z (locales a la bandeja)
    // donde descansan los portaobjetos; el portaobjetos queda a 0,015 m sobre la base de la bandeja.
    internal static GameObject Bandeja(Transform padre, Vector3 pos, float[] ranuras)
    {
        var b = new GameObject("Bandeja").transform;
        b.SetParent(padre, false);
        b.localPosition = new Vector3(pos.x, 0f, pos.z);
        const float ancho = 0.1f, fondo = 0.115f;
        var cuerpo = MM.Caja(b, "Base", new Vector3(0f, 0.006f, 0f), new Vector3(ancho, 0.012f, fondo), M["PlasticoBlanco"], true);
        cuerpo.name = "Base";
        // Reborde perimetral.
        foreach (var s in new[] { -1f, 1f })
        {
            MM.Caja(b, "BordeX", new Vector3(s * (ancho / 2f - 0.002f), 0.0145f, 0f), new Vector3(0.004f, 0.005f, fondo), M["PlasticoBlanco"], false);
            MM.Caja(b, "BordeZ", new Vector3(0f, 0.0145f, s * (fondo / 2f - 0.002f)), new Vector3(ancho, 0.005f, 0.004f), M["PlasticoBlanco"], false);
        }
        // Separadores entre ranuras y una sombra de ranura bajo cada portaobjetos.
        var orden = ranuras.OrderBy(z => z).ToArray();
        for (int i = 0; i + 1 < orden.Length; i++)
            MM.Caja(b, "Separador", new Vector3(0f, 0.0138f, (orden[i] + orden[i + 1]) / 2f), new Vector3(ancho - 0.008f, 0.0035f, 0.0025f), M["PlasticoBlanco"], false);
        foreach (var z in orden)
            MM.Caja(b, "Ranura", new Vector3(0f, 0.01205f, z), new Vector3(0.078f, 0.0002f, 0.027f), M["Hueco"], false);
        return b.gameObject;
    }

    // ================================================================== criotubos

    // Criotubo de polipropileno (Ø 16 mm × 64 mm con tapa; algo mayor que uno real de 2 ml para verlo en VR):
    // fondo cónico, muestra congelada dentro y tapa de rosca con estrías del color de la muestra.
    // Devuelve el renderer del brillo de resaltado.
    internal static Renderer Criotubo(Transform tubo, MuestraData v)
    {
        Limpiar(tubo, "Tubo", "Tapa", "Brillo", "Muestra", "Rosca");
        Torno(tubo, "Tubo", MallaCriotubo(), M["Polipropileno"]);
        Torno(tubo, "Muestra", MallaMuestraCriotubo(), MaterialMuestra(v));
        Torno(tubo, "Tapa", MallaTapaCriotubo(), MaterialTapa(v));
        var brillo = MM.Cilindro(tubo, "Brillo", new Vector3(0f, 0.033f, 0f), new Vector3(0.026f, 0.036f, 0.026f), Quaternion.identity, M["Brillo"]);
        return brillo.GetComponent<Renderer>();
    }

    // Gradilla de criotubos: bloque azul con 3 filas de huecos; los tubos van en la fila central.
    internal static void Gradilla(Transform rack)
    {
        Limpiar(rack, "Rack", "Huecos");
        MM.Caja(rack, "Rack", new Vector3(0f, 0.02f, 0f), new Vector3(0.62f, 0.04f, 0.09f), M["PlasticoAzul"], true);
        var huecos = new GameObject("Huecos").transform;
        huecos.SetParent(rack, false);
        for (int fila = -1; fila <= 1; fila++)
            for (int k = 0; k < 20; k++)
                MM.Cilindro(huecos, "Hueco", new Vector3(-0.285f + k * 0.03f, 0.0402f, fila * 0.026f), new Vector3(0.019f, 0.0002f, 0.019f), Quaternion.identity, M["Hueco"]);
    }

    static Material MaterialTapa(MuestraData v)
    {
        // Plástico de color, sin brillo propio (antes era emisivo y parecía una luz).
        var m = MM.MatEn(MatDir, "Tapa_" + v.name, Shader.Find("Standard"));
        m.color = v.color;
        m.DisableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", Color.black);
        m.SetFloat("_Glossiness", 0.45f);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    static Material MaterialMuestra(MuestraData v)
    {
        var m = MM.MatEn(MatDir, "Muestra_" + v.name, Shader.Find("Standard"));
        m.color = Color.Lerp(v.color, new Color(0.95f, 0.93f, 0.9f), 0.55f);   // congelada: pálida y escarchada
        m.SetFloat("_Glossiness", 0.25f);
        m.SetFloat("_Metallic", 0f);
        return m;
    }

    // ================================================================== gradilla de cultivos del puesto

    // Gradilla con un tubo de cultivo de cada bacteria, junto al microscopio: se puede tomar una
    // muestra nada más llegar al puesto, sin ir al cepario. Los virus siguen en la sala de alto riesgo.
    internal static void GradillaCultivos(Transform puesto, Vector3 pos, MuestraData[] bacterias)
    {
        Limpiar(puesto, "GradillaCultivos");
        var g = new GameObject("GradillaCultivos").transform;
        g.SetParent(puesto, false);
        g.localPosition = new Vector3(pos.x, 0f, pos.z);
        const float paso = 0.065f;
        float ancho = paso * bacterias.Length + 0.035f;
        MM.Caja(g, "Base", new Vector3(0f, 0.0175f, 0f), new Vector3(ancho, 0.035f, 0.05f), M["PlasticoAzul"], true);
        for (int i = 0; i < bacterias.Length; i++)
        {
            var b = bacterias[i];
            float x = (i - (bacterias.Length - 1) / 2f) * paso;
            MM.Cilindro(g, "Hueco", new Vector3(x, 0.0352f, 0f), new Vector3(0.019f, 0.0002f, 0.019f), Quaternion.identity, M["Hueco"]);
            var tubo = new GameObject("Cultivo_" + b.name).transform;
            tubo.SetParent(g, false);
            tubo.localPosition = new Vector3(x, 0.022f, 0f);
            var brillo = Criotubo(tubo, b);
            var bc = tubo.gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = new Vector3(0f, 0.04f, 0f); bc.size = new Vector3(0.055f, 0.09f, 0.055f);
            tubo.gameObject.AddComponent<FuenteMuestra>().muestra = b;
            var res = tubo.gameObject.AddComponent<Resaltado>();
            res.brillos = new[] { brillo };
            res.radio = 0.08f;
            // Nombre impreso en el frente de la gradilla (lado del usuario: +z local del puesto).
            var t = MM.Texto3D(g, b.nombre, new Vector3(x, 0.0175f, 0.0252f), Quaternion.Euler(0f, 180f, 0f), 0.09f, Color.white, paso);
            t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
            t.enableWordWrapping = false;
        }
        var titulo = MM.Texto3D(g, "CULTIVOS", new Vector3(0f, 0.115f, 0f), Quaternion.Euler(0f, 180f, 0f), 0.12f, new Color(0.4f, 0.9f, 1f), ancho);
        titulo.alignment = TextAlignmentOptions.Center; titulo.fontStyle = FontStyles.Bold;
    }

    // Los microscopios se acercan al borde de la mesa (AcercarMicroscopios) y la bandeja y el
    // desinfectante quedaban ~0,5 m al fondo. Se ponen a la altura del microscopio, al alcance de la
    // mano, y se añade la gradilla de cultivos al otro lado del desinfectante.
    internal static void ReubicarPuestos(MuestraData[] bacterias)
    {
        int n = 0;
        foreach (var puesto in Object.FindObjectsOfType<Transform>(true).Where(t => t.name.StartsWith("Puesto_") && t.parent != null && t.parent.name == "Puestos").ToArray())
        {
            var micro = puesto.GetComponentInChildren<MicroscopioOptico>(true);
            if (micro == null || micro.platina == null || micro.platina.anclaje == null) continue;
            float z = puesto.InverseTransformPoint(micro.platina.anclaje.position).z;   // profundidad cómoda
            var bandeja = puesto.Find("Bandeja");
            if (bandeja != null)
            {
                float dz = z - bandeja.localPosition.z;
                bandeja.localPosition += new Vector3(0f, 0f, dz);
                foreach (var p in puesto.GetComponentsInChildren<Portaobjetos>(true))
                    if (p.transform.parent == puesto && Mathf.Abs(p.transform.localPosition.x - bandeja.localPosition.x) < 0.02f)
                        p.transform.localPosition += new Vector3(0f, 0f, dz);
            }
            var desinf = puesto.Find("Desinfectante");
            if (desinf != null) desinf.localPosition = new Vector3(desinf.localPosition.x, desinf.localPosition.y, z);
            if (bacterias != null && bacterias.Length > 0) GradillaCultivos(puesto, new Vector3(0.48f, 0f, z), bacterias);
            n++;
        }
        Debug.Log($"[Utileria] {n} puestos con bandeja, desinfectante y gradilla de cultivos junto al microscopio.");
    }

    // ================================================================== frasco lavador

    // Frasco lavador de 250 ml con etanol al 70 %: cuerpo de polietileno translúcido, líquido, tapa
    // roja, boquilla doblada y etiqueta. Devuelve el renderer del brillo de resaltado.
    internal static Renderer FrascoLavador(Transform v)
    {
        Limpiar(v, "Vaso", "Liquido", "Brillo", "Cuerpo", "Tapa", "Boquilla", "Etiqueta", "TextoEtiqueta");
        // La etiqueta flotante "DESINFECTANTE" (Cartel) sube por encima de la boquilla.
        foreach (var c in v.GetComponentsInChildren<Cartel>(true)) c.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        Torno(v, "Cuerpo", MallaFrasco(), M["PEBlanco"]);
        Torno(v, "Liquido", MallaLiquidoFrasco(), M["Etanol"]);
        Torno(v, "Tapa", MallaTapaFrasco(), M["PlasticoRojo"]);
        var boq = new GameObject("Boquilla").transform;
        boq.SetParent(v, false);
        MM.Cilindro(boq, "Tubo", new Vector3(0f, 0.163f, 0f), new Vector3(0.006f, 0.014f, 0.006f), Quaternion.identity, M["PlasticoBlanco"]);
        var codo = Quaternion.Euler(55f, 0f, 0f);
        MM.Cilindro(boq, "Brazo", new Vector3(0f, 0.183f, 0.016f), new Vector3(0.0045f, 0.022f, 0.0045f), codo, M["PlasticoBlanco"]);
        MM.Cilindro(boq, "Punta", new Vector3(0f, 0.192f, 0.036f), new Vector3(0.0035f, 0.006f, 0.0035f), Quaternion.Euler(150f, 0f, 0f), M["PlasticoBlanco"]);

        // Etiqueta envolvente con el texto por delante y por detrás.
        Torno(v, "Etiqueta", MallaEtiqueta(), M["Papel"]);
        foreach (var yaw in new[] { 0f, 180f })
        {
            var cara = Quaternion.Euler(0f, yaw, 0f);
            var t = MM.Texto3D(v, "<b>ETANOL 70 %</b>\n<size=70%>DESINFECTANTE · INFLAMABLE</size>",
                               cara * new Vector3(0f, 0.058f, -0.0312f), cara, 0.11f, new Color(0.7f, 0.08f, 0.08f), 0.05f);
            t.name = "TextoEtiqueta";
            t.alignment = TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(0.05f, 0.03f);
        }
        var brillo = MM.Cilindro(v, "Brillo", new Vector3(0f, 0.09f, 0f), new Vector3(0.072f, 0.095f, 0.072f), Quaternion.identity, M["Brillo"]);
        foreach (var r in v.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = ShadowCastingMode.Off;
        return brillo.GetComponent<Renderer>();
    }

    // ================================================================== mallas de torno

    static Vector2[] P(params float[] rx) { var p = new Vector2[rx.Length / 2]; for (int i = 0; i < p.Length; i++) p[i] = new Vector2(rx[2 * i], rx[2 * i + 1]); return p; }

    // Perfiles (radio, altura) en metros, recorridos como el contorno de la sección.
    static Mesh MallaCriotubo() => Malla("Criotubo", P(
        0f, 0f, 0.0045f, 0.004f, 0.0078f, 0.012f, 0.008f, 0.014f, 0.008f, 0.046f,
        0.0072f, 0.046f, 0.0072f, 0.0145f, 0.007f, 0.0125f, 0.0038f, 0.0055f, 0f, 0.0018f));

    static Mesh MallaMuestraCriotubo() => Malla("CriotuboMuestra", P(
        0f, 0.0019f, 0.0037f, 0.0057f, 0.0069f, 0.0127f, 0.0069f, 0.026f, 0.006f, 0.0272f, 0f, 0.0274f));

    static Mesh MallaTapaCriotubo()
    {
        var p = new List<Vector2> { new Vector2(0.0071f, 0.044f), new Vector2(0.0094f, 0.044f) };
        // Estrías de agarre.
        for (int i = 0; i < 8; i++)
        {
            float y = 0.0455f + i * 0.0018f;
            p.Add(new Vector2(0.0094f, y)); p.Add(new Vector2(0.0099f, y + 0.0004f));
            p.Add(new Vector2(0.0099f, y + 0.0011f)); p.Add(new Vector2(0.0094f, y + 0.0015f));
        }
        p.Add(new Vector2(0.0094f, 0.0615f)); p.Add(new Vector2(0.0088f, 0.0635f)); p.Add(new Vector2(0f, 0.0638f));
        return Malla("CriotuboTapa", p.ToArray());
    }

    static Mesh MallaFrasco() => Malla("Frasco", P(
        0f, 0.001f, 0.026f, 0f, 0.0295f, 0.003f, 0.03f, 0.008f, 0.03f, 0.1f, 0.027f, 0.112f, 0.019f, 0.121f,
        0.0125f, 0.124f, 0.0125f, 0.134f,
        0.011f, 0.134f, 0.011f, 0.125f, 0.0178f, 0.12f, 0.0258f, 0.111f, 0.0285f, 0.1f, 0.0285f, 0.008f, 0.0275f, 0.0035f, 0f, 0.0025f));

    static Mesh MallaLiquidoFrasco() => Malla("FrascoLiquido", P(
        0f, 0.0026f, 0.0274f, 0.0036f, 0.0283f, 0.008f, 0.0283f, 0.072f, 0f, 0.072f));

    static Mesh MallaTapaFrasco() => Malla("FrascoTapa", P(
        0.0124f, 0.129f, 0.0148f, 0.129f, 0.0152f, 0.131f, 0.0152f, 0.146f, 0.0145f, 0.149f, 0.004f, 0.15f, 0.004f, 0.157f, 0f, 0.157f));

    static Mesh MallaEtiqueta() => Malla("FrascoEtiqueta", P(0.0303f, 0.04f, 0.0303f, 0.078f));

    static GameObject Torno(Transform padre, string nombre, Mesh malla, Material mat)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.AddComponent<MeshFilter>().sharedMesh = malla;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveGI = ReceiveGI.LightProbes;
        return go;
    }

    // Revoluciona el perfil alrededor del eje Y. Cada tramo del perfil tiene su propia normal
    // (aristas vivas entre tramos, suave alrededor). Se guarda como asset para que la escena lo conserve.
    static Mesh Malla(string nombre, Vector2[] perfil, int lados = 28)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float largo = 0f;
        for (int i = 0; i + 1 < perfil.Length; i++) largo += Vector2.Distance(perfil[i], perfil[i + 1]);
        float acum = 0f;
        for (int s = 0; s + 1 < perfil.Length; s++)
        {
            Vector2 a = perfil[s], b = perfil[s + 1];
            var d = b - a;
            float tramo = d.magnitude;
            if (tramo < 1e-6f) continue;
            var nPerfil = new Vector2(d.y, -d.x).normalized;   // hacia fuera del contorno
            int baseIdx = v.Count;
            for (int k = 0; k <= lados; k++)
            {
                float ang = k / (float)lados * Mathf.PI * 2f;
                float c = Mathf.Cos(ang), sn = Mathf.Sin(ang);
                var nn = new Vector3(nPerfil.x * c, nPerfil.y, nPerfil.x * sn).normalized;
                v.Add(new Vector3(a.x * c, a.y, a.x * sn)); n.Add(nn); uv.Add(new Vector2(k / (float)lados, acum / largo));
                v.Add(new Vector3(b.x * c, b.y, b.x * sn)); n.Add(nn); uv.Add(new Vector2(k / (float)lados, (acum + tramo) / largo));
            }
            for (int k = 0; k < lados; k++)
            {
                int a0 = baseIdx + k * 2, b0 = a0 + 1, a1 = a0 + 2, b1 = a0 + 3;
                Triangulo(v, n, tri, a0, b0, b1);
                Triangulo(v, n, tri, a0, b1, a1);
            }
            acum += tramo;
        }
        var m = new Mesh { name = nombre };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateBounds();
        m.RecalculateTangents();
        return Guardar(m);
    }

    // Añade el triángulo con el orden que hace visible su cara del lado de la normal (Unity: horario).
    static void Triangulo(List<Vector3> v, List<Vector3> n, List<int> tri, int i0, int i1, int i2)
    {
        var c = Vector3.Cross(v[i1] - v[i0], v[i2] - v[i0]);
        if (c.sqrMagnitude < 1e-16f) return;   // degenerado (en el eje)
        if (Vector3.Dot(c, n[i0] + n[i1] + n[i2]) < 0f) { var t = i1; i1 = i2; i2 = t; }
        tri.Add(i0); tri.Add(i1); tri.Add(i2);
    }

    static Mesh Guardar(Mesh m)
    {
        Directory.CreateDirectory(MallaDir);
        string ruta = $"{MallaDir}/{m.name}.asset";
        var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
        if (existente == null) { AssetDatabase.CreateAsset(m, ruta); return m; }
        EditorUtility.CopySerialized(m, existente);   // conserva las referencias de la escena
        Object.DestroyImmediate(m);
        return existente;
    }

    static void Limpiar(Transform t, params string[] nombres)
    {
        foreach (Transform h in t.Cast<Transform>().ToArray())
            if (nombres.Contains(h.name)) Object.DestroyImmediate(h.gameObject);
    }

    // ================================================================== actualizar la escena

    // Rehace en la escena actual la utilería de E2 sin regenerar la sala ni hornear la luz.
    [MenuItem("NanoLab/Actualizar objetos interactivos (sin hornear)", false, 6)]
    public static void Actualizar()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MM.CrearMateriales();
        MM.CrearPrefabPortaobjetos();
        int frascos = 0, tubos = 0, bandejas = 0;

        foreach (var f in Object.FindObjectsOfType<FuenteMuestra>(true))
        {
            if (f.esDesinfectante && f.GetComponent<Resaltado>() != null)
            {
                f.GetComponent<Resaltado>().brillos = new[] { FrascoLavador(f.transform) };
                AjustarTriggerFrasco(f.gameObject);
                frascos++;
            }
            else if (f.muestra != null && f.name.StartsWith("Criotubo_"))
            {
                f.transform.localPosition = new Vector3(f.transform.localPosition.x, 0.026f, f.transform.localPosition.z);
                f.GetComponent<Resaltado>().brillos = new[] { Criotubo(f.transform, f.muestra) };
                tubos++;
            }
        }
        foreach (var rack in Object.FindObjectsOfType<Transform>(true).Where(t => t.name == "RackCriotubos").ToArray()) Gradilla(rack);

        // Bandejas de los puestos, del cepario y de la sala de alto riesgo (las ranuras salen de los portaobjetos).
        foreach (var vieja in Object.FindObjectsOfType<Transform>(true).Where(t => t.name == "Bandeja" && t.parent != null &&
                     (t.parent.name.StartsWith("Puesto_") || t.parent.name == "CeparioBacterias" || t.parent.name == "RackCriotubos")).ToArray())
        {
            var padre = vieja.parent;
            var pos = vieja.localPosition;
            var ranuras = padre.GetComponentsInChildren<Portaobjetos>(true)
                .Where(p => p.transform.parent == padre && Mathf.Abs(p.transform.localPosition.x - pos.x) < 0.02f)
                .Select(p => p.transform.localPosition.z - pos.z).ToArray();
            Object.DestroyImmediate(vieja.gameObject);
            if (ranuras.Length > 0) { Bandeja(padre, pos, ranuras); bandejas++; }
        }

        Physics.SyncTransforms();
        ReubicarPuestos(CrearMuestras.Todas().Where(m => m.tipo == TipoMuestra.Bacteria).ToArray());

        ConfigurarMetaISDK.ActualizarPrefabPortaobjetos();
        ConfigurarMetaISDK.Configurar(scene);
        AgruparLab.Agrupar(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Utileria] Actualizados: {frascos} frascos lavadores, {tubos} criotubos, {bandejas} bandejas y el prefab Portaobjetos.");
        Capturas();
    }

    internal static void AjustarTriggerFrasco(GameObject v)
    {
        var bc = v.GetComponent<BoxCollider>();
        if (bc == null) return;
        bc.isTrigger = true; bc.center = new Vector3(0f, 0.09f, 0f); bc.size = new Vector3(0.09f, 0.18f, 0.09f);
    }

    // Logs/Capturas/utileria_*.png: primeros planos del puesto, la gradilla y el cepario.
    [MenuItem("NanoLab/Capturas de los objetos interactivos", false, 7)]
    public static void Capturas()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Capturas");
        Directory.CreateDirectory(dir);
        void Plano(string nombre, Transform objetivo, Vector3 desde)
        {
            if (objetivo == null) return;
            var ojo = objetivo.position + desde;
            ML.Capturar(dir, nombre, ojo, objetivo.position - ojo);
        }
        var puesto = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Puesto_01");
        var bandeja = puesto != null ? puesto.Find("Bandeja") : null;
        if (puesto != null)
        {
            Plano("utileria_bandeja", bandeja, Vector3.up * 0.22f + puesto.right * -0.12f + puesto.forward * 0.2f);
            Plano("utileria_frasco", puesto.Find("Desinfectante"), Vector3.up * 0.18f + puesto.forward * 0.3f + puesto.right * 0.1f);
        }
        var rack = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "RackCriotubos");
        if (rack != null) Plano("utileria_criotubos", rack, Vector3.up * 0.22f - rack.forward * 0.45f);
        Debug.Log("[Utileria] Capturas guardadas en Logs/Capturas (utileria_*.png).");
    }
}
