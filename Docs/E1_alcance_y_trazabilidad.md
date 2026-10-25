# E1 (15 %) · Alcance del MVP, trazabilidad, backlog y ajustes

Entregable 1 del cronograma del informe del hito 1 (Cuadro 6, semana 10):

> Cerrar el alcance de un reto MVP; construir la matriz de trazabilidad y los criterios de aceptación para los requisitos seleccionados; revisar el prototipo existente e identificar las pantallas que requieren adaptación microbiológica. **Salida:** backlog priorizado y lista de ajustes.

Requisitos que el informe asocia a E1: RF-02, RF-03, RF-07, RF-08, RNF-02 y RNF-03. La matriz cubre **todos** los requisitos para que sirva de referencia en las entregas E2–E6.

---

## 1. Alcance del reto MVP

**Caso 01 · "La herida que no sana"**. La muestra es *Staphylococcus aureus*, en una placa de agar sangre con colonias doradas.

| Entra en el MVP | Queda fuera (ampliación) |
|---|---|
| Introducción narrativa estática: "Eres Baymax y perdiste tus recuerdos científicos" (RF-01) | Animación corporal de Baymax, voz grabada o secuencias cinemáticas |
| Banco de muestras con el CASO 01 activo y los CASOS 02 y 03 visibles pero bloqueados (RF-02) | Contenido jugable de los casos 02 y 03 |
| Frotis y tinción de Gram en 4 pasos con validación del orden (RF-06 / RF-03) | Ensamblaje libre de cápsulas y lanzamiento físico |
| Microscopio virtual a 400x/1000x con 3 evidencias: forma, agrupación y Gram (RF-03) | Simulación de movimiento o comportamiento microbiano |
| Fichas digitales en la biblioteca (RF-04) | Libros físicos interactivos y escaneo |
| Hipótesis elegida entre 3 opciones en la terminal (RF-05) | Texto libre |
| Elección de 1 de 4 agentes y formulación en la prensa (RF-06) | Dosificación o mezclas reales |
| Cámara experimental activada con un botón (RF-07) | Física de fluidos |
| Comparación ANTES / DESPUÉS en el microscopio (RF-08) | Reconstrucción 3D dinámica de la muestra |
| Pista ligada a la evidencia, reintento sin penalización y recuerdo recuperado (RF-09) | Mural animado de recuerdos |
| Registro JSON de tiempo, decisiones, intentos y pistas (RF-10) | Panel docente o recopilación remota |

Ciclo: **EXPLORAR → OBSERVAR → INVESTIGAR → HIPÓTESIS → PREPARAR → EXPERIMENTAR → ANALIZAR → APRENDER**. Sus etapas en `RetoManager` son: Exploración, Tinción, Observación, Investigación, Hipótesis, Preparación, Experimento, Análisis y Cierre.

---

## 2. Matriz de trazabilidad y criterios de aceptación

**Evidencia:**
- **PR** = comprobación automática de `NanoLab/Probar reto completo` (`Logs/prueba_reto.txt`).
- **PL** = `NanoLab/Probar locomoción` (`Logs/prueba_locomocion.txt`).
- **REC** = `NanoLab/Probar recorrido del jugador`.
- **CAP** = captura en `Logs/Capturas/`.
- **VR** = prueba manual en Meta Quest.

**Estado:**
- ✅ cumplido y verificado.
- 🟡 implementado, falta validarlo en el visor o con usuarios.
- ⬜ pendiente.

### Requisitos funcionales

