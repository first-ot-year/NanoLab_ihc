# Guion de voces de NanoLab VR

En un laboratorio presencial alguien te habla: el docente explica, el compañero comenta. Antes, NanoLab solo se **leía**. Ahora hay dos voces:

- **Voz general (narrador):** la graba el equipo. Suena al empezar y la primera vez que se entra en cada sala. No sale de un punto de la sala (2D).
- **Voz de Baymax:** la "voz interior" del jugador, en primera persona (el jugador **es** Baymax). Se puede generar con la voz sintética de Windows o con tu voz editada con IA.

Si falta un audio, **el juego sigue igual** y queda el texto de siempre (reloj, carteles), que además hace de subtítulo. Así se pueden ir grabando poco a poco.

## Dónde van los audios

| Voz | Carpeta | Nombre del archivo |
|---|---|---|
| Narrador | `Assets/NanoLab/Resources/Voz/Narrador/` | `<clave>.ogg` (o `.wav`, `.mp3`) |
| Baymax | `Assets/NanoLab/Resources/Voz/Baymax/` | `<clave>.ogg` (o `.wav`, `.mp3`) |

- La **clave** es la de las tablas (por ejemplo `N01_bienvenida.ogg`, `B_M1_frotis.wav`). Unity los importa solos al abrir el proyecto.
- Los audios van a **LFS**:
  - `.ogg` mono: unas 50 frases ocupan entre 1 y 3 MB. Es lo recomendado.
  - `.wav` de la herramienta: entre 4 y 6 MB en total.

## Cómo grabar (narrador y tu voz)

- **Sala:** silenciosa, sin eco. Un armario con ropa o un cuarto con cortinas sirve.
- **Micrófono:** el móvil, a un palmo de la boca, sin soplar.
- **Ritmo:** habla tranquilo, como un guía de museo. Cada frase dura menos de 8 s.
- **Tomas:** deja medio segundo de silencio al principio y al final. Graba cada frase dos veces y elige la mejor.
- **Exportar:** en Audacity (gratis), recorta los silencios largos, *Efecto › Normalizar* a -3 dB y *Archivo › Exportar › OGG*, mono.
- **Con IA:** grábalas con tu voz y pásalas por la herramienta (cambio de voz, tono más grave y suave). Exporta un archivo por clave con el mismo nombre.

## Voz de Baymax con la voz sintética de Windows

