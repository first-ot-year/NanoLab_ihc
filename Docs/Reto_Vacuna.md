# MISIÓN 3 · Crea la vacuna (Laboratorio de vacunas)

**Tercera misión** del **Centro de Experimentación** (antes se llamaba CASO 01); se **desbloquea al terminar la MISIÓN 2** ([Misiones_1_y_2.md](Misiones_1_y_2.md)). El estudiante (Baymax) **fabrica con sus manos una vacuna de ARN mensajero** contra un coronavirus respiratorio (modelo educativo basado en el SARS-CoV-2): elige el antígeno, diseña el ARNm, lo sintetiza, lo envuelve en nanopartículas lipídicas, pasa el control de calidad y la cadena de frío, comprueba en un ensayo de neutralización que genera anticuerpos y termina con las fases clínicas y la eficacia. Al resolverla, Baymax recupera su **tercer recuerdo científico** (3/3: los dos primeros los dan las misiones 1 y 2) y se guarda un **checkpoint**. Antes del paso 1 y al terminar responde las mismas **4 preguntas de comprensión** (no cuentan como error) para medir lo aprendido (HEART · Retention).

> Modelo educativo simplificado: cantidades, tiempos y resultados son ilustrativos y no representan un protocolo real de fabricación ni de ensayo (RNF-06).

## Objetivos de aprendizaje

Al terminar, el estudiante debería poder:

