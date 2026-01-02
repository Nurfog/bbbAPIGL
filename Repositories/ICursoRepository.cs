using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.Models;

/// <summary>
/// Interfaz para el repositorio de cursos, proporcionando métodos para la gestión de cursos y sus invitaciones.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 14-10-2025
/// </summary>
namespace bbbAPIGL.Repositories;

public interface ICursoRepository
{
    /// <summary>
    /// Obtiene una lista de correos electrónicos de los alumnos inscritos en un curso.
    /// </summary>
    /// <param name="idCurso">El ID del curso.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una lista de correos electrónicos.</returns>
    Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso);
    /// <summary>
    /// Obtiene una lista de tuplas (IdAlumno, Email) para un curso abierto específico.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una lista de tuplas con el ID del alumno y su correo electrónico.</returns>
    Task<List<(string IdAlumno, string Email)>> ObtenerAlumnosConCorreosPorCursoAsync(int idCursoAbierto);

    /// <summary>
    /// Obtiene el correo electrónico de un alumno específico para un curso abierto.
    /// </summary>
    /// <param name="idAlumno">El ID del alumno.</param>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el correo electrónico del alumno o null si no se encuentra.</returns>
    Task<string?> ObtenerCorreoPorAlumnoAsync(string idAlumno, int idCursoAbierto);

    /// <summary>
    /// Obtiene los datos de la sala asociados a un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto CursoAbiertoSala o null si no se encuentra.</returns>
    Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto);
    /// <summary>
    /// Desasocia una sala de todos los cursos a los que está vinculada.
    /// </summary>
    /// <param name="roomId">El ID de la sala a desasociar.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es true si se desasoció con éxito, false en caso contrario.</returns>
    Task<bool> DesasociarSalaDeCursosAsync(Guid roomId);
    /// <summary>
    /// Actualiza el ID del calendario asociado a un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <param name="idCalendario">El nuevo ID del calendario.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task ActualizarIdCalendarioCursoAsync(int idCursoAbierto, string idCalendario);
    /// <summary>
    /// Guarda una invitación de curso abierto.
    /// </summary>
    /// <param name="invitacion">El objeto CursoAbiertoInvitacion a guardar.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task GuardarInvitacionAsync(CursoAbiertoInvitacion invitacion);
    /// <summary>
    /// Obtiene una invitación de curso abierto por el ID del curso y el ID del alumno.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <param name="idAlumno">El ID del alumno.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto CursoAbiertoInvitacion o null si no se encuentra.</returns>
    Task<CursoAbiertoInvitacion?> ObtenerInvitacionPorCursoAlumnoAsync(int idCursoAbierto, string idAlumno);
    Task<List<CursoAbiertoInvitacion>> ObtenerInvitacionesPorCursoAsync(int idCursoAbierto);
    Task<int> EliminarInvitacionesPorCursoAsync(int idCursoAbierto);
        Task<bool> EliminarCursoAsync(int idCursoAbierto);
        Task<bool> ActualizarHorarioDesdeFuenteExternaAsync(int idCursoAbierto);

    /// <summary>
    /// Obtiene el ID del calendario asociado a un curso a través del RoomId de la sala.
    /// </summary>
    /// <param name="roomId">El ID de la sala (de PostgreSQL).</param>
    /// <returns>El ID del calendario (de MySQL) o null si no se encuentra.</returns>
    Task<string?> ObtenerIdCalendarioPorRoomIdAsync(Guid roomId);

    /// <summary>
    /// Obtiene todas las sesiones de un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una lista de sesiones del curso.</returns>
    Task<List<CursoAbiertoSesion>> ObtenerSesionesPorCursoAsync(int idCursoAbierto);

    /// <summary>
    /// Obtiene todas las sesiones activas de un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una lista de sesiones activas del curso.</returns>
    Task<List<CursoAbiertoSesion>> ObtenerSesionesActivasPorCursoAsync(int idCursoAbierto);
    Task<bool> ReprogramarSesionAsync(int idCursoAbierto, int sesionNumero, DateOnly fechaNuevaSesion, string? idCalendario);
    Task<CursoAbiertoSesion?> ObtenerSesionAsync(int idCursoAbierto, int sesionNumero);
    Task ActualizarHorarioCursoAsync(int idCursoAbierto, DateTime fechaInicio, DateTime fechaTermino, string? dias, DateTime horaInicio, DateTime horaTermino);

    /// <summary>
    /// Guarda los datos de la sala BBB asociados a un curso en la tabla cursosabiertosbbb.
    /// Realiza un insert o un update según corresponda.
    /// </summary>
    Task<bool> GuardarDatosSalaEnCursoAsync(CursoAbiertoSala sala);
}
    