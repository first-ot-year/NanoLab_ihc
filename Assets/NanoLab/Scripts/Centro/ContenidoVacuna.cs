// Contenido educativo del CASO 01 · "Crea la vacuna" (único reto del Centro de Experimentación).
// Todo el texto del reto está aquí para poder revisarlo o traducirlo sin tocar la lógica (RetoVacuna)
// ni el montaje (MontajeVacuna). Modelo educativo simplificado: no es un protocolo real de fabricación.
public static class ContenidoVacuna
{
    public const string Codigo = "CASO 01";
    public const string Titulo = "Crea la vacuna";

    public const string Historia =
        "Un coronavirus respiratorio se extiende por la ciudad (modelo educativo basado en el SARS-CoV-2). " +
        "NanoLab te encarga <b>crear una vacuna de ARN mensajero</b>: elegir el antígeno, diseñar el ARNm, fabricarlo, " +
        "envolverlo en nanopartículas de lípidos, controlar su calidad y comprobar en el laboratorio que genera anticuerpos " +
        "que neutralizan al virus. Al final verás cómo se prueba en personas.";

    public const string Aviso =
        "Modelo educativo simplificado: cantidades, tiempos y resultados son ilustrativos y no representan un protocolo real de fabricación ni de ensayo.";

    // Pasos del reto (barra de progreso). El índice + 1 coincide con EtapaVacuna.
    public static readonly string[] Pasos = { "ANTÍGENO", "DISEÑO", "SÍNTESIS", "ENCAPSULAR", "CALIDAD", "PRECLÍNICA", "TÍTULO", "FASES", "EFICACIA" };

    // Zona del mostrador donde se hace cada paso (se muestra en el panel y en el suelo).
    public static readonly string[] Donde =
    {
        "Pantalla derecha", "Mesa de DISEÑO (izquierda)", "Mesa de SÍNTESIS", "MEZCLADOR de nanopartículas", "Mesa de CALIDAD Y FRÍO (derecha)",
        "Placa de cultivo (izquierda)", "Pantalla derecha", "Botones A-E", "Botones A-D",
    };

    public class Opcion
    {
        public string corto;       // texto del cartelito junto al botón o del rótulo
        public string texto;       // texto completo en el panel
        public bool correcta;
        public string respuesta;   // por qué sí / por qué no
        public Opcion(string corto, string texto, bool correcta, string respuesta)
        { this.corto = corto; this.texto = texto; this.correcta = correcta; this.respuesta = respuesta; }
    }

    public class Pregunta
    {
        public string titulo, enunciado, concepto, visual;
        public Opcion[] opciones;
        public string[] pistas;
    }

    public class PasoSecuencia
    {
        public string boton;       // nombre de la acción
        public string corto;       // cartelito
        public string hecho;       // qué ocurre al hacerlo bien
        public PasoSecuencia(string boton, string corto, string hecho) { this.boton = boton; this.corto = corto; this.hecho = hecho; }
    }

    // Pieza que se agarra (bloque de la secuencia, reactivo, cartucho de lípido) o recipiente.
    public class Pieza
    {
        public string id, nombre, sub, explica;
        public bool correcta;
        public UnityEngine.Color color;
        public Pieza(string id, string nombre, string sub, UnityEngine.Color color, bool correcta, string explica)
        { this.id = id; this.nombre = nombre; this.sub = sub; this.color = color; this.correcta = correcta; this.explica = explica; }
    }

    // ------------------------------------------------------------------ 1 · antígeno (apuntar a la parte del virus)

    public static readonly Pregunta Antigeno = new Pregunta
    {
        titulo = "ELIGE EL ANTÍGENO",
        enunciado = "Una vacuna enseña al sistema inmune a reconocer una pieza del virus (el <b>antígeno</b>) sin causar la enfermedad. " +
                    "Los anticuerpos útiles son los que <b>neutralizan</b>: impiden que el virus entre en nuestras células.\n" +
                    "<color=#FFD25A>Apunta con el rayo de la mano vacía al rótulo de una parte del virus (pantalla derecha) y aprieta el gatillo.</color>",
        visual = "Apunta a un rótulo: la parte se ilumina en el modelo.",
        opciones = new[]
        {
            new Opcion("ARN (genoma)", "El ARN, el material genético del virus.", false,
                "El ARN viaja DENTRO del virus, protegido por la envoltura: los anticuerpos no pueden alcanzarlo en un virus intacto. Además, el sistema inmune aprende a reconocer proteínas, no las instrucciones genéticas del virus."),
            new Opcion("Espiga S", "La proteína espiga (S) de la superficie.", true,
                "La espiga (S) está en el EXTERIOR y es la llave que el virus usa para unirse al receptor ACE2 de nuestras células. Los anticuerpos que se pegan a la espiga tapan esa llave: el virus ya no puede entrar. Por eso las vacunas de ARNm contra la COVID-19 llevan las instrucciones de la espiga."),
            new Opcion("Envoltura lipídica", "La envoltura lipídica que rodea al virus.", false,
                "La envoltura es un trozo de membrana que el virus 'roba' a la célula que infectó: está hecha de los mismos lípidos que nuestras células. El sistema inmune no la ve como extraña, y si la atacara atacaría también nuestras membranas."),
            new Opcion("Proteína N", "La proteína N de la nucleocápside, que envuelve el ARN.", false,
                "La proteína N sí es extraña y genera anticuerpos, pero está DENTRO del virus: esos anticuerpos no impiden que el virus entre en la célula (no neutralizan). Se usa en pruebas de diagnóstico, no como antígeno de la vacuna."),
        },
        pistas = new[]
        {
            "Para impedir la infección, el anticuerpo tiene que bloquear algo que el virus usa para ENTRAR en la célula. ¿Qué parte toca primero a nuestras células?",
            "Descarta lo que está DENTRO del virus (no se alcanza) y lo que el virus copia de nuestras propias células (no parece extraño). Queda la ESPIGA S.",
        },
        concepto = "ANTÍGENO: molécula que el sistema inmune reconoce como extraña. Un buen antígeno vacunal está expuesto en la superficie y es esencial para el virus.",
    };

