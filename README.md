# bbbAPIGL API

API para la gestión de salas de reuniones virtuales (BBB), envío de invitaciones y acceso a grabaciones.

## Descripción General

Esta API proporciona una interfaz robusta para interactuar con un sistema de BigBlueButton (BBB), facilitando la automatización de la gestión de salas de reuniones virtuales. Permite la creación y eliminación de salas, la gestión de invitaciones a través de correo electrónico con una profunda integración con Google Calendar para agendar eventos, y la recuperación segura de URLs de grabaciones almacenadas en un servicio compatible con S3.

El objetivo principal es simplificar la administración de entornos de aprendizaje o reunión en línea, ofreciendo funcionalidades clave para educadores y administradores de plataformas.

## Características

-   **Gestión de Salas BBB**: Creación y eliminación programática de salas de reuniones virtuales.
-   **Sistema de Invitaciones Inteligente**:
    -   Envío de invitaciones masivas para cursos completos.
    -   Envío de invitaciones individuales personalizadas.
    -   Integración con Google Calendar para la creación y eliminación automática de eventos de calendario, tanto para sesiones únicas como recurrentes.
-   **Acceso a Grabaciones**: Recuperación de enlaces pre-firmados a grabaciones de sesiones, garantizando un acceso seguro y temporal a los contenidos.
-   **Arquitectura Modular**: Diseño basado en principios de Clean Architecture y Dependency Injection para facilitar la mantenibilidad y escalabilidad.

## Configuración

Para poder ejecutar el proyecto, es necesario configurar las credenciales y ajustes de los servicios externos.

1.  **Configuración de la aplicación**: Renombre el archivo `appsettings.example.json` a `appsettings.json` y rellene los valores correspondientes a su base de datos (PostgreSQL y MySQL), S3 y BigBlueButton.
    *   `ConnectionStrings:PostgresDb`: Cadena de conexión para la base de datos PostgreSQL (usada por `SalaRepository`).
    *   `ConnectionStrings:MySqlDb`: Cadena de conexión para la base de datos MySQL (usada por `MySqlCursoRepository`).
    *   `S3Settings:BucketName`: Nombre del bucket S3 para grabaciones.
    *   `S3Settings:Region`: Región de AWS donde se encuentra el bucket S3.
    *   `SalaSettings:PublicUrl`: URL pública base para acceder a las salas y grabaciones de BBB.

2.  **Credenciales de Google**: Renombre `google-credentials.example.json` a `google-credentials.json` y añada las credenciales de su cuenta de servicio de Google Cloud para la integración con Google Calendar y Gmail. Asegúrese de que la cuenta de servicio tenga los permisos necesarios para gestionar eventos de calendario y enviar correos electrónicos.
    *   `GoogleCalendarSettings:CredentialsFile`: Ruta al archivo `google-credentials.json`.
    *   `GoogleCalendarSettings:UserToImpersonate`: Correo electrónico del usuario que será suplantado para crear eventos de calendario y enviar correos.
    *   `GoogleCalendarSettings:DefaultTimeZone`: Zona horaria por defecto para los eventos de calendario (ej. "America/Santiago").

## Documentación del Código

El proyecto sigue una arquitectura limpia y modular, organizada en las siguientes capas:

### 1. Controllers

Ubicados en la carpeta `Controllers`, son responsables de manejar las peticiones HTTP entrantes, invocar la lógica de negocio a través de los servicios y devolver las respuestas HTTP.

-   **`SalasController.cs`**: Expone los endpoints para la creación, eliminación, actualización de salas, envío de invitaciones (curso e individual) y obtención de URLs de grabaciones.

### 2. DTOs (Data Transfer Objects)

Ubicados en la carpeta `DTOs`, definen la estructura de los datos que se envían y reciben a través de la API.

-   `CrearSalaRequest`, `CrearSalaResponse`: Para la creación de salas.
-   `EliminarSalaRequest`: Para la eliminación de salas (aunque el endpoint usa un `Guid` directamente).
-   `EnviarInvitacionCursoRequest`, `EnviarInvitacionCursoResponse`: Para el envío de invitaciones a cursos.
-   `EnviarInvitacionIndividualRequest`: Para el envío de invitaciones individuales.
-   `GrabacionDto`: Para la información de grabaciones.

