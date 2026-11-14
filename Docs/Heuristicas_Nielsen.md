# Evaluación heurística · las 10 heurísticas de Nielsen y la interacción con el SDK de Meta

Revisión de la interacción de NanoLab VR antes del piloto con usuarios (E6). El piloto mide *claridad* ("siempre supe qué tenía que hacer después"), un máximo de 2 ayudas por misión y que cada misión se complete sin el facilitador ([Piloto_HEART.md](Piloto_HEART.md)). Esta evaluación busca los problemas que un experto puede encontrar sin usuarios, para que el piloto mida el diseño y no errores evitables.

## Método

- **Qué se evaluó:** las 3 misiones y los sistemas comunes (avisos flotantes, botones, tarjeta de objetivo, menú ☰, tutorial de inicio, partida guardada), sobre el código de interacción y las capturas de las pruebas automáticas.
- **Con qué criterio:** las 10 heurísticas de usabilidad de Jakob Nielsen, pensando en la persona principal (*Sebastián*, 15 años, poca experiencia en VR).
- **Severidad** (escala de Nielsen): 0 no es un problema · 1 cosmético · 2 menor · 3 mayor · 4 catastrófico.
- **Alcance de los cambios:** misiones 1 y 2 y sistemas comunes, a fondo. La misión 3 (vacuna) solo recibió retoques: ya tenía AHORA, AYUDA y PASO A PASO.
- **Lo que no se cambió a propósito:** los nombres PISTA, AYUDA, PASO A PASO, CONTINUAR y la tecla Y, porque los usa el protocolo del piloto.

Evidencia: **PR** = `NanoLab/Probar misiones 1 y 2` (`Logs/prueba_reto.txt`) · **PL** = `NanoLab/Probar locomoción` (`Logs/prueba_locomocion.txt`) · **CAP** = captura en `Logs/Capturas/` · **VR** = pendiente de validar en Meta Quest.

## Hallazgos y cambios

### 1 · Visibilidad del estado del sistema

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| La tarjeta de objetivo decía "PASO n DE 9" con las etapas internas; no reflejaba las misiones ni sus subpasos | 2 | La tarjeta dice **MISIÓN n · PASO x DE y · NOMBRE** (misión 1: 6 pasos; misión 2: 5) con un punto por paso (`RetoManager.PasoMision`, `GuiaReto.Etapa`) | PR "La tarjeta de objetivo empieza en MISIÓN 1 · paso 1 de 6…", "MISIÓN 2 · paso 1 de 5…" |
| Al terminar las misiones 1 y 2 la tarjeta decía "Sigue las flechas del suelo hasta" y nada más | 2 | Las flechas llevan al LABORATORIO DE VACUNAS; sin destino, la tarjeta solo enseña su pie | VR |
| En la misión 3 la tecla Y seguía enseñando "MISIONES 1 y 2 COMPLETAS" | 2 | La tarjeta enseña el paso de la vacuna (PASO x DE 9 y la indicación del paso) | VR |
| El portaobjetos no decía cómo iba la tinción | 2 | Su etiqueta añade `GRAM 2/4`, `GRAM COMPLETA` o `TINCIÓN NO VÁLIDA` (`Portaobjetos.Refrescar`) | VR |

### 2 · Relación entre el sistema y el mundo real

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| El primer objetivo mandaba al BANCO (1), pero el portaobjetos está en TINCIÓN (2); para las muestras B y C las flechas llevaban al microscopio cuando había que ir al banco | 3 | El objetivo y las flechas siguen **lo que el jugador lleva de verdad**: primero coger un portaobjetos, después la muestra; al banco si falta cargar la B o la C (`RetoManager.EstacionActual`, sondeo cada 0,25 s) | PR "…manda a coger un portaobjetos (H1, H2)" |
| Vocabulario sin explicar (frotis, Gram, platina, hifas, antígeno) | 2 | Glosario de dos tarjetas en la AYUDA del menú | PL "AYUDA del panel ☰ abre la ayuda…" |
| La misión 3 decía "CASO RESUELTO" y las otras "MISIÓN n COMPLETA" | 1 | Mismo vocabulario en las tres | — |

