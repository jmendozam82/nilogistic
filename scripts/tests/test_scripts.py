import contextlib
import io
import os
import subprocess
import sys
import tempfile
import unittest

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(RAIZ, "ci"))
sys.path.insert(0, os.path.join(RAIZ, "trazabilidad"))

import verificar_cobertura as vc  # noqa: E402
import escenarios_sin_prueba as esp  # noqa: E402
import validar_conventional_commits as cvc  # noqa: E402
import verificar_migraciones as vm  # noqa: E402
import verificar_codificacion as vcod  # noqa: E402
import cambios_despliegue as cdesp  # noqa: E402


def escribir(ruta, texto):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    with open(ruta, "w", encoding="utf-8") as f:
        f.write(texto)


class CoberturaTests(unittest.TestCase):
    def _xml(self, d, tasa_raiz, paquetes):
        paqs = "".join(f'<package name="{n}" line-rate="{t}"/>' for n, t in paquetes)
        escribir(
            os.path.join(d, "g", "coverage.cobertura.xml"),
            f'<coverage line-rate="{tasa_raiz}"><packages>{paqs}</packages></coverage>',
        )

    def test_pasa_si_la_bll_alcanza_el_umbral_aunque_la_raiz_no(self):
        with tempfile.TemporaryDirectory() as d:
            self._xml(d, 0.49, [("Nilogistic.BLL", 0.95)])
            self.assertEqual(vc.main(["--minimo", "70", "--directorio", d]), 0)

    def test_imprime_el_paquete_medido(self):
        with tempfile.TemporaryDirectory() as d:
            self._xml(d, 0.49, [("Nilogistic.BLL", 0.95)])
            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                vc.main(["--minimo", "70", "--directorio", d])
            self.assertIn("(Nilogistic.BLL): 95.0 %", salida.getvalue())

    def test_falla_si_la_bll_esta_bajo_el_umbral(self):
        with tempfile.TemporaryDirectory() as d:
            self._xml(d, 0.49, [("Nilogistic.BLL", 0.60)])
            self.assertEqual(vc.main(["--minimo", "70", "--directorio", d]), 1)

    def test_error_si_el_paquete_no_aparece(self):
        with tempfile.TemporaryDirectory() as d:
            self._xml(d, 0.99, [("Nilogistic.DAL", 0.95)])
            self.assertEqual(vc.main(["--minimo", "70", "--directorio", d]), 2)

    def test_error_si_no_hay_reporte(self):
        with tempfile.TemporaryDirectory() as d:
            self.assertEqual(vc.main(["--minimo", "70", "--directorio", d]), 2)


class TrazabilidadTests(unittest.TestCase):
    def test_detecta_escenario_sin_prueba(self):
        with tempfile.TemporaryDirectory() as d:
            escribir(os.path.join(d, "hu", "S0.txt"), "HU-001 E1\nHU-001 E2\n")
            escribir(os.path.join(d, "t", "A.cs"), '[Hu("HU-001", "E1")]\npublic void X(){}')
            self.assertEqual(esp.main(["--hu-dir", os.path.join(d, "hu"), "--pruebas", os.path.join(d, "t"), "--sprint", "S0"]), 1)

    def test_ok_si_todos_tienen_prueba(self):
        with tempfile.TemporaryDirectory() as d:
            escribir(os.path.join(d, "hu", "S0.txt"), "HU-001 E1\n")
            escribir(os.path.join(d, "t", "A.cs"), '[Hu("HU-001","E1")]')
            self.assertEqual(esp.main(["--hu-dir", os.path.join(d, "hu"), "--pruebas", os.path.join(d, "t"), "--sprint", "S0"]), 0)

    def test_sin_archivo_de_sprint_no_bloquea(self):
        with tempfile.TemporaryDirectory() as d:
            self.assertEqual(esp.main(["--hu-dir", d, "--pruebas", d, "--sprint", "S0"]), 0)

    def _sprints(self, d, pruebas):
        escribir(os.path.join(d, "hu", "S0.txt"), "HU-001 E1\n")
        escribir(os.path.join(d, "hu", "S1.txt"), "HU-007 E1\nHU-007 E2\n")
        escribir(os.path.join(d, "t", "A.cs"), pruebas)
        return ["--hu-dir", os.path.join(d, "hu"), "--pruebas", os.path.join(d, "t")]

    def test_todos_falla_si_falta_un_escenario_de_cualquier_sprint(self):
        with tempfile.TemporaryDirectory() as d:
            base = self._sprints(d, '[Hu("HU-001","E1")]\n[Hu("HU-007","E1")]')
            self.assertEqual(esp.main(base + ["--sprint", "todos"]), 1)

    def test_todos_ok_si_cada_sprint_tiene_sus_pruebas(self):
        with tempfile.TemporaryDirectory() as d:
            base = self._sprints(d, '[Hu("HU-001","E1")]\n[Hu("HU-007","E1")]\n[Hu("HU-007","E2")]')
            self.assertEqual(esp.main(base + ["--sprint", "todos"]), 0)

    def test_un_sprint_concreto_no_evalua_los_demas(self):
        with tempfile.TemporaryDirectory() as d:
            base = self._sprints(d, '[Hu("HU-001","E1")]')
            self.assertEqual(esp.main(base + ["--sprint", "S0"]), 0)


