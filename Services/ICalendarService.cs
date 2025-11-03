using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using bbbAPIGL.DTOs;

/// <summary>
/// Interfaz para el servicio de calendario, proporcionando métodos para la gestión de eventos.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 21-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public interface ICalendarService
{
    /// <summary>
    /// Envía una invitación de calendario no recurrente.
    /// </summary>
    /// <param name="detallesSala">Detalles de la sala.</param>
    /// <param name="correosParticipantes">Lista de correos electrónicos de los participantes.</param>
    /// <returns>El ID del evento de calendario creado o null si falla.</returns>
    Task<string?> EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala,
        List<string> correosParticipantes);
    /// <summary>
    /// Envía una invitación de calendario recurrente.
    /// </summary>
    /// <param name="detallesSala">Detalles de la sala.</param>
    /// <param name="correosParticipantes">Lista de correos electrónicos de los participantes.</param>
    /// <param name="fechaInicio">Fecha de inicio del evento.</param>
    /// <param name="fechaTermino">Fecha de término del evento.</param>
    /// <param name="diasSemana">Días de la semana en que se repite el evento (ej. "LU,MI,VI").</param>
    /// <param name="horaInicio">Hora de inicio del evento.</param>
    /// <param name="horaTermino">Hora de término del evento.</param>
    /// <returns>El ID del evento de calendario creado o null si falla.</returns>
    Task<string?> EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala, 
        List<string> correosParticipantes, 
        DateTime fechaInicio, 
        DateTime fechaTermino, 
        string diasSemana,
        DateTime horaInicio,
        DateTime horaTermino);
    /// <summary>
    /// Elimina un evento de calendario.
    /// </summary>
    /// <param name="eventoId">El ID del evento de calendario a eliminar.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EliminarEventoCalendarioAsync(string eventoId);
    /// <summary>
    /// Actualiza un evento de calendario existente.
    /// </summary>
    /// <param name="eventoId">El ID del evento de calendario a actualizar.</param>
    /// <param name="detallesSala">Detalles de la sala para la invitación.</param>
    /// <param name="correosParticipantes">Lista de correos electrónicos de los participantes.</param>
    /// <param name="fechaInicio">Fecha de inicio del evento.</param>
    /// <param name="fechaTermino">Fecha de término del evento.</param>
    /// <param name="diasSemana">Días de la semana en que se repite el evento (ej. "LU,MI,VI").</param>
    /// <param name="horaInicio">Hora de inicio del evento.</param>
    /// <param name="horaTermino">Hora de término del evento.</param>
    /// <returns>El ID del evento de calendario actualizado o null si falla.</returns>
    Task<string?> ActualizarEventoCalendarioAsync(string eventoId, CrearSalaResponse detallesSala, List<string> correosParticipantes, DateTime fechaInicio, DateTime fechaTermino, string diasSemana, DateTime horaInicio, DateTime horaTermino);
}