### 3 · Control y libertad del usuario

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| REINICIAR (archivo 8) borraba el avance de las misiones 1 y 2 con una pulsación | 3 | Pide una segunda pulsación y dice qué se pierde y qué se conserva (`PanelResumen.PedirReinicio`) | PR "REINICIAR pide confirmación…" |
| VOLVER AL CHECKPOINT descartaba lo avanzado sin confirmar | 3 | Segunda pulsación, igual que NUEVA PARTIDA | VR |
| El tutorial de inicio (1 + 6 tarjetas) no se podía saltar | 2 | Botón **Saltar** en cada tarjeta | VR |
| El HUD del microscopio no decía cómo se sale | 1 | "Aparta la cara: salir" junto a los controles | CAP `hud_*` |

### 4 · Consistencia y estándares

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| Tres formas de confirmar (la prensa y NUEVA PARTIDA pedían doble pulsación; REINICIAR y CHECKPOINT, ninguna) | 2 | Un único patrón, `Confirmacion` (doble pulsación en 5 s), para REINICIAR, CHECKPOINT, NUEVA PARTIDA y el desinfectante | PR, PL |
| El botón se llama PISTA y el aviso salía como "AYUDA · …"; los errores salían "ATENCIÓN · AÚN NO. …" | 2 | Prefijos fijos: `BIEN ·`, `AÚN NO ·`, `PISTA ·`, `FALTA UN PASO ·` | CAP `prensa_1_no_coincide` |
| Los botones del menú ☰ no eran poke como los demás: se activaban al acercar la mano | 2 | Botones poke oficiales de Meta (ver la sección del SDK) | PL "Los botones del panel ☰ son poke de Meta… 11/11" |

### 5 · Prevención de errores

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| Rozar la placa B con el portaobjetos de la A ya teñida borraba la tinción | 3 | La muestra teñida no se pisa: avisa y pide otro portaobjetos o limpiarlo (`FuenteMuestra.Aplicar`) | PR "Tocar la MUESTRA B con el portaobjetos teñido de la A no borra la tinción" |
| El desinfectante borraba una tinción válida solo por acercar el portaobjetos | 2 | Si la tinción es válida y aún no se observó, pide tocarlo otra vez | PR "El desinfectante pide confirmar…" |
| Con la muestra A ya identificada, pulsar otra bacteria contaba un error en las métricas de la misión 2 | 3 | La fila 1 de la terminal queda cerrada y lo dice | PR "…la fila 1 de la terminal ya no cuenta errores" |
| Botones que parecían activos y solo rechazaban (terminal, FORMULAR, ACTIVAR) | 2 | Se ven **grises** mientras no se pueden usar; al pulsarlos siguen explicando qué falta (`BotonFisico.Habilitar`) | PR "Los botones de la terminal se ven deshabilitados…" |
| NUEVA PARTIDA estaba pegada a GUARDAR PARTIDA | 2 | Lo que borra el avance va en otra fila y en rojo | CAP `ajustes_menu` |

### 6 · Reconocer en lugar de recordar

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| Entre la biblioteca (4) y la terminal (5) había que recordar qué decía cada ficha | 2 | La pantalla de la terminal resume los rasgos de cada ficha **ya consultada** junto a su letra; las demás salen como "ficha sin consultar" | VR |
| Los controles solo se veían en el tutorial inicial | 2 | Tarjetas CONTROLES y EN EL MICROSCOPIO en la AYUDA del menú | PL |
| Los avisos flotantes se iban y no se podían releer | 2 | Historial de los 6 últimos avisos importantes; la AYUDA enseña el último | PR "Los avisos importantes quedan en el historial…" |
| Al cargar la partida no se recordaba qué tocaba hacer | 2 | El aviso "Partida cargada" añade la misión, el paso y el objetivo, y avisa si hay que repetir el último paso | VR |

### 7 · Flexibilidad y eficiencia de uso

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| Quien vuelve a abrir la app pasaba otra vez por todo el tutorial | 2 | Con partida guardada solo sale el aviso de ajuste del casco; el tutorial queda en el menú > AYUDA | VR |
| No se decía que se pueden llevar dos portaobjetos (B y C) a la vez | 1 | Pista nueva en la Observación | — |

