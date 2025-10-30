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

Documentación de la API para Integración con BigBlueButton
Versión: 2.1 URL Base: https://bbb.norteamericano.com/apiv2
Introducción
Esta API proporciona una interfaz para interactuar con la plataforma de conferencias web BigBlueButton (BBB) a través de su sistema de gestión Greenlight. Permite la creación y eliminación de salas, así como el envío de notificaciones a cursos específicos.
Toda la comunicación con la API se realiza a través de HTTPS. Los cuerpos de las peticiones y respuestas deben estar en formato JSON.

### Endpoints
#### 1.1 Crear una Nueva Sala
Crea una nueva sala en la base de datos de Greenlight y la asocia a un usuario creador.
-   **Método**: `POST`
-   **URL**: `/salas/{nombre}/{emailCreador}`
-   **URL Completa**: `https://bbb.norteamericano.com/apiv2/salas/{nombre}/{emailCreador}`
-   **Parámetros de URL (Path Parameters)**
    | Campo        | Tipo   | Requerido | Descripción                                                              |
    |--------------|--------|----------|--------------------------------------------------------------------------|
    | `nombre`       | string | Sí       | El nombre que se le asignará a la sala de conferencia.                   |
    | `emailCreador` | string | Sí       | El correo electrónico del usuario registrado en Greenlight que será el propietario de la sala. |


-   **Respuesta Exitosa (201 Created)**
    Devuelve un objeto JSON con todos los detalles de la sala recién creada.
    | Campo           | Tipo   | Descripción                                                              |
    |-----------------|--------|--------------------------------------------------------------------------|
    | `roomId`          | guid   | El ID único de la sala en la base de datos de Greenlight (UUID).         |
    | `urlSala`         | string | La URL directa para unirse a la sala.                                    |
    | `claveModerador`  | string | La contraseña para unirse a la sala como moderador.                      |
    | `claveEspectador` | string | La contraseña para unirse a la sala como espectador.                     |
    | `meetingId`       | string | El ID interno de la reunión utilizado por BigBlueButton.                 |
    | `friendlyId`      | string | El ID "amigable" que forma parte de la URL de la sala.                   |
    | `recordId`        | string | Un identificador único generado para una posible grabación de esta sesión. |

-   **JSON de Ejemplo (Response)**
    ```json
    {
        "roomId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
        "urlSala": "https://bbb.norteamericano.com/rooms/abc-123-def-456/join",
        "claveModerador": "g3h4j5k6",
        "claveEspectador": "a1b2c3d4",
        "meetingId": "una_cadena_larga_de_40_caracteres",
        "friendlyId": "abc-123-def-456",
        "recordId": "una_cadena_larga_de_40_caracteres-1756543210"
    }
    ```

#### 1.2 Eliminar una Sala
Elimina permanentemente una sala y todas sus configuraciones asociadas de la base de datos de Greenlight.
-   **Método**: `DELETE`
-   **URL**: `/salas/{roomId}`
-   **URL Completa**: `https://bbb.norteamericano.com/apiv2/salas/{roomId}`
-   **Parámetros de URL (Path Parameters)**
    | Parámetro | Tipo | Requerido | Descripción                                            |
    |-----------|------|----------|--------------------------------------------------------|
    | `roomId`    | guid | Sí       | El identificador único (UUID) de la sala que se desea eliminar. |

-   **Respuesta Exitosa (204 No Content)**
    Si la eliminación es exitosa, la API responderá con un código de estado 204 y sin cuerpo de respuesta.
-   **Respuestas de Error**
    -   `404 Not Found`: Si no se encuentra ninguna sala con el `roomId` proporcionado.
    -   `500 Internal Server Error`: Si ocurre un error en la base de datos durante la eliminación.

#### 2. Enviar Invitaciones a un Curso
Envía un correo electrónico de invitación a todos los alumnos de un curso específico registrado en la base de datos MySQL del cliente.
-   **Método**: `POST`
-   **URL**: `/invitaciones/{idCursoAbierto}`
-   **URL Completa**: `https://bbb.norteamericano.com/apiv2/invitaciones/{idCursoAbierto}`
-   **Parámetros de URL (Path Parameters)**
    | Campo            | Tipo    | Requerido | Descripción                                                              |
    |------------------|---------|----------|--------------------------------------------------------------------------|
    | `idCursoAbierto` | integer | Sí       | El ID numérico del curso (de la tabla `cursosabiertosbbb`) al que se enviarán las invitaciones. |

-   **JSON de Ejemplo (Request)**
    ```json
    // No se requiere cuerpo de petición para este endpoint.
    ```
-   **Respuesta Exitosa (200 OK)**
    Devuelve un objeto JSON confirmando el resultado de la operación.
    | Campo            | Tipo    | Descripción                               |
    |------------------|---------|-------------------------------------------|
    | `mensaje`          | string  | Un mensaje de confirmación.               |
    | `correosEnviados` | integer | El número de correos que se enviaron a los alumnos del curso. |

-   **JSON de Ejemplo (Response)**
    ```json
    {
        "mensaje": "Invitaciones enviadas exitosamente.",
        "correosEnviados": 42
    }
    ```
-   **Respuestas de Error**
    -   `404 Not Found`: Si no se encuentra el curso o la sala asociada en la base de datos MySQL.

#### 3. Obtener Grabaciones de un Curso 🎥
Obtiene una lista de todas las grabaciones disponibles para un curso específico, incluyendo su URL de reproducción y fecha de creación.
-   **Método**: `GET`
-   **URL**: `/grabaciones/{idCursoAbierto}`
-   **URL Completa**: `https://bbb.norteamericano.com/apiv2/grabaciones/{idCursoAbierto}`
-   **Parámetros de URL (Path Parameters)**
    | Parámetro        | Tipo    | Requerido | Descripción                                                              |
    |------------------|---------|----------|--------------------------------------------------------------------------|
    | `idCursoAbierto` | integer | Sí       | El ID numérico del curso del que se desean obtener las grabaciones.      |

-   **Respuesta Exitosa (200 OK)**
    Devuelve un arreglo de objetos JSON, donde cada objeto representa una grabación. El arreglo está ordenado de la más reciente a la más antigua. Si no hay grabaciones, devuelve un arreglo vacío `[]`.
    | Campo         | Tipo   | Descripción                                                              |
    |---------------|--------|--------------------------------------------------------------------------|
    | `recordId`      | string | El ID único de la grabación, utilizado para construir la URL.            |
    | `playbackUrl`   | string | La URL completa para ver la grabación en un navegador.                   |
    | `createdAt`     | string | La fecha en que se creó la grabación, en formato `YYYY-MM-DD`.           |

-   **JSON de Ejemplo (Response)**
    ```json
    [
        {
            "recordId": "0cf9da8040fa52677185fdd34e4b02faa7326af6-1756918398921",
            "playbackUrl": "https://bbb.norteamericano.com/playback/presentation/2.3/0cf9da8040fa52677185fdd34e4b02faa7326af6-1756918398921",
            "createdAt": "2025-09-12"
        },
        {
            "recordId": "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0-1756910000000",
            "playbackUrl": "https://bbb.norteamericano.com/playback/presentation/2.3/a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0-1756910000000",
            "createdAt": "2025-09-10"
        }
    ]
    ```

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