    // ------------------------------------------------------------------ 2 · diseño del ARNm (ordenar bloques en el molde)

    public const string DisenoTitulo = "DISEÑA EL ARN MENSAJERO";
    public const string DisenoEnunciado =
        "Tu vacuna no lleva la espiga: lleva las <b>instrucciones</b> para que nuestras propias células la fabriquen. Esas instrucciones son un <b>ARN mensajero</b>, " +
        "y un ARNm útil tiene 5 partes en un orden fijo, de 5' a 3' (de izquierda a derecha).\n" +
        "<color=#FFD25A>Agarra los bloques de la bandeja (grip) y suéltalos en los 5 huecos del MOLDE, en orden. Sobra un bloque.</color>";

    public static readonly Pieza[] Bloques =
    {
        new Pieza("cap", "CAP 5'", "caperuza", new UnityEngine.Color(0.95f, 0.85f, 0.3f), true,
            "CAPERUZA (cap): protege el inicio del ARNm y le dice al ribosoma 'empieza a leer aquí'."),
        new Pieza("utr5", "UTR 5'", "región inicial", new UnityEngine.Color(0.45f, 0.75f, 1f), true,
            "UTR 5': región que no se traduce; ayuda al ribosoma a colocarse y regula cuánta proteína se fabrica."),
        new Pieza("genS", "GEN S-2P", "espiga estabilizada", new UnityEngine.Color(0.95f, 0.3f, 0.32f), true,
            "GEN DE LA ESPIGA S-2P: las instrucciones de la espiga con dos cambios ('2P') que la mantienen en la forma que tiene ANTES de unirse a la célula. Así los anticuerpos aprenden la forma que importa."),
        new Pieza("utr3", "UTR 3'", "región final", new UnityEngine.Color(0.55f, 0.55f, 1f), true,
            "UTR 3': región final que no se traduce; da estabilidad al ARNm para que dure más en la célula."),
        new Pieza("polyA", "COLA POLI(A)", "AAAA…", new UnityEngine.Color(0.4f, 0.95f, 0.6f), true,
            "COLA POLI(A): unas 100 adeninas seguidas (AAAA…) que protegen el final del ARNm."),
        new Pieza("genN", "GEN N", "proteína N", new UnityEngine.Color(1f, 0.6f, 0.2f), false,
            "GEN DE LA PROTEÍNA N: no es tu antígeno. La proteína N está DENTRO del virus y sus anticuerpos no neutralizan. Quítalo y usa el gen de la espiga."),
    };

    public static readonly string[] OrdenDiseno = { "cap", "utr5", "genS", "utr3", "polyA" };

    public static string ErrorDiseno(string[] colocado, out int hueco)
    {
        hueco = System.Array.IndexOf(colocado, "genN");
        if (hueco >= 0) return Bloques[5].explica;
        for (int i = 0; i < OrdenDiseno.Length; i++)
        {
            if (colocado[i] == OrdenDiseno[i]) continue;
            hueco = i;
            switch (colocado[i])
            {
                case "cap": return "La CAPERUZA va en el EXTREMO 5' (hueco 1): es la señal de 'empieza aquí' y protege el principio del ARNm.";
                case "polyA": return "La COLA POLI(A) va en el EXTREMO 3' (hueco 5): protege el final del ARNm.";
                case "genS": return "El GEN de la espiga va EN MEDIO, entre las dos regiones UTR: el ribosoma necesita colocarse antes (UTR 5') y el ARNm necesita estabilidad después (UTR 3').";
                case "utr5": return "La UTR 5' va justo DESPUÉS de la caperuza y ANTES del gen: es donde se coloca el ribosoma para empezar a leer.";
                case "utr3": return "La UTR 3' va DESPUÉS del gen y ANTES de la cola poli(A): estabiliza el final del mensaje.";
            }
        }
        hueco = -1;
        return "";
    }

