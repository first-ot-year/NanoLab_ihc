# Auditoría de las sillas — NanoLab VR (sala de Microbiología)

Fecha: 2026-10-30. Solo lectura: no se modificó ningún archivo del proyecto ni del repositorio.

## 1. Qué había en `C:\ihc\chair`

| Archivo | Contenido real |
|---|---|
| `chair.zip` | `chair.fbx` (FBX binario 7.x, 2022) + `chair.fbx.meta` (GUID `54b30e10…`, importador de Unity 2022.2+). Es la silla del paquete *3D Laboratory Environment with Appratus*. |
| `SillaAsiento.mat.zip` | `SillaAsiento.mat` y `SillaMetal.mat` + sus `.meta` (GUID `0e2e2c80…` y `964df491…`). Shader **Standard** (Built-in RP), sin texturas. Son los materiales que crea `MontarSillas.cs`. |

No hay texturas en ninguno de los dos ZIP. **No había proyecto Unity en esta carpeta.** El proyecto es el repositorio `first-ot-year/NanoLab_ihc`:

- `C:\ihc\proyecto2\nanolab.git`: clon *bare* parcial (sin blobs), en `f16adff` (2026-10-17). Está **desactualizado** y su commit ya no existe en el remoto.
- Remoto `main`: `69afc95` (2026-10-29 20:00, "add: avance Entregable3"). La auditoría se hizo sobre una copia de solo lectura de este commit.
- Unity **no está instalado** en este equipo. Blender 5.2 sí.

## 2. Proyecto

- Unity **2022.3.62f3**, Built-in Render Pipeline (materiales Standard).
- Paquetes: `com.meta.xr.sdk.all` 201.0.0 (Meta XR / **Interaction SDK**), `com.unity.xr.oculus` 4.5.5, `com.unity.inputsystem` 1.14.0. **No** usa XR Interaction Toolkit.
- Escenas del build: 0 = `Assets/NanoLab/Scenes/Microbiologia.unity` (primer cuarto), 1 = `Laboratorio.unity` (Química).
- Las escenas se montan con scripts de editor (`Assets/NanoLab/Editor/Montar*.cs`, menú *NanoLab/…*).

## 3. Sistema de interacción y locomoción (en `69afc95`)

| Pieza | Función |
|---|---|
| `JugadorNanoLab` (raíz de `OVRCameraRigInteraction`) | Configura el `FirstPersonLocomotor` de Meta: joystick izquierdo para deslizarse, derecho para girar por pasos, sin teletransporte. Instala `PunteroMuestras`. Tiene `Sentarse(Silla)` / `Levantarse()`, que usan `HeightOffset` y un `LocomotionEvent` absoluto (no mueven la cámara a mano). |
| Agarre | Meta ISDK (`Grabbable`, agarre a distancia con gatillo o grip, alcance de 1,2 m). `QuietoHastaAgarrar` deja los objetos kinemáticos hasta el primer agarre. |
| `Apuntable` (abstracta) | Cualquier cosa que se usa apuntando con la mano vacía y pulsando gatillo o A/X: fichas, botones del panel ☰ y sillas. |
| `PunteroMuestras` | Rayo de mano vacía de 2,5 m. Busca `GetComponentInParent<Apuntable>()` (incluye triggers), muestra el aviso "Gatillo o A/X: {Accion}" y llama a `Activar()`. Así se detecta la proximidad: el rayo solo aparece al apuntar a algo usable. |
| `Silla : Apuntable` | `Activar()` llama a `Sentarse(this)` o a `Levantarse()`. Anclas `asiento` y `salida`. `ojosSobreAsiento` = 0,72 m. |
| Levantarse | Se mueve el joystick izquierdo, después de soltarlo una vez y si no se está usando el microscopio. |

Un solo jugador: no hay red ni multiusuario. "Ocupada" significa ocupada por el jugador local.

## 4. Causa exacta por la que las sillas no permiten sentarse

Comprobado en la historia de git y en el YAML de las escenas, no solo leyendo métodos:

| Versión | Sillas en `Microbiologia.unity` | `Silla.cs` / `Apuntable.cs` | `Sentarse()` | El puntero busca `Apuntable` |
|---|---|---|---|---|
| Local `f16adff` (17-oct) | — | no existen | no existe | no |
| `main` hasta `a143f6b` (24-oct) | instancias de `chair.prefab` (718 referencias) | **no existen** | **no existe** | **no** |
| `main` `69afc95` (29-oct) | 10 × `Silla.prefab` (`Silla_Puesto_01…10`) | sí | sí | sí |

