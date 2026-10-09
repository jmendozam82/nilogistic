# Ficha de tarea — HU-XXX · <título>

> Escrita por Claude para ser ejecutada por un modelo de OpenCode. **No improvises:** si algo es ambiguo o contradictorio, detente, anótalo en "Dudas" y no inventes APIs, paquetes ni nombres.

## 1. Cabecera
| Campo | Valor |
|---|---|
| HU | HU-XXX |
| Sprint / Release | S? / R? |
| Rama | `feature/HU-XXX` (desde `develop`) |
| Dependencias | HU-… (deben estar mergeadas) |
| Tamaño | S / M / L (una ficha = una capa o un módulo pequeño) |
| Modelo asignado | <Big Pickle / GLM / LongCat / otro> |

## 2. Objetivo
Una o dos frases: qué debe poder hacer el usuario al terminar.

## 3. Lee solo esto
- `ESTADO_PROYECTO.md`
- `AGENTS.md` §7 (arquitectura) y §8 (buenas prácticas)
- Criterios de la HU en `docs fase/…` (sección exacta)
- Archivos de ejemplo del repo a imitar: `src/…`

## 4. Alcance por capa (de abajo hacia arriba)
| Orden | Proyecto | Ruta exacta | Acción (crear/modificar) | Notas |
|---|---|---|---|---|
| 1 | Nilogistic.Entity | `src/Nilogistic.Entity/…` | | |
| 2 | Nilogistic.DTO | `src/Nilogistic.DTO/…` | | Request y Response |
| 3 | Nilogistic.DAL | `src/Nilogistic.DAL/…` | | IRepository + Repository |
| 4 | Nilogistic.BLL | `src/Nilogistic.BLL/…` | | Autorización y auditoría aquí |
| 5 | Nilogistic.API | `src/Nilogistic.API/…` | | `/api/v1`, entra por la BLL |
| 6 | Nilogistic.Aplicacion | `src/Nilogistic.Aplicacion/…` | | Controller MVC + Views |
| 7 | Nilogistic.IOC | `src/Nilogistic.IOC/…` | | Registro de dependencias |
| 8 | supabase | `supabase/migrations/…` | | SQL, RLS deny-by-default |

## 5. Fuera de alcance
- Lista explícita de lo que **no** se toca (otros módulos, estilos, refactors, paquetes nuevos).

## 6. Prohibiciones (se revisan al pie de la letra)
- No llamar al DAL desde Controllers (MVC o API). No poner lógica de negocio en el DAL. No llamar a la BLL desde Vistas.
- No usar DELETE (SQL, EF ni repositorios): solo cambio de estado activo/inactivo.
- No agregar paquetes NuGet fuera de `Directory.Packages.props` ni sin que la ficha lo diga.
- No escribir secretos, claves ni cadenas de conexión en archivos versionados.
- No usar nombres de otros proyectos; todo namespace empieza con `Nilogistic`.
- No tocar `AGENTS.md`, `CLAUDE.md`, `.github/` ni `Directory.*.props` sin que la ficha lo pida.

## 7. Criterios de aceptación ↔ pruebas esperadas
| CA | Escenario (Gherkin resumido) | Prueba esperada (proyecto · clase · método) |
|---|---|---|
| CA-1 | Dado… cuando… entonces… | `tests/Nilogistic.BLL.Tests` · `…Tests` · `…` |

Casos borde obligatorios: <permisos por rol, datos vacíos, duplicados, entradas maliciosas, etc.>

## 8. Seguridad, permisos, auditoría y notificaciones
- Permisos por rol (READ / CREATE / UPDATE): …
- Evento de auditoría a registrar (quién, qué, cuándo, antes/después, IP): …
- Notificación (Resend), si aplica: …
- Validaciones (FluentValidation) y protección CSRF/anti-bot si hay formulario público: …

## 9. Verificación antes de entregar
```
dotnet build Nilogistic.slnx
dotnet test
```
Ambos deben terminar en verde. Pega en el PR la salida resumida (total de pruebas, fallidas, omitidas).

## 10. Entrega
1. Commits con Conventional Commits (`feat(HU-XXX): …`, `test(HU-XXX): …`). Sin push a `develop` ni `main`.
2. Actualiza `ESTADO_PROYECTO.md` según el paso 8 del ciclo (AGENTS.md §12).
3. Completa la sección 11 y avisa a Jorge para pedir revisión a Claude.

## 11. Informe del ejecutor (lo llena OpenCode)
- Archivos creados/modificados:
- Resultado de `dotnet build` / `dotnet test`:
- Dudas o supuestos asumidos:
- Deuda técnica detectada:
