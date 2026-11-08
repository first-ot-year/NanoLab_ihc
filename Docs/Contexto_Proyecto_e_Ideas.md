# NanoLab VR · Contexto del proyecto e ideas para nuevos casos

> Documento de contexto para retomar el proyecto (o pasárselo a otra persona o a una IA): qué es NanoLab VR, en qué
> estado está al **7 de noviembre de 2026**, cómo están organizados los archivos, qué espacios tiene el laboratorio y
> **cómo aprovecharlos todos** con nuevos casos/desafíos. Las ideas de la sección 7 en adelante son **propuestas**:
> ninguna está implementada todavía.

---

## Índice

1. [Qué es NanoLab VR](#1-qué-es-nanolab-vr)
2. [Estado actual](#2-estado-actual-7112026)
3. [Cómo abrirlo y probarlo](#3-cómo-abrirlo-y-probarlo)
4. [Los espacios del laboratorio](#4-los-espacios-del-laboratorio)
5. [Mapa de archivos del proyecto](#5-mapa-de-archivos-del-proyecto)
6. [Lenguaje de interacción común (criterios IHC)](#6-lenguaje-de-interacción-común-criterios-ihc)
7. [Idea central: la historia "El brote"](#7-idea-central-la-historia-el-brote)
8. [Fichas de los casos propuestos](#8-fichas-de-los-casos-propuestos)
9. [Otros usos de cada espacio](#9-otros-usos-de-cada-espacio)
10. [Cómo se construye un caso nuevo (guía técnica)](#10-cómo-se-construye-un-caso-nuevo-guía-técnica)
11. [Hoja de ruta sugerida](#11-hoja-de-ruta-sugerida)
12. [Pendientes y avisos conocidos](#12-pendientes-y-avisos-conocidos)

---

## 1. Qué es NanoLab VR

**Laboratorio de microbiología en realidad virtual** para Meta Quest. Proyecto del curso **Interacción Humano
Computador** (UTEC, 2026-2, prof. Teófilo Chambilla). Integrantes: Carlos Angel, Luis Romero, Carlos Villegas.

- **Problema:** gran parte de la microbiología no se ve a simple vista; entenderla exige microscopios, muestras, tiempo
  de laboratorio y acompañamiento docente. Eso limita explorar, repetir y equivocarse.
- **Propuesta:** el estudiante **es Baymax** (versión infantil), un robot asistente de salud que perdió sus **recuerdos
  científicos** y los recupera resolviendo casos con sus propias manos.
- **Objetivo:** el **aprendizaje**. Los errores no penalizan: explican por qué y se puede reintentar.
- **Audiencia:** estudiantes de 4.º y 5.º de secundaria y universitarios de carreras afines. Persona principal:
  *Sebastián* (15 años, aprende experimentando, poca experiencia en VR). Secundaria: *Angie* (universitaria, busca
  repasos breves y repetibles).
- **Aviso educativo:** NanoLab VR es un complemento. Cantidades, tiempos y resultados están simplificados y no
  representan protocolos reales.

**Tecnología:** Unity **2022.3.62f3** · Meta XR SDK **201** (Interaction SDK, Meta XR Simulator) · TextMeshPro ·
Git LFS para modelos, texturas y audio. No hay asmdef: todos los scripts compilan juntos (Assembly-CSharp).

---

## 2. Estado actual (7/11/2026)

### Lo que funciona

- **Laboratorio completo** en la escena `Assets/NanoLab/Scenes/Microbiologia.unity`: recibidor, 10 puestos de
  microscopía, cepario, sala de alto riesgo BSL-3 con cabina de descontaminación, Centro de Experimentación y portal.
- **Microscopio óptico** jugable (portaobjetos, muestras, platina, ocular) y **estación de análisis 3D** con 5 virus.
- **CASO 01 · Crea la vacuna** (único caso por ahora), detallado en [`Docs/Reto_Vacuna.md`](Reto_Vacuna.md):
  el estudiante fabrica una vacuna de ARNm en **9 pasos** (antígeno, diseño, síntesis, nanopartículas, calidad y frío,
  neutralización, título, fases clínicas y eficacia) en un mostrador con 4 mesas. **Se construye por código** al dar
  Play (`MontajeVacuna`); no hay que hornear nada.
- **Ayudas pensadas para no tener que leer:**
  - apartado fijo **AHORA** en la estación (la acción actual + el icono del botón del mando);
  - **AYUDA**: una animación hace el gesto (copia fantasma del objeto, "COGE ESTO > LLÉVALO > PONLO AQUÍ");
  - **PASO A PASO**: la animación se repite sin parar y guía **pieza a pieza en el orden real** (p. ej. "COGE CAP 5'
    > HUECO 1", luego "UTR 5' > HUECO 2"); si algo está mal colocado, primero enseña a sacarlo;
  - virus **holográfico** para el paso del antígeno (se toca y se gira con la mano).
- **Interacción física:** botones **poke** de Meta dentro de una **carcasa** apoyada en el mostrador (se hunden con el
  dedo y con el rayo); carteles sobre **atril**; ultracongelador y nevera con **puerta animada**.
- **EFICACIA** como 4 preguntas sencillas sobre las barras (sin fórmulas), con revelaciones en la gráfica.
- **Aviso de inicio** con etiquetas claras: "Ya lo veo bien ›", "Siguiente ›", "Empezar a jugar" y "‹ Atrás".
- Letra **Nunito SemiBold** en toda la interfaz; sala principal sin los paneles celestes antiguos.
- **Prueba automática:** menú `NanoLab > Probar reto de vacuna (play mode)` → `Logs/prueba_vacuna.txt` con
  `RESULTADO: CORRECTO` y **36 OK**, más capturas en `Logs/Capturas/`.

### Repositorios

| Repositorio | Para qué |
|---|---|
| `first-ot-year/NanoLab_ihc` | Repositorio del grupo, con todo el historial. Superó su cuota de Git LFS (no deja descargar 16 lightmaps antiguos). |
| `CarlosVillegasArce/NanoLab_v2` · rama **`main`** | Copia personal de respaldo con el estado actual completo (contenido idéntico al commit `b775389` de `feat-respaldo`). **Es la que hay que clonar.** |
| `CarlosVillegasArce/NanoLab_v2` · rama `escena-10-24` | Solo respaldo: la escena y la luz horneada guardadas el 7/11 a las 10:24 (puerta automática, compuerta y botón de checkpoint ya colocados; el juego los crea solo si faltan). No mezclar con `main`. |

---

## 3. Cómo abrirlo y probarlo

```bash
git clone https://github.com/CarlosVillegasArce/NanoLab_v2.git
cd NanoLab_v2
git lfs pull            # baja modelos, texturas y audio
```

1. Abrir la carpeta con **Unity 2022.3.62f3** (no con Unity 6).
2. Escena: `Assets/NanoLab/Scenes/Microbiologia.unity`.
3. **Sin visor:** Meta XR Simulator (ver [`Docs/Simulador_PC.md`](Simulador_PC.md)). En el Editor, **F7** resuelve el
   paso actual del caso y **F6** comete el error típico.
4. **Prueba automática:** `NanoLab > Probar reto de vacuna (play mode)`; esperado `RESULTADO: CORRECTO` con 36 OK.
   Para capturas con colores reales, ejecutarla con el editor **con ventana** (en modo batch los botones poke salen
   casi transparentes porque su color se anima con `WaitForEndOfFrame`).
5. **Compilar para Quest:** menú `NanoLab > Compilar APK para Quest` (ver README).

---

## 4. Los espacios del laboratorio

| # | Espacio | Qué tiene hoy | Uso actual |
|---|---|---|---|
| 1 | **Recibidor** (`Bienvenida`) | Espejo de Baymax, 5 balizas de áreas (`AreasLaboratorio/Area_1..5`) | Aparición del jugador y aviso de inicio |
| 2 | **Sala principal** (`Puestos/Puesto_01..10`) | 10 puestos con microscopio, gradilla de cultivos, bandeja y desinfectante | Microscopía libre |
| 3 | **Cepario** (`Cepario/CeparioBacterias`) | Placas de *E. coli*, *S. aureus* y *B. subtilis*, bandeja, desinfectante | Decorado / consulta |
| 4 | **Sala de alto riesgo BSL-3** (`SalaAltoRiesgo`) | Cabina de descontaminación (esclusa de 11 s), cabina de bioseguridad, autoclave, incubadora, zona de criopreservación a -80 °C, mesa y rack de criotubos, **estación de análisis 3D** (5 virus) | Visor de virus |
| 5 | **Centro de Experimentación** (`CentroExperimentacion`) | Recepción, **estación de vacunas (CASO 01)**, 8 estaciones (`1_BancoMuestras`, `2_Tincion`, `3_Microscopia`, `4_Biblioteca`, `5_Terminal`, `6_Prensa`, `7_CamaraExperimental` oculta, `8_Archivo`), equipamiento (autoclave, cabina, incubadora, incubadora CO₂, ultracongelador), puerta automática | CASO 01; el resto es mobiliario del caso antiguo de bacterias (oculto) |
| 6 | **Portal** (`Portal/Portal_Laboratorio`) | Puerta a otro laboratorio | Sin uso |
| 7 | **Sillas** | Sillas interactuables y modo sentado | Comodidad / accesibilidad |

---

## 5. Mapa de archivos del proyecto

### Carpetas principales (`Assets/NanoLab/`)

| Carpeta | Contenido |
|---|---|
| `Scenes/` | `Microbiologia.unity` (la única escena del juego) y su luz horneada |
| `Scripts/Microbiologia/` | Todo lo general del laboratorio (jugador, manos, microscopio, avisos, puertas, menú, partida…) |
| `Scripts/Centro/` | Centro de Experimentación: el **CASO 01** (vacuna) y el caso antiguo de bacterias (oculto) |
| `Editor/` | Herramientas del menú `NanoLab` (montajes de la escena, pruebas automáticas, compilar APK) |
| `Resources/NanoLab/` | Fuente Nunito (`Fuentes/`) e iconos del mando (`Iconos/`: gatillo, grip, botón, rayo, pulsar, siguiente, repetir) |
| `Modelos/`, `Texturas/`, `Materiales/`, `Prefabs/`, `Audio/`, `Shaders/` | Recursos 3D, audio y shaders (Git LFS) |
| `Muestras/`, `Retos/` | Datos de muestras del microscopio y del caso antiguo |
| `Fuentes/` | Atkinson Hyperlegible y licencias OFL (incluida `OFL_Nunito.txt`) |
| `CREDITOS.md` | Créditos y licencias de todos los assets |

### Scripts clave

**Generales (`Scripts/Microbiologia/`)**

| Script | Qué hace |
|---|---|
| `JugadorNanoLab`, `ManosRig`, `PunteroMuestras` | Jugador, manos y rayo de la mano vacía (`Apuntable` es la base de todo lo que se activa con el rayo) |
| `AdaptadorEscena` | Ajustes al empezar: letra Nunito, quita la química, monta puerta y compuerta si faltan, tableros, quita los paneles celestes de la sala y **llama a `MontajeVacuna.Asegurar`** |
| `EstiloNanoLab` | Letra Nunito, iconos del mando y botones poke de Meta creados por código |
| `BotonFisico` | Botón de las estaciones (poke de Meta + rayo), con hundimiento, halo, LED y sonido |
| `AvisoInicio`, `Aviso` | Aviso de bienvenida/tutorial y mensajes flotantes |
| `MicroscopioOptico`, `Platina`, `Portaobjetos`, `FuenteMuestra`, `CampoMicroscopio`, `HUDMicroscopio` | Microscopio jugable |
| `EstacionAnalisis3D`, `VisorVirus`, `ModeloVirus` | Estación de análisis 3D con los modelos de virus |
| `CabinaDescontaminacion`, `CamaraDescontaminacion` | Esclusa de la BSL-3 (dos puertas y desinfección en 4 fases) |
| `Compartimento`, `Hueco`, `Guardable` | Cajones, vitrinas y huecos donde se guardan objetos |
| `PuertaAutomatica`, `PuertaCorrediza` | Puerta del Centro |
| `Partida`, `IndicadorRecuerdos` (`RecuerdosBaymax`) | Guardado, checkpoint y contador de recuerdos (hoy `Total = 1`) |
| `MenuAjustes`, `BotonMenu`, `AjustesNanoLab` | Menú ☰ (volumen, modo sentado, checkpoint) |

**CASO 01 · Vacuna (`Scripts/Centro/`)**

| Script | Qué hace |
|---|---|
| `MontajeVacuna` | Construye la estación en tiempo de ejecución (mostrador, pantallas, mesas, botones, almacenes, holograma) |
| `RetoVacuna` | Lógica de los 9 pasos, plan de acciones (`SubPaso`), PASO A PASO guiado (`PlanGuiado`), métricas y la prueba automática (`Simular`, `Acertar`, `Fallar`) |
| `ContenidoVacuna` | Todos los textos educativos y datos (opciones, pistas, preguntas de eficacia) |
| `InteraccionVacuna` | Piezas agarrables, pipeta, zonas de encaje, selectores y `AlmacenFrio` (puertas) |
| `GuiaVacuna` | Apartado AHORA, animación de AYUDA/PASO A PASO (`DemoAyuda`) y virus holográfico |
| `VisualesVacuna`, `DiagramasVacuna` | Virus, placa de neutralización, fases, gráfica de eficacia y diagramas de la pantalla derecha |
| `KitVacuna` | Utilidades de construcción (piezas, textos, paneles, materiales) |

**Caso antiguo de bacterias (oculto, reutilizable):** `RetoManager`, `RetoData`, `GuiaReto`, `EstacionGuia`,
`EstacionFicha`, `TerminalHipotesis`, `PrensaCapsulas`, `CamaraExperimental`, `PanelResumen`…

### Herramientas del Editor (`Editor/`)

`MontarTodo` / `MontarLaboratorio` / `MontarMicrobiologia` / `MontarCentroExperimentacion` / `MontarSalaAltoRiesgo`
(construyen la escena), `PruebaVacuna` y `PruebaReto` (pruebas automáticas), `CompilarAPK`, `ConfigurarMetaISDK`,
`ConvertirBaymax*` (avatar), `CapturasE3`, `VistaCenital`, `LimpiarProgreso`.

### Documentación (`Docs/`)

`E1_alcance_y_trazabilidad.md`, `E2_avance.md`, `E3_avance.md` (entregas del curso), `Reto_Vacuna.md` (CASO 01),
`Simulador_PC.md` (probar sin visor) y este documento.

---

## 6. Lenguaje de interacción común (criterios IHC)

Todo caso nuevo debe sentirse igual que el CASO 01: **lo que se aprende una vez sirve en todo el laboratorio**.

| Principio | Cómo se aplica |
|---|---|
| **Consistencia** | Los mismos gestos (grip = agarrar, gatillo = pipeta, rayo + gatillo = elegir, poke = pulsar) y los mismos botones en todas las estaciones: **AYUDA** (amarillo), **PASO A PASO** (violeta), **CONTINUAR** (verde solo cuando el paso está resuelto) |
| **Etiquetas que dicen lo que pasa** | Nada de "Continuar" genérico: "Ya lo veo bien", "Siguiente", "Empezar a jugar", "Siguiente paso" |
| **Una meta visible a la vez** | Franja de luz en la mesa del paso actual y apartado **AHORA** con una sola acción |
| **Mostrar, no hacer leer** | Iconos del mando y animaciones (copia fantasma del objeto) en lugar de párrafos |
| **Control y libertad** | Botón "Atrás" en los tutoriales, reintentos ilimitados, REPETIR al final |
| **Errores que enseñan** | Cada error explica por qué y no penaliza; tras dos errores se sugiere AYUDA |
| **Objetos con apoyo físico** | Botones en carcasa, carteles en atril, nada flotando sin soporte (salvo los avisos que acompañan la vista) |
| **Progreso visible** | Barra de pasos, recuerdos recuperados, checkpoint |
| **Accesibilidad** | Letra Nunito grande y contrastada, color siempre acompañado de forma o texto, modo sentado |
| **Medible** | Cada caso registra errores, ayudas y tiempo (`MetricsLogger`) para la evaluación **HEART** |

**Duración:** 10–15 minutos por caso. **Independencia:** cada caso se puede jugar solo, pero juntos cuentan una historia.

---

## 7. Idea central: la historia "El brote"

Una enfermedad desconocida aparece en la ciudad. Baymax debe **descubrir qué la causa, protegerse, analizarla y crear
la respuesta**. Cada espacio del laboratorio es un capítulo y cada capítulo devuelve un **recuerdo científico** (el
contador pasaría de 1 a 8). El recorrido va de lo básico a lo complejo y del espacio abierto al de alto riesgo, como en
un laboratorio real.

| Orden | Espacio | Caso | Qué hace el estudiante | Qué aprende |
|---|---|---|---|---|
| 0 | Recibidor | **Tutorial · Soy Baymax** | Practica agarrar, pulsar y el rayo frente al espejo | Controles, antes de que importen |
| 1 | Sala principal (puestos) | **CASO 03 · ¿Qué hay en la muestra?** | Prepara portaobjetos y compara muestras de pacientes | Uso del microscopio; células y bacterias |
| 2 | Cepario | **CASO 04 · Conoce a los sospechosos** | Siembra, incuba y compara colonias de 3 bacterias | Cultivo y morfología de colonias |
| 3 | Centro · estaciones 1–3 | **CASO 05 · ¿Bacteria o virus?** | Tinción de Gram, microscopía y antibiograma | Gram; antibióticos (no sirven contra virus) |
| 4 | Entrada BSL-3 | **CASO 06 · Zona de alto riesgo** | Pone y quita el EPP en orden, pasa la descontaminación | Bioseguridad y niveles BSL |
| 5 | BSL-3 interior | **CASO 07 · Identifica al virus** | Criotubo del -80 °C, cabina, análisis 3D y comparación con 5 virus | Estructura de virus y criopreservación |
| 6 | Centro · estación de vacunas | **CASO 01 · Crea la vacuna** (hecho) | Fabrica la vacuna de ARNm | Vacunas de ARNm |
| 7 | Centro · estaciones 4, 5 y 8 | **CASO 02 · La variante** | Encuentra la mutación y actualiza la vacuna | Mutaciones, escape inmune, refuerzos |
| 8 | Centro · estaciones 6 y 7 | **CASO 08 · La bacteria que resiste** | Generaciones de bacterias con tratamiento completo o cortado | Resistencia antibiótica |
| 9 | Portal | **Final · Del laboratorio al mundo** | Mapa de la ciudad: decide aislar, vacunar y tratar | Epidemiología (R0, inmunidad de grupo) |

> Los números de caso siguen el orden en que se propusieron (el 02 ya estaba pedido); el orden de juego es el de la tabla.

---

## 8. Fichas de los casos propuestos

Cada ficha indica el espacio, los pasos, la interacción principal, qué se reutiliza y el esfuerzo estimado.

### Tutorial · Soy Baymax (recibidor)

- **Objetivo:** aprender los 4 gestos antes del primer caso.
- **Pasos:** 1) mirarse en el espejo; 2) agarrar un objeto con el grip y dejarlo en su silueta; 3) pulsar un botón poke
  con el dedo; 4) apuntar con el rayo a un cartel y apretar el gatillo. Cada gesto enciende una baliza.
- **Reutiliza:** espejo, balizas de áreas, `BotonFisico`, `Encajable`/`ZonaEncaje`, `DemoAyuda`.
- **Esfuerzo:** bajo. **Impacto:** alto (primera impresión).

### CASO 02 · La variante (Centro: Biblioteca, Terminal, Archivo + estación de vacunas)

- **Historia:** aparece una variante del virus y la vacuna del CASO 01 protege menos.
- **Pasos:**
  1. **Archivo:** fichas de casos; se ve que hay vacunados que enferman.
  2. **Biblioteca:** qué es una mutación (tarjeta corta con animación).
  3. **Terminal:** comparar la secuencia original y la de la variante y tocar la letra que cambió.
  4. **Holograma:** la espiga muestra la zona mutada.
  5. **Estación de vacunas:** cambiar en el molde el bloque "Gen S" por "Gen S variante".
  6. **Neutralización:** el título baja con la vacuna vieja y se recupera con la nueva.
- **Reutiliza:** casi toda la estación de vacunas (molde, placa, holograma, gráficas).
- **Esfuerzo:** medio-bajo.

### CASO 03 · ¿Qué hay en la muestra? (sala principal, 10 puestos)

- **Pasos:** recoger 3 muestras de pacientes, prepararlas, observarlas en distintos puestos y clasificar lo que se ve
  (células humanas, bacterias, levaduras, "nada anormal").
- **Reutiliza:** microscopio, portaobjetos, muestras, `FuenteMuestra`.
- **Esfuerzo:** bajo-medio.

### CASO 04 · Conoce a los sospechosos (cepario)

- **Pasos:** sembrar las 3 bacterias con el asa, incubar y comparar colonias (forma, color, velocidad); usar el
  desinfectante si se deja una placa abierta ("contaminación" visible).
- **Reutiliza:** placas del cepario, incubadora, animaciones de crecimiento de la placa de neutralización.
- **Esfuerzo:** medio.

### CASO 05 · ¿Bacteria o virus? (Centro: Banco de muestras, Tinción, Microscopía, Incubadora)

- **Pasos:** 1) muestra del paciente; 2) **tinción de Gram en orden** (cristal violeta > lugol > alcohol > safranina);
  3) microscopía: forma y color (cocos o bacilos, Gram + o −); 4) **antibiograma**: discos en la placa, incubar y medir
  los halos; 5) elegir el antibiótico.
- **Pregunta clave:** ¿por qué un antibiótico no sirve contra un virus?
- **Reutiliza:** estaciones 1–3, pipeta y frascos del CASO 01, incubadora.
- **Esfuerzo:** medio.

### CASO 06 · Zona de alto riesgo (entrada de la BSL-3)

- **Pasos:** ponerse el EPP en orden (bata, N95, gafas, doble guante), entrar por la cabina de descontaminación,
  trabajar en la cabina de bioseguridad, llevar residuos a la autoclave y quitarse el EPP en el orden correcto.
- **Feedback:** los errores no penalizan pero se ven (manchas de "contaminación" en las manos).
- **Reutiliza:** sala BSL-3 y cabina ya construidas.
- **Esfuerzo:** bajo-medio.

### CASO 07 · Identifica al virus (interior de la BSL-3)

- **Pasos:** sacar el criotubo del ultracongelador a -80 °C (puerta animada como en el CASO 01), descongelarlo en la
  cabina, analizarlo en la estación 3D y compararlo con los 5 virus mediante preguntas sencillas (¿envoltura? ¿ADN o
  ARN? ¿tamaño?); residuos a la autoclave. Final: qué tipo de vacuna encaja (ARNm, inactivada, de vector).
- **Reutiliza:** `EstacionAnalisis3D`, modelos de virus, rack de criotubos, `AlmacenFrio`.
- **Esfuerzo:** bajo-medio.

### CASO 08 · La bacteria que resiste (Centro: Prensa y Cámara experimental)

- **Pasos:** cultivo en la cámara experimental durante varias "generaciones"; comparar tratamiento completo frente a
  tratamiento cortado a mitad (sobreviven las resistentes y la placa se llena).
- **Reutiliza:** `CamaraExperimental`, `PrensaCapsulas` y la compuerta (del caso antiguo, hoy ocultos).
- **Esfuerzo:** medio.

### Final · Del laboratorio al mundo (portal)

- **Pasos:** cruzar el portal a una sala con el mapa de la ciudad; con preguntas del estilo de la eficacia decidir
  aislamiento, vacunación y tratamiento y ver cómo la curva del brote se aplana. Conceptos: R0 e inmunidad de grupo.
- **Reutiliza:** portal, gráficas tipo eficacia, `Partida` (cierre de la campaña).
- **Esfuerzo:** medio.

---

## 9. Otros usos de cada espacio

| Espacio | Ideas además de los casos |
|---|---|
| **Recibidor** | El **espejo** muestra en Baymax los recuerdos recuperados (insignias en el pecho). Las **5 balizas** se iluminan si el área tiene un caso pendiente y en verde cuando está completa: un mapa vivo |
| **Sala principal** | **Práctica libre** sin presión; cada puesto con una muestra distinta (sangre, saliva, agua del río, yogur) y una ficha breve; en clase, un puesto por estudiante |
| **Cepario** | **Catálogo vivo**: tocar una placa explica la bacteria (dónde vive, si es buena o mala) |
| **Sala BSL-3** | La zona de criopreservación como **biblioteca de virus** (cada criotubo desbloqueado abre su modelo 3D); la esclusa de salida **resume lo hecho** durante sus 11 s |
| **Centro** | **Recepción** como tablero de casos (cuáles hay, cuáles están hechos y dónde); equipamiento compartido con el mismo gesto en todos los casos; incubadora de CO₂ para cultivos celulares |
| **Portal** | Además del final, un **laboratorio de repaso** con preguntas rápidas de todos los casos (modo examen) |
| **Sillas** | **Modo sentado** en los casos de pantalla (Biblioteca, Terminal, Archivo, mapa final) por accesibilidad |

---

## 10. Cómo se construye un caso nuevo (guía técnica)

El CASO 01 sirve de plantilla. Patrón recomendado (sin hornear nada en la escena):

| Pieza | Equivalente en el CASO 01 | Qué contiene |
|---|---|---|
| `ContenidoCasoXX` | `ContenidoVacuna` | Textos, opciones, pistas y preguntas (todo el contenido educativo en un solo archivo) |
| `MontajeCasoXX` | `MontajeVacuna` | Construye la estación en tiempo de ejecución con `KitVacuna` y `EstiloNanoLab` (paneles, textos, botones poke con carcasa, piezas agarrables) |
| `RetoCasoXX` | `RetoVacuna` | Máquina de estados con un `enum` de etapas, `Poner(etapa)`, `Plan()` con `SubPaso` (texto, objetivo, hecho, icono, gesto y frase sencilla), `PlanGuiado()` para PASO A PASO, `Fallo/Registrar` y métricas |
| Visuales | `VisualesVacuna`, `DiagramasVacuna` | Lo que se ve en la pantalla de visualización |
| Interacción | `InteraccionVacuna`, `GuiaVacuna` | Reutilizar `PiezaVacuna`, `ZonaEncaje`, `PipetaVacuna`, `SelectorVacuna`, `AlmacenFrio`, `DemoAyuda` y `VentanaGuia` (conviene renombrarlos a genéricos cuando haya un segundo caso) |
| Prueba automática | `RetoVacuna.Simular/Acertar/Fallar` + `Editor/PruebaVacuna` | Un error típico y la solución de cada paso; informe en `Logs/` y capturas |

**Integración:**

1. Llamar al montaje desde `AdaptadorEscena.Start` (como `MontajeVacuna.Asegurar`).
2. Subir `RecuerdosBaymax.Total` en `IndicadorRecuerdos.cs` y llamar a `RecuerdosBaymax.Recuperar(codigo)` al terminar.
3. Guardar checkpoint con `Partida.GuardarCheckpoint(codigo)`.
4. Documentar el caso en `Docs/` (como `Reto_Vacuna.md`) y en el README.

**Reglas de texto:** la fuente Nunito no tiene ●, →, ▶ ni ✓ (ni letras griegas como Ψ); usar "›", ">", "[OK]".

---

## 11. Hoja de ruta sugerida

Ordenada por coste/beneficio (aprovecha lo que ya existe):

1. **Tutorial del recibidor** — bajo esfuerzo, mejora la primera impresión y reduce la carga en el CASO 01.
2. **CASO 02 · La variante** — reutiliza la estación de vacunas; continuación natural de la historia.
3. **CASO 06 · Zona de alto riesgo** — la sala BSL-3 y la cabina ya están construidas.
4. **CASO 07 · Identifica al virus** — usa la estación 3D y el `AlmacenFrio`.
5. **CASO 05 · ¿Bacteria o virus?** — reactiva las estaciones 1–3 del Centro.
6. **CASO 03 y 04** — microscopía y cepario.
7. **CASO 08 y Final** — cierran la campaña.

Cada caso: plantear primero (objetivo, pasos, criterios IHC) y validar con el profesor/grupo antes de programar.

---

## 12. Pendientes y avisos conocidos

- **Probar en el visor o en el Meta XR Simulator** lo que las capturas no muestran: hundimiento de los botones dentro de
  la carcasa, ritmo de la animación de PASO A PASO y apertura de las puertas al acercar el vial con la mano.
- **Aviso de inicio:** en una prueba simulada (editor con ventana), la primera pulsación desde la bienvenida pareció
  avanzar dos tarjetas. El código no lo explica; se sospecha de la simulación de manos. Hay un diagnóstico preparado
  (solo en batch) por si se quiere confirmar.
- **Botón CONTINUAR de la estación:** por coherencia con el aviso de inicio, valorar renombrarlo a "SIGUIENTE PASO".
- **Repositorio del grupo sin cuota LFS:** no se pueden descargar 16 lightmaps antiguos de su historial; el estado
  actual está completo en `NanoLab_v2`.
- **Capturas en modo batch:** los botones poke salen casi transparentes (artefacto del modo batch, no del juego).
- **Rama `escena-10-24`:** respaldo de cambios de escena sin dueño claro; no es necesaria para que el juego funcione.
