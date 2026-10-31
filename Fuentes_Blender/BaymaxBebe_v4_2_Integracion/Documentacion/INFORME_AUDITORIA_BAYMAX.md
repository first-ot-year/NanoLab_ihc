# Integrating Baby Baymax v4.2 into the Unity project: audit of the `Baymax` folder (2026-10-29)

**Scope:** read-only inspection of `C:\ihc\baymax\Baymax\`, the folder shared by your colleague. No file has been modified, moved or exported (86/86 hashes intact).
- Nothing has been tested in Unity.
- Where there is inference without proof, it is marked **(inference)**.
- `C:\ihc\baymax\BaymaxBebe\` also exists: an earlier rigid-piece baby that uses the same script. It is outside the scope requested and was not analysed.

## 1. Structure received
```
Baymax/                                   (39 files, all dated 2026-10-27 18:33)
├─ AvatarBaymax.prefab (+.meta)           the avatar: hierarchy, renderers, controller script and its values
├─ Baymax_image_0/2/4/6/8.mat (+.meta)    5 materials, Built-in "Standard" shader (fileID 46), each with a texture in _MainTex
├─ Mallas/ (+.meta)                       8 rigid meshes (.asset): Cabeza, Cuerpo, BrazoDer/Izq, ManoDer/Izq, PiernaDer/Izq
└─ Texturas/ (+.meta)                     5 PNG 512×512 (sRGB): almost uniform white/grey; image_4 holds the painted face (eyes + line)
```
| Type | Found | Not in the folder |
|---|---|---|
| 3D models | 8 `Mesh` .asset files (no FBX/OBJ) | — |
| Prefabs | `AvatarBaymax.prefab` | — |
| C# scripts | **none** | the controller script, **GUID `e15141ca0fc58d14bbfa393b741d97ef`** |
| Materials / textures | 5 .mat + 5 .png | — |
| Animations / Animator Controllers / Avatars | **none** | — (the avatar has no Animator) |
| Built-in references | built-in Sphere mesh (fileID 10207), built-in Standard shader (fileID 46) | come with Unity |

**Unity version (inference):** the serialized fields (`m_RayTraceProcedural` present; `m_SmallMeshCulling` and `m_RayTracingAccelStructBuildFlagsOverride` absent) point to **2021.3–2022.3 LTS**. Confirm with `ProjectSettings/ProjectVersion.txt`.

**Render pipeline (inference):** the materials use the Built-in **Standard** shader, so the project is probably on the Built-in pipeline, not URP. **This corrects** the earlier recommendation (URP/Lit) in `INFORME_VALIDACION_FINAL.md`.

## 2. Architecture of the current Baymax
A rigid-piece avatar with no skeleton and no animations. It is driven by **one script** that moves empty pivots.
```
AvatarBaymax  (layer 0)  ← MonoBehaviour e15141ca… (the controller)
├─ Cuerpo      mesh Baymax_Cuerpo      mat image_0            layer 29
├─ PiernaDer   mesh Baymax_PiernaDer   mat image_8            layer 29
├─ PiernaIzq   mesh Baymax_PiernaIzq   mat image_8            layer 29
├─ Cuello      pivot (−0.012, 1.236, −0.015)                  layer 0
│  └─ Cabeza   mesh Baymax_Cabeza      mat image_4 (face)     layer 29
├─ HombroDer   pivot ( 0.415, 1.040, −0.020)                  layer 0
│  └─ BrazoDer mesh Baymax_BrazoDer    mats image_2/4/6       layer 29
├─ HombroIzq   pivot (−0.417, 1.061, −0.033)                  layer 0
│  └─ BrazoIzq mesh Baymax_BrazoIzq    mats image_2/6         layer 29
├─ ManoDer     ( 0.535, 0.555, −0.002) mesh ManoDer, mats 2/4/6   layer 0   ← child of the ROOT, not the arm
│  └─ Puno     built-in Sphere, scale (0.235, 0.153, 0.235), mat image_2   layer 0
└─ ManoIzq     (−0.536, 0.569, −0.014) mesh ManoIzq, mats 2/6     layer 0
   └─ Puno     built-in Sphere, same scale, mat image_2               layer 0
