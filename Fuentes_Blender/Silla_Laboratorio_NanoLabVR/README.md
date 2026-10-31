# Silla de laboratorio interactuable — NanoLab VR

Paquete para integrar en el proyecto **NanoLab VR** (`first-ot-year/NanoLab_ihc`) una silla de **laboratorio de microbiología** nueva en la que el estudiante puede sentarse frente al microscopio y levantarse.

> **Estado: preparado, sin probar en Unity ni en el visor.** El equipo donde se preparó no tiene Unity ni visor. Las pruebas de Unity Editor y de VR **están pendientes** y son tu parte (ver «Pruebas»).

## Qué contiene

```text
Silla_Laboratorio_NanoLabVR/
├── README.md                                   este archivo
├── Blender/
│   ├── Silla_Laboratorio_Interactuable.blend   modelo fuente (Blender 5.2)
│   ├── Silla_Laboratorio_Interactuable.fbx     exportación final (idéntica a la de Unity/.../Models)
│   ├── modelar_silla.py                        script que genera el .blend y el .fbx (reproducible)
│   └── validar.py                              comprobaciones de geometría y del FBX
├── Unity/
│   ├── cambios_scripts_existentes.patch        cambios en 3 scripts existentes (para git apply)
│   └── Assets/NanoLab/Silla_Laboratorio_Interactuable/
│       ├── Models/Silla_Laboratorio_Interactuable.fbx   el modelo, en la ruta que espera el script
│       └── Editor/MontarSillaLaboratorio.cs             herramienta de editor (menú NanoLab)
└── Documentacion/
    ├── AUDITORIA_SILLAS.md                     por qué las sillas no permitían sentarse
    ├── GUIA_INTEGRACION.md                     detalle técnico de la integración
    ├── INFORME_PRUEBAS.md                      pruebas hechas y pendientes (checklists)
    └── img/                                    renders de la silla original y la nueva
```

No hay archivos `.meta`: Unity los genera al importar. Quien importe primero debe subirlos al repo para que el GUID sea el mismo para todos. No hay materiales ni texturas nuevos (se reutilizan los del proyecto).

## Qué se creó y qué se modificó

**Creado (archivos nuevos):**
- Modelo de la silla (`.blend` y `.fbx`).
- `MontarSillaLaboratorio.cs`: herramienta de editor, menú **NanoLab → Silla de laboratorio**:
  1. **Crear prefab**: configura el importador del FBX, asigna los materiales existentes y crea `Assets/NanoLab/Silla_Laboratorio_Interactuable/Prefabs/Silla_Laboratorio_Interactuable.prefab`.
  2. **Probar en el puesto 1 (Microbiología)**: pone la silla nueva en el sitio y la orientación de `Silla_Puesto_01` (`LAB_Microbiologia/Microbiologia/Sillas`) y **desactiva** la silla original sin borrarla.
  3. **Deshacer prueba**: quita la silla nueva y reactiva la original.

**Modificado (3 scripts existentes, en el `.patch`, +40/−16 líneas):**

| Script | Cambio |
|---|---|
| `Scripts/Microbiologia/Silla.cs` | **Corrige el fallo de cambiar de silla estando sentado.** Mientras estás sentado, el rayo solo encuentra tu propia silla (para levantarte). |
| `Scripts/Microbiologia/PunteroMuestras.cs` | La búsqueda del rayo de mano vacía pasa a un método `ApuntableEn(...)`. **Mismo comportamiento.** Así la prueba usa el mismo código que el visor. |
| `Scripts/Microbiologia/DebugLocomocion.cs` | La prueba automática ahora comprueba que el rayo detecta la silla, la orientación hacia el microscopio, que no te sientas dos veces y que las demás sillas quedan bloqueadas. Prueba primero la silla nueva. |

No se crea ningún sistema de interacción nuevo. La silla usa el que ya existe en el proyecto: `Apuntable` + `PunteroMuestras` (rayo con la mano vacía, gatillo o A/X) → `Silla.Activar()` → `JugadorNanoLab.Sentarse()` / `Levantarse()`.

## La silla

- **Taburete de laboratorio de microbiología**, no una silla doméstica ni de oficina. Conserva la silueta de la silla original del paquete de assets: asiento redondo, columna central y base en estrella.
- **Asiento a 0,65 m**, la misma altura que el taburete actual. Sentado, los ojos quedan a ≈1,37 m, a la altura del ocular en mesas de 0,9 m.
- **Respaldo lumbar** bajo y curvo, y **aro para los pies** a 0,27 m.
- **Base en estrella de 5 radios con patas fijas** (tacos niveladores, sin ruedas): más estable, y la silla no rueda en la simulación.
- 0,51 × 0,93 × 0,49 m, **2.212 triángulos**, 4 mallas (Asiento, Respaldo, Estructura, Tacos). Sin errores de geometría ni normales invertidas.
- **Reutiliza los materiales `SillaAsiento` y `SillaMetal`** de `Assets/NanoLab/Materiales/Micro/`. El FBX trae materiales con esos nombres y el menú 1 los enlaza a los existentes.
- El prefab tiene el componente `Silla`, anclas **`SitPoint`** (asiento) y **`ExitPoint`** (0,6 m detrás) y **un solo collider trigger** (como `Silla.prefab`). No bloquea al jugador y el rayo lo encuentra.

