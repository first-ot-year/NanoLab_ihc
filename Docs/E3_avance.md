# E3 (50 %) · Observación microscópica, ficha digital y accesibilidad

Entregable 3 del cronograma del informe del hito 1 (Cuadro 6, semana 12):

> Implementar la observación microscópica inicial y una ficha digital con información sobre las características de la muestra. **Salida:** el usuario observa la muestra seleccionada y consulta información que le permite interpretarla.
> RF asociado: RF-04 (con RF-03) · RNF asociados: RNF-05, RNF-07.

## Checklist de salida

| # | Criterio | Cómo se cumple | Evidencia | Estado |
|---|---|---|---|---|
| 1 | **RF-03:** el usuario observa la muestra seleccionada | Portaobjetos con el frotis teñido en la platina y la cara en el ocular: HUD ampliado (paleta azul oscura, siempre encima) con la micrografía A5 de *S. aureus*. El joystick izquierdo desplaza la muestra y A/X–B/Y cambian el aumento. A 400x se registra la **forma** y a 1000x la **agrupación** y el **color Gram**. Una tinción errónea no da evidencias | PR (4 comprobaciones de microscopía); CAP `hud_1_*`, `hud_2_*` | ✅ |
| 2 | **RF-04:** consulta información para interpretarla | Biblioteca (estación 4) con **5 fichas**: *E. coli*, *S. aureus*, *B. subtilis*, **virus de la hepatitis B** y pared Gram +/−. Cada ficha muestra la micrografía, la forma, la agrupación, la pared y los datos clave. Consultarla registra la evidencia y avanza a Hipótesis | PR "Consultar una ficha > etapa Hipótesis"; CAP `centro_6_biblioteca.png` | ✅ |
| 3 | Ficha del virus (A6, RNF-06) | Micrografía **electrónica** del CDC con el aviso de que un virus (~42 nm) no se ve con el microscopio óptico del laboratorio | PR "La biblioteca tiene 5 fichas, con la del virus como micrografía electrónica (A6)"; CAP `ficha_virus.png` | ✅ |
| 4 | Ficha accesible: botón, rayo o toque | La ficha se abre con el botón ABRIR del mostrador, **apuntándola con el rayo** (gatillo o A/X) o **tocando el panel**. El rayo de la mano vacía solo aparece sobre algo usable, para no competir con el agarre de Meta | PR "Una ficha se abre apuntándola con el rayo…" y "La ficha se abre al tocar el panel…" | ✅ (validar en VR) |
| 5 | **RNF-07:** control de volumen | Panel ☰ (botón menú del mando izquierdo), en cualquier sala: **música** y **efectos** por separado, en pasos de 10 %, con barra y porcentaje (no solo color). Se guarda entre sesiones y se cierra solo si el usuario se aleja más de 2 m. Incluye el aviso de contenido simplificado (RNF-06) | PL "Volumen de efectos…", "Volumen de música al 0 %…", "El panel se abre…"; CAP `ajustes_menu.png` | ✅ (validar en VR) |
| 6 | **RNF-07:** modo sentado | En el panel ☰: al activarlo se mide la cabeza del usuario sentado y la vista sube hasta 1.6 m (altura de alguien de pie) con el `HeightOffset` del Locomotor de Meta. "Recalibrar altura" vuelve a medir | PL "Modo sentado: la vista sube…" | ✅ (validar en VR) |
| 7 | **Sillas en las que sentarse** | Las 12 sillas decorativas del paquete (repartidas por el suelo, algunas en el pasillo de entrada) se quitaron de Microbiología: ahora hay un **taburete de laboratorio** propio (`Silla`, asiento a 0.65 m porque las mesas son bancos de 0.9 m) **frente a cada uno de los 10 microscopios de puesto**. En Química se cambiaron por la propia en su sitio. No bloquean al jugador, así el microscopio también se usa de pie. Se apuntan con el rayo para sentarse: los ojos quedan a ~1.4 m, a la altura del ocular, y la locomoción se pausa. Se levanta moviendo el joystick izquierdo (fuera del microscopio) y queda detrás de la silla | PL "Sentado en…", "Al levantarse…"; CAP `silla_sentado_puesto.png` | ✅ (validar en VR) |
| 8 | **RNF-05:** consistencia | Los mismos controles en todo el laboratorio: el rayo usa siempre gatillo o A/X. El panel ☰ y las sillas son iguales en Microbiología y en Química. Las fichas mantienen el número y el color de la estación 4 | CAP `centro_*.png` | ✅ |

