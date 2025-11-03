using System;

/// <summary>
/// DTO para la respuesta de creación de una sala.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 05-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class CrearSalaResponse
{
    /// <summary>
    /// Obtiene o establece el ID único de la sala.
    /// </summary>
    public Guid RoomId { get; set; }
    /// <summary>
    /// Obtiene o establece la URL de acceso a la sala.
    /// </summary>
    public string UrlSala { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece la clave de moderador para la sala.
    /// </summary>
    public string ClaveModerador { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece la clave de espectador para la sala.
    /// </summary>
    public string ClaveEspectador { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece el ID de la reunión.
    /// </summary>
    public string MeetingId { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece un ID amigable para la sala.
    /// </summary>
    public string FriendlyId { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece el ID de grabación de la sala.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece el nombre de la sala.
    /// </summary>
    public string? NombreSala { get; set; }
}