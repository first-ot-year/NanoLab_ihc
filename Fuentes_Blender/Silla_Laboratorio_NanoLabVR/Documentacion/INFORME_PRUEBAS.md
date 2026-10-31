# Informe de pruebas — Silla de laboratorio interactuable

Entorno disponible: Windows 11, Blender 5.2, .NET SDK 10 (compilador Roslyn). **No hay Unity ni visor Meta Quest en este equipo.**

Leyenda: ✅ realizada y superada · ⏳ preparada pero no ejecutada (lo debe ejecutar el equipo) · ❌ fallida

## 1. Blender (realizadas)

| Prueba | Resultado |
|---|---|
| Importar `chair.fbx` original | ✅ 535 polígonos, 9 mallas, material `blinn2`, sin respaldo, base de 4 radios con ruedas. |
| Geometría nueva: bordes no-manifold, caras degeneradas, vértices duplicados | ✅ 0 / 0 / 0 en las 4 mallas (`validar.py`). |
| Normales hacia fuera (rayo desde cada cara, por pieza) | ✅ 0 caras invertidas. Una primera pasada sobre la malla entera marcó 38 caras en `Estructura`: falso positivo por piezas que se interpenetran (radios dentro de la columna). Por islas sale 0. |
| Número de polígonos | ✅ 2212 triángulos (asiento 560, estructura 1192, respaldo 100, tacos 360). |
| Escala del FBX exportado (reimportado) | ✅ 0,51 × 0,93 × 0,49 m, tapa del asiento a 0,652 m. |
| Orientación | ✅ Respaldo en +Y en Blender (detrás). Con `axis_forward=-Z`, `axis_up=Y` y `bake_space_transform`, el frente queda en +Z en Unity. ⏳ El script de Unity lo comprueba y lo corrige al crear el prefab. |
| Materiales | ✅ El FBX lleva `SillaAsiento` y `SillaMetal`, los mismos nombres que en el proyecto. |
| Revisión visual | ✅ Renders en `Documentacion/img/`. |

## 2. Código (realizadas sin Unity)

| Prueba | Resultado |
|---|---|
| Compilación de sintaxis C# 9 (la de Unity 2022.3) de `Silla.cs`, `PunteroMuestras.cs`, `DebugLocomocion.cs` y `MontarSillaLaboratorio.cs` con Roslyn | ✅ Sin errores de sintaxis ni de lenguaje. Los únicos errores son CS0246 (tipos de Unity y Meta no referenciados fuera de Unity). **No equivale a compilar en Unity.** |
| Auditoría estática de la escena (YAML) | ✅ 10 instancias de `Silla.prefab` intactas en `Microbiologia.unity`. `Silla_Puesto_01` está en `LAB_Microbiologia/Microbiologia/Sillas`. |

## 3. Unity Editor (⏳ no ejecutadas: no hay Unity)

Prueba automática: menú *NanoLab → Probar locomoción (play mode)*, después de *Silla de laboratorio → 2. Probar en el puesto 1*. Las líneas relacionadas con la silla, en orden:

| # | Comprobación (línea del informe) | Requisito |
|---|---|---|
| 1 | `Hay N sillas en las que sentarse` | La silla tiene SitPoint y ExitPoint. |
| 2 | `El rayo encuentra 'Silla_Laboratorio_Puesto_01' y ofrece "sentarte"` | El jugador detecta la silla (mismo código del rayo que en el visor). |
| 3 | `Sentado en '…': cuerpo a ≤0,2 m del asiento, ojos a 1,37 m, locomoción pausada` | La interacción sienta al jugador a la altura correcta. |
| 4 | `Sentado mira hacia donde mira la silla, al microscopio (desvío <5°)` | Orientación hacia el microscopio. |
| 5 | `Sentado: volver a sentarse no cambia nada, las otras sillas no se pueden usar…` | No se sienta dos veces ni en dos sillas. |
| 6 | `Al levantarse queda detrás de la silla, altura normal y locomoción activa` | Levantarse y recuperar la locomoción. |

Comprobaciones manuales en el Editor:
- [ ] Compila sin errores tras copiar los archivos.
- [ ] *1. Crear prefab*: la consola no muestra errores `[SillaLab]`, el alto es ≈0,93 m y los renderers usan `SillaAsiento`/`SillaMetal` de `Materiales/Micro`.
- [ ] En la escena, la silla nueva está en el sitio de `Silla_Puesto_01`, mirando al microscopio y sin atravesar la mesa con la base.
- [ ] El trigger cubre la silla (gizmo verde) y no hay otros colliders.
- [ ] *3. Deshacer prueba* deja la escena como estaba.

## 4. Visor VR (⏳ no ejecutadas: no hay visor)

**No se afirma que funcione en VR.** Lista para el equipo (Meta Quest, APK o Link):

- [ ] Con la mano vacía, al apuntar a la silla aparece el rayo y el aviso "Gatillo o A/X: sentarte".
- [ ] Gatillo o A/X: el jugador queda sentado frente al microscopio, con una vista natural y el ocular a la altura de los ojos. Probarlo de pie y sentado en una silla real.
- [ ] Sentado: no camina ni gira con los joysticks. Mirar por el microscopio funciona y el joystick no lo levanta mientras se mira.
- [ ] Sentado: apuntar a otra silla no hace nada. Apuntar a la propia ofrece "levantarte".
- [ ] Mover el joystick izquierdo lo levanta detrás de la silla y vuelve a caminar y girar con normalidad.
- [ ] Sin mareo al sentarse ni al levantarse: el cambio es instantáneo, con la viñeta de confort del Locomotor.
- [ ] El resto del laboratorio sigue igual: agarre de portaobjetos, fichas, panel ☰ y sala BSL-3.

## 5. Movimiento de la silla (fase 4)

No implementado (motivos en `GUIA_INTEGRACION.md` §6). No hay pruebas de agarre, colisiones ni física.
