# Recorrido en U del Centro de Experimentación

Objetivo: que el estudiante no se pierda y que la sala no se sienta cargada (pedido tras la revisión del profesor).

## Distribución

```
 z 17 ┌ Ultracong. ── MESA DE VACUNAS ── Incub. CO2 ┐   SALA DE VACUNAS (misión 3)
 z 11 ├─puerta─┬─────────── Autoclave ─ Cámara ─ Prensa ─ Terminal ┤ ← pared de vidrio (misión 2)
      │        Archivo                                   Biblioteca │
puerta│                    (centro despejado)          Conclusiones │
 z  0 └ Incub.37 ─ Banco ─ Cabina ─ Tinción ─ Microscopía ──────────┘   (misión 1)
      x 6.9                                                     x 24.1
```

- **Misión 1**, pared sur, en orden de uso: incubadora 37 °C → banco → cabina de bioseguridad (muestra C, junto al banco) → tinción → microscopía. Biblioteca y panel de CONCLUSIONES en la pared este, junto a la microscopía (las conclusiones se usan al clasificar en la misión 1 y al explicar en la misión 2, así no hay que cruzar la sala).
- **Misión 2**, a lo largo de la pared de vidrio: terminal → prensa → cámara → autoclave (se usa justo después de la cámara); la explicación final se escribe en CONCLUSIONES, en la esquina este.
- **Misión 3**, en su propia sala detrás del vidrio: mesa de vacunas en el centro, ultracongelador e incubadora de CO₂ a los lados. La puerta de vidrio se abre al completar la misión 2 (luz roja → verde, sonido y aviso).
- **Entrada**: archivo de recuerdos y tótem de bienvenida junto a la puerta. Centro de la sala despejado; la torre del objetivo cuelga en el centro de la zona de experimentos.
- **Menos carga visual**: fuera los rótulos numerados del suelo (queda la franja de color de cada estación) y el tótem "TU OBJETIVO" (el objetivo ya está en el reloj y en la torre).
- La **luz guía** cruza por la puerta cuando la estación está al otro lado del vidrio.

## Cómo se aplica

1. `NanoLab › Recorrido en U (ordenar el centro)`: mueve en la escena las estaciones y los equipos, oculta los rótulos, deja la marca `RecorridoU` y guarda la escena. Es idempotente.
2. `NanoLab › Recorrido en U: hornear la luz`: vuelve a hornear la iluminación para que las sombras del suelo sigan a los muebles (tarda unos minutos).
3. Al jugar, con la marca presente: la mesa de vacunas, las conclusiones y la pared de vidrio se colocan solas (`RecorridoU.cs`, `ParedVacunas.cs`).

Se hace en el editor (no al jugar) porque mesas y equipos son geometría estática (lotes estáticos y luz horneada): moverlos en tiempo de ejecución dejaría el dibujo en el sitio viejo.

## Archivos

| Archivo | Qué hace |
|---|---|
| `Editor/RecorridoCentro.cs` | Menús para ordenar el centro y hornear; `Vista()` para que las capturas automáticas sigan a las estaciones |
| `Scripts/Centro/RecorridoU.cs` | Posiciones de la distribución y `Activo` (marca en la escena) |
| `Scripts/Centro/ParedVacunas.cs` | Pared de vidrio, puerta corredera bloqueada hasta la misión 2, cartel y luz de estado |
| `MontajeVacuna.cs`, `MontajeMisiones.cs` | Con el recorrido activo: vacuna en su sala, cámara sin reubicar, conclusiones en la esquina este, junto a la microscopía |
| `GuiaReto.cs` | La luz guía pasa por la puerta de vidrio |
| `DebugReto.cs` | Comprueba la pared, la sala de la vacuna y el bloqueo de la puerta |

## Entrada con luces por zonas

La primera vez que el jugador entra al centro en cada sesión, el suelo se ilumina estación por estación siguiendo la U (cian = misión 1, violeta = misión 2, coral = misión 3) y Baymax dice el orden en una frase. Dura unos 5 s y no bloquea nada (`EntradaCentro.cs`, se monta desde `MontajeMisiones.cs`).
