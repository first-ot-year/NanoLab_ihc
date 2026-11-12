# E5 (85 %) · Tres misiones, feedback, pistas, reintento, microscopía antes/después y métricas

Entregable 5 del cronograma del informe del hito 1 (Cuadro 6, semana 14):

> Cámara experimental, feedback, pistas y reintento. RF asociado: RF-09 · RNF asociados: RNF-01, RNF-08.

Esta entrega **cierra los huecos entre lo que se prometió en el hito 1 y lo que hacía el proyecto** y deja listo todo lo que se puede preparar antes del piloto.

**Antes de esta entrega:**
- El único reto era la vacuna; las estaciones del caso de bacterias estaban ocultas como mobiliario. RF-02 (elegir muestra en el banco), RF-03 (observación inicial al microscopio) y RF-08 (comparar antes/después) solo los cumplía ese caso oculto.
- Baymax recuperaba **1** recuerdo (0/1), aunque el README y RF-09 hablaban de 3.
- No existía la **pregunta de comprensión** del cierre (backlog n.º 9, métrica R de HEART).
- `MetricsLogger` no medía el tiempo inactivo, la indagación ni el tiempo de orientación (E y A de HEART), y no había forma de medir los **72 FPS** (RNF-08) desde el juego.

**Decisiones del equipo:** 3 misiones en **orden fijo**, **un recuerdo por misión** (la vacuna da el tercero), **HEART medido por misión** y un reto con **hongos** (el moho de Fleming).

## Las 3 misiones

| Misión | Recuerdo | Estaciones | Detalle |
|---|---|---|---|
| 1 · ¿Qué hay en las muestras? | OBSERVAR | Banco, Tinción, Microscopía, CONCLUSIONES, Biblioteca, Terminal | [Misiones_1_y_2.md](Misiones_1_y_2.md) |
| 2 · ¿Qué la detiene? | EXPERIMENTAR | Terminal, Prensa, Cámara (reubicada), Microscopía, CONCLUSIONES, Archivo | [Misiones_1_y_2.md](Misiones_1_y_2.md) |
| 3 · Crea la vacuna | CREAR | Laboratorio de vacunas (bloqueado hasta la 2) | [Reto_Vacuna.md](Reto_Vacuna.md) |

## Checklist de salida

| # | Criterio | Cómo se cumple | Evidencia | Estado |
|---|---|---|---|---|
| 1 | **RF-09** feedback y reintento | Cada error explica por qué, no penaliza y se puede repetir; pistas graduales en cada estación (misiones 1-2) y AYUDA / PASO A PASO (misión 3) | PR, PV | ✅ |
| 2 | **RF-09** progreso: 3 recuerdos | Un recuerdo por misión (0/3 > 3/3), con cartel, checkpoint e informe. Las ranuras del indicador que falten se crean al empezar; los recuerdos de partidas antiguas se adaptan | PR "recuerdo 1", "recuerdo 2 y la vacuna desbloqueada"; PV "recuerdo de la MISIÓN 3" | ✅ |
| 3 | **RF-02** selección de muestras en el banco | El banco tiene las MUESTRAS A (herida), B (placa con moho) y C (hisopado del brote); se cargan tocándolas con un portaobjetos, que solo dice "MUESTRA X" | PR "El BANCO tiene la MUESTRA B… y la C", "Frotis de la MUESTRA B/C con su etiqueta" | ✅ (validar en Quest) |
| 4 | **RF-03** microscopía inicial | Misión 1: microscopio óptico de la estación 3 (ocular, 400x/1000x) con la A teñida y las B y C | PR (evidencias a 400x y 1000x, muestras B y C registradas) | ✅ (validar en Quest) |
| 5 | **RF-08** comparación | Misión 1: el HUD pone otra muestra al lado ("COMPARA CON LA MUESTRA A") y el cuadro comparativo; misión 2: **ANTES \| DESPUÉS** del experimento; misión 3: monitor del microscopio invertido CONTROL / PLACEBO | PR "comparación ANTES \| DESPUÉS observada"; PV "…se compararon CONTROL (antes) y PLACEBO (después)" | ✅ (validar en Quest) |
| 6 | Reto de **hongos** | La muestra B es el moho de Fleming (*Penicillium*): se compara con la bacteria y el virus, se clasifica como HONGO, su halo es la pista del agente que rompe la pared (penicilina) y el informe cierra la historia | PR "B = HONGO en CONCLUSIONES" | ✅ |
| 7 | Pregunta de comprensión al cierre (backlog n.º 9, HEART · R) | Misión 1: clasificación en CONCLUSIONES; misión 2: 2 preguntas para explicar el resultado; misión 3: 4 preguntas antes y después | PR, PV; `postAciertos` / `totalComprension` | ✅ |
| 8 | **RF-10** métricas HEART por misión | Un JSON por misión (`caso` = `MISIÓN 1/2/3`): errores por tipo, pistas, indagación, tiempo inactivo, tiempo hasta empezar, comprensión, comparación y rendimiento. `analizar_metricas.py` da la tabla HEART de cada misión | Script probado con sesiones de ejemplo | ✅ |
| 9 | **RNF-08** 72 FPS: forma de medirlo | `MedidorRendimiento` (siempre activo): FPS medio, 1 % peor, p95/p99, % de fotogramas lentos y frecuencia del visor, en cada sesión y en `rendimiento_<fecha>.json` | JSON en el visor | 🟡 falta la medición en Quest |
| 10 | **RNF-02** < 100 ms | La lógica responde en el mismo fotograma: el p99 y el peor tiempo de fotograma son la cota de la latencia (`cumple100ms`) | Mismo JSON | 🟡 falta la medición en Quest |
| 11 | **RNF-01** confort | Las misiones 1 y 2 piden caminar entre estaciones (velocidad constante, giro por pasos, viñeta); la 3, todo frente al mostrador | PL | 🟡 mareo ≤ 2 en el piloto |
| 12 | **RNF-06** | Micrografías ilustrativas generadas por código; avisos de modelo simplificado; agentes con nombres genéricos | — | ✅ |