| ID | Prior. | Implementación | Criterio de aceptación | Evidencia | Estado |
|---|---|---|---|---|---|
| RF-01 Introducción y orientación narrativa | Must | `MontarIntroduccion` (tótem "ERES BAYMAX" frente al punto de aparición), `EspejoAvatar` (espejo del recibidor), `IndicadorRecuerdos`, avatar `AvatarBaymax` | Al iniciar, el jugador ve sin moverse un panel que explica que es Baymax, que perdió sus recuerdos y adónde ir (hasta el banco de muestras). Puede verse como Baymax en el espejo. El indicador muestra 0/3 recuerdos | PR "Introducción con indicador de recuerdos"; CAP `micro_0_intro.png`; CAP `avatar_*_espejo.png` | 🟡 (VR) |
| RF-02 Banco y selección de muestras | Must | Estación 1 `BancoMuestras` (`MontarCentroExperimentacion.Banco`), `PlacaCaso`, `Encajable`/`ZonaEncaje`; en la sala principal, el cepario y la zona de alto riesgo (`FuenteMuestra`) | El banco muestra el CASO 01 activo y los casos 02 y 03 bloqueados con etiqueta. La placa del caso se agarra y, si se cae, vuelve sola a su ranura. Tocarla con un portaobjetos carga la muestra | PR "Frotis del CASO 01 cargado > etapa Tinción"; CAP `centro_3_banco.png` | ✅ |
| RF-03 Microscopía inicial | Must | `MicroscopioOptico`, `Platina`, `HUDMicroscopio` (siempre encima, paleta azul oscura), `RetoManager.EvaluarEvidencias` | Con el portaobjetos teñido en la platina y la cara en el ocular aparece la vista ampliada. A 400x se registra la forma y a 1000x la agrupación. Una tinción errónea no da evidencias | PR (4 comprobaciones de microscopía); CAP `hud_1_*`, `hud_2_*`, `ocular_*.png` | ✅ |
| RF-04 Consulta de información | Must | Estación 4 `Biblioteca` (`EstacionFicha`), micrografías A5/A6 | Al abrir una ficha se registra la consulta y se avanza a Hipótesis. Las fichas muestran forma, agrupación, Gram y una micrografía | PR "Consultar una ficha > etapa Hipótesis"; CAP `centro_6_biblioteca.png` | ✅ |
| RF-05 Hipótesis y estrategia | Must | Estación 5 `TerminalHipotesis` | Hay 3 opciones predefinidas. Se rechazan si faltan evidencias (indicando cuáles) y pueden cambiarse antes de experimentar | PR "La terminal rechaza una hipótesis sin evidencias" y "hipótesis … > etapa Preparación" | ✅ |
| RF-06 Preparación experimental | Must | Estación 2 tinción (`FuenteMuestra` goteros 1–4), estación 6 `PrensaCapsulas` + `VialAgente` + `Capsula` | Se elige 1 de 4 agentes, se encaja en la prensa y se pulsa FORMULAR para obtener la cápsula. El vial vuelve a su sitio | PR "cápsula … formulada" y "el vial vuelve a su sitio" | ✅ |
| RF-07 Cámara de experimentación | Must | Estación 7 `CamaraExperimental` | Con la placa y la cápsula encajadas, el botón ACTIVAR inicia la prueba con feedback visual, sonoro y háptico y entrega el portaobjetos DESPUÉS | PR "la cámara arranca" y "resultado … portaobjetos DESPUÉS entregado"; CAP `camara_intento*.png` | ✅ |
| RF-08 Microscopía posterior y comparación | Must | `HUDMicroscopio.MostrarReto` con vista ANTES \| DESPUÉS | El portaobjetos DESPUÉS en el microscopio muestra ambas vistas, el agente, la acción y el resultado. No se puede formular otra cápsula sin analizar antes | PR "comparación ANTES \| DESPUÉS observada" y "Se registró la comparación"; CAP `hud_3_*.png` | ✅ |
| RF-09 Feedback, progreso y reintento | Must | `RetoManager` (pistas por etapa y pista ligada al fallo), `EstacionGuia` (botón PISTA), `PanelResumen` (archivo de recuerdos), `RecuerdosBaymax`, `Aviso`, `Sonidos` | Un fallo da una pista ligada a la evidencia, devuelve a Hipótesis y regenera la placa. Un éxito cierra el caso, muestra el informe y suma un recuerdo de Baymax (1/3) | PR "Tras un fallo…", "La placa se regenera…" y "Baymax recupera un recuerdo…"; CAP `archivo.png` | ✅ |
| RF-10 Registro y nuevos retos | Could | `MetricsLogger` (JSON en `persistentDataPath/NanoLab/`), `RetoData` (caso configurable sin código) | Se guardan el tiempo, las etapas, las evidencias, las fichas, las hipótesis, los intentos y las pistas | Fichero JSON tras PR | ✅ (casos extra ⬜) |