    public static readonly string[] PistasDiseno =
    {
        "El ribosoma lee de izquierda (5') a derecha (3'). Los dos extremos llevan las protecciones; el gen del antígeno va en medio, rodeado por las dos regiones UTR.",
        "Orden: CAP 5'  >  UTR 5'  >  GEN S-2P  >  UTR 3'  >  COLA POLI(A). El GEN N sobra.",
    };

    public const string DisenoHecho =
        "<color=#39FF88><b>DISEÑO CORRECTO.</b></color> Tu ARNm dice: 'empieza aquí > colócate > fabrica la espiga > mantente estable > protege el final'. " +
        "Este diseño se convierte en un <b>molde de ADN</b> (fabricado en bacterias) que usarás en la síntesis.";

    // ------------------------------------------------------------------ 3 · síntesis del ARNm (pipeta + termobloque)

    public const string SintesisTitulo = "FABRICA EL ARNm (TRANSCRIPCIÓN IN VITRO)";
    public const string SintesisEnunciado =
        "Ahora hay que fabricar millones de copias de tu ARNm en un tubo, sin células. Para copiar un ADN en ARN hacen falta exactamente <b>3 ingredientes</b>.\n" +
        "<color=#FFD25A>Agarra la PIPETA. Acerca su punta a un frasco y aprieta el gatillo (o A/X) para cargar; acércala al TUBO DE REACCIÓN y aprieta otra vez para descargar. " +
        "Pon el tubo en el TERMOBLOQUE y pulsa INCUBAR.</color>";

    public static readonly Pieza[] ReactivosSintesis =
    {
        new Pieza("molde", "MOLDE ADN", "tu diseño", new UnityEngine.Color(0.45f, 0.75f, 1f), true,
            "MOLDE DE ADN: tu diseño del paso 2, fabricado en bacterias y cortado justo después de la cola poli(A). La enzima lo usa como guía."),
        new Pieza("t7", "T7 ARN POL", "enzima", new UnityEngine.Color(0.75f, 0.45f, 1f), true,
            "ARN POLIMERASA T7: la enzima que lee el ADN y va uniendo nucleótidos para fabricar ARN."),
        new Pieza("ntp", "NUCLEÓTIDOS", "A G C Ψ + cap", new UnityEngine.Color(1f, 0.85f, 0.35f), true,
            "NUCLEÓTIDOS: los ladrillos del ARN (A, G, C y una U modificada, la N1-metilpseudouridina, más el análogo de caperuza). La U modificada evita que la célula confunda el ARNm con un virus y lo destruya."),
        new Pieza("adnpol", "ADN POL", "enzima", new UnityEngine.Color(0.55f, 0.85f, 0.85f), false,
            "La ADN POLIMERASA copia ADN en más ADN (es la enzima de la PCR). Tú necesitas fabricar ARN."),
        new Pieza("ribo", "RIBOSOMAS", "orgánulos", new UnityEngine.Color(0.85f, 0.6f, 0.5f), false,
            "Los RIBOSOMAS leen el ARNm para fabricar PROTEÍNAS. Eso lo harán las células de la persona vacunada: en el tubo solo queremos fabricar el ARNm."),
    };

    public static string FaltaSintesis(string id)
    {
        switch (id)
        {
            case "molde": return "Falta el MOLDE: sin ADN, la enzima no sabe qué copiar.";
            case "t7": return "Falta la ENZIMA que fabrica ARN copiando un ADN.";
            default: return "Faltan los LADRILLOS del ARN (los nucleótidos).";
        }
    }

    public static readonly string[] PistasSintesis =
    {
        "Para copiar ADN en ARN se necesita: lo que se copia (el molde), quien copia (una enzima que fabrique ARN) y con qué se copia (los ladrillos del ARN).",
        "MOLDE ADN + T7 ARN POL + NUCLEÓTIDOS en el tubo. Después, el tubo en el termobloque y pulsa INCUBAR.",
    };

    public const string SintesisHecho =
        "<color=#39FF88><b>¡ARNm FABRICADO!</b></color> En 2 h a 37 °C la T7 hizo millones de copias. Después, una enzima (DNasa) elimina el molde de ADN y el ARNm se purifica.\n" +
        "<color=#8FD8FF>Pero el ARNm desnudo se destruye en segundos en el cuerpo y no atraviesa la membrana de las células: hay que protegerlo.</color>";

    // ------------------------------------------------------------------ 4 · nanopartículas lipídicas (mezclador)

