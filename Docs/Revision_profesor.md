# Revisión del profesor (14/11/2026): cambios e ideas

Todo se aplica **en tiempo de ejecución**, sin volver a montar ni hornear `Microbiologia.unity`, para no chocar con los cambios del equipo en la escena.

## Qué se cambió

| # | Comentario del profesor | Cambio | Dónde |
|---|---|---|---|
| 1 | Los marcadores de lugar sobran | Se quitan los rótulos flotantes de área (`BalizaArea`: "4 · CENTRO DE EXPERIMENTACIÓN", "CEPARIO"…) | `LimpiezaEscena.cs` |
| 2 | El indicador "Centro de experimentación" no sirve | Se quita el tótem del pasillo ("¡Nuevo! Resuelve el CASO 01") | `LimpiezaEscena.cs` |
| 3 | Las flechas y los anillos del suelo no son buena UI/UX | Se reemplazan por una **LUZ GUÍA** de Baymax (ver abajo). Fuera las 40 flechas, el anillo y el nombre flotante | `GuiaReto.cs` |
| 4 | Un microscopio más por mesa | Se clona el puesto de cada mesa de MICROSCOPÍA (3): ahora hay 2 por mesa (4 en total), en el lado libre de cada mesa | `LimpiezaEscena.cs` |
| 5 | Las pantallas al lado del microscopio no van; mejor como antes, con las características y el ZOOM | Vuelve el HUD del ocular de la versión anterior (características, micrografía, observaciones, ZOOM y X para mover la vista). Ya no se crean monitores | `HUDMicroscopio.cs`, `MicroscopioOptico.cs` |
| 6 | Los botones de la terminal confunden | **Una pregunta a la vez y 3 botones**: pregunta 1 (¿qué bacteria es?) → los mismos 3 botones pasan a la pregunta 2 (¿qué estructura atacarás?). Al cambiar de pregunta, las respuestas saltan y suena un tono. La fila 2 y los rótulos fijos se ocultan | `TerminalHipotesis.cs` |
| 7 | No se entiende por qué se usa cada cápsula y no hay sonido | Al **coger un vial** aparece su **ficha** (qué hace, qué ataca, si coincide con tu estrategia) y suena. La prensa dice "ATACA: …". La cápsula hace "pop" y vibra al formularse. La cámara suena al encajarla, al liberar el agente, cada 6 h de incubación y al final (fanfarria si funciona, caída grave si no) y **explica por qué funcionó o no** | `VialAgente.cs`, `PrensaCapsulas.cs`, `CamaraExperimental.cs`, `Sonidos.cs`, `RetoManager.cs` |
| 8 | El evento de falla de seguridad no aporta conocimiento | El evento sorpresa ya no se dispara ni se monta (sin kit, sin alarma). El código queda en `BiohazardEvent.cs` por si se reutiliza | `RetoManager.cs`, `MontajeMisiones.cs` |
| 9-10 | Después de un experimento fallido te manda al microscopio y no tiene sentido | Si **no funcionó**, la cámara explica el porqué allí mismo, no entrega el portaobjetos DESPUÉS y la guía vuelve a la TERMINAL (5). Si **funcionó**, sí se compara ANTES/DESPUÉS al microscopio (ahí se ven las células rotas: es la evidencia para explicar el resultado) | `RetoManager.cs`, `CamaraExperimental.cs` |
| 11a | El menú se queda estático y no pausa | El menú ☰ **pausa el juego** (`Time.timeScale = 0`, con el rótulo "JUEGO EN PAUSA") y **sigue al jugador** con retardo: si te giras o te mueves, vuelve suave delante de ti | `MenuAjustes.cs` |
| 11b | Lo que tiene en la mano no tiene sentido | El "brazalete" era una pantalla de 15 cm sobre el mando, dentro de la mano: parecía un móvil. Ahora es un **reloj** pequeño en la **muñeca**. Al girar la muñeca hacia la cara, sale un holograma encima del reloj con el objetivo | `BrazaleteBaymax.cs` |

