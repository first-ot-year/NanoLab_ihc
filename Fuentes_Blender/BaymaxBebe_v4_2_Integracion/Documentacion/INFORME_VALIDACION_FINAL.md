# Baby Baymax v4.2: final rig, validation and FBX test (2026-10-29), awaiting approval

**Status:** final adjustments made, validated in Blender, and a test FBX exported and checked with a Blender round trip.
- **The FBX has NOT been tested in Unity** (we don't have the project). The checks still to be done in Unity are listed at the end.
- **The geometry of v4.2 is unchanged.**

## Files (`rig_unity/v4_2_rig_corregido/final/`)
| File | Contents |
|---|---|
| `baymax_rig_v4_2.blend` | Final rig: LOD0/LOD1, face, armature, weights, every action (including the test ones) and the v3 studio |
| `export/BaymaxBebe_v4_2.fbx` | **Test FBX**, 3.6 MB: armature + `Cuerpo_LOD0/LOD1` + `Cara_LOD0/LOD1` + 6 game animations |
| `export/baymax_bebe_v4_2_export.blend` | The copy the FBX came from: game actions only, bind pose, `Cara` renamed `Cara_LOD0` |
| `export/materiales.json` | The 2 materials and their values; no textures |
| `export/export_check.json` | Pre-export checks and the Blender round trip |
| `metricas/tabla_validacion.md` | Validation, approved rig vs final (generated from `fase5_pruebas.json`) |
| `renders/hoja_brazos_{frontal,tres_cuartos,lateral}.png` | Poses (Cycles, v3 studio) |
| `renders/hoja_caminata.png` | Walk frames |
| `renders/hoja_detalles.png` | Close-ups (hands, shoulder, elbow, leg–hip junction) |
| `renders/caminata_{frontal,tres_cuartos,lateral}.gif/.mp4` | Continuous walk (EEVEE) |
| `renders/brazos_continuo_{frontal,tres_cuartos}.gif/.mp4` | Continuous arm movement (EEVEE) |
| `renders/comparacion_caminata_{lateral,tres_cuartos}.gif` | Approved walk vs final walk side by side, synchronised (96 frames) |

**Walk candidates:** `../candidato_paso_actual/`, `../candidato_paso_105/` and `../candidato_paso_090/`; the last one is identical to `final`.

**Reproduction (from `scripts/`):**
1. `fase4_rig.py -- v4.2 out=v4_2_rig_corregido/final fix=hombro,pesos,piernas,limites codo=65 saludo=1 paso=0.09,24`
2. `fase5_pruebas.py -- v4.2 out=v4_2_rig_corregido/final`
3. `v42_codo_render.py -- out=v4_2_rig_corregido/final`
4. `fase7_export.py -- out=v4_2_rig_corregido/final`

Rebuilding is deterministic: the final rig produced metrics identical, byte for byte, to its candidate.

**Untouched:**
- v1, v2, v3, v4, v4.1, v4.2;
- `v4_2_rig`, and the approved rig in `v4_2_rig_corregido/` (including its `.blend`);
- the Unity assets (86/86 hashes).

## Phase 1: final adjustments
1. **Elbow ≤ 65°.** Verified with a `LIMIT_ROTATION` self-check: 45° → 45°, 70° → 65°, 95° → 65°. The 70° test pose now gives 0 self-intersections.
2. **Wave:** the head tilt (−6°) is replaced by an 8° turn towards the hand. Head/neck contacts go **5 → 0**.
3. **Walk with shorter steps at the same speed.** Step and cycle shrink in the same proportion, so the speed stays at 0.1875 m/s:

| Variant | Step / cycle | Max self-int. | Mean per frame | Frames with contact | Foot slip |
|---|---|---|---|---|---|
| Approved | 12 cm / 32 frames | 78 | 36.9 | 24 of 32 | 3.54 mm |
| Shorter | 10.5 cm / 28 | 76 | 36.3 | 22 of 28 | 0.86 mm |
| **Final** | **9 cm / 24** | **69** | **33.5** | **20 of 24** | **0.01 mm** |

**Chosen:** 9 cm / 24 frames.
- It improves every walk metric, and foot slip practically disappears.
- Cadence rises to 2.5 steps per second (a quick toddler waddle).
- The hip contacts drop by ~10 % but **are not eliminated**: they are the overhang of the belly over the legs, which is geometric.

## Phase 2: validation (LOD0; self-intersections / folded / creased triangles)
Full table: `metricas/tabla_validacion.md`.

| Test | Approved rig | Final | Final LOD1 (self-int. / folded) |
|---|---|---|---|
| Idle | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 |
| Wave | 5 / 12 / 30 | **0** / 1 / 11 | 0 / 0 |
| Raised 60° / 75° / 90° | 0 / 0 / 0 self-int. | 0 / 0 / 0 self-int. | 0 / 0 |
| Elbow 45° / 65° | 0 / 0 | 0 / 0 | 0 / 1 |
| Elbow 70° requested | 1 | **0** (limited to 65°) | 0 / 1 |
| Individual fingers | fist 40, index 15, middle 1, little 18, thumb 7 | same | 0, 0, 0, 0, 0 |
| Squat | 0 / 0 / 0 | 0 / 0 / 0 | 0 / 0 |
| Walk, every frame | max 78, mean 36.9, slip 3.5 mm | **max 69, mean 33.5, slip 0.01 mm** | max 22 |
| Walk + torso twist ±12° (8 frames) | 55 | 61 (other frames sampled; see note) | 22 |
| Head: turn ±35° / tilt ±8° | 1 / 11 | 1 / 11 | 0 / 0 |

- **Fingers:** when one finger bends, the neighbouring fingers move 0 mm on the left hand and 1.5–2.5 mm on the right (4.8 mm next to the thumb). The palm moves ≤ 1.8 mm.
  - The fist and single-finger contacts are thick fingers touching the palm (unchanged, and visually clean in `hoja_detalles`).
- **Feet:** no floor penetration (−0.06 mm); minimum leg-to-leg gap 3.2 cm.
- **Note:** the "8 frames" rows sample different frames for a 24-frame cycle than for a 32-frame one; the every-frame row is the comparable one.

## Phase 3: preparing for Unity
**Pre-export checks (`export/export_check.json`):**
- **Transforms:** armature and the 4 meshes have identity transforms and are parented to the armature. The origin is at the feet (min z = 0), and the height is 1.200 m.
- **Skeleton:** 36 bones, names unchanged (`Raiz`, `Pelvis`, `Columna`, `Pecho`, `Cuello`, `Cabeza`, `Hombro/Brazo/Antebrazo/Mano.L/.R`, `Pulgar/Indice/Medio/Menique_01/02.L/.R`, `Muslo/Pierna/Pie.L/.R`). `Raiz` does not deform.
- **Skinning:** ≤ 4 influences per vertex and weights normalised (sum = 1.000) on all 4 meshes, matching Unity's default. The only modifier is Armature.
- **LODs:**

  | LOD | Mesh | Triangles |
  |---|---|---|
  | LOD0 | `Cuerpo_LOD0` | 50,000 |
  | LOD0 | `Cara_LOD0` | 4,796 |
  | LOD1 | `Cuerpo_LOD1` | 12,500 |
  | LOD1 | `Cara_LOD1` | 4,796 |

  The `_LOD0`/`_LOD1` suffixes are the ones Unity uses to create a LOD Group automatically on import.
- **Animations in the FBX:** `Idle` (61 frames), `Walk` (25 = a 24-frame cycle + the closing frame), `Walk_Giro` (25), `Wave` (50), `Gestos_Manos` (60), `Cabeza` (60).
  - The test actions stay in the `.blend` and are not exported.
  - Walk is **in place**: the game moves the character at **0.1875 m/s**.
- **Materials:** `Baymax_Marfil_Satinado` (base 0.86/0.84/0.80, roughness 0.42) and `Baymax_Ojos_Negro` (black, roughness 1.0).
  - **No textures** (plain colours), so none are needed.
  - The FBX carries the base colour; the coat (0.12) and subsurface (0.08) are Blender-only and do not carry over.
- **UVs:** the body has **no UVs**, which is fine for plain colours. If textures or lightmaps are wanted later, they would have to be added.
- **Export settings:**
  - Forward −Z, Up Y;
  - scale "FBX All" with unit scale applied;
  - no leaf bones;
  - "Apply Transform" **disabled** (it is experimental and breaks armatures);
  - every action baked with no simplification.

**Round trip in Blender:** the FBX was reimported into an empty scene and compared with the original.
- The 36 bones and the 6 animations are present, with the correct ranges. Height 1.1998 m, character facing −Y, same vertex counts.
- **Deformed LOD0 at 8 frames of the 6 animations: maximum deviation 0.014 mm.** Skinning, bind pose and animation survive the FBX.
- **Caveat found and fixed in the check:** Blender's FBX importer *connects* 18 bones when rebuilding the skeleton, `Pelvis` among them. A connected bone ignores its location, so in Blender the walk lost the pelvis translation (14–18 mm). The FBX does contain that translation (verified on the curves). Unity doesn't connect bones, so it shouldn't happen there, but **this is unverified**.

## Recommended Unity import settings (to try; not verified)
| Area | Setting |
|---|---|
| Model | Scale Factor 1, Convert Units on, Bake Axis Conversion on (otherwise the root may come in rotated −90° on X), Import BlendShapes off |
| Rig | Animation Type **Generic**, Root node `Raiz`, Skin Weights 4 bones |
| Animation | Loop Time on for `Idle`, `Walk` and `Walk_Giro`. Clip names may arrive as `Baymax_Rig\|Walk`. |
| Materials | Extract Materials. URP/Lit: ivory base colour, Smoothness ≈ 0.58; eyes black, Smoothness 0 |

- Humanoid is not recommended at this stage: the fingers have 2 phalanges and there is no T-pose.
- `LIMIT_ROTATION` (elbow) does not exist in Unity. The animations already respect it, but procedural motion or IK in Unity must cap the elbow at 65° itself.

## Pending
1. **Integration in Unity** (none of it verified):
   - orientation and scale on import;
   - the automatic LOD Group;
   - clip loops;
   - materials;
   - the walk's pelvis translation;
   - performance.
2. **Belly–leg contact in the walk:** up to 69 pairs, hidden under the belly. It is geometric; it would need corrective blendshapes or retouching the belly.
3. **Small dip at the base of the neck** with the arm at ≥ 60°: residual geometry of v4.2.
4. **No UVs on the body:** only needed if textures or lightmaps are added.
5. **The current prefab `AvatarBaymax.prefab`** uses rigid pieces and a script that hides renderers by piece (see `tecnico_unity/COMPATIBILIDAD_UNITY_PASO1.md`). It is not compatible with a single skinned mesh without changes to that script. Not touched.