    public const string EncapsuladoTitulo = "ENVUELVE EL ARNm EN NANOPARTÍCULAS";
    public const string EncapsuladoEnunciado =
        "Para que el ARNm llegue intacto al interior de las células se envuelve en <b>nanopartículas lipídicas (LNP)</b>: burbujas de grasa de unos 80 nm, parecidas a una membrana.\n" +
        "<color=#FFD25A>Coloca en el MEZCLADOR los 4 cartuchos de lípidos correctos (sobra uno) y el TUBO con tu ARNm en la entrada de fase acuosa. Luego pulsa MEZCLAR.</color>";

    public static readonly Pieza[] Lipidos =
    {
        new Pieza("ionizable", "LÍPIDO IONIZABLE", "carga +", new UnityEngine.Color(0.3f, 0.85f, 1f), true,
            "LÍPIDO IONIZABLE: en el tampón ácido tiene carga positiva y atrapa al ARNm (carga negativa). Dentro de la célula ayuda a liberarlo."),
        new Pieza("fosfo", "FOSFOLÍPIDO", "DSPC", new UnityEngine.Color(0.95f, 0.95f, 0.95f), true,
            "FOSFOLÍPIDO (DSPC): forma la pared de la nanopartícula, como en nuestras membranas."),
        new Pieza("colesterol", "COLESTEROL", "rigidez", new UnityEngine.Color(1f, 0.85f, 0.35f), true,
            "COLESTEROL: rellena huecos entre los lípidos y da estabilidad a la partícula."),
        new Pieza("peg", "LÍPIDO-PEG", "capa externa", new UnityEngine.Color(0.55f, 1f, 0.6f), true,
            "LÍPIDO-PEG: capa exterior que evita que las partículas se peguen entre sí y controla su tamaño."),
        new Pieza("detergente", "DETERGENTE", "jabón", new UnityEngine.Color(1f, 0.45f, 0.75f), false,
            "Los DETERGENTES deshacen las membranas de lípidos (por eso el jabón destruye la envoltura del virus). Destruiría tus nanopartículas: no va en el mezclador."),
    };

    public static readonly string[] PistasEncapsulado =
    {
        "La nanopartícula es una burbuja de lípidos parecida a una membrana. ¿Qué cartucho DESTRUYE las membranas?",
        "Van los 4 lípidos: IONIZABLE, FOSFOLÍPIDO, COLESTEROL y PEG. El DETERGENTE no. El tubo con el ARNm va en la entrada de fase acuosa.",
    };

    public const string EncapsuladoHecho =
        "<color=#39FF88><b>¡VACUNA FORMULADA!</b></color> En el chip del mezclador, el ARNm (en agua ácida) y los lípidos (en etanol) se encuentran y se autoensamblan en nanopartículas. " +
        "Luego se quita el etanol y se ajusta el pH. <color=#8FD8FF>Lleva el VIAL al control de calidad.</color>";

    // ------------------------------------------------------------------ 5 · control de calidad y cadena de frío

    public const string CalidadTitulo = "CONTROL DE CALIDAD Y CADENA DE FRÍO";
    public const string CalidadEnunciado =
        "Ningún lote sale de la fábrica sin analizarse. Después hay que guardarlo donde el ARNm no se estropee.\n" +
        "<color=#FFD25A>Pon el VIAL en el ANALIZADOR. Cuando el lote esté aprobado, guárdalo en el sitio correcto: ULTRACONGELADOR, NEVERA o MESA.</color>";

    public static readonly string[] Analisis =
    {
        "Tamaño de partícula|82 nm|60-120 nm",
        "ARNm encapsulado|94 %|> 80 %",
        "ARNm íntegro|90 %|> 50 %",
        "Esterilidad y endotoxinas|conforme|conforme",
    };

    public static readonly Pieza[] Almacenes =
    {
        new Pieza("ultra", "ULTRACONGELADOR", "-70 °C", new UnityEngine.Color(0.45f, 0.75f, 1f), true,
            "A -70 °C el ARNm y las nanopartículas se conservan durante meses. Mantener esa temperatura desde la fábrica hasta el centro de vacunación es la CADENA DE FRÍO."),
        new Pieza("nevera", "NEVERA", "2-8 °C", new UnityEngine.Color(0.55f, 0.95f, 0.85f), false,
            "En la nevera la vacuna aguanta solo días o pocas semanas: sirve en el centro de vacunación, justo antes de usarla, pero no para guardar el lote."),
        new Pieza("mesa", "MESA", "25 °C", new UnityEngine.Color(1f, 0.7f, 0.4f), false,
            "A temperatura ambiente el ARNm se degrada en horas y las nanopartículas se agregan: el lote se perdería."),
    };

    public static readonly string[] PistasCalidad =
    {
        "Primero el ANALIZADOR. Después piensa: el ARNm es una molécula muy frágil… ¿dónde se estropea más despacio?",
        "Cuanto más frío, más despacio se degrada: el lote se guarda en el ULTRACONGELADOR a -70 °C.",
    };