### Requisitos no funcionales

| ID | Prior. | Implementación | Criterio de aceptación | Evidencia | Estado |
|---|---|---|---|---|---|
| RNF-01 Confort | Must | Locomotor de Meta (`JugadorNanoLab`): 1.5 m/s constante, sin rampa; giro por pasos de 30°; viñeta de confort; sin teletransporte | Velocidad constante (90 % en ≤0.2 s, frenado en ≤0.25 s), giro por pasos y viñeta activa. **Ajuste justificado: caminata libre en vez de estación fija** (ver §4) | PL (velocidad 1.5 m/s, 0.14 s / 0.11 s) | 🟡 (VR, mareo ≤ 2 en el piloto) |
| RNF-02 Tiempo de respuesta < 100 ms | Must | Lógica por eventos en el mismo frame (agarre, encaje, botón, HUD) | Ninguna acción espera más de un frame de lógica. En el visor no se percibe retraso | PR (todas las transiciones son inmediatas) | 🟡 (medir en VR) |
| RNF-03 Claridad visual | Must | Textos TMP de alto contraste sobre fondo oscuro; estaciones con número + color + nombre; resultados con color + palabra (EFICAZ / SIN EFECTO) + icono | Ningún estado depende solo del color. El texto se lee a 1–1.5 m | CAP de estaciones y HUD | 🟡 (VR) |
| RNF-04 Tolerancia al error | Must | Zonas de encaje amplias (`ZonaEncaje`, `Platina`); retorno automático (`Encajable.VolverACasa`, `Portaobjetos.VolverACasa`); desinfectante para rehacer el frotis; reintento sin castigo | Un objeto caído al suelo vuelve a su sitio en ≤3 s. Una tinción errónea se puede limpiar y repetir | PR "Un portaobjetos caído vuelve a su sitio en 1.5 s", "el vial vuelve a su sitio" y "El desinfectante limpia…" | ✅ |
| RNF-05 Consistencia | Must | Misma codificación por zonas (número y color 1–8) en carteles, balizas y objetivo; mismos controles en todas las salas | Cada estación conserva su número y color en el objetivo, la baliza y el cartel | CAP `centro_*.png` | ✅ |
| RNF-06 Seguridad pedagógica | Must | Avisos en la introducción, la recepción del centro, el tablero de agentes de la prensa y el informe final | El aviso de "contenido simplificado, no es un protocolo real" es visible al inicio y al cierre | CAP `micro_0_intro.png`, `archivo.png` | ✅ |
| RNF-07 Accesibilidad básica | Should | — | Control de volumen y modo sentado | — | ⬜ (E3) |
| RNF-08 Rendimiento 72 FPS | Must | Iluminación horneada en la sala principal, primitivas sin sombras, espejos que renderizan cada 2 frames y solo a < 7 m | 72 FPS estables en Quest medidos con OVR Metrics Tool en el recorrido completo | — | ⬜ (E5) |

---

## 3. Backlog priorizado (MoSCoW)

| # | Ítem | Requisito | Prior. | Entrega | Estado |
|---|---|---|---|---|---|
| 1 | Arranque en Microbiología e introducción "Eres Baymax" con espejo e indicador de recuerdos | RF-01 | Must | E2 | ✅ |
| 2 | Selección de la muestra en el banco y retorno automático de objetos | RF-02, RNF-04 | Must | E2 | ✅ |
| 3 | Caminata natural sin aceleración artificial (velocidad constante) | RNF-01 | Must | E2 | ✅ (validar en VR) |
| 4 | Observación microscópica y fichas | RF-03, RF-04 | Must | E3 | ✅ |
| 5 | Control de volumen y modo sentado | RNF-07 | Should | E3 | ⬜ |
| 6 | Hipótesis y preparación de la intervención | RF-05, RF-06 | Must | E4 | ✅ |
| 7 | Cámara, feedback, pistas y reintento | RF-07, RF-09 | Must | E5 | ✅ |
| 8 | Perfilado a 72 FPS en Quest (OVR Metrics Tool) y medición de latencia | RNF-08, RNF-02 | Must | E5 | ⬜ |
| 9 | Pregunta breve de comprensión al cierre (métrica R de HEART) | RF-09 / HEART | Should | E5 | ⬜ |
| 10 | Piloto con usuarios: cuestionario Likert (H), tiempos y ayudas (T) | HEART | Must | E6 | ⬜ |
| 11 | Video de respaldo y demostración | — | Must | E6 | ⬜ |
| 12 | Casos 02 y 03 (contenido de los casos bloqueados) | RF-10 | Could | Post-MVP | ⬜ |
| 13 | Guardar los recuerdos recuperados entre sesiones | RF-09 | Could | Post-MVP | ⬜ |

