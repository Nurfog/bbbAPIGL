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

    /// <summary>
    /// Registra una invitación/sesión para un curso en el módulo EMP.
    /// </summary>
    Task<bool> CrearInvitacionSesionAsync(int id, DateOnly fecha);

    /// <summary>
    /// Modifica (reprograma) una invitación existente. El registro anterior se marcará como suspendido.
    /// </summary>
    Task<bool> ModificarInvitacionSesionAsync(int id, DateOnly fechaNueva);

    /// <summary>
    /// Suspende una invitación sin crear un nuevo registro.
    /// </summary>
    Task<bool> EliminarInvitacionSesionAsync(int id);

    /// <summary>
    /// Procesa una lista de operaciones (crear/editar/eliminar) para invitaciones EMP.
    /// </summary>
    Task<bool> GestionarInvitacionesAsync(int idCursoAbierto, IEnumerable<OperacionInvitacionEmpresaRequest> operaciones);
}
