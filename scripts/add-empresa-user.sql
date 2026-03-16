-- Script para verificar/crear usuario sedeempresa en PostgreSQL (Greenlight v3)
-- Ejecutar: psql -h <host> -p 5433 -U greenlight -d greenlight_v3 -f add-empresa-user.sql

-- 1. Verificar si el usuario ya existe
SELECT id, name, email, provider 
FROM users 
WHERE email = 'sedeempresa@norteamericano.cl';

-- 2. Si no existe, crear el usuario
-- Descomentar la siguiente línea si el usuario no existe:

/*
INSERT INTO users (id, name, email, provider, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'Sede Empresa',
    'sedeempresa@norteamericano.cl',
    'default',
    NOW(),
    NOW()
);
*/

-- 3. Verificar creación
SELECT id, name, email, provider, created_at 
FROM users 
WHERE email = 'sedeempresa@norteamericano.cl';