---

## 4. Lista de ajustes

### 4.1 Pantallas del prototipo (informe, Figs. 2–7) y su adaptación microbiológica

| Pantalla | Problema detectado en el hito 1 | Adaptación realizada |
|---|---|---|
| Fig. 2 · Orientación | Laboratorio genérico con señalética de química | Sala de **Microbiología** propia: se quitaron los aparatos de química y se añadieron señalética de riesgo biológico (A7), zona de alto riesgo (virus), cepario y cabina de desinfección. Química queda como sala secundaria |
| Fig. 3 · Preparación | Panel "Proceso de observación celular" de química | Tótem tutorial de 4 pasos (portaobjetos → muestra → platina → ocular) y estación de **tinción de Gram** con goteros numerados 1–4 |
| Fig. 4 · Exploración (Baymax en 3.ª persona) | Baymax como acompañante externo | El estudiante **es** Baymax: avatar en primera persona (solo manos), cuerpo completo en el espejo y tótem "ERES BAYMAX" |
| Fig. 5 · Microscopía | Escena conceptual | Microscopios en cada puesto, al borde de la mesa. Al acercar la cara se abre el HUD azul oscuro siempre visible |
| Fig. 6 · Análisis (panel de virus) | No conectaba micrografía, rasgos, decisión y comparación | HUD del reto con la micrografía de *S. aureus* (A5), evidencias por aumento, agente aplicado y vista **ANTES \| DESPUÉS** |
| Fig. 7 · Inspección 3D | Riesgo de "enemigo gigante" | Proyector holográfico (`EstacionAnalisis3D`) con rotación, zoom, vistas y corte, aplicado a modelos de virus del NIH. Se etiqueta como modelo, no como lo que ve el microscopio (RNF-06) |

### 4.2 Desviaciones justificadas respecto al informe

| Informe | Decisión | Motivo |
|---|---|---|
| XR Interaction Toolkit o Meta XR SDK | **Meta XR SDK + Interaction SDK** | El visor objetivo es Meta Quest. El SDK ofrece agarre con manos y mandos, locomoción y confort integrados |
| Baymax como modelo estático o paneles | **Avatar del jugador** (Baby Baymax, CC BY 4.0) con espejo | Refuerza la narrativa "eres Baymax" y la presencia. En primera persona solo se ven las manos para no estorbar la manipulación |
| RNF-01 · estación fija | **Caminata libre** con el Locomotor de Meta a velocidad constante (sin rampa), giro por pasos de 30° y viñeta de confort; sin teletransporte | Las salas se ampliaron (sala principal + Centro de 302 m²) y el equipo prefirió caminar de forma continua. Se cumple "sin aceleraciones artificiales" y se mitiga con las técnicas de confort de Meta. Se vigilará el mareo en el piloto (meta ≤ 2) |
| Microscopio estereoscópico (A2) | Se conserva el modelo, pero se usa como **microscopio óptico simulado** (400x/1000x) | La consideración de A2 lo permitía, etiquetando la vista como ampliada y simplificada (RNF-06) |
| "Archivo científico" del centro (versión previa del código) | **Recuerdos científicos de Baymax** (0/3) | Coherencia con la narrativa del informe (RF-01, RF-09) |
