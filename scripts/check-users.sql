-- Consultar usuarios registrados en PostgreSQL (Greenlight)
-- Ejecutar contra la base de datos Greenlight v3

-- 1. Listar todos los usuarios con su email y rol
SELECT 
    id,
    name,
    email,
    provider,
    created_at,
    updated_at
FROM users
ORDER BY created_at DESC;

-- 2. Contar total de usuarios
SELECT COUNT(*) as total_usuarios FROM users;

-- 3. Buscar usuarios por dominio (ej: norteamericano.cl)
SELECT 
    id,
    name,
    email,
    provider
FROM users
WHERE email LIKE '%@norteamericano.cl'
ORDER BY email;

-- 4. Verificar si existe un usuario específico (ej: sedeempresa)
SELECT 
    id,
    name,
    email,
    provider
FROM users
WHERE email = 'sedeempresa@norteamericano.cl';

-- 5. Usuarios administradores (si existe la columna role o similar)
SELECT 
    id,
    name,
    email
FROM users
WHERE email IN (
    'admin@norteamericano.cl',
    'norteamericanoonline@norteamericano.cl',
    'sedeempresa@norteamericano.cl'
);
