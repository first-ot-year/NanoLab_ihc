# NanoLab VR

**Laboratorio de experimentación microbiológica en realidad virtual**

Proyecto del curso **Interacción Humano Computador** — UTEC, semestre 2026-2.
Profesor: Teófilo Chambilla.

| Integrantes |
|---|
| Carlos Angel |
| Luis Romero |
| Carlos Villegas |

> NanoLab VR es un **complemento educativo**. Las muestras, intervenciones y resultados están simplificados y **no** representan protocolos clínicos, químicos ni microbiológicos reales. No sustituye la práctica en un laboratorio físico.

---

## Índice

1. [Problema](#problema)
2. [La experiencia](#la-experiencia)
3. [Audiencia y persona](#audiencia-y-persona)
4. [Requisitos](#requisitos)
5. [Framework de interacción](#framework-de-interacción)
6. [Stack técnico](#stack-técnico)
7. [Estructura del repositorio](#estructura-del-repositorio)
8. [Cómo abrir y ejecutar el proyecto](#cómo-abrir-y-ejecutar-el-proyecto)
9. [Arquitectura del código](#arquitectura-del-código)
10. [Assets y licencias](#assets-y-licencias)
11. [Evaluación (HEART)](#evaluación-heart)
12. [Cronograma](#cronograma)
13. [Referencias](#referencias)

---

## Problema

Gran parte del objeto de estudio de la microbiología no puede observarse a simple vista. Comprender la forma, estructura y comportamiento de los microorganismos exige microscopios, muestras, tiempo de laboratorio y acompañamiento docente. Estas condiciones limitan la posibilidad de explorar, repetir observaciones y ensayar decisiones, por lo que muchos conceptos quedan abstractos o desconectados de la experimentación.

## La experiencia

El estudiante **es Baymax** (versión infantil): ha perdido sus recuerdos científicos y los recupera a medida que investiga muestras en el laboratorio. Baymax no es un guía externo: es el avatar del jugador (manos en primera persona, cuerpo completo en los espejos). Las pistas y el feedback orientan, pero **no resuelven los retos**.

El reto (10–15 minutos) es **CASO 01 · Crea la vacuna**, el único del Centro de Experimentación, y sigue el ciclo:

```text
EXPLORAR → OBSERVAR → INVESTIGAR → HIPÓTESIS → PREPARAR → EXPERIMENTAR → ANALIZAR → APRENDER
```

1. **Introducción narrativa:** al aparecer, una ventana pequeña paso a paso (SIGUIENTE / ATRÁS / SALTAR) explica quién es Baymax, que perdiste tus recuerdos, el objetivo de NanoLab VR, el ciclo de investigación y los controles; te invita a mirarte en el espejo y te orienta hasta el banco de muestras. Al cerrarla queda una insignia con los recuerdos recuperados y un botón para volver a verla.
2. **Exploración y elección:** el estudiante reconoce los recursos y elige un caso en el banco de muestras.
3. **Primera observación:** coloca la muestra en el microscopio y registra el estado inicial.
4. **Investigación e hipótesis:** consulta una ficha digital y elige una hipótesis predefinida.
5. **Experimentación:** prepara una intervención predefinida, la coloca en la cámara experimental y activa la prueba con un botón.
6. **Verificación:** vuelve al microscopio y compara el estado inicial con el posterior.
7. **Recuerdo recuperado:** si la interpretación es correcta, Baymax recupera un recuerdo científico (indicador 0/3); si falla, recibe una pista y puede reintentar sin penalización.
8. **Cierre:** resumen de lo observado, las decisiones tomadas y lo aprendido.

**CASO 01 · Crea la vacuna** (*Laboratorio de vacunas*, en el centro de la sala; es el **único reto**: el anterior de bacterias queda oculto al empezar, sin borrarse de la escena): el estudiante fabrica con sus manos una vacuna de ARNm contra el SARS-CoV-2 en 9 pasos, en un mostrador con 4 mesas (antígeno → diseño del ARNm con bloques → síntesis con pipeta y termobloque → nanopartículas lipídicas en el mezclador → control de calidad y cadena de frío → ensayo de neutralización en la placa → título → fases clínicas → eficacia), con un tablero-guía de cómo se crea una vacuna de ARNm y otro de conceptos clave. Detalle completo en [Docs/Reto_Vacuna.md](Docs/Reto_Vacuna.md).

## Audiencia y persona

- **Audiencia principal:** estudiantes de 4.º y 5.º de secundaria y universitarios de carreras afines (microbiología, biología, biotecnología, química).
- **Audiencia secundaria:** docentes y asistentes de laboratorio.

**Roles de uso:** estudiante–experimentador, docente–facilitador y asistente–operador.

**Persona principal — Sebastián, “aprendo experimentando”:** 15 años, 5.º de secundaria. Aprende explorando, observando, decidiendo y comprobando consecuencias; se frustra con actividades pasivas. Poca o ninguna experiencia previa en VR.

**Persona secundaria — Angie:** universitaria de 5.º ciclo en ciencias biológicas. Busca retos breves y repetibles para repasar cuando no tiene acceso inmediato al laboratorio.

## Requisitos

Priorización MoSCoW: **Must** (imprescindible para el MVP), **Should** (importante pero reducible), **Could** (ampliación).

### Funcionales

| ID | Requisito | Prioridad |
|---|---|---|
| RF-01 | Introducción y orientación narrativa (subtitulada) | Must |
| RF-02 | Banco y selección de muestras | Must |
| RF-03 | Microscopía inicial | Must |
| RF-04 | Investigación guiada (ficha digital) | Must |
| RF-05 | Hipótesis y estrategia predefinidas | Must |
| RF-06 | Preparación experimental | Must |
| RF-07 | Cámara de experimentación con activación por botón | Must |
| RF-08 | Microscopía posterior y comparación antes/después | Must |
| RF-09 | Feedback, progreso y reintento | Must |
| RF-10 | Registro de métricas y nuevos retos | Could |

### No funcionales

| ID | Categoría | Criterio | Prioridad |
|---|---|---|---|
| RNF-01 | Confort | Estación fija, solo giros y desplazamientos laterales cortos, sin aceleraciones artificiales | Must |
| RNF-02 | Tiempo de respuesta | < 100 ms en agarre, selección, ajuste y activación | Must |
| RNF-03 | Claridad visual | Texto legible, alto contraste, color acompañado de forma/icono/etiqueta | Must |
| RNF-04 | Tolerancia al error | Zonas de interacción amplias, objetos recuperables, acciones reversibles | Must |
| RNF-05 | Consistencia | Misma codificación de muestras, herramientas, estados y controles | Must |
| RNF-06 | Seguridad pedagógica | Aviso visible de que el contenido está simplificado | Must |
| RNF-07 | Accesibilidad básica | Control de volumen y modo sentado | Should |
| RNF-08 | Rendimiento | 72 FPS estables en el visor | Must |

## Framework de interacción

Cuatro capas integradas en el propio laboratorio (interfaz diegética):

- **Orientar:** una meta visible a la vez e instrucciones donde se necesitan, para reducir la carga cognitiva.
- **Manipular:** mapping natural (tomar una placa, ponerla en la platina, pulsar un botón físico).
- **Comprender el estado:** feedback redundante en al menos dos canales (color + forma, sonido + texto, vibración).
- **Reflexionar:** comparación antes/después en el microscopio y resumen final de decisiones e intentos.

**Dirección visual:** laboratorio claro (blanco, gris, acero) con acentos funcionales (magenta, cian, violeta, naranja) siempre acompañados de iconos o etiquetas. La vista microscópica usa una paleta azul oscura para marcar la transición entre explorar y analizar.

## Stack técnico

| Componente | Versión / herramienta |
|---|---|
| Motor | Unity **2022.3.62f3** (LTS) |
| Lenguaje | C# |
| VR | Meta XR SDK (`com.meta.xr.sdk.all` 201.0.0) + Meta Interaction SDK |
| Plugin XR | Oculus XR Plugin (`com.unity.xr.oculus` 4.5.5) |
| Visor objetivo | Meta Quest |
| Texto | TextMesh Pro |
| Modelado | Blender (adaptación de escala, pivotes y colisiones) |
| Optimización | Mallas simplificadas, iluminación horneada |

## Estructura del repositorio

```text
NanoLab_ihc/
├── Assets/
│   ├── NanoLab/                     # Código y contenido propio del proyecto
│   │   ├── Scenes/
│   │   │   └── Microbiologia.unity  # Sala de Microbiología + Centro de Experimentación (única escena)
│   │   ├── Scripts/
│   │   │   ├── Centro/              # Lógica del reto (RetoManager, cámara, prensa, fichas…)
│   │   │   └── Microbiologia/       # Microscopio, portaobjetos, HUD, avatar Baymax, jugador…
│   │   ├── Editor/                  # Herramientas de montaje automático (menú NanoLab)
│   │   ├── Prefabs/
│   │   ├── Materiales/
│   │   └── CREDITOS.md              # Atribuciones y licencias de terceros
│   ├── Descargas/                   # Modelos fuente: Baymax y virus (NIH 3D)
│   ├── stereo_microscope/           # Modelo Sketchfab (CC BY 4.0)
│   ├── microbial_culture_petri_dish_-_agar_plate/
│   ├── petri_dish_collection__by_3dlabware/
│   ├── 3D Laboratory Environment with Appratus/
│   ├── FreeLabAssets/
│   └── Oculus/, XR/, Plugins/, Resources/, TextMesh Pro/
├── Packages/                        # manifest.json y packages-lock.json
└── ProjectSettings/
```

## Cómo abrir y ejecutar el proyecto

### Requisitos previos

- Unity Hub con **Unity 2022.3.62f3** y el módulo **Android Build Support** (OpenJDK + SDK/NDK).
- **Git LFS** instalado (los `.fbx`, `.obj` y `.blend` se versionan con LFS).
- Un visor **Meta Quest** con modo desarrollador activado (opcional para probar en el Editor).

### Clonar

```bash
git lfs install
git clone https://github.com/first-ot-year/NanoLab_ihc.git
cd NanoLab_ihc
git lfs pull
```

### Abrir

1. En Unity Hub: **Add → Add project from disk** y seleccionar la carpeta clonada.
2. Esperar la importación inicial (puede tardar varios minutos).
3. Abrir `Assets/NanoLab/Scenes/Microbiologia.unity` (la sala de Química y su portal se quitaron).

### Herramientas del menú `NanoLab`

El proyecto incluye scripts de Editor que regeneran las escenas desde los assets base:

| Menú | Acción |
|---|---|
| `NanoLab/Montar TODO (Microbiología)` | Convierte modelos, crea fichas de muestras y regenera la sala (sobrescribe cambios manuales y vuelve a hornear la luz) |
| `NanoLab/Aplicar E4 (terminal de hipótesis y estrategia)` | Sobre la escena guardada y sin hornear: rehace la terminal (5) con las dos filas de botones, el tablero de agentes de la prensa (con la estrategia de cada agente) y el reto `Caso01` (estrategias y pistas). No toca el resto de Microbiología |
| `NanoLab/Aplicar novedades (puertas, compuerta, colisiones, panel ☰)` | Sobre la escena guardada y sin hornear: pone la puerta corredera automática del Centro, la compuerta de la cámara experimental y la ventana de introducción paso a paso; rehace el panel ☰ (con el checkpoint) y los bloqueos de los muebles |
| `NanoLab/Reconstruir solo el centro de experimentación` | Regenera únicamente el Centro de Experimentación |
| `NanoLab/Montar cámara de descontaminación (BSL-3)` | Cambia la cabina de la puerta del BSL-3 por una esclusa con dos puertas correderas y ciclo de descontaminación (niebla, luz UV-C, extracción), sin volver a hornear la luz |
| `NanoLab/Probar reto completo (play mode)` | Prueba del reto anterior de bacterias (hoy oculto: ya no aplica mientras `MontajeVacuna.OcultarRetoAnterior` esté activo) |
| `NanoLab/Probar reto de vacuna (play mode)` | Ejecuta el Caso 01 (crea la vacuna) de extremo a extremo sin visor, con las mismas acciones del estudiante y un error típico por paso: informe en `Logs/prueba_vacuna.txt` y capturas `Logs/Capturas/vacuna_*.png`. No recarga la escena si ya está abierta |
| `NanoLab/Configurar interacción Meta (…)` | Configura los componentes del Meta Interaction SDK en las salas |
| `NanoLab/Avatar Baymax/…` | Convierte e instala el modelo de Baymax |
| `NanoLab/Montar introducción Baymax` | Monta la ventana de introducción paso a paso (`VentanaIntro`), la insignia de recuerdos y las balizas, y ordena las escenas de build |
| `NanoLab/Probar locomoción (play mode)` · `NanoLab/Probar recorrido del jugador` | Pruebas de confort (velocidad constante, giro por pasos) y de rutas transitables |
| `NanoLab/Reemplazar sillas (las dos salas)` | Cambia las sillas decorativas del paquete por sillas en las que sentarse (sin volver a hornear) |
| `NanoLab/Asegurar colisiones (las dos salas)` | Armarios, mesas, estanterías y objetos sin collider bloquean al jugador (capa `BloqueoJugador`, que no choca con los objetos agarrables ni tapa el rayo). Los taburetes no bloquean, para poder usar el microscopio de pie |
| `NanoLab/Capturas del E3 (sillas, ajustes, fichas)` | Capturas de la vista sentado, el panel de ajustes y la ficha del virus |

### Probar sin visor

En Play Mode, desde el teclado:

- **F9 / F10:** recorridos del reto anterior de bacterias (oculto: no aplican).
- **F7 / F6 (Caso 01, vacuna):** resuelve el paso actual / comete el error típico del paso.

### Compilar para Meta Quest

1. **File → Build Settings → Android → Switch Platform.**
2. Verificar que `Microbiologia.unity` sea la **única** escena de la lista. `NanoLab/Montar introducción Baymax` la deja así.
3. Conectar el visor por USB y usar **Build And Run**.

## Arquitectura del código

El reto se coordina desde `RetoManager`, que avanza por las etapas `Exploracion → Tincion → Observacion → Investigacion → Hipotesis → Preparacion → Experimento → Analisis → Cierre`, muestra una meta a la vez y registra evidencias, decisiones, intentos y pistas sin resolver nada por el usuario.

| Script | Responsabilidad |
|---|---|
| `RetoManager` | Flujo del reto, objetivo actual, pistas y evidencias |
| `RetoData` / `MuestraData` | Datos configurables del caso y de cada muestra (sin tocar código) |
| `PlacaCaso` | Placa de Petri cuyas colonias cambian tras la intervención |
| `MicroscopioOptico` / `HUDMicroscopio` / `Platina` | Observación ampliada antes y después |
| `EstacionFicha` | Fichas de investigación consultables (RF-04): 3 bacterias, el virus (micrografía electrónica) y la pared celular; se abren con el botón, el rayo o tocándolas |
| `TerminalHipotesis` | Terminal (5) con dos decisiones (RF-05): 1) qué microorganismo es y 2) qué estructura atacar (pared, membrana externa, replicación viral o control). Solo acepta la hipótesis con las evidencias completas y tras consultar una ficha; la pantalla resume las evidencias. Botones por toque o rayo |
| `VialAgente` / `PrensaCapsulas` / `Capsula` | Preparación de la intervención (RF-06): la pantalla de la prensa muestra la hipótesis y la estrategia y dice si el vial encajado COINCIDE con ella; si no coincide, FORMULAR avisa y hay que pulsarlo otra vez para probarlo igual |
| `CamaraExperimental` | Activación de la prueba con feedback multicanal (RF-07); compuerta de guillotina que se abre para que pase la bandeja y sella la cámara durante la incubación |
| `PanelResumen` | Informe final, conocimiento recuperado y repetición del caso |
| `RetoVacuna` / `ContenidoVacuna` / `MontajeVacuna` / `InteraccionVacuna` / `DiagramasVacuna` / `VisualesVacuna` / `KitVacuna` | CASO 01 · *Laboratorio de vacunas*, único reto: al empezar oculta el reto anterior de bacterias (`OcultarRetoAnterior`) y se monta en el centro de la sala (sin hornear); mostrador con 4 mesas y piezas agarrables creadas por código (Meta Interaction SDK inyectado en tiempo de ejecución), pipeta, termobloque, mezclador, analizador y almacenes; lógica de los 9 pasos, textos educativos y diagramas ([Docs/Reto_Vacuna.md](Docs/Reto_Vacuna.md)). El mobiliario del Centro (`Estaciones`) se mantiene; solo se ocultan los carteles del caso viejo. Botones **poke** de Meta (A–E, AYUDA, CONTINUAR verde al resolver, REPETIR, máquinas y **PASO A PASO**, que repite sin parar la animación de la acción actual con frases sencillas); ultracongelador y nevera con **puerta animada** (`AlmacenFrio`); **EFICACIA** con 4 preguntas sencillas sobre las barras; **virus holográfico** en el mostrador para el paso ANTÍGENO (se toca con la mano o el rayo y se gira con el grip) |
| `GuiaVacuna` | Apartado fijo **AHORA** en la estación (PASO n/9 · acción actual con el icono del mando), animación guiada de **AYUDA** y **PASO A PASO** (copia fantasma del objeto, "COGE ESTO > PONLO AQUÍ") y `VirusHolograma` (tocar y girar el virus) |
| `EstiloNanoLab` | Letra **Nunito SemiBold** para todos los textos 3D, iconos del mando (gatillo, grip, botón, rayo, pulsar, siguiente, repetir) y botones poke de Meta creados por código a partir de un botón de la escena |
| `Partida` / `GestorPartida` | Partida guardada en el visor (autoguardado) y **checkpoint** al terminar cada reto: el botón VOLVER AL CHECKPOINT del panel ☰ restaura ese avance y lleva al jugador a ese punto. Cada guardado deja la versión anterior en `partida.json.bak`: si `partida.json` se daña, se aparta y se carga el respaldo. Las pruebas usan `Logs/partida_prueba.json` y el menú `NanoLab/Partida` borra o abre la partida del Editor |
| `GuiaReto` | Guía del reto en el Centro: tarjeta de objetivo (caso, paso n de 9, qué hacer y a qué estación ir) que aparece al entrar, en cada paso y al llegar, y con **Y**; flechas en el suelo que fluyen hasta la estación y un anillo con su número y nombre. Sustituye al panel de la mano (`ObjetivoMuneca`) y a la baliza |
| `Compartimento` | Cajones, vitrinas y tapas: se abren con el rayo (gatillo o A/X) y los cajones también se **jalan con la mano** (grip junto al frente): siguen a la mano y se quedan donde se sueltan |
| `PuertaAutomatica` / `PuertaCorrediza` | Puerta corredera de dos hojas en la entrada del Centro de Experimentación: se abre al acercarse (o al llegar con un objeto en la mano) y se esconde en el muro; franja verde encima cuando está abierta |
| `AdaptadorEscena` | Al empezar (lo añade `JugadorNanoLab`): quita la cristalería de química y el portal (pone placas de Petri en los huecos), monta la puerta del Centro y la compuerta de la cámara si la escena guardada no las tiene, pone Nunito en todos los textos, deja los tableros azules con borde fino (3 mm) y el texto contorneado, y quita los tableros informativos sueltos de la sala principal (rótulos de áreas y tótems de la bienvenida) manteniendo los paneles de los equipos (autoclave, portal, incubadora…); convierte en interactivos los cajones de todas las mesas que el montaje no convirtió. Los rótulos de las balizas numeradas de las áreas se mantienen (orientación, RF-01) |
| `CabinaDescontaminacion` | Esclusa con dos puertas de vidrio (la exterior se crea al empezar si falta): nunca abiertas a la vez; al quedarte dentro se cierran las dos y hay una desinfección completa de 11 s en 4 fases (ducha de aire, nebulización, UV-C, extracción) con una pantalla fija en cada pared lateral (fase, explicación, barra, % y segundos); al terminar se abre la otra puerta. Igual al salir de la BSL-3 |
| `AvisoInicio` | Aviso cuadrado delante de la vista: al iniciar la partida (aviso de casco + tutorial corto de 6 tarjetas) y cada vez que se pone el visor; botones poke de Meta con etiquetas que dicen lo que pasa («Ya lo veo bien ›», «Siguiente ›», «Empezar a jugar») y «‹ Atrás» para volver (criterio IHC: acción clara y control del usuario); atajos X/A = siguiente, Y/B = atrás. Sustituye a la ventana grande de introducción y al tótem del tutorial (`VentanaIntro` queda solo como insignia que reabre el tutorial) |
| `Encajable` / `ZonaEncaje` | Objetos agarrables con encaje y retorno automático (RNF-04) |
| `MetricsLogger` | Registro JSON de la sesión para HEART (RF-10) |
| `AvatarBaymaxBebe` / `ConvertirBaymaxBebe` (Editor) | Baymax Bebé v4.2 (modelo con esqueleto hecho en Blender por un compañero) como avatar del jugador: escala a la altura del jugador, cabeza al visor, brazos al mando con IK (codo ≤ 65°), dedos con grip y gatillo, Idle/Walk y piernas dobladas al sentarse. En primera persona solo se ven las manos (malla recortada en la muñeca) |
| `AvatarBaymax` / `EspejoAvatar` | Avatar anterior de piezas rígidas (respaldo si falta el FBX nuevo) y espejos donde Baymax se ve completo |
| `IndicadorRecuerdos` / `RecuerdosBaymax` | Recuerdos científicos recuperados (0/3) |
| `MontarIntroduccion` (Editor) | Tótem de introducción "ERES BAYMAX", balizas de las áreas y orden de escenas de build |
| `BalizaArea` / `PanelRedondeado` | Balizas numeradas sobre cada área del laboratorio (orientación, RF-01) |
| `JugadorNanoLab` / `PunteroMuestras` / `ManosRig` | Locomoción de Meta, agarre con gatillo o grip y rayo corto para tomar muestras y soltar en platinas o mesas |
| `MontarSalaAltoRiesgo` / `EstacionAnalisis3D` / `VisorVirus` | Sala de vidrio BSL-3 con los criotubos de virus y la estación holográfica 3D |
| `Utileria` / `MontarMusica` / `Realismo` (Editor) | Utilería realista (portaobjetos, bandejas, criotubos, frasco lavador), música ambiental y ajustes de realismo |
| `AjustesNanoLab` / `MenuAjustes` / `BotonMenu` / `MontarAjustes` (Editor) | Panel ☰ (botón menú del mando izquierdo): volumen de música y efectos, modo sentado y recalibrar la altura (RNF-07); guardar, nueva partida y volver al checkpoint |
| `Silla` / `MontarSillas` / `MontarSillaLaboratorio` (Editor) | Taburete de laboratorio de Blender (asiento a 0.65 m, aro reposapiés, base de 5 radios) frente a cada microscopio de puesto: se sienta con el rayo y se levanta con el joystick; no bloquean al jugador. Prefab en `Assets/NanoLab/Silla_Laboratorio_Interactuable/Prefabs/` |
| `Apuntable` | Base de lo que se usa con el rayo de la mano vacía (fichas, botones del panel, sillas) |

Las métricas de sesión se guardan como JSON en `Application.persistentDataPath/NanoLab/`.

## Assets y licencias

Detalle completo en [`Assets/NanoLab/CREDITOS.md`](Assets/NanoLab/CREDITOS.md). Resumen:

| ID | Asset | Fuente | Licencia |
|---|---|---|---|
| A1 | Chemistry Lab Items Pack — Animatics Studio | Unity Asset Store | Standard Unity Asset Store EULA |
| A2 | Stereo Microscope — Touati_Fellouh | Sketchfab | CC BY 4.0 |
| A3 | Microbial Culture (petri dish - agar plate) — Ioannis Stavridis | Sketchfab | CC BY 4.0 |
| A4 | Petri Dish Collection — 3DLabWare | Sketchfab | CC BY 4.0 |
| A5 | *Staphylococcus aureus*, tinción de Gram — Dr Graham Beards | Wikimedia Commons | CC BY-SA 4.0 |
| A6 | Virus de la hepatitis B — CDC / Dr. Erskine Palmer | Wikimedia Commons | Dominio público |
| A7 | Símbolo de riesgo biológico — Silsor | Wikimedia Commons | Dominio público |
| A8 | UnityMeshSimplifier — Whinarn | GitHub | MIT |
| — | Baby Baymax — hlaphoneshwe | Sketchfab | CC BY 4.0 |
| — | Modelos de virus (Influenza, VIH, VPH, Hepatitis B, SARS-CoV-2) | NIH 3D / NIAID | CC BY 4.0 |
| — | Free Laboratory Pack — StormBringer Studios | Unity Asset Store | Standard Unity Asset Store EULA |
| — | Meta XR SDK | Meta | Meta Platforms Technologies SDK License |
| — | Nunito (SemiBold) — The Nunito Project Authors | Google Fonts | SIL OFL 1.1 |

Baymax es un personaje de Disney; su uso en este proyecto es exclusivamente académico.

## Evaluación (HEART)

Plan de evaluación (aún no son resultados medidos):

| Dim. | Objetivo | Métrica propuesta |
|---|---|---|
| **H**appiness | Comodidad, claridad, utilidad y motivación | Likert 1–5: ≥ 4 en claridad, utilidad y motivación; ≤ 2 en mareo |
| **E**ngagement | Participación en exploración e investigación | ≥ 3 acciones de indagación deliberadas; < 20 % del tiempo inactivo |
| **A**doption | Adopción rápida de las interacciones | 80 % completa el flujo básico tras ≤ 2 min de orientación |
| **R**etention | Recordar la relación evidencia–resultado | ≥ 80 % relaciona una característica observada con el resultado al cierre |
| **T**ask success | Completar el reto | 80 % completa el MVP en ≤ 15 min, con ≤ 2 ayudas y sin omitir la comparación |

## Cronograma

Según el informe del hito 1 (Cuadro 6):

| Entrega | Semana | Avance | Objetivo | RF | RNF | Estado |
|---|---|---|---|---|---|---|
| E1 | 10 | 15 % | Alcance del MVP, matriz de trazabilidad, criterios de aceptación, backlog y lista de ajustes | RF-02, 03, 07, 08 | RNF-02, 03 | ✅ [Docs/E1](Docs/E1_alcance_y_trazabilidad.md) |
| E2 | 11 | 30 % | Entorno VR, introducción en la que el estudiante es Baymax y selección de una muestra | RF-01 | RNF-01, 04 | ✅ [Docs/E2](Docs/E2_avance.md) (validar en Quest) |
| E3 | 12 | 50 % | Observación microscópica inicial y ficha digital | RF-04 | RNF-05, 07 | ✅ [Docs/E3](Docs/E3_avance.md) (validar en Quest) |
| E4 | 13 | 70 % | Hipótesis, estrategia y preparación de la intervención | RF-05, 06 | RNF-06 | ✅ [Docs/E4](Docs/E4_avance.md) (validar en Quest) |
| E5 | 14 | 85 % | Cámara experimental, feedback, pistas y reintento | RF-09 | RNF-01, 08 | Implementado (falta perfilar 72 FPS) |
| E6 | 15 | 100 % | Integración, piloto, preguntas de comprensión, demostración y video | RF-01…09 | RNF-01…08 | Pendiente |

## Enlaces

- Video del avance en Unity: <https://drive.google.com/file/d/1GUsvIlhsO3Fw2tBtkCMtJUN2xaoJttb5/view?usp=sharing>
- Video experimental: <https://drive.google.com/file/d/1Ij6XtIlsqoqD3I2-UXguvejerJl7QbVk/view?usp=sharing>

## Referencias

1. E. Chitra et al., “A 3-D interactive microbiology laboratory via virtual reality for enhancing practical skills,” *Scientific Reports*, vol. 14, p. 12809, 2024.
2. J. Radianti, T. A. Majchrzak, J. Fromm e I. Wohlgenannt, “A systematic review of immersive virtual reality applications for higher education,” *Computers & Education*, vol. 147, p. 103778, 2020.
3. M. Reist et al., “Incorporating a human parasite virtual reality experience into a tertiary microbiology teaching curriculum increases student engagement and learning outcomes,” *Journal of Microbiology & Biology Education*, vol. 27, 2026.
4. J. Sweller, “Cognitive load during problem solving: Effects on learning,” *Cognitive Science*, vol. 12, no. 2, pp. 257–285, 1988.
5. S. A. W. Andersen et al., “The effect of implementing cognitive load theory-based design principles in virtual reality simulation training of surgical skills,” *Advances in Simulation*, vol. 1, no. 20, 2016.
6. D. Norman, *The Design of Everyday Things: Revised and Expanded Edition*. Basic Books, 2013.
7. J. Nielsen, “Heuristic evaluation,” en *Usability Inspection Methods*. John Wiley & Sons, 1994, pp. 25–62.
8. D. A. Bowman, E. Kruijff, J. J. LaViola Jr. e I. Poupyrev, *3D User Interfaces: Theory and Practice*. Addison-Wesley, 2004.
9. J. Jerald, *The VR Book: Human-Centered Design for Virtual Reality*. ACM Books / Morgan & Claypool, 2016.
10. M. Slater y S. Wilbur, “A framework for immersive virtual environments (FIVE),” *Presence: Teleoperators and Virtual Environments*, vol. 6, no. 6, pp. 603–616, 1997.
