# Mejoras de experiencia (noviembre 2026)

Cinco mejoras para que el laboratorio enseñe más y se sienta más vivo. Todas tienen prueba automática en `NanoLab › Probar misiones 1 y 2` (103/103) y `NanoLab › Probar reto de vacuna` (48/48).

| # | Mejora | Qué ve el jugador | Archivos |
|---|---|---|---|
| 7 | **Pistas progresivas** | El botón PISTA da 3 niveles por paso: 1) una pregunta para pensar, 2) la pista concreta, 3) la solución. El nivel se reinicia al cambiar de paso. | `RetoManager.cs` (`PedirPista`, `PreguntaGuia`, `Solucion`), `EstacionGuia.cs` |
| 6 | **Tinción visible paso a paso** | Cada reactivo cae como gota sobre el portaobjetos, el frotis cambia de color poco a poco y aparece una lupa con 3 células: explica qué hizo el reactivo y, al final, si la muestra es GRAM POSITIVA (pared gruesa, violeta) o NEGATIVA (pared fina, rosa). | `TincionVisible.cs` (nuevo), `Portaobjetos.cs` |
| 13 | **Recuerdos de Baymax animados** | Al terminar cada misión aparece delante un núcleo de memoria dorado con anillo de luces y un panel "RECUERDO RECUPERADO n/3" con la frase aprendida; a los 5 s vuela al pecho de Baymax con sonido y vibración. | `RecuerdoAnimado.cs` (nuevo), `Misiones.cs` (`TextoRecuerdo3`), `RetoManager.cs`, `RetoVacuna.cs` |
| 1 | **Entrada con luces por zonas** | Al entrar por primera vez, las estaciones se encienden en el orden de la U con el color de su misión. | `EntradaCentro.cs` (nuevo), `MontajeMisiones.cs` |
| 4 | **Pipeta con gatillo progresivo** | Cuanto más se aprieta el gatillo, más rápido carga o suelta líquido (se ve el nivel y caen gotas; vibra según el caudal). El botón A/X sigue haciendo la acción completa de una vez (accesibilidad). | `InteraccionVacuna.cs` (`PipetaVacuna.Fluir`) |

## Cómo probarlo en el visor

- **Pistas**: en cualquier paso, pulsa PISTA tres veces.
- **Tinción**: en la mesa de TINCIÓN aplica cristal violeta → lugol → alcohol → safranina sobre la MUESTRA A y mira la lupa sobre el portaobjetos.
- **Recuerdo**: completa la misión 1 identificando la muestra en la TERMINAL.
- **Luces de entrada**: entra al centro de experimentación (solo la primera vez en cada sesión).
- **Pipeta**: en la misión 3, acerca la punta a un frasco y aprieta el gatillo a medias y luego a fondo.

## Guía más intuitiva en los retos (17/11/2026)

Ideas 2, 4, 5 y 6 de la revisión del profesor ([Revision_profesor.md](Revision_profesor.md)), sin carteles nuevos ni nada que siga a la cabeza.

| Mejora | Qué ve / oye el jugador | Archivos |
|---|---|---|
| **Estación que despierta** | La estación del paso se enciende como un equipo en uso: luz cálida que respira sobre la zona de trabajo, un charco de luz en el suelo y el marco del cartel que late. Las demás quedan en reposo. | `EstacionGuia.cs` |
| **Sonido espacial** | Si estás a más de 3 m y no miras hacia la estación, de ella sale un "ping" suave cada 7 s: el oído lleva la cabeza hacia allí. Calla con la cara en el microscopio o fuera del centro. | `EstacionGuia.cs`, `Sonidos.Llamada` |
| **Brújula en el reloj** | Al mirar la muñeca, el pie dice adónde ir: `TINCIÓN DE GRAM (2) · 4 m · a tu izquierda` (o `puerta de vidrio …` si está al otro lado). Desaparece al llegar. | `GuiaReto.TextoRumbo`, `BrazaleteBaymax.Rumbo` |
| **Guía bajo demanda (Y)** | Y ("¿adónde voy?") vuelve a lanzar la luz guía desde tu lado; si ya estás en la estación, destella allí. | `BrazaleteBaymax.Llamado`, `GuiaReto.Relanzar` |
| **Ayuda si te atascas** | Tras 60 s en el mismo paso, el reloj vibra una sola vez con "¿Te ayudo? Pulsa PISTA… o Y…" y la luz vuelve a salir. No cuenta como pista en las métricas. | `GuiaReto.Atasco` |

