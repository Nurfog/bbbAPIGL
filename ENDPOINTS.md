# Documentación de Endpoints - bbbAPIGL API

API para la gestión de salas de reuniones virtuales (BigBlueButton), envío de invitaciones y acceso a grabaciones.

**Versión:** 2.2
**Base URL:** `https://tudominio.com/apiv2`
**Documentación Interactiva:** Disponible en `/api-docs` (Scalar)

## Configuración de Producción

### Archivos de Configuración

**IMPORTANTE:** El archivo `appsettings.json` **NO se sube al repositorio** (está en `.gitignore`). Debe crearse/editarse directamente en el servidor.

#### Estructura del `appsettings.json` en producción:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "PostgresDb": "Host=127.0.0.1;Port=5433;Username=postgres;Password=GLv3-Prod-2025-Pass.R3D1S-PGs;Database=greenlight-v3-production",
    "MySqlDb": "server=172.31.23.134;user=root;password=TU_CONTRASENA;database=sige_sam_v3;SslMode=None",
    "MySqlDbEmpresa": "server=172.31.23.134;user=root;password=TU_CONTRASENA;database=sige_sam_empresa;SslMode=None"
  },
  "SalaSettings": {
    "PublicUrl": "https://bbb.norteamericano.com",
    "DefaultRoomCreatorEmail": "norteamericanoonline@norteamericano.cl",
    "DefaultRoomCreatorEmailEmpresa": "sedeempresa@norteamericano.cl"
  },
  "BigBlueButtonApi": {
    "BaseUrl": "https://bbb.norteamericano.com/bigbluebutton/api",
    "Secret": "TU_SECRETO_BBB"
  },
  "GoogleCalendarSettings": {
    "CredentialsFile": "google-credentials.json",
    "UserToImpersonate": "norteamericanoonline@norteamericano.cl",
    "DefaultTimeZone": "America/Santiago",
    "CalendarId": "primary"
  },
  "S3Settings": {
    "BucketName": "bbbgl",
    "Region": "us-east-2"
  }
}
```

### Requisitos Previos

1. **Usuario en PostgreSQL (Greenlight):**
   ```sql
   -- El usuario sedeempresa@norteamericano.cl debe existir en PostgreSQL
   SELECT id, name, email FROM users WHERE email = 'sedeempresa@norteamericano.cl';
   ```

2. **Firewall de Windows Server (MySQL):**
   ```powershell
   # En el Windows Server donde está MySQL
   New-NetFirewallRule -DisplayName "MySQL 3306" -Direction Inbound -Protocol TCP -LocalPort 3306 -Action Allow -RemoteAddress 172.31.0.0/16
   ```

3. **Security Group de AWS:**
   - Puerto 3306 abierto desde el VPC (`172.31.0.0/16`)

### Despliegue

El script `publish.sh` ahora:
- ✅ Hace backup del `appsettings.json` existente
- ✅ Restaura la configuración después del despliegue
- ✅ Elimina `appsettings.Production.json` para no sobrescribir credenciales

```bash
./publish.sh
```

---

## Tabla de Contenidos

1. [Módulo Central (`/apiv2`)](#1-módulo-central-apiv2)
2. [Módulo Empresa (`/apiv2/emp`)](#2-módulo-empresa-apiv2emp)
3. [Modelos de Datos](#3-modelos-de-datos)
4. [Códigos de Estado HTTP](#4-códigos-de-estado-http)

---

## 1. Módulo Central (`/apiv2`)

Este módulo incluye integración completa con Google Calendar e invitaciones por correo electrónico.

### 1.1 Crear Sala

Crea una nueva sala de reuniones virtual en BigBlueButton y la vincula automáticamente a un curso en MySQL.

**Endpoint:**
```http
POST /apiv2/salas
```

**Headers:**
```
Content-Type: application/json
```

**Request Body:**
```json
{
  "nombre": "Sala de Clases - Matemáticas",
  "emailCreador": "profesor@universidad.cl",
  "idCursoAbierto": 123
}
```

**Parámetros:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `nombre` | string | Sí | Nombre descriptivo de la sala |
| `emailCreador` | string | Sí | Correo del usuario que crea la sala (debe existir en PostgreSQL) |
| `idCursoAbierto` | integer | Sí | ID del curso en la base de datos `sige_sam_v3` |

**Respuesta Exitosa (201 Created):**
```json
{
  "roomId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
  "nombreSala": "Sala de Clases - Matemáticas",
  "urlSala": "https://bbb.universidad.cl/rooms/abc-123-def-456/join",
  "claveModerador": "g3h4j5k6",
  "claveEspectador": "a1b2c3d4",
  "meetingId": "a1b2c3d4e5f6789012345678901234567890abcd",
  "friendlyId": "abc-123-def-456",
  "recordId": "a1b2c3d4e5f6789012345678901234567890abcd-1710604800"
}
```

**Campos de Respuesta:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `roomId` | guid | ID único de la sala en Greenlight (PostgreSQL) |
| `nombreSala` | string | Nombre de la sala |
| `urlSala` | string | URL pública para unirse a la sala |
| `claveModerador` | string | Contraseña para ingresar como moderador |
| `claveEspectador` | string | Contraseña para ingresar como espectador |
| `meetingId` | string | ID interno usado por BigBlueButton |
| `friendlyId` | string | ID amigable que forma parte de la URL |
| `recordId` | string | ID único para grabaciones de esta sesión |

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | El email del creador no existe en la base de datos o datos inválidos |
| `500 Internal Server Error` | Error interno del servidor |

---

### 1.2 Eliminar Sala

Elimina permanentemente una sala y todas sus configuraciones asociadas de la base de datos PostgreSQL.

**Endpoint:**
```http
DELETE /apiv2/salas/{roomId}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `roomId` | guid | Sí | ID único de la sala a eliminar |