    public const string CalidadHecho =
        "<color=#39FF88><b>LOTE APROBADO Y GUARDADO A -70 °C.</b></color> Tienes una vacuna candidata. Pero ¿funciona? " +
        "Se vacunó a un grupo de ratones con tu lote (y a otro con placebo) y se tomó su suero.";

    // ------------------------------------------------------------------ 6 · ensayo de neutralización (pipeta + placa)

    public const string NeutralizacionTitulo = "PRECLÍNICA: ENSAYO DE NEUTRALIZACIÓN";
    public const string NeutralizacionEnunciado =
        "¿Tienen los ratones vacunados anticuerpos que <b>neutralizan</b> al virus? Prepara la placa de cultivo en el <b>orden correcto</b>.\n" +
        "<color=#FFD25A>Usa la PIPETA para llevar cada reactivo a la PLACA (izquierda) y el botón INCUBAR 1 h de la placa cuando toque.</color>\n" +
        "<size=85%><color=#A9BED2>Las células son el 'detector': solo deben encontrarse con el virus cuando los anticuerpos hayan tenido tiempo de atraparlo.</color></size>";

    // Orden correcto: 0 > 1 > 2 > 3 > 4. El 2 (incubar) es un botón; los demás, reactivos de la pipeta.
    public static readonly PasoSecuencia[] Neutralizacion =
    {
        new PasoSecuencia("SUERO", "Diluir suero",
            "SUERO: preparas diluciones seriadas del suero (1:10, 1:20, 1:40… 1:1280) en la fila VACUNADO y en la fila PLACEBO. Cada pocillo tiene la mitad de anticuerpos que el anterior. La fila de CONTROL solo lleva medio de cultivo."),
        new PasoSecuencia("VIRUS", "Añadir virus",
            "VIRUS: añades la MISMA cantidad de virus a las filas VACUNADO y PLACEBO (puntos rojos). La fila CONTROL no lleva virus: servirá para comprobar que las células están sanas. (En la realidad este paso se hace en una cabina de bioseguridad nivel 3, como la sala de vidrio de NanoLab.)"),
        new PasoSecuencia("INCUBAR 1 h", "Incubar 1 h",
            "INCUBAR 1 h a 37 °C: los anticuerpos del suero se pegan a las espigas del virus. Donde hay suficientes anticuerpos, el virus queda neutralizado (los puntos se vuelven verdes)."),
        new PasoSecuencia("CÉLULAS", "Añadir células",
            "CÉLULAS: añades células Vero (de riñón de mono, muy sensibles al virus) a todos los pocillos. Son el detector: si el virus está libre, las infecta y las destruye (efecto citopático)."),
        new PasoSecuencia("CRISTAL VIOLETA", "3 días + teñir",
            "TEÑIR Y LEER: tras 3 días a 37 °C tiñes la placa con CRISTAL VIOLETA (el mismo colorante de la tinción de Gram). Las células vivas se tiñen de VIOLETA; donde el virus las destruyó, el pocillo queda TRANSPARENTE."),
    };

    public static readonly Pieza[] ReactivosEnsayo =
    {
        new Pieza("suero", "SUERO", "vacunados / placebo", new UnityEngine.Color(1f, 0.88f, 0.5f), true, ""),
        new Pieza("virus", "VIRUS", "SARS-CoV-2 (modelo)", new UnityEngine.Color(1f, 0.25f, 0.25f), true, ""),
        new Pieza("celulas", "CÉLULAS VERO", "el detector", new UnityEngine.Color(0.95f, 0.5f, 0.58f), true, ""),
        new Pieza("violeta", "CRISTAL VIOLETA", "colorante", new UnityEngine.Color(0.55f, 0.25f, 0.9f), true, ""),
    };

    // Paso de la secuencia que corresponde a cada reactivo del ensayo.
    public static int PasoDeReactivo(string id)
    {
        switch (id)
        {
            case "suero": return 0;
            case "virus": return 1;
            case "celulas": return 3;
            case "violeta": return 4;
            default: return -1;
        }
    }

