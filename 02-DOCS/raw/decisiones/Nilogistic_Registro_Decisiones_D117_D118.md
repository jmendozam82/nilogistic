# NILOGISTIC — Registro vivo: decisiones D-117 y D-118 (15/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D116.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-117 | **Dependabot integra en `develop`, agrupa menores y parches e ignora versiones mayores.** Sin `target-branch`, sus PR iban a `main`, cuyo árbol conserva una copia antigua de `validar_conventional_commits.py` sin `--no-merges`: el commit de merge sintético de GitHub fallaba como «formato no convencional» (la puerta G1 no era la causa). Las versiones mayores se evalúan a mano en una HU o ADR. **FluentAssertions se queda en 7.x:** desde la 8.x la licencia es de pago para uso comercial (Xceed) y Nilogistic es comercial; se cerró el PR #34. Se aceptan los parches (EF Core 10.0.x, Mvc.Testing) ya contra `develop` y con CI en verde. Configuración en `.github/dependabot.yml` (PR #40). | `.github/dependabot.yml`, CI, dependencias NuGet |
| D-118 | **Staging y Producción siguen en el plan gratuito de Render hasta que el cliente decida.** La instancia se duerme por inactividad y el primer acceso tarda 50 s o más; el sondeo de salud del pipeline ya espera 90 s y 40 intentos, y las comprobaciones manuales deben calentar la instancia y usar un timeout de 180 s. Antes de Producción se revisa de nuevo con el cliente (disponibilidad, arranque en frío, Blueprint D-19). | D-19, S10, despliegue |
