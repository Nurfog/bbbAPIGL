# bbbAPIGL

API para la gestión de salas de BigBlueButton y la integración con Google Calendar.

## Descripción

Esta API proporciona endpoints para crear y eliminar salas de videoconferencia en BigBlueButton, así como para enviar invitaciones a cursos a través de Google Calendar. También permite obtener las URLs de las grabaciones de los cursos.

## Cómo Empezar

### Prerrequisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Conexión a una base de datos MySQL.
- Conexión a una base de datos PostgreSQL.
- Credenciales de Google Cloud con acceso a la API de Google Calendar y Gmail.

### Instalación

1. Clona el repositorio:
   ```sh
   git clone <URL_DEL_REPOSITORIO>
   cd bbbAPIGL
   ```

2. Configura las conexiones a la base de datos y las credenciales de Google en el archivo `appsettings.json`.

3. Restaura las dependencias de .NET:
   ```sh
   dotnet restore
   ```

### Cómo Ejecutar la Aplicación

Para ejecutar la aplicación en modo de desarrollo, utiliza el siguiente comando:

```sh
dotnet run
```

La API estará disponible en la URL especificada en `Properties/launchSettings.json` (por ejemplo, `http://localhost:5000`).

## Endpoints de la API

A continuación se describen los endpoints disponibles en esta API:

### Salas

- **`POST /salas`**
  - **Descripción:** Crea una nueva sala de BigBlueButton.
  - **Request Body:**
    ```json
    {
      "nombre": "Nombre de la Sala",
      "emailCreador": "creador@example.com",
      "correosParticipantes": ["participante1@example.com", "participante2@example.com"]
    }
    ```
  - **Respuesta Exitosa (201 Created):**
    ```json
    {
      "roomId": "...",
      "urlSala": "...",
      "claveModerador": "...",
      "claveEspectador": "...",
      "meetingId": "...",
      "friendlyId": "...",
      "recordId": "..."
    }
    ```

- **`DELETE /salas/{roomId}`**
  - **Descripción:** Elimina una sala de BigBlueButton por su ID.
  - **Parámetros de URL:**
    - `roomId` (GUID): El ID de la sala a eliminar.
  - **Respuesta Exitosa:** `204 No Content`

### Invitaciones

- **`POST /invitaciones`**
  - **Descripción:** Envía invitaciones de calendario de Google para un curso específico.
  - **Request Body:**
    ```json
    {
      "idCursoAbierto": 123
    }
    ```
  - **Respuesta Exitosa (200 OK):**
    ```json
    {
      "mensaje": "Invitaciones enviadas exitosamente.",
      "correosEnviados": 10
    }
    ```

### Grabaciones

- **`GET /grabaciones/{idCursoAbierto}`**
  - **Descripción:** Obtiene las URLs de las grabaciones para un curso abierto específico.
  - **Parámetros de URL:**
    - `idCursoAbierto` (int): El ID del curso abierto.
  - **Respuesta Exitosa (200 OK):**
    ```json
    [
      {
        "recordId": "...",
        "createdAt": "...",
        "playbackUrl": "..."
      }
    ]
    ```
