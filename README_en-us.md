# bbbAPIGL API

API for managing virtual meeting rooms (BBB), sending invitations, and accessing recordings.

## General Description

This API provides a robust interface to interact with a BigBlueButton (BBB) system, facilitating the automation of virtual meeting room management. It allows for the creation and deletion of rooms, management of invitations via email with deep integration with Google Calendar for scheduling events, and secure retrieval of recording URLs stored in an S3-compatible service.

The main objective is to simplify the administration of online learning or meeting environments, offering key functionalities for educators and platform administrators.

## Features

-   **BBB Room Management**: Programmatic creation and deletion of virtual meeting rooms.
-   **Intelligent Invitation System**:
    -   Bulk sending of invitations for entire courses.
    -   Sending of personalized individual invitations.
    -   Integration with Google Calendar for automatic creation and deletion of calendar events, for both single and recurring sessions.
-   **Access to Recordings**: Retrieval of pre-signed links to session recordings, ensuring secure and temporary access to content.
-   **Modular Architecture**: Design based on Clean Architecture and Dependency Injection principles to facilitate maintainability and scalability.

## Requirements

To compile and run this project, you will need:

-   **SDK .NET 10.0**
-   **Databases**:
    -   PostgreSQL (for `SalaRepository`)
    -   MySQL (for `MySqlCursoRepository`)
-   **External Services**:
    -   Google Cloud account with Calendar and Gmail APIs enabled.
    -   S3-compatible storage service.

The following .NET libraries are used:

-   `AWSSDK.S3`
-   `Google.Apis.Auth`
-   `Google.Apis.Calendar.v3`
-   `Google.Apis.Gmail.v1`
-   `Microsoft.AspNetCore.OpenApi`
-   `MimeKit`
-   `MySqlConnector`
-   `Npgsql`
-   `Swashbuckle.AspNetCore`

## Download and Deployment

Follow these steps to get and run the project locally or in a production environment:

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/your-username/bbbAPIGL.git
    cd bbbAPIGL
    ```

2.  **Restore Dependencies**:
    ```bash
    dotnet restore
    ```

3.  **Configure `appsettings.json` and `google-credentials.json`**:
    Make sure you have configured the `appsettings.json` and `google-credentials.json` files as described in the [Configuration](#configuration) section.

4.  **Build the Project**:
    ```bash
    dotnet build
    ```

5.  **Run in Development (Optional)**:
    To run the API in a development environment:
    ```bash
    dotnet run
    ```
    The API will be available at the URLs configured in `launchSettings.json` (usually `https://localhost:7000` and `http://localhost:5000`).

6.  **Publish for Production**:
    To prepare the application for a production environment, you can publish it:
    ```bash
    dotnet publish -c Release -o ./publish
    ```
    This will create an optimized version of the application in the `./publish` folder.

7.  **Production Deployment**:
    Copy the contents of the `./publish` folder to your production server. Make sure the .NET 9.0 runtime is installed on the server. You can run the application directly from the published folder:
    ```bash
    dotnet bbbAPIGL.dll
    ```
    For a robust deployment, consider using a web server like Nginx or Apache as a reverse proxy, and a process manager like Systemd (Linux) or IIS (Windows) to keep the application running.

## Configuration

To run the project, it is necessary to configure the credentials and settings of external services.

1.  **Application Configuration**: Rename the `appsettings.example.json` file to `appsettings.json` and fill in the corresponding values for your database (PostgreSQL and MySQL), S3, and BigBlueButton.
    *   `ConnectionStrings:PostgresDb`: Connection string for the PostgreSQL database (used by `SalaRepository`).
    *   `ConnectionStrings:MySqlDb`: Connection string for the MySQL database (used by `MySqlCursoRepository`).
    *   `S3Settings:BucketName`: Name of the S3 bucket for recordings.
    *   `S3Settings:Region`: AWS region where the S3 bucket is located.
    *   `SalaSettings:PublicUrl`: Base public URL to access BBB rooms and recordings.
    *   `SalaSettings:DefaultRoomCreatorEmail`: Default email for room creation in the central module (e.g., "norteamericanoonline@norteamericano.cl").
    *   `SalaSettings:DefaultRoomCreatorEmailEmpresa`: Default email for room creation in the enterprise module (e.g., "sedeempresa@norteamericano.cl"). **This user must exist in the PostgreSQL database (Greenlight)**.

