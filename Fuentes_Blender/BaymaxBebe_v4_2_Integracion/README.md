# Baymax Bebé v4.2: paquete de integración para Unity

Este paquete entrega el **nuevo modelo de Baymax Bebé (versión v4.2 final)** para integrarlo en el proyecto Unity del avatar VR.

**Validado únicamente en Blender.** El FBX **todavía no se ha probado dentro del proyecto Unity original**. Las comprobaciones que faltan están en «Pendiente en Unity».

---

## 1. Contenido del paquete

```text
BaymaxBebe_v4_2_Integracion/
├── README.md                          este documento
├── Unity/
│   └── Assets/
│       └── BaymaxBebe_v4_2/
│           └── BaymaxBebe_v4_2.fbx    ÚNICO asset para Unity: malla, LOD0/LOD1, cara, esqueleto, pesos y animaciones
├── Documentacion/
│   ├── INFORME_AUDITORIA_BAYMAX.md    análisis del avatar actual (carpeta Baymax) y de la compatibilidad
│   ├── INFORME_VALIDACION_FINAL.md    validación final del modelo y de la exportación
│   ├── tabla_validacion.md            métricas de deformación por pose y animación
│   ├── materiales.json                valores de los 2 materiales
│   ├── export_check.json              comprobaciones de la exportación y prueba de ida y vuelta del FBX en Blender
│   └── previews/                      imágenes y animaciones de las pruebas (ver §9)
└── Fuente_Blender/
    └── baymax_rig_v4_2.blend          archivo fuente (Blender 5.2), para reexportar si hiciera falta
```

- **Archivos `.meta`:** no se incluyen. Unity los genera al importar, con identificadores nuevos, así que no hay conflictos con los assets existentes.
- **Lo que no se toca:** este paquete **no reemplaza ni modifica** el prefab `AvatarBaymax`, los scripts, ni la carpeta `Baymax/` del proyecto.

## 2. Versión que se debe usar

| | |
|---|---|
| Versión | **Baymax Bebé v4.2, rig corregido, final** (2026-10-29) |
| FBX | `BaymaxBebe_v4_2.fbx`; el SHA-256 está en `CHECKSUMS.txt`, junto al ZIP |
| Fuente | `baymax_rig_v4_2.blend` (es el mismo que se usó para exportar) |

No uses versiones anteriores (v3, v4, v4.1, ni el rig v4.2 sin corregir).

## 3. Datos del personaje

| Dato | Valor |
|---|---|
| Altura | **1.20 m** (comprobado al reimportar el FBX: 1.1998 m) |
| Orientación | Mira hacia **+Z** en Unity; la mano derecha queda en +X (en Blender mira a −Y). La exportación usa Forward −Z / Up Y. **Pendiente de confirmar en Unity.** |
| Origen | Entre los pies, a la altura del suelo (y = 0) |
| Tipo | **Malla con esqueleto, `Skinned Mesh Renderer`** (no piezas rígidas) |
| Esqueleto | 36 huesos; `Raiz` es la raíz y no deforma |
| Pesos | Máximo **4 huesos por vértice**, normalizados (el valor por defecto de Unity) |

**LOD:** ambos niveles están **incluidos en el FBX**.

| Nivel | Malla | Triángulos |
|---|---|---|
| LOD0 | `Cuerpo_LOD0` | 50 000 |
| LOD0 | `Cara_LOD0` | 4 796 |
| LOD1 | `Cuerpo_LOD1` | 12 500 |
| LOD1 | `Cara_LOD1` | 4 796 |

- Los sufijos `_LOD0`/`_LOD1` permiten que Unity cree el LOD Group al importar.
- La cara (ojos y línea) es **geometría negra**, no una textura. Está unida al hueso `Cabeza`.

**Huesos principales** (nombres tal como vienen en el FBX; `.L` = izquierda del personaje, `.R` = derecha):

