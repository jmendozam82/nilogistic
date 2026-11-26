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

## Rotación (E10; el verbo `rotar-claves` llega con el Pase C)

1. Emite el PFX nuevo (vigencia 2 años, S-612) y guárdalo según la tabla de arriba.
2. Render: sube el PFX nuevo como secret file en `ProteccionDatos__RutaPfx` y el **anterior** en
   `ProteccionDatos__RutaPfxAnterior` (con sus contraseñas en variables distintas).
3. Despliega: la app protege con el nuevo y desprotege con cualquiera de los dos.
4. `rotar-claves` re-protege todas las filas con el certificado nuevo (Pase C); las cookies siguen válidas.
5. Verifica sesiones vigentes y retira el certificado anterior de Render y del gestor.

## Vigencia y avisos

- Vigencia de 2 años; el aviso por correo a 60 y 30 días llega con la tarea del Pase C (E9).
- El arranque rechaza un certificado ya vencido: la rotación debe hacerse antes (este runbook).
- Sin base de datos disponible, el anillo degrada a claves del proceso con un error visible en el
  log: el sitio sirve, pero esas sesiones no sobreviven a un reinicio (aviso D-156 temporal).