**Ejemplo:**
```http
DELETE /apiv2/salas/a1b2c3d4-e5f6-7890-1234-567890abcdef
```

**Respuesta Exitosa (204 No Content):**
```
Sin cuerpo de respuesta
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `404 Not Found` | No se encontró la sala con el `roomId` proporcionado |
| `500 Internal Server Error` | Error de base de datos durante la eliminación |

---

### 1.3 Obtener Estado de Sala

Obtiene un diagnóstico integral del estado de una sala, verificando su existencia en SAM (MySQL), Greenlight (PostgreSQL) y si está activa en BigBlueButton.

**Endpoint:**
```http
GET /apiv2/salas/{idCursoAbierto}/status
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso abierto |

**Ejemplo:**
```http
GET /apiv2/salas/123/status
```

**Respuesta Exitosa (200 OK):**
```json
{
  "existeEnSam": true,
  "existeEnGreenlight": true,
  "estaActivaEnBBB": false,
  "urlSala": "https://bbb.universidad.cl/rooms/abc-123-def-456/join"
}
```

**Campos de Respuesta:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `existeEnSam` | boolean | Indica si la sala existe en la base de datos MySQL |
| `existeEnGreenlight` | boolean | Indica si la sala existe en PostgreSQL (Greenlight) |
| `estaActivaEnBBB` | boolean | Indica si hay una reunión activa en BigBlueButton |
| `urlSala` | string | URL de acceso a la sala |

---

### 1.4 Enviar Invitaciones a Curso (Masivo)

Envía invitaciones por correo electrónico a todos los estudiantes de un curso específico. Incluye integración con Google Calendar.

