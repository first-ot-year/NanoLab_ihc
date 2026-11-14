# Rompecabezas del ADN holográfico (entre la MISIÓN 2 y la MISIÓN 3)

Baymax aisló el virus del brote (la MUESTRA C), pero su banco genómico quedó corrupto. De un fragmento real del **gen de la espiga S** solo queda una hebra. En el **PROYECTOR DE ADN (14)**, junto al laboratorio de vacunas, el estudiante reconstruye la hebra complementaria. Sin ese gen no hay vacuna: la MISIÓN 3 se desbloquea al terminarlo.

## Cómo se juega

1. Al terminar la MISIÓN 2, el proyector deja de mostrar DATOS CORRUPTOS y aparecen las bases. La tarjeta de objetivo, las flechas del suelo y el AHORA llevan al proyector (14).
2. Arriba flota la hebra molde: **5'-C C A C T A G T-3'**, los nucleótidos 25 a 32 del gen S del SARS-CoV-2. Debajo hay 8 huecos. En la bandeja hay 12 bases agarrables (3 de cada tipo, mezcladas) con su letra y su color: A verde, T rojo, C azul y G amarillo.
3. Se agarra una base con el grip y se suelta en un hueco:
   - **Correcta** (A con T, C con G): suena, saltan chispas, aparecen los puentes de hidrógeno (2 para A-T y 3 para C-G) y la base queda fija: cinemática y sin agarre.
   - **Incorrecta**: no queda puesta. Un aviso explica qué base hacía falta y por qué (número de puentes y forma), y la pieza vuelve a la bandeja. Cuenta como error en las métricas, sin castigo.
4. Con las 8 bases puestas, el holograma **brilla** (se vuelve casi opaco y sube una luz cian), el **núcleo de memoria de Baymax** pasa de gris a dorado, Baymax lo celebra y la **MISIÓN 3 queda desbloqueada**. El laboratorio de vacunas explica que ya tiene el gen.

## Archivos y cambios

| Archivo | Cambio |
|---|---|
| `Scripts/Centro/HoloDNA_Manager.cs` (nuevo) | `HoloDNA_Manager`: monta el holograma, los huecos, la bandeja, el núcleo de memoria, la luz y las chispas; valida cada base, hace el brillo final, guarda las métricas (`caso: "ADN"`) y restaura el estado desde la partida. `BaseADN`: el bloque agarrable de cada base |
| `Scripts/Centro/MontajeMisiones.cs` | `CrearHoloDNA`: coloca el proyector junto al laboratorio de vacunas o, si no hay sitio, en un hueco libre del centro. Comprueba que el proyector y el sitio del usuario no choquen con nada |
| `Scripts/Centro/Misiones.cs` | `AdnReparado` y `AdnPendiente`. Ahora `VacunaDesbloqueada` exige el recuerdo 2 **y** el ADN reparado. `TextoBloqueada` y `TextoBloqueadaCorto` explican qué falta |
| `Scripts/Centro/RetoManager.cs` | Al cerrar la misión 2, si falta el ADN, el objetivo y las flechas llevan al proyector (14). El aviso de MISIÓN 2 COMPLETA anuncia el siguiente paso. Nuevos métodos `AdnCambio` y `AdnCompletado` para refrescar la guía |
| `Scripts/Centro/RetoVacuna.cs` | Los mensajes de bloqueo usan los textos de `Misiones`, así el laboratorio de vacunas dice que falta reparar el ADN |
| `Scripts/Microbiologia/Partida.cs` | Nuevo campo `adnReparado` (se guarda y se restaura con las piezas en su sitio). NUEVA PARTIDA lo borra |
| `Scripts/Centro/DebugReto.cs` | Prueba del rompecabezas: una base incorrecta, las 8 correctas y la comprobación del desbloqueo de la vacuna |
| `Editor/CapturasPasoAPaso.cs` | Capturas `adn_1_inicio`, `adn_2_error`, `adn_3_mitad` y `adn_4_completo` |

## Decisiones técnicas

- **Sockets y filtros (equivalente a XRIT).** El proyecto no usa el XR Interaction Toolkit, sino el Meta Interaction SDK con su propio sistema de encajes:
  - Las piezas son `Encajable` y los huecos son `ZonaEncaje`, que hacen el papel de `XRGrabInteractable` y `XRSocketInteractor`.
  - El **filtro del socket** es el campo `tipo`: los huecos solo aceptan piezas de tipo `"baseADN"`, igual que un *interaction layer* o un *socket filter*. Así no encaja ningún otro objeto del laboratorio.
  - La regla A-T / C-G se valida aparte, en `AlCambiarSocket`. Así se puede **explicar el error**: si el filtro rechazara la pieza sin más, el estudiante no sabría por qué no entra.
  - Las piezas bloqueadas usan `Encajable.Bloquear(true)` (desactiva el Grabbable) y `Rigidbody.isKinematic = true`.
- **Materiales holográficos.** Son copias del material `Vidrio` de la escena (Standard transparente) teñidas de cian o del color de cada base, con alfa entre 0,06 y 0,55. Se crean desde ese material para que su shader entre en el APK; un shader aditivo creado con `Shader.Find` podría no estar incluido en la compilación. El brillo final sube el color y el alfa durante 2,2 s y enciende una luz puntual en tiempo real, sin sombras y solo para este proyector.
- **Partículas.** Usan el material de la niebla de la cámara experimental, que ya está en el build.
- **Rendimiento.** El holograma son unas 70 piezas simples sin colisionador, más 12 bloques con colisionador. La luz extra solo se enciende al completar el rompecabezas.

## Probar

- **NanoLab › Probar misiones 1 y 2** y **NanoLab › Capturas paso a paso**: el rompecabezas va justo después de la MISIÓN 2.
- Prueba del 13/11/2026 con *Capturas paso a paso*: misiones 1 y 2 `CORRECTO` (97 comprobaciones, incluidas las del ADN) y misión 3 `CORRECTO`.
