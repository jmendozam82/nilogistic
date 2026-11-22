# NILOGISTIC — Registro vivo: decisiones D-102 a D-104 (10/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D093_D101.md`. Cierra las deudas documentales D-13 y D-14 de `ESTADO_PROYECTO.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-102 | **Umbral de cobertura (cierra D-13).** El umbral de 70 % de cobertura de la BLL es la **puerta G1**, que se evalúa al cierre de cada Sprint y en cada release con `scripts/ci/verificar_cobertura.py --minimo 70`. No bloquea el merge de cada pull request: el CI por PR solo ejecuta build, pruebas, secretos, SQL y Conventional Commits. Ratifica la decisión verbal del 07/10/2026. HU-038 (regla y E2) reescrita en `Nilogistic_Fase4_HU_Lote2.md` v1.5; E2 se verifica en el cierre de S1. Una excepción al umbral exige registro explícito. | HU-038, DoD-02, cierre de Sprint |
| D-103 | **Flujo de capas en DoD-01 (cierra D-14).** DoD-01 pasa a "Vista → Controller MVC → BLL → DAL → PostgreSQL; el API Controller (/api/v1) también entra por la BLL (ADR-01)", en coherencia con ADR-01 y las instrucciones del proyecto. Reemplaza la redacción original con el salto MVC → API Controller. `Nilogistic_Fase4_HU_Lote1.md` pasa a v1.6. | DoD-01, todas las HU |
| D-104 | **Archivo canónico por lote.** Cada lote de HU tiene un único archivo vigente en `docs fase/` (`…_Lote1.md` v1.6, `…_Lote2.md` v1.5). Las versiones anteriores se conservan en `docs fase/_archivo/` con sufijo de versión (`Lote1_v1.1`, `Lote2_v1.0`) y no se editan. Los nombres con sufijo " (1)" (duplicados de descarga) quedan prohibidos. La copia del Project de claude.ai debe coincidir con el archivo del repositorio. | Gestión documental |

## Efectos sobre otras decisiones

- D-09 (confirmar versiones y fechas de aprobación de las Fases 4 y 5) sigue abierta: ahora existe una única versión vigente por lote, lo que facilita confirmarla.