## Cambios de esta entrega

- **Accesibilidad (RNF-07):**
  - `AjustesNanoLab`: volumen de música y efectos, modo sentado y altura calibrada, guardados en `PlayerPrefs`. `MusicaAmbiente` y `Sonidos` aplican el volumen.
  - `MenuAjustes` / `BotonMenu`: panel ☰ con botones que se tocan o se apuntan con el rayo. Lo monta `MontarAjustes` (Editor) como hijo del rig, desde `ConfigurarMetaISDK`.
  - `JugadorNanoLab`: `CalibrarSentado`, `AplicarAltura`, `Sentarse` y `Levantarse`. Usa el `HeightOffset` y los eventos de locomoción absolutos del Locomotor de Meta, y pausa la locomoción mientras se está sentado.
- **Sillas:**
  - `Silla` (runtime).
  - `MontarSillas` (Editor): crea el prefab `Assets/NanoLab/Prefabs/Silla.prefab` solo si no existe. En Microbiología quita las sillas del paquete (`Assets/3D Laboratory Environment with Appratus/Prefabs/chair.prefab`) y pone una frente a cada microscopio de puesto, con el asiento a 0.3 m del borde de la mesa. En Química las cambia en su sitio, mirando a la mesa.
  - Para poner un modelo de Blender: abre el prefab, cambia los hijos de `Modelo` y conserva los anclas `Asiento` y `Salida`.
- **Rayo de la mano vacía:** `Apuntable` (base) y `PunteroMuestras` lo detectan; la usan las fichas, los botones del panel y las sillas.
- **Fichas:**
  - `EstacionFicha` se abre también con el rayo o el toque.
  - `MontarCentroExperimentacion.Biblioteca`: 5 fichas en el mismo mostrador e incluye la del virus.
- **Introducción:** la línea de controles del tótem añade "botón menú: volumen y modo sentado".
- **Corrección:** la micrografía de las fichas coincidía en profundidad con la cara del fondo (z-fighting) y se veía cortada o no se veía (le pasaba a *S. aureus*, *E. coli* y al virus). Ahora va 7 mm por delante.
- **Pruebas:**
  - `PruebaReto` añade 4 comprobaciones de fichas.
  - `PruebaLocomocion` añade 7 de accesibilidad.
  - Menú `NanoLab/Capturas del E3`.

## Resultados de verificación (batchmode)

| Prueba | Resultado |
|---|---|
| `NanoLab/Probar reto completo` | CORRECTO: 39 comprobaciones OK (4 nuevas de fichas), 0 excepciones |
| `NanoLab/Probar locomoción` | CORRECTO: 22 comprobaciones OK (7 nuevas de accesibilidad). Caminata 1.52 m/s, 90 % en 0.12 s, frenado en 0.12 s. Volumen de efectos 0 % = silencio y 50 % = mitad; música al 0 % baja a 0.000; modo sentado +0.45 m; sentado en el taburete: ojos a 1.37 m y cuerpo sobre el asiento; al levantarse queda en la salida |
| `NanoLab/Probar recorrido del jugador` | Todas las rutas son transitables, incluida entrada → puesto 1 con el taburete delante |
| `NanoLab/Probar física de los objetos agarrables` | 0 objetos movidos (Química 107, Microbiología 49) |
| `NanoLab/Capturas del E3` | Sentado, el ocular queda a 0.34 m en horizontal y a la altura de los ojos (se activa a 0.22 m: basta inclinarse unos 12 cm) |

## Validación pendiente en Meta Quest

1. El botón ☰ abre el panel de ajustes; los botones responden al tacto y al rayo; el volumen de música y de efectos se oye cambiar.
2. Sentado en una silla real, activar el modo sentado: la vista queda a la altura de alguien de pie y se alcanzan las mesas.
3. Apuntar una silla de un puesto y sentarse: la vista queda frente a la mesa y el microscopio se puede usar sentado. El joystick izquierdo levanta al jugador.
4. En la biblioteca, abrir una ficha tocándola y otra con el rayo. Comprobar que el texto se lee a 1–1.5 m: con 5 fichas en el mostrador están al 82 % del tamaño anterior; si no se leen bien, se pueden repartir en dos filas.

