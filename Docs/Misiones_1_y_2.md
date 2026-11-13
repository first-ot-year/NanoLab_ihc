# MISIONES 1 y 2 · Observar y experimentar en el Centro de Experimentación

Las dos primeras misiones de NanoLab VR usan el **mobiliario del Centro de Experimentación** (las 8 estaciones del caso de bacterias de E2-E4) y un panel nuevo, **CONCLUSIONES**, junto a los microscopios. Cada misión devuelve un **recuerdo científico** de Baymax. La tercera es el [Laboratorio de vacunas](Reto_Vacuna.md).

> Modelo educativo simplificado: muestras, agentes y resultados son ilustrativos y no representan protocolos clínicos ni tratamientos reales (RNF-06).

## La historia

Hay un brote en la ciudad. Baymax recibe tres muestras y debe averiguar qué hay en ellas (misión 1), probar qué detiene al microbio que sí puede ver (misión 2) y, como contra el virus del brote no funciona, crear una vacuna (misión 3). **Orden fijo:** la vacuna está bloqueada hasta terminar la misión 2.

| Recuerdo | Misión | Estaciones | Tiempo previsto |
|---|---|---|---|
| 1/3 · OBSERVAR | **MISIÓN 1 · ¿Qué hay en las muestras?** | 1 Banco, 2 Tinción, 3 Microscopía, CONCLUSIONES, 4 Biblioteca, 5 Terminal (fila 1) | 5-8 min |
| 2/3 · EXPERIMENTAR | **MISIÓN 2 · ¿Qué la detiene?** | 5 Terminal (fila 2), 6 Prensa, 7 Cámara, 3 Microscopía, CONCLUSIONES, 8 Archivo | 5-8 min |
| 3/3 · CREAR | **MISIÓN 3 · Crea la vacuna** | Laboratorio de vacunas | 10-15 min |

## Las tres muestras del banco

| Muestra | Qué es | De dónde viene | Qué se ve al microscopio |
|---|---|---|---|
| **A** | Bacteria (*S. aureus*) | Herida de piel que no sana | Con tinción de Gram: cocos violetas de ~1 µm en racimos |
| **B** | **Hongo** (moho *Penicillium*) | Placa olvidada en la incubadora: le creció un moho | Hifas largas y ramificadas (3-5 µm) con esporas en forma de pincel. En la placa, **alrededor del moho no crece ninguna colonia** (halo) |
| **C** | Virus del brote | Hisopado nasal de un paciente | Solo células grandes de la nariz (~50 µm): **ningún microbio visible**, porque el virus (~100 nm) es demasiado pequeño |

Las imágenes de B y C son ilustraciones generadas por código (`MicrografiasMisiones`), sin licencias de terceros.

### El reto de los hongos: el moho de Fleming

La muestra B reproduce lo que vio Alexander Fleming en 1928: una placa de bacterias contaminada por un moho, con un halo sin bacterias alrededor. Aparece en tres momentos:

1. **Misión 1:** se compara al microscopio con la A (las bacterias son diminutas y sueltas; el hongo forma filamentos grandes con esporas) y se clasifica como HONGO. *Penicillium* significa "pincelito" en latín, por la forma de sus esporas.
2. **Misión 2:** si un agente falla, la pista recuerda el halo de la muestra B ("algo rompía a las bacterias"). Al explicar el éxito, el panel cuenta que esa sustancia es la **penicilina**, que rompe la pared de las bacterias, igual que el agente PG.
3. **Informe final:** el moho como origen de los antibióticos, y por qué no sirven contra el virus de la muestra C.

## MISIÓN 1 · ¿Qué hay en las muestras? (RF-01 a RF-05, RF-08)