class ConventionalCommitsTests(unittest.TestCase):
    def test_acepta_tipos_convencionales(self):
        ok = [
            "feat: nueva funcionalidad",
            "fix(ci): corregir YAML",
            "docs(hu): actualizar estado",
            "chore(scripts): refactor de validacion",
            "refactor!: cambio rompedor",
            "ci: agregar job de validacion",
        ]
        self.assertTrue(cvc.validar(ok))

    def test_rechaza_formato_no_convencional(self):
        malos = [
            "Actualizar archivos",
            "Feat: tipo en mayusculas",
            "fix(CI): contexto con mayusculas",
            "fix:",
            "fix : sin dos punto pegados",
        ]
        self.assertFalse(cvc.validar(malos))

    def test_rechaza_no_ascii(self):
        self.assertFalse(cvc.validar(["fix: corregir aplicaci\u00f3n"]))

    def test_rechaza_descripcion_muy_larga(self):
        self.assertFalse(cvc.validar(["feat: " + "x" * 73]))

    def test_ignora_commits_de_merge(self):
        with tempfile.TemporaryDirectory() as d:
            def git(*args):
                subprocess.run(
                    ["git", *args], cwd=d, check=True,
                    stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                )

            git("init", "-q")
            git("config", "user.email", "prueba@nilogistic.test")
            git("config", "user.name", "Prueba")
            escribir(os.path.join(d, "a.txt"), "a")
            git("add", "a.txt")
            git("commit", "-q", "-m", "feat: base del repositorio")
            primero = subprocess.run(
                ["git", "rev-parse", "HEAD"], cwd=d,
                capture_output=True, text=True, check=True,
            ).stdout.strip()
            rama = subprocess.run(
                ["git", "symbolic-ref", "--short", "HEAD"], cwd=d,
                capture_output=True, text=True, check=True,
            ).stdout.strip()
            git("checkout", "-q", "-b", "rama")
            escribir(os.path.join(d, "b.txt"), "b")
            git("add", "b.txt")
            git("commit", "-q", "-m", "fix: trabajo en la rama")
            git("checkout", "-q", rama)
            escribir(os.path.join(d, "c.txt"), "c")
            git("add", "c.txt")
            git("commit", "-q", "-m", "docs: avance en la base")
            git("merge", "-q", "--no-ff", "rama", "-m", "Merge branch 'rama'")

            previo = os.getcwd()
            os.chdir(d)
            try:
                mensajes = cvc.commits_del_rango(primero)
            finally:
                os.chdir(previo)

        self.assertIn("fix: trabajo en la rama", mensajes)
        self.assertNotIn("Merge branch 'rama'", mensajes)


