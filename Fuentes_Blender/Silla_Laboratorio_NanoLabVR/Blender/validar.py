import bpy, bmesh, sys
from mathutils import Vector
from mathutils.bvhtree import BVHTree
argv = sys.argv[sys.argv.index("--")+1:]
bpy.ops.wm.open_mainfile(filepath=argv[0])
ok = True; tris = 0
for o in bpy.data.objects:
    if o.type != 'MESH': continue
    bm = bmesh.new(); bm.from_mesh(o.data); bm.faces.index_update()
    nm = sum(1 for e in bm.edges if not e.is_manifold)
    deg = sum(1 for f in bm.faces if f.calc_area() < 1e-9)
    n0 = len(bm.verts); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5); dup = n0 - len(bm.verts)
    # Normales hacia fuera: un rayo desde cada cara siguiendo su normal no debe volver a entrar en un número impar de caras de la misma pieza.
    # Por islas: las piezas se tocan (radios dentro de la columna) y cada una se comprueba sola.
    inv = 0; vistas = set()
    for f0 in bm.faces:
        if f0.index in vistas: continue
        isla, pila = [], [f0]; vistas.add(f0.index)
        while pila:
            f = pila.pop(); isla.append(f)
            for e in f.edges:
                for g in e.link_faces:
                    if g.index not in vistas: vistas.add(g.index); pila.append(g)
        vs = sorted({v for f in isla for v in f.verts}, key=lambda v: v.index); idx = {v: i for i, v in enumerate(vs)}
        bvh = BVHTree.FromPolygons([v.co for v in vs], [[idx[v] for v in f.verts] for f in isla])
        for f in isla:
            d = f.normal; p = f.calc_center_median() + d*1e-4; n = 0
            while True:
                hit = bvh.ray_cast(p, d)
                if hit[0] is None: break
                n += 1; p = hit[0] + d*1e-4
            if n % 2: inv += 1
    t = sum(len(f.verts)-2 for f in bm.faces); tris += t
    print(f"{o.name:11s} tris={t:5d} no_manifold={nm} degeneradas={deg} duplicados={dup} normales_hacia_dentro={inv} mat={[m.name for m in o.data.materials]}")
    ok &= nm == 0 and deg == 0 and dup == 0 and inv == 0
    bm.free()
print("TRIS TOTAL", tris, "| GEOMETRIA", "OK" if ok else "FALLA")
# FBX exportado: dimensiones y lado del respaldo.
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=argv[1])
pts = [o.matrix_world @ Vector(v) for o in bpy.data.objects if o.type == 'MESH' for v in o.bound_box]
mn = Vector(map(min, *pts)); mx = Vector(map(max, *pts))
print("FBX objetos", sorted(o.name for o in bpy.data.objects), "mats", sorted(m.name for m in bpy.data.materials))
print("FBX bbox", tuple(round(x, 3) for x in mn), tuple(round(x, 3) for x in mx))
r = bpy.data.objects["Respaldo"]; rc = sum((r.matrix_world @ Vector(v) for v in r.bound_box), Vector()) / 8
a = bpy.data.objects["Asiento"]; top = max((a.matrix_world @ Vector(v)).z for v in a.bound_box)
print("respaldo centro", tuple(round(x, 3) for x in rc), "(debe ser +Y: detrás) | tapa asiento z", round(top, 3))
