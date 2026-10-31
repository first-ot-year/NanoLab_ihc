# Probar NanoLab en PC con el Meta XR Simulator

Con el Meta XR Simulator se prueba el laboratorio en la laptop, sin el Meta Quest. Simula el visor y los dos mandos con el teclado y el ratón dentro del modo Play de Unity.

> El simulador **no forma parte del repositorio**: es un programa de Windows y cada integrante lo instala en su PC. No hace falta cambiar nada del proyecto para usarlo, funciona en cualquier rama.

## Requisitos

| | |
|---|---|
| Unity | 2022.3.62f3 |
| Meta XR SDK | 201 (ya está en el proyecto) |
| Plugin XR | `com.unity.xr.oculus` 4.5.5 (ya está en el proyecto) |
| Sistema | Windows 10/11 de 64 bits |
| Espacio | ~1.5 GB para el simulador instalado |

## 1. Instalar el simulador

1. Descarga el instalador oficial de Meta (versión compatible con el SDK 201):
   https://www.facebook.com/horizon_devcenter_download?app_id=28549923061320041&sdk_version=201
   Pide iniciar sesión con la cuenta de Meta. El archivo es `meta_xr_simulator.msi` (~436 MB).
2. Ejecuta el `.msi` y deja la carpeta que propone por defecto. Queda en:
   ```
   C:\Program Files\MetaXRSimulator\v207.1\
   ```
3. Cierra Unity si estaba abierto antes de instalar. Unity lee la instalación al arrancar.

> ⚠️ **No instales paquetes `com.meta.xr.simulator.*` desde el Package Manager.** Esos paquetes son de versiones antiguas del simulador. El que aparece en Unity, `com.meta.xr.simulator.synthenvbuilder` 60.0.1, es el constructor de entornos sintéticos, no el simulador. Añade dependencias al `manifest.json`, cambia `ProjectSettings` y da el error *"Meta XR Simulator Not Installed"*.

## 2. Activarlo en Unity

1. Abre el proyecto en Unity.
2. Menú **Meta → Meta XR Simulator → Activate**.
3. En la consola debe aparecer: `Meta XR Simulator is activated`.
4. Abre la escena de **Microbiología** (o la de Química) y dale **Play**. Se abre la ventana del simulador junto al Game view.

La activación solo afecta a esa sesión del editor. No modifica archivos del proyecto, así que no aparece nada en `git status`.

## 3. Controles

- **Del simulador:** la ventana muestra el panel de controles con las teclas para mover la cabeza (visor) y cada mando y para pulsar sus botones. Revisa ahí las teclas de tu versión, porque Meta las cambia entre versiones.
- **De NanoLab:** son los mismos que en el visor (no hay teletransporte):

| Acción | Botón del mando |
|---|---|
| Caminar | Joystick izquierdo |
| Girar (por pasos de 30°) | Joystick derecho |
| Agarrar objetos | Grip |
| Usar el rayo (fichas, botones, sillas) | Gatillo o A/X |
| Abrir el panel de ajustes ☰ (volumen, modo sentado) | Botón menú del mando izquierdo |
| Microscopio: mover la muestra | Joystick izquierdo |
| Microscopio: cambiar el aumento | A/X y B/Y |
| Levantarse de una silla | Joystick izquierdo |

## 4. Antes de probar en el Quest

Para compilar el APK o usar Quest Link, desactiva el simulador desde **Meta → Meta XR Simulator → Deactivate** y reinicia Unity.

## Problemas frecuentes

| Problema | Solución |
|---|---|
| *"Meta XR Simulator Not Installed"* | El `.msi` no está instalado o se instaló un paquete equivocado del Package Manager. Instala el `.msi` (paso 1) y quita cualquier `com.meta.xr.simulator.*` del `Packages/manifest.json` |
| Se activó, pero al dar Play no aparece la ventana del simulador | Cierra y vuelve a abrir Unity: las variables de entorno del simulador se leen al iniciar el editor |
| El jugador no se mueve | Haz clic en la ventana del simulador para que reciba el teclado |
| Va lento | Es normal en una laptop: el simulador renderiza los dos ojos. El rendimiento real (72 FPS, RNF-08) se mide en el Quest, no en el simulador |

## Qué se puede validar en PC y qué no

- **En PC:** el flujo del reto, las fichas, el microscopio, el panel de ajustes, sentarse en las sillas, el avatar en el espejo y los textos.
- **Solo en el Quest:** el rendimiento, la sensación de las manos y el IK del avatar, la escala real, la legibilidad a distancia y el confort de la locomoción.
