# Piloto con usuarios · protocolo HEART (E6)

Protocolo para la prueba piloto de NanoLab VR (3 misiones: 1 · ¿Qué hay en las muestras?, 2 · ¿Qué la detiene?, 3 · Crea la vacuna). Sirve para medir las metas HEART del README y la validación en el visor de los requisitos marcados con 🟡 en [E5_avance.md](E5_avance.md).

## 1. Objetivo

Comprobar con estudiantes reales que:

- completan cada misión sin ayuda del facilitador (Task success, Adoption);
- exploran e investigan en lugar de solo seguir instrucciones (Engagement);
- recuerdan la relación entre lo observado y el resultado (Retention);
- se sienten cómodos, entienden qué hacer y les resulta útil (Happiness), sin mareo;
- el juego mantiene 72 FPS en el visor (RNF-08).

## 2. Participantes

| Grupo | Cantidad | Perfil |
|---|---|---|
| Persona principal (*Sebastián*) | 3-5 | 4.º o 5.º de secundaria, poca o ninguna experiencia con VR |
| Persona secundaria (*Angie*) | 2-3 | Primeros ciclos de una carrera de ciencias (biología, biotecnología, química, medicina) |

Criterios de exclusión: epilepsia fotosensible, problemas de equilibrio o haberse mareado mucho antes con VR, o que el visor no se pueda ajustar a sus lentes.

## 3. Consentimiento y datos

- **Menores de edad:** autorización firmada del padre, madre o apoderado **y** asentimiento del estudiante. Si es en un colegio, coordinar antes con la dirección.
- **Mayores de edad:** consentimiento informado.
- Explicar que se evalúa **la aplicación, no a la persona**, que pueden parar en cualquier momento y que la participación es voluntaria.
- **Anonimato:** a cada participante se le asigna un código (P01, P02…). El JSON del juego no guarda nombres; en el CSV se anota solo el código y el nombre del archivo de la sesión. No grabar caras; si se graba video del visor, solo la vista del juego.

## 4. Materiales

- Meta Quest con la última APK instalada y cargado (cada sesión dura ~20 min).
- Toallitas para limpiar el visor entre participantes y un separador de lentes si hace falta.
- Espacio despejado de 2 x 2 m con el guardián configurado y una silla (modo sentado).
- PC con `adb` y el cable USB para sacar los datos.
- Esta guía impresa, la hoja de observación (sección 8) y el cuestionario (sección 7) en papel o formulario.
- Opcional: OVR Metrics Tool activado para comparar los FPS.

## 5. Preparación (antes de cada participante)

1. En el panel ☰ del juego, **NUEVA PARTIDA** (así empieza desde la introducción y responde las preguntas del principio).
2. Comprobar el volumen y el ajuste del visor.
3. Anotar la hora de inicio en la hoja de observación.

## 6. Procedimiento (≈ 35-40 min por persona, o dos sesiones)

| Momento | Duración | Qué se hace |
|---|---|---|
| Bienvenida | 2 min | Explicar el propósito y pedir el consentimiento/asentimiento. No explicar las misiones ni los controles: eso lo hace el juego |
| Ajuste del visor | 1-2 min | Ponerse el visor y los mandos; ajustar correas y distancia entre lentes |
| Orientación (en el juego) | ≤ 2 min | Aviso de inicio, tutorial corto e introducción de Baymax. El facilitador **no ayuda** salvo por seguridad |
| Misión 1 | 5-8 min | Del banco de muestras a identificar la MUESTRA A en la terminal (cartel MISIÓN 1 COMPLETA) |
| Misión 2 | 5-8 min | De elegir la estrategia a explicar el resultado en CONCLUSIONES (MISIÓN 2 COMPLETA) |
| Pausa (opcional) | 2-3 min | Quitarse el visor si lo necesita; la partida se guarda sola (checkpoint de la misión 2) |
| Misión 3 | 10-15 min | Laboratorio de vacunas, con las preguntas de antes y de después |
| Cuestionario | 3 min | Fuera del visor (sección 7) |
| Entrevista corta | 3 min | 3 preguntas abiertas (sección 7) |

**Si no da el tiempo** (clase de 45 min con varios estudiantes): una sesión para las misiones 1 y 2 y otra para la 3, continuando la partida del mismo visor. El análisis es por misión, así que vale igual.

**Reglas del facilitador:**