### 3. Models

Ubicados en la carpeta `Models`, representan las entidades de dominio del negocio.

-   **`Sala.cs`**: Representa una sala de reuniones virtual, incluyendo su `MeetingId`, `FriendlyId`, claves de acceso y el `IdCalendario` para la integración con Google Calendar.
-   **`CursoAbiertoSala.cs`**: Modelo que combina información de un curso abierto con detalles de sala BBB.
-   **`RecordingInfo.cs`**: Información básica de una grabación.

### 4. Services

Ubicados en la carpeta `Services`, contienen la lógica de negocio principal y orquestan las operaciones.

-   **`ISalaService` / `SalaService.cs`**: Implementa la lógica central para la gestión de salas, incluyendo la generación de IDs, claves, interacción con repositorios y servicios de correo/calendario.
-   **`IEmailService` / `GoogleCalendarService.cs`**: Abstracción e implementación para el envío de correos electrónicos y la gestión de eventos en Google Calendar (creación, actualización y eliminación). Utiliza la API de Google Calendar y Gmail.
-   **`IS3Service` / `S3Service.cs`**: Abstracción e implementación para interactuar con servicios de almacenamiento compatibles con S3, específicamente para generar URLs pre-firmadas para el acceso a grabaciones.

### 5. Repositories

Ubicados en la carpeta `Repositories`, son responsables de la abstracción de la capa de acceso a datos.

-   **`ISalaRepository` / `SalaRepository.cs`**: Proporciona métodos para persistir y recuperar datos de salas en una base de datos PostgreSQL. Incluye operaciones para guardar, eliminar y obtener salas, así como sus IDs de calendario.
-   **`ICursoRepository` / `MySqlCursoRepository.cs`**: Proporciona métodos para interactuar con una base de datos MySQL, obteniendo información de cursos, correos de alumnos y desasociando salas de cursos.

### 6. Program.cs

Configura la inyección de dependencias, registrando los servicios y repositorios con sus respectivas interfaces. También configura el pipeline de peticiones HTTP (Swagger, HTTPS redirection, etc.).

## Documentación de Uso

### Ejecución del Proyecto

1.  Asegúrese de haber configurado `appsettings.json` y `google-credentials.json` como se describe en la sección de Configuración.
2.  Abra una terminal en la raíz del proyecto.
3.  Ejecute el siguiente comando para construir y correr la aplicación:
    ```bash
    dotnet run
    ```
4.  La API estará disponible en `https://localhost:XXXX` (el puerto exacto se mostrará en la consola).

### Endpoints de la API

Puede acceder a la documentación interactiva de la API a través de Swagger UI en `https://localhost:XXXX/swagger` (reemplace `XXXX` con el puerto de su aplicación).

---


### Salas

#### `POST /salas`

Crea una nueva sala de reuniones virtual y, opcionalmente, envía invitaciones de calendario.

-   **Request Body**: `CrearSalaRequest`
    ```json
    {
        "nombre": "Nombre de la Sala de Clase",
        "emailCreador": "creador@example.com"
    }
    ```
-   **Success Response**: `201 Created`
    ```json
    {
        "roomId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
        "urlSala": "https://public.url/rooms/friendly-id/join",
        "claveModerador": "moderatorPassword",
        "claveEspectador": "attendeePassword",
        "meetingId": "guid-meeting-id",
        "friendlyId": "friendly-id",
        "recordId": "meeting-id-timestamp",
        "nombreSala": "Nombre de la Sala de Clase"
    }
    ```
-   **Error Response**: `400 Bad Request`, `500 Internal Server Error`

#### `DELETE /salas/{roomId}`

Elimina una sala de reuniones existente y su evento de calendario asociado (si existe).

-   **URL Parameters**:
    -   `roomId` (guid): El ID de la sala a eliminar.
-   **Success Response**: `204 No Content`
-   **Error Response**: `404 Not Found` (si la sala no existe), `500 Internal Server Error`

---


### Invitaciones

#### `POST /invitaciones`

Envía invitaciones por correo y crea un evento de calendario recurrente para todos los participantes de un curso.

-   **Request Body**: `EnviarInvitacionCursoRequest`
    ```json
    {
        "idCursoAbierto": 123
    }
    ```