**Endpoint:**
```http
POST /apiv2/invitaciones/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso al que se enviarán las invitaciones |

**Request Body (Opcional):**
```json
{
  "emailCreador": "profesor@universidad.cl"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "mensaje": "Invitaciones enviadas correctamente.",
  "emailsEnviados": 42
}
```

**Notas:**
- Si falla Google Calendar, los correos se envían de todas formas
- El `emailCreador` es opcional; si no se proporciona, usa el configurado por defecto

**Errores:**

| Código | Descripción |
|--------|-------------|
| `404 Not Found` | El curso o la sala asociada no fue encontrada |
| `500 Internal Server Error` | Error interno del servidor |

---

### 1.5 Enviar Invitación Individual

Envía una invitación personalizada a un alumno específico de un curso.

**Endpoint:**
```http
POST /apiv2/invitaciones/individual/{idAlumno}/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idAlumno` | string | Sí | ID del alumno en la base de datos |
| `idCursoAbierto` | integer | Sí | ID del curso |

**Ejemplo:**
```http
POST /apiv2/invitaciones/individual/ALU001/123
```

**Respuesta Exitosa (200 OK):**
```json
{
  "mensaje": "Invitación enviada correctamente.",
  "emailsEnviados": 1
}
```

**Notas:**
- Si el curso ya tiene un evento de calendario, el alumno se agrega al evento existente
- Si no existe evento, se crea uno nuevo y se guarda el ID para futuras invitaciones

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Solicitud inválida |
| `500 Internal Server Error` | Error interno del servidor |

---

### 1.6 Actualizar Invitaciones de Curso

Actualiza el evento de calendario y reenvía invitaciones para un curso.

**Endpoint:**
```http
PUT /apiv2/invitaciones/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso |

**Request Body:**
```json
{
  "fechaInicio": "2026-03-15",
  "fechaTermino": "2026-06-15",
  "dias": ["Lunes", "Miércoles"],
  "horaInicio": "18:00",
  "horaTermino": "20:00"
}
```

**Parámetros del Body:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `fechaInicio` | date | Sí | Fecha de inicio del curso (YYYY-MM-DD) |
| `fechaTermino` | date | Sí | Fecha de término del curso (YYYY-MM-DD) |
| `dias` | array[string] | Sí | Días de la semana (ej: "Lunes", "Martes") |
| `horaInicio` | string | Sí | Hora de inicio (HH:MM) |
| `horaTermino` | string | Sí | Hora de término (HH:MM) |

**Respuesta Exitosa (200 OK):**
```json
{
  "mensaje": "Invitaciones actualizadas correctamente.",
  "emailsEnviados": 10
}
```

---

### 1.7 Obtener Grabaciones del Curso

Obtiene una lista de todas las grabaciones disponibles para un curso específico, incluyendo URL de reproducción y fecha de creación.

**Endpoint:**
```http
GET /apiv2/grabaciones/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso |

**Ejemplo:**
```http
GET /apiv2/grabaciones/123
```

**Respuesta Exitosa (200 OK):**
```json
[
  {
    "recordId": "0cf9da8040fa52677185fdd34e4b02faa7326af6-1756918398921",
    "createdAt": "2026-03-10",
    "playbackUrl": "https://bbb.universidad.cl/playback/presentation/2.3/0cf9da8040fa52677185fdd34e4b02faa7326af6-1756918398921"
  },
  {
    "recordId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0-1756910000000",
    "createdAt": "2026-03-08",
    "playbackUrl": "https://bbb.universidad.cl/playback/presentation/2.3/a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0-1756910000000"
  }
]
```

**Campos de Respuesta:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `recordId` | string | ID único de la grabación |
| `createdAt` | string | Fecha de creación (YYYY-MM-DD) |
| `playbackUrl` | string | URL completa para ver la grabación |

**Notas:**
- El arreglo está ordenado de más reciente a más antiguo
- Si no hay grabaciones, retorna un arreglo vacío `[]`

---

### 1.8 Reprogramar Sesión

Reprograma una sesión específica de un curso, actualizando su fecha en el calendario.

**Endpoint:**
```http
POST /apiv2/reprogramar-sesion
```

**Request Body:**
```json
{
  "idCursoAbierto": 123,
  "sesionNumero": 5,
  "fechaOriginalSesion": "2026-03-15",
  "fechaNuevaSesion": "2026-03-22"
}
```

**Parámetros del Body:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso |
| `sesionNumero` | integer | Sí | Número de sesión a reprogramar |
| `fechaOriginalSesion` | date | Sí | Fecha original de la sesión |
| `fechaNuevaSesion` | date | Sí | Nueva fecha para la sesión |

**Respuesta Exitosa (200 OK):**
```
Sin cuerpo de respuesta (o mensaje de confirmación)
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Datos inválidos o sesión no encontrada |
| `500 Internal Server Error` | Error interno del servidor |