    // Por qué no es el paso correcto (esperado = paso que tocaba, hecho = el que se intentó). 'arruina' = hay que repetir la placa.
    public static string ErrorNeutralizacion(int esperado, int hecho, out bool arruina)
    {
        arruina = false;
        switch (hecho)
        {
            case 0: return "Las diluciones del suero ya están hechas.";
            case 1:
                return "Primero se preparan las diluciones del SUERO en la placa: así cada pocillo tiene una cantidad distinta de anticuerpos. Después se añade la misma cantidad de virus a todos.";
            case 2:
                return esperado < 2
                    ? "Incubar sirve para que los anticuerpos del suero atrapen al virus. Todavía no tienes en la placa " + (esperado == 0 ? "ni suero ni virus." : "el virus.")
                    : "Suero y virus ya se incubaron juntos.";
            case 3:
                arruina = true;
                return esperado < 2
                    ? "PLACA DESCARTADA. Las células son el detector: si llegan antes de que suero y virus estén mezclados, no se puede saber si los anticuerpos protegen. Empieza con una placa nueva."
                    : "PLACA DESCARTADA. Si las células llegan antes de que los anticuerpos tengan tiempo de unirse al virus, el virus las infecta primero y el ensayo subestima la protección. Incuba suero + virus 1 h antes. Empieza con una placa nueva.";
            case 4:
                return "Aún no hay nada que teñir: faltan pasos, y el efecto del virus sobre las células tarda unos días en verse.";
        }
        return "Ese no es el paso que toca.";
    }

    public static readonly string[] PistasNeutralizacion =
    {
        "Piensa en la lógica: 1) pon lo que quieres medir (los anticuerpos del suero), 2) ponlos frente al enemigo (el virus), 3) dales tiempo para actuar, 4) añade el detector (las células), 5) espera y tiñe.",
        "El orden es SUERO > VIRUS > botón INCUBAR 1 h > CÉLULAS > CRISTAL VIOLETA.",
    };

    // ------------------------------------------------------------------ 7 · lectura del título (apuntar a la columna)

    public const string TituloTitulo = "LEE LA PLACA: TÍTULO DE ANTICUERPOS";
    public const string TituloEnunciado =
        "VIOLETA = células vivas (el virus fue neutralizado); TRANSPARENTE con X = células destruidas. " +
        "El <b>título</b> es la dilución MÁS ALTA del suero que todavía protege a las células.\n" +
        "<color=#FFD25A>Apunta con el rayo a la dilución del título (los números de arriba de la placa, pantalla derecha) y aprieta el gatillo.</color>";

    public static string RespuestaColumna(int c, out bool correcta)
    {
        correcta = c == 5;
        string d = c >= 0 && c < 8 ? new[] { "1:10", "1:20", "1:40", "1:80", "1:160", "1:320", "1:640", "1:1280" }[c] : "?";
        if (correcta)
            return "Hasta 1:320 los pocillos de la fila VACUNADO siguen violeta: aunque el suero se diluyó 320 veces, aún tenía anticuerpos suficientes para neutralizar al virus. A 1:640 ya no. El TÍTULO NEUTRALIZANTE es 320.";
        if (c < 5)
            return $"A {d} el suero sí protege, pero no es la dilución MÁS ALTA que protege. Sigue la fila VACUNADO hacia la derecha hasta el último pocillo violeta.";
        return $"A {d} el pocillo de la fila VACUNADO está transparente y con X: las células murieron porque quedaban muy pocos anticuerpos. Busca el ÚLTIMO pocillo violeta.";
    }

    public static readonly string[] PistasTitulo =
    {
        "Recorre la fila VACUNADO de izquierda a derecha: el suero está cada vez más diluido. ¿Dónde está el último pocillo violeta?",
        "Fíjate en los controles: PLACEBO (todas destruidas) demuestra que sin anticuerpos el virus gana; CONTROL (todas vivas) demuestra que las células estaban sanas. La respuesta es 1:320.",
    };

    public const string TituloConcepto =
        "CONTROLES: la fila PLACEBO prueba que la protección se debe a la vacuna y la fila CONTROL prueba que las células estaban sanas. Sin controles, el resultado no se puede interpretar.";

    // ------------------------------------------------------------------ 8 · fases (secuencia con botones)

    public const string FasesTitulo = "ORDENA LAS FASES CLÍNICAS";
    public const string FasesEnunciado =
        "Tu vacuna funciona en el laboratorio y en animales. Ahora hay que probarla en personas, siempre con voluntarios informados y bajo la supervisión de un comité de ética. " +
        "Pulsa los botones <b>en el orden en que se hacen</b>: cada etapa solo empieza si la anterior salió bien.";

    public static readonly PasoSecuencia[] Fases =
    {
        new PasoSecuencia("FASE I", "Fase I", "FASE I · 20 a 100 voluntarios sanos. Pregunta: ¿es SEGURA? ¿Qué DOSIS se tolera? Se vigilan de cerca los efectos adversos."),
        new PasoSecuencia("FASE II", "Fase II", "FASE II · Cientos de voluntarios, de distintas edades. Pregunta: ¿genera RESPUESTA INMUNE (anticuerpos)? ¿Qué dosis y cuántas dosis (pauta)?"),
        new PasoSecuencia("FASE III", "Fase III", "FASE III · Miles a decenas de miles de voluntarios, por SORTEO, con PLACEBO y DOBLE CIEGO. Pregunta: ¿PROTEGE de la enfermedad (eficacia)?"),
        new PasoSecuencia("APROBACIÓN", "Aprobación", "APROBACIÓN · La agencia reguladora (DIGEMID en Perú, FDA en EE. UU., EMA en Europa) y la OMS revisan TODOS los datos de seguridad, eficacia y fabricación antes de autorizarla."),
        new PasoSecuencia("FASE IV", "Fase IV", "FASE IV · Farmacovigilancia: con la vacuna ya en uso en millones de personas, se siguen vigilando efectos muy raros (1 en 100 000) y cuánto dura la protección."),
    };

