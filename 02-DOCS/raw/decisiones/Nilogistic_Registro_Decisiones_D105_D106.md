# NILOGISTIC — Registro vivo: decisiones D-105 y D-106 (10/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D102_D104.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-105 | **Fechas de aprobación (cierra D-09).** Jorge autorizó proyectar las fechas para que no limiten el avance; se ajustan sobre la marcha si cambian. Fase 4 (Lotes 1 y 2): aprobada el **05/10/2026**, fecha de la versión inicial de los lotes, que la Fase 5 ya cita como aprobada. Fase 5 (A, B y C): aprobada el **07/10/2026**, fecha en que el Plan de Sprints (Fase 6) la da por aprobada; los documentos pasan de v0.1 a **v1.0**. La meta original de aprobación (30/10/2026) se adelantó. Fase 6 sigue en borrador v0.5; aprobación final prevista el 06/11/2026. | Fases 4 y 5 |
| D-106 | **Puerta G1 automatizada.** El CI ejecuta `verificar_cobertura.py --minimo 70` sobre la cobertura de la BLL en PR hacia `main`, en push a `main` y a demanda (`workflow_dispatch`); no corre en PR hacia `develop`. Se implementa como paso condicional del job `build-y-pruebas` para reutilizar el informe de cobertura sin repetir pruebas. Complementa D-102. Para que bloquee el merge, Jorge marca `build-y-pruebas` como check requerido en la protección de `main`. | CI, cierre de Sprint |