class MigracionesTests(unittest.TestCase):
    def test_acepta_un_nombre_bien_formado(self):
        self.assertEqual(vm.validar_nombres(["20261109000100_extensiones_y_esquemas.sql"]), [])

    def test_rechaza_nombres_mal_formados(self):
        errores = vm.validar_nombres(["20261109000100_uno.sql", "sin_formato.sql", "2026_corta.sql"])
        self.assertEqual(len(errores), 2)
        self.assertTrue(all("regla 1" in e for e in errores))

    def test_rechaza_marcas_de_tiempo_duplicadas(self):
        errores = vm.validar_nombres(
            ["20261109000100_uno.sql", "20261109000100_dos.sql", "20261109000200_tres.sql"]
        )
        self.assertEqual(len(errores), 1)
        self.assertIn("regla 2", errores[0])

    def test_orden_rechaza_migracion_anterior(self):
        errores = vm.validar_orden(["20261109000900_ultima.sql"], ["20261109000100_vieja.sql"])
        self.assertEqual(len(errores), 1)
        self.assertIn("regla 3", errores[0])

    def test_orden_rechaza_migracion_igual(self):
        errores = vm.validar_orden(["20261109000900_ultima.sql"], ["20261109000900_otra.sql"])
        self.assertEqual(len(errores), 1)
        self.assertIn("regla 3", errores[0])

    def test_orden_acepta_migracion_posterior(self):
        self.assertEqual(
            vm.validar_orden(["20261109000900_ultima.sql"], ["20261109001000_nueva.sql"]), []
        )

    def test_orden_sin_existentes_no_restringe(self):
        self.assertEqual(vm.validar_orden([], ["20261109000100_primera.sql"]), [])

    def test_main_pasa_con_migraciones_correctas(self):
        with tempfile.TemporaryDirectory() as d:
            escribir(os.path.join(d, "20261109000100_a.sql"), "")
            escribir(os.path.join(d, "20261109000200_b.sql"), "")
            self.assertEqual(vm.main(["--directorio", d]), 0)

    def test_main_falla_con_nombre_mal_formado(self):
        with tempfile.TemporaryDirectory() as d:
            escribir(os.path.join(d, "20261109000100_a.sql"), "")
            escribir(os.path.join(d, "malo.sql"), "")
            self.assertEqual(vm.main(["--directorio", d]), 1)

    def test_main_error_si_no_existe_el_directorio(self):
        self.assertEqual(vm.main(["--directorio", "no/existe/jamas"]), 2)

    def test_main_error_si_la_base_no_existe(self):
        with tempfile.TemporaryDirectory() as d:
            escribir(os.path.join(d, "20261109000100_a.sql"), "")
            self.assertEqual(vm.main(["--directorio", d, "--base", "ref_que_no_existe"]), 2)


class CodificacionTests(unittest.TestCase):
    def test_limpio_con_acentos_y_simbolos_pasa(self):
        texto = "ultima \u00f1and\u00fa \u2713 \u2b1c \u2192"
        self.assertEqual(vcod.analyze("x.md", texto.encode("utf-8")), [])

    def test_bom_utf8_falla(self):
        hallazgos = vcod.analyze("x.md", b"\xef\xbb\xbf texto")
        self.assertTrue(any(h.startswith("R1") for h in hallazgos))

    def test_bom_utf16_falla(self):
        hallazgos = vcod.analyze("x.md", b"\xff\xfe\x00")
        self.assertTrue(any(h.startswith("R1") for h in hallazgos))

    def test_bytes_no_utf8_falla(self):
        hallazgos = vcod.analyze("x.md", b"\xff")
        self.assertTrue(any(h.startswith("R2") for h in hallazgos))

    def test_doble_codificacion_de_e_falla(self):
        doble = "\u00e9".encode("utf-8").decode("cp1252")
        contenido = f"linea 1\n{doble}\nlinea 3".encode("utf-8")
        hallazgos = vcod.analyze("x.md", contenido)
        self.assertTrue(any("linea 2" in h for h in hallazgos))

    def test_doble_codificacion_de_check_falla(self):
        doble = "\u2705".encode("utf-8").decode("latin-1")
        contenido = f"ok\n{doble}\nfin".encode("utf-8")
        hallazgos = vcod.analyze("x.md", contenido)
        self.assertTrue(any("linea 2" in h for h in hallazgos))

    def test_sentinel_omite_r3(self):
        doble = "\u00e9".encode("utf-8").decode("cp1252")
        contenido = f"nilogistic:codificacion-ok {doble}\n".encode("utf-8")
        self.assertEqual(vcod.analyze("x.md", contenido), [])

    def test_ignora_wwwroot_lib(self):
        self.assertFalse(vcod.is_text("src/Nilogistic.Aplicacion/wwwroot/lib/bootstrap/css/bootstrap.css"))

    def test_ignora_extension_no_listada(self):
        self.assertFalse(vcod.is_text("binario.bin"))

    def _inicializar_repo(self, d):
        subprocess.run(["git", "init", "-q"], cwd=d, check=True)
        subprocess.run(["git", "config", "user.email", "p@n.test"], cwd=d, check=True)
        subprocess.run(["git", "config", "user.name", "P"], cwd=d, check=True)

    def test_main_pasa_en_repo_limpio(self):
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo(d)
            escribir(os.path.join(d, "limpio.md"), "ultima \u00f1and\u00fa \u2705")
            subprocess.run(["git", "add", "limpio.md"], cwd=d, check=True)
            subprocess.run(["git", "commit", "-q", "-m", "inicial"], cwd=d, check=True)
            self.assertEqual(vcod.main(["--raiz", d]), 0)

    def test_main_falla_con_bom(self):
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo(d)
            with open(os.path.join(d, "bom.md"), "wb") as f:
                f.write(b"\xef\xbb\xbf texto")
            subprocess.run(["git", "add", "bom.md"], cwd=d, check=True)
            subprocess.run(["git", "commit", "-q", "-m", "inicial"], cwd=d, check=True)
            self.assertEqual(vcod.main(["--raiz", d]), 1)

    def test_main_error_si_no_es_repo(self):
        with tempfile.TemporaryDirectory() as d:
            self.assertEqual(vcod.main(["--raiz", d]), 2)

    def test_main_error_si_directorio_inexistente(self):
        self.assertEqual(vcod.main(["--raiz", "no/existe/jamas"]), 2)


