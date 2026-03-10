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

## Requisitos

Para compilar y ejecutar este proyecto, necesitarás:

-   **SDK de .NET 9.0** (IMPORTANTE: El servidor utiliza .NET 9.0, no usar versiones superiores para asegurar compatibilidad).
-   **Bases de Datos**:
    -   PostgreSQL (para `SalaRepository` - Configurada en puerto **5433** para entornos Docker)
    -   MySQL (Central): Base de datos `sige_sam_v3` (usada por `MySqlCursoRepository`).
    -   MySQL (Empresa): Base de datos `sige_sam_empresa` (usada por `MySqlCursoEmpresaRepository`).
-   **Servidores Web**:
    -   Nginx (como Proxy Inverso, ver sección de [Nginx](#configuración-de-nginx))
-   **Servicios Externos**:
    -   Cuenta de Google Cloud con API de Calendar y Gmail habilitadas.
    -   Servicio de almacenamiento compatible con S3.

Las siguientes librerías de .NET son utilizadas:

-   `AWSSDK.S3`
-   `Google.Apis.Auth`
-   `Google.Apis.Calendar.v3`
-   `Google.Apis.Gmail.v1`
-   `Microsoft.AspNetCore.OpenApi`
-   `MimeKit`
-   `MySqlConnector`
-   `Npgsql`
-   `Swashbuckle.AspNetCore`

## Descarga e Implementación

Sigue estos pasos para obtener y ejecutar el proyecto localmente o en un entorno de producción:

1.  **Clonar el Repositorio**:
    ```bash
    git clone https://github.com/tu-usuario/bbbAPIGL.git
    cd bbbAPIGL
    ```

2.  **Restaurar Dependencias**:
    ```bash
    dotnet restore
    ```

3.  **Configurar `appsettings.json` y `google-credentials.json`**:
    Asegúrate de haber configurado los archivos `appsettings.json` y `google-credentials.json` como se describe en la sección de [Configuración](#configuración).

4.  **Compilar el Proyecto**:
    ```bash
    dotnet build
    ```

5.  **Ejecutar en Desarrollo (Opcional)**:
    Para ejecutar la API en un entorno de desarrollo:
    ```bash
    dotnet run
    ```
    La API estará disponible en las URLs configuradas en `launchSettings.json` (usualmente `https://localhost:7000` y `http://localhost:5000`).

6.  **Publicar para Producción**:
    Para preparar la aplicación para un entorno de producción, puedes publicarla:
    ```bash
    dotnet publish -c Release -o ./publish
    ```
    Esto creará una versión optimizada de la aplicación en la carpeta `./publish`.

7.  **Implementación en Producción**:
    Copia el contenido de la carpeta `./publish` a tu servidor de producción. Asegúrate de que el entorno de ejecución de .NET 9.0 esté instalado en el servidor. Puedes ejecutar la aplicación directamente desde la carpeta publicada:
    ```bash
    dotnet bbbAPIGL.dll
    ```
    Para una implementación robusta, considera usar un servidor web como Nginx o Apache como proxy inverso, y un gestor de procesos como Systemd (Linux) o IIS (Windows) para mantener la aplicación en ejecución.

## Configuración

Para poder ejecutar el proyecto, es necesario configurar las credenciales y ajustes de los servicios externos.

1.  **Configuración de la aplicación**: Renombre el archivo `appsettings.example.json` a `appsettings.json` y rellene los valores correspondientes a su base de datos (PostgreSQL y MySQL), S3 y BigBlueButton.
    *   `ConnectionStrings:PostgresDb`: Cadena de conexión para la base de datos PostgreSQL (usada por `SalaRepository`).
    *   `ConnectionStrings:MySqlDb`: Cadena de conexión para la base de datos MySQL central (`sige_sam_v3`).
    *   `ConnectionStrings:MySqlDbEmpresa`: Cadena de conexión para la base de datos MySQL de empresas (`sige_sam_empresa`).
    *   `S3Settings:BucketName`: Nombre del bucket S3 para grabaciones.
    *   `S3Settings:Region`: Región de AWS donde se encuentra el bucket S3.
    *   `SalaSettings:PublicUrl`: URL pública base para acceder a las salas y grabaciones de BBB.
    *   `SalaSettings:DefaultRoomCreatorEmail`: Correo electrónico por defecto para la creación de salas (ej. "norteamericanoonline@norteamericano.cl").

2.  **Credenciales de Google**: Renombre `google-credentials.example.json` a `google-credentials.json` y añada las credenciales de su cuenta de servicio de Google Cloud para la integración con Google Calendar y Gmail. Asegúrese de que la cuenta de servicio tenga los permisos necesarios para gestionar eventos de calendario y enviar correos electrónicos.
    *   `GoogleCalendarSettings:CredentialsFile`: Ruta al archivo `google-credentials.json`.
    *   `GoogleCalendarSettings:UserToImpersonate`: Correo electrónico del usuario que será suplantado para crear eventos de calendario y enviar correos.
    *   `GoogleCalendarSettings:DefaultTimeZone`: Zona horaria por defecto para los eventos de calendario (ej. "America/Santiago").

3.  **Configuración de API BigBlueButton**:
    *   `BigBlueButtonApi:BaseUrl`: URL base de la API de BBB (ej. `https://bbb.example.com/bigbluebutton/api`).
    *   `BigBlueButtonApi:Secret`: Secreto compartido (salt) de la API de BBB.

## Documentación del Código

El proyecto sigue una arquitectura limpia y modular, organizada en las siguientes capas:

### 1. Controllers

Ubicados en la carpeta `Controllers`, son responsables de manejar las peticiones HTTP entrantes, invocar la lógica de negocio a través de los servicios y devolver las respuestas HTTP.

-   **`SalasController.cs`**: Expone los endpoints para la creación, eliminación, actualización de salas, envío de invitaciones (curso e individual) y obtención de URLs de grabaciones.

### 2. DTOs (Data Transfer Objects)

Ubicados en la carpeta `DTOs`, definen la estructura de los datos que se envían y reciben a través de la API.

-   `CrearSalaRequest`, `CrearSalaResponse`: Para la creación de salas.
-   `EliminarSalaRequest`: Para la eliminación de salas (aunque el endpoint usa un `Guid` directamente).
-   `EnviarInvitacionCursoRequest`, `EnviarInvitacionCursoResponse`: Para el envío de invitaciones a cursos. Acepta opcionalmente `EmailCreador`.
-   `EnviarInvitacionIndividualRequest`: Para el envío de invitaciones individuales. Acepta opcionalmente `EmailCreador`.
-   `GrabacionDto`: Para la información de grabaciones.
-   `SalaStatusDto`: Para la respuesta del diagnóstico de estado de una sala.

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
-   **`ICursoRepository` / `MySqlCursoRepository.cs`**: Acceso a datos para el sistema central `sige_sam_v3`.
-   **`ICursoEmpresaRepository` / `MySqlCursoEmpresaRepository.cs`**: Acceso a datos para el sistema de empresas `sige_sam_empresa`.

### 6. Program.cs

Configura la inyección de dependencias, registrando los servicios y repositorios con sus respectivas interfaces. También configura el pipeline de peticiones HTTP (Swagger, HTTPS redirection, etc.).

## Endpoints de la API

La API cuenta con dos módulos principales diferenciados por su prefijo de ruta:

### 1. Módulo Central (Normal) - `/apiv2`

Este módulo incluye integración completa con Google Calendar e invitaciones por correo.

#### `POST /apiv2/salas`
Crea una nueva sala de reuniones virtual y la vincula automáticamente a un curso en MySQL.
- **Cuerpo (`CrearSalaRequest`)**:
    ```json
    { "nombre": "Sala X", "emailCreador": "admin@example.com", "idCursoAbierto": 123 }
    ```

#### `DELETE /apiv2/salas/{roomId}`
Elimina una sala existente por su GUID.

#### `GET /apiv2/salas/{idCursoAbierto}/status`
Obtiene un diagnóstico integral del estado de la sala.

#### `POST /apiv2/invitaciones/{idCursoAbierto}`
Envía invitaciones masivas.
- **Robustez**: Si falla Google Calendar, se envían los correos de todas formas.
- **Cuerpo (opcional)**: `{ "emailCreador": "admin@example.com" }`

#### `POST /apiv2/invitaciones/individual/{idAlumno}/{idCursoAbierto}`
Envía una invitación individual a un alumno.

#### `PUT /apiv2/invitaciones/{idCursoAbierto}`
Actualiza el evento de calendario y las invitaciones.
- **Cuerpo (`ActualizarEventoCalendarioRequest`)**:
    ```json
    { "fechaInicio": "2026-03-15", "fechaTermino": "2026-06-15", "dias": ["Lunes", "Miercoles"], "horaInicio": "18:00", "horaTermino": "20:00" }
    ```

#### `GET /apiv2/grabaciones/{idCursoAbierto}`
Obtiene las URLs de las grabaciones del curso.

#### `POST /apiv2/reprogramar-sesion`
Reprograma una sesión específica en el calendario.
- **Cuerpo (`ReprogramarSesionRequest`)**:
    ```json
    { "idCursoAbierto": 123, "sesionNumero": 5, "fechaOriginalSesion": "2026-03-15", "fechaNuevaSesion": "2026-03-22" }
    ```

#### `DELETE /apiv2/cursos/{idCursoAbierto}`
Elimina un curso y sus invitaciones de calendario.

---

### 2. Módulo Empresa - `/apiv2/emp`

Versión simplificada para la base de datos `sige_sam_empresa` (sin lógica de calendario).

#### `POST /apiv2/emp/invitaciones/{idCursoAbierto}`
Crea un registro de invitación en la base de empresa.
- **Cuerpo (`CrearInvitacionEmpresaRequest`)**:
    ```json
    { "id": 12345, "fecha": "2026-03-15" }
    ```

#### `PUT /apiv2/emp/invitaciones/{id}`
Reprograma una invitación existente por su ID único.
- **Cuerpo (`ReprogramarSesionRequest`)**:
    ```json
    { "fechaNuevaSesion": "2026-03-22" }
    ```

#### `POST /apiv2/emp/invitaciones/batch`
Ejecuta operaciones masivas (crear, editar, eliminar) en un solo lote.
- **Cuerpo (`List<OperacionInvitacionEmpresaRequest>`)**:
    ```json
    [
      { "accion": "crear", "id": 555, "fecha": "2026-04-01" },
      { "accion": "editar", "id": 123, "fechaNueva": "2026-04-05" },
      { "accion": "eliminar", "id": 999 }
    ]
    ```

#### `POST /apiv2/emp/salas`
Crea una sala para el módulo empresa.
- **Cuerpo (`CrearSalaRequest`)**: Similar al Módulo Central.

#### `GET /apiv2/emp/salas/{idCursoAbierto}/status`
Diagnóstico del estado de sala para empresas.

#### `GET /apiv2/emp/grabaciones/{idCursoAbierto}`
Recupera grabaciones del módulo empresa.

#### `POST /apiv2/emp/reprogramar-sesion`
Reprogramación general de sesiones para empresas.

#### `DELETE /apiv2/emp/cursos/{idCursoAbierto}`
Elimina los datos de un curso de empresa.


## Historial de Cambios

### 10-03-2026

-   **Robustecimiento de Invitaciones (Módulo Central)**: Se ha implementado una lógica de manejo de errores más profunda para las integraciones con Google Calendar. Ahora, fallos en la actualización o creación de eventos de calendario (ej. eventos eliminados manualmente) no bloquean el envío de correos electrónicos recordatorios a los alumnos. El sistema intenta recuperar eventos perdidos automáticamente.
-   **Corrección de Despliegue**: Se ajustó el `TargetFramework` a `net9.0` para asegurar compatibilidad con el runtime instalado en el servidor Amazon EC2, resolviendo fallos de inicio tras la publicación.


### 25-02-2026

-   **Nuevo Módulo Empresa (`/apiv2/emp`)**: Implementación de un nuevo conjunto de endpoints para la gestión de salas vinculadas a la base de datos `sige_sam_empresa`.
-   **Arquitectura Paralela**: Creación de `MySqlCursoEmpresaRepository` y `SalaEmpresaService` para manejar la lógica de empresas de forma independiente, optimizada para escenarios que no requieren (por ahora) integración con Google Calendar.
-   **Configuración Dinámica**: Soporte para múltiples cadenas de conexión MySQL en `appsettings.json`.

### 23-02-2026

-   **Diagnóstico Integral**: Nuevo endpoint `/salas/{id}/status` que verifica la sincronización entre SAM (MySQL), Greenlight (PostgreSQL) y el estado de la sesión activa en BigBlueButton.
-   **Soporte Multi-Creador**: Se habilitó la posibilidad de especificar el correo electrónico del creador en los endpoints de invitaciones. Esto permite que las salas sean propiedad de diferentes usuarios en Greenlight dinámicamente.
-   **Configuración Centralizada de Creador**: Se añadió `DefaultRoomCreatorEmail` en la configuración para definir el usuario por defecto de todas las salas nuevas.
-   **Integración con API de BBB**: Implementación de llamadas directas a la API de BigBlueButton (ej. `isMeetingRunning`) con cálculo de checksum seguro.

### 02-01-2026

-   **Lógica de Creación Inteligente**: Se modificó `CrearNuevaSalaAsync` para verificar la existencia previa de una sala por `idCursoAbierto`. Si existe, se retornan los datos actuales, evitando duplicidad en PostgreSQL y BigBlueButton.
-   **Sincronización Automática con MySQL**: Al crear una sala, la API ahora vincula automáticamente el `roomId` y las claves en la tabla `cursosabiertosbbb` y realiza una sincronización inmediata del horario (fechas y días) desde el sistema central `sige_sam_v3`.
-   **Robustez en Despliegue**: Se refinó el script `publish.ps1` para automatizar la limpieza, compilación, transferencia vía SCP a carpeta temporal y reinicio de servicios (`systemd` y `nginx`) con un solo comando.
-   **Corrección de Infraestructura (Nginx)**: Se identificó y resolvió un conflicto de rutas mediante el uso del modificador `^~ /apiv2/` en la configuración de Nginx, evitando que las reglas internas de BigBlueButton interceptaran las peticiones a la API.
-   **Ajuste de PostgreSQL**: Se configuró la conexión al puerto `5433` para apuntar correctamente al contenedor Docker de Greenlight v3, evitando conflictos con instancias locales del host.

### 18-11-2025

-   **Corrección en Reprogramación de Sesiones**: Se solucionó un error en la lógica de reprogramación de sesiones (`ReprogramarSesionAsync`) que impedía la creación de eventos en Google Calendar para sesiones de secuencia baja o media. Ahora, el evento se crea correctamente en la fecha solicitada, asegurando que todas las sesiones reprogramadas se reflejen en el calendario.

### 06-11-2025

-   **Corrección de Enrutamiento en API:** Se ajustó el `SalasController` para usar `[Route("apiv2")]` a nivel de clase y se eliminaron los prefijos redundantes de las acciones, resolviendo problemas de 404.
-   **Solución de Error 502 (Bad Gateway):** Se corrigió el archivo de servicio `systemd` (`kestrel-bbbapigl.service`) para que el comando `ExecStart` apunte correctamente al archivo `.dll` de la aplicación, resolviendo el error 502.
-   **Resolución de Error de Base de Datos (PostgreSQL):** Se eliminó la referencia a la columna `calendar_event_id` de la consulta `INSERT` en `SalaRepository.cs` y se eliminó el método `ObtenerIdCalendarioPorSalaIdAsync` de `SalaRepository` e `ISalaRepository`, ya que esta columna no pertenece a PostgreSQL.
-   **Corrección de Lógica de Invitaciones Masivas:** Se añadió la lógica de sincronización de horarios (`ActualizarHorarioDesdeFuenteExternaAsync`) al método `EnviarInvitacionesCursoAsync` en `SalaService.cs`, evitando el error de "horario no definido" al enviar invitaciones a cursos.
-   **Ajuste de Configuración de Nginx:** Se corrigió la directiva `proxy_pass` en la configuración de Nginx para asegurar que las peticiones a `/apiv2/` se redirijan correctamente a la aplicación sin modificar la URL.

### 05-11-2025

-   **Mejora en la Lógica de Invitaciones Individuales**: Se ha mejorado la lógica de envío de invitaciones individuales (`EnviarInvitacionIndividualAsync`). Ahora, si un curso ya tiene un evento de calendario creado, se añadirá al nuevo alumno a ese evento existente en lugar de crear uno nuevo. Si el curso no tiene un evento, se creará uno con el primer alumno invitado y se guardará el ID del evento para futuras invitaciones, asegurando que todos los alumnos de un curso compartan el mismo evento de calendario.
-   **Corrección de Error en Base de Datos MySQL**: Se solucionó un error crítico que ocurría al intentar leer la tabla `cursosabiertosbbbinvitacion` debido a que el código esperaba una columna `idCalendario` que no existía en la base de datos. Se han modificado los métodos del repositorio (`MySqlCursoRepository`) para que ya no intenten acceder a esta columna, evitando el fallo de la aplicación.
-   **Corrección de Codificación en Correos Electrónicos**: Se solucionó un problema en el servicio de envío de correos (`GmailService`) que causaba que los acentos y caracteres especiales no se mostraran correctamente en las plantillas de correo. Se ha refactorizado la construcción de los mensajes para asegurar la codificación UTF-8.
-   **Mejora en Plantilla de Correo**: Se actualizó la plantilla de correo para que muestre la URL de la sala de reuniones en lugar de su ID interno, haciendo la invitación más clara para el usuario final.
-   **Mejoras Internas y Corrección de Advertencias**: Se realizaron varias mejoras menores en el código y se corrigieron advertencias del compilador para mejorar la calidad y mantenibilidad del código.

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