| Grupo | Huesos |
|---|---|
| Tronco y cabeza | `Raiz` → `Pelvis` → `Columna` → `Pecho` → `Cuello` → `Cabeza` |
| Brazo | `Pecho` → `Hombro.L/.R` (clavícula) → `Brazo` (hombro) → `Antebrazo` (codo) → `Mano` (muñeca) |
| Dedos | `Pulgar`, `Indice`, `Medio`, `Menique`, cada uno con `_01` y `_02` |
| Pierna | `Pelvis` → `Muslo` → `Pierna` → `Pie` |

## 4. Animaciones incluidas (30 fps)

| Clip | Fotogramas | Uso |
|---|---|---|
| `Idle` | 61 (bucle) | Respiración y movimiento sutil |
| `Walk` | 25 (ciclo de 24 + cierre, bucle) | Caminata **en el sitio**: el juego debe mover el personaje a **0,1875 m/s**. Paso de 9 cm. Incluye traslación de la pelvis. |
| `Walk_Giro` | 25 (bucle) | Caminata con giro de torso ±12° |
| `Wave` | 50 | Saludo con la mano derecha |
| `Gestos_Manos` | 60 | Mano abierta, puño, dedos separados, cada dedo por separado, pulgar |
| `Cabeza` | 60 | Giro ±35°, inclinación ±8°, asentir 8° |

- **Pose de reposo de las animaciones:** brazos abiertos 12°.
- **Pose de enlace (bind pose) del FBX:** los brazos como están modelados.
- **Nombres al importar:** los clips pueden llegar como `Baymax_Rig|Walk`, etc.; se pueden renombrar en la pestaña Animation.

## 5. Validaciones realizadas (solo en Blender)

Detalle completo en `Documentacion/INFORME_VALIDACION_FINAL.md` y `Documentacion/tabla_validacion.md`.

**Deformación:**
- 0 autointersecciones en: reposo, brazos abiertos 30°/45°, brazos elevados 60°/75°/90°, codo 45°/65°, saludo, sentadilla, giro de cabeza ±35°.
- **Codo limitado a 65°:** una petición de 95° da 65° en Blender.

**Dedos:**
- Se mueven de forma independiente.
- Al doblar un dedo, los vecinos se mueven ≤ 4,8 mm.
- En el puño hay contactos dedo–palma (dedos gruesos), visualmente limpios.

**Caminata:**
- Deslizamiento del pie de apoyo: 0,01 mm. El pie no atraviesa el suelo y las piernas no se cruzan (separación mínima 3,2 cm).
- **Queda contacto vientre–pierna escondido bajo la barriga** (máx. 69 pares de triángulos). Es geométrico.

**LOD1:** 0 autointersecciones en todas las pruebas salvo la caminata (máx. 22).

**Exportación** (`Documentacion/export_check.json`):
- El FBX se reimportó en Blender y la malla deformada se comparó con la original en 8 fotogramas de las 6 animaciones: desviación máxima **0,014 mm**.
- Los 36 huesos, los 6 clips y los rangos llegan correctos.
- **Esto es una prueba en Blender, no en Unity.**

## 6. Cómo importar el FBX en Unity

1. **Copia** `Unity/Assets/BaymaxBebe_v4_2/` dentro de la carpeta `Assets/` del proyecto (queda `Assets/BaymaxBebe_v4_2/BaymaxBebe_v4_2.fbx`).
   - Es una **carpeta nueva**: no sustituye nada.
2. **Selecciona el FBX** y revisa estas pestañas:

| Pestaña | Ajuste | Valor recomendado |
|---|---|---|
| **Model** | Scale Factor | 1 |
| | Convert Units | Activado |
| | **Bake Axis Conversion** | Activado (si no, la raíz puede entrar rotada −90° en X) |
| | Import BlendShapes | Desactivado (no hay) |
| | Read/Write | Solo si algún script lee la malla |
| **Rig** | Animation Type | **Generic** |
| | Avatar Definition / Root node | Create From This Model / **`Raiz`** |
| | Skin Weights | 4 Bones |
| **Animation** | Clips | Activa **Loop Time** en `Idle`, `Walk` y `Walk_Giro` |
| **Materials** | Material Creation | Extract Materials, por ejemplo a `Assets/BaymaxBebe_v4_2/Materiales/`; luego ajústalos según §7 |

