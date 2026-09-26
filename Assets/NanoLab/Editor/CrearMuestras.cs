using System.IO;
using UnityEditor;
using UnityEngine;

// Crea (o actualiza) las fichas de muestra en Assets/NanoLab/Muestras.
public static class CrearMuestras
{
    const string Dir = "Assets/NanoLab/Muestras";
    const string ImgDir = Dir + "/Imagenes";
    const string ModelosDir = "Assets/NanoLab/Modelos/Virus";

    static MuestraData.Dato D(string c, string v) => new MuestraData.Dato { clave = c, valor = v };

    public static MuestraData[] Todas()
    {
        return new[]
        {
            Muestra("Influenza", "#V-01", "Influenza A", "Influenzavirus A (H5N1)", TipoMuestra.Virus, "influenza", "Influenza",
                new Color(0.95f, 0.45f, 0.35f), "ALTO CONTAGIO",
                new[] { D("Tipo", "Virus de ARN"), D("Familia", "Orthomyxoviridae"), D("Diámetro", "80 - 120 nm"), D("Forma", "Esférica / pleomórfica"), D("Envoltura", "Sí, lipídica"), D("Genoma", "ARN (-) segmentado, 8") },
                "Partícula esférica cubierta de espículas cortas y muy densas. Algunas cepas forman filamentos largos.",
                "Envoltura lipídica con dos glicoproteínas: hemaglutinina (HA) y neuraminidasa (NA). Dentro, 8 segmentos de ARN con su nucleoproteína.",
                "Hemaglutinina (espículas en bastón), neuraminidasa (espículas en hongo), proteína M1 bajo la envoltura y canal M2.",
                "La HA se une al ácido siálico de las células respiratorias; la NA libera los nuevos virus. Cambia cada temporada (deriva antigénica).",
                "Espículas HA/NA claramente visibles.\nEnvoltura íntegra.\nCompatible con cepa aviar H5N1."),

            Muestra("VIH", "#V-02", "VIH-1", "Virus de la inmunodeficiencia humana", TipoMuestra.Virus, "vih", "HIV",
                new Color(0.95f, 0.75f, 0.3f), "ALTO RIESGO",
                new[] { D("Tipo", "Retrovirus (ARN)"), D("Familia", "Retroviridae"), D("Diámetro", "~120 nm"), D("Forma", "Esférica, cápside cónica"), D("Envoltura", "Sí, lipídica"), D("Genoma", "2 copias de ARN (+)") },
                "Virión esférico con pocas espículas (glicoproteína Env) y una cápside interna con forma de cono.",
                "Envoltura con gp120/gp41, matriz p17, cápside cónica p24 que contiene el ARN, la transcriptasa inversa y la integrasa.",
                "gp120 (unión), gp41 (fusión), p24 (cápside), transcriptasa inversa, integrasa y proteasa.",
                "Infecta linfocitos T CD4+. Copia su ARN a ADN y lo integra en el genoma de la célula. Tratamiento: antirretrovirales.",
                "Cápside cónica característica.\nEspículas Env escasas.\nMorfología de virión maduro."),

            Muestra("SARSCoV2", "#V-03", "SARS-CoV-2", "Coronavirus del síndrome respiratorio agudo grave 2", TipoMuestra.Virus, "sarscov2", "SARS",
                new Color(0.95f, 0.3f, 0.4f), "ALTO CONTAGIO",
                new[] { D("Tipo", "Virus de ARN"), D("Familia", "Coronaviridae"), D("Diámetro", "80 - 120 nm"), D("Forma", "Esférica con espículas"), D("Envoltura", "Sí, lipídica"), D("Genoma", "ARN (+) monocatenario") },
                "Partícula esférica rodeada de espículas en forma de maza que le dan aspecto de corona.",
                "Envoltura lipídica con proteínas S (espícula), M (membrana) y E (envoltura). Dentro, el ARN unido a la nucleocápside N.",
                "Proteína S (espícula), proteína M, proteína E, nucleoproteína N y ARN viral de ~30 000 bases.",
                "La proteína S se une al receptor ACE2 de las células del tracto respiratorio. Sensible a temperatura y desinfectantes.",
                "Partícula esférica con espículas superficiales.\nAlta similitud con la familia Coronaviridae.\nManipular en cabina de bioseguridad."),

            Muestra("VPH", "#V-04", "VPH", "Virus del papiloma humano", TipoMuestra.Virus, "vph", "Papiloma",
                new Color(0.4f, 0.8f, 0.95f), "MODERADO",
                new[] { D("Tipo", "Virus de ADN"), D("Familia", "Papillomaviridae"), D("Diámetro", "50 - 55 nm"), D("Forma", "Icosaédrica"), D("Envoltura", "No (desnudo)"), D("Genoma", "ADN circular bicatenario") },
                "Cápside icosaédrica sin envoltura, formada por 72 capsómeros en forma de estrella.",
                "La proteína L1 forma los 72 pentámeros de la cápside; L2 está en el interior junto al ADN y las histonas de la célula.",
                "Proteínas L1 y L2 de la cápside y ADN circular de ~8 000 pares de bases.",
                "Infecta células de piel y mucosas. Algunos tipos (16, 18) se asocian a cáncer. Existe vacuna.",
                "Cápside icosaédrica bien definida.\nSin envoltura lipídica.\nCapsómeros visibles."),

            Muestra("HepatitisB", "#V-05", "Hepatitis B", "Virus de la hepatitis B (VHB)", TipoMuestra.Virus, "hepatitisb", "Hepatitis",
                new Color(0.6f, 0.95f, 0.45f), "ALTO RIESGO",
                new[] { D("Tipo", "Virus de ADN"), D("Familia", "Hepadnaviridae"), D("Diámetro", "~42 nm"), D("Forma", "Esférica (partícula de Dane)"), D("Envoltura", "Sí, lipídica"), D("Genoma", "ADN parcialmente bicatenario") },
                "Partícula esférica pequeña (partícula de Dane) con una nucleocápside icosaédrica en su interior.",
                "Envoltura con el antígeno de superficie HBsAg; dentro, la nucleocápside de proteína HBc con el ADN y la polimerasa.",
                "HBsAg (superficie), HBcAg (núcleo), HBeAg, polimerasa viral y ADN circular de ~3 200 pares de bases.",
                "Infecta los hepatocitos del hígado. Se transmite por sangre y fluidos. Prevención con vacuna.",
                "Partículas de Dane esféricas.\nNucleocápside icosaédrica.\nAbundantes partículas de HBsAg."),

            Muestra("Ecoli", "#B-01", "E. coli", "Escherichia coli", TipoMuestra.Bacteria, "ecoli", null,
                new Color(0.9f, 0.35f, 0.55f), "BSL-2",
                new[] { D("Tipo", "Bacteria"), D("Gram", "Negativa (rosa)"), D("Tamaño", "1 - 2 µm"), D("Forma", "Bacilo (bastón)"), D("Movilidad", "Flagelos peritricos"), D("Oxígeno", "Anaerobia facultativa") },
                "Bacilos cortos, aislados o en parejas, teñidos de rosa-rojo por la safranina.",
                "Pared Gram negativa: capa fina de peptidoglucano y membrana externa con lipopolisacárido.",
                "Peptidoglucano, lipopolisacárido (LPS), flagelina, fimbrias y un cromosoma circular.",
                "Habitante normal del intestino. Algunas cepas causan diarrea o infecciones urinarias.",
                "Bacilos Gram negativos.\nDistribución uniforme.\nTinción correcta."),

            Muestra("Saureus", "#B-02", "S. aureus", "Staphylococcus aureus", TipoMuestra.Bacteria, "saureus", null,
                new Color(0.55f, 0.35f, 0.95f), "BSL-2",
                new[] { D("Tipo", "Bacteria"), D("Gram", "Positiva (violeta)"), D("Tamaño", "~1 µm"), D("Forma", "Coco en racimos"), D("Movilidad", "Inmóvil"), D("Oxígeno", "Anaerobia facultativa") },
                "Cocos agrupados en racimos irregulares, como uvas, teñidos de violeta.",
                "Pared Gram positiva: capa gruesa de peptidoglucano con ácidos teicoicos.",
                "Peptidoglucano, ácidos teicoicos, proteína A, coagulasa y toxinas.",
                "Coloniza piel y fosas nasales. Causa infecciones de piel y, en cepas resistentes (MRSA), infecciones graves.",
                "Cocos Gram positivos en racimo.\nCompatible con Staphylococcus.\nRealizar prueba de coagulasa."),

            Muestra("Bsubtilis", "#B-03", "B. subtilis", "Bacillus subtilis", TipoMuestra.Bacteria, "bsubtilis", null,
                new Color(0.35f, 0.55f, 0.95f), "BSL-1",
                new[] { D("Tipo", "Bacteria"), D("Gram", "Positiva (violeta)"), D("Tamaño", "2 - 3 µm"), D("Forma", "Bacilo en cadenas"), D("Movilidad", "Flagelos"), D("Oxígeno", "Aerobia") },
                "Bacilos largos, a menudo en cadenas, teñidos de violeta. Pueden verse esporas claras.",
                "Pared Gram positiva gruesa. Forma endosporas muy resistentes al calor y a la sequía.",
                "Peptidoglucano, ácidos teicoicos, endosporas con dipicolinato de calcio.",
                "Vive en el suelo. No es patógena; se usa en biotecnología para producir enzimas.",
                "Bacilos Gram positivos en cadena.\nPresencia de esporas.\nMuestra no patógena."),
        };
    }

