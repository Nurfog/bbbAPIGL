using bbbAPIGL.DTOs;

/// <summary>
/// Interfaz para el servicio de gestión de salas.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 23-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public interface ISalaService
{
    /// <summary>
    /// Crea una nueva sala.
    /// </summary>
    /// <param name="request">La solicitud para crear la sala.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto CrearSalaResponse.</returns>
    Task<CrearSalaResponse> CrearNuevaSalaAsync(CrearSalaRequest request);
    /// <summary>
    /// Elimina una sala existente.
    /// </summary>
    /// <param name="roomId">El ID de la sala a eliminar.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es true si la sala fue eliminada con éxito, false en caso contrario.</returns>
    Task<bool> EliminarSalaAsync(Guid roomId);
    /// <summary>
    /// Envía invitaciones para un curso.
    /// </summary>
    /// <param name="request">La solicitud para enviar invitaciones al curso.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto EnviarInvitacionCursoResponse.</returns>
    Task<EnviarInvitacionCursoResponse> EnviarInvitacionesCursoAsync(EnviarInvitacionCursoRequest request);
    /// <summary>
    /// Obtiene las URLs de grabación para un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una lista de objetos GrabacionDto o null si no se encuentran grabaciones.</returns>
    Task<List<GrabacionDto>?> ObtenerUrlsGrabacionesAsync(int idCursoAbierto);
    Task<EnviarInvitacionCursoResponse> EnviarInvitacionIndividualAsync(EnviarInvitacionIndividualRequest request);
    Task<EnviarInvitacionCursoResponse> ActualizarInvitacionesCursoAsync(ActualizarEventoCalendarioRequest request);
    Task<bool> EliminarCursoAsync(int idCursoAbierto);
    Task<bool> ReprogramarSesionAsync(ReprogramarSesionRequest request);
    Task SincronizarCalendarioAsync(int idCursoAbierto);
}
