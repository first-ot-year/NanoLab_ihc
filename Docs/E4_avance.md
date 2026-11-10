# E4 (70 %) · Hipótesis, estrategia y preparación de la intervención

Entregable 4 del cronograma del informe del hito 1 (Cuadro 6, semana 13):

> Implementar la selección de una hipótesis y la preparación de una intervención predefinida. **Salida:** el usuario selecciona una estrategia a partir de la información consultada y realiza una acción sencilla de preparación.
> RF asociados: RF-05, RF-06 · RNF asociado: RNF-06.

**Antes de esta entrega:**
- La terminal solo preguntaba el microorganismo.
- La estrategia quedaba implícita en el vial que se encajaba en la prensa.
- Se podía registrar la hipótesis sin haber consultado ninguna ficha.

**Ahora el ciclo es explícito:** evidencia → hipótesis → estrategia → agente.

## Checklist de salida

| # | Criterio | Cómo se cumple | Evidencia | Estado |
|---|---|---|---|---|
| 1 | **RF-05:** elegir una hipótesis entre opciones predefinidas | Terminal (5), fila 1: *E. coli*, *S. aureus* o *B. subtilis* (botones A, B, C). Sin texto libre | PR "Intento n: hipótesis … registrada; sin estrategia sigue en Hipótesis"; CAP `terminal_1_hipotesis.png` | ✅ |
| 2 | **RF-05:** elegir una **estrategia** | Fila 2 de la terminal: **PARED** (atacar la pared de peptidoglucano), **MEMBRANA EXT.** (romper la membrana externa), **VIRUS** (bloquear la replicación viral) o **CONTROL** (no atacar nada). Solo se acepta después de la hipótesis. Con las dos se pasa a Preparación | PR "sin hipótesis, la terminal no acepta todavía la estrategia", "estrategia … elegida con el rayo > etapa Preparación"; CAP `terminal_2_estrategia.png` | ✅ |
| 3 | **RF-05:** "a partir de la información consultada" | La terminal rechaza la hipótesis si faltan evidencias (dice cuáles) o si aún no se consultó una ficha de la biblioteca (4). La pantalla resume las evidencias del estudiante ("cocos · en racimos · positiva (VIOLETA)") junto a las dos decisiones | PR "La terminal rechaza una hipótesis sin evidencias", "Con las evidencias completas, la terminal pide consultar antes una ficha (RF-05)" | ✅ |
| 4 | **RF-06:** acción sencilla de preparación | Prensa (6): se encaja el vial del agente en la silueta y se pulsa FORMULAR. Sale la cápsula del color del agente y el vial vuelve a su sitio | PR "cápsula … formulada > etapa Experimento", "el vial vuelve a su sitio"; CAP `prensa_3_capsula_lista.png` | ✅ |
| 5 | **RF-06:** la preparación sigue la decisión | La pantalla de la prensa muestra "hipótesis · estrategia", el agente encajado y **[OK] COINCIDE / [!] NO COINCIDE con tu estrategia**, con texto y no solo color (RNF-03). El tablero AGENTES DISPONIBLES indica a qué estrategia corresponde cada agente, con el mismo nombre que la terminal (RNF-05) | PR "la pantalla de la prensa muestra la hipótesis y que coincide / NO coincide con la estrategia"; CAP `prensa_2_coincide_pantalla.png` | ✅ |
| 6 | El error como información | Con un vial que no coincide, FORMULAR avisa y hay que pulsarlo otra vez antes de 4 s para probarlo igual (no se bloquea). Si el experimento falla, la pista lo explica: "Tu estrategia (…) era buena, pero el agente ME no hace eso…". La estrategia se borra para repensarla y la hipótesis se mantiene | PR "la prensa avisa y no formula hasta pulsar FORMULAR otra vez", "La pista dice que la estrategia era buena y el vial no…"; CAP `prensa_1_no_coincide*.png` | ✅ |
| 7 | Registro y cierre | `MetricsLogger` guarda las estrategias y, en cada intento, la estrategia y si el vial la seguía. El informe del ARCHIVO (8) lista cada intento como "hipótesis · estrategia · agente: eficaz/sin efecto". El HUD ANTES \| DESPUÉS muestra la estrategia | PR "El registro guarda la estrategia del intento…", "El informe del caso muestra hipótesis · estrategia · agente…"; CAP `archivo.png`, `hud_3_*.png` | ✅ |
| 8 | **RNF-06:** contenido simplificado | Aviso "Modelo educativo simplificado: no es un protocolo clínico ni un tratamiento real" en la terminal, la pantalla de la prensa y el tablero de agentes. Se suma al de la introducción, la recepción, el panel ☰ y el informe. Los agentes tienen nombres genéricos (PG, ME, AV, SAL), no de fármacos reales | PR "Aviso de contenido simplificado (RNF-06) en: introducción, recepción, terminal, prensa y tablero de agentes, panel ☰" | ✅ |
| 9 | Guardado | La partida y el checkpoint guardan la estrategia (por su diana, no por su posición). Una partida anterior al E4 en Preparación, sin estrategia, vuelve a Hipótesis | PR "La partida y el checkpoint guardan la estrategia…", "Una partida en Preparación sin estrategia…" | ✅ |
| 10 | Accesibilidad de los botones | Los 7 botones de la terminal se tocan (poke de Meta) o se apuntan con el rayo, igual que las fichas y el panel ☰ del E3 | PR "estrategia … elegida con el rayo" | ✅ (validar en VR) |

