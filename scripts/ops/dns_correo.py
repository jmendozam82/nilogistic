#!/usr/bin/env python3
r"""HU-011: verificador de solo lectura de los registros DNS de correo.

Consulta DNS sobre HTTPS (Cloudflare) y guarda/compara fotos de los registros
de autenticacion de correo (SPF, DKIM, DMARC y retorno de Resend). Solo
biblioteca estandar; ningun subcomando escribe en DNS ni usa credenciales.

Uso:
    python dns_correo.py snapshot --dominio nilogistic.com \
        --marketing news.nilogistic.com --salida antes.json
    python dns_correo.py verificar --antes antes.json --despues despues.json
"""

import argparse
import json
import re
import sys
import urllib.parse
import urllib.request
from datetime import datetime, timezone

DOH_URL = "https://cloudflare-dns.com/dns-query"
TIMEOUT_SEGUNDOS = 15
TIPOS = {"TXT": 16, "MX": 15}
POLITICAS_DMARC = ("none", "quarantine", "reject")


def construir_url(nombre, tipo):
    parametros = urllib.parse.urlencode({"name": nombre, "type": tipo})
    return f"{DOH_URL}?{parametros}"


def texto_txt(dato):
    fragmentos = re.findall(r'"((?:[^"\\]|\\.)*)"', dato or "")
    if fragmentos:
        return "".join(fragmentos)
    return (dato or "").strip()


def extraer_registros(datos, tipo):
    if not isinstance(datos, dict):
        return []
    if datos.get("Status", 0) != 0:
        return []
    numero = TIPOS.get(tipo)
    registros = []
    for respuesta in datos.get("Answer", []) or []:
        if respuesta.get("type") != numero:
            continue
        dato = respuesta.get("data", "")
        registros.append(texto_txt(dato) if tipo == "TXT" else dato)
    return registros


def pedir(url, timeout=TIMEOUT_SEGUNDOS, abrir=None):
    abrir = abrir or urllib.request.urlopen
    peticion = urllib.request.Request(url, headers={"accept": "application/dns-json"})
    with abrir(peticion, timeout=timeout) as respuesta:
        return json.loads(respuesta.read().decode("utf-8"))


def consultar(nombre, tipo, timeout=TIMEOUT_SEGUNDOS, abrir=None):
    datos = pedir(construir_url(nombre, tipo), timeout, abrir)
    return extraer_registros(datos, tipo)


def es_spf(valor):
    return valor.strip().lower().startswith("v=spf1")


def contar_spf(registros):
    return sum(1 for r in registros if es_spf(r))


def etiquetas(valor):
    resultado = {}
    for parte in valor.split(";"):
        parte = parte.strip()
        if not parte or "=" not in parte:
            continue
        clave, _, contenido = parte.partition("=")
        resultado[clave.strip().lower()] = contenido.strip()
    return resultado


def evaluar_dmarc(registros):
    if len(registros) != 1:
        return False, f"se esperaba un unico DMARC y hay {len(registros)}"
    valor = registros[0].strip()
    if not valor.lower().startswith("v=dmarc1"):
        return False, "el registro no empieza por v=DMARC1"
    campos = etiquetas(valor)
    politica = campos.get("p", "").lower()
    if politica not in POLITICAS_DMARC:
        return False, "falta la etiqueta p= con valor none, quarantine o reject"
    if not campos.get("rua", "").lower().startswith("mailto:"):
        return False, "falta la etiqueta rua=mailto:"
    return True, f"un unico DMARC con p={politica} y rua"


def _regs(foto, tipo, nombre):
    return list(foto.get("consultas", {}).get(f"{tipo} {nombre}", []))