**Causa:** en cualquier versión anterior a `69afc95` las sillas son instancias de `3D Laboratory Environment with Appratus/Prefabs/chair.prefab`. Ese prefab solo tiene MeshFilter, MeshRenderer y **un MeshCollider sólido** (`m_IsTrigger: 0`), sin ningún script. Por eso se comportan "como bloques de piedra": chocan y nada las reconoce como usables. En esas versiones **no existía ninguna parte de la cadena**: ni `Apuntable`, ni `Silla`, ni `Sentarse()`, ni un rayo que buscara objetos usables. No era un fallo de conexión: el sistema no existía.

Si el compañero ve sillas sólidas, está en una versión anterior a `69afc95`, o no ha hecho *pull* o abierto la escena actualizada. El `Silla.prefab` nuevo **no tiene colliders sólidos**, solo un trigger, y se puede atravesar.

### Comprobación punto por punto sobre `69afc95`

| # | Pregunta | Resultado |
|---|---|---|
| 1 | ¿`Silla.cs` implementa la interacción? | Sí: `Activar()` llama a `Sentarse` o `Levantarse`, y tiene `Accion`, `Ocupada` y `Colisiones()`. |
| 2 | ¿`Silla.prefab` tiene lo necesario? | Sí: `MonoBehaviour Silla` (GUID `7d4eb07d…`) con `asiento` y `salida` asignados y un `BoxCollider` trigger de 0,46×1,1×0,44 en la raíz, capa 0. |
| 3 | ¿Las instancias de `EnPuestos` lo conservan? | Sí: 10 instancias, sin `m_RemovedComponents` ni componentes añadidos ni desactivados. Solo se sobrescribe `m_ReceiveGI`. |
| 4 | ¿`Apuntable` permite seleccionarlas? | Sí: el raycast usa `QueryTriggerInteraction.Collide`, máscara `~0` y orden por distancia. |
| 5–6 | ¿Hay una acción conectada a `Sentarse()`? | Sí: gatillo o A/X → `PunteroMuestras.ApuntarManoVacia` → `Silla.Activar()` → `JugadorNanoLab.Sentarse()`. `JugadorNanoLab` está en la escena (1 referencia) y añade `PunteroMuestras` en `Awake`. |
| 7 | ¿`Levantarse()` funciona? | En el código: mueve a `salida`, restaura la altura y reactiva la locomoción y los colliders. |
| 8 | ¿Hay punto para sentarse? | Sí: `Asiento` a 0,65 m y `Salida` a 0,6 m detrás. |
| 9 | ¿Problemas de collider, capa o referencia? | No se encontró ninguno que impida usar las sillas. **Fallo de lógica:** sentado, el rayo podía activar **otra** silla y el jugador saltaba de una a otra sin levantarse. Corregido. |

**Límite de esta auditoría:** todo lo anterior es **estático** (código + YAML). No se ha ejecutado en Unity ni en el visor. La prueba en *play mode* `NanoLab/Probar locomoción` ya existía, pero **no probaba el rayo**: llamaba a `silla.Activar()` directamente. Se amplió (ver `INFORME_PRUEBAS.md`).

## 5. Silla original (referencia de diseño)

- `chair.fbx`: taburete giratorio de laboratorio con asiento redondo, columna de gas, base de 4 radios con ruedas y **sin respaldo**. 535 polígonos en 9 mallas sueltas (`pCylinder*`, `polySurface*`), un material `blinn2`. Bbox 1,07 × 1,06 × 0,96 (unidades del FBX).
- `Silla.prefab` actual: taburete hecho con 13 cubos primitivos, asiento a 0,65 m (mesas de 0,9 m), respaldo bajo, reposapiés, materiales `SillaAsiento` (azul 0,18/0,32/0,45, mate) y `SillaMetal` (gris metálico).

## 6. Qué se reutiliza

- Toda la interacción: `Apuntable`, `PunteroMuestras`, `Silla`, `JugadorNanoLab.Sentarse/Levantarse`. **No se crea un sistema nuevo.**
- Los materiales `SillaAsiento` y `SillaMetal` (los mismos del ZIP, con el mismo GUID). No se duplican.
- La colocación de `MontarSillas.EnPuestos`: la silla nueva toma la pose exacta de la silla del puesto, orientada al microscopio.
- La prueba `DebugLocomocion` / `PruebaLocomocion`.

## 7. Riesgos detectados

- El repo local `nanolab.git` no es una copia de trabajo y está atrasado. Hay que integrar sobre `main` (`69afc95`).
- `MontarSillas.AmbasSalas` / `MontarMicrobiologia` borran **todas** las `Silla` de Microbiología y vuelven a poner `Silla.prefab`. Si se vuelve a montar la sala, la silla de prueba desaparece.
- Mover sillas (fase 4) choca con la configuración global de agarre (ver `GUIA_INTEGRACION.md`).
