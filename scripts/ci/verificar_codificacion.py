#!/usr/bin/env python3
"""Verify encoding of tracked text files (D-139).

Rules:
  1. No BOM at the start of the file.
  2. The file content must be valid UTF-8.
  3. No line may contain double-encoded (mojibake) text.

Usage:
    python verificar_codificacion.py [--raiz .]

Exit codes: 0 if valid, 1 if findings, 2 on usage error.
"""

import argparse
import os
import subprocess
import sys

TEXT_EXTENSIONS = frozenset([
    ".md", ".txt", ".cs", ".cshtml", ".csproj", ".slnx", ".props",
    ".css", ".js", ".json", ".yml", ".yaml", ".sql", ".py", ".sh",
    ".html", ".xml", ".svg", ".config", ".editorconfig",
    ".gitattributes", ".gitignore",
])

EXCLUDED_PREFIX = os.path.normpath("src/Nilogistic.Aplicacion/wwwroot/lib")

SENTINEL = "nilogistic:codificacion-ok"

BOMS = (
    (b"\xef\xbb\xbf", "UTF-8"),
    (b"\xff\xfe", "UTF-16 LE"),
    (b"\xfe\xff", "UTF-16 BE"),
)


class GitError(Exception):
    """Git command failed."""


def tracked_files(root):
    """Return tracked files under root using git ls-files -z."""
    try:
        result = subprocess.run(
            ["git", "ls-files", "-z"],
            cwd=root,
            capture_output=True,
            check=False,
        )
    except FileNotFoundError as error:
        raise GitError("git executable not found") from error
    if result.returncode != 0:
        raise GitError(result.stderr.strip() or "git ls-files failed")
    if not result.stdout:
        return []
    return [path for path in result.stdout.decode("ascii").split("\x00") if path]


def is_text(path):
    """Return True if path should be checked."""
    normalized = os.path.normpath(path)
    if normalized.startswith(EXCLUDED_PREFIX):
        return False
    _, ext = os.path.splitext(path)
    if ext in TEXT_EXTENSIONS:
        return True
    name = os.path.basename(path)
    return name in TEXT_EXTENSIONS


def has_non_ascii(line):
    """Return True if line contains any non-ASCII character."""
    return any(ord(c) > 127 for c in line)


def detect_mojibake(line):
    """Detect mojibake by reinterpreting the line as UTF-8 bytes.

    Tries cp1252 first; if encoding fails, tries latin-1.
    Returns the decoded string if it differs from the original,
    otherwise None.
    """
    for encoding in ("cp1252", "latin-1"):
        try:
            reinterpreted = line.encode(encoding).decode("utf-8")
        except (UnicodeEncodeError, UnicodeDecodeError):
            continue
        if reinterpreted != line:
            return reinterpreted
    return None


def analyze(path, content_bytes):
    """Analyze file bytes and return a list of findings."""
    findings = []

    # R1: BOM
    for bom, name in BOMS:
        if content_bytes.startswith(bom):
            findings.append(f"R1: BOM {name}")
            break

    # R2: valid UTF-8
    try:
        content = content_bytes.decode("utf-8")
    except UnicodeDecodeError as error:
        findings.append(f"R2: UTF-8 invalido en desplazamiento {error.start}")
        return findings

    # R3: mojibake
    mojibake_lines = []
    for number, line in enumerate(content.split("\n"), start=1):
        if not has_non_ascii(line):
            continue
        if SENTINEL in line:
            continue
        if detect_mojibake(line) is not None:
            mojibake_lines.append(number)
            if len(mojibake_lines) <= 5:
                findings.append(f"R3: doble codificacion en linea {number}")

    if len(mojibake_lines) > 5:
        remaining = len(mojibake_lines) - 5
        findings.append(f"... y {remaining} mas")

    return findings


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("--raiz", default=".")
    args = parser.parse_args(argv)

    root = os.path.abspath(args.raiz)
    if not os.path.isdir(root):
        print(f"ERROR: el directorio no existe: {root}", file=sys.stderr)
        return 2

    try:
        paths = tracked_files(root)
    except GitError as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2

    checked = 0
    failures = 0
    for path in paths:
        if not is_text(path):
            continue
        full_path = os.path.join(root, path)
        try:
            with open(full_path, "rb") as handle:
                content = handle.read()
        except OSError as error:
            print(f"ERROR: no se pudo leer {path}: {error}", file=sys.stderr)
            return 2

        checked += 1
        findings = analyze(path, content)
        for finding in findings:
            if finding.startswith("R1") or finding.startswith("R2"):
                print(f"KO {path}: {finding}")
            elif finding.startswith("..."):
                print(f"KO {path}: {finding}")
            else:
                # R3: "R3: doble codificacion en linea N"
                parts = finding.split("linea ")
                line_number = parts[1]
                print(f"KO {path}:{line_number}: {finding}")
            failures += 1

    if failures:
        return 1

    print(f"OK: {checked} archivo(s) revisado(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
