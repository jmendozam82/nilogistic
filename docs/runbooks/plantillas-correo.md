# Runbook — Plantillas de correo (HU-030)

Las plantillas viven como recursos embebidos en `Nilogistic.BLL`, bajo
`src/Nilogistic.BLL/Correo/Plantillas/`. Cada plantilla tiene un archivo `.html`
y otro `.txt`, y el manifiesto `catalogo.json` describe sus variables, versiones
y hashes.

## Agregar o cambiar una plantilla

1. Edita los archivos `.html` y `.txt` de la plantilla. Manten la estructura
   comun (banda navy, filete ambar, un solo `h1`, pie). Los estilos van en
   linea por compatibilidad con clientes de correo.
2. Actualiza `catalogo.json`:
   - Si es cambio menor, incrementa la parte `MENOR` (por ejemplo `1.0` a `1.1`).
   - Si es cambio mayor, incrementa `MAYOR` y reinicia `MENOR` (por ejemplo
     `1.1` a `2.0`).
   - Mueve la version anterior a `historial` con su `sha256Html`, `sha256Txt`,
     fecha ISO (`yyyy-MM-dd`) y motivo del cambio.
3. Recalcula los hashes SHA-256 de los archivos cambiados y actualiza
   `sha256Html` y `sha256Txt`.
4. Ejecuta `scripts/local/verificar.ps1` antes del push.

## Recalcular SHA-256 con PowerShell

Desde la raiz del repositorio:

```powershell
$archivo = "src/Nilogistic.BLL/Correo/Plantillas/activacion_cuenta.html"
$bytes = [System.IO.File]::ReadAllBytes($archivo)
$hash = [BitConverter]::ToString([System.Security.Cryptography.SHA256]::HashData($bytes)).Replace("-", "").ToLower()
Write-Host $hash
```

Asegurate de que el archivo este guardado en UTF-8 sin BOM y con saltos LF
(verifica con `git diff --check`).

## Configurar `Correo:UrlBaseSitio`

- Desarrollo: `appsettings.json` tiene `"UrlBaseSitio": "https://localhost"`.
- Staging y Produccion: variable de entorno `Correo__UrlBaseSitio` (por ejemplo
  `https://staging.nilogistic.com`). No se fija el dominio real en los archivos
  versionados.

Esta URL se usa para los enlaces de las plantillas y para la imagen del logo
(`/img/nilogistic-logo-blanco.png`).

## Ver la vista previa

El Pase B anade `/diseno/correo` (lista) y `/diseno/correo/{plantilla}`
(HTML o texto) en `DisenoController`. Esas rutas responden 404 en Produccion y
se usan solo para validacion visual local y en Staging. El HTML devuelto lleva
una CSP propia (`default-src 'none'; style-src 'unsafe-inline'; img-src 'self'`)
para permitir los estilos en linea de las plantillas sin relajar la CSP global.
