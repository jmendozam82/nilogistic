# Checklist de revisión (Claude) — por HU

> Revisión **por diff**, no por proyecto completo. Solo se solicita con la CI en verde.

## 0. Preparación (barata)
1. Confirmar CI en verde (build, tests, arquitectura, secretos, sql).
2. Leer la ficha `docs/tareas/HU-XXX.md` y el informe del ejecutor (sección 11).
3. Obtener primero el resumen: `git diff develop...feature/HU-XXX --stat`.
4. Abrir el diff completo solo de los archivos que la ficha declara, más cualquier archivo inesperado del `--stat`.

## 1. Alcance contra la ficha
- [ ] Todo lo pedido está implementado y nada fuera de alcance fue tocado.
- [ ] Sin archivos inesperados (`AGENTS.md`, `CLAUDE.md`, `.github/`, `Directory.*.props`, `render.yaml`) salvo que la ficha lo pida.
- [ ] Sin paquetes NuGet nuevos no autorizados.

## 2. Arquitectura (bloqueante)
- [ ] Flujo Vista → Controller → BLL → DAL; ningún Controller (MVC o API) usa tipos del DAL.
- [ ] El DAL no contiene lógica de negocio ni reglas de autorización.
- [ ] La Vista no invoca la BLL; los datos viajan por DTO, nunca por Entity.
- [ ] Módulo completo: Entity, DTOs Request/Response, IRepository/Repository, IService/Service, API Controller, MVC Controller + Views, registro en IOC.
- [ ] Namespaces `Nilogistic.*`; sin nombres de otros proyectos.

## 3. Datos y reglas de negocio (bloqueante)
- [ ] Sin DELETE en código, SQL ni repositorios; el cambio es activo/inactivo.
- [ ] Migración SQL en `supabase/`, RLS deny-by-default y permisos de `nilogistic_app` sin DELETE.
- [ ] Autorización de negocio en la BLL, no solo en el Controller.
- [ ] Auditoría: quién, qué, cuándo, antes/después, IP.
- [ ] Notificaciones por Resend cuando la HU lo exige.

## 4. Seguridad (OWASP Top 10)
- [ ] Sin secretos, claves ni cadenas de conexión en el diff (revisar también ejemplos y documentos).
- [ ] Validación de entrada (FluentValidation) y salida codificada; HTML enriquecido pasa por HtmlSanitizer.
- [ ] Consultas parametrizadas; sin concatenar SQL.
- [ ] CSRF (antiforgery) y anti-bot (Turnstile) en formularios públicos; rate limiting donde aplique.
- [ ] Control de acceso por rol verificado en cada endpoint nuevo; sin exposición de datos de otros usuarios (IDOR).
- [ ] Sin registrar datos sensibles en logs.

## 5. Pruebas
- [ ] Cada criterio de aceptación tiene al menos una prueba (xUnit + Moq + FluentAssertions).
- [ ] Casos borde de la ficha cubiertos (permisos, vacíos, duplicados, entradas maliciosas).
- [ ] Las pruebas verifican comportamiento, no solo que el código "no falle".
- [ ] Pruebas de arquitectura en verde sin excepciones agregadas para hacerlas pasar.

## 6. Calidad
- [ ] Clean Code/SOLID; sin código muerto ni duplicado evidente.
- [ ] Sin pseudo-código, TODO sin ID de deuda ni APIs inventadas.
- [ ] Comentarios solo donde aportan.
- [ ] Si hay UI: etiquetas, foco, contraste y responsive (WCAG 2.2 AA).

## 7. Git y estado
- [ ] Commits con Conventional Commits y mensajes coherentes con la HU.
- [ ] Rama `feature/HU-XXX`; PR hacia `develop`.
- [ ] `ESTADO_PROYECTO.md` actualizado (HU y CA marcados, "HU actual" movida, deuda con ID y fecha).

## 8. Veredicto
Formato de respuesta de Claude:

**Veredicto:** ✅ Aprobado · 🔁 Cambios requeridos · ⛔ Rechazado

| # | Severidad | Archivo:línea | Hallazgo | Corrección pedida |
|---|---|---|---|---|
| 1 | Bloqueante / Importante / Menor | | | |

Cierre: ✅ hecho · ⏭ siguiente paso · ⚠ decisiones que requieren aprobación.

## Economía de límite de uso
- Usar Sonnet para revisiones rutinarias; reservar Opus para decisiones de arquitectura.
- `/clear` entre HU. No releer documentos de fase completos: citar sección y ID.
- Si el diff supera ~800 líneas, pedir que se divida la rama antes de revisar.
