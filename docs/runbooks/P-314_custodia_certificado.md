# P-314 — Custodia del certificado X.509 de Data Protection (D-075)

| Elemento | Dónde vive | Dónde NO puede estar |
|---|---|---|
| PFX | Secret file de Render + copia cifrada en el gestor de contraseñas de Jorge | Repositorio, Supabase, junto a respaldos |
| Contraseña del PFX | Entrada **distinta** del gestor + sobre sellado para la persona de la tarea #13 | Repositorio, Supabase, junto al PFX, junto a respaldos |

- Vigencia 2 años; avisos a 60 y 30 días; rotación y prueba de restauración en S4 (HU-014 E6 a E10).
- Confirmación final antes del 04/01/2027 (S-612). El sobre se entrega cuando se nombre a la persona (18/01/2027).
- Rotación: generar PFX nuevo, añadir a Render, desplegar, verificar que las cookies siguen válidas, retirar el anterior.