## Cambios de esta entrega

- **Datos del reto:**
  - `RetoData`: clase `Estrategia` (nombre, rótulo corto, descripción y diana) y lista `estrategias`.
  - `Eficacia(diana, muestra)` y `DianaCorrecta`, que da la estrategia que funciona con la muestra del caso.
  - `MontarCentroExperimentacion.CrearReto` define las 4 estrategias del CASO 01 y reescribe las pistas de Hipótesis y Preparación.
- **`RetoManager`:**
  - `Estrategia`, `RegistrarEstrategia`, `PuedeEstrategia` y `EsCoherente(agente)`.
  - `PuedeHipotesis` exige haber consultado una ficha.
  - Solo se pasa a Preparación con hipótesis y estrategia.
  - Tras un fallo, la pista depende de la estrategia y la estrategia se borra.
  - Objetivos, HUD de comparación e informe con la estrategia.
  - `Restaurar(…, estrategia)`.
- **`TerminalHipotesis`:**
  - segunda fila de botones (`botonesEstrategia`, `ElegirEstrategia`);
  - pantalla con evidencias, el estado de las dos decisiones y el aviso de RNF-06;
  - se refresca con `RetoManager.Cambio`.
- **Montaje de la terminal:**
  - consola más honda (0,7 m) con dos filas rotuladas: "1) HIPÓTESIS: ¿QUÉ MICROORGANISMO ES?" y "2) ESTRATEGIA: ¿QUÉ ESTRUCTURA ATACARÁS?";
  - pantalla más grande;
  - suelo y cartel "HIPÓTESIS Y ESTRATEGIA".
- **`PrensaCapsulas`:**
  - la pantalla muestra la decisión y si el vial coincide con ella;
  - doble pulsación para un vial que no coincide (`EsperandoConfirmacion`);
  - tablero de agentes con "> estrategia" (`TableroAgentes`).
- **Corrección:** los avisos flotantes de la terminal y de la prensa salían a 0,55–0,6 m del suelo, dentro de la consola y de la mesa, porque el origen de la estación está en el suelo. Ahora salen sobre el borde de la consola y sobre la pantalla de la prensa.
- **Guardado y registro:**
  - `Partida`: campo `estrategia` en `Datos` y `Checkpoint`. Es aditivo y sigue en la versión 2.
  - `MetricsLogger`: `estrategias`, `Intento.estrategia` e `Intento.coherente`.
- **Escena:**
  - menú `NanoLab/Aplicar E4 (terminal de hipótesis y estrategia)` (`AplicarNovedades.AplicarE4` → `MontarCentroExperimentacion.AplicarE4`);
  - rehace solo la terminal y el tablero de agentes, sin hornear la luz;
  - `Construir()` usa las mismas funciones en un montaje completo.
- **Pruebas:**
  - `PruebaReto` pasa de 46 a 61 comprobaciones.
  - El primer intento usa a propósito la estrategia correcta con un vial que no la sigue.
  - Capturas `terminal_*` y `prensa_*` (con primer plano de la pantalla de la prensa).

## Resultados de verificación (batchmode)

| Prueba | Resultado |
|---|---|
| `NanoLab/Aplicar E4` | Terminal rehecha con 3 hipótesis y 4 estrategias, y tablero de agentes con su estrategia. Colisiones: 39 muebles y 8 objetos |
| `NanoLab/Probar reto completo` | CORRECTO: 61 comprobaciones OK, 0 excepciones |
| `NanoLab/Probar locomoción` | CORRECTO: 25 comprobaciones OK |
| `NanoLab/Probar recorrido del jugador` | Todas las rutas son transitables, incluida microscopía → biblioteca → terminal con la consola más honda |
| `NanoLab/Probar física de los objetos agarrables` | 0 objetos movidos (Química 107, Microbiología 49) |

**Capturas** (`Logs/Capturas/`):
- `terminal_1_hipotesis.png`
- `terminal_2_estrategia.png`
- `prensa_1_no_coincide.png` y `prensa_1_no_coincide_pantalla.png`
- `prensa_2_coincide_pantalla.png`
- `prensa_3_capsula_lista.png`

## Validación pendiente en Meta Quest

1. En la terminal, a 0,5–1 m, se leen las dos filas y la pantalla. Pulsar un botón de cada fila con el dedo y otro con el rayo.
2. Elegir la estrategia antes que la hipótesis: aparece el aviso "elige primero el microorganismo" sobre la consola, no dentro.
3. En la prensa, encajar un vial que no coincide: la pantalla dice NO COINCIDE, el primer FORMULAR avisa (el aviso se ve sobre la prensa) y el segundo formula.
4. Fallar a propósito: tras analizar el DESPUÉS, la pista habla de la estrategia y la terminal pide elegirla otra vez.
5. Guardar en Preparación, salir y volver: la estrategia sigue elegida.