1. Explicar qué es un **antígeno** y por qué la proteína espiga es un buen antígeno vacunal.
2. Nombrar y ordenar las 5 partes de un **ARNm** (CAP 5' > UTR 5' > gen > UTR 3' > cola poli(A)) y decir para qué sirve cada una.
3. Explicar la **transcripción in vitro**: molde de ADN + ARN polimerasa T7 + nucleótidos (y por qué no sirven la ADN polimerasa ni los ribosomas).
4. Explicar por qué el ARNm va dentro de **nanopartículas lipídicas** y qué hace cada uno de sus 4 lípidos (y por qué un detergente las destruiría).
5. Justificar el **control de calidad** de cada lote y la **cadena de frío** (-70 °C).
6. Describir un **ensayo de neutralización** y su orden lógico (suero > virus > incubar > células > teñir), y leer un **título** de anticuerpos usando los **controles**.
7. Ordenar las **fases clínicas** (I > II > III > aprobación > IV).
8. Interpretar la **eficacia** sin fórmulas: comparar las barras, pasar a "de cada 10" (9 se salvan), llegar al 90 % y decidir si se aprueba.
9. Explicar por qué un **microscopio óptico no ve el virus** (~100 nm, por debajo del límite de ~200 nm) y reconocer su **efecto en las células** comparando un pocillo CONTROL (antes) con uno PLACEBO (después).

## Dónde está

Se monta sola al iniciar la escena (`AdaptadorEscena` > `MontajeVacuna.Asegurar`), sin volver a montar la escena ni hornear la luz, en el **centro de la sala** (x 15.5, z 8.5, donde estaba la cámara experimental), de cara a la puerta de entrada. Si ese sitio estuviera ocupado, usa el primer hueco libre (comprueba colisiones).

**Antes de montarse** aparta la cámara experimental, que estaba en ese sitio (`MontajeMisiones.AntesDeLaVacuna`), y **después** prepara las misiones 1 y 2 con el resto del mobiliario del centro y reubica la cámara en un hueco libre (`MontajeMisiones.DespuesDeLaVacuna`). Mientras la misión 2 no esté terminada, la estación muestra **MISIÓN BLOQUEADA** y CONTINUAR no deja empezar (orden fijo). Los recuerdos de Baymax son **0/3** (`RecuerdosBaymax.Total`; si la escena guardada solo trae una ranura en el indicador, `IndicadorRecuerdos` crea las que faltan).

La estación tiene un **mostrador con 4 mesas**, de izquierda a derecha (vistas por el estudiante) en el orden del proceso. Una **franja de luz** en el borde del mostrador marca la mesa del paso actual, y en cada paso solo aparecen las piezas que se usan.

| Mesa | Qué hay |
|---|---|
| **DISEÑO** (izquierda) | Molde de 5 huecos (5' > 3') y bandeja con 6 bloques. En el paso 6, la placa de cultivo y el botón INCUBAR 1 h. En el paso 7, el **microscopio invertido con monitor** (donde estaba la bandeja) |
| **SÍNTESIS** | Gradilla con 5 frascos (cambian según el paso), pipeta en su soporte, termobloque con botón INCUBAR y tubo de reacción |
| **NANOPARTÍCULAS** | Mezclador microfluídico (entrada de ARNm + 4 entradas de lípidos + chip), botón MEZCLAR, gradilla con 5 cartuchos y bandeja de SALIDA |
| **CALIDAD Y FRÍO** (derecha) | Analizador, ultracongelador (-70 °C) y nevera (2-8 °C) con **puerta que se abre** al acercar el vial (el vial entra con animación y la puerta se cierra; se ve por la ventana), y mesa (25 °C). Botones AYUDA y CONTINUAR |

Encima: pantalla del caso (izquierda), pantalla de visualización (derecha) y barra de 9 pasos. A los lados: tablero **GUÍA** (cómo se crea una vacuna de ARNm) y **CONCEPTOS CLAVE**.

## Cómo se juega

- **Agarrar**: grip (o el rayo corto de agarre). Al soltar cerca de una **silueta brillante**, la pieza encaja sola; si se cae o se pierde, vuelve a su sitio (RNF-04).
- **Pipeta**: con ella en la mano, punta junto a un frasco + **gatillo (o A/X)** = cargar; punta junto al tubo o la placa + gatillo = descargar. Es el mismo gesto que el portaobjetos del microscopio.
- **Botones**: son botones **poke del Interaction SDK de Meta** (la tapa se hunde con la punta del dedo o del mando y cambia de color al acercarse y al pulsar). También se pulsan con el rayo de la mano vacía + gatillo.
- **Virus holográfico** (paso ANTÍGENO): está sobre el mostrador, con la mitad delantera cortada para ver el interior. Se **toca** una parte con la mano (o se apunta con el rayo) y se **gira con el grip**; la pantalla derecha muestra solo la clave de colores.
- **AYUDA** (amarillo, `?`): una bola fantasma **hace el gesto** de la acción actual 3 veces (de dónde a dónde, con el icono del botón del mando), llevando una **copia transparente del objeto** y con rótulos sencillos que cambian durante la animación ("COGE ESTO" > "LLÉVALO…" > "PONLO AQUÍ"); un anillo amarillo marca el destino. Además deja una pista corta en el panel.
- **PASO A PASO** (violeta, a la izquierda del mostrador): modo guiado "como a un niño". La animación de la acción actual **se repite sin parar** (coge esto > llévalo > ponlo aquí) y cambia sola a la siguiente acción al completarla (se pausa mientras trabaja una máquina). El panel deja **una sola acción en grande**, con su icono y una frase sencilla ("COGE el tubo y PONLO en el termobloque"). En los pasos de manipulación guía **pieza a pieza y en el orden real**: "COGE CAP 5' > HUECO 1", luego "COGE UTR 5' > HUECO 2"…; en síntesis, cada frasco y el tubo; en nanopartículas, cada cartucho correcto; en calidad, el ultracongelador; en el ensayo, cada reactivo en su orden. Si algo está mal colocado, primero enseña a SACARLO. En las preguntas de decisión (antígeno, título, fases, eficacia) señala dónde mirar, sin dar la respuesta. Se activa y desactiva con el mismo botón (LED encendido = activo).
- **CONTINUAR** (icono `>>`): gris mientras el paso no está hecho y **verde con LED y halo** cuando ya se puede avanzar. Si se pulsa antes de tiempo no suena a error: recuerda qué falta y enseña el gesto. **REPETIR** (icono circular): en el informe final.
- **Botones**: poke de Meta metido en una **carcasa** apoyada en el mostrador (solo asoman 5 mm de tapa, con el LED y el halo encima); se hunden con el dedo (SDK) y también al pulsarlos con el rayo o el gatillo (bajan 6 mm y vuelven). Los carteles de AYUDA, CONTINUAR y las opciones van sobre un **atril** apoyado en el mostrador.
- Un error **no penaliza**: explica por qué y se puede reintentar. Tras dos errores en un paso se sugiere AYUDA.
- **Apartado "AHORA"**: panel **fijo** en la estación, bajo la pantalla del caso (ya no sigue la vista). Muestra **siempre un solo texto corto** —la acción actual— con el icono del botón del mando que hay que usar.
- **Microscopio invertido** (paso 7): se coge la **placa** de la mesa de DISEÑO (grip) y se suelta sobre la **platina** del microscopio; encaja sola y se enciende su **monitor**, que compara un pocillo **ANTES · CONTROL** (sin virus: capa continua de células sanas) con uno **DESPUÉS · PLACEBO** (el virus las destruyó: células redondas, sueltas y huecos). Es un microscopio de cultivo celular con cámara: no hay que acercar la cara al ocular. Las micrografías son ilustrativas y se generan por código (`MicrografiasCultivo`).
- **Preguntas de comprensión**: al pulsar CONTINUAR en la bienvenida salen **4 preguntas rápidas** (antígeno, qué lleva una vacuna de ARNm, por qué no se ve el virus al microscopio, qué significa 90 % de eficacia) con las letras del mostrador; no se corrigen ni cuentan como error. Después de EFICACIA salen **las mismas**, esta vez con la explicación de cada respuesta, y el informe compara "al principio" y "al final". Al REPETIR el caso solo se hacen las del final.
- **Recuerdo de Baymax**: al resolver la misión aparece el cartel con el tercer recuerdo (3/3) y se guarda el checkpoint.

### Guía del paso (interacción e inmersión)

- **Al pulsar CONTINUAR**, aparece un cartel del paso delante de la vista y la **pantalla derecha presenta lo que sigue**: número y nombre del paso, en qué mesa se hace y el **plan de acciones, que se va agregando línea a línea**. A los pocos segundos (o en cuanto el estudiante empieza a trabajar) vuelve el diagrama del paso.
- **TU PLAN** (pantalla izquierda): la lista de acciones del paso se marca en vivo: `[OK]` en verde lo hecho, la acción actual en amarillo y el resto atenuado. Al terminar, el estado dice cuál es el paso siguiente.
- **Flecha amarilla** flotando sobre el objeto de la acción actual (la pipeta, el frasco, el tubo, el termobloque, el botón…), con el texto de la acción orientado hacia el estudiante. Salta al siguiente objeto con un clic y una vibración al completar cada acción. No señala la respuesta (por ejemplo, en el almacén apunta al grupo, no al ultracongelador).

### Legibilidad

- **Letra Nunito SemiBold** (redondeada y amigable, licencia OFL) en todos los textos 3D; las letras de la estación son un 12 % más grandes (los textos con autoajuste se reducen solos si no caben).
- **Bordes finos y simétricos**: 3 mm como máximo y el mismo radio de esquina en todos los paneles.
- **Pantallas más pequeñas y más bajas** (escala 0,82, centro a 1,62 m) y cabecera algo más baja.
- **Sala principal sin paneles de borde celeste**: se ocultan al empezar los tableros sueltos del recibidor (introducción, tutorial, progreso, rótulos de áreas). Se mantienen los paneles que forman parte de un equipo (autoclave, portal, incubadora, ultracongelador) o de una estación con función. La bienvenida es el aviso delante de la vista, con botones poke cuya etiqueta dice lo que pasa: **Ya lo veo bien ›** (casco ajustado), **Siguiente ›** en el tutorial y **Empezar a jugar** al final, más **‹ Atrás** para volver a la tarjeta anterior. Atajos: X/A = siguiente, Y/B = atrás.


Los paneles de los tableros son transparentes, igual que el texto. Unity los ordenaba por distancia a la cámara y, según desde dónde se mirara, dibujaba el panel encima del texto (letras tenues o que desaparecían al alejarse). Ahora el texto se dibuja siempre después de su panel (`sortingOrder` 5): en la estación (`KitVacuna.Texto`) y en **todos los tableros de la escena** (`AdaptadorEscena.TextosAlFrente`, al empezar). La cabecera es más grande, está inclinada hacia el usuario y los tamaños mínimos de texto son mayores.

## Los 9 pasos

| # | Paso | Interacción | Respuesta | Error típico y qué se aprende |
|---|---|---|---|---|
| 1 | **Antígeno** | Rayo a un rótulo del virus 3D | **Espiga S** | ARN / proteína N están dentro; la envoltura es membrana nuestra |
| 2 | **Diseño del ARNm** | Encajar 5 bloques en el molde | **CAP 5' > UTR 5' > GEN S-2P > UTR 3' > COLA POLI(A)** | Poner el GEN N (antígeno equivocado) o un bloque fuera de orden: la pantalla marca OK / X por hueco |
| 3 | **Síntesis** | Pipetear 3 reactivos al tubo, tubo al termobloque, INCUBAR | **Molde ADN + T7 ARN pol + nucleótidos** | ADN polimerasa (copia ADN) o ribosomas (fabrican proteína) |
| 4 | **Nanopartículas** | Encajar 4 cartuchos y el tubo en el mezclador, MEZCLAR | **Ionizable + fosfolípido + colesterol + PEG** | El detergente destruye las membranas de lípidos (vuelve solo a la gradilla) |
| 5 | **Calidad y frío** | Vial al analizador; luego a un almacén | **Ultracongelador -70 °C** | Nevera (aguanta días) o mesa (se degrada en horas); guardar sin analizar |
| 6 | **Preclínica** | Pipetear a la placa + botón INCUBAR 1 h | **Suero > virus > incubar > células > cristal violeta** | Células antes de incubar: **placa descartada**, se empieza de nuevo |
| 7 | **Microscopio y título** | Placa a la platina del microscopio; 2 preguntas sobre el monitor (botones A-C); rayo a una dilución de la placa ampliada | **El virus destruyó las células del placebo** > **el virus es demasiado pequeño para verlo** > **1:320** | Creer que se ven los virus o que el placebo está igual que el control; diluciones que protegen pero no son la más alta, o pocillos destruidos. Elegir la dilución antes de mirar al microscopio no es un error: recuerda qué falta |
| 8 | **Fases clínicas** | Botones A-E en orden | **I > II > III > aprobación > IV** | Empezar con muchas personas o vigilar antes de aprobar |
| 9 | **Eficacia** | 4 preguntas sencillas con los botones (mirando las barras) | **Vacuna** (enfermó menos) > **9 de cada 10** se salvan > **90 %** > **Aprobar** | Placebo / igual; 1, 5 o 10 de cada 10; 10 % (los que enferman), 99,9 % (no enfermaron), 135 (resta); rechazar o repetir el ensayo |

**Después del paso 9:** las mismas 4 preguntas de comprensión del principio, con su explicación.

**Cierre:** informe con lo aprendido, "y en el brazo…" (cómo actúa la vacuna en el cuerpo: el ARNm se degrada en días y no entra en el núcleo), las decisiones (aciertos, errores y recuerdos), pistas, tiempo y aciertos de las preguntas (al principio > al final); tercer recuerdo científico (3/3) y checkpoint.

## Trazabilidad con los requisitos

| Requisito | Cómo lo cumple el reto |
|---|---|
| RF-01 Introducción y orientación | Bienvenida al llegar a la estación, cartel de cada paso, apartado AHORA y franja de luz en la mesa del paso |
| RF-02 Selección de la muestra | El antígeno se elige en el virus holográfico (paso 1). *El banco de muestras del caso antiguo queda como mobiliario: el caso de la vacuna no tiene un paso de elegir muestra en el banco* |
| RF-03 Microscopía inicial | Paso 7: la placa se pone en la platina del microscopio invertido y su monitor muestra el pocillo CONTROL (células sanas, ANTES) |
| RF-04 Investigación guiada | Tableros GUÍA y CONCEPTOS CLAVE; explicación de cada pieza al colocarla |
| RF-05 Hipótesis y estrategia | Antígeno y diseño del ARNm |
| RF-06 / RF-07 Preparación y experimento | Síntesis, formulación y ensayo con piezas físicas, pipeta y máquinas con botón |
| RF-08 Comparación antes/después | Monitor del microscopio: ANTES (CONTROL, sin virus) junto a DESPUÉS (PLACEBO, con virus) y 2 preguntas sobre lo que se ve; al elegir el título, el monitor muestra el pocillo VACUNADO de esa dilución. Además, filas VACUNADO / PLACEBO / CONTROL de la placa y vacuna frente a placebo en la fase III |
| RF-09 Feedback, progreso y reintento | Explicación de cada error, pistas graduales, barra de 9 pasos, sin penalización; tercer recuerdo de Baymax (3/3) |
| RF-10 Métricas | JSON de sesión (`MetricsLogger`, caso `MISIÓN 3`): tiempos por paso, decisiones, errores por paso, pistas, PASO A PASO, observaciones al microscopio y comparación, acciones de indagación, tiempo inactivo, tiempo hasta empezar, preguntas de comprensión (antes / después) y rendimiento. `Herramientas/analizar_metricas.py` lo resume en la tabla HEART |
| RNF-01 Confort | Todo de pie (o sentado) frente al mostrador; solo pasos laterales cortos |
| RNF-03 Claridad visual | Color siempre con texto: rótulos en cada pieza, OK / X en el diseño, X en los pocillos, prefijos CORRECTO / AÚN NO / ALTO / PISTA |
| RNF-04 Tolerancia al error | Siluetas de encaje, retorno automático, la pieza equivocada vuelve sola, errores reversibles |
| RNF-05 Consistencia | Agarre con grip y pipeta con gatillo, como el resto del laboratorio |
| RNF-06 Seguridad pedagógica | Aviso de modelo simplificado en la introducción y en CONCEPTOS CLAVE |
| RNF-08 Rendimiento | Solo se muestran las piezas del paso actual (el microscopio solo en el paso 7); mallas del virus combinadas; sin luces nuevas; micrografías de 256 px generadas una sola vez. `MedidorRendimiento` mide los FPS en el visor y los guarda en el JSON |

## Probar sin visor

- Menú **`NanoLab/Probar reto de vacuna (play mode)`**: recorre el caso entero con las mismas acciones del estudiante (encajar piezas, pipetear, pulsar botones), comete un error típico en cada paso y luego lo resuelve. Comprueba también que el reto anterior queda oculto. Escribe `Logs/prueba_vacuna.txt`, dos capturas por paso en `Logs/Capturas/vacuna_*.png` (vista del estudiante y `_mesa`) y una vista general del centro (`vacuna_0_centro.png`). Si la escena ya está abierta no se recarga (no se pierden cambios sin guardar).
- En Play Mode: **F7** resuelve el paso actual (o pulsa CONTINUAR) y **F6** comete el error típico del paso. En las preguntas de comprensión, **F7** responde bien y **F6** responde la opción A.
- Resultado esperado de la prueba automática: `RESULTADO: CORRECTO` con unos **46 OK** (uno más si la misión estaba bloqueada al empezar; 3 menos si en esa partida ya se respondieron las preguntas del principio). Comprueba también que sin la misión 2 la vacuna no deja empezar (la prueba la desbloquea para recorrerla), que las misiones 1 y 2 y la cámara reubicada están listas, las preguntas de comprensión, el microscopio, el recuerdo 3/3 y las métricas.
- El agarre físico con el mando se prueba en el visor o con el Meta XR Simulator ([Simulador_PC.md](Simulador_PC.md)).

## Archivos

| Archivo | Qué hace |
|---|---|
| `Scripts/Centro/ContenidoVacuna.cs` | Todo el texto educativo: piezas, reactivos, explicaciones, errores, pistas, guía y conceptos |
| `Scripts/Centro/RetoVacuna.cs` | Lógica del caso: pasos, respuestas, errores, pistas, microscopio (paso 7), preguntas de comprensión, informe, bloqueo hasta la misión 2, recuerdo 3/3, checkpoint, métricas y pruebas |
| `Scripts/Centro/MicroscopioVacuna.cs` | Microscopio invertido con monitor (platina, comparación ANTES / DESPUÉS) y `MicrografiasCultivo`, que genera las micrografías ilustrativas de células sanas y destruidas |
| `Scripts/Centro/MetricsLogger.cs` | JSON de la sesión con los indicadores HEART |
| `Scripts/Microbiologia/MedidorRendimiento.cs` | FPS y tiempo de fotograma de toda la sesión (RNF-08 / RNF-02) |
| `Scripts/Centro/MontajeVacuna.cs` | Oculta el reto anterior y monta la estación al empezar (centro de la sala, mostrador, 4 mesas, máquinas, piezas, pantallas, tableros y señales) |
| `Scripts/Centro/InteraccionVacuna.cs` | Agarrables creados en tiempo de ejecución (Meta Interaction SDK inyectado por código), zonas de encaje, pipeta y selectores con rayo |
| `Scripts/Centro/DiagramasVacuna.cs` | Diagramas de fabricación: ARNm, transcripción, nanopartícula y control de calidad |
| `Scripts/Centro/VisualesVacuna.cs` | Virus 3D, placa de neutralización, línea de fases y gráfica de eficacia |
| `Scripts/Centro/KitVacuna.cs` | Piezas, materiales, textos y paneles creados en tiempo de ejecución |
| `Scripts/Centro/GuiaVacuna.cs` | Apartado fijo "AHORA" (un texto + icono), animación guiada de AYUDA / PASO A PASO (copia fantasma del objeto y rótulos) y control del virus holográfico (tocar y girar) |
| `Scripts/Microbiologia/EstiloNanoLab.cs` | Letra Nunito, iconos del mando y plantilla de los botones poke de Meta |
| `Resources/NanoLab/Fuentes`, `Resources/NanoLab/Iconos` | Nunito SemiBold y los iconos (gatillo, grip, botón, rayo, pulsar, siguiente, repetir) |
| `Editor/PruebaVacuna.cs` | Prueba de punta a punta en play mode (también fotografía la sala principal desde el punto de aparición) |
