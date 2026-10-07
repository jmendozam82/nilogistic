-- HU-004 / ADR-06: buckets de R1. Solo se ejecuta en Supabase (donde existe el esquema storage).
-- Los buckets de R2 y R3 (comprobantes, CVs, facturas, material, descargas) los crea la HU de su módulo.
-- RLS de storage.objects: Supabase la tiene habilitada y sin políticas = denegación para anon y
-- authenticated; la aplicación opera con la clave service_role, solo en el servidor (decisión #3).

do $$
begin
  if to_regclass('storage.buckets') is not null then
    insert into storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
    values
      ('editorial-publico', 'editorial-publico', true,  2097152, array['image/jpeg', 'image/png', 'image/webp']),
      ('perfiles',          'perfiles',          false, 1048576, array['image/jpeg', 'image/png', 'image/webp'])
    on conflict (id) do update
      set public = excluded.public,
          file_size_limit = excluded.file_size_limit,
          allowed_mime_types = excluded.allowed_mime_types;
  end if;
end
$$;
