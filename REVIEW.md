# REVIEW.md — Nilogistic

> Resumen obligatorio: toda revisión empieza con `N hallazgos: X bloqueantes, Y nits`.

## Qué es un defecto aquí

- Salto de capa: Vista → BLL/DAL, Controller → DAL, lógica de negocio en DAL o HTTP en BLL.
- DELETE físico en código o migraciones; falta de soft delete o de asiento de auditoría.
- Auditoría mutable, sin IP/antes-después, o corrección que reescribe un asunto existente.
- RLS ausente, tabla sin política deny-by-default o `nilogistic_app` con DELETE.
- Secreto en código, logs, mensajes de error o archivos versionados.
- Formulario público sin CSRF/anti-bot, CSP global debilitada, o correo sin SPF/DKIM/DMARC.
- Migración sin timestamp `<14 dígitos>_<descripcion>.sql`, sin RLS o sin privilegios revisados.
- Prueba existente borrada o modificada; criterio de aceptación sin prueba asociada.
- UI sin foco visible, contraste AA, navegación por teclado, 360 px sin desborde o axe limpio.
- Título de squash no convencional o PR abierto contra `main`.

## Rutas a omitir

`bin/**`, `obj/**`, `TestResults/**`, `wwwroot/lib/**`, `*.min.js`, `.rsc/**`, `.git/**`,
`02-DOCS/raw/**`, archivos de lock generados. El código generado y los artefactos de CI no se revisan
línea a línea.

## Límite de nits

Máximo **3 nits** por revisión. El resto se descarta; si es importante, se convierte en hallazgo
con evidencia o se registra como deuda `D-XX`.

## Rutas siempre humanas

No se integran sin aprobación de Jorge: `.github/workflows/ci.yml`, DNS/Resend/Render/Producción,
credenciales y rotación, certificado X.509/PFX y su resguardo, merges `develop→main`, decisiones de
alcance o release y títulos de squash.

Esta lista informa al revisor. Para que `rsc risk` la haga cumplir de forma automática hace falta
espejarla en `.rsc/sello-config.json` bajo `risk.raise`; ese archivo no se ha creado porque la
configuración del harness es de nivel 2 y requiere aprobación explícita.

## Formato del resumen

`3 hallazgos: 1 bloqueante, 2 nits`, seguido de una línea por hallazgo con ruta, principio
incumplido y evidencia. Un hallazgo repetido se marca `repeat` y no se vuelve a plantear.
