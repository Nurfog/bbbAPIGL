using System;

/// <summary>
/// Representa una sala de curso abierto con sus detalles asociados.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 11-10-2025
/// </summary>
namespace bbbAPIGL.Models;

public class CursoAbiertoSala
{
    /// <summary>
    /// Obtiene o establece el ID del curso abierto.
    /// </summary>
    public int IdCursoAbierto { get; set; }
    /// <summary>
    /// Obtiene o establece el ID de la sala.
    /// </summary>
    public string? RoomId { get; set; }
    /// <summary>
    /// Obtiene o establece la URL de la sala.
    /// </summary>
    public string? UrlSala { get; set; }
    /// <summary>
    /// Obtiene o establece la clave de moderador de la sala.
    /// </summary>
    public string? ClaveModerador { get; set; }
    /// <summary>
    /// Obtiene o establece la clave de espectador de la sala.
    /// </summary>
    public string? ClaveEspectador { get; set; }
    /// <summary>
    /// Obtiene o establece el ID de la reunión.
    /// </summary>
    public string? MeetingId { get; set; }
    /// <summary>
    /// Obtiene o establece el ID amigable de la sala.
    /// </summary>
    public string? FriendlyId { get; set; }
    /// <summary>
    /// Obtiene o establece el ID de grabación de la sala.
    /// </summary>
    public string? RecordId { get; set; }
    /// <summary>
    /// Obtiene o establece el nombre de la sala.
    /// </summary>
    public string? NombreSala { get; set; }
    /// <summary>
    /// Obtiene o establece el ID del calendario asociado.
    /// </summary>
    public string? IdCalendario { get; set; }
    /// <summary>
    /// Obtiene o establece la fecha de inicio del curso.
    /// </summary>
    public DateTime FechaInicio { get; set; }
    /// <summary>
    /// Obtiene o establece la fecha de término del curso.
    /// </summary>
    public DateTime FechaTermino { get; set; }
    /// <summary>
    /// Obtiene o establece los días de la semana en que se imparte el curso.
    /// </summary>
    public string? Dias { get; set; }
    /// <summary>
    /// Obtiene o establece la hora de inicio del curso.
    /// </summary>
    public DateTime HoraInicio { get; set; }
    /// <summary>
    /// Obtiene o establece la hora de término del curso.
    /// </summary>
    public DateTime HoraTermino { get; set; }
}