class DespliegueTests(unittest.TestCase):
    def test_solo_docs_no_requiere_despliegue(self):
        self.assertFalse(cdesp.requiere_despliegue(["docs/runbooks/despliegue.md", "docs/tareas/DT-3.md"]))

    def test_estado_proyecto_no_requiere_despliegue(self):
        self.assertFalse(cdesp.requiere_despliegue(["ESTADO_PROYECTO.md"]))

    def test_cs_en_src_requiere_despliegue(self):
        self.assertTrue(cdesp.requiere_despliegue(["src/Nilogistic.BLL/Servicios/EmpleoServicio.cs"]))

    def test_migracion_requiere_despliegue(self):
        self.assertTrue(cdesp.requiere_despliegue(["supabase/migrations/20261109000100_extensiones.sql"]))

    def test_render_yaml_requiere_despliegue(self):
        self.assertTrue(cdesp.requiere_despliegue(["render.yaml"]))

    def test_lista_vacia_no_requiere_despliegue(self):
        self.assertFalse(cdesp.requiere_despliegue([]))

    def test_mezcla_docs_y_codigo_requiere_despliegue(self):
        self.assertTrue(cdesp.requiere_despliegue(["docs/tareas/DT-3.md", "src/Nilogistic.Aplicacion/Program.cs"]))

    def test_slnx_requiere_despliegue(self):
        self.assertTrue(cdesp.requiere_despliegue(["Nilogistic.slnx"]))

    def test_global_json_requiere_despliegue(self):
        self.assertTrue(cdesp.requiere_despliegue(["global.json"]))

    def test_sha_nulo_devuelve_true(self):
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                codigo = cdesp.main(["--antes", cdesp.SHA_NULO, "--despues", "HEAD", "--raiz", d])
            self.assertEqual(codigo, 0)
            self.assertIn("desplegar=true", salida.getvalue())

    def test_solo_docs_en_repo_devuelve_false(self):
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            primero = self._commit_inicial(d, "docs/readme.md", "hola")
            escribir(os.path.join(d, "docs", "otro.md"), "adios")
            subprocess.run(["git", "add", "."], cwd=d, check=True)
            subprocess.run(["git", "commit", "-q", "-m", "docs: otro documento"], cwd=d, check=True)
            segundo = subprocess.run(
                ["git", "rev-parse", "HEAD"], cwd=d,
                capture_output=True, text=True, check=True,
            ).stdout.strip()
            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                codigo = cdesp.main(["--antes", primero, "--despues", segundo, "--raiz", d])
            self.assertEqual(codigo, 0)
            self.assertIn("desplegar=false", salida.getvalue())

    def test_codigo_en_repo_devuelve_true(self):
        with tempfile.TemporaryDirectory() as d:
            self._inicializar_repo_git(d)
            primero = self._commit_inicial(d, "docs/readme.md", "hola")
            escribir(os.path.join(d, "src", "Program.cs"), "class Program {}")
            subprocess.run(["git", "add", "."], cwd=d, check=True)
            subprocess.run(["git", "commit", "-q", "-m", "feat: codigo"], cwd=d, check=True)
            segundo = subprocess.run(
                ["git", "rev-parse", "HEAD"], cwd=d,
                capture_output=True, text=True, check=True,
            ).stdout.strip()
            salida = io.StringIO()
            with contextlib.redirect_stdout(salida):
                codigo = cdesp.main(["--antes", primero, "--despues", segundo, "--raiz", d])
            self.assertEqual(codigo, 0)
            self.assertIn("desplegar=true", salida.getvalue())

    def _inicializar_repo_git(self, d):
        subprocess.run(["git", "init", "-q"], cwd=d, check=True)
        subprocess.run(["git", "config", "user.email", "p@n.test"], cwd=d, check=True)
        subprocess.run(["git", "config", "user.name", "P"], cwd=d, check=True)

    def _commit_inicial(self, d, ruta, contenido):
        escribir(os.path.join(d, ruta), contenido)
        subprocess.run(["git", "add", "."], cwd=d, check=True)
        subprocess.run(["git", "commit", "-q", "-m", "docs: inicial"], cwd=d, check=True)
        return subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=d,
            capture_output=True, text=True, check=True,
        ).stdout.strip()


if __name__ == "__main__":
    unittest.main()