## Cambios de esta entrega (resumen)

- **Misiones 1 y 2** (`RetoManager`, `Misiones`, `MontajeMisiones`, `PanelConclusiones`): el caso de bacterias vuelve partido en dos misiones, con las muestras B y C, el panel CONCLUSIONES, la identificación comprobada, la explicación del resultado y la cámara reubicada en un hueco libre (se busca al empezar con comprobación de colisiones).
- **Misión 3** (`RetoVacuna`): bloqueada hasta la misión 2; microscopio invertido en el paso 7; preguntas de comprensión; un recuerdo (3/3). Se renombra de CASO 01 a MISIÓN 3.
- **Métricas** (`MetricsLogger`, `MedidorRendimiento`, `analizar_metricas.py`) por misión.
- **Muestras**: `MuestraData` admite el tipo `Hongo` y virus vistos con el microscopio óptico (`vistaOptica`).
- **Partida**: guarda si ya se vieron la B y la C y un checkpoint por misión; adapta los recuerdos de partidas antiguas.
- **Pruebas**: `NanoLab/Probar misiones 1 y 2` (antes "Probar reto completo") y `NanoLab/Probar reto de vacuna`.

## Matriz de trazabilidad actualizada

**Evidencia:** PR = `NanoLab/Probar misiones 1 y 2` (`Logs/prueba_reto.txt`) · PV = `NanoLab/Probar reto de vacuna` (`Logs/prueba_vacuna.txt`) · PL = `NanoLab/Probar locomoción` · JSON = métricas · VR = prueba en Meta Quest · PIL = piloto.