1. **Banco (1):** con un portaobjetos de la mesa de tinción se toca la placa de la **MUESTRA A** (frotis). El portaobjetos dice "MUESTRA A", no el nombre.
2. **Tinción (2):** tinción de Gram 1 > 2 > 3 > 4 (un orden erróneo invalida el frotis; el desinfectante lo limpia).
3. **Microscopía (3):** a 400x se registra la forma y a 1000x la agrupación; con el Gram completo, el color.
4. **Comparar B y C:** con el portaobjetos limpio (o con otro) se tocan la placa B y el tubo C del banco y se miran al microscopio. El HUD pone **a la derecha la imagen de otra muestra ya vista** ("COMPARA CON LA MUESTRA A") y el **cuadro comparativo** de las tres.
5. **CONCLUSIONES:** el panel muestra el cuadro comparativo y pregunta, una por una, si cada muestra es **BACTERIA, HONGO o VIRUS** (botones A, B, C). Cada respuesta se explica; un error cuenta como error y se reintenta.
6. **Biblioteca (4):** se consulta una ficha (se puede hacer antes).
7. **Terminal (5), fila 1:** se identifica la bacteria de la A. **Una identificación que no encaja se explica** comparando la ficha elegida con las evidencias y no se registra. Al acertar: **MISIÓN 1 COMPLETA**, recuerdo 1/3 y checkpoint.

## MISIÓN 2 · ¿Qué la detiene? (RF-05 a RF-09)

1. **Terminal (5), fila 2:** qué estructura de *S. aureus* atacar (pared, membrana externa, replicación viral o control).
2. **Prensa (6):** vial del agente; la pantalla dice si COINCIDE con la estrategia; FORMULAR.
3. **Cámara experimental (7):** placa de la MUESTRA A + cápsula, ACTIVAR. La cámara está **reubicada** (su sitio original lo ocupa el laboratorio de vacunas): `MontajeMisiones` busca al empezar un hueco libre (por defecto, detrás del laboratorio de vacunas, de cara a él).
4. **Microscopía (3):** portaobjetos DESPUÉS: **ANTES | DESPUÉS**. Si falló, pista y vuelta a la terminal (la estrategia se repiensa).
5. **CONCLUSIONES:** con el agente eficaz, dos preguntas para explicar el resultado: *¿por qué funcionó?* (pared gruesa de peptidoglucano, por eso quedó violeta) y *¿serviría contra el virus del brote?* (no: los virus no tienen esa pared). Al acertar: **MISIÓN 2 COMPLETA**, recuerdo 2/3, checkpoint, informe en el **Archivo (8)** y **MISIÓN 3 desbloqueada**.

## Todo el mobiliario del centro se usa (equipos 9 a 13)

Los equipos que antes solo decoraban tienen ahora una consola (pantalla + botón amarillo, con el dedo o con el rayo) y un número en la guía. Cada uno es un paso obligatorio con sentido de laboratorio real; pulsarlo cuando no toca no penaliza: explica para qué sirve y cuándo se usa (`Scripts/Centro/EquiposCentro.cs`).

| N.º | Equipo | Misión y paso | Qué bloquea hasta usarlo | Qué enseña |
|---|---|---|---|---|
| 9 | Incubadora 37 °C | M1, al empezar: **SACAR PLACAS** | Las placas A y B no aparecen en el banco (no hay frotis) | Las bacterias crecen a la temperatura del cuerpo |
| 10 | Cabina de bioseguridad | M1, antes de la muestra C: **ENCENDER FLUJO** | El hisopado C (ahora dentro de la cabina) no se abre | Un posible virus respiratorio se manipula con flujo de aire filtrado |
| 11 | Autoclave 121 °C | M2, último paso: **ESTERILIZAR** | La misión 2 no se completa | Lo que tocó una muestra se esteriliza con vapor antes de tirarlo |
| 12 | Ultracongelador -80 °C | M3, paso 3: **SACAR REACTIVOS** | La pipeta no descarga en el tubo de síntesis | El molde de ADN y la enzima T7 se degradan sin frío |
| 13 | Incubadora CO2 | M3, paso 6: **SACAR CÉLULAS** | La pipeta no descarga en la placa del ensayo | Las células Vero necesitan 37 °C y 5 % de CO2 |

