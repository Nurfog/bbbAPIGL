using bbbAPIGL.DTOs;

namespace bbbAPIGL.Services;

public interface ISalaService
{
    Task<CrearSalaResponse> CrearNuevaSalaAsync(CrearSalaRequest request);
    Task<bool> EliminarSalaAsync(Guid roomId);
    Task<EnviarInvitacionCursoResponse> EnviarInvitacionesCursoAsync(EnviarInvitacionCursoRequest request);
    Task<List<GrabacionDto>?> ObtenerUrlsGrabacionesAsync(int idCursoAbierto);
    Task<EnviarInvitacionCursoResponse> EnviarInvitacionIndividualAsync(EnviarInvitacionIndividualRequest request);
}