```
- **Size and orientation:**
  - About **1.68 m** tall (head top ≈ 1.68 m).
  - Faces **+Z**: the eye point sits at z +0.097. The right hand (`ManoDer`) is at **+X**.
- **Geometry:** ~19,800 triangles in 8 meshes, all readable, with no bindposes or bones (they are rigid).
- **Shadows:** every renderer has `Cast Shadows = Off`.
- **Layers:** body, head, arms and legs are on **layer 29**; pivots, **hands and fists** are on layer 0.
  - **(Inference)** Layer 29 is hidden from the first-person camera (culling mask), so you only see your own hands.

**Script values (serialized fields of the controller):**

| Field | Value | Probable use (inference) |
|---|---|---|
| `cuello`, `hombroIzq`, `hombroDer` | Transforms of the pivots | Rotate head and arms |
| `manoIzq`, `manoDer` | Transforms of the hands | **Placed independently** at the controllers' / tracked hands' positions |
| `cabeza` | list of Renderers: `[Cabeza MeshRenderer]` | Hide the head in first person |
| `ojos` | (−0.012, 1.436, 0.097) | Eye point: aligns avatar and camera |
| `largoBrazo` | 0.4846 | Exactly the vertical drop `HombroDer → ManoDer` (1.0400 − 0.5554); used to stretch the arm towards the hand |
| `largoMano` | 0.1367 | Hand length/offset |
| `proporcion` | (0.9, 1.2, 0.9) | Body scale adjustment |
| `escalaMin` / `escalaMax` | 0.5 / 1.3 | Scale limits (arm and body) |
| `retrocesoMano` | 0.03 | Pulls the hand back from the tracked point |
| `giroLibre` | 35 | Degrees the head can turn before the body follows |
| `cuerpoEnPrimeraPersona` | 0 | Show the body in first person: off |
| `ocultarManosMeta` | 1 | Hide the hands under a "Meta" mode, **(inference)** Meta Quest / hand tracking |

**Reading:** this is a **procedural VR avatar driven by tracking**: head = HMD, hands = controllers, arms stretched by scale. It is **not** a character that plays animations. The `Puno` spheres are probably a "closed hand" toggled by the script **(inference: they are not referenced by any field, so it would have to find them by name or through `GetComponentsInChildren`)**.

## 3. Scripts and dependencies
| Script | Location | Responsibility | Depends on |
|---|---|---|---|
| Controller `e15141ca…` | **Not received** | Moves `Cuello`, `HombroIzq/Der`, `ManoIzq/Der`; hides `cabeza` renderers; aligns `ojos`; scales the arms | The exact hierarchy and names above; separate renderers per piece; layers 29/0; possibly `Puno` by name |

**Dependencies on the individual pieces (what breaks with a single skinned mesh):**
1. **`cabeza` = list of Renderers.** The new body is one `SkinnedMeshRenderer`. Disabling it would hide the **whole** body. Only the face (`Cara_LOD0/1`) is a separate renderer.
2. **Layers 29/0 per piece.** A single renderer has one layer, so hands visible and body hidden can't be split by layer.
3. **Free hands + arms stretched by scale.**
   - In the new rig `Mano` is a child of `Antebrazo`. Moving it freely tears the skin, and scaling bones distorts the weighting.
   - It needs **two-bone IK** instead: `Brazo → Antebrazo → Mano` aiming at the controller (Unity Animation Rigging or a small script).
4. **`Puno`** (sphere) doesn't exist in the new model. The fist is done by bending the fingers (2 phalanges per finger).
5. **Size values** (`ojos`, `largoBrazo`, `largoMano`, `proporcion`, scales) are tuned for a 1.68 m avatar; the new one is **1.20 m**.

## 4. Current vs new
| | Current `AvatarBaymax` | New `BaymaxBebe_v4_2.fbx` |
|---|---|---|
| Format | 8 rigid meshes + 2 spheres, Mesh .asset | FBX: armature + 2 skinned bodies (LOD0 50,000 tris / LOD1 12,500) + 2 faces |
| Height / orientation | ~1.68 m, faces +Z, right hand at +X | **1.20 m**, faces −Y in Blender, which should arrive as **+Z** in Unity with Forward −Z / Up Y (**not verified**), right hand at **+X** (verified on the rig) |
| Movement | Script-driven pivots, no Animator | 36 bones; 6 clips (`Idle`, `Walk` in place at 0.1875 m/s, `Walk_Giro`, `Wave`, `Gestos_Manos`, `Cabeza`). Generic rig. |
| Joints | head, 2 shoulders, 2 free hands | spine (Pelvis/Columna/Pecho), neck, head, clavicles, shoulder, elbow (≤ 65°), wrist, **2 phalanges per finger + thumb**, hip/knee/ankle |
| Face | painted in the texture (image_4) | geometry (`Cara_LOD0/1`, black), no texture |
| Materials | 5 Standard materials with 512 textures | 2 plain colours (ivory 0.86/0.84/0.80 roughness 0.42; black), **no textures or UVs on the body** |
| Shadows / layers | no shadows; 29 body, 0 hands | to be decided (see §6) |

**Equivalent joints (Unity coordinates of the new rig, in metres):**

| Script field | Current pivot | New bone | Position |
|---|---|---|---|
| `cuello` | Cuello (y 1.236) | **`Cabeza`** (head) or `Cuello` (base of the neck) | (0, 0.77, 0) / (0, 0.69, 0) |
| `hombroDer` | HombroDer (y 1.040) | **`Brazo.R`** (shoulder joint; `Hombro.R` is the clavicle) | (0.28, 0.602, 0) |
| `hombroIzq` | HombroIzq | **`Brazo.L`** | (−0.28, 0.602, 0) |
| `manoDer` | ManoDer (y 0.555) | **`Mano.R`** (wrist) | (0.425, 0.402, 0.02) |
| `manoIzq` | ManoIzq | **`Mano.L`** | (−0.425, 0.402, 0.02) |

**Proposed values for the new size (to confirm against the script's code):**
- `largoBrazo` ≈ **0.200**: the vertical drop `Brazo → Mano`, measured the same way as the current one.
- `largoMano` ≈ **0.17**: the length of the `Mano` bone. That reading of the field is an inference.
- `ojos` ≈ **(0, 0.94, 0.09)**: eye height 0.942. The depth uses the same proportion within the head as the current value.

## 5. What your colleague needs to receive, and what we need from them
**To send (the minimum):**
1. `BaymaxBebe_v4_2.fbx`: the only asset needed. It contains the mesh, LODs, face, skeleton, weights and clips.
2. An integration guide: import settings, joint mapping, values, and the decisions in §6.
3. Validation evidence: the metrics and a few previews, so they know what to expect.

**Deliberately NOT sent:**
- **Textures:** the new model doesn't use any. The face is geometry and the materials are plain colours.
- **`.mat` files:**
  - Unity creates them from the FBX ("Extract Materials"), and setting 2 values takes seconds.
  - A hand-written `.mat` without Unity to check it adds risk.
  - The existing `Baymax_image_*.mat` are not reused, because they carry textures and the UVs of the old avatar.
- **`.meta` files:** Unity generates them on import with new GUIDs. That avoids clashing with the project's existing GUIDs.
- **A prefab:**
  - A working prefab needs the controller script, which we don't have.
  - `AvatarBaymax.prefab` must not be replaced.
  - The new prefab should be created **in their project**, as a separate copy.
- **The `Baymax/` folder:** it already exists in their project.
- **Test actions** (`Poses_Prueba`, `Prueba_Brazos`, `Brazos_Continuo`): they stay in the `.blend`.

**To ask them for (needed to close the integration):**
1. The **controller script** (`.cs`, GUID `e15141ca…`) and any script it uses. This is the most important.
2. `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json` (Unity version, XR / Meta SDK, Animation Rigging).
3. `ProjectSettings/TagManager.asset`: the name of layer 29.
4. The scene(s) that use `AvatarBaymax`, or at least: which camera uses which culling mask, and how the controllers / tracked hands reach the script.
5. `ProjectSettings/GraphicsSettings.asset`: to confirm Built-in vs URP.

## 6. Decisions to make with your colleague (they change the procedure)
1. **How the arms move in VR.**
   - **(a)** Two-bone IK, Unity Animation Rigging or a script (recommended).
   - **(b)** Keep the stretched arm: not compatible with skinning without distortion.
2. **Hiding the head and body in first person.** The options:
   - **(a)** Scale the `Cabeza` bone to ~0 from the first-person camera. Common and simple, but needs a script change.
   - **(b)** Split the export into "body" and "hands" as two `SkinnedMeshRenderer`s sharing the skeleton. This lets the layer 29/0 scheme stay; it's an export change on our side, with no geometric change.
   - **(c)** Accept that only the face hides (it is a separate renderer).
3. **Animations or tracking.** In a tracked VR avatar the clips may be used partially:
   - legs (`Walk`) when moving;
   - fingers (`Gestos_Manos`) driven by the controller trigger;
   - `Idle` when no input arrives.

   That calls for an Animator Controller with layers and masks, plus IK on top.
4. **Fist (`Puno`):** use the finger bones (a closed pose), not a sphere.

## 7. Recommended procedure (in their project, step by step, nothing replaced)
1. Create a **new** folder `Assets/BaymaxBebe_v4_2/` and import the FBX there. Don't touch `Assets/.../Baymax/`.
2. **FBX import settings:**

   | Tab | Settings |
   |---|---|
   | Model | Scale Factor 1, Convert Units on, **Bake Axis Conversion on**, Import BlendShapes off, Read/Write only if a script reads the mesh |
   | Rig | **Generic**, Root node `Raiz`, Skin Weights 4 bones |
   | Animation | **Loop Time** on for `Idle`, `Walk`, `Walk_Giro`. Clip names may arrive as `Baymax_Rig\|Walk`; rename them in the clip list if needed. |
   | Materials | Extract Materials into `Assets/BaymaxBebe_v4_2/Materiales/` |

   **Standard shader values:**

   | Material | Albedo (RGB) | Smoothness | Metallic |
   |---|---|---|---|
   | `Baymax_Marfil_Satinado` | 0.86 / 0.84 / 0.80 | ≈ 0.58 | 0 |
   | `Baymax_Ojos_Negro` | black | 0 | 0 |

3. **Checks in an empty test scene** (before touching the avatar):
   - height 1.20 m; facing +Z; root rotation (0, 0, 0);
   - LOD Group created automatically (`_LOD0`/`_LOD1`);
   - every clip plays;
   - **in `Walk` the pelvis moves** (side sway and bob);
   - fingers bend in `Gestos_Manos`;
   - the face follows the head.
4. **New prefab**, a copy: `AvatarBaymaxBebe_v4_2.prefab` = root + FBX instance + the controller script.
   - Point its fields at the bones in §4, and set the proposed values (§4).
   - `cabeza` = `Cara_LOD0`/`Cara_LOD1` until decision 6.2 is made.
   - `Cast Shadows Off` to match the current one.
   - Layers according to decision 6.2.
5. **Adapt the controller** (their decision): IK for the arms, head hiding, and an optional Animator for legs and fingers.
6. **In VR:** check camera height, reach to the controllers, the elbow limit (≤ 65°; Unity has no such constraint, so the IK must cap it), and performance (LOD1 at a distance).
7. **Rollback:** delete `Assets/BaymaxBebe_v4_2/` and the new prefab. Nothing original was changed.

## 8. ZIP proposal (to be prepared once approved)
```
BaymaxBebe_v4_2_integracion.zip                       (3.6 MB without previews; ~19 MB with all three)
├─ LEEME_INTEGRACION.md        steps from §7, mapping and values from §4, decisions from §6, list of what to send back (§5)
├─ Assets/
│  └─ BaymaxBebe_v4_2/
│     └─ BaymaxBebe_v4_2.fbx    the only asset; no .meta (Unity generates it, avoiding GUID clashes)
└─ Documentacion/              outside Assets/ so Unity doesn't import it
   ├─ INFORME_AUDITORIA_BAYMAX.md   this report
   ├─ materiales.json               material values (no textures)
   ├─ export_check.json             export checks and the Blender round trip (deviation ≤ 0.014 mm)
   ├─ tabla_validacion.md           deformation tests per pose and animation
   └─ previews/                     hoja_brazos_frontal.png (5.3 MB), hoja_detalles.png (3.7 MB), comparacion_caminata_tres_cuartos.gif (6.5 MB; leave it out if size matters)
```
- **Not included:** `.blend` files, textures, `.mat` and `.meta` files, prefabs, scripts, and the old assets.
- **Optional, if your colleague uses Blender:** `baymax_bebe_v4_2_export.blend` (2.3 MB), to re-export with other settings.