### 8 · Diseño estético y minimalista

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| Objetivos de hasta 230 caracteres en una tarjeta de 42 × 24 cm | 2 | La tarjeta enseña **una frase** (`RetoManager.ObjetivoCorto`) y el texto se encoge si no cabe; el texto largo queda en los carteles de las estaciones | PR (textos entre comillas en el informe) |
| Al completar una misión salían tres avisos apilados | 2 | Un solo aviso: misión, recuerdo y "Progreso guardado (checkpoint)" | VR |

### 9 · Ayudar a reconocer, diagnosticar y recuperarse de los errores

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| "Límpialo y repite 1 > 2 > 3 > 4" omitía que hay que volver a hacer el frotis; el objetivo seguía diciendo "Tiñe el frotis" con el portaobjetos limpio | 3 | Los mensajes dicen todos los pasos y, con el portaobjetos limpio, el objetivo, la pista y las flechas llevan al BANCO | PR "Con el portaobjetos limpio, el objetivo manda a repetir el frotis en el BANCO" |
| Todos los avisos duraban 5 s aunque tuvieran 200 caracteres | 2 | La duración depende de la longitud (4 a 12 s) y llevan un fondo oscuro para leerse sobre el laboratorio claro | CAP `prensa_1_no_coincide` |
| "Aún no puedes" y "te equivocaste" salían igual, en rojo y con sonido de error | 2 | Tipo nuevo `Falta` ("FALTA UN PASO · …", ámbar, sonido de pista). No cuenta como error en las métricas | PR |

### 10 · Ayuda y documentación

| Problema | Sev. | Cambio | Evidencia |
|---|---|---|---|
| No había ayuda bajo demanda fuera de la PISTA de cada estación | 2 | Botón **AYUDA** en el menú ☰ con 7 tarjetas: qué hago ahora, controles, microscopio, si te atascas, glosario (2) y partida (`AvisoInicio.MostrarAyuda`). No cuenta como pista | PL "AYUDA del panel ☰ abre la ayuda con el objetivo actual, los controles y el glosario" |

## Lo que ya cumplía

- Feedback redundante en al menos dos canales (color + texto, sonido, vibración).
- Tolerancia al error: los objetos que se caen vuelven solos a su sitio.
- Errores sin castigo, con explicación, y pistas graduales.
- La prensa avisa si el vial no sigue la estrategia y deja probarlo igual.
- La terminal resume las evidencias y CONCLUSIONES muestra el cuadro comparativo.
- "‹ Atrás" en el tutorial y partida guardada con respaldo.

## Interacción con el Meta XR Interaction SDK (poke interaction)

SDK: `com.meta.xr.sdk.all` 201. Rig: `OVRCameraRigInteraction` de Meta, con interactores de poke para manos y mandos.

| Pieza de Meta | Dónde se usa |
|---|---|
| `PokeInteractable` + `PokeInteractableVisual` + `InteractableColorVisual` (prefab oficial `Runtime/Prefabs/Poke/PokeInteractable.prefab`) | **Todos los botones del juego**: estaciones de las misiones 1 y 2 (13), vacuna, aviso de inicio y ayuda, y ahora los 11 del menú ☰ |
| `Grabbable`, `GrabInteractable`, `HandGrabInteractable`, `DistanceGrabInteractable`, `PhysicsGrabbable` | Portaobjetos, placas, viales, cápsulas y piezas de la vacuna |
| `FirstPersonLocomotor`, giro por pasos y viñeta de confort | Movimiento del jugador |
| Shader `Interaction/RoundedBoxUnlit` | Paneles redondeados y fondo de los avisos |

**Añadido en esta revisión:**

- **Menú ☰ con poke.** Sus botones eran paneles planos que se activaban cuando el ancla de la mano se acercaba a 4,5 cm: se pulsaban sin querer y con manos rastreadas casi no se podían usar (el ancla es la muñeca). Ahora cada botón lleva el bloque poke oficial (`MontarAjustes.Poke`): se pulsa con la punta del dedo o del mando y se hunde. `BotonMenu` escucha `WhenStateChanged`; el toque por cercanía queda solo como respaldo si falta el bloque.
- **Estado deshabilitado** con el `InteractableColorVisual` del propio SDK, sin componentes nuevos.

**Evaluado y dejado para cuando se pueda validar en el visor:**

