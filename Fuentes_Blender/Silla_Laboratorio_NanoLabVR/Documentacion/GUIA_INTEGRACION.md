# Guía de integración — Silla de laboratorio interactuable

Base: rama `main` de `first-ot-year/NanoLab_ihc` en el commit **`69afc95`** o posterior. Las versiones anteriores no tienen el sistema de sillas (ver `Auditoria/AUDITORIA_SILLAS.md`).

## 1. Qué se entrega

```text
Silla_Laboratorio_NanoLabVR/
├── README.md
├── Blender/
│   ├── Silla_Laboratorio_Interactuable.blend      modelo fuente
│   ├── Silla_Laboratorio_Interactuable.fbx        exportación final (idéntica a la de Unity/.../Models)
│   ├── modelar_silla.py                           genera el .blend y el .fbx (reproducible)
│   └── validar.py                                 comprobaciones de geometría y del FBX exportado
├── Unity/
│   ├── cambios_scripts_existentes.patch           diff de los 3 scripts modificados (git apply)
│   └── Assets/NanoLab/Silla_Laboratorio_Interactuable/
│       ├── Models/Silla_Laboratorio_Interactuable.fbx
│       └── Editor/MontarSillaLaboratorio.cs       crea el prefab y lo prueba en un puesto
└── Documentacion/ (AUDITORIA_SILLAS.md, esta guía, INFORME_PRUEBAS.md, img/)
```

Se adaptó la estructura pedida a la del proyecto, `Assets/NanoLab/…`. **No hay carpeta `Materials`**, porque se reutilizan `Assets/NanoLab/Materiales/Micro/SillaAsiento.mat` y `SillaMetal.mat`. **No hay `Scripts` nuevos en tiempo de ejecución**, porque se reutiliza `Silla.cs`. La carpeta `Prefabs` la crea el menú 1.

## 2. Pasos