## Modelos de Blender integrados (30-oct-2026)

Dos paquetes hechos en Blender por un compañero. Sus fuentes (`.blend`, scripts de Python, informes y vistas previas) están en `Fuentes_Blender/`, fuera de `Assets/`, para que Unity no los importe. En el proyecto solo entran los FBX y el editor de la silla.

**Silla de laboratorio** (`Assets/NanoLab/Silla_Laboratorio_Interactuable/`)
- Taburete de microbiología:
  - asiento redondo a 0.65 m (igual que el anterior);
  - respaldo lumbar;
  - aro reposapiés a 0.27 m;
  - base de 5 radios con tacos, sin ruedas;
  - 2 212 triángulos;
  - reutiliza los materiales `SillaAsiento` y `SillaMetal`.
- `MontarSillaLaboratorio.CrearPrefab` crea el prefab con `Silla`, las anclas `SitPoint` y `ExitPoint` y un solo collider trigger. `MontarSillas` lo usa en los 10 puestos de Microbiología y en las 12 sillas de Química. El taburete de primitivas queda como respaldo si falta el FBX.
- Se aplicó el parche del compañero:
  - estando sentado, el rayo solo encuentra la propia silla (no se salta de una silla a otra);
  - `PunteroMuestras.ApuntableEn` pasa a ser estático y lo usa también la prueba;
  - la prueba comprueba el rayo, la orientación y el bloqueo de las otras sillas.
- Sentado: ojos a 1.37 m, ocular a 0.34–0.35 m en horizontal y a la altura de los ojos.

**Baymax Bebé v4.2** (`Assets/NanoLab/Modelos/BaymaxBebe/`)
- FBX con esqueleto (36 huesos) y 6 clips. Se usa el LOD1 (12 500 triángulos) por el rendimiento en Quest (RNF-08).
- `ConvertirBaymaxBebe` (menú *NanoLab/Avatar Baymax/Baymax Bebé v4.2: convertir e instalar*):
  - configura el importador (Generic, clips en bucle) y crea los materiales Standard marfil y negro;
  - crea el Animator Idle↔Walk y saca la pose de puño del clip `Gestos_Manos`.
- La malla se parte en dos, con el mismo esqueleto:
  - **Manos**: recortadas con un plano en la muñeca y con el corte tapado. Capa 0, se ven en primera persona.
  - **Cuerpo** y **Cara**: capa 29, solo los espejos.
- `AvatarBaymaxBebe` (en tiempo de ejecución):
  - **Escala:** uniforme, para que los ojos del modelo coincidan con los del jugador. De pie mide ~2 m en el espejo.
  - **Cabeza:** sigue al visor, y el cuerpo gira con ella.
  - **Brazos:** IK de dos huesos hacia el mando, con el codo limitado a 65°. Si el mando queda lejos, el brazo se estira separando los huesos. El antebrazo gira con la mano para que la muñeca no se retuerza.
  - **Manos y dedos:** las manos tienen el tamaño de una mano real; los dedos se cierran con el grip y el gatillo.
  - **Piernas:** Idle o Walk según la velocidad, y dobladas al sentarse en una silla.
- `ConvertirBaymax.InstalarEn` instala el avatar nuevo en las dos salas. El `AvatarBaymax` de piezas rígidas queda como respaldo.

**Verificación (batchmode):**

| Prueba | Resultado |
|---|---|
| `PruebaLocomocion` | CORRECTO, 25 OK |
| `PruebaReto` | CORRECTO, 39 OK |
| `ProbarRecorrido` | Todas las rutas transitables |
| `ProbarFisica` | 0 objetos movidos (Química 107, Microbiología 49) |

Capturas: `avatar_*`, `silla_prefab`, `silla_sentado_puesto`.

**Pendiente en Quest:**
- que la mano coincida con el mando en posición y giro;
- el FPS con el espejo activo;
- sentarse en la silla nueva frente al microscopio.