1. Instala una voz en español: **Configuración › Hora e idioma › Voz › Agregar voces › Español (México o España)**. Hoy en el PC solo hay voces en inglés.
2. Desde la raíz del proyecto, en PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File Herramientas\generar_voces_baymax.ps1 -Listar
powershell -ExecutionPolicy Bypass -File Herramientas\generar_voces_baymax.ps1 -Velocidad 0.9 -Tono 0.85
```

- Lee `Herramientas/guion_baymax.csv` y deja un `.wav` por frase en `Resources/Voz/Baymax`.
- Opciones:
  - `-Voz Sabina` elige la voz por su nombre.
  - `-Solo B_M1_frotis,B_bien_1` genera solo esas frases.
  - `-Forzar` sobrescribe también las frases grabadas a mano.
- Para un narrador provisional mientras no se graba: `-Guion Herramientas\guion_narrador.csv -Salida Assets\NanoLab\Resources\Voz\Narrador`.
- No sobrescribe un `.ogg` o `.mp3` que ya hayas grabado con la misma clave.

## Voz general (narrador), grabada por el equipo

| Clave | Cuándo suena | Texto |
|---|---|---|
| `N01_bienvenida` | Al pulsar "Ya lo veo bien" en el aviso del casco | Bienvenido a NanoLab, el laboratorio de microbiología. Aquí puedes equivocarte sin miedo: todo se puede repetir. Si no sabes qué hacer, mira tu muñeca izquierda. |
| `N02_espejo` | Primera vez cerca del espejo de la entrada, mirándolo | Ese que ves en el espejo eres tú: Baymax. Sus manos son tus manos. Para agarrar algo, aprieta el botón lateral del mando. |
| `N03_microscopia` | Primera vez en los puestos de microscopía | Estos son los puestos de microscopía. Aquí se observa lo que el ojo no puede ver. |
| `N04_cepario` | Primera vez en el cepario | El cepario guarda las cepas de bacterias, bien rotuladas y siempre en frío. |
| `N05_alto_riesgo` | Primera vez en la sala de alto riesgo | Sala de alto riesgo, nivel de bioseguridad tres. Aquí se trabaja con virus: el aire se filtra y nada sale sin descontaminar. |
| `N06_centro` | Primera vez en el Centro de Experimentación (con las luces de la U) | Este es el Centro de Experimentación. Las estaciones están en el orden del trabajo, en forma de U. La misión uno empieza en el banco de muestras. |
| `N07_vacunas` | Primera vez en la sala de vacunas | El laboratorio de vacunas. Lo que aprendiste observando y experimentando te servirá ahora para crear una vacuna. |
| `N08_final` | Después del tercer recuerdo | Recuperaste los tres recuerdos de Baymax. Recuerda que esto es un modelo simplificado: en un laboratorio real se trabaja con un docente y con protocolos de seguridad. |

Cada frase de sala suena **una vez por sesión** y nunca con el menú, la ayuda abierta o la cara en el ocular.

## Voz de Baymax (voz interior, primera persona)

### Pasos de las misiones (suenan al empezar el paso; **Y** repite el del paso actual)

| Clave | Paso | Texto |
|---|---|---|
| `B_M1_inicio` | Empieza la misión 1 | Misión uno: ¿qué hay en estas muestras? Empecemos por la muestra A, en el banco. |
| `B_M1_frotis` | FROTIS | Cojo un portaobjetos de la bandeja y toco la placa de la muestra A. |
| `B_M1_tincion` | TINCIÓN | Ahora la tinción de Gram: los cuatro goteros, en orden, del uno al cuatro. |
| `B_M1_observar` | OBSERVAR | Pongo el portaobjetos en la platina y acerco la cara al ocular. |
| `B_M1_clasificar` | CLASIFICAR | Comparo las tres muestras y las clasifico en conclusiones: bacteria, hongo o virus. |
| `B_M1_identificar` | IDENTIFICAR | Ya tengo las pistas. En la terminal: ¿qué bacteria es la muestra A? |
| `B_M2_inicio` | Empieza la misión 2 | Misión dos: ¿qué detiene a esta bacteria? |
| `B_M2_estrategia` | ESTRATEGIA | En la terminal decido qué parte de la bacteria voy a atacar. |
| `B_M2_preparar` | PREPARAR | En la prensa elijo el vial que ataca esa parte y formulo la cápsula. |
| `B_M2_experimentar` | EXPERIMENTAR | Pongo la cápsula en la cámara y pulso activar. |
| `B_M2_no_funciono` | El agente no funcionó | No funcionó. No pasa nada: pienso otra estrategia en la terminal. |
| `B_M2_comparar` | COMPARAR (solo si funcionó) | ¡Funcionó! Comparo el antes y el después en el microscopio. |
| `B_M2_explicar` | EXPLICAR | En conclusiones explico por qué funcionó. |
| `B_M3_inicio` | Empieza la misión 3 | Misión tres: contra un virus no sirve un antibiótico. Voy a crear una vacuna. |
| `B_M3_antigeno` | Paso 1 · ANTÍGENO | Toco la parte del virus que usaría como antígeno. |
| `B_M3_diseno` | Paso 2 · DISEÑO | Ordeno las piezas del ARN mensajero en el molde. |
| `B_M3_sintesis` | Paso 3 · SÍNTESIS | Con la pipeta llevo los reactivos al tubo y fabrico el ARN. |
| `B_M3_encapsular` | Paso 4 · ENCAPSULAR | Envuelvo el ARN en nanopartículas de grasa con el mezclador. |
| `B_M3_calidad` | Paso 5 · CALIDAD | Analizo el lote y lo guardo en frío. |
| `B_M3_preclinica` | Paso 6 · PRECLÍNICA | Pruebo si el suero bloquea al virus en las células. |
| `B_M3_titulo` | Paso 7 · TÍTULO | Miro al microscopio y leo hasta dónde protege la vacuna. |
| `B_M3_fases` | Paso 8 · FASES | Ordeno las fases de los ensayos clínicos. |
| `B_M3_eficacia` | Paso 9 · EFICACIA | ¿A cuántas personas protege? Comparo los grupos. |

### Respuestas, ayuda, recuerdos y descansos

| Clave | Cuándo | Texto |
|---|---|---|
| `B_bien_1` / `B_bien_2` / `B_bien_3` | Acierto (una al azar) | ¡Eso es! · Correcto. · Muy bien, sigamos. |
| `B_aun_no_1` / `B_aun_no_2` / `B_aun_no_3` | Error (una al azar) | Todavía no. Leo la pista de la pantalla. · Casi. Pruebo otra vez. · No pasa nada, lo intento de nuevo. |
| `B_falta_paso` | Aviso "FALTA UN PASO" | Antes me falta un paso. |
| `B_atasco` | 60 s sin avanzar en un paso | ¿Me ayudo? Puedo pulsar pista, o el botón Y para ver adónde ir. |
| `B_recuerdo_1` | Misión 1 completa | Recuerdo recuperado: observar. Ya sé distinguir una bacteria, un hongo y un virus. |
| `B_recuerdo_2` | Misión 2 completa | Recuerdo recuperado: experimentar. Ya sé probar qué detiene a una bacteria. |
| `B_recuerdo_3` | Misión 3 completa | Recuerdo recuperado: crear. Ya sé cómo se hace una vacuna. |
| `B_descanso` | Tras las misiones 1 y 2, y cada 20 min seguidos (si DESCANSOS = SÍ) | Progreso guardado. Si me canso, me quito el visor un momento: todo seguirá aquí. |

Las respuestas suenan como mucho una vez cada 4 s y nunca interrumpen otra frase.

## Cómo funciona (para el equipo)

| Pieza | Qué hace |
|---|---|
| `Scripts/Microbiologia/VozNanoLab.cs` | Cola con prioridades: narrador, luego paso, luego respuesta. Nunca suenan dos voces a la vez. Baja la música mientras habla y se oye con el volumen VOZ del menú ☰ › COMODIDAD |
| `Scripts/Microbiologia/GuiaVoz.cs` | Frases de las salas (una vez por sesión) y descansos |
| Enganches | Bienvenida: `AvisoInicio`. Pasos: `GuiaReto` (misiones 1 y 2, y vacuna). Respuestas: `Aviso`. Fallo: `RetoManager`. Recuerdos: `RetoManager` y `RetoVacuna`. **Y**: `BrazaleteBaymax` |
| Prueba | `NanoLab › Probar misiones 1 y 2` comprueba que no falla sin audios, que se piden las frases de cada paso, el fallo y los recuerdos sin solaparse, y los descansos |