1. Actualiza la copia de trabajo: `git pull` en `main`. Comprueba que existen `Assets/NanoLab/Scripts/Microbiologia/Silla.cs` y `Assets/NanoLab/Prefabs/Silla.prefab`.
2. Copia `Unity\Assets\NanoLab\Silla_Laboratorio_Interactuable\` dentro de `Assets/NanoLab/`.
3. Aplica los cambios de scripts desde la raíz del proyecto: primero `git apply --check <ruta>/Unity/cambios_scripts_existentes.patch` y, si no da errores, `git apply` con la misma ruta. Si `--check` falla, **no lo fuerces** (ver README, «Si el parche no aplica»).
4. Abre Unity y espera a que compile. **Esto no se ha podido comprobar aquí: no hay Unity instalado.**
5. Menú **NanoLab → Silla de laboratorio → 1. Crear prefab**. Configura el importador del FBX, enlaza los materiales existentes por nombre y crea `Prefabs/Silla_Laboratorio_Interactuable.prefab`. Revisa la consola: no debe haber `[SillaLab]` en rojo.
6. Menú **2. Probar en el puesto 1 (Microbiología)**. Pone la silla nueva en la pose de `Silla_Puesto_01` (`LAB_Microbiologia/Microbiologia/Sillas`) y **desactiva** la original, sin borrarla.
7. Menú **NanoLab → Probar locomoción (play mode)**. Prueba automática: la silla nueva se elige primero. Todas las líneas deben salir `OK`.
8. Prueba con el visor (lista en `INFORME_PRUEBAS.md`).
9. Para volver atrás: **3. Deshacer prueba**.

Solo cuando la silla esté validada en el visor, para usarla en los 10 puestos basta con cambiar en `MontarSillas.cs` la línea `PrefabRuta` por `"Assets/NanoLab/Silla_Laboratorio_Interactuable/Prefabs/Silla_Laboratorio_Interactuable.prefab"` y ejecutar *NanoLab/Reemplazar sillas (las dos salas)*. **Ojo:** ese menú también cambia las sillas de Química. Si solo quieres Microbiología, ejecuta *Montar Microbiología*. No se ha hecho aquí por la regla de no sustituir sin validar.

## 3. El prefab

```text
Silla_Laboratorio_Interactuable    Silla (Apuntable) + BoxCollider trigger (bounds del modelo)
├── Modelo                         instancia del FBX
│   ├── Asiento                    SillaAsiento
│   ├── Respaldo                   SillaAsiento
│   ├── Estructura                 SillaMetal
│   └── Tacos                      SillaAsiento
├── SitPoint                       (0, 0.652, 0) → campo Silla.asiento
└── ExitPoint                      (0, 0, -0.6) → campo Silla.salida
```

- **Orientación:** el frente de la silla es +Z. `MontarSillas.EnPuestos` ya la coloca mirando al microscopio, y la prueba copia esa pose. El script comprueba que el respaldo quede en −Z y gira el modelo si no.
- **Altura:** asiento a 0,65 m, igual que el taburete anterior. Con `ojosSobreAsiento` = 0,72, los ojos quedan a 1,37 m, a la altura del ocular en mesas de 0,9 m. La cámara no se mueve a mano: `JugadorNanoLab` usa `HeightOffset` del Locomotor según la altura real de la cabeza.
- **Colliders:** solo el trigger, igual que `Silla.prefab`. No bloquea al jugador, que puede usar el microscopio de pie con la silla delante, ni al rayo de otras cosas. No hay colliders sólidos que desactivar al sentarse.
- **Iluminación:** `receiveGI = LightProbes`, igual que las sillas actuales, porque la sala está horneada.

## 4. Cambios en scripts existentes (mínimos)

| Archivo | Cambio | Motivo |
|---|---|---|
| `Silla.cs` | `Disponible` es falso para las demás sillas mientras el jugador está sentado. | Sentado, el rayo activaba otra silla y el jugador saltaba de una a otra. Ahora solo puede levantarse. |
| `PunteroMuestras.cs` | La búsqueda del rayo de mano vacía pasa a `public static ApuntableEn(...)`. Mismo comportamiento. | Que la prueba use **el mismo código** que el visor para detectar la silla. |
| `DebugLocomocion.cs` | Comprobaciones nuevas: el rayo encuentra la silla, orientación al sentarse, no se sienta dos veces y las demás sillas quedan bloqueadas. La silla nueva se prueba primero. | La prueba anterior llamaba a `Activar()` directamente y no probaba la detección. |

## 5. Diseño de la silla

- **Referencia:** el taburete original (asiento redondo, columna central, base en estrella), del que se conserva la silueta, y el taburete actual, del que se toman las medidas funcionales.
- **Fijo, sin regulación ni ruedas.** El sistema de sentado fija la altura de los ojos desde el asiento, y una regulación no tendría interacción. Con ruedas, la silla parecería que rueda cuando no lo hace. Se usan tacos niveladores fijos, más estables.
- **Respaldo lumbar bajo**, curvo, centrado a 0,86 m, para mantener la postura al inclinarse sobre el ocular sin chocar con la mesa.
- **Aro reposapiés** a 0,27 m, necesario con un asiento a 0,65 m.
- **Base de 5 radios** de 0,27 m de radio (más estable que la de 4), con uno hacia atrás para dejar el frente abierto hacia la mesa.
- **Medidas:** 0,51 × 0,93 × 0,49 m (ancho × alto × fondo). 2212 triángulos, 4 mallas, 2 materiales existentes. Sin texturas: no existían y no se inventaron.

## 6. Fase 4 — mover la silla: **no implementada**

No se implementó porque comprometería la interacción principal:

1. **Conflicto de botones.** `JugadorNanoLab.ConfigurarAgarre()` añade el **gatillo** a todos los selectores de agarre de Meta. En una silla `Grabbable`, el mismo gatillo serviría para sentarse (`Apuntable`) y para agarrarla. Habría que separar las acciones (por ejemplo, solo grip para agarrar sillas), y esa configuración hoy es global para todo el laboratorio.
2. **Colliders.** El diseño actual no tiene colliders sólidos a propósito, para no bloquear al jugador frente al microscopio. Una silla con física necesita colliders sólidos, y eso vuelve a hacer de la silla un obstáculo.
3. **Orientación.** `EnPuestos` coloca cada silla mirando a su microscopio. Si se mueve, `Sentarse` orientaría al jugador hacia donde mire la silla, no hacia el microscopio.

Qué haría falta, en orden:
- (a) Selectores de agarre por objeto, con solo grip para las sillas.
- (b) `Rigidbody` kinemático hasta agarrarla (`QuietoHastaAgarrar` ya existe), con `freezeRotation X/Z` y colliders sólidos en una capa que no choque con la cápsula del jugador.
- (c) En `Silla.Activar`, no sentarse mientras está agarrada.
- (d) Comprobar al soltarla que `ExitPoint` queda libre, con `Physics.CheckCapsule`, y si no, probar a los lados.
- (e) Orientar al sentarse hacia el microscopio más cercano, no hacia `forward`.

Bloquear el movimiento mientras está ocupada ya lo da `Silla.Ocupada`.

## 7. Limitaciones conocidas

- `ExitPoint` está fijo a 0,6 m detrás. Las sillas están en puestos con pasillo detrás, pero no se comprueba que el punto esté libre. Solo importa si las sillas se mueven.
- Volver a montar la sala (*Montar Microbiología* o *Reemplazar sillas*) quita la silla de prueba y vuelve a poner `Silla.prefab` en todos los puestos.
- El FBX no tiene `.meta`: lo genera Unity al importarlo. Quien lo importe primero debe subir ese `.meta` para que el GUID sea estable en el equipo.
