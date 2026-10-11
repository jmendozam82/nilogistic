# NILOGISTIC — Registro vivo: decisiones D-093 a D-100 (10/10/2026)

Continúa `Nilogistic_Registro_Decisiones_D088_D092.md`. Supuestos SUP-1 a SUP-8 de la ficha `docs/tareas/HU-034.md`, aprobados por Jorge el 10/10/2026.

| ID | Decisión | Afecta |
|---|---|---|
| D-093 | **HU-034, SUP-1:** el ejecutor soporta solo programación por intervalo (`intervalo:<segundos>`); cron queda diferido a la primera tarea de R2 que lo necesite (deuda D-16). Desviación parcial aceptada de "intervalo o cron". | HU-034 |
| D-094 | **HU-034, SUP-2:** `Tarea` y `TareaEjecucion` no heredan `EntidadBase` (tablas técnicas con columnas propias según Fase 5 B). | HU-034 |
| D-095 | **HU-034, SUP-3:** exclusión entre instancias con reserva atómica por fila (`FOR UPDATE SKIP LOCKED` + lease en `proxima_ejecucion_utc`), no advisory locks de sesión. Semántica de al menos una vez; la idempotencia de los manejadores es obligatoria. | HU-034 |
| D-096 | **HU-034, SUP-4:** la HU no inserta filas en `app.tareas`; cada HU de negocio añade la suya por migración. | HU-034 |
| D-097 | **HU-034, SUP-5:** una tarea sin manejador registrado queda `Fallida` (`SinManejador`) y se reprograma. | HU-034 |
| D-098 | **HU-034, SUP-6 (con cambio):** la lógica del ejecutor vive en la BLL; el `BackgroundService` vive en el host `Nilogistic.Aplicacion.Tareas`, sin dependencia del DAL ni de `Hosting.Abstractions` en la BLL. | HU-034 |
| D-099 | **HU-034, SUP-7:** un timeout no se reintenta; solo se reintentan las excepciones (con espera exponencial). | HU-034 |
| D-100 | **HU-034, SUP-8:** el campo `error` guarda solo el nombre del tipo de la excepción; el detalle completo va a los logs. | HU-034 |