## Integración en Unity

### 1. Versión correcta del proyecto
El paquete está hecho sobre **`main` en el commit `69afc95`** («add: avance Entregable3», 29-oct-2026). Ese commit es el que introdujo el sistema de sillas. En versiones anteriores las sillas son el `chair.prefab` decorativo y **no existe** `Silla.cs` (ver `Documentacion/AUDITORIA_SILLAS.md`).

```bash
git checkout main
git pull
git log --oneline -1                          # debe ser 69afc95 o posterior
git merge-base --is-ancestor 69afc95 HEAD && echo "OK: contiene 69afc95"
```

### 2. Rama aparte (recomendado)
```bash
git switch -c feature/silla-laboratorio
```
Trabaja con la escena `Microbiologia.unity` cerrada o guardada, y sin cambios sin confirmar en esos 3 scripts.

### 3. Aplicar el parche (desde la raíz del proyecto)
```bash
git apply --check <ruta>/Silla_Laboratorio_NanoLabVR/Unity/cambios_scripts_existentes.patch
git apply         <ruta>/Silla_Laboratorio_NanoLabVR/Unity/cambios_scripts_existentes.patch
git diff --stat    # 3 archivos: Silla.cs, PunteroMuestras.cs, DebugLocomocion.cs
```
Se comprobó que `--check` pasa sobre `origin/main` (`69afc95`).

**Si el parche no aplica:** **no lo fuerces** (nada de `--reject` ni de copiar archivos encima). Significa que alguno de los 3 scripts cambió después de `69afc95`. Mira cuál con `git log --oneline 69afc95..HEAD -- Assets/NanoLab/Scripts/Microbiologia/`. El parche es pequeño y legible: ábrelo y aplica los cambios a mano, o usa `git apply --3way` si sabes resolver conflictos.

### 4. Copiar la carpeta `Assets`
Copia `Unity/Assets/NanoLab/Silla_Laboratorio_Interactuable/` dentro de `Assets/NanoLab/` del proyecto. Solo añade archivos nuevos, no sobrescribe nada. El orden entre los pasos 3 y 4 no importa.

### 5. Importar y comprobar materiales
Abre Unity y espera a que compile **sin errores**. Ejecuta **NanoLab → Silla de laboratorio → 1. Crear prefab**. En la consola:
- No debe aparecer ningún error `[SillaLab]`.
- Debe aparecer `Prefab en … 0.51×0.93×0.49 m, asiento a 0.65 m` (aprox.).
- En el prefab, los renderers deben usar `SillaAsiento` y `SillaMetal` de `Materiales/Micro`. Si el script tuvo que girar el modelo, avisa con un warning.

### 6. Colocar la silla de prueba
**NanoLab → Silla de laboratorio → 2. Probar en el puesto 1 (Microbiología)**. Abre y guarda `Microbiologia.unity`. La silla original `Silla_Puesto_01` queda **desactivada, no borrada**. Ninguna otra sala se toca.

### 7. Prueba automática
**NanoLab → Probar locomoción (play mode)**. En el informe, todas las líneas deben salir `OK`, en especial estas:
- `El rayo encuentra 'Silla_Laboratorio_Puesto_01' y ofrece "sentarte"`
- `Sentado en '…' … locomoción pausada`
- `Sentado mira hacia donde mira la silla, al microscopio`
- `Sentado: volver a sentarse no cambia nada, las otras … sillas no se pueden usar`
- `Al levantarse queda detrás de la silla … locomoción activa`

### 8. Visor VR
Sigue la checklist de `Documentacion/INFORME_PRUEBAS.md` §4: apuntar → «Gatillo o A/X: sentarte» → sentado frente al microscopio → joystick izquierdo para levantarse → caminar y girar con normalidad.

**Restaurar la silla original en cualquier momento:** NanoLab → Silla de laboratorio → **3. Deshacer prueba**.

## Pruebas

| Dónde | Estado |
|---|---|
| Blender: geometría (sin bordes no-manifold, caras degeneradas, duplicados ni normales invertidas), 2.212 triángulos, escala y orientación del FBX, materiales | ✅ Hechas |
| Código: sintaxis C# 9 de los 4 scripts con Roslyn (sin las DLL de Unity) | ✅ Hecha (no sustituye compilar en Unity) |
| El parche aplica sobre `origin/main` (`69afc95`) | ✅ Hecha |
| Unity Editor: compilación, prefab, materiales, prueba automática | ⏳ **Pendiente** |
| Visor VR | ⏳ **Pendiente** |

## A tener en cuenta
- **Mover la silla no está implementado.** El gatillo ya sirve para agarrar y para sentarse, y la silla tendría que ser sólida. Ver `GUIA_INTEGRACION.md` §6.
- Por ahora la silla nueva está **solo en el puesto 1**. Para usarla en los 10 puestos tras validarla, ver `GUIA_INTEGRACION.md` §2 (al final).
- Volver a ejecutar *Montar Microbiología* o *Reemplazar sillas* quita la silla de prueba y pone de nuevo `Silla.prefab` en todos los puestos.
- Las rutas `C:\ihc\...` que aparecen en la auditoría son del equipo donde se preparó el paquete. Solo sirven de referencia.