    public static readonly int[] OrdenBotonesFases = { 2, 3, 0, 4, 1 };   // FASE III, APROBACIÓN, FASE I, FASE IV, FASE II

    public static string ErrorFases(int esperado, int pulsado)
    {
        switch (esperado)
        {
            case 0: return "Nunca se empieza con muchas personas: lo primero es comprobar en un grupo PEQUEÑO de voluntarios sanos (20-100) que la vacuna es segura.";
            case 1: return "Ya sabes que es segura en pocas personas. Antes de un ensayo enorme hay que fijar la DOSIS y comprobar la respuesta inmune en cientos de personas.";
            case 2: return "Para aprobar una vacuna hay que demostrar que PROTEGE. Eso exige un ensayo grande, con miles de personas, sorteo y grupo placebo.";
            case 3: return pulsado == 4 ? "La fase IV es vigilancia DESPUÉS de autorizar la vacuna. Antes, alguien tiene que revisar los datos y aprobarla." : "Las fases I, II y III ya están hechas. ¿Qué falta para que la vacuna pueda usarse en la población?";
            default: return "Ya está aprobada. ¿Se deja de vigilar?";
        }
    }

    public static readonly string[] PistasFases =
    {
        "Las fases van de MENOS a MÁS personas: primero seguridad, luego dosis y respuesta inmune, luego eficacia.",
        "Después de la fase III viene la revisión de la agencia reguladora, y la vigilancia continúa cuando la vacuna ya se usa.",
    };

    // ------------------------------------------------------------------ 9 · eficacia (pregunta con botones)

    public const int Voluntarios = 15000, CasosVacuna = 15, CasosPlacebo = 150;

    public static readonly Pregunta Eficacia = new Pregunta
    {
        titulo = "ANALIZA LA FASE III: EFICACIA",
        enunciado = "30 000 voluntarios, repartidos por sorteo y con doble ciego. Tras 3 meses se abre el ciego: de 15 000 vacunados enfermaron <b>15</b>; de 15 000 con placebo enfermaron <b>150</b>.\n" +
                    "<b>Eficacia = (1 - riesgo en vacunados / riesgo en placebo) x 100.</b> ¿Cuál es la eficacia de tu vacuna?",
        visual = "Casos de COVID-19 sintomática en cada grupo.",
        opciones = new[]
        {
            new Opcion("10 %", "10 %", false,
                "10 % es el cociente 15 / 150 = 0,10: los vacunados tuvieron el 10 % del riesgo del grupo placebo. La eficacia es lo que la vacuna EVITA: 1 - 0,10."),
            new Opcion("90 %", "90 %", true,
                "Riesgo vacunados = 15 / 15 000 = 0,1 %. Riesgo placebo = 150 / 15 000 = 1 %. Eficacia = (1 - 0,1 / 1) x 100 = 90 %: la vacuna evitó 9 de cada 10 casos que habrían ocurrido sin ella."),
            new Opcion("99,9 %", "99,9 %", false,
                "99,9 % es la proporción de vacunados que NO enfermó, pero en el grupo placebo también se libró el 99 %: casi nadie enferma en 3 meses. La eficacia compara ambos grupos."),
            new Opcion("135", "135 casos", false,
                "135 es la diferencia de casos (150 - 15). Depende de cuánta gente participa y de cuánto circula el virus. La eficacia es una proporción: compara los riesgos de los dos grupos."),
        },
        pistas = new[]
        {
            "Primero calcula el riesgo de cada grupo: casos / personas. Luego divide el de vacunados entre el de placebo.",
            "15/15 000 dividido entre 150/15 000 = 0,1. La eficacia es (1 - 0,1) x 100.",
        },
        concepto = "Con 90 % de eficacia y efectos graves iguales que con placebo, la agencia reguladora APRUEBA tu vacuna… y la sigue vigilando (fase IV).",
    };

    public const string Seguridad =
        "<b>SEGURIDAD</b> (vacuna / placebo)\n" +
        "Dolor en el brazo: 70 % / 20 %\n" +
        "Cansancio o fiebre leve (1-2 días): 15 % / 4 %\n" +
        "Efectos adversos graves: 0,05 % / 0,05 %";

    // ------------------------------------------------------------------ cierre

