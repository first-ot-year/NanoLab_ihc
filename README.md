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

El estudiante entra a un laboratorio virtual acompañado por **Baymax** (versión infantil), un personaje guía que ha perdido sus recuerdos científicos y los recupera a medida que ambos investigan. Baymax ofrece contexto y pistas, pero **no resuelve los retos**.

El reto MVP (10–15 minutos) sigue el ciclo:

```text
EXPLORAR → OBSERVAR → INVESTIGAR → HIPÓTESIS → PREPARAR → EXPERIMENTAR → ANALIZAR → APRENDER
```

1. **Introducción narrativa:** Baymax explica que perdió sus recuerdos e invita a explorar.
2. **Exploración y elección:** el estudiante reconoce los recursos y elige un caso en el banco de muestras.
3. **Primera observación:** coloca la muestra en el microscopio y registra el estado inicial.
4. **Investigación e hipótesis:** consulta una ficha digital y elige una hipótesis predefinida.
5. **Experimentación:** prepara una intervención predefinida, la coloca en la cámara experimental y activa la prueba con un botón.
6. **Verificación:** vuelve al microscopio y compara el estado inicial con el posterior.
7. **Recuerdo recuperado:** si la interpretación es correcta, Baymax recupera conocimiento; si falla, recibe una pista y puede reintentar sin penalización.
8. **Cierre:** resumen de lo observado, las decisiones tomadas y lo aprendido.

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
│   │   │   ├── Laboratorio.unity    # Sala de Química (laboratorio base)
│   │   │   └── Microbiologia.unity  # Sala de Microbiología + Centro de Experimentación
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
3. Abrir `Assets/NanoLab/Scenes/Microbiologia.unity` o `Assets/NanoLab/Scenes/Laboratorio.unity`.

### Herramientas del menú `NanoLab`

El proyecto incluye scripts de Editor que regeneran las escenas desde los assets base:

| Menú | Acción |
|---|---|
| `NanoLab/Montar TODO (Química + Microbiología)` | Convierte modelos, crea fichas de muestras y regenera ambas salas (sobrescribe cambios manuales y vuelve a hornear la luz) |
| `NanoLab/Reconstruir solo el centro de experimentación` | Regenera únicamente el Centro de Experimentación |
| `NanoLab/Probar reto completo (play mode)` | Ejecuta el Caso 01 de extremo a extremo sin visor |
| `NanoLab/Configurar interacción Meta (…)` | Configura los componentes del Meta Interaction SDK en las salas |
| `NanoLab/Avatar Baymax/…` | Convierte e instala el modelo de Baymax |

### Probar sin visor

En Play Mode, el componente `DebugReto` permite recorrer el Caso 01 desde el teclado:

- **F9:** recorrido con acierto directo.
- **F10:** recorrido que falla primero (prueba pista y reintento).

### Compilar para Meta Quest

1. **File → Build Settings → Android → Switch Platform.**
2. Verificar que `Laboratorio.unity` y `Microbiologia.unity` estén en la lista de escenas.
3. Conectar el visor por USB y usar **Build And Run**.

## Arquitectura del código

El reto se coordina desde `RetoManager`, que avanza por las etapas `Exploracion → Tincion → Observacion → Investigacion → Hipotesis → Preparacion → Experimento → Analisis → Cierre`, muestra una meta a la vez y registra evidencias, decisiones, intentos y pistas sin resolver nada por el usuario.

| Script | Responsabilidad |
|---|---|
| `RetoManager` | Flujo del reto, objetivo actual, pistas y evidencias |
| `RetoData` / `MuestraData` | Datos configurables del caso y de cada muestra (sin tocar código) |
| `PlacaCaso` | Placa de Petri cuyas colonias cambian tras la intervención |
| `MicroscopioOptico` / `HUDMicroscopio` / `Platina` | Observación ampliada antes y después |
| `EstacionFicha` | Fichas de investigación consultables (RF-04) |
| `TerminalHipotesis` | Registro de la hipótesis (RF-05) |
| `VialAgente` / `PrensaCapsulas` / `Capsula` | Preparación de la intervención (RF-06) |
| `CamaraExperimental` | Activación de la prueba con feedback multicanal (RF-07) |
| `PanelResumen` | Informe final, conocimiento recuperado y repetición del caso |
| `Encajable` / `ZonaEncaje` | Objetos agarrables con encaje y retorno automático (RNF-04) |
| `MetricsLogger` | Registro JSON de la sesión para HEART (RF-10) |
| `AvatarBaymax` | Presentación del personaje Baymax |
| `Introduccion` / `BalizaArea` / `PanelRedondeado` | Introducción subtitulada (el estudiante es Baby Baymax), balizas de orientación por áreas y tarjeta de la primera muestra seleccionada (RF-01, E2) |
| `JugadorNanoLab` / `ManosRig` | Configuración del rig de Meta y acceso a manos/mandos |

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

| Entrega | Semana | Avance | Objetivo |
|---|---|---|---|
| E1 | 10 | 15 % | Alcance del MVP, matriz de trazabilidad y backlog priorizado |
| E2 | 11 | 30 % | XR, escala, introducción de Baymax, selección de muestra y microscopía básica |
| E3 | 12 | 50 % | Observación, ficha digital, hipótesis y preparación de la intervención |
| E4 | 13 | 70 % | Cámara experimental, comparación antes/después, pista y reintento |
| E5 | 14 | 100 % | Integración, pruebas de extremo a extremo, 72 FPS y piloto |
| E6 | 15 | — | Demostración, video de respaldo y exposición |

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
