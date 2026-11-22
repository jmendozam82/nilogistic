# NILOGISTIC — Registro vivo: decisión D-115 (12/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D114.md`.

| ID | Decisión | Afecta |
|---|---|---|
| D-115 | **D-20 se amplía a la contraseña del rol propietario.** Los ejecutores (HU-008 Pase C y HU-009) leyeron `scripts/db/.secrets.local`, que contiene la contraseña del propietario además de la de `nilogistic_app`. Antes de Producción (S10) se rotan ambas y no se reutilizan en Producción. Hasta entonces, las comprobaciones de Staging con el rol propietario las ejecuta el ejecutor cargando el secreto en una variable sin imprimirla. A evaluar en D-20: que las comprobaciones con propietario las corra Jorge, o un script que no exponga el secreto al modelo. | D-20, HU-008, HU-009 |
