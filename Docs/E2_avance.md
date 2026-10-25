# E2 (30 %) · Entorno VR, introducción de Baymax y selección de muestra

Entregable 2 del cronograma del informe del hito 1 (Cuadro 6, semana 11):

> Configurar el entorno de Unity y la interacción básica en realidad virtual. Incorporar la introducción de la experiencia, en la que el estudiante asume el papel de Baymax, e implementar la selección de una muestra. **Salida:** el usuario inicia la experiencia y selecciona una muestra en el laboratorio.
> RF asociado: RF-01 · RNF asociados: RNF-01, RNF-04.

## Checklist de salida

| # | Criterio | Cómo se cumple | Evidencia | Estado |
|---|---|---|---|---|
| 1 | El entorno Unity está configurado para VR | Unity 2022.3.62f3, Meta XR SDK 201 + Interaction SDK, Oculus XR Plugin, plataforma Android/Quest | `Packages/manifest.json`, `ProjectSettings/` | ✅ |
| 2 | **La experiencia arranca en el laboratorio de microbiología** | `Microbiologia.unity` es la escena 0 de la build (Química es la 1 y se quitó `SampleScene`) | `ProjectSettings/EditorBuildSettings.asset` | ✅ |
| 3 | Hay interacción VR básica | Agarre con mandos y manos (Grabbable, HandGrab, DistanceGrab), botones físicos, zonas de encaje | `ConfigurarMetaISDK`; `NanoLab/Probar física de los objetos agarrables` | ✅ |
| 4 | **RF-01:** el estudiante asume el papel de Baymax | 1) Avatar Baby Baymax del jugador: manos en primera persona y cuerpo completo en el espejo. 2) Tótem **"ERES BAYMAX"** frente al punto de aparición: perdiste tus recuerdos científicos; "mírate en el espejo de tu derecha"; dónde ir (puestos, cepario, zona de alto riesgo, Centro → banco de muestras); controles; aviso de contenido simplificado. 3) Indicador **Recuerdos científicos 0/3**. 4) La narrativa del centro y del archivo habla ahora de los recuerdos de Baymax | `Logs/Capturas/micro_0_intro.png`, `avatar_*_espejo.png`; PR "Introducción con indicador de recuerdos" | ✅ (validar en VR) |
| 5 | **Selección de una muestra** | Banco de muestras (estación 1) con el CASO 01 activo y los CASOS 02 y 03 bloqueados. En la sala principal también se eligen muestras en el cepario (bacterias) y en la zona de alto riesgo (virus) | `centro_3_banco.png`; PR "Frotis del CASO 01 cargado" | ✅ |
| 6 | **RNF-01** confort | Velocidad constante de 1.5 m/s sin rampa (90 % en 0.14 s, parado en 0.11 s), zona muerta y respuesta progresiva del joystick, giro por pasos de 30°, viñeta de Meta, sin teletransporte. Se documenta como ajuste justificado frente a "estación fija" (E1 §4.2) | `Logs/prueba_locomocion.txt` | ✅ (validar el mareo en VR) |
| 7 | **RNF-04** tolerancia al error | Portaobjetos, placas, viales y cápsulas vuelven solos a su sitio si se caen; zonas de encaje amplias; desinfectante para rehacer el frotis | PR "Un portaobjetos caído vuelve a su sitio en 1.5 s (RNF-04)" | ✅ |

## Cambios de esta entrega

- `Assets/NanoLab/Editor/MontarIntroduccion.cs`:
  - Tótem de introducción (menú `NanoLab/Montar introducción Baymax`).
  - Orden de las escenas de build.
- `Assets/NanoLab/Scripts/Microbiologia/IndicadorRecuerdos.cs`: `RecuerdosBaymax` (contador) e `IndicadorRecuerdos` (panel).
- `RetoManager`, `PanelResumen`, `RetoData`, `MontarCentroExperimentacion`: "Archivo Científico/fragmentos" pasa a ser "recuerdos científicos de Baymax".
- `Portaobjetos`: vuelve a su sitio si se cae al suelo y conserva la muestra.
- `JugadorNanoLab` / `ConfigurarMetaISDK`: velocidad de 1.2 a 1.5 m/s, sin rampa de aceleración y respuesta del joystick menos lenta a media inclinación.
- Pruebas:
  - `PruebaReto` añade 4 comprobaciones: introducción, recuperación de un portaobjetos y dos de recuerdos.
  - `PruebaLocomocion` comprueba que no haya rampa.
  - `ProbarRecorrido` añade la ruta entrada → espejo.

## Resultados de verificación (batchmode)

| Prueba | Resultado |
|---|---|
| `NanoLab/Probar reto completo` | CORRECTO: 35 comprobaciones OK, 0 excepciones |
| `NanoLab/Probar locomoción` | CORRECTO: 1.5 m/s, 90 % en 0.14 s, frenado en 0.11 s, giro por pasos de 30°, sin teletransporte |
| `NanoLab/Probar recorrido del jugador` | Todas las rutas son transitables (incluida entrada → introducción → espejo) |

## Validación pendiente en Meta Quest

1. La app arranca en Microbiología y el tótem "ERES BAYMAX" se ve al aparecer.
2. El espejo de la derecha muestra a Baymax y sigue la cabeza y las manos.
3. El jugador llega al Centro, toma la placa del CASO 01 en el banco y, al soltarla en el suelo, vuelve a su ranura.
4. La caminata se siente natural y sin mareo; el giro de 30° es cómodo.
5. Se apunta cualquier caída de FPS (el perfilado formal queda para E5).
