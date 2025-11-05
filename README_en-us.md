# bbbAPIGL API

API for managing virtual meeting rooms (BBB), sending invitations, and accessing recordings.

## General Description

This API provides a robust interface to interact with a BigBlueButton (BBB) system, facilitating the automation of virtual meeting room management. It allows the creation and deletion of rooms, management of invitations via email with deep integration with Google Calendar to schedule events, and secure retrieval of recording URLs stored in an S3-compatible service.

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

-   **SDK .NET 9.0**
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

2.  **Google Credentials**: Rename `google-credentials.example.json` to `google-credentials.json` and add the credentials of your Google Cloud service account for integration with Google Calendar and Gmail. Make sure the service account has the necessary permissions to manage calendar events and send emails.
    *   `GoogleCalendarSettings:CredentialsFile`: Path to the `google-credentials.json` file.
    *   `GoogleCalendarSettings:UserToImpersonate`: Email of the user to be impersonated to create calendar events and send emails.
    *   `GoogleCalendarSettings:DefaultTimeZone`: Default time zone for calendar events (e.g., "America/Santiago").

## Code Documentation

The project follows a clean and modular architecture, organized into the following layers:

### 1. Controllers

Located in the `Controllers` folder, they are responsible for handling incoming HTTP requests, invoking business logic through services, and returning HTTP responses.

-   **`SalasController.cs`**: Exposes the endpoints for creating, deleting, updating rooms, sending invitations (course and individual), and obtaining recording URLs.

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
-   **`IEmailService` / `GoogleCalendarService.cs`**: Abstraction and implementation for sending emails and managing events in Google Calendar (creation, update, and deletion). It uses the Google Calendar and Gmail API.
-   **`IS3Service` / `S3Service.cs`**: Abstraction and implementation to interact with S3-compatible storage services, specifically to generate pre-signed URLs for access to recordings.

### 5. Repositories

Located in the `Repositories` folder, they are responsible for abstracting the data access layer.

-   **`ISalaRepository` / `SalaRepository.cs`**: Provides methods to persist and retrieve room data in a PostgreSQL database. It includes operations to save, delete, and get rooms, as well as their calendar IDs.
-   **`ICursoRepository` / `MySqlCursoRepository.cs`**: Provides methods to interact with a MySQL database, obtaining information about courses, student emails, and disassociating rooms from courses.

### 6. Program.cs

Configures dependency injection, registering services and repositories with their respective interfaces. It also configures the HTTP request pipeline (Swagger, HTTPS redirection, etc.).

## Usage Documentation

API Documentation for BigBlueButton Integration
Version: 2.1 https://www.example.com/apiv2/
Introduction
This API provides an interface to interact with the BigBlueButton (BBB) web conferencing platform through its Greenlight management system. It allows the creation and deletion of rooms, as well as sending notifications to specific courses.
All communication with the API is done via HTTPS. The bodies of requests and responses must be in JSON format.

### Endpoints
#### 1.1 Create a New Room
Creates a new room in the Greenlight database and associates it with a creator user.
-   **Method**: `POST`
-   **URL**: `/salas`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/salas`
-   **Request Body**
    ```json
    {
        "nombre": "string",
        "emailCreador": "string"
    }
    ```
-   **Successful Response (201 Created)**
    Returns a JSON object with all the details of the newly created room.
    | Field           | Type   | Description                                                              |
    |-----------------|--------|--------------------------------------------------------------------------|
    | `roomId`          | guid   | The unique ID of the room in the Greenlight database (UUID).         |
    | `urlSala`         | string | The direct URL to join the room.                                    |
    | `claveModerador`  | string | The password to join the room as a moderator.                      |
    | `claveEspectador` | string | The password to join the room as a spectator.                     |
    | `meetingId`       | string | The internal meeting ID used by BigBlueButton.                 |
    | `friendlyId`      | string | The "friendly" ID that is part of the room URL.                   |
    | `recordId`        | string | A unique identifier generated for a possible recording of this session. |

-   **JSON Example (Response)**
    ```json
    {
        "roomId": "a1b2c3d4-e5f6-7890-1234-567890abcdef",
        "urlSala": "https://bbb.norteamericano.com/rooms/abc-123-def-456/join",
        "claveModerador": "g3h4j5k6",
        "claveEspectador": "a1b2c3d4",
        "meetingId": "a_long_string_of_40_characters",
        "friendlyId": "abc-123-def-456",
        "recordId": "a_long_string_of_40_characters-1756543210"
    }
    ```

#### 1.2 Delete a Room
Permanently deletes a room and all its associated configurations from the Greenlight database.
-   **Method**: `DELETE`
-   **URL**: `/salas/{roomId}`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/salas/{roomId}`
-   **URL Parameters (Path Parameters)**
    | Parameter | Type | Required | Description                                            |
    |-----------|------|----------|--------------------------------------------------------|
    | `roomId`    | guid | Yes       | The unique identifier (UUID) of the room to be deleted. |

-   **Successful Response (204 No Content)**
    If the deletion is successful, the API will respond with a 204 status code and no response body.
-   **Error Responses**
    -   `404 Not Found`: If no room is found with the provided `roomId`.
    -   `500 Internal Server Error`: If a database error occurs during deletion.

