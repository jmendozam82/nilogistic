# NILOGISTIC — Registro vivo: decisión D-114 y deuda D-24 (12/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D113.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-114 | **HU-009 aprobada tras revisión de Claude (12/10/2026).** (1) Mientras no exista un tipo de excepción de validación, la BLL puede lanzar `InvalidOperationException` con mensaje genérico; se revisa al introducir el patrón de validación (HU-020 / FluentValidation en la API). (2) `IRepositorioAuditoria` queda registrado en `IOC/Dependencias.cs` conforme a la ficha. Tras el merge se comprueba §12 en Staging dentro de una transacción con `rollback`. | HU-009, HU-020, HU-064 |
| D-24 | **Deuda menor:** unificar el registro de repositorios (hoy `PersistenciaExtensions` para Salud/Tareas/Correo e `IOC/Dependencias.cs` para Auditoría) en una sola ubicación; se resuelve al introducir el siguiente repositorio de negocio. | HU-009, IOC, DAL |
