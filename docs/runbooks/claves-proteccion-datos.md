# Claves de Data Protection y certificado X.509 — HU-014 (S-604, S-612)

> Anillo de claves en `identidad.claves_proteccion_datos`, cifrado con certificado X.509 de vigencia 2 años.
> Las cookies y los enlaces sobreviven a los despliegues; el respaldo y la restauración son verbos del host.

## Dónde vive cada cosa (P-314, D-075)

| Elemento | Dónde vive | Dónde NO puede estar |
|---|---|---|
| PFX del certificado | Secret file de Render + copia cifrada en el gestor de contraseñas de Jorge | Repositorio, Supabase, junto a los respaldos |
| Contraseña del PFX | Entrada **distinta** del gestor + sobre sellado para la persona de la tarea #13 | Repositorio, Supabase, junto al PFX, junto a los respaldos |
| Respaldo del anillo (JSON) | Fuera del servicio (equipo de Jorge); contiene filas **ya cifradas** con el certificado | Junto al PFX ni junto a su contraseña |
| Anillo en caliente | `identidad.claves_proteccion_datos` (solo INSERT/SELECT para `nilogistic_app`) | — |

## Emisión del certificado (SUP-9)

En el equipo de Jorge (SDK .NET 10), sin que el secreto pase por ninguna sesión de agente:

```powershell
dotnet run scripts/ops/generar_certificado_dp.cs -- "$env:USERPROFILE\Documents\nilogistic-dp-staging.pfx"
```

- Crea el PFX (RSA 3072, vigencia 2 años, S-612) y un `<ruta>.render.txt` con el mismo PFX en
  **base64**: los secret files de Render viajan como texto y el binario directo se corrompe al
  pegarlo; la guardia de la app acepta ambas formas.
- La contraseña se genera al azar y se muestra **una sola vez**: cópiala de inmediato al gestor.
- Custodia inmediata: PFX → gestor (entrada 1) + Render secret file; contraseña → gestor
  (entrada **distinta**) + sobre sellado para la persona de la tarea #13.
- Borra el `.render.txt` cuando Render ya lo tenga cargado. Nunca versiones ninguna salida.

## Configuración por entorno

| Variable | Dónde | Notas |
|---|---|---|
| `ProteccionDatos__RutaPfx` | Render (secret file) | Ruta del PFX vigente. **Obligatoria en Staging/Producción: sin ella la app no arranca** (fallo cerrado, HU-014 B3) |
| `CERTIFICADO_DP_CONTRASENA` | Render (variable) | Contraseña del PFX vigente; nunca versionada |
| `ProteccionDatos__RutaPfxAnterior` | Render (solo durante rotación) | PFX del certificado anterior |
| `CERTIFICADO_DP_CONTRASENA_ANTERIOR` | Render (solo durante rotación) | Contraseña del PFX anterior |
| `ProteccionDatos__PermitirSinCertificado` | Solo hosts de pruebas sin base real (p. ej. el job `accesibilidad` de la CI) | Última salida documentada: arranca sin cifrar y deja un error visible en el log. **Nunca en Render** |

Fuera de Local sin certificado el arranque falla con un mensaje claro; el certificado vencido o con
contraseña incorrecta también (los mensajes jamás incluyen la contraseña).

## Respaldo (E6)

```bash
dotnet Nilogistic.Aplicacion.dll respaldar-claves /ruta/segura/claves-$(date -u +%Y%m%d).json
```

- Escribe un JSON con `respaldo_utc` y las filas `nombre`/`xml_protegido`/`creado_utc` (ya cifradas).
- Queda asiento `respaldo_claves_dp` en auditoría con fecha y número de filas.
- Mueve el archivo fuera del servicio; el PFX y su contraseña **no** van con él.

## Restauración (E7) — entorno recreado

1. Recrea el entorno (base vacía, certificado montado de nuevo como secret file).
2. Aplica migraciones (`supabase db push` o la CI).
3. Restaura el anillo:

```bash
dotnet Nilogistic.Aplicacion.dll restaurar-claves /ruta/segura/claves-2026xxxx.json --confirmo
```

- Inserta solo lo que falta; repetirlo no duplica (cuenta `ya_presentes`).
- Un archivo vacío, corrupto o con filas inválidas se rechaza **sin tocar el anillo**.
- Asiento `restauracion_claves_dp` con las cantidades.
4. Comprueba: una cookie emitida antes sigue siendo válida y el aviso D-156 no aparece en el arranque.
   La prueba completa en Staging queda documentada al cierre de la HU (§12 de la ficha).

## Rotación (E10; implementada en el Pase C)

1. Emite el PFX nuevo (2 años, S-612) con el generador y guárdalo según la tabla de arriba.
2. Render: sube el PFX nuevo como secret file en `ProteccionDatos__RutaPfx` (el anterior pasa a
   `ProteccionDatos__RutaPfxAnterior`, con sus contraseñas en variables distintas:
   `CERTIFICADO_DP_CONTRASENA` y `CERTIFICADO_DP_CONTRASENA_ANTERIOR`).
3. Despliega: la app protege con el nuevo y desprotege con cualquiera de los dos
   (`UnprotectKeysWithAnyCertificate`).
4. Ejecuta la rotación (crea la clave nueva protegida con el certificado nuevo; las filas viejas
   no se reescriben — el anillo es de solo inserción — y el anterior las sigue desprotegiendo):

   ```bash
   dotnet Nilogistic.Aplicacion.dll rotar-claves --confirmo
   ```

   - Verifica que la clave nueva quedó en la base (el verbo la cuenta antes y después y rechaza
     si no cuadra: o todo o nada; queda asiento `rotacion_certificado_dp`).
   - Si la base no respondiera, el anillo degrada al proceso con error visible: el verbo lo
     detecta y no audita ni autoriza a retirar el anterior.
5. Comprueba que las sesiones y las cookies siguen válidas (el `§12` del cierre lo hizo en
   Staging; en Producción se repite tras la primera rotación real).
6. **Solo entonces** retira el anterior: borra `RutaPfxAnterior` y su contraseña de Render, y el
   PFX viejo del gestor. Los avisos de vencimiento (E9) se rearman solos con la nueva fecha.

## Vigencia y avisos

- Vigencia de 2 años (S-612). La tarea `vigilar_certificado` corre a diario (siembra de la
  migración `20261125150000`) y avisa a la lista `alertas_tecnicas` a los **60 y a los 30 días**
  (E9): un aviso por umbral, certificado y destinatario (la clave de idempotencia lo garantiza;
  rotar el certificado rearma los avisos con la nueva fecha). El correo sugiere este runbook.
- El arranque rechaza un certificado ya vencido: la rotación debe hacerse antes (este runbook);
  si el certificado venciera de noche, la app no arranca hasta montar el nuevo.
- Sin base de datos disponible, el anillo degrada a claves del proceso con un error visible en el
  log: el sitio sirve, pero esas sesiones no sobreviven a un reinicio (aviso D-156 temporal).
