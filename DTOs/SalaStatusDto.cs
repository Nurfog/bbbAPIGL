namespace bbbAPIGL.DTOs;

/// <summary>
/// DTO para el estado de una sala en los diferentes sistemas.
/// </summary>
public class SalaStatusDto
{
    /// <summary>
    /// Indica si el curso existe en la tabla cursosabiertosbbb de SAM (MySQL).
    /// </summary>
    public bool ExisteEnSam { get; set; }

    /// <summary>
    /// Indica si la sala existe en la tabla rooms de Greenlight (Postgres).
    /// </summary>
    public bool ExisteEnGreenlight { get; set; }

    /// <summary>
    /// Indica si hay una sesión activa en el servidor BigBlueButton.
    /// </summary>
    public bool EstaActivaEnBBB { get; set; }

    /// <summary>
    /// URL de la sala para unirse.
    /// </summary>
    public string? UrlSala { get; set; }

    /// <summary>
    /// Detalles adicionales sobre la sala.
    /// </summary>
    public SalaDetallesDto? Detalles { get; set; }
}

/// <summary>
/// Detalles adicionales de la sala.
/// </summary>
public class SalaDetallesDto
{
    public int IdCursoAbierto { get; set; }
    public string? RoomId { get; set; }
    public string? MeetingId { get; set; }
    public string? FriendlyId { get; set; }
    public string? NombreSala { get; set; }
    public DateTime? FechaCreacion { get; set; }
}
