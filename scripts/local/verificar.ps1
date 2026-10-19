#!/usr/bin/env pwsh
#Requires -Version 5.1
<#
.SYNOPSIS
    Ejecuta las comprobaciones locales previas al PR (D-141).

.DESCRIPTION
    Replica las verificaciones de la CI antes de abrir un PR:
    Conventional Commits, migraciones, sintaxis de workflows, codificacion
    UTF-8, pruebas de scripts, build de la solucion y pruebas .NET.

.PARAMETER Base
    Rama base contra la que comparar (por defecto: develop).

.PARAMETER OmitirDal
    Omite las pruebas DAL que requieren Docker.

.PARAMETER ConNavegador
    Incluye las pruebas de accesibilidad con Playwright.
#>
[CmdletBinding()]
param(
    [string]$Base = "develop",
    [switch]$OmitirDal,
    [switch]$ConNavegador
)

$ErrorActionPreference = "Stop"

function Encontrar-Python {
    $candidatos = @("python3", "python", "py")
    foreach ($candidato in $candidatos) {
        if (Get-Command $candidato -ErrorAction SilentlyContinue) {
            try {
                $salida = & $candidato --version 2>&1
                if ($LASTEXITCODE -eq 0 -or "$salida" -match "Python") {
                    return $candidato
                }
            } catch {
                # El comando existe pero no funciona; probar el siguiente.
            }
        }
    }
    return $null
}

function Requerir-Comando {
    param([string]$Nombre)
    if (-not (Get-Command $Nombre -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: no se encontro '$Nombre' en el PATH." -ForegroundColor Red
        exit 2
    }
}

$python = Encontrar-Python
Requerir-Comando "git"
if (-not $python) { Requerir-Comando "python" }
Requerir-Comando "dotnet"

$repo = Resolve-Path "$PSScriptRoot\..\.."
Set-Location -LiteralPath $repo

$comprobaciones = New-Object System.Collections.Generic.List[object]

function Ejecutar {
    param(
        [string]$Nombre,
        [scriptblock]$Bloque
    )
    Write-Host "`n== $Nombre ==" -ForegroundColor Cyan
    $ok = $false
    try {
        & $Bloque
        if ($LASTEXITCODE -eq 0) {
            $ok = $true
        }
    } catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
    }
    $comprobaciones.Add([PSCustomObject]@{ Nombre = $Nombre; OK = $ok })
    return $ok
}

Ejecutar "Conventional Commits" { & $python scripts/ci/validar_conventional_commits.py --base $Base }
Ejecutar "Migraciones" { & $python scripts/ci/verificar_migraciones.py --directorio supabase/migrations --base $Base }
Ejecutar "Sintaxis de los workflows" { & $python scripts/ci/verificar_workflows.py }
Ejecutar "Codificacion UTF-8" { & $python scripts/ci/verificar_codificacion.py }
Ejecutar "Pruebas de scripts" { & $python -m unittest discover -s scripts/tests }
Ejecutar "Build de la solucion" { dotnet build Nilogistic.slnx -c Release -warnaserror }

$proyectos = @(
    "tests/Nilogistic.Arquitectura.Tests",
    "tests/Nilogistic.BLL.Tests",
    "tests/Nilogistic.Aplicacion.Tests"
)
if (-not $OmitirDal) {
    $proyectos += "tests/Nilogistic.DAL.Tests"
}
if ($ConNavegador) {
    $proyectos += "tests/Nilogistic.Accesibilidad.Tests"
}

foreach ($proyecto in $proyectos) {
    Ejecutar "Tests: $proyecto" { dotnet test $proyecto --no-build -c Release }
}

Write-Host "`n== Resumen ==" -ForegroundColor Cyan
$fallos = 0
foreach ($c in $comprobaciones) {
    $estado = if ($c.OK) { "OK" } else { "KO" }
    $color = if ($c.OK) { "Green" } else { "Red" }
    Write-Host ("{0,-50} {1}" -f $c.Nombre, $estado) -ForegroundColor $color
    if (-not $c.OK) { $fallos++ }
}

if ($fallos -gt 0) {
    Write-Host "`n$fallos comprobacion(es) fallaron." -ForegroundColor Red
    exit 1
}

Write-Host "`nTodas las comprobaciones pasaron." -ForegroundColor Green
exit 0