Guía paso a paso: el objetivo de la tarjeta (botón Y), las flechas del suelo y el AHORA: de la pantalla del equipo apuntan al mueble que toca; en la Observación el orden es MUESTRA A (microscopio) > B (banco) > cabina > C > microscopio. En la misión 3 el plan del paso (y el modo PASO A PASO) empieza por el equipo, y las flechas del centro llevan hasta él.

## Guía, ayudas y accesibilidad

- **Flechas del suelo y tarjeta de objetivo** (`GuiaReto`, tecla Y): llevan a la estación del paso actual, también al panel CONCLUSIONES. Cada estación tiene su cartel con **PISTA** (pistas graduales por paso).
- **Errores sin castigo:** toda respuesta equivocada explica por qué; nada bloquea el reintento.
- Todo se puede hacer de pie o sentado (taburetes frente a los microscopios).

## Métricas (una sesión por misión)

`MetricsLogger` guarda un JSON por misión (`caso` = `MISIÓN 1` / `MISIÓN 2`): tiempos por etapa, errores por tipo (tinción, clasificación, identificación, experimento, explicación), pistas, observaciones al microscopio, acciones de indagación (frotis, muestras observadas, evidencias, fichas, clasificaciones), tiempo inactivo, tiempo hasta empezar, comparación ANTES/DESPUÉS y rendimiento. **Retention:** misión 1 = clasificar las 3 muestras a la primera (`postAciertos`/3); misión 2 = las 2 preguntas de explicación a la primera (`postAciertos`/2). `Herramientas/analizar_metricas.py` resume cada misión por separado.

## Partida

La partida guarda la etapa, las evidencias de la A, la identificación, la estrategia y si ya se vieron la B y la C; los recuerdos (`MISIÓN 1 · OBSERVAR`, `MISIÓN 2 · EXPERIMENTAR`, `MISIÓN 3 · CREAR`) y un checkpoint por misión. Las partidas antiguas se adaptan: el recuerdo "CASO 01" pasa a ser el de la misión 3 y los dos intermedios de la vacuna del E5 se descartan.

## Probar sin visor

- Menú **`NanoLab/Probar misiones 1 y 2 (play mode)`**: recorre las dos misiones con las mismas funciones que disparan las manos, con errores a propósito (tinción, clasificación, identificación, vial que no sigue la estrategia, explicación) y comprueba recuerdos, desbloqueo de la vacuna y partida. Informe en `Logs/prueba_reto.txt` y capturas (`hud_comparacion_muestra_b/c`, `conclusiones_*`…).
- En Play Mode: **F9** recorre las misiones acertando y **F10** fallando primero.

## Archivos

| Archivo | Qué hace |
|---|---|
| `Scripts/Centro/Misiones.cs` | Códigos, títulos y recuerdos de las 3 misiones, desbloqueo de la vacuna, muestras B y C y sus micrografías |
| `Scripts/Centro/RetoManager.cs` | Flujo de las misiones 1 y 2, comparación de muestras, panel CONCLUSIONES, recuerdos, métricas por misión |
| `Scripts/Centro/MontajeMisiones.cs` | Al empezar: reubica la cámara, crea las muestras B y C en el banco, rotula la A, monta CONCLUSIONES y actualiza carteles |
| `Scripts/Centro/PanelConclusiones.cs` | Atril con pantalla y 3 botones (cuadro comparativo, clasificación y explicación) |
| `Scripts/Centro/TerminalHipotesis.cs` | Fila 1 (misión 1: identificar) y fila 2 (misión 2: estrategia) |
| `Scripts/Microbiologia/MuestraData.cs` | Tipo `Hongo` y `vistaOptica` (virus que se mira con el microscopio óptico) |
| `Scripts/Centro/DebugReto.cs`, `Editor/PruebaReto.cs` | Prueba automática de las misiones 1 y 2 |
