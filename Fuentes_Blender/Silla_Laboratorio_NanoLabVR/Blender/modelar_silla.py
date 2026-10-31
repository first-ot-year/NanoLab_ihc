# Silla de laboratorio (taburete) para NanoLab. Blender: Z arriba, frente hacia -Y (en Unity: +Z, la mesa).
import bpy, bmesh, sys, math, os
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index("--")+1:]
blend_out, fbx_out = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = 'METRIC'

def mat(nombre, color, metal, rugos):
    m = bpy.data.materials.new(nombre); m.diffuse_color = (*color, 1); m.metallic = metal; m.roughness = rugos
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if b: b.inputs["Base Color"].default_value = (*color, 1); b.inputs["Metallic"].default_value = metal; b.inputs["Roughness"].default_value = rugos
    return m
# Mismos nombres y valores que los materiales del proyecto (Unity los reasigna por nombre).
TELA = mat("SillaAsiento", (0.18, 0.32, 0.45), 0.0, 0.65)
METAL = mat("SillaMetal", (0.62, 0.65, 0.70), 0.85, 0.40)

def lathe(bm, perfil, segs, M=Matrix(), cerrado=False):
    """Revoluciona un perfil (r, z) alrededor de Z. Los puntos con r=0 se colapsan en un vértice del eje."""
    anillos = []
    for r, z in perfil:
        if r == 0: anillos.append([bm.verts.new(M @ Vector((0, 0, z)))])
        else: anillos.append([bm.verts.new(M @ Vector((r*math.cos(2*math.pi*i/segs), r*math.sin(2*math.pi*i/segs), z))) for i in range(segs)])
    pares = list(zip(anillos, anillos[1:])) + ([(anillos[-1], anillos[0])] if cerrado else [])
    for a, b in pares:
        for i in range(segs):
            j = (i+1) % segs
            if len(a) == 1: bm.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1: bm.faces.new((a[i], b[0], a[j]))
            else: bm.faces.new((a[i], a[j], b[j], b[i]))

def caja(bm, esquinas):
    """8 vértices: 0-3 base (antihorario visto desde arriba), 4-7 tapa encima de cada uno."""
    v = [bm.verts.new(p) for p in esquinas]
    for f in ((0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5), (2,3,7,6), (3,0,4,7)): bm.faces.new([v[i] for i in f])

def caja_eje(bm, p0, p1, ancho, alto):
    """Barra recta de p0 a p1 con sección ancho × alto."""
    d = (p1 - p0).normalized(); up = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
    lado = d.cross(up).normalized() * ancho/2; arriba = lado.cross(d).normalized() * alto/2
    base = [p0-lado-arriba, p1-lado-arriba, p1+lado-arriba, p0+lado-arriba]
    caja(bm, base + [p+2*arriba for p in base])

def objeto(nombre, bm, material, sharp_deg=40):
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for f in bm.faces: f.smooth = True
    for e in bm.edges:
        if not e.is_manifold or e.calc_face_angle(0) > math.radians(sharp_deg): e.smooth = False
    me = bpy.data.meshes.new(nombre); bm.to_mesh(me); bm.free()
    me.materials.append(material)
    ob = bpy.data.objects.new(nombre, me); bpy.context.scene.collection.objects.link(ob)
    return ob

# --- Asiento: redondo como el original, borde redondeado y ligera concavidad (ergonómico). Tapa a 0.65 m.
bm = bmesh.new()
lathe(bm, [(0, .595), (.185, .595), (.198, .605), (.2, .625), (.196, .643), (.185, .65), (.15, .652), (.08, .643), (0, .64)], 40)
asiento = objeto("Asiento", bm, TELA)

# --- Respaldo lumbar bajo: banda curva detrás (+Y), centrada a 0.86 m como el respaldo del taburete actual.
bm = bmesh.new(); a = math.radians(38); n = 12; R0, R1, z0, z1 = .225, .255, .79, .93
filas = []
for i in range(n+1):
    t = -a + 2*a*i/n; c, s = math.sin(t), math.cos(t)   # ángulo desde +Y
    filas.append([bm.verts.new((r*c, r*s, z)) for r, z in ((R0, z0), (R1, z0), (R1, z1), (R0, z1))])
for f0, f1 in zip(filas, filas[1:]):
    for k in range(4): bm.faces.new((f0[k], f1[k], f1[(k+1) % 4], f0[(k+1) % 4]))
bm.faces.new(filas[0][::-1]); bm.faces.new(filas[-1])
respaldo = objeto("Respaldo", bm, TELA, sharp_deg=60)

# --- Estructura metálica: columna de gas, base de 5 radios, aro reposapiés y soporte del respaldo.
bm = bmesh.new()
lathe(bm, [(0, .035), (.05, .035), (.05, .08), (.032, .092), (.032, .39), (.03, .4), (.022, .4), (.022, .58), (.09, .58), (.09, .595), (0, .595)], 24)
for k in range(5):   # un radio hacia atrás (+Y): delante quedan dos, abiertos hacia la mesa
    ang = math.pi/2 + 2*math.pi*k/5; d = Vector((math.cos(ang), math.sin(ang), 0)); l = d.cross(Vector((0, 0, 1)))
    p0, p1 = d*.04, d*.255
    caja(bm, [p0-l*.022+Vector((0,0,.035)), p1-l*.015+Vector((0,0,.03)), p1+l*.015+Vector((0,0,.03)), p0+l*.022+Vector((0,0,.035)),
              p0-l*.022+Vector((0,0,.075)), p1-l*.015+Vector((0,0,.05)), p1+l*.015+Vector((0,0,.05)), p0+l*.022+Vector((0,0,.075))])
lathe(bm, [(.011*math.cos(2*math.pi*i/8)+.21, .011*math.sin(2*math.pi*i/8)+.27) for i in range(8)], 40, cerrado=True)   # aro
for ang in (math.pi/2, math.pi/2 + 2*math.pi/3, math.pi/2 - 2*math.pi/3):
    d = Vector((math.cos(ang), math.sin(ang), 0)); caja_eje(bm, d*.03 + Vector((0,0,.27)), d*.205 + Vector((0,0,.27)), .016, .016)
caja_eje(bm, Vector((0, .02, .575)), Vector((0, .215, .575)), .04, .02)    # brazo bajo el asiento
caja_eje(bm, Vector((0, .215, .565)), Vector((0, .215, .835)), .04, .02)   # montante del respaldo
estructura = objeto("Estructura", bm, METAL)

# --- Tacos niveladores (fijos, sin ruedas: la silla no rueda en la simulación).
bm = bmesh.new()
for k in range(5):
    ang = math.pi/2 + 2*math.pi*k/5
    lathe(bm, [(0, 0), (.02, 0), (.022, .01), (.018, .03), (0, .032)], 12, Matrix.Translation((math.cos(ang)*.245, math.sin(ang)*.245, 0)))
tacos = objeto("Tacos", bm, TELA)

raiz = bpy.data.objects.new("Silla_Laboratorio", None); bpy.context.scene.collection.objects.link(raiz)
for o in (asiento, respaldo, estructura, tacos): o.parent = raiz

os.makedirs(os.path.dirname(blend_out), exist_ok=True); os.makedirs(os.path.dirname(fbx_out), exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=blend_out, compress=True)
bpy.ops.export_scene.fbx(filepath=fbx_out, object_types={'EMPTY', 'MESH'}, apply_scale_options='FBX_SCALE_ALL',
                         axis_forward='-Z', axis_up='Y', bake_space_transform=True, mesh_smooth_type='OFF',
                         use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='STRIP')
print("OK", blend_out, fbx_out)
