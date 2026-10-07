# Nilogistic

Sitio corporativo y comunidad logística (nilogistic.com). .NET 10, N-Capas + MVC + Repositorio.

## Requisitos
- .NET SDK 10 (ver `global.json`), Docker (Testcontainers y PostgreSQL local), Supabase CLI, Python 3.11+.

## Arranque local
```bash
docker compose up -d postgres
cd src/Nilogistic.Aplicacion
dotnet user-secrets set "ConnectionStrings:Nilogistic" "Host=localhost;Database=nilogistic_dev;Username=nilogistic_app;Password=<tu-clave-local>"
dotnet run --launch-profile https
```
Las migraciones SQL (`supabase/migrations`) se aplican con Supabase CLI; EF Core no genera migraciones (ADR-03).

## Pruebas
```bash
dotnet test Nilogistic.slnx
python3 -m unittest discover -s scripts/tests
```
Cada prueba lleva `[Hu("HU-nnn","E#")]` (trazabilidad PU-HU-nnn-E#). BLL ≥ 70 % de cobertura.

## Reglas
- Vista → Controller → BLL → DAL → PostgreSQL. Las pruebas de arquitectura bloquean saltos de capa.
- Sin DELETE: solo soft delete. El rol `nilogistic_app` no tiene privilegio DELETE.
- Secretos fuera del repo (user-secrets, variables de Render). Ver `docs/ENTORNOS.md`.
- Conventional Commits; GitFlow ligero (`develop` → `main`).