3. **Prueba en una escena vacía** antes de tocar el avatar:
   - altura 1,20 m;
   - mira hacia +Z;
   - el LOD Group se ha creado;
   - los clips se reproducen;
   - en `Walk` la **pelvis se balancea y sube/baja**;
   - en `Gestos_Manos` los dedos se doblan;
   - la cara sigue a la cabeza.

**Por qué Generic:** Humanoid no se recomienda por ahora, porque los dedos tienen 2 falanges y el modelo no está en T-pose.

## 7. Materiales

El modelo **no usa texturas** y el cuerpo **no tiene UV**: son colores planos. Valores de Blender (`Documentacion/materiales.json`):

| Material | Usado en | Color base (RGB lineal 0–1) | Rugosidad | Metálico | Solo Blender (no pasan al FBX) |
|---|---|---|---|---|---|
| `Baymax_Marfil_Satinado` | `Cuerpo_LOD0/1` | 0,86 / 0,84 / 0,80 | 0,42 | 0 | coat 0,12, subsurface 0,08 |
| `Baymax_Ojos_Negro` | `Cara_LOD0/1` | 0 / 0 / 0 | 1,0 | 0 | — |

**En Unity:**
- Los materiales del avatar actual usan el shader **Standard** (pipeline Built-in), así que se recomienda **Standard** también aquí:
  - marfil: Smoothness ≈ 0,58 (= 1 − rugosidad);
  - negro: Smoothness 0.
- Si el proyecto usa URP, el equivalente es URP/Lit con los mismos valores.
- **No se usan** los materiales `Baymax_image_*.mat` ni las texturas del avatar actual: dependen de las UV de las piezas antiguas.
- **Corrección:** `INFORME_VALIDACION_FINAL.md` sugería URP/Lit. La auditoría posterior de la carpeta `Baymax` mostró que el proyecto usa Standard. Prevalece este README.

## 8. Advertencias de integración

1. **El avatar original usa piezas independientes y tracking VR.**
   - `AvatarBaymax` son 8 mallas rígidas.
   - Un script (GUID `e15141ca…`, no recibido) gira los pivotes `Cuello`, `HombroIzq/Der` y coloca `ManoIzq/Der` en los mandos. También estira los brazos escalándolos.
2. **El nuevo modelo es una única malla con esqueleto** (`Skinned Mesh Renderer`). No tiene piezas separadas que activar, desactivar o escalar.
3. **Los brazos necesitan IK de dos huesos** (`Brazo → Antebrazo → Mano`) para seguir a los mandos, por ejemplo con Unity Animation Rigging (Two Bone IK) o con un script propio. Escalar huesos para estirar el brazo deforma la piel.
4. **Las manos forman parte del esqueleto:** `Mano` es hija de `Antebrazo`. Ya no cuelgan de la raíz como `ManoIzq/Der`; moverlas libremente estira el brazo.
5. **Primera persona y ocultación del cuerpo hay que adaptarlas.**
   - El script oculta la cabeza desactivando su Renderer, y usa la capa 29 para el cuerpo y la capa 0 para las manos.
   - Con una sola malla, desactivar el Renderer oculta **todo el cuerpo**, y no se pueden poner las manos en otra capa.
   - Opciones (decisión vuestra):
     - **(a)** escalar el hueso `Cabeza` a ~0 para la cámara en primera persona;
     - **(b)** que exportemos cuerpo y manos como dos `SkinnedMeshRenderer` que comparten esqueleto;
     - **(c)** ocultar solo la cara (`Cara_LOD0/1` es un Renderer aparte).
