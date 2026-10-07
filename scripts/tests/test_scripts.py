import os
import sys
import tempfile
import unittest

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(RAIZ, "ci"))
sys.path.insert(0, os.path.join(RAIZ, "trazabilidad"))

import verificar_cobertura as vc  # noqa: E402
import escenarios_sin_prueba as esp  # noqa: E402
import validar_conventional_commits as cvc  # noqa: E402


def escribir(ruta, texto):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    with open(ruta, "w", encoding="utf-8") as f:
        f.write(texto)


class CoberturaTests(unittest.TestCase):
    def _xml(self, d, tasa):
        escribir(os.path.join(d, "g", "coverage.cobertura.xml"), f'<coverage line-rate="{tasa}"></coverage>')

    def test_pasa_si_alcanza_el_umbral(self):
        with tempfile.TemporaryDirectory() as d:
            self._xml(d, 0.71)
            self.assertEqual(vc.main(["--minimo", "70", "--directorio", d]), 0)

    def test_falla_si_esta_bajo_el_umbral(self):
        with tempfile.TemporaryDirectory() as d:
            self._xml(d, 0.69)
            self.assertEqual(vc.main(["--minimo", "70", "--directorio", d]), 1)

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


if __name__ == "__main__":
    unittest.main()
