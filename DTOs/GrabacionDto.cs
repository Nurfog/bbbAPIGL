/// <summary>
/// DTO para la información de una grabación.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 09-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class GrabacionDto
{
    /// <summary>
    /// Obtiene o establece el ID de la grabación.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece la URL de reproducción de la grabación.
    /// </summary>
    public string PlaybackUrl { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece la fecha de creación de la grabación.
    /// </summary>
    public string CreatedAt { get; set; } = string.Empty;
}