| ID | Requisito | Prior. | Cómo lo cumple ahora | Evidencia | Estado |
|---|---|---|---|---|---|
| RF-01 | Introducción y orientación narrativa | Must | Aviso de inicio, ventana "eres Baymax" con las 3 misiones, tótem de bienvenida, flechas del suelo y tarjeta de objetivo, carteles de las estaciones | PR, PV | 🟡 VR |
| RF-02 | Banco y selección de muestras | Must | MUESTRAS A, B y C en el banco; frotis con portaobjetos etiquetado | PR | ✅ (VR) |
| RF-03 | Microscopía inicial | Must | Misión 1: microscopio óptico con ocular (400x/1000x), evidencias de la A y observación de B y C; misión 3: microscopio invertido | PR, PV | ✅ (VR) |
| RF-04 | Investigación guiada | Must | Fichas de la biblioteca, cuadro comparativo en el HUD y en CONCLUSIONES; tableros GUÍA y CONCEPTOS de la vacuna | PR, PV | ✅ |
| RF-05 | Hipótesis y estrategia | Must | Misión 1: clasificación e identificación (comprobadas); misión 2: estrategia; misión 3: antígeno y diseño | PR, PV | ✅ |
| RF-06 | Preparación experimental | Must | Prensa y viales (misión 2); síntesis, nanopartículas, calidad y frío (misión 3) | PR, PV | ✅ (VR) |
| RF-07 | Experimento con activación por botón | Must | Cámara experimental ACTIVAR (misión 2); INCUBAR y MEZCLAR (misión 3) | PR, PV | ✅ (VR) |
| RF-08 | Microscopía posterior y comparación | Must | Comparación de muestras (misión 1), ANTES \| DESPUÉS (misión 2), CONTROL / PLACEBO (misión 3) | PR, PV, JSON `comparacionAntesDespues` | ✅ (VR) |
| RF-09 | Feedback, progreso y reintento | Must | Errores explicados, pistas graduales, AYUDA y PASO A PASO, 3 recuerdos, checkpoints, REINICIAR / REPETIR | PR, PV | ✅ |
| RF-10 | Registro de métricas y nuevos retos | Could | JSON por misión con HEART y rendimiento; script de análisis; las misiones son ampliables (guía en [Contexto_Proyecto_e_Ideas.md](Contexto_Proyecto_e_Ideas.md)) | PR, PV, JSON | ✅ |
| RNF-01 | Confort | Must | Locomoción a velocidad constante, giro por pasos y viñeta; taburetes en los microscopios | PL | 🟡 PIL (mareo ≤ 2) |
| RNF-02 | Respuesta < 100 ms | Must | Lógica en el mismo fotograma; p99 y peor tiempo de fotograma medidos | JSON `rendimiento.msP99` | 🟡 medir en VR |
| RNF-03 | Claridad visual | Must | Color siempre con texto; rótulos MUESTRA A/B/C; respuestas con CORRECTO / AÚN NO | Capturas | 🟡 VR |
| RNF-04 | Tolerancia al error | Must | Retorno automático de objetos, desinfectante, errores reversibles, nada bloquea el reintento | PR, PV | ✅ |
| RNF-05 | Consistencia | Must | Mismos gestos y botones (poke o rayo) en todas las estaciones y en CONCLUSIONES | PR | ✅ |
| RNF-06 | Seguridad pedagógica | Must | Avisos de modelo simplificado; micrografías ilustrativas; agentes genéricos | PR, PV | ✅ |
| RNF-07 | Accesibilidad básica | Should | Volumen, modo sentado y sillas | PL | 🟡 VR |
| RNF-08 | 72 FPS | Must | Iluminación horneada, piezas por paso, mallas combinadas; `MedidorRendimiento` para medirlo | JSON, OVR Metrics Tool | 🟡 medir en VR |

## Lo que solo se puede cerrar con el visor y con personas

1. **Abrir Unity** y correr `NanoLab/Probar misiones 1 y 2` y `NanoLab/Probar reto de vacuna`: ambos deben terminar en `RESULTADO: CORRECTO` / `TODO CORRECTO`.
2. **Compilar y abrir en el Quest** y recorrer las 3 misiones: dónde quedaron la cámara y el panel CONCLUSIONES (se eligen al empezar; mirar la consola: `[Misiones] … cámara (x, y, z), panel CONCLUSIONES …`), el frotis de las muestras B y C, la lectura del HUD comparativo y del panel.
3. **Medir el rendimiento** en el recorrido completo (JSON de sesión y `rendimiento_*.json`, y si se quiere OVR Metrics Tool). Meta: `fpsMedio` ≥ 68 a 72 Hz y `porcentajeLentos` < 5 %.
4. **Piloto** con 5-8 personas siguiendo [Piloto_HEART.md](Piloto_HEART.md) y análisis con `analizar_metricas.py`.
5. **Video** siguiendo [Guion_Video_E6.md](Guion_Video_E6.md).

## Validación pendiente en Meta Quest

1. Al entrar al Centro, las flechas llevan al BANCO (1). Los rótulos dicen MUESTRA A / B / C; la vacuna muestra MISIÓN BLOQUEADA.
2. Frotis de la A, tinción y microscopio; luego, con el portaobjetos limpio, la B y la C: el HUD muestra la otra muestra al lado.
3. En CONCLUSIONES, los botones A, B y C se pulsan con el dedo y con el rayo, y el texto se lee a ~1 m.
4. Identificar mal la A en la terminal: el aviso explica la diferencia con la ficha; al acertar, cartel MISIÓN 1 COMPLETA (recuerdo 1/3).
5. La cámara experimental en su nuevo sitio: placa + cápsula + ACTIVAR, y el portaobjetos DESPUÉS en el microscopio.
6. Explicación en CONCLUSIONES: MISIÓN 2 COMPLETA (2/3) y la vacuna se desbloquea.
7. Vacuna completa: recuerdo 3/3. Sacar los JSON (`adb pull /sdcard/Android/data/com.utec.NanoLab/files/NanoLab`) y pasarlos por el script.