def comparar_instantaneas(antes, despues):
    dominio = despues.get("dominio") or antes.get("dominio")
    selector = despues.get("selector") or antes.get("selector") or "resend"
    marketing = despues.get("marketing") or antes.get("marketing")
    resultados = []

    spf_antes = [r for r in _regs(antes, "TXT", dominio) if es_spf(r)]
    spf_despues = [r for r in _regs(despues, "TXT", dominio) if es_spf(r)]
    c1 = len(spf_despues) <= 1 and spf_despues == spf_antes
    resultados.append({
        "id": "C1",
        "ok": c1,
        "mensaje": f"SPF de la raiz sin cambios (registros: {len(spf_despues)})"
        if c1 else
        f"el SPF de la raiz cambio o se duplico (antes: {len(spf_antes)}, "
        f"despues: {len(spf_despues)})",
    })

    mx_antes = _regs(antes, "MX", dominio)
    mx_despues = _regs(despues, "MX", dominio)
    c2 = sorted(mx_despues) == sorted(mx_antes)
    resultados.append({
        "id": "C2",
        "ok": c2,
        "mensaje": "MX de la raiz sin cambios" if c2
        else "los MX de la raiz cambiaron",
    })

    dmarc_ok, dmarc_mensaje = evaluar_dmarc(_regs(despues, "TXT", f"_dmarc.{dominio}"))
    resultados.append({"id": "C3", "ok": dmarc_ok, "mensaje": dmarc_mensaje})

    dkim = _regs(despues, "TXT", f"{selector}._domainkey.{dominio}")
    dkim_ok = bool(dkim) and any(r.strip() for r in dkim)
    prefijo = dkim[0][:12] if dkim else ""
    resultados.append({
        "id": "C4",
        "ok": dkim_ok,
        "mensaje": f"DKIM presente (inicio: {prefijo}...)" if dkim_ok
        else "falta el registro DKIM de Resend o esta vacio",
    })

    mx_send = _regs(despues, "MX", f"send.{dominio}")
    c5 = len(mx_send) > 0
    resultados.append({
        "id": "C5",
        "ok": c5,
        "mensaje": "MX de retorno de Resend presente" if c5
        else "falta el MX de retorno en send.",
    })

    if marketing:
        dkim_m = _regs(despues, "TXT", f"{selector}._domainkey.{marketing}")
        mx_m = _regs(despues, "MX", f"send.{marketing}")
        c6 = (bool(dkim_m) and any(r.strip() for r in dkim_m)) and len(mx_m) > 0
        resultados.append({
            "id": "C6",
            "ok": c6,
            "mensaje": "DKIM y MX de retorno del subdominio de marketing presentes"
            if c6 else
            "faltan DKIM o MX de retorno del subdominio de marketing",
        })
    return resultados


def plan_consultas(dominio, selector, marketing):
    plan = [
        ("TXT", dominio),
        ("MX", dominio),
        ("TXT", f"_dmarc.{dominio}"),
        ("TXT", f"{selector}._domainkey.{dominio}"),
        ("MX", f"send.{dominio}"),
        ("TXT", f"send.{dominio}"),
    ]
    if marketing:
        plan += [
            ("TXT", marketing),
            ("MX", marketing),
            ("TXT", f"_dmarc.{marketing}"),
            ("TXT", f"{selector}._domainkey.{marketing}"),
            ("MX", f"send.{marketing}"),
            ("TXT", f"send.{marketing}"),
        ]
    return plan


def tomar_snapshot(dominio, selector, marketing, consultar_fn=consultar):
    consultas = {}
    for tipo, nombre in plan_consultas(dominio, selector, marketing):
        consultas[f"{tipo} {nombre}"] = consultar_fn(nombre, tipo)
    return {
        "dominio": dominio,
        "selector": selector,
        "marketing": marketing,
        "capturado_utc": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "consultas": consultas,
    }


def _cmd_snapshot(args):
    foto = tomar_snapshot(args.dominio, args.selector, args.marketing)
    with open(args.salida, "w", encoding="utf-8", newline="\n") as f:
        json.dump(foto, f, indent=2, sort_keys=True, ensure_ascii=True)
        f.write("\n")
    for clave in sorted(foto["consultas"]):
        print(f"{clave}: {len(foto['consultas'][clave])} registro(s)")
    print(f"Snapshot guardado en {args.salida}")
    return 0


def _cmd_verificar(args):
    with open(args.antes, encoding="utf-8") as f:
        antes = json.load(f)
    with open(args.despues, encoding="utf-8") as f:
        despues = json.load(f)
    resultados = comparar_instantaneas(antes, despues)
    print(f"{'ID':<4} {'RESULTADO':<8} COMPROBACION")
    for r in resultados:
        estado = "OK" if r["ok"] else "FALLA"
        print(f"{r['id']:<4} {estado:<8} {r['mensaje']}")
    fallos = sum(1 for r in resultados if not r["ok"])
    if fallos:
        print(f"\n{fallos} comprobacion(es) fallidas")
        return 1
    print("\nTodas las comprobaciones pasan")
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Verificador de solo lectura de DNS de correo (HU-011)")
    sub = parser.add_subparsers(dest="comando", required=True)

    p_snap = sub.add_parser("snapshot", help="Consulta y guarda los registros en JSON")
    p_snap.add_argument("--dominio", default="nilogistic.com")
    p_snap.add_argument("--marketing", default=None)
    p_snap.add_argument("--selector", default="resend")
    p_snap.add_argument("--salida", required=True)
    p_snap.set_defaults(fn=_cmd_snapshot)

    p_ver = sub.add_parser("verificar", help="Compara dos fotos y comprueba C1..C6")
    p_ver.add_argument("--antes", required=True)
    p_ver.add_argument("--despues", required=True)
    p_ver.set_defaults(fn=_cmd_verificar)

    args = parser.parse_args(argv)
    return args.fn(args)


if __name__ == "__main__":
    sys.exit(main())
