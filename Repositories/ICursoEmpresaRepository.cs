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
}