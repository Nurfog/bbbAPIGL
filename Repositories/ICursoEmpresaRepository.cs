using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.Models;

namespace bbbAPIGL.Repositories;

public interface ICursoEmpresaRepository
{
    Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso);
    Task<List<(string IdAlumno, string Email)>> ObtenerAlumnosConCorreosPorCursoAsync(int idCursoAbierto);
    Task<string?> ObtenerCorreoPorAlumnoAsync(string idAlumno, int idCursoAbierto);
    Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto);
    Task<bool> DesasociarSalaDeCursosAsync(Guid roomId);
    Task<bool> EliminarCursoAsync(int idCursoAbierto);
    Task<bool> ActualizarHorarioDesdeFuenteExternaAsync(int idCursoAbierto);
    Task<List<CursoAbiertoSesion>> ObtenerSesionesPorCursoAsync(int idCursoAbierto);
    Task<List<CursoAbiertoSesion>> ObtenerSesionesActivasPorCursoAsync(int idCursoAbierto);
    Task<bool> ReprogramarSesionAsync(int idCursoAbierto, int sesionNumero, DateOnly fechaNuevaSesion, string? idCalendario);
    Task<CursoAbiertoSesion?> ObtenerSesionAsync(int idCursoAbierto, int sesionNumero);
    Task ActualizarHorarioCursoAsync(int idCursoAbierto, DateTime fechaInicio, DateTime fechaTermino, string? dias, DateTime horaInicio, DateTime horaTermino);
    Task<bool> GuardarDatosSalaEnCursoAsync(CursoAbiertoSala sala);

    /// <summary>
    /// Inserta un registro en la tabla `sesionescursos`.
    /// Devuelve true si el registro fue creado correctamente.
    /// </summary>
    Task<bool> CrearInvitacionSesionAsync(int id, DateOnly fecha);

    /// <summary>
    /// Marca la invitación/sesión existente como suspendida y agrega una nueva con la fecha indicada.
    /// Este comportamiento imita la reprogramación de sesiones, dejando el registro anterior con
    /// <c>Activo = 0</c> y <c>TipoSesion = 'SUSPENDIDA'</c>.
    /// </summary>
    Task<bool> ModificarInvitacionSesionAsync(int id, DateOnly fechaNueva);

    /// <summary>
    /// Suspende una invitación/sesión activa sin crear uno nuevo.
    /// </summary>
    Task<bool> EliminarInvitacionSesionAsync(int id);
}