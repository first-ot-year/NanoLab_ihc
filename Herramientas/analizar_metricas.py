#!/usr/bin/env python3
"""Resumen HEART de las sesiones de NanoLab VR, por misión.

  MISIÓN 1 · ¿Qué hay en las muestras?   MISIÓN 2 · ¿Qué la detiene?   MISIÓN 3 · Crea la vacuna

Lee los sesion_*.json (una sesión = una misión jugada; también los antiguos "CASO 01", que eran la vacuna) y los
rendimiento_*.json que guarda el juego en el visor y, si se le pasa, el CSV del cuestionario Likert del piloto.
Escribe un informe en Markdown con una tabla por misión y el cumplimiento de cada meta del plan de evaluación
(README, sección "Evaluación (HEART)").

Cómo sacar los archivos del Quest (con el visor conectado por USB y la depuración activada):
    adb pull /sdcard/Android/data/com.utec.NanoLab/files/NanoLab ./datos_piloto

Uso:
    python3 Herramientas/analizar_metricas.py ./datos_piloto
    python3 Herramientas/analizar_metricas.py ./datos_piloto --likert Herramientas/plantilla_likert.csv --salida Docs/Resultados_Piloto.md

Solo usa la biblioteca estándar de Python 3.
"""
import argparse
import csv
import glob
import json
import os
import statistics
import sys

# Metas del plan de evaluación (README · Evaluación (HEART)). Tiempo máximo y meta R de cada misión.
MISIONES = {
    "MISIÓN 1": ("¿Qué hay en las muestras?", 8.0, "clasificar las 3 muestras a la primera"),
    "MISIÓN 2": ("¿Qué la detiene?", 8.0, "explicar el resultado (2 preguntas) a la primera"),
    "MISIÓN 3": ("Crea la vacuna", 15.0, "≥ 80 % en las preguntas del final"),
}
META_MINUTOS = 15.0
META_AYUDAS = 2
META_INACTIVO = 20.0        # % del tiempo
META_INDAGACION = 3
META_ORIENTACION_S = 120.0  # flujo básico tras ≤ 2 min de orientación
META_RETENCION = 0.8        # ≥ 80 % de aciertos al final
META_PORCENTAJE = 0.8       # 80 % de los participantes cumple
LIKERT_MIN = 4.0            # claridad, utilidad y motivación ≥ 4
MAREO_MAX = 2.0             # mareo ≤ 2


def cargar(carpeta, patron):
    datos = []
    for ruta in sorted(glob.glob(os.path.join(carpeta, "**", patron), recursive=True)):
        try:
            with open(ruta, encoding="utf-8-sig") as f:
                d = json.load(f)
            d["_archivo"] = os.path.basename(ruta)
            datos.append(d)
        except (OSError, json.JSONDecodeError) as e:
            print(f"Aviso: no se pudo leer {ruta}: {e}", file=sys.stderr)
    return datos


def cargar_likert(ruta):
    if not ruta:
        return {}
    filas = {}
    with open(ruta, encoding="utf-8-sig", newline="") as f:
        for fila in csv.DictReader(f):
            clave = (fila.get("sesion") or "").strip()
            if clave:
                filas[clave] = fila
    return filas


def num(x, defecto=None):
    try:
        return float(x)
    except (TypeError, ValueError):
        return defecto


def si_no(b):
    return "sí" if b else "no"


def pct(n, total):
    return f"{(100.0 * n / total):.0f} %" if total else "—"


def mision(s):
    caso = (s.get("caso") or "").strip()
    if caso.startswith("CASO 01"):
        return "MISIÓN 3"   # sesiones de antes de las misiones (la vacuna se llamaba CASO 01)
    return caso if caso in MISIONES else None


def evaluar(s, meta_min=META_MINUTOS):
    """Indicadores HEART de una sesión."""
    minutos = num(s.get("duracionSegundos"), 0.0) / 60.0
    ayudas = int(s.get("pistasPedidas", 0))
    completado = bool(s.get("completado"))
    comparacion = bool(s.get("comparacionAntesDespues"))
    total_q = int(s.get("totalComprension", 0) or 0)
    pre = int(s.get("preAciertos", -1))
    post = int(s.get("postAciertos", -1))
    hasta = num(s.get("segundosHastaEmpezar"), -1.0)
    inactivo = num(s.get("porcentajeInactivo"), None)
    indag = int(s.get("accionesIndagacion", 0) or 0)
    return {
        "minutos": minutos,
        "ayudas": ayudas,
        "errores": int(s.get("errores", 0) or 0),
        "completado": completado,
        "comparacion": comparacion,
        # En la misión 3 la comparación es la del microscopio del paso 7 (también va en comparacionAntesDespues).
        "T": completado and minutos <= meta_min and ayudas <= META_AYUDAS and comparacion,
        "inactivo": inactivo,
        "indagacion": indag,
        "E": inactivo is not None and inactivo < META_INACTIVO and indag >= META_INDAGACION,
        "hasta": hasta,
        "A": 0 <= hasta <= META_ORIENTACION_S and completado,
        "pre": pre,
        "post": post,
        "total_q": total_q,
        "R": total_q > 0 and post >= 0 and post / total_q >= META_RETENCION,
        "paso_a_paso": bool(s.get("pasoAPasoUsado")),
        "rend": s.get("rendimiento") or {},
    }