2.  **Google Credentials**: Rename `google-credentials.example.json` to `google-credentials.json` and add the credentials of your Google Cloud service account for integration with Google Calendar and Gmail. Make sure the service account has the necessary permissions to manage calendar events and send emails.
    *   `GoogleCalendarSettings:CredentialsFile`: Path to the `google-credentials.json` file.
    *   `GoogleCalendarSettings:UserToImpersonate`: Email of the user to be impersonated to create calendar events and send emails.
    *   `GoogleCalendarSettings:DefaultTimeZone`: Default time zone for calendar events (e.g., "America/Santiago").

3.  **BigBlueButton API Configuration**:
    *   `BigBlueButtonApi:BaseUrl`: Base URL of the BBB API (e.g., `https://bbb.example.com/bigbluebutton/api`).
    *   `BigBlueButtonApi:Secret`: Shared secret (salt) of the BBB API.

4.  **Important Configuration Files**:
    *   `appsettings.json`: **NOT uploaded to the repository** (it's in `.gitignore`). Contains production credentials. Must be created/edited directly on the server.
    *   `appsettings.Production.json`: Sample/placeholder file. **Automatically deleted** after each deployment to avoid overwriting `appsettings.json`.

## Code Documentation

The project follows a clean and modular architecture, organized into the following layers:

### 1. Controllers

Located in the `Controllers` folder, they are responsible for handling incoming HTTP requests, invoking business logic through services, and returning HTTP responses.

-   **`SalasController.cs`**: Exposes endpoints for creating, deleting, updating rooms, sending invitations (course and individual), and obtaining recording URLs for the central module.
-   **`SalasEmpController.cs`**: Exposes endpoints for managing rooms and invitations in the enterprise module (without Google Calendar integration). Includes endpoints for batch operations.
-   **`TestController.cs`**: Controller for testing and system diagnostics.

### 2. DTOs (Data Transfer Objects)

Located in the `DTOs` folder, they define the structure of the data sent and received through the API.

-   `CrearSalaRequest`, `CrearSalaResponse`: For creating rooms.
-   `EliminarSalaRequest`: For deleting rooms (although the endpoint uses a `Guid` directly).
-   `EnviarInvitacionCursoRequest`, `EnviarInvitacionCursoResponse`: For sending course invitations.
-   `EnviarInvitacionIndividualRequest`: For sending individual invitations.
-   `GrabacionDto`: For recording information.

### 3. Models

Located in the `Models` folder, they represent the business domain entities.

-   **`Sala.cs`**: Represents a virtual meeting room, including its `MeetingId`, `FriendlyId`, access keys, and the `IdCalendario` for integration with Google Calendar.
-   **`CursoAbiertoSala.cs`**: Model that combines information from an open course with BBB room details.
-   **`RecordingInfo.cs`**: Basic information of a recording.

### 4. Services

Located in the `Services` folder, they contain the main business logic and orchestrate operations.

-   **`ISalaService` / `SalaService.cs`**: Implements the central logic for room management, including the generation of IDs, keys, interaction with repositories, and email/calendar services.
-   **`ISalaEmpresaService` / `SalaEmpresaService.cs`**: Implements the logic for managing rooms in the enterprise module, optimized for scenarios without Google Calendar integration.
-   **`IEmailService` / `GoogleCalendarService.cs`**: Abstraction and implementation for sending emails and managing events in Google Calendar (creation, update, and deletion). It uses the Google Calendar and Gmail API.
-   **`IAcademicCalendarService` / `AcademicCalendarService.cs`**: Service for managing and calculating academic calendars.
-   **`IS3Service` / `S3Service.cs`**: Abstraction and implementation to interact with S3-compatible storage services, specifically to generate pre-signed URLs for access to recordings.

### 5. Repositories

Located in the `Repositories` folder, they are responsible for abstracting the data access layer.

-   **`ISalaRepository` / `SalaRepository.cs`**: Provides methods to persist and retrieve room data in a PostgreSQL database. It includes operations to save, delete, and get rooms, as well as their calendar IDs.
-   **`ICursoRepository` / `MySqlCursoRepository.cs`**: Provides methods to interact with a MySQL database, obtaining information about courses, student emails, and disassociating rooms from courses.

### 6. Program.cs

Configures dependency injection, registering services and repositories with their respective interfaces. It also configures the HTTP request pipeline (Scalar API Reference, HTTPS redirection, etc.).

## API Endpoints

The API has two main modules differentiated by their route prefix. For detailed documentation with complete examples, see the [ENDPOINTS.md](ENDPOINTS.md) file.

### Interactive Documentation (Scalar)

The API includes interactive documentation using Scalar, available at:

- **Development:** `https://localhost:7000/api-docs`
- **Production:** `https://bbb.norteamericano.cl/api-docs`

Scalar provides:
- Complete list of all endpoints (Central and Enterprise)
- Interactive testing from the browser
- Detailed data models
- Request/response examples
- Modern and responsive UI

---

### 1. Central Module (Normal) - `/apiv2`

This module includes full integration with Google Calendar and email invitations.

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/apiv2/salas` | Creates a new virtual meeting room |
| `DELETE` | `/apiv2/salas/{roomId}` | Deletes an existing room by GUID |
| `GET` | `/apiv2/salas/{idCursoAbierto}/status` | Gets comprehensive room status diagnosis |
| `POST` | `/apiv2/invitaciones/{idCursoAbierto}` | Sends bulk invitations to a course |
| `POST` | `/apiv2/invitaciones/individual/{idAlumno}/{idCursoAbierto}` | Sends individual invitation to a student |
| `PUT` | `/apiv2/invitaciones/{idCursoAbierto}` | Updates calendar event and resends invitations |
| `GET` | `/apiv2/grabaciones/{idCursoAbierto}` | Gets recording URLs for the course |
| `POST` | `/apiv2/reprogramar-sesion` | Reschedules a specific session |
| `DELETE` | `/apiv2/cursos/{idCursoAbierto}` | Deletes a course and its calendar invitations |

> **Note:** The invitations module is robust: if Google Calendar fails, emails are still sent.

---

### 2. Enterprise Module - `/apiv2/emp`

Simplified version for the `sige_sam_empresa` database (no Google Calendar integration).

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/apiv2/emp/salas` | Creates a room for the enterprise module |
| `DELETE` | `/apiv2/emp/salas/{roomId}` | Deletes a room from the enterprise module |
| `GET` | `/apiv2/emp/salas/{idCursoAbierto}/status` | Gets room status diagnosis for enterprises |
| `GET` | `/apiv2/emp/grabaciones/{idCursoAbierto}` | Retrieves recordings from the enterprise module |
| `POST` | `/apiv2/emp/invitaciones/{idCursoAbierto}` | Creates invitation/session record |
| `PUT` | `/apiv2/emp/invitaciones/{id}` | Reschedules existing invitation |
| `POST` | `/apiv2/emp/invitaciones/batch` | Executes bulk operations (create/edit/delete) |
| `POST` | `/apiv2/emp/reprogramar-sesion` | General session rescheduling for enterprises |
| `DELETE` | `/apiv2/emp/cursos/{idCursoAbierto}` | Deletes enterprise course data |

> **Note:** If a room already exists for the `idCursoAbierto`, the API returns existing data without creating duplicates.

---

### View Complete Documentation

For detailed examples of requests, responses, and error codes, see:
- 📄 **[ENDPOINTS.md](ENDPOINTS.md)** - Complete documentation of all endpoints

## Change History

### 2026-03-16

-   **New Invitation Endpoints (Enterprise Module)**: Additional endpoints for session management in the enterprise module:
    -   `POST /apiv2/emp/invitaciones/{idCursoAbierto}`: Registers a new session in the `sesionescursos` table.
    -   `PUT /apiv2/emp/invitaciones/{id}`: Modifies (reschedules) an existing invitation, marking the previous one as suspended.
    -   `POST /apiv2/emp/invitaciones/batch`: Executes bulk operations (create, edit, delete) in a single request.
-   **Interactive Documentation with Scalar**: Replaced Swagger with Scalar as the interactive documentation system, available at `/api-docs`. Provides a modern experience with complete endpoint list, detailed data models, and interactive testing from the browser.
-   **Improved Error Handling**: Refined exception handling in both controllers (`SalasController` and `SalasEmpController`) to distinguish between application errors (`ApplicationException`), validation errors (`InvalidOperationException`), and unexpected errors.

### 2026-03-10

-   **Invitation Robustness (Central Module)**: Implemented deeper error handling logic for Google Calendar integrations. Calendar event creation/update failures (e.g., manually deleted events) no longer block reminder email sending to students. The system automatically attempts to recover lost events.
-   **Deployment Fix**: Adjusted `TargetFramework` to `net9.0` to ensure compatibility with the runtime installed on the Amazon EC2 server, resolving startup failures after publishing.

### 2025-11-06

-   **API Routing Fix:** Adjusted `SalasController` to use `[Route("apiv2")]` at the class level and removed redundant prefixes from actions, resolving 404 issues.
-   **502 Bad Gateway Error Solution:** Corrected the `systemd` service file (`kestrel-bbbapigl.service`) so that the `ExecStart` command correctly points to the application's `.dll` file, resolving the 502 error.
-   **Database Error Resolution (PostgreSQL):** Removed the reference to the `calendar_event_id` column from the `INSERT` query in `SalaRepository.cs` and removed the `ObtenerIdCalendarioPorSalaIdAsync` method from `SalaRepository` and `ISalaRepository`, as this column does not belong to PostgreSQL.
-   **Bulk Invitation Logic Correction:** Added schedule synchronization logic (`ActualizarHorarioDesdeFuenteExternaAsync`) to the `EnviarInvitacionesCursoAsync` method in `SalaService.cs`, preventing the "schedule not defined" error when sending course invitations.
-   **Nginx Configuration Adjustment:** Corrected the `proxy_pass` directive in the Nginx configuration to ensure that requests to `/apiv2/` are correctly redirected to the application without modifying the URL.

### 2025-11-05

-   **Improvement in Individual Invitation Logic**: The logic for sending individual invitations (`EnviarInvitacionIndividualAsync`) has been improved. Now, if a course already has a calendar event created, the new student will be added to that existing event instead of creating a new one. If the course does not have an event, one will be created with the first invited student and the event ID will be saved for future invitations, ensuring that all students in a course share the same calendar event.
-   **MySQL Database Error Correction**: A critical error that occurred when trying to read the `cursosabiertosbbbinvitacion` table was fixed. The code expected an `idCalendario` column that did not exist in the database. The repository methods (`MySqlCursoRepository`) have been modified to no longer attempt to access this column, preventing the application from failing.
-   **Email Encoding Correction**: An issue in the email sending service (`GmailService`) that caused accents and special characters to not be displayed correctly in email templates was fixed. The message construction has been refactored to ensure UTF-8 encoding.
-   **Email Template Improvement**: The email template was updated to display the meeting room URL instead of its internal ID, making the invitation clearer for the end user.
-   **Internal Improvements and Warning Fixes**: Several minor improvements were made to the code and compiler warnings were fixed to improve code quality and maintainability.

## Folder Structure

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