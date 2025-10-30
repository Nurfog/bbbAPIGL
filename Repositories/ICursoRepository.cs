using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.Models;

namespace bbbAPIGL.Repositories;

public interface ICursoRepository
{
    Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso);
    Task<List<(int IdAlumno, string Email)>> ObtenerAlumnosConCorreosPorCursoAsync(int idCursoAbierto);

    Task<string?> ObtenerCorreoPorAlumnoAsync(string idAlumno, int idCursoAbierto);

    Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto);
    Task<bool> DesasociarSalaDeCursosAsync(Guid roomId);
    Task ActualizarIdCalendarioCursoAsync(int idCursoAbierto, string idCalendario);
    Task GuardarInvitacionAsync(CursoAbiertoInvitacion invitacion);
    Task<CursoAbiertoInvitacion?> ObtenerInvitacionPorCursoAlumnoAsync(int idCursoAbierto, int idAlumno);
}