---

### 1.9 Eliminar Curso

Elimina un curso abierto y todas sus invitaciones asociadas en el calendario.

**Endpoint:**
```http
DELETE /apiv2/cursos/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso a eliminar |

**Respuesta Exitosa (204 No Content):**
```
Sin cuerpo de respuesta
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `404 Not Found` | El curso no fue encontrado |
| `500 Internal Server Error` | Error de base de datos |

---

## 2. Módulo Empresa (`/apiv2/emp`)

Versión simplificada para la base de datos `sige_sam_empresa`. **No incluye integración con Google Calendar.**

### 2.1 Crear Sala (Empresa)

Crea una nueva sala para el módulo de empresas.

**Endpoint:**
```http
POST /apiv2/emp/salas
```

**Request Body:**
```json
{
  "nombre": "Sala Empresa - Ventas",
  "emailCreador": "admin@empresa.cl",
  "idCursoAbierto": 456
}
```

> **Nota:** El campo `emailCreador` es **opcional**. Si no se proporciona, se usa el configurado en `SalaSettings:DefaultRoomCreatorEmailEmpresa` (por defecto: `sedeempresa@norteamericano.cl`).

**Respuesta Exitosa (201 Created):**
```json
{
  "roomId": "b2c3d4e5-f6a7-8901-2345-678901bcdefg",
  "nombreSala": "Sala Empresa - Ventas",
  "urlSala": "https://bbb.empresa.cl/rooms/def-456-ghi-789/join",
  "claveModerador": "x9y8z7w6",
  "claveEspectador": "v5u4t3s2",
  "meetingId": "b2c3d4e5f6a7890123456789012345678901bcde",
  "friendlyId": "def-456-ghi-789",
  "recordId": "b2c3d4e5f6a7890123456789012345678901bcde-1710604800"
}
```

**Notas:**
- Si ya existe una sala para el `idCursoAbierto`, retorna los datos existentes sin crear duplicados
- **Requisito:** El usuario `emailCreador` (o el configurado por defecto) debe existir en la base de datos PostgreSQL (Greenlight)

**Ejemplos con curl:**

```bash
# Sin emailCreador (usa el por defecto: sedeempresa@norteamericano.cl)
curl -k -X POST https://tudominio.com/apiv2/emp/salas \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Sala Prueba",
    "idCursoAbierto": 6180
  }'

# Con emailCreador explícito
curl -k -X POST https://tudominio.com/apiv2/emp/salas \
  -H "Content-Type: application/json" \
  -d '{
    "nombre": "Sala Prueba",
    "emailCreador": "sedeempresa@norteamericano.cl",
    "idCursoAbierto": 6180
  }'
```

**Posibles errores:**

| Código | Mensaje | Causa |
|--------|---------|-------|
| `400 Bad Request` | `{"error":"Ocurrió un error interno en el servidor."}` | El usuario `emailCreador` no existe en PostgreSQL |
| `400 Bad Request` | `{"error":"Ocurrió un error interno en el servidor."}` | Error de conexión a MySQL (verificar configuración) |
| `500 Internal Server Error` | `{"error":"Ocurrió un error interno en el servidor."}` | Error inesperado del servidor |

---

### 2.2 Eliminar Sala (Empresa)

Elimina una sala del módulo empresa.

**Endpoint:**
```http
DELETE /apiv2/emp/salas/{roomId}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `roomId` | guid | Sí | ID de la sala a eliminar |

**Respuesta Exitosa (204 No Content):**
```
Sin cuerpo de respuesta
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `404 Not Found` | Sala no encontrada |
| `500 Internal Server Error` | Error interno |

---

### 2.3 Obtener Estado de Sala (Empresa)

