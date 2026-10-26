#!/usr/bin/env python3
"""Pruebas del despliegue condicionado con etiqueta (DT-6).

Cubre los 7 casos requeridos por la ficha DT-6 (§4 y §7):
1. Reproduce el incidente del 22/10/2026: etiqueta en A; B (codigo) no desplegado;
   C (solo docs). Con --antes B --despues C --etiqueta: desplegar=true.
   Sin --etiqueta: false (el defecto actual).
2. Etiqueta en el mismo commit y solo cambios de documentacion: false.
3. Etiqueta ausente: usa --antes (true o false segun el diff).
4. Etiqueta que no es ancestro: true.
5. Error de git: true.
6. --antes nulo (40 ceros) con etiqueta valida: compara contra la etiqueta.
7. Se escribe desplegar= en GITHUB_OUTPUT en todos los casos.
"""

import contextlib
import io
import os
import subprocess
import tempfile
import unittest

import scripts.ci.cambios_despliegue as cdesp


def escribir(ruta, contenido):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    with open(ruta, "w", encoding="utf-8", newline="\n") as f:
        f.write(contenido)


class DespliegueEtiquetaTests(unittest.TestCase):
    def _inicializar_repo_git(self, d):
        subprocess.run(["git", "init", "-q"], cwd=d, check=True)
        subprocess.run(["git", "config", "user.email", "ci@nilogistic.test"], cwd=d, check=True)
        subprocess.run(["git", "config", "user.name", "CI"], cwd=d, check=True)

    def _commit(self, d, ruta, contenido, mensaje):
        escribir(os.path.join(d, ruta), contenido)
        subprocess.run(["git", "add", "."], cwd=d, check=True)
        subprocess.run(["git", "commit", "-q", "-m", mensaje], cwd=d, check=True)
        return subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=d,
            capture_output=True, text=True, check=True,
        ).stdout.strip()

    def _crear_etiqueta(self, d, nombre, ref):
        subprocess.run(["git", "tag", nombre, ref], cwd=d, check=True)

    def test_caso_1_reproduce_incidente_22_10_2026(self):
        """Caso 1: etiqueta en A; B (codigo); C (solo docs).

        Con --etiqueta staging-desplegado: desplegar=true.
        Sin --etiqueta: desplegar=false (comportamiento anterior).
        """
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            commit_a = self._commit(d, "docs/readme.md", "v1", "docs: inicio")
            self._crear_etiqueta(d, "staging-desplegado", commit_a)

            # B introduce cambio en src/ (codigo)
            commit_b = self._commit(d, "src/Modulo.cs", "public class Modulo {}", "feat: nuevo codigo")

            # C solo toca documentacion
            commit_c = self._commit(d, "docs/guia.md", "# Guia", "docs: agregar guia")

            # Con --etiqueta staging-desplegado: compara contra A -> ve el codigo de B -> true
            salida_con = io.StringIO()
            with contextlib.redirect_stdout(salida_con):
                cod_con = cdesp.main([
                    "--antes", commit_b,
                    "--despues", commit_c,
                    "--etiqueta", "staging-desplegado",
                    "--raiz", d,
                ])
            self.assertEqual(cod_con, 0)
            self.assertIn("desplegar=true", salida_con.getvalue())
            self.assertIn("origen=etiqueta", salida_con.getvalue())

            # Sin --etiqueta: compara B..C -> solo docs -> false (reproduce fallo del 22/10)
            salida_sin = io.StringIO()
            with contextlib.redirect_stdout(salida_sin):
                cod_sin = cdesp.main([
                    "--antes", commit_b,
                    "--despues", commit_c,
                    "--raiz", d,
                ])
            self.assertEqual(cod_sin, 0)
            self.assertIn("desplegar=false", salida_sin.getvalue())
            self.assertIn("origen=antes", salida_sin.getvalue())

    def test_caso_2_etiqueta_en_mismo_commit_solo_docs_devuelve_false(self):
        """Caso 2: Etiqueta en el mismo commit (o ancestro) y solo cambios de documentacion: false."""
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            commit_a = self._commit(d, "docs/readme.md", "v1", "docs: inicio")
            commit_b = self._commit(d, "docs/readme.md", "v2", "docs: cambio docs")
            self._crear_etiqueta(d, "staging-desplegado", commit_b)

            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                cod = cdesp.main([
                    "--antes", commit_a,
                    "--despues", commit_b,
                    "--etiqueta", "staging-desplegado",
                    "--raiz", d,
                ])
            self.assertEqual(cod, 0)
            self.assertIn("desplegar=false", salida.getvalue())
            self.assertIn("origen=etiqueta", salida.getvalue())

    def test_caso_3_etiqueta_ausente_usa_antes(self):
        """Caso 3: Etiqueta ausente: usa --antes (true o false segun el diff)."""
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            commit_a = self._commit(d, "docs/readme.md", "v1", "docs: inicio")
            commit_b_docs = self._commit(d, "docs/readme.md", "v2", "docs: actualizacion")
            commit_c_cod = self._commit(d, "src/App.cs", "class App {}", "feat: app")

            # Subcaso A: diff entre commit_a y commit_b_docs (solo docs) -> false
            salida_docs = io.StringIO()
            with contextlib.redirect_stdout(salida_docs):
                cod_docs = cdesp.main([
                    "--antes", commit_a,
                    "--despues", commit_b_docs,
                    "--etiqueta", "etiqueta_inexistente",
                    "--raiz", d,
                ])
            self.assertEqual(cod_docs, 0)
            self.assertIn("desplegar=false", salida_docs.getvalue())
            self.assertIn("origen=antes", salida_docs.getvalue())

            # Subcaso B: diff entre commit_b_docs y commit_c_cod (codigo) -> true
            salida_cod = io.StringIO()
            with contextlib.redirect_stdout(salida_cod):
                cod_cod = cdesp.main([
                    "--antes", commit_b_docs,
                    "--despues", commit_c_cod,
                    "--etiqueta", "etiqueta_inexistente",
                    "--raiz", d,
                ])
            self.assertEqual(cod_cod, 0)
            self.assertIn("desplegar=true", salida_cod.getvalue())
            self.assertIn("origen=antes", salida_cod.getvalue())

    def test_caso_4_etiqueta_que_no_es_ancestro_devuelve_true(self):
        """Caso 4: Etiqueta existe pero no es ancestro (ej. historial reescrito o rama divergente): true."""
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            commit_raiz = self._commit(d, "docs/readme.md", "v1", "docs: inicio")

            # Rama divergente para la etiqueta
            subprocess.run(["git", "checkout", "-q", "-b", "otra_rama"], cwd=d, check=True)
            commit_otra = self._commit(d, "docs/otra.md", "otra", "docs: en otra rama")
            self._crear_etiqueta(d, "staging-desplegado", commit_otra)

            # Volvemos a master/main
            subprocess.run(["git", "checkout", "-q", "-"], cwd=d, check=True)
            commit_main = self._commit(d, "docs/readme.md", "v2", "docs: en main")

            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                cod = cdesp.main([
                    "--antes", commit_raiz,
                    "--despues", commit_main,
                    "--etiqueta", "staging-desplegado",
                    "--raiz", d,
                ])
            self.assertEqual(cod, 0)
            self.assertIn("desplegar=true", salida.getvalue())

    def test_caso_5_error_de_git_devuelve_true(self):
        """Caso 5: Error de git (raiz no valida o ref corrupta): true."""
        salida = io.StringIO()
        with contextlib.redirect_stdout(salida):
            cod = cdesp.main([
                "--antes", "ref_invalida_1",
                "--despues", "ref_invalida_2",
                "--etiqueta", "staging-desplegado",
                "--raiz", "directorio_que_no_existe_jamasss",
            ])
        self.assertEqual(cod, 0)
        self.assertIn("desplegar=true", salida.getvalue())

    def test_caso_6_antes_nulo_con_etiqueta_valida_compara_contra_etiqueta(self):
        """Caso 6: --antes nulo (40 ceros) con etiqueta valida: compara contra la etiqueta."""
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            commit_a = self._commit(d, "docs/readme.md", "v1", "docs: inicio")
            self._crear_etiqueta(d, "staging-desplegado", commit_a)

            # Commit posterior solo con docs
            commit_b = self._commit(d, "docs/readme.md", "v2", "docs: actualizacion docs")

            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                cod = cdesp.main([
                    "--antes", cdesp.SHA_NULO,
                    "--despues", commit_b,
                    "--etiqueta", "staging-desplegado",
                    "--raiz", d,
                ])
            self.assertEqual(cod, 0)
            # Como la etiqueta es A y el cambio en B es solo docs, debe ser false
            self.assertIn("desplegar=false", salida.getvalue())
            self.assertIn("origen=etiqueta", salida.getvalue())

    def test_caso_7_escribe_en_github_output_en_todos_los_casos(self):
        """Caso 7: Se escribe desplegar= en GITHUB_OUTPUT en todos los casos."""
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            commit_a = self._commit(d, "docs/readme.md", "v1", "docs: inicio")
            self._crear_etiqueta(d, "staging-desplegado", commit_a)
            commit_b = self._commit(d, "docs/readme.md", "v2", "docs: otro")

            output_file = os.path.join(d, "github_output.txt")
            os.environ["GITHUB_OUTPUT"] = output_file
            try:
                # 1. Caso desplegar=false
                salida = io.StringIO()
                with contextlib.redirect_stdout(salida):
                    cdesp.main([
                        "--antes", commit_a,
                        "--despues", commit_b,
                        "--etiqueta", "staging-desplegado",
                        "--raiz", d,
                    ])
                with open(output_file, "r", encoding="utf-8") as f:
                    contenido = f.read()
                self.assertIn("desplegar=false\n", contenido)

                # 2. Caso desplegar=true (cambio en src/)
                commit_c = self._commit(d, "src/Nuevo.cs", "public class Nuevo {}", "feat: nuevo")
                salida2 = io.StringIO()
                with contextlib.redirect_stdout(salida2):
                    cdesp.main([
                        "--antes", commit_b,
                        "--despues", commit_c,
                        "--etiqueta", "staging-desplegado",
                        "--raiz", d,
                    ])
                with open(output_file, "r", encoding="utf-8") as f:
                    contenido2 = f.read()
                self.assertIn("desplegar=true\n", contenido2)
            finally:
                del os.environ["GITHUB_OUTPUT"]


if __name__ == "__main__":
    unittest.main()