    public const string Fragmento = "Recuerdo científico";
    public const string TextoFragmento =
        "Baymax recuerda: \"Una vacuna de ARNm es un mensaje: le enseña a tus células a fabricar la espiga para que tu sistema inmune aprenda a reconocerla. " +
        "Y solo se confía en ella cuando un ensayo justo lo demuestra.\"";

    public const string EnElCuerpo =
        "<b>Y en el brazo…</b> las células toman las nanopartículas, leen el ARNm con sus ribosomas y fabrican la espiga. El sistema inmune la ve, " +
        "produce anticuerpos y guarda MEMORIA (linfocitos B y T). El ARNm se degrada en pocos días y nunca entra en el núcleo: no cambia tu ADN.";

    public const string Aprendido =
        "• El ANTÍGENO es la pieza del virus que el sistema inmune aprende a reconocer: la espiga S, la llave de entrada.\n" +
        "• Un ARNm tiene 5 partes: CAP 5' > UTR 5' > GEN > UTR 3' > COLA POLI(A).\n" +
        "• Se fabrica en un tubo copiando un molde de ADN con la ARN polimerasa T7 y nucleótidos (transcripción in vitro).\n" +
        "• Las NANOPARTÍCULAS LIPÍDICAS protegen el ARNm y lo meten en las células.\n" +
        "• Cada lote pasa CONTROL DE CALIDAD y viaja en CADENA DE FRÍO.\n" +
        "• El ensayo de NEUTRALIZACIÓN mide si los anticuerpos impiden la infección; el TÍTULO es la dilución más alta que protege.\n" +
        "• FASES I > II > III > aprobación > IV: de pocas a millones de personas.\n" +
        "• EFICACIA = (1 - riesgo vacunados / riesgo placebo) x 100.";

    // ------------------------------------------------------------------ tableros

    public const string GuiaTitulo = "GUÍA · ¿CÓMO SE CREA UNA VACUNA DE ARNm?";
    public static readonly (string titulo, string texto)[] Guia =
    {
        ("ANTÍGENO", "Se estudia el virus y se elige la pieza que el sistema inmune debe reconocer: la proteína ESPIGA (S)."),
        ("DISEÑO DEL ARNm", "Se escribe el mensaje: CAP 5' > UTR 5' > gen de la espiga > UTR 3' > cola poli(A)."),
        ("SÍNTESIS", "Transcripción in vitro: molde de ADN + ARN polimerasa T7 + nucleótidos, 2 h a 37 °C. Luego se purifica."),
        ("NANOPARTÍCULAS", "El ARNm se envuelve en 4 lípidos (ionizable, fosfolípido, colesterol, PEG) en un mezclador microfluídico."),
        ("CALIDAD Y FRÍO", "Cada lote se analiza (tamaño, encapsulación, integridad, esterilidad) y se guarda a -70 °C."),
        ("PRECLÍNICA", "En cultivos y animales: ¿genera anticuerpos neutralizantes? ¿es segura? Ensayo de neutralización y título."),
        ("FASES I, II y III", "De decenas a miles de voluntarios: seguridad, dosis y respuesta inmune, y por último EFICACIA con placebo y doble ciego."),
        ("APROBACIÓN Y FASE IV", "La agencia reguladora (DIGEMID, FDA, EMA) revisa todo; después se sigue vigilando en millones de personas."),
    };
    public const string GuiaPie =
        "Contra la COVID-19 las primeras vacunas de ARNm se diseñaron en días y se aprobaron en menos de un año porque la tecnología llevaba décadas investigándose, " +
        "las fases se solaparon y se fabricó a la vez que se probaba. NO se saltó ninguna fase.";

    public const string ConceptosTitulo = "CONCEPTOS CLAVE";
    public static readonly (string termino, string definicion)[] Conceptos =
    {
        ("Antígeno", "molécula del patógeno que el sistema inmune reconoce."),
        ("ARN mensajero (ARNm)", "copia de trabajo de un gen; los ribosomas la leen para fabricar una proteína."),
        ("Transcripción in vitro", "fabricar ARN en un tubo copiando un molde de ADN con una ARN polimerasa."),
        ("Nanopartícula lipídica", "burbuja de lípidos de ~80 nm que protege el ARNm y lo lleva al interior de la célula."),
        ("Cadena de frío", "mantener la vacuna a la temperatura correcta desde la fábrica hasta la inyección."),
        ("Anticuerpo neutralizante", "anticuerpo que impide que el virus entre en la célula."),
        ("Título", "dilución más alta de un suero que aún tiene efecto."),
        ("Placebo / doble ciego", "inyección sin vacuna para comparar / nadie sabe quién recibió qué."),
        ("Eficacia / efectividad", "protección medida en el ensayo / en la vida real."),
        ("Memoria inmunológica", "linfocitos B y T que recuerdan al antígeno y responden rápido si llega el virus real."),
        ("Farmacovigilancia", "vigilancia de la seguridad cuando la vacuna ya está en uso."),
    };
}