**Para revisar en el visor:**
- La posición del reloj (`PosLocal` y `NormalLocal` en `BrazaleteBaymax.cs`) se calculó para el mando de Quest. Si queda desplazado de la muñeca, se ajusta ahí.
- La altura y la distancia de la luz guía están en las constantes de arriba de `GuiaReto.cs`.

## Ideas para guiar sin "señales" (punto 3)

1. **Luz guía de Baymax (implementada).** Una luz pequeña con estela nace junto al jugador en cada paso nuevo (con un tono suave) y vuela por delante de él hacia la estación. Si el jugador se para, ella también. Si la estación queda a la espalda, se asoma por el borde de la vista del lado hacia el que hay que girar. Al llegar, se funde en la estación con un destello y una vibración. Es un solo elemento en lugar de 40 flechas.
2. **Estación que "despierta".** La estación del paso se ilumina por dentro (pantallas encendidas, borde que respira). Las demás quedan en reposo, sin carteles flotantes. Es diegético: así se ve un laboratorio de verdad cuando un equipo está en uso.
3. **Mini-Baymax acompañante.** Una versión pequeña de Baymax que camina o flota y señala con el brazo. Tiene más personalidad y refuerza la historia de los recuerdos. Cuesta más (animaciones).
4. **Brújula en el reloj.** Al mirar la muñeca, una flecha en el holograma apunta a la estación y dice a cuántos metros está. Es guía bajo demanda: no ensucia la escena.
5. **Sonido espacial.** Un "ping" que sale de la estación del paso: el oído lleva la cabeza hacia allí sin nada en pantalla. Sirve de complemento a cualquiera de las anteriores.
6. **Guía solo si la pides.** La luz aparece al pulsar Y o tras unos 20 s sin avanzar, y nunca a quien ya sabe adónde va. Reduce la sobrecarga para usuarios expertos (heurística 7 de Nielsen: flexibilidad).

## Ideas para la INCUBADORA, en lugar de la alarma (punto 8)

Objetivo: que el rincón de la incubadora (donde estaba el kit de la alarma) **enseñe microbiología** y se viva con las manos.

1. **Antibiograma de Kirby-Bauer (recomendada).** El estudiante siembra S. aureus en una placa y coloca con pinzas 3 discos (PG, ME, AV). En la incubadora, a 37 °C, se ven pasar las 18 h en unos segundos. Al sacar la placa, mide los halos con una regla virtual y los compara con una tabla (sensible / intermedio / resistente).
   - **Qué enseña:** por qué funciona cada cápsula, con datos medidos y no adivinados. Responde directamente el punto 7 del profesor.
   - **Cómo encaja:** puede ir antes de la cámara (la evidencia para elegir la estrategia) o después (para confirmar).
2. **Fleming en la incubadora.** Se recrea el descubrimiento de 1928: una placa de S. aureus con una espora de moho (la muestra B) se incuba y se ve crecer el halo alrededor del moho. Baymax recupera un recuerdo ("la penicilina"). Une la misión 1 (moho) con la misión 2 (atacar la pared).
3. **¿A qué temperatura crece?** Tres placas iguales a 25, 37 y 42 °C. Una gráfica en la pantalla de la incubadora dibuja la curva de crecimiento (latencia, exponencial, estacionaria). Se aprende que S. aureus es mesófila (crece mejor a 37 °C, la del cuerpo humano) y el moho a 25-30 °C.
4. **Técnica aséptica (reutiliza la "contaminación").** Si el estudiante deja la placa destapada o no trabaja junto al mechero, tras incubar aparecen colonias extrañas. Debe reconocer que la placa está contaminada y repetir. Se aprovecha la mecánica de manchas de `BiohazardEvent`, pero con causa y efecto de microbiología.
5. **Aerobios y anaerobios.** Tubos de tioglicolato en la incubadora: según dónde crezca la bacteria (arriba, abajo o en todo el tubo), se deduce si necesita oxígeno. S. aureus es anaerobia facultativa: crece en todo el tubo.

Para el curso, conviene implementar la 1 (o la 2, que es más corta): conecta con las misiones que ya existen y añade una medida, no solo un botón.
