# Runbook — Verificación local previa al PR (DT-3)

Script: `scripts/local/verificar.ps1`

## Cuándo correrlo

Antes de cada push a `develop` o `main`, según D-140 (una sola entrega por HU).
Ejecuta las mismas comprobaciones que la CI en menos tiempo y sin depender de la red.

## Requisitos

- `git`
- `python3` / `python` / `py` (cualquiera de los tres)
- SDK de .NET 10 (segun `global.json`)
- Docker (solo si no se usa `-OmitirDal`)
- Playwright y Chromium (solo si se usa `-ConNavegador`)

## Uso

```powershell
# Verificacion completa (incluye DAL; requiere Docker)
pwsh -File scripts/local/verificar.ps1

# Omitir pruebas DAL
pwsh -File scripts/local/verificar.ps1 -OmitirDal

# Incluir pruebas de accesibilidad con Playwright
pwsh -File scripts/local/verificar.ps1 -ConNavegador

# Comparar contra otra base (por defecto: develop)
pwsh -File scripts/local/verificar.ps1 -Base origin/develop
```

## Que comprueba

1. Conventional Commits del rango `Base..HEAD`.
2. Nombre y orden de las migraciones de Supabase.
3. Sintaxis y estructura de los workflows de GitHub Actions (requiere PyYAML: `pip install pyyaml`).
4. Codificacion UTF-8 sin BOM ni doble codificacion.
5. Pruebas de los scripts de CI.
6. Build de `Nilogistic.slnx` con advertencias como errores.
7. Pruebas .NET:
   - Siempre: Arquitectura, BLL y Aplicacion.
   - Con `-OmitirDal`: omite DAL.
   - Con `-ConNavegador`: incluye Accesibilidad.

## Como leer el resultado

El script imprime una tabla `OK` / `KO` al final:

```
== Resumen ==
Conventional Commits                               OK
Migraciones                                        OK
Sintaxis de los workflows                          OK
Codificacion UTF-8                                 OK
Pruebas de scripts                                 OK
Build de la solucion                               OK
Tests: tests/Nilogistic.Arquitectura.Tests         OK
...
Todas las comprobaciones pasaron.
```

- Codigo de salida `0`: todas las comprobaciones pasaron.
- Codigo de salida `1`: al menos una comprobacion fallo.
- Codigo de salida `2`: falta algun prerequisito (`git`, `python` o `dotnet`).

Todos los pasos se ejecutan aunque uno falle, para mostrar el diagnostico completo.
