using System;
using System.Threading.Tasks;
using bbbAPIGL.Models;

/// <summary>
/// Interfaz para el repositorio de salas, proporcionando métodos para la gestión de salas.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 15-10-2025
/// </summary>
namespace bbbAPIGL.Repositories;

public interface ISalaRepository

{

    /// <summary>
    /// Guarda una nueva sala en el repositorio.
    /// </summary>
    /// <param name="sala">El objeto Sala a guardar.</param>
    /// <param name="userEmail">El correo electrónico del usuario que crea la sala.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el GUID de la sala guardada o null si falla.</returns>
    Task<Guid?> GuardarSalaAsync(Sala sala, string userEmail);

    /// <summary>
    /// Elimina una sala del repositorio por su ID.
    /// </summary>
    /// <param name="roomId">El ID de la sala a eliminar.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es true si la sala fue eliminada con éxito, false en caso contrario.</returns>
    Task<bool> EliminarSalaAsync(Guid roomId);

    /// <summary>
    /// Obtiene una sala del repositorio por su ID.
    /// </summary>
    /// <param name="roomId">El ID de la sala a obtener.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el objeto Sala o null si no se encuentra.</returns>
    Task<Sala?> ObtenerSalaPorIdAsync(Guid roomId);

    /// <summary>
    /// Obtiene todos los IDs de grabación asociados a un RoomId específico.
    /// </summary>
    /// <param name="roomId">El ID de la sala.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una lista de objetos RecordingInfo.</returns>
    Task<List<RecordingInfo>> ObtenerTodosLosRecordIdsPorRoomIdAsync(Guid roomId);

}