def informe(sesiones, rendimientos, likert):
    L = []
    L.append("# Resultados del piloto · NanoLab VR (3 misiones)\n")
    L.append(f"Sesiones analizadas: **{len(sesiones)}** · archivos de rendimiento: **{len(rendimientos)}** · "
             f"cuestionarios Likert: **{len(likert)}**\n")
    if not sesiones:
        L.append("No se encontraron archivos `sesion_*.json`.\n")
        return "\n".join(L)
    for m, (titulo, meta_min, meta_r) in MISIONES.items():
        delas = [s for s in sesiones if mision(s) == m]
        L.append(f"# {m} · {titulo}\n")
        if not delas:
            L.append("Sin sesiones de esta misión.\n")
            continue
        informe_mision(L, delas, likert, meta_min, meta_r)
    rendimiento(L, sesiones, rendimientos)
    return "\n".join(L)


def informe_mision(L, sesiones, likert, meta_min, meta_r):
    ev = [(s, evaluar(s, meta_min)) for s in sesiones]
    L.append("## Por sesión\n")
    L.append("| Sesión | Completó | Min | Ayudas | Errores | Comparó | Inactivo | Indagación | Hasta empezar | Antes | Después | Paso a paso | FPS medio | Lentos |")
    L.append("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    for s, e in ev:
        r = e["rend"]
        q = e["total_q"] or "?"
        L.append("| {a} | {c} | {m:.1f} | {ay} | {er} | {co} | {ina} | {ind} | {h} | {pre} | {post} | {pp} | {fps} | {len} |".format(
            a=s["_archivo"], c=si_no(e["completado"]), m=e["minutos"], ay=e["ayudas"], er=e["errores"], co=si_no(e["comparacion"]),
            ina=f"{e['inactivo']:.0f} %" if e["inactivo"] is not None else "—", ind=e["indagacion"],
            h=f"{e['hasta']:.0f} s" if e["hasta"] >= 0 else "—",
            pre=f"{e['pre']}/{q}" if e["pre"] >= 0 else "—", post=f"{e['post']}/{q}" if e["post"] >= 0 else "—",
            pp=si_no(e["paso_a_paso"]),
            fps=f"{num(r.get('fpsMedio'), 0):.0f}" if r else "—",
            len=f"{num(r.get('porcentajeLentos'), 0):.1f} %" if r else "—"))
    L.append("")

    n = len(ev)
    cuenta = {k: sum(1 for _, e in ev if e[k]) for k in ("T", "E", "A", "R")}
    L.append("## HEART: metas del plan\n")
    L.append("| Dim. | Meta | Resultado | ¿Cumple? |")
    L.append("|---|---|---|---|")

    # Happiness (Likert)
    filas_l = [likert[s["_archivo"]] for s, _ in ev if s["_archivo"] in likert] or list(likert.values())
    if filas_l:
        def media(col):
            v = [num(f.get(col)) for f in filas_l if num(f.get(col)) is not None]
            return statistics.mean(v) if v else None
        m = {c: media(c) for c in ("claridad", "utilidad", "motivacion", "comodidad", "mareo")}
        txt = ", ".join(f"{c} {v:.1f}" for c, v in m.items() if v is not None)
        ok = all(m[c] is not None and m[c] >= LIKERT_MIN for c in ("claridad", "utilidad", "motivacion")) and \
            (m["mareo"] is None or m["mareo"] <= MAREO_MAX)
        L.append(f"| **H** | Likert ≥ 4 en claridad, utilidad y motivación; mareo ≤ 2 | {txt} | {si_no(ok)} |")
    else:
        L.append("| **H** | Likert ≥ 4 en claridad, utilidad y motivación; mareo ≤ 2 | sin cuestionario (`--likert`) | — |")

    L.append(f"| **E** | Inactivo < 20 % y ≥ 3 acciones de indagación | {cuenta['E']}/{n} sesiones ({pct(cuenta['E'], n)}) | {si_no(cuenta['E'] / n >= META_PORCENTAJE)} |")
    L.append(f"| **A** | 80 % empieza la misión en ≤ 2 min y la completa | {cuenta['A']}/{n} ({pct(cuenta['A'], n)}) | {si_no(cuenta['A'] / n >= META_PORCENTAJE)} |")
    con_post = [e for _, e in ev if e["post"] >= 0]
    L.append(f"| **R** | {meta_r} | {cuenta['R']}/{len(con_post) or n} ({pct(cuenta['R'], len(con_post) or n)}) | "
             f"{si_no(con_post and cuenta['R'] / len(con_post) >= META_PORCENTAJE)} |")
    L.append(f"| **T** | 80 % completa en ≤ {meta_min:.0f} min, con ≤ 2 ayudas y la comparación del microscopio | {cuenta['T']}/{n} ({pct(cuenta['T'], n)}) | {si_no(cuenta['T'] / n >= META_PORCENTAJE)} |")
    L.append("")

    pares = [(e["pre"], e["post"], e["total_q"]) for _, e in ev if e["pre"] >= 0 and e["post"] >= 0 and e["total_q"]]
    if pares:
        antes = statistics.mean(p[0] / p[2] for p in pares) * 100
        despues = statistics.mean(p[1] / p[2] for p in pares) * 100
        L.append(f"**Comprensión (mismas preguntas antes y después, {len(pares)} sesiones):** {antes:.0f} % → {despues:.0f} % de aciertos.\n")

    tiempos = [e["minutos"] for _, e in ev if e["completado"]]
    if tiempos:
        L.append(f"**Tiempo de los que completaron:** mediana {statistics.median(tiempos):.1f} min (mín. {min(tiempos):.1f}, máx. {max(tiempos):.1f}).\n")

    # Errores por paso (dónde se atascan)
    por_paso = {}
    for s, _ in ev:
        for c in s.get("erroresPorEtapa", []) or []:
            por_paso[c.get("etapa")] = por_paso.get(c.get("etapa"), 0) + int(c.get("veces", 0))
    if por_paso:
        L.append("**Errores por paso (todas las sesiones):** " +
                 ", ".join(f"{k} {v}" for k, v in sorted(por_paso.items(), key=lambda x: -x[1])) + "\n")



def rendimiento(L, sesiones, rendimientos):
    L.append("# Rendimiento (RNF-08 · 72 FPS, RNF-02 · < 100 ms)\n")
    todos = [("sesión " + s["_archivo"], s.get("rendimiento")) for s in sesiones if s.get("rendimiento")] + \
            [("recorrido " + r["_archivo"], r) for r in rendimientos]
    todos = [(n_, r) for n_, r in todos if r and int(r.get("fotogramas", 0) or 0) > 0]
    if not todos:
        L.append("Sin datos de rendimiento.\n")
    else:
        L.append("| Tramo | Dispositivo | Hz | FPS medio | FPS 1 % peor | ms p99 | ms peor | Lentos | 72 FPS | < 100 ms |")
        L.append("|---|---|---|---|---|---|---|---|---|---|")
        for nombre, r in todos:
            L.append("| {n} | {d}{ed} | {hz:.0f} | {fps:.1f} | {low:.1f} | {p99:.1f} | {peor:.1f} | {len:.1f} % | {c72} | {c100} |".format(
                n=nombre, d=r.get("dispositivo", "?"), ed=" (Editor)" if r.get("enEditor") else "",
                hz=num(r.get("objetivoHz"), 0), fps=num(r.get("fpsMedio"), 0), low=num(r.get("fps1PorCientoPeor"), 0),
                p99=num(r.get("msP99"), 0), peor=num(r.get("msPeor"), 0), len=num(r.get("porcentajeLentos"), 0),
                c72=si_no(r.get("cumple72")), c100=si_no(r.get("cumple100ms"))))
        en_visor = [r for _, r in todos if not r.get("enEditor")]
        if not en_visor:
            L.append("\n> Todos los datos son del Editor: para RNF-08 hay que medir en el Meta Quest.")
        L.append("")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("carpeta", help="carpeta con los sesion_*.json (se busca también en subcarpetas)")
    ap.add_argument("--likert", help="CSV del cuestionario (ver Herramientas/plantilla_likert.csv)")
    ap.add_argument("--salida", help="archivo .md donde guardar el informe (si no, se imprime)")
    a = ap.parse_args()
    sesiones = [s for s in cargar(a.carpeta, "sesion_*.json") if mision(s)]
    rend = cargar(a.carpeta, "rendimiento_*.json")
    texto = informe(sesiones, rend, cargar_likert(a.likert))
    if a.salida:
        with open(a.salida, "w", encoding="utf-8") as f:
            f.write(texto + "\n")
        print(f"Informe guardado en {a.salida}")
    else:
        print(texto)


if __name__ == "__main__":
    main()