#### 2. Send Invitations to a Course
Sends an invitation email to all students of a specific course registered in the client's MySQL database.
-   **Method**: `POST`
-   **URL**: `/invitaciones/{idCursoAbierto}`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/invitaciones/{idCursoAbierto}`
-   **URL Parameters (Path Parameters)**
    | Field            | Type    | Required | Description                                                              |
    |------------------|---------|----------|--------------------------------------------------------------------------|
    | `idCursoAbierto` | integer | Yes       | The numeric ID of the course (from the `cursosabiertosbbb` table) to which the invitations will be sent. |

-   **JSON Example (Request)**
    ```json
    // No request body is required for this endpoint.
    ```
-   **Successful Response (200 OK)**
    Returns a JSON object confirming the result of the operation.
    | Field            | Type    | Description                               |
    |------------------|---------|-------------------------------------------|
    | `mensaje`          | string  | A confirmation message.               |
    | `correosEnviados` | integer | The number of emails sent to the course students. |

-   **JSON Example (Response)**
    ```json
    {
        "mensaje": "Invitations sent successfully.",
        "correosEnviados": 42
    }
    ```
-   **Error Responses**
    -   `404 Not Found`: If the course or the associated room is not found in the MySQL database.

#### 2.1 Send Individual Invitation to a Course
Sends an invitation email to a specific student of a course registered in the client's MySQL database.
-   **Method**: `POST`
-   **URL**: `/invitaciones/individual/{idAlumno}/{idCursoAbierto}`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/invitaciones/individual/{idAlumno}/{idCursoAbierto}`
-   **URL Parameters (Path Parameters)**
    | Field            | Type    | Required | Description                                                              |
    |------------------|---------|----------|--------------------------------------------------------------------------|
    | `idAlumno` | string | Yes       | The ID of the student to whom the invitation will be sent. |
    | `idCursoAbierto` | integer | Yes       | The numeric ID of the course (from the `cursosabiertosbbb` table) to which the invitation will be sent. |

-   **JSON Example (Request)**
    ```json
    // No request body is required for this endpoint.
    ```
-   **Successful Response (200 OK)**
    Returns a JSON object confirming the result of the operation.
    | Field            | Type    | Description                               |
    |------------------|---------|-------------------------------------------|
    | `mensaje`          | string  | A confirmation message.               |
    | `correosEnviados` | integer | The number of emails sent. |

-   **JSON Example (Response)**
    ```json
    {
        "mensaje": "Invitation sent successfully.",
        "correosEnviados": 1
    }
    ```
-   **Error Responses**
    -   `400 Bad Request`: If the request is invalid.
    -   `500 Internal Server Error`: Internal server error.

#### 2.2 Update Course Invitations
Updates the invitations for an open course.
-   **Method**: `PUT`
-   **URL**: `/invitaciones/{idCursoAbierto}`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/invitaciones/{idCursoAbierto}`
-   **URL Parameters (Path Parameters)**
    | Field            | Type    | Required | Description                                                              |
    |------------------|---------|----------|--------------------------------------------------------------------------|
    | `idCursoAbierto` | integer | Yes       | The numeric ID of the course (from the `cursosabiertosbbb` table) for which the invitations will be updated. |

-   **Request Body**
    ```json
    {
        "idCursoAbierto": 0,
        "fechaInicio": "2025-11-05T15:07:26.158Z",
        "fechaTermino": "2025-11-05T15:07:26.158Z",
        "dias": [
            "Monday"
        ],
        "horaInicio": "string",
        "horaTermino": "string"
    }
    ```
-   **Successful Response (200 OK)**
    Returns a JSON object confirming the result of the operation.
    | Field            | Type    | Description                               |
    |------------------|---------|-------------------------------------------|
    | `mensaje`          | string  | A confirmation message.               |
    | `correosEnviados` | integer | The number of emails that were updated. |

-   **JSON Example (Response)**
    ```json
    {
        "mensaje": "Invitations updated successfully.",
        "correosEnviados": 10
    }
    ```
-   **Error Responses**
    -   `400 Bad Request`: If the request is invalid.
    -   `500 Internal Server Error`: Internal server error.

#### 3. Get Course Recordings 🎥
Gets a list of all available recordings for a specific course, including their playback URL and creation date.
-   **Method**: `GET`
-   **URL**: `/grabaciones/{idCursoAbierto}`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/grabaciones/{idCursoAbierto}`
-   **URL Parameters (Path Parameters)**
    | Parameter        | Type    | Required | Description                                                              |
    |------------------|---------|----------|--------------------------------------------------------------------------|
    | `idCursoAbierto` | integer | Yes       | The numeric ID of the course from which the recordings are to be obtained.      |

-   **Successful Response (200 OK)**
    Returns an array of JSON objects, where each object represents a recording. The array is sorted from newest to oldest. If there are no recordings, it returns an empty array `[]`.
    | Field         | Type   | Description                                                              |
    |---------------|--------|--------------------------------------------------------------------------|
    | `recordId`      | string | The unique ID of the recording, used to build the URL.            |
    | `playbackUrl`   | string | The full URL to watch the recording in a browser.                   |
    | `createdAt`     | string | The date the recording was created, in `YYYY-MM-DD` format.           |

-   **JSON Example (Response)**
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

#### 4. Delete a Course
Deletes an open course and all its associated invitations.
-   **Method**: `DELETE`
-   **URL**: `/cursos/{idCursoAbierto}`
-   **Full URL**: `https://bbb.norteamericano.com/apiv2/cursos/{idCursoAbierto}`
-   **URL Parameters (Path Parameters)**
    | Parameter | Type | Required | Description                                            |
    |-----------|------|----------|--------------------------------------------------------|
    | `idCursoAbierto`    | integer | Yes       | The unique identifier of the course to be deleted. |

-   **Successful Response (204 No Content)**
    If the deletion is successful, the API will respond with a 204 status code and no response body.
-   **Error Responses**
    -   `404 Not Found`: If the course with the provided `idCursoAbierto` is not found.
    -   `500 Internal Server Error`: If a database error occurs during deletion.

## Change History

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
