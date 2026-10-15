import json
import os
import sys
import tempfile
import unittest

RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(RAIZ, "ops"))

import dns_correo as dns  # noqa: E402


def foto(dominio="nilogistic.com", selector="resend", marketing=None, consultas=None):
    return {
        "dominio": dominio,
        "selector": selector,
        "marketing": marketing,
        "consultas": consultas or {},
    }


def resultado(resultados, cid):
    return next(r for r in resultados if r["id"] == cid)


def escribir(ruta, texto):
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    with open(ruta, "w", encoding="utf-8") as f:
        f.write(texto)


class ParseoTests(unittest.TestCase):
    def test_es_spf_insensible_a_mayusculas_y_espacios(self):
        self.assertTrue(dns.es_spf("  v=spf1 include:a ~all"))
        self.assertTrue(dns.es_spf("V=SPF1 -all"))
        self.assertFalse(dns.es_spf("google-site-verification=abc"))

    def test_contar_spf(self):
        registros = ["v=spf1 include:a", "otro", "v=spf1 include:b"]
        self.assertEqual(dns.contar_spf(registros), 2)

    def test_texto_txt_concatena_fragmentos_partidos(self):
        self.assertEqual(dns.texto_txt('"abc" "def"'), "abcdef")

    def test_texto_txt_sin_comillas_devuelve_el_texto(self):
        self.assertEqual(dns.texto_txt("v=spf1 -all"), "v=spf1 -all")

    def test_status_distinto_de_cero_no_lanza_y_no_hay_registros(self):
        self.assertEqual(dns.extraer_registros({"Status": 3}, "TXT"), [])

    def test_answer_ausente_no_lanza_y_no_hay_registros(self):
        self.assertEqual(dns.extraer_registros({"Status": 0}, "TXT"), [])
        self.assertEqual(dns.extraer_registros({}, "MX"), [])

    def test_extrae_solo_el_tipo_pedido(self):
        datos = {
            "Status": 0,
            "Answer": [
                {"type": 16, "data": '"v=spf1 -all"'},
                {"type": 15, "data": "10 mx.siteground.com."},
            ],
        }
        self.assertEqual(dns.extraer_registros(datos, "TXT"), ["v=spf1 -all"])
        self.assertEqual(dns.extraer_registros(datos, "MX"), ["10 mx.siteground.com."])


class DmarcTests(unittest.TestCase):
    def test_sin_dmarc_falla(self):
        ok, _ = dns.evaluar_dmarc([])
        self.assertFalse(ok)

    def test_dos_dmarc_falla(self):
        ok, _ = dns.evaluar_dmarc(["v=DMARC1; p=none", "v=DMARC1; p=none"])
        self.assertFalse(ok)

    def test_p_none_sin_rua_falla(self):
        ok, _ = dns.evaluar_dmarc(["v=DMARC1; p=none"])
        self.assertFalse(ok)

    def test_p_quarantine_con_rua_pasa(self):
        ok, _ = dns.evaluar_dmarc(
            ["v=DMARC1; p=quarantine; rua=mailto:analista@nilogistic.com"])
        self.assertTrue(ok)

    def test_p_invalida_falla(self):
        ok, _ = dns.evaluar_dmarc(["v=DMARC1; p=tedioso; rua=mailto:a@b.com"])
        self.assertFalse(ok)