-   **Success Response**: `200 OK`
    ```json
    {
        "mensaje": "Invitaciones enviadas exitosamente.",
        "correosEnviados": 15
    }
    ```
-   **Error Response**: `400 Bad Request` (si el curso no tiene horario definido o no se encuentran alumnos), `500 Internal Server Error`

#### `POST /invitaciones/individual`

Envía una invitación individual por correo y crea un evento de calendario recurrente para un alumno específico de un curso.

-   **Request Body**: `EnviarInvitacionIndividualRequest`
    ```json
    {
        "idAlumno": "ID_DEL_ALUMNO",
        "idCursoAbierto": 123
    }
    ```
-   **Success Response**: `200 OK`
    ```json
    {
        "mensaje": "Invitación enviada exitosamente.",
        "correosEnviados": 1
    }
    ```
-   **Error Response**: `400 Bad Request` (si el curso no tiene horario definido o el alumno no se encuentra), `500 Internal Server Error`

#### `PUT /invitaciones/{idCursoAbierto}`

Actualiza un evento de calendario existente para un curso. Permite modificar participantes, fechas y horarios.

-   **URL Parameters**:
    -   `idCursoAbierto` (int): El ID del curso abierto cuyo evento de calendario se desea actualizar.
-   **Request Body**: `ActualizarEventoCalendarioRequest`
    ```json
    {
        "correosParticipantes": [
            "nuevo_alumno1@example.com",
            "nuevo_alumno2@example.com"
        ],
        "fechaInicio": "2025-01-01T10:00:00Z",
        "fechaTermino": "2025-03-31T12:00:00Z",
        "diasSemana": "LU,MI,VI",
        "horaInicio": "10:00:00",
        "horaTermino": "11:30:00"
    }
    ```
-   **Success Response**: `200 OK`
    ```json
    {
        "mensaje": "Invitaciones actualizadas exitosamente.",
        "correosEnviados": 2
    }
    ```
-   **Error Response**: `400 Bad Request` (si el curso no tiene horario definido, no se encuentran alumnos o no hay evento de calendario asociado), `500 Internal Server Error`

---


### Grabaciones

#### `GET /grabaciones/{idCursoAbierto}`

Obtiene las URLs pre-firmadas de las grabaciones disponibles para un curso específico.

-   **URL Parameters**:
    -   `idCursoAbierto` (int): El ID del curso abierto.
-   **Success Response**: `200 OK`
    ```json
    [
        {
            "recordId": "record-id-ejemplo",
            "playbackUrl": "https://s3.bucket.url/presigned-url-to-recording.mp4",
            "createdAt": "2023-10-27"
        }
    ]
    ```
-   **Error Response**: `404 Not Found` (si el curso no existe o no tiene grabaciones asociadas)

## Estructura de Carpetas

```
bbbAPIGL\
├───.gitignore
├───appsettings.example.json
├───appsettings.Production.json
├───bbbAPIGL.csproj
├───bbbAPIGL.http
├───bbbAPIGL.sln
├───google-credentials.example.json
├───Program.cs
├───README.md
├───.git\...
├───.vscode\
├───bin\...
├───Controllers\
│   └───SalasController.cs
├───DTOs\
│   ├───ActualizarEventoCalendarioRequest.cs
│   ├───CrearSalaRequest.cs
│   ├───CrearSalaResponse.cs
│   ├───EliminarSalaRequest.cs
│   ├───EnviarInvitacionCursoRequest.cs
│   ├───EnviarInvitacionCursoResponse.cs
│   ├───EnviarInvitacionIndividualRequest.cs
│   └───GrabacionDto.cs
├───Models\
│   ├───CursoAbiertoSala.cs
│   ├───RecordingInfo.cs
│   └───Sala.cs
├───obj\...
├───Properties\
│   └───launchSettings.json
├───publish\...
├───Repositories\
│   ├───ICursoRepository.cs
│   ├───ISalaRepository.cs
│   ├───MySqlCursoRepository.cs
│   └───SalaRepository.cs
└───Services\
    ├───GoogleCalendarService.cs
    ├───IEmailService.cs
    ├───Is3Service.cs
    ├───ISalaService.cs
    ├───S3Service.cs
    └───SalaService.cs
```
