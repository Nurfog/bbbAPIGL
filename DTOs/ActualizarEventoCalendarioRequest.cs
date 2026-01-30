using System;
using System.Collections.Generic;

/// <summary>
/// DTO para la solicitud de actualización de un evento de calendario.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 03-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class ActualizarEventoCalendarioRequest
{
    /// <summary>
    /// Obtiene o establece el ID del curso abierto.
    /// </summary>
    public int IdCursoAbierto { get; set; }
    /// <summary>
    /// Obtiene o establece la lista de correos electrónicos de los participantes.
    /// </summary>
    public List<string>? CorreosParticipantes { get; set; }
    /// <summary>
    /// Obtiene o establece la fecha de inicio del evento.
    /// </summary>
    public DateTime? FechaInicio { get; set; }
    /// <summary>
    /// Obtiene o establece la fecha de término del evento.
    /// </summary>
    public DateTime? FechaTermino { get; set; }
    /// <summary>
    /// Obtiene o establece los días de la semana en que se repite el evento.
    /// </summary>
    public string? DiasSemana { get; set; }
    /// <summary>
    /// Obtiene o establece la hora de inicio del evento.
    /// </summary>
    public DateTime? HoraInicio { get; set; }
    /// <summary>
    /// Obtiene o establece la hora de término del evento.
    /// </summary>
    public DateTime? HoraTermino { get; set; }
}