Prueba: `NanoLab › Probar misiones 1 y 2` comprueba que solo la estación del paso se enciende.

## Primeros pasos más directos (17/11/2026)

Tras probar la misión 1: "no es tan directo", "tengo que ir a la mesa del costado", "letras pequeñas", "no es sencillo de entender".

| Mejora | Qué ve el jugador | Archivos |
|---|---|---|
| **Portaobjetos en el BANCO** | Una bandeja con 2 portaobjetos junto a la placa de la MUESTRA A: el frotis se hace sin ir a la mesa de TINCIÓN. Allí quedan otros 2 para repetir. El objetivo, la pista, el cartel y la luz guía llevan al BANCO. | `MontajeMisiones.PortaobjetosEnBanco`, `RetoManager.cs` |
| **Letras más grandes** | Todos los textos 3D se agrandan un 40 % (antes, un 25 %); si un texto no cabe, encoge para no salirse. | `LegibilidadTextos.Escala` |
| **Demostración animada del gesto** | Un portaobjetos holográfico cian sale del tuyo (o de la bandeja), viaja en arco hasta donde hay que tocar, "toca" con un anillo que se abre y se repite. Encima, una etiqueta corta: `COGE UN PORTAOBJETOS`, `TOCA LA MUESTRA A`, `GOTERO 2 DE 4 · Lugol`, `LÍMPIALO AQUÍ`, `PONLO EN LA PLATINA`. Sigue lo que llevas de verdad y se apaga con la cara en el ocular o lejos (más de 4,5 m). | `DemoGesto.cs` (nuevo) |

## Ubicaciones, alcance y respuesta a los errores (18/11/2026)

A partir de las métricas de la sesión del 18/11 (misión 3, 28 min): SÍNTESIS tardó 7,7 min y DISEÑO 6,5 min, y en FASES y TÍTULO el mismo botón equivocado se pulsó 5-8 veces seguidas (el jugador no veía que había fallado).

| Mejora | Qué ve el jugador | Archivos |
|---|---|---|
| **Frascos al alcance** | La gradilla de la mesa SÍNTESIS pasa de 0,63 m a 0,27 m de fondo (se usa 3 veces o más con la punta de la pipeta). Detrás, a 0,5 m, lo que se usa una vez: soporte de la pipeta, termobloque con su botón INCUBAR al lado y tubo. Fuera de síntesis y neutralización, la gradilla se oculta (deja sitio a los atriles de A-E). | `MontajeVacuna.MesaSintesis`, `RetoVacuna.PrepararMesa` |
| **Bloques al alcance** | En DISEÑO, la bandeja de bloques va delante (0,23 m) y el molde detrás (0,44 m), porque los bloques se cogen 5-6 veces y el molde los encaja solos a 12 cm. | `MontajeVacuna.MesaDiseno` |
| **"AÚN NO" en el botón** | Al fallar con A-E, la tapa se pone roja y la letra tiembla, y sale un aviso corto junto a la mano: "AÚN NO: mira la pantalla de la izquierda". En TÍTULO (rayo), el mismo aviso junto a la columna. | `BotonFisico.MarcarError`, `RetoVacuna.NoEra` |
| **Repetir no penaliza** | Pulsar otra vez la misma respuesta equivocada en menos de 8 s no cuenta como error nuevo (ni en las métricas). Avisa con "Esa ya la probaste…". | `RetoVacuna.Fallo` |
| **Menos texto flotante** | Los rótulos "MUESTRA B/C", que flotaban sobre las muestras del BANCO, ahora van impresos en el mostrador delante de cada una. | `MontajeMisiones.Rotulo` |
| **Auditoría de ubicaciones** | `NanoLab › Auditar ubicaciones (play mode)` escribe `Docs/Auditoria_Ubicaciones.md` con objetos que flotan o se hunden, cosas que se tocan fuera de 0,75-1,45 m de altura o a más de 0,5 m del borde, y estaciones que se pisan. La variante "y corregir lo que flota" los baja a la superficie durante esa sesión de Play. | `Editor/AuditarUbicaciones.cs` |

## Pendiente

- Hornear la luz tras el reordenamiento: `NanoLab › Recorrido en U: hornear la luz` (tarda unos minutos).
