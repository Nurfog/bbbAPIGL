using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.Models;

namespace bbbAPIGL.Repositories;

public interface ICursoRepository
{
    Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso);

    Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto);
    Task<bool> DesasociarSalaDeCursosAsync(Guid roomId);
}