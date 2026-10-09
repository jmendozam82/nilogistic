# NILOGISTIC — Registro vivo: decisiones D-088 a D-092 (09/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D072_D087.md`. Decisiones de gestión aprobadas por Jorge el 09/10/2026.

| ID | Decisión | Afecta |
|---|---|---|
| D-088 | **El Sprint 1 se inicia el 09/10/2026**, antes de su fecha planificada (23/11/2026). El calendario oficial no cambia; lo adelantado cuenta como holgura, en el mismo criterio de D-072 para S0. Orden propuesto: HU-007 → HU-034 y HU-010 → HU-006 → HU-036 y HU-037, según cuentas (D-01) y diseño visual (D-02). | Fase 6, S1 |
| D-089 | **El escenario E1 de HU-007 se implementa sin la cláusula de auditoría** ("se genera auditoría con valores antes y después"); esa cláusula se verifica con una prueba en HU-008 (S2). | HU-007, HU-008 (deuda D-12) |
| D-090 | **HU-038 se cierra** con 4 de 5 criterios (E1, E3, E4, E5). E2 (el pipeline falla bajo el 70 % de cobertura) queda como puerta G1 al cierre de sprint y no bloquea el merge de cada PR, según la decisión del 07/10/2026 reflejada en `ci.yml`; pendiente formalizar el texto (deuda D-13). | HU-038, CI |
| D-091 | **Modelo ejecutor de HU-007:** Big Pickle (OpenCode, gratuito), con la ficha `docs/tareas/HU-007.md` como única entrada; revisión posterior por Claude con `docs/revision/checklist-revision.md`. Se evaluará su desempeño para decidir el reparto de modelos en el resto del Sprint 1. | Flujo de trabajo |
| D-092 | **Supuestos SUP-2 a SUP-7 de la ficha HU-007 aprobados tal como están:** `IActorActual` mínimo con `ActorSistema`; filtro de soft delete con nombre `"SoftDelete"` (plan B: filtro anónimo y deuda técnica); cada método del repositorio persiste por sí mismo; paginación de 1 a 100 con 20 por defecto; `xmin` fuera de la entidad base (HU-048); propiedades `FechaCreacionUtc` etc. mapeadas a `creado_utc` etc. | HU-007 |