    static MuestraData Muestra(string archivo, string codigo, string nombre, string cientifico, TipoMuestra tipo,
                               string imagen, string modeloClave, Color color, string riesgo, MuestraData.Dato[] datos,
                               string morf, string estr, string comp, string compor, string obs)
    {
        Directory.CreateDirectory(Dir);
        string path = $"{Dir}/{archivo}.asset";
        var m = AssetDatabase.LoadAssetAtPath<MuestraData>(path);
        bool nueva = m == null;
        if (nueva) m = ScriptableObject.CreateInstance<MuestraData>();

        m.codigo = codigo; m.nombre = nombre; m.nombreCientifico = cientifico; m.tipo = tipo;
        m.color = color; m.riesgo = riesgo; m.caracteristicas = datos;
        m.morfologia = morf; m.estructura = estr; m.composicion = comp; m.comportamiento = compor; m.observaciones = obs;
        m.micrografia = Imagen(imagen);
        m.creditoImagen = Credito(imagen);
        if (modeloClave != null) m.modelo3D = BuscarModelo(modeloClave);

        if (nueva) AssetDatabase.CreateAsset(m, path);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Texture2D Imagen(string clave)
    {
        foreach (var ext in new[] { ".jpg", ".png" })
        {
            string p = $"{ImgDir}/{clave}{ext}";
            if (AssetImporter.GetAtPath(p) is TextureImporter ti)
            {
                if (ti.maxTextureSize != 1024 || ti.mipmapEnabled == false)
                {
                    ti.maxTextureSize = 1024;
                    ti.mipmapEnabled = true;
                    ti.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
        }
        Debug.LogWarning($"[Muestras] Falta la imagen {clave}");
        return null;
    }

    static string Credito(string clave)
    {
        switch (clave)
        {
            case "influenza": return "CDC / C. Goldsmith, J. Katz, S. Zaki · Dominio público";
            case "vih": return "CDC / Dr. Edwin P. Ewing, Jr. · Dominio público";
            case "sarscov2": return "NIAID · CC BY 2.0";
            case "vph": return "NIAID · Dominio público";
            case "hepatitisb": return "CDC / Dr. Erskine Palmer · Dominio público";
            case "ecoli": return "Owashxd (Wikimedia) · CC0";
            case "saureus": return "Dr Graham Beards (Wikimedia) · CC BY-SA 4.0";
            case "bsubtilis": return "CDC / Dr. W.A. Clark · Dominio público";
        }
        return "";
    }

    // Prefab convertido del modelo del NIH (Assets/NanoLab/Modelos/Virus/<clave>*.prefab), si ya existe.
    static GameObject BuscarModelo(string clave)
    {
        if (!AssetDatabase.IsValidFolder(ModelosDir)) return null;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ModelosDir }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).ToLowerInvariant().Contains(clave.ToLowerInvariant()))
                return AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        return null;
    }

    [MenuItem("NanoLab/Crear fichas de muestras")]
    public static void Crear()
    {
        var todas = Todas();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Muestras] {todas.Length} fichas creadas/actualizadas.");
    }
}