class CompararTests(unittest.TestCase):
    def test_c1_falla_con_dos_spf_en_la_raiz(self):
        antes = foto(consultas={"TXT nilogistic.com": ["v=spf1 include:a ~all"]})
        despues = foto(consultas={"TXT nilogistic.com": [
            "v=spf1 include:a ~all", "v=spf1 include:b ~all"]})
        self.assertFalse(resultado(dns.comparar_instantaneas(antes, despues), "C1")["ok"])

    def test_c1_falla_si_el_spf_cambia(self):
        antes = foto(consultas={"TXT nilogistic.com": ["v=spf1 include:a ~all"]})
        despues = foto(consultas={"TXT nilogistic.com": ["v=spf1 include:b ~all"]})
        self.assertFalse(resultado(dns.comparar_instantaneas(antes, despues), "C1")["ok"])

    def test_c1_pasa_si_el_spf_es_identico(self):
        registro = "v=spf1 include:_spf.siteground.com ~all"
        antes = foto(consultas={"TXT nilogistic.com": [registro]})
        despues = foto(consultas={"TXT nilogistic.com": [registro]})
        self.assertTrue(resultado(dns.comparar_instantaneas(antes, despues), "C1")["ok"])

    def test_c2_pasa_con_mx_en_distinto_orden(self):
        antes = foto(consultas={"MX nilogistic.com": ["10 mx1.", "20 mx2."]})
        despues = foto(consultas={"MX nilogistic.com": ["20 mx2.", "10 mx1."]})
        self.assertTrue(resultado(dns.comparar_instantaneas(antes, despues), "C2")["ok"])

    def test_c2_falla_con_mx_distinto(self):
        antes = foto(consultas={"MX nilogistic.com": ["10 mx1."]})
        despues = foto(consultas={"MX nilogistic.com": ["10 mx9."]})
        self.assertFalse(resultado(dns.comparar_instantaneas(antes, despues), "C2")["ok"])

    def test_c3_falla_sin_dmarc(self):
        self.assertFalse(resultado(dns.comparar_instantaneas(foto(), foto()), "C3")["ok"])

    def test_c4_falla_con_dkim_vacio(self):
        despues = foto(consultas={"TXT resend._domainkey.nilogistic.com": [""]})
        self.assertFalse(resultado(dns.comparar_instantaneas(foto(), despues), "C4")["ok"])

    def test_c4_pasa_con_dkim_no_vacio(self):
        despues = foto(consultas={
            "TXT resend._domainkey.nilogistic.com": ["p=MIGfMA0GCSqGSIb3DQEBAQUAA4GN"]})
        self.assertTrue(resultado(dns.comparar_instantaneas(foto(), despues), "C4")["ok"])

    def test_c5_falla_sin_mx_de_retorno(self):
        self.assertFalse(resultado(dns.comparar_instantaneas(foto(), foto()), "C5")["ok"])

    def test_c6_se_evalua_solo_con_marketing(self):
        despues = foto(marketing="news.nilogistic.com")
        resultados = dns.comparar_instantaneas(foto(), despues)
        self.assertFalse(resultado(resultados, "C6")["ok"])

    def test_c6_pasa_con_dkim_y_mx_de_marketing(self):
        marketing = "news.nilogistic.com"
        despues = foto(marketing=marketing, consultas={
            "TXT resend._domainkey.news.nilogistic.com": ["p=MIGfMA0GCSq"],
            "MX send.news.nilogistic.com": ["10 feedback-smtp.us-east-1.amazonses.com."],
        })
        self.assertTrue(resultado(dns.comparar_instantaneas(foto(), despues), "C6")["ok"])


class CliTests(unittest.TestCase):
    def _foto_ok(self, dominio="nilogistic.com"):
        return foto(consultas={
            f"TXT {dominio}": ["v=spf1 include:_spf.siteground.com ~all"],
            f"MX {dominio}": ["10 mx1.siteground.com."],
            f"TXT _dmarc.{dominio}": ["v=DMARC1; p=none; rua=mailto:analista@nilogistic.com"],
            f"TXT resend._domainkey.{dominio}": ["p=MIGfMA0GCSqGSIb3DQEBAQUAA4GN"],
            f"MX send.{dominio}": ["10 feedback-smtp.us-east-1.amazonses.com."],
        })

    def test_verificar_cli_ok(self):
        with tempfile.TemporaryDirectory() as d:
            antes = os.path.join(d, "antes.json")
            despues = os.path.join(d, "despues.json")
            escribir(antes, json.dumps(self._foto_ok()))
            escribir(despues, json.dumps(self._foto_ok()))
            self.assertEqual(dns.main(["verificar", "--antes", antes, "--despues", despues]), 0)

    def test_verificar_cli_falla_devuelve_1(self):
        with tempfile.TemporaryDirectory() as d:
            antes = os.path.join(d, "antes.json")
            despues = os.path.join(d, "despues.json")
            escribir(antes, json.dumps(self._foto_ok()))
            rota = self._foto_ok()
            rota["consultas"]["MX nilogistic.com"] = ["10 mx9.siteground.com."]
            escribir(despues, json.dumps(rota))
            self.assertEqual(dns.main(["verificar", "--antes", antes, "--despues", despues]), 1)


if __name__ == "__main__":
    unittest.main()