- El **UI Set** del SDK (diálogos, interruptores, deslizadores): obliga a rehacer el menú con Canvas y cambia el estilo visual.
- `SnapInteractable` en lugar de las zonas de encaje propias: tocaría todas las piezas guardadas en la escena.
- `RayInteractor` en lugar del rayo propio, que además hace el frotis y deja el portaobjetos en la platina.

## Relación con HEART

- **Happiness (claridad):** tarjeta con paso y objetivo de una frase, flechas al sitio correcto y ayuda bajo demanda.
- **Task success (≤ 2 ayudas):** la AYUDA del menú y los avisos "FALTA UN PASO" no cuentan como pista.
- **Errores por paso:** las confirmaciones y los "falta un paso" no cuentan como error, y la fila 1 de la terminal ya no añade errores a la misión 2, así que la tabla de errores refleja equivocaciones reales.

## Resultados de verificación (batchmode)

| Prueba | Resultado |
|---|---|
| `NanoLab/Probar misiones 1 y 2` | CORRECTO: 85 comprobaciones OK (9 nuevas), 0 fallos, 0 excepciones |
| `NanoLab/Probar reto de vacuna` | CORRECTO: 47 OK |
| `NanoLab/Probar locomoción` | CORRECTO: 27 OK (2 nuevas: AYUDA y poke del menú) |
| `NanoLab/Probar recorrido del jugador` | Todas las rutas son transitables |
| `NanoLab/Probar física de los objetos agarrables` | 49 agarrables, 0 movidos |
| `NanoLab/Aplicar heurísticas (panel ☰ con AYUDA y botones poke)` | Panel rehecho con 11 botones, sin hornear la luz |

## Validación pendiente en Meta Quest

1. La tarjeta de objetivo (tecla Y) y los avisos con fondo se leen a 0,75 m.
2. Al empezar, las flechas llevan a la mesa de TINCIÓN y, con el portaobjetos en la mano, al BANCO.
3. Un botón gris se distingue de uno activo y, al pulsarlo, explica qué falta.
4. Los botones del menú ☰ se pulsan con el dedo y con el mando (poke) y con el rayo; no se activan al pasar la mano.
5. REINICIAR, VOLVER AL CHECKPOINT y NUEVA PARTIDA piden la segunda pulsación.
6. "Saltar" cierra el tutorial; con partida guardada no vuelve a salir.
7. AYUDA del menú: las 7 tarjetas se leen y el texto no pisa los botones.
8. La etiqueta del portaobjetos (GRAM 2/4) se lee en la mano.
9. Rozar la placa B con la muestra A teñida no la borra.

## Archivos

| Archivo | Cambio |
|---|---|
| `Scripts/Microbiologia/Aviso.cs` | Tipos `Falta`, prefijos, duración según longitud, fondo e historial |
| `Scripts/Microbiologia/Confirmacion.cs` | Nuevo: doble pulsación común |
| `Scripts/Microbiologia/BotonFisico.cs` | `Habilitar` (botón gris) |
| `Scripts/Centro/RetoManager.cs`, `GuiaReto.cs` | Paso por misión, `ObjetivoCorto`, destino según el portaobjetos, tarjeta de la misión 3 |
| `Scripts/Microbiologia/Portaobjetos.cs`, `FuenteMuestra.cs` | Etiqueta con la tinción, mensajes de recuperación, protección de la muestra teñida |
| `Scripts/Centro/TerminalHipotesis.cs`, `PrensaCapsulas.cs`, `CamaraExperimental.cs`, `PanelResumen.cs` | Botones deshabilitados, fichas consultadas, confirmación de REINICIAR |
| `Scripts/Microbiologia/AvisoInicio.cs`, `MenuAjustes.cs`, `BotonMenu.cs` | AYUDA, Saltar, confirmaciones, poke en el menú |
| `Editor/MontarAjustes.cs`, `Editor/AplicarNovedades.cs` | Panel ☰ con 11 botones poke y menú para aplicarlo |
| `Scripts/Centro/RetoVacuna.cs` | Vocabulario "MISIÓN COMPLETA" y datos para la tarjeta |
| `Scripts/Centro/DebugReto.cs`, `Scripts/Microbiologia/DebugLocomocion.cs` | Comprobaciones nuevas |
