#!/usr/bin/env python3
r"""HU-005 / RNF-MAN-03: valida mensajes de commit con Conventional Commits.

Uso:
    python validar_conventional_commits.py [--base origin/main]

Falla (exit 1) si algún commit del rango <base>..HEAD no cumple:
    ^(feat|fix|docs|chore|refactor|test|build|ci|perf|style|revert|security)
    (\([a-z0-9][a-z0-9._-]*\))?(!)?: <descripcion>
con tipo en minúsculas, sin acentos y descripción ≤ 72 caracteres.
"""

import re
import subprocess
import sys

PATRON = re.compile(
    r"^(?P<tipo>feat|fix|docs|chore|refactor|test|build|ci|perf|style|revert|security)"
    r"(\((?P<contexto>[a-z0-9][a-z0-9._-]*)\))?"
    r"(?P<ruptura>!)?: (?P<descripcion>.+)$"
)

TIPOS_VALIDOS = {
    "feat", "fix", "docs", "chore", "refactor",
    "test", "build", "ci", "perf", "style", "revert", "security",
}


def commits_del_rango(base: str) -> list[str]:
    salida = subprocess.run(
        ["git", "log", f"{base}..HEAD", "--format=%s"],
        capture_output=True, text=True, check=False,
    )
    if salida.returncode != 0:
        print(f"No se pudo leer el rango {base}..HEAD: {salida.stderr.strip()}")
        sys.exit(2)
    return [l.strip() for l in salida.stdout.splitlines() if l.strip()]


def validar(mensajes: list[str]) -> bool:
    errores = []
    for msg in mensajes:
        m = PATRON.match(msg)
        if not m:
            errores.append(f"formato no convencional        : {msg}")
            continue
        if m.group("tipo") not in TIPOS_VALIDOS:
            errores.append(f"tipo fuera de la lista         : {msg}")
        if len(m.group("descripcion")) > 72:
            errores.append(f"descripcion mayor a 72 chars   : {msg}")
        if not msg.isascii():
            errores.append(f"contiene caracteres no ascii   : {msg}")
    for e in errores:
        print(f"KO {e}")
    return not errores


def main() -> int:
    base = "origin/main"
    if "--base" in sys.argv:
        base = sys.argv[sys.argv.index("--base") + 1]
    mensajes = commits_del_rango(base)
    if not mensajes:
        print("OK sin commits propios (rango vacio)")
        return 0
    print(f"Evaluando {len(mensajes)} commit(s) del rango {base}..HEAD")
    return 0 if validar(mensajes) else 1


if __name__ == "__main__":
    sys.exit(main())