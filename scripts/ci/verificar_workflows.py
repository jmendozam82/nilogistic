#!/usr/bin/env python3
"""Valida la sintaxis y la estructura minima de los workflows de GitHub Actions (D-144).

Reglas:
  1. YAML valido (yaml.YAMLError), con linea y columna si el error las trae.
  2. Sin claves duplicadas en ningun mapa (PyYAML no las detecta por defecto).
  3. La raiz es un mapa con `name` (texto), `on` y `jobs`. PyYAML convierte la
     clave sin comillas `on` en el booleano True; se aceptan ambas formas.
  4. Cada job es un mapa con `runs-on` o `uses`; si trae `steps`, es una lista
     de mapas con `run` o `uses` (no ambos).
  5. Cada `needs` apunta a un job que existe.
  6. Un archivo vacio o solo con comentarios es un error.

Uso:
    python verificar_workflows.py [--directorio .github/workflows]

Codigos de salida: 0 si todo esta bien, 1 si hay errores, 2 si falta PyYAML
(mensaje: «Falta PyYAML; instala con: pip install pyyaml») o el directorio
no existe.
"""

import argparse
import os
import sys

try:
    import yaml
except ImportError:
    yaml = None


def _crear_cargador():
    """Devuelve un SafeLoader que falla al encontrar claves duplicadas."""

    class CargadorSinDuplicados(yaml.SafeLoader):
        pass

    def construir_mapa(cargador, nodo, profundidad=False):
        mapa = {}
        for clave_nodo, valor_nodo in nodo.value:
            clave = cargador.construct_object(clave_nodo, deep=profundidad)
            if clave in mapa:
                raise yaml.constructor.ConstructorError(
                    "al construir un mapa", nodo.start_mark,
                    f"clave duplicada: {clave!r}", clave_nodo.start_mark)
            mapa[clave] = cargador.construct_object(valor_nodo, deep=profundidad)
        return mapa

    CargadorSinDuplicados.add_constructor(
        yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, construir_mapa)
    return CargadorSinDuplicados


def _describir_error_yaml(error):
    """Convierte un YAMLError en un mensaje en español con linea y columna."""
    problema = getattr(error, "problem", None) or str(error).splitlines()[0]
    marca = getattr(error, "problem_mark", None)
    if marca is not None:
        return f"{problema} (linea {marca.line + 1}, columna {marca.column + 1})"
    return problema


def analizar_workflow(texto, nombre):
    """Devuelve la lista de errores del workflow; vacia si todo esta bien."""
    try:
        documento = yaml.load(texto, Loader=_crear_cargador())
    except yaml.YAMLError as error:
        return [f"YAML invalido: {_describir_error_yaml(error)}"]

    if documento is None:
        return ["el archivo esta vacio o solo contiene comentarios"]
    if not isinstance(documento, dict):
        return ["la raiz debe ser un mapa"]

    errores = []

    name = documento.get("name")
    if not isinstance(name, str) or not name.strip():
        errores.append("falta 'name' (texto) en la raiz")

    if "on" not in documento and True not in documento:
        errores.append("falta 'on' en la raiz")

    trabajos = documento.get("jobs")
    if not isinstance(trabajos, dict):
        errores.append("falta 'jobs' (mapa) en la raiz")
        return errores

    for id_trabajo, definicion in trabajos.items():
        if not isinstance(definicion, dict):
            errores.append(f"el job '{id_trabajo}' debe ser un mapa")
            continue

        if "runs-on" not in definicion and "uses" not in definicion:
            errores.append(f"el job '{id_trabajo}' debe tener 'runs-on' o 'uses'")

        pasos = definicion.get("steps")
        if pasos is not None:
            if not isinstance(pasos, list):
                errores.append(f"'steps' del job '{id_trabajo}' debe ser una lista")
            else:
                for indice, paso in enumerate(pasos, start=1):
                    if not isinstance(paso, dict):
                        errores.append(
                            f"el paso {indice} del job '{id_trabajo}' debe ser un mapa")
                        continue
                    tiene_run = "run" in paso
                    tiene_uses = "uses" in paso
                    if tiene_run and tiene_uses:
                        errores.append(
                            f"el paso {indice} del job '{id_trabajo}' no puede tener "
                            "'run' y 'uses' a la vez")
                    elif not tiene_run and not tiene_uses:
                        errores.append(
                            f"el paso {indice} del job '{id_trabajo}' debe tener "
                            "'run' o 'uses'")

        necesidades = definicion.get("needs")
        if necesidades is not None:
            if isinstance(necesidades, str):
                lista = [necesidades]
            elif isinstance(necesidades, list):
                lista = necesidades
            else:
                lista = None
                errores.append(
                    f"'needs' del job '{id_trabajo}' debe ser texto o lista")
            if lista is not None:
                for destino in lista:
                    if destino not in trabajos:
                        errores.append(
                            f"el job '{id_trabajo}' necesita el job inexistente "
                            f"'{destino}'")

    return errores


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--directorio", default=".github/workflows")
    args = parser.parse_args(argv)

    if yaml is None:
        print("Falta PyYAML; instala con: pip install pyyaml", file=sys.stderr)
        return 2

    directorio = args.directorio
    if not os.path.isdir(directorio):
        print(f"ERROR: el directorio no existe: {directorio}", file=sys.stderr)
        return 2

    archivos = sorted(
        os.path.join(directorio, nombre)
        for nombre in os.listdir(directorio)
        if nombre.endswith((".yml", ".yaml"))
        and os.path.isfile(os.path.join(directorio, nombre))
    )

    fallos = 0
    for ruta in archivos:
        nombre = os.path.basename(ruta)
        try:
            with open(ruta, encoding="utf-8") as archivo:
                texto = archivo.read()
        except (OSError, UnicodeDecodeError) as error:
            print(f"{nombre}: no se pudo leer el archivo: {error}")
            fallos += 1
            continue
        for error in analizar_workflow(texto, nombre):
            print(f"{nombre}: {error}")
            fallos += 1

    if fallos:
        return 1

    print(f"OK: {len(archivos)} workflow(s) revisado(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
