# T-ROT — Rotación de credenciales de base de datos de Staging (D-20 / D-115)

> Lo ejecuta **Jorge**, no el ejecutor ni el modelo. **Ningún valor de contraseña, cadena de conexión ni token se pega en el chat, en una ficha, en un commit ni en un registro de CI.** Los valores viven solo en tu gestor de contraseñas, en GitHub (Environment secrets) y en Render (variables secretas).
> Motivo: los ejecutores de HU-008 y HU-009 leyeron `scripts/db/.secrets.local`, que contiene la contraseña del rol propietario y la de `nilogistic_app` (D-115). Se rotan ambas **ahora**; para Producción (S10) se crean credenciales nuevas y distintas (D-110).
> Duración estimada: 20 a 30 minutos. Ventana: Staging puede quedar sin servicio unos minutos.

## 0. Antes de empezar
- Gestor de contraseñas abierto (nombre sugerido de las entradas: `Nilogistic Staging · owner`, `Nilogistic Staging · app`).
- Sesión iniciada en Supabase, GitHub (permisos de administrador del repositorio) y Render.
- Ninguna ejecución de CI o despliegue en curso.

## 1. Generar dos contraseñas nuevas (PowerShell)
No uses `Get-Random` (no es criptográficamente seguro). Genera cada una así y guárdala en el gestor, sin comillas simples (el script de `nilogistic_app` las rechaza):

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Fill($b)
([Convert]::ToBase64String($b) -replace '[^A-Za-z0-9]','').Substring(0,32)
```

Repite el comando para la segunda. Salen 32 caracteres alfanuméricos cada una.

## 2. Rol propietario (`postgres.<ref>`)
1. Supabase → proyecto `nilogistic` (`gpribzfgwvlzxcpdzezs`) → **Project Settings → Database → Database password → Reset database password**. Pega la contraseña **owner** del paso 1.
2. GitHub → repositorio → **Settings → Environments → `staging` → Environment secrets → `DB_URL` → Update**. Reemplaza **solo la contraseña** dentro de la cadena del Session pooler (puerto 5432, usuario `postgres.<ref>`); el resto no cambia.
3. Local: actualiza la contraseña del propietario en `scripts/db/.secrets.local` (archivo ignorado por git). No lo abras desde herramientas que lo muestren a un modelo.

## 3. Rol de la aplicación (`nilogistic_app`)
1. En tu terminal local, con la CLI de Supabase vinculada y la contraseña **app** del paso 1 (sin imprimirla):
   ```powershell
   $env:NILOGISTIC_DB_APP_PASSWORD = Read-Host "Contraseña app" -MaskInput
   bash ./scripts/db/asignar_password_app.sh gpribzfgwvlzxcpdzezs
   Remove-Item Env:NILOGISTIC_DB_APP_PASSWORD
   ```
   Debe responder «Password de nilogistic_app actualizado».
2. Render → servicio de Staging → **Environment** → `ConnectionStrings__Nilogistic` → cambia solo la contraseña de la cadena Npgsql (`Password=...`). Guarda.
3. Actualiza la contraseña de `nilogistic_app` en `scripts/db/.secrets.local`.

## 4. Verificar
1. Render → **Manual Deploy → Deploy latest commit** (o dispara el hook desde GitHub). Espera a que quede *Live* (arranque en frío de 50 s o más en el plan gratuito, D-118).
2. Abre `https://<URL_BASE>/api/v1/estado` y comprueba que responde 200 y que el estado de base de datos es correcto.
3. GitHub → **Actions → CI → Run workflow** sobre `develop`: el job `desplegar-staging` debe terminar en verde (usa el nuevo `DB_URL`).
4. Prueba un correo transaccional de Staging, si tienes la cola activa, para confirmar que la aplicación escribe con el rol nuevo (opcional).

## 5. Cierre
- Si algo falla: error de autenticación en la aplicación → revisa la contraseña de `ConnectionStrings__Nilogistic`; error en `desplegar-staging` → revisa `DB_URL`. Las contraseñas anteriores ya no sirven, no hay vuelta atrás: corrige el valor, no restaures el viejo.
- Borra las contraseñas temporales de la terminal (`Clear-History`) y del portapapeles.
- Avísame «T-ROT hecho» (sin valores). Yo marco D-20 ✅ en `ESTADO_PROYECTO.md` y anoto la fecha.

## 6. Reglas que siguen vigentes (D-115, D-116)
- Los ejecutores **no** leen ni imprimen `scripts/db/.secrets.local`.
- Las comprobaciones con el rol propietario se hacen con `supabase db query --linked` y la variable `SUPABASE_ACCESS_TOKEN`, sin imprimirla.
- Producción (S10): proyecto propio, contraseñas nuevas y distintas de las de Staging; nunca se reutilizan.
