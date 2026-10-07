#!/usr/bin/env bash
# Asigna la contrasena del rol de la aplicacion `nilogistic_app` en el proyecto vinculado.
# HU-003 / ADR-03: el password es un secreto, por eso NO vive en las migraciones.
# Requisitos: supabase CLI vinculado (supabase link --project-ref <ref>) o variable
# SUPABASE_PROJECT_REF con la referencia del proyecto.
# Uso:  NILOGISTIC_DB_APP_PASSWORD='...'  ./scripts/db/asignar_password_app.sh [<project-ref>]
set -euo pipefail

ref="${1:-${SUPABASE_PROJECT_REF:-}}"
if [[ -z "$ref" ]]; then
  echo "Indica el project-ref o define SUPABASE_PROJECT_REF." >&2
  exit 1
fi

if [[ -z "${NILOGISTIC_DB_APP_PASSWORD:-}" ]]; then
  echo "Define NILOGISTIC_DB_APP_PASSWORD con la nueva contrasena." >&2
  exit 1
fi

if [[ "$NILOGISTIC_DB_APP_PASSWORD" == *"'"* ]]; then
  echo "La contrasena no puede contener comilla simple." >&2
  exit 1
fi

# El charset generado en .secrets.local no incluye comillas simples.
supabase db query --linked \
  "alter role nilogistic_app with password '${NILOGISTIC_DB_APP_PASSWORD}';" >/dev/null

echo "Password de nilogistic_app actualizado en ${ref}."