Verifica el estado de una sala en el módulo empresa.

**Endpoint:**
```http
GET /apiv2/emp/salas/{idCursoAbierto}/status
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso empresa |

**Respuesta Exitosa (200 OK):**
```json
{
  "existeEnSam": true,
  "existeEnGreenlight": true,
  "estaActivaEnBBB": false,
  "urlSala": "https://bbb.empresa.cl/rooms/def-456-ghi-789/join"
}
```

---

### 2.4 Obtener Grabaciones (Empresa)

Recupera las grabaciones del módulo empresa.

**Endpoint:**
```http
GET /apiv2/emp/grabaciones/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso empresa |

**Respuesta Exitosa (200 OK):**
```json
[
  {
    "recordId": "rec123-1710604800",
    "createdAt": "2026-03-10",
    "playbackUrl": "https://bbb.empresa.cl/playback/presentation/2.3/rec123-1710604800"
  }
]
```

---

### 2.5 Crear Invitación/Sesión (Empresa)

Crea un registro de invitación/sesión en la base de datos de empresa.

**Endpoint:**
```http
POST /apiv2/emp/invitaciones/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso empresa |

**Request Body:**
```json
{
  "id": 12345,
  "fecha": "2026-03-15"
}
```

**Parámetros del Body:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `id` | integer | Sí | Identificador de la sesión |
| `fecha` | date | Sí | Fecha de la sesión (YYYY-MM-DD) |

**Respuesta Exitosa (200 OK):**
```json
{
  "mensaje": "Invitación registrada."
}
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | No se pudo crear la invitación |
| `500 Internal Server Error` | Error interno |

---

### 2.6 Modificar Invitación (Empresa)

Reprograma una invitación existente marcando la anterior como suspendida.

**Endpoint:**
```http
PUT /apiv2/emp/invitaciones/{id}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `id` | integer | Sí | ID de la invitación a modificar |

**Request Body:**
```json
{
  "fechaNuevaSesion": "2026-03-22"
}
```

**Respuesta Exitosa (200 OK):**
```json
{
  "mensaje": "Invitación modificada."
}
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | No se encontró invitación activa |
| `500 Internal Server Error` | Error interno |

---

### 2.7 Operaciones por Lote (Empresa)

Ejecuta múltiples operaciones (crear, editar, eliminar) en un solo request.

**Endpoint:**
```http
POST /apiv2/emp/invitaciones/batch
```

**Request Body:**
```json
[
  {
    "accion": "crear",
    "id": 555,
    "fecha": "2026-04-01"
  },
  {
    "accion": "editar",
    "id": 123,
    "fechaNueva": "2026-04-05"
  },
  {
    "accion": "eliminar",
    "id": 999
  }
]
```

**Estructura de Operaciones:**

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `accion` | string | Sí | Tipo de operación: `crear`, `editar`, `eliminar` |
| `id` | integer | Sí | ID de la sesión |
| `fecha` | date | No | Fecha para operación `crear` |
| `fechaNueva` | date | No | Fecha para operación `editar` |

**Respuesta Exitosa (200 OK):**
```json
{
  "mensaje": "Operaciones procesadas."
}
```

**Notas:**
- Retorna éxito si al menos una operación fue procesada correctamente

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | Ninguna operación tuvo efecto o datos inválidos |
| `500 Internal Server Error` | Error interno |

---

### 2.8 Reprogramar Sesión (Empresa)

Reprogramación general de sesiones para empresas.

**Endpoint:**
```http
POST /apiv2/emp/reprogramar-sesion
```

**Request Body:**
```json
{
  "idCursoAbierto": 456,
  "sesionNumero": 3,
  "fechaNuevaSesion": "2026-03-22"
}
```

