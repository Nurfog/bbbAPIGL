using bbbAPIGL.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace bbbAPIGL.Services;

public interface ISalaEmpresaService
{
    Task<CrearSalaResponse> CrearNuevaSalaAsync(CrearSalaRequest request);
    Task<bool> EliminarSalaAsync(Guid roomId);
    Task<List<GrabacionDto>?> ObtenerUrlsGrabacionesAsync(int idCursoAbierto);
    Task<bool> EliminarCursoAsync(int idCursoAbierto);
    Task<SalaStatusDto> ObtenerEstadoSalaAsync(int idCursoAbierto);
    Task<bool> ReprogramarSesionAsync(ReprogramarSesionRequest request);
}
