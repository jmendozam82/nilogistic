# NILOGISTIC — Registro vivo: decisiones D-072 a D-087 (07/10/2026)

Se aplican a la Fase 6 v0.5 **sin cambio de versión** (aprobación de Jorge, 07/10/2026).

## A. Decisiones de gestión (aprobadas por Jorge el 07/10/2026)

| ID | Decisión | Sección de la Fase 6 afectada |
|---|---|---|
| D-072 | No se crea sprint de colchón (S12). S0 puede arrancar antes del 09/11/2026 y el trabajo adelantado cuenta como holgura. El calendario oficial se mantiene; el corte DNS se comunica como rango. | 3 (Calendario), 6.2 |
| D-073 | T-SUPR la ejecuta Jorge (Administrador). Respaldo: la persona de la tarea #13, a nombrar el 18/01/2027. Declarado en `runbooks/T-SUPR_supresion_de_datos.md`. | S-614, runbook |
| D-074 | Única fecha no disponible: 04/12/2026. Cualquier otra se avisa con 15 días de anticipación. | S-608 |
| D-075 | **P-314 resuelto.** PFX como secret file de Render + copia cifrada en el gestor de contraseñas; contraseña en una entrada distinta del gestor + sobre sellado para la persona de la tarea #13. Nunca en repo, Supabase ni junto a respaldos. | S-612, `runbooks/P-314_custodia_certificado.md` |
| D-076 | **D-071 aprobada:** T-SUPR de 2 SP en S11. | S-614 pasa a Aprobado |
| D-077 | Plazo de supresión de datos: 15 días hábiles, sujeto a validación legal **antes del 18/12/2026** (HU-044 se redacta con ese plazo provisional). | HU-044, S-613 |
| D-078 | HU-014 se reestima **al inicio de S3** (antes: S4). Se avisa a Jorge si supera **6 SP**. Ver ⚠ de discrepancia con D-070 (umbral de 5 SP). | 6.4 |
| D-079 | Umbral verde ratificado: ≥ 25 SP en 10 días hábiles (≥ 48 SP en S0 + S1). | D-064 |

## B. Decisiones técnicas tomadas al construir S0 (propuestas; piden aprobación en la revisión de S0)

| ID | Decisión | Motivo |
|---|---|---|
| D-080 | Solución `.slnx`, gestión central de paquetes con versiones flotantes, `NuGetAudit` en nivel alto y `TreatWarningsAsErrors` cuando `CI=true`. FluentAssertions **7.x** (la 8 es comercial). | Licencia y reproducibilidad |
| D-081 | Todas las migraciones SQL corren con el **mismo rol propietario**; cada archivo en una transacción e idempotente. | Los privilegios por defecto solo aplican a objetos creados por el rol que los declaró |
| D-082 | Patrón **DB-1**: funciones `SECURITY DEFINER` con `search_path` fijo, `EXECUTE` revocado a PUBLIC y concedido solo a `nilogistic_app`. Verificado en PG16 (`supabase/spikes/db1_security_definer.sql`); falta PG17. | El rol de la app no tiene DELETE |
| D-083 | CSP con nonce por solicitud; modo `Reporte` solo en Staging (variable de entorno); `/swagger` relajado fuera de Producción. | HU-012 |
| D-084 | Bandera de indexación, modo CSP y confianza de proxy **no** van en `appsettings`: se fijan por variable de entorno (permite `UseSetting` en pruebas y evita fugas a Producción). | S-611 |
| D-085 | HSTS de 30 días, sin `includeSubDomains` ni `preload` hasta después del corte DNS. | Convivencia con SiteGround (D-005) |
| D-086 | `Proxy:ConfiarEnCualquierProxy` solo durante el spike en Staging; el arranque falla en Producción si está activa. HU-013 (S4) la reemplaza por `Proxy:Redes`. | ADR-14 |
| D-087 | El entregable "Staging con noindex" de S0 se verifica por pruebas automáticas; el Staging real aparece con HU-006 (S1). Se ajusta la redacción del entregable de S0. | Inconsistencia S0/S1 |

## C. Pendientes que cambian de estado
P-314 → Resuelto (D-075). S-612 → resguardo definido. P-313/D-071 → Aprobado (D-076).

## D. Cambios exactos a aplicar en la Fase 6 v0.5 (sin cambio de versión)

| Sección | Cambio |
|---|---|
| 2, S-608 | Añadir "única fecha no disponible: 04/12/2026; las demás se avisan con 15 días" (D-074) |
| 2, S-612 | Estado: resguardo definido (D-075); ver runbook P-314 |
| 2, S-613 y S-614 | S-614 pasa a **Aprobado**; añadir responsable Jorge y respaldo de la tarea #13 (D-073, D-076); plazo de 15 días hábiles sujeto a validación legal antes del 18/12/2026 (D-077) |
| 3 (Calendario) | Nota: S0 puede arrancar antes del 09/11; lo adelantado cuenta como holgura; no hay S12 (D-072) |
| 6.1 y 6.2 | Umbral verde ≥ 25 SP / ≥ 48 SP ratificado (D-079); fecha de R1 y corte DNS comunicados como rango (D-072) |
| 6.4 y 4 (S4) | HU-014 se reestima al **inicio de S3** y se avisa si supera **6 SP** (D-078; reemplaza "en S4" y "5 SP" de D-070) |
| 4 (S0, Entregable) | "Staging con noindex" se verifica por pruebas automáticas; Staging real con HU-006 en S1 (D-087, propuesta) |
| 8 (Hitos) | T-SUPR: responsable Jorge; respaldo nombrado el 18/01/2027 |
| 10 (RP-12, RP-13) | RP-12: ejecutor definido; RP-13: validación legal del plazo antes del 18/12/2026 |
| 11 | Añadir D-072 a D-087 y marcar P-314 y D-071 como resueltos |