**Respuesta Exitosa (200 OK):**
```
Sin cuerpo de respuesta
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `400 Bad Request` | No se pudo reprogramar la sesión |
| `500 Internal Server Error` | Error interno |

---

### 2.9 Eliminar Curso (Empresa)

Elimina los datos de un curso de empresa.

**Endpoint:**
```http
DELETE /apiv2/emp/cursos/{idCursoAbierto}
```

**Parámetros de Ruta:**

| Parámetro | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso empresa |

**Respuesta Exitosa (204 No Content):**
```
Sin cuerpo de respuesta
```

**Errores:**

| Código | Descripción |
|--------|-------------|
| `404 Not Found` | Curso no encontrado |
| `500 Internal Server Error` | Error interno |

---

## 3. Modelos de Datos

### CrearSalaRequest
```json
{
  "nombre": "string",
  "emailCreador": "string (email, opcional)",
  "idCursoAbierto": "integer"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `nombre` | string | Sí | Nombre descriptivo de la sala |
| `emailCreador` | string | No | Email del creador (usa el por defecto si no se especifica) |
| `idCursoAbierto` | integer | Sí | ID del curso en la base de datos |

### CrearSalaResponse
```json
{
  "roomId": "guid",
  "nombreSala": "string",
  "urlSala": "string (url)",
  "claveModerador": "string",
  "claveEspectador": "string",
  "meetingId": "string",
  "friendlyId": "string",
  "recordId": "string"
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `roomId` | guid | ID único de la sala en Greenlight (PostgreSQL) |
| `nombreSala` | string | Nombre de la sala |
| `urlSala` | string | URL pública para unirse a la sala |
| `claveModerador` | string | Contraseña para ingresar como moderador |
| `claveEspectador` | string | Contraseña para ingresar como espectador |
| `meetingId` | string | ID interno usado por BigBlueButton |
| `friendlyId` | string | ID amigable que forma parte de la URL |
| `recordId` | string | ID único para grabaciones de esta sesión |

### GrabacionDto
```json
{
  "recordId": "string",
  "createdAt": "string (date, YYYY-MM-DD)",
  "playbackUrl": "string (url)"
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `recordId` | string | ID único de la grabación |
| `createdAt` | string | Fecha de creación (YYYY-MM-DD) |
| `playbackUrl` | string | URL completa para ver la grabación |

### SalaStatusDto
```json
{
  "existeEnSam": "boolean",
  "existeEnGreenlight": "boolean",
  "estaActivaEnBBB": "boolean",
  "urlSala": "string (url)"
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `existeEnSam` | boolean | Indica si la sala existe en MySQL (SAM) |
| `existeEnGreenlight` | boolean | Indica si la sala existe en PostgreSQL (Greenlight) |
| `estaActivaEnBBB` | boolean | Indica si hay una reunión activa en BigBlueButton |
| `urlSala` | string | URL de acceso a la sala |

### ReprogramarSesionRequest
```json
{
  "idCursoAbierto": "integer",
  "sesionNumero": "integer",
  "fechaOriginalSesion": "date (YYYY-MM-DD)",
  "fechaNuevaSesion": "date (YYYY-MM-DD)"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `idCursoAbierto` | integer | Sí | ID del curso |
| `sesionNumero` | integer | Sí | Número de sesión a reprogramar |
| `fechaOriginalSesion` | date | Sí | Fecha original de la sesión |
| `fechaNuevaSesion` | date | Sí | Nueva fecha para la sesión |

### CrearInvitacionEmpresaRequest
```json
{
  "id": "integer",
  "fecha": "date (YYYY-MM-DD)"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `id` | integer | Sí | Identificador de la sesión en `sesionescursos` |
| `fecha` | date | Sí | Fecha de la sesión (YYYY-MM-DD) |

### OperacionInvitacionEmpresaRequest
```json
{
  "accion": "string (crear|editar|eliminar)",
  "id": "integer",
  "fecha": "date (YYYY-MM-DD, opcional)",
  "fechaNueva": "date (YYYY-MM-DD, opcional)"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `accion` | string | Sí | Tipo de operación: `crear`, `editar`, `eliminar` |
| `id` | integer | Sí | Identificador de la sesión |
| `fecha` | date | No | Fecha de la sesión (solo para `crear`) |
| `fechaNueva` | date | No | Nueva fecha (solo para `editar`) |

### ActualizarEventoCalendarioRequest
```json
{
  "fechaInicio": "date (YYYY-MM-DD)",
  "fechaTermino": "date (YYYY-MM-DD)",
  "dias": ["string"],
  "horaInicio": "string (HH:MM)",
  "horaTermino": "string (HH:MM)"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `fechaInicio` | date | Sí | Fecha de inicio del curso |
| `fechaTermino` | date | Sí | Fecha de término del curso |
| `dias` | string[] | Sí | Días de la semana (ej: ["Lunes", "Miércoles"]) |
| `horaInicio` | string | Sí | Hora de inicio (HH:MM) |
| `horaTermino` | string | Sí | Hora de término (HH:MM) |

### EnviarInvitacionCursoRequest
```json
{
  "emailCreador": "string (email, opcional)"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `emailCreador` | string | No | Email del creador (usa el por defecto si no se especifica) |

### EnviarInvitacionIndividualRequest
```json
{
  "idAlumno": "string",
  "idCursoAbierto": "integer",
  "emailCreador": "string (email, opcional)"
}
```

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `idAlumno` | string | Sí | ID del alumno en la base de datos |
| `idCursoAbierto` | integer | Sí | ID del curso |
| `emailCreador` | string | No | Email del creador (opcional) |

---

## 4. Códigos de Estado HTTP

| Código | Estado | Descripción |
|--------|--------|-------------|
| `200` | OK | Solicitud exitosa |
| `201` | Created | Recurso creado exitosamente |
| `204` | No Content | Operación exitosa, sin contenido en la respuesta |
| `400` | Bad Request | Solicitud inválida o mal formada |
| `404` | Not Found | Recurso no encontrado |
| `500` | Internal Server Error | Error interno del servidor |

---

## Documentación Interactiva

La API incluye documentación interactiva generada automáticamente usando **Scalar**, disponible en:

- **Desarrollo:** `https://localhost:7000/api-docs`
- **Producción:** `https://bbb.norteamericano.cl/api-docs`

### Características de Scalar

Scalar ofrece una experiencia moderna de documentación API con:

- ✅ Lista completa de todos los endpoints (Central y Empresa)
- ✅ Pruebas interactivas directamente desde el navegador
- ✅ Modelos de datos detallados con descripciones
- ✅ Ejemplos de requests y responses
- ✅ UI responsiva y moderna
- ✅ Búsqueda rápida de endpoints
- ✅ Soporte para temas claro/oscuro

---

## Soporte

Para consultas o problemas con la API, contactar a:
- **Email:** soporte@universidad.cl
- **Documentación Interactiva:** `https://tudominio.com/api-docs`

---

## Historial de Versiones

### Versión 2.2 (16-03-2026)

- **Nuevos Endpoints de Invitaciones (Módulo Empresa)**:
  - `POST /apiv2/emp/invitaciones/{idCursoAbierto}`: Registra una nueva sesión en la tabla `sesionescursos`.
  - `PUT /apiv2/emp/invitaciones/{id}`: Modifica (reprograma) una invitación existente, marcando la anterior como suspendida.
  - `POST /apiv2/emp/invitaciones/batch`: Ejecuta operaciones masivas (crear, editar, eliminar) en un solo request.
- **Documentación Interactiva con Scalar**: Se reemplazó Swagger por Scalar como sistema de documentación interactiva.
- **Mejora en Manejo de Errores**: Se refinó el manejo de excepciones para distinguir entre errores de aplicación, validación y errores inesperados.

### Versión 2.1 (10-03-2026)

- **Robustecimiento de Invitaciones (Módulo Central)**: Lógica de manejo de errores mejorada para Google Calendar.
- **Corrección de Despliegue**: Ajuste del `TargetFramework` a `net9.0`.

### Versión 2.0 (25-02-2026)

- **Nuevo Módulo Empresa (`/apiv2/emp`)**: Implementación completa para la base de datos `sige_sam_empresa`.
