# Variables de entorno (ninguna con valor real en el repo)

| Variable | Dónde | Notas |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Render | `Staging` / `Production` |
| `ConnectionStrings__Nilogistic` | Render (secret) | Rol `nilogistic_app`, nunca el propietario |
| `Supabase__ReferenciaProyecto` | Render | Obligatoria en Staging/Production: la guardia impide usar la BD de otro entorno |
| `Seguridad__IndexacionHabilitada` | Render | Solo surte efecto en Production; la enciende el corte DNS (HU-031) |
| `Seguridad__Csp__Modo` | Render | `Reporte` en Staging mientras se afina; `Aplicar` por defecto |
| `Proxy__ConfiarEnCualquierProxy` | Render Staging | Solo durante el spike; en Production el arranque falla si está activa |
| `Proxy__Redes` | Render | CIDR de confianza (HU-013, S4) |
