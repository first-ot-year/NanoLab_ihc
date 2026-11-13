# Evento sorpresa de bioseguridad (MISIÓN 2)

Al terminar el **primer experimento** de la cámara experimental (7), falla el sellado de su compuerta y salen aerosoles de *S. aureus*. El laboratorio entra en alarma y la misión 2 se detiene hasta que el estudiante encuentra las zonas contaminadas y las limpia con el pulverizador.

## Cómo se juega

1. **Alarma.** Se encienden las luces rojas intermitentes (unas 0,9 veces por segundo) y las balizas de los equipos, suena un pitido que se va espaciando y aparece el aviso de ALERTA DE BIOSEGURIDAD.
2. **Kit de derrames.** La tarjeta de objetivo, las flechas del suelo y el cartel AHORA llevan al KIT DE DERRAMES. Está entre la INCUBADORA (9) y el AUTOCLAVE (11).
3. **Pulverizador.** Se agarra con el grip, como cualquier otro objeto del juego. Mientras se aprieta el **gatillo** de esa mano, sale niebla, suena el rociado y vibra el mando.
4. **Zonas contaminadas.** Son entre 2 y 4 manchas con un anillo rojo que late, en el suelo y en las mesas que rodean la cámara. Las flechas llevan a la más cercana. Al rociarlas pierden opacidad poco a poco (unos 1,2 s rociando de lleno), se desvanecen y se desactivan. No se puede rociar a través de una mesa ni de una pared.
5. **Fin de la alarma.** Cuando ya no quedan zonas, las luces vuelven a su estado normal y Baymax felicita al estudiante con un clip de audio y un mensaje. La misión 2 sigue donde estaba: analizar el portaobjetos DESPUÉS en el microscopio.

Mientras dura la alarma están bloqueados el microscopio (para el portaobjetos DESPUÉS), la terminal, la prensa, la cámara y el panel CONCLUSIONES. Cada uno explica qué hacer antes. El evento pasa **una vez por partida**: queda guardado en la partida y vuelve a pasar después de pulsar REINICIAR en el ARCHIVO (8).

## Archivos y cambios

| Archivo | Cambio |
|---|---|
| `Scripts/Centro/BiohazardEvent.cs` (nuevo) | `BiohazardEvent`: estados Inactivo → Alarma → Resuelto, corrutinas `Parpadeo`, `Sirena` y `VolverANormal`, creación de las zonas, `Rociar` (un SphereCast hacia delante más un OverlapSphere en la boquilla), guías y felicitación. `IluminacionAlarma`: gestor de las luces de alarma. `Contaminacion`: la mancha que pierde opacidad. `Pulverizador`: el spray agarrable con su gatillo |
| `Editor/ConfigurarBiohazard.cs` (nuevo) | Crea la etiqueta **Contaminated** y la capa **Biohazard** (capa 9) en `ProjectSettings/TagManager.asset` al abrir o recompilar el proyecto. También añade los menús *NanoLab › Bioseguridad: crear etiqueta y capa* y *NanoLab › Bioseguridad: lanzar el evento (play mode)* |
| `Scripts/Centro/RetoManager.cs` | `ResultadoExperimento` dispara el evento en el primer intento. `Bloqueado()` y `AvisarBloqueo()` detienen la misión 2: los usan `PuedeHipotesis`, `PuedeFormular`, `PuedeExperimentar`, el microscopio DESPUÉS y CONCLUSIONES. `ObjetivoActual` y `EstacionActual` llevan la guía al kit y luego a las zonas. `BiohazardCambio` y `BiohazardResuelto` refrescan la guía y guardan las métricas. `Reiniciar` llama a `BiohazardEvent.Reiniciar()` |
| `Scripts/Centro/MontajeMisiones.cs` | Llama a `BiohazardEvent.Montar(centro)` justo después de montar los equipos del centro |
| `Scripts/Centro/MetricsLogger.cs` | Nuevo bloque `bioseguridad` en el JSON de la misión 2: `segundosHastaSpray`, `segundosLimpieza`, `zonas` y `minuto` |
| `Scripts/Microbiologia/Partida.cs` | Nuevo campo `biohazard`, que indica si el evento ya se resolvió |
| `Scripts/Centro/DebugReto.cs` | Prueba del evento completo (14 comprobaciones). Corre dentro de *NanoLab › Probar misiones 1 y 2* y de *Capturas paso a paso* |
| `Editor/CapturasPasoAPaso.cs` | Capturas `biohazard_1_alarma` … `biohazard_5_resuelto` |
| `ProjectSettings/TagManager.asset` | Etiqueta `Contaminated` y capa `Biohazard`. Esta modificación se sube con el commit |

## Decisiones técnicas

- **Por qué no se usa `XRGrabInteractable` ni `ActivateEventArgs`.** El proyecto usa el **Meta XR SDK (Interaction SDK)**, no el XR Interaction Toolkit, que no está instalado. Añadirlo duplicaría los sistemas de agarre y de rayo, y entraría en conflicto con `OVRCameraRig`. Se usa lo mismo que en el resto de objetos agarrables (`AgarreRuntime.Hacer`: Grabbable, HandGrab y DistanceGrab). El gatillo se lee de la mano que sostiene el spray con `OVRInput.Get(PrimaryIndexTrigger)`. En el Editor, sin visor, la tecla **R** rocía mientras el spray está agarrado.
- **Por qué hay luces nuevas.** De las 57 luces de la escena, 55 están **horneadas** (Baked). Cambiar su color o intensidad en ejecución no hace nada. `IluminacionAlarma` crea 2 luces puntuales en tiempo real sin sombras: una que se calcula por píxel, sobre el centro de la sala, y otra sobre la cámara. Además añade balizas emisivas y tiñe de rojo las luces Realtime o Mixed que ya había. Al terminar lo restaura todo.
- **Rendimiento en Quest (RNF-08).** En Android la calidad es *Medium*, con una sola luz por píxel, y la luz extra solo existe durante el evento. Conviene medirlo en el visor con `MedidorRendimiento`.
- **Etiqueta y capa.** El rocío solo busca en la capa `Biohazard` y descarta cualquier objeto que no tenga la etiqueta `Contaminated`. Si alguna de las dos faltara, sigue funcionando gracias al componente `Contaminacion` y avisa en la consola.

## Clip de felicitación de Baymax

Para usar un audio propio, guarda el archivo como `Assets/NanoLab/Resources/Audio/BaymaxFelicitacion.wav` (también sirven .ogg o .mp3). Si no existe, suena una melodía generada por código. El clip se reproduce en la cabeza del jugador, que es Baymax.

## Probar

- *NanoLab › Probar misiones 1 y 2*: el evento salta en el primer intento de la misión 2.
- En play mode, *NanoLab › Bioseguridad: lanzar el evento* lo dispara cuando quieras. Toma el spray y mantén **R** mientras apuntas a las manchas.
