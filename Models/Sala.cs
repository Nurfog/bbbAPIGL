/// <summary>
/// Representa una sala de reuniones.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 13-10-2025
/// </summary>
namespace bbbAPIGL.Models;

public class Sala
{
    /// <summary>
    /// Obtiene o establece el nombre de la sala.
    /// </summary>
    public required string Nombre { get; set; }
    /// <summary>
    /// Obtiene o establece el ID de la reunión.
    /// </summary>
    public required string MeetingId { get; set; }
    /// <summary>
    /// Obtiene o establece el ID amigable de la sala.
    /// </summary>
    public required string FriendlyId { get; set; }
    /// <summary>
    /// Obtiene o establece la clave de moderador de la sala.
    /// </summary>
    public required string ClaveModerador { get; set; }
    /// <summary>
    /// Obtiene o establece la clave de espectador de la sala.
    /// </summary>
    public required string ClaveEspectador { get; set; }
    /// <summary>
    /// Obtiene o establece el ID del calendario asociado.
    /// </summary>
    public string? IdCalendario { get; set; }
}