- Solo interviene si el participante lo pide dos veces seguidas, está atascado más de 2 minutos o hay un riesgo físico. Cada intervención se anota (hora, paso y qué se dijo).
- No usa palabras que den la respuesta ("es un hongo", "ponla en el microscopio", "es la espiga"). Puede decir: "¿qué dice tu objetivo? (botón Y)", "sigue las flechas", "prueba el botón PISTA / AYUDA".
- **Detener la sesión** si el participante dice que se marea, está pálido o sudoroso. Anotar el paso en el que se detuvo.

## 7. Cuestionario (Happiness) y entrevista

Escala de 1 (totalmente en desacuerdo) a 5 (totalmente de acuerdo), salvo el mareo:

| Columna del CSV | Afirmación |
|---|---|
| `claridad` | Siempre supe qué tenía que hacer después |
| `utilidad` | Me ayudó a entender cómo se crea y se prueba una vacuna |
| `motivacion` | Me gustaría volver a usar NanoLab con otras misiones |
| `comodidad` | Me sentí cómodo/a con el visor y los mandos |
| `mareo` | ¿Cuánto mareo o malestar sentiste? (1 = nada, 5 = mucho) |

Entrevista (anotar textual en `comentario`):

1. ¿Qué fue lo más difícil y por qué?
2. ¿En qué se diferenciaban la bacteria, el hongo y el virus al microscopio? ¿Por qué el antibiótico no serviría contra el virus?
3. ¿Qué cambiarías?

Las respuestas se pasan a `Herramientas/plantilla_likert.csv` (una fila por participante, con el nombre del `sesion_*.json` de su sesión en la columna `sesion`).

## 8. Hoja de observación (una por participante)

| Dato | Valor |
|---|---|
| Código / perfil | P__ · secundaria / universidad |
| Hora de inicio y de fin | |
| ¿Completó cada misión? | 1: sí / no · 2: sí / no · 3: sí / no (paso en el que paró: __) |
| Intervenciones del facilitador | hora · paso · qué se dijo |
| Pasos donde dudó más de 1 min | |
| ¿Usó AYUDA? ¿PASO A PASO? | |
| ¿Miró las muestras B y C sin indicación? ¿Puso la placa de la vacuna en el microscopio sin indicación? | sí / no · sí / no |
| ¿Encontró la cámara experimental y el panel CONCLUSIONES siguiendo las flechas? | sí / no |
| Problemas técnicos (agarre, encaje, texto ilegible, tirones) | |
| Signos de mareo | |

## 9. Datos del juego

El juego guarda en el visor, sin hacer nada:

- Un `sesion_<fecha>.json` **por misión** al terminarla (o al reiniciarla): `caso` = `MISIÓN 1/2/3`, tiempos por etapa, errores por tipo, ayudas, microscopio y comparación, indagación, tiempo inactivo, tiempo hasta empezar, comprensión (clasificación, explicación o preguntas antes/después) y rendimiento de ese tramo.
- `rendimiento_<fecha>.json` al quitarse el visor o salir de la app: FPS de todo el recorrido.

Al terminar la jornada:

```bash
adb pull /sdcard/Android/data/com.utec.NanoLab/files/NanoLab ./datos_piloto
python3 Herramientas/analizar_metricas.py ./datos_piloto --likert likert.csv --salida Docs/Resultados_Piloto.md
```

El script escribe, **para cada misión**, la tabla por sesión y si se cumple cada meta HEART (80 % de los participantes):

| Dim. | Meta |
|---|---|
| H | Likert ≥ 4 en claridad, utilidad y motivación; mareo ≤ 2 |
| E | Inactivo < 20 % y ≥ 3 acciones de indagación |
| A | Empieza la misión en ≤ 2 min y la completa |
| R | Misión 1: clasifica las 3 muestras a la primera · Misión 2: explica el resultado a la primera · Misión 3: ≥ 80 % en las preguntas del final |
| T | Completa la misión 1 y la 2 en ≤ 8 min cada una y la 3 en ≤ 15 min, con ≤ 2 ayudas y con la comparación del microscopio |

## 10. Después del piloto

- Pegar el informe generado en el documento de la entrega E6 y comentarlo: qué metas se cumplieron, en qué pasos se atascaron (tabla de errores por paso) y qué se cambiaría.
- Con 5-8 personas los resultados son **indicativos**, no estadísticamente concluyentes: decirlo en el informe.
