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

## Pendiente

- Hornear la luz tras el reordenamiento: `NanoLab › Recorrido en U: hornear la luz` (tarda unos minutos).