6. **El codo está limitado a 65° solo en Blender.**
   - Las animaciones incluidas lo respetan.
   - Unity no recibe esa restricción: el IK o cualquier movimiento procedural **debe limitar el codo a 65°**. Más allá, la cara interna del codo se interpenetra.
   - Otros límites validados en Blender:
     - elevación del brazo hasta ~75–90°;
     - inclinación de cabeza ±8°;
     - no bajar los brazos por debajo de la pose modelada (se meten en la barriga).
7. **Las animaciones y el movimiento del esqueleto hay que probarlos en Unity.**
   - **Caveat de Blender:** al reimportar el FBX en Blender, el importador «conecta» `Pelvis` a `Raiz` y se pierde la traslación de la pelvis en `Walk`. El FBX sí la contiene (está verificado). **Comprobad en Unity que la pelvis se mueve.**
8. **El FBX todavía no se ha probado dentro del proyecto Unity original.** Nada de este paquete está validado en Unity.

## 9. Previews (`Documentacion/previews/`)

| Archivo | Muestra |
|---|---|
| `poses_brazos_frontal.jpg` | Reposo, brazos 30°/45°, elevados 60°/75°/90°, codos 45–70° (a partir de 65° el límite actúa), balanceo y saludo |
| `caminata_fotogramas.jpg` | Fotogramas de `Walk` y `Walk_Giro` desde frente, tres cuartos y lateral |
| `detalles.jpg` | Primeros planos: puño, índice, pulgar, hombro a 75°, codo al límite, unión pierna–cadera |
| `caminata_tres_cuartos.gif` | Ciclo de caminata animado |
| `caminata_lateral.gif` | Ciclo de caminata animado, de lado |
| `brazos_continuo_frontal.gif` | Movimiento continuo de los brazos (abrir, elevar, codo, balanceo) |

Los renders son de Blender (Cycles y EEVEE), no de Unity.

## 10. Pendiente en Unity (integración con el controlador VR)

**Pasos:**
1. Crear un prefab **nuevo** (por ejemplo, `AvatarBaymaxBebe_v4_2`), sin reemplazar `AvatarBaymax`, con el FBX y el script controlador.
2. Asignar los campos del script a los huesos nuevos:

| Campo del script | Hueso nuevo |
|---|---|
| `cuello` | `Cabeza` (o `Cuello`) |
| `hombroDer` / `hombroIzq` | `Brazo.R` / `Brazo.L` |
| `manoDer` / `manoIzq` | `Mano.R` / `Mano.L`, con IK |

3. Recalcular los valores de tamaño para 1,20 m. Los valores propuestos están en `INFORME_AUDITORIA_BAYMAX.md` §4 y hay que confirmarlos con el código:

| Campo | Valor propuesto |
|---|---|
| `largoBrazo` | ≈ 0,20 |
| `largoMano` | ≈ 0,17 |
| `ojos` | ≈ (0, 0,94, 0,09) |

4. Implementar el IK de los brazos con el límite de codo de 65°.
5. Resolver la ocultación en primera persona y las capas (§8.5).
6. Decidir si se usan los clips (`Walk` para las piernas, `Gestos_Manos` con el gatillo, `Idle`) mediante un Animator Controller con IK encima.
7. Sombras: el avatar actual tiene `Cast Shadows = Off` en todo; replicarlo si se quiere el mismo aspecto.
8. Probar en VR: altura de cámara, alcance a los mandos, LOD1 a distancia y rendimiento.

**Para completar la integración necesitamos de vuestro proyecto:**
- el script controlador (`.cs`);
- `ProjectSettings/ProjectVersion.txt` y `Packages/manifest.json`;
- el nombre de la capa 29;
- cómo están configuradas la cámara y la escena que usan `AvatarBaymax`;
- el pipeline de render (`GraphicsSettings`).
