using bbbAPIGL.DTOs;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Servicio para la gestión de eventos en Google Calendar.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 20-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public class GoogleCalendarService : ICalendarService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Envía una invitación de calendario no recurrente para una sala.
    /// </summary>
    /// <param name="detallesSala">Detalles de la sala para la invitación.</param>
    /// <param name="correosParticipantes">Lista de correos electrónicos de los participantes.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el ID del evento de calendario creado o null si falla.</returns>
    public async Task<string?> EnviarInvitacionCalendarioAsync(CrearSalaResponse detallesSala, List<string> correosParticipantes)
    {
        try
        {
            _logger.LogInformation("Enviando invitación de calendario (no recurrente) para la sala {salaNombre}.", detallesSala.NombreSala);
            var service = await GetCalendarServiceAsync();
            var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";

            var newEvent = CrearEventoBase(detallesSala, correosParticipantes, timeZone);
            newEvent.Start = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow, TimeZone = timeZone };
            newEvent.End = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow.AddHours(1), TimeZone = timeZone };

            _logger.LogDebug("Creando evento en calendario: Summary='{summary}', Start='{start}', End='{end}'", newEvent.Summary, newEvent.Start.DateTimeDateTimeOffset, newEvent.End.DateTimeDateTimeOffset);

            var request = service.Events.Insert(newEvent, calendarId);
            request.SendNotifications = true;

            var createdEvent = await request.ExecuteAsync();
            _logger.LogInformation("Evento de calendario (no recurrente) creado exitosamente con ID: {eventId}", createdEvent.Id);
            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar la invitación de calendario (no recurrente).");
            throw;
        }
    }

    /// <summary>
    /// Envía una invitación de calendario recurrente para una sala.
    /// </summary>
    /// <param name="detallesSala">Detalles de la sala para la invitación.</param>
    /// <param name="correosParticipantes">Lista de correos electrónicos de los participantes.</param>
    /// <param name="fechaInicio">Fecha de inicio del evento.</param>
    /// <param name="fechaTermino">Fecha de término del evento.</param>
    /// <param name="diasSemana">Días de la semana en que se repite el evento (ej. "LU,MI,VI").</param>
    /// <param name="horaInicio">Hora de inicio del evento.</param>
    /// <param name="horaTermino">Hora de término del evento.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el ID del evento de calendario creado o null si falla.</returns>
    public async Task<string?> EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala, 
        List<string> correosParticipantes,
        DateTime fechaInicio, 
        DateTime fechaTermino, 
        string diasSemana,
        DateTime horaInicio,
        DateTime horaTermino,
        bool sendNotifications = true)
    {
        try
        {
            _logger.LogInformation("Enviando invitación de calendario recurrente para la sala {salaNombre}.", detallesSala.NombreSala);
            var service = await GetCalendarServiceAsync();
            var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";

            var diasRrule = ConvertirDiasParaRRule(diasSemana);
            if (string.IsNullOrEmpty(diasRrule))
            {
                _logger.LogWarning("No se enviará invitación porque no se proporcionaron días válidos para la recurrencia ('{diasSemana}').", diasSemana);
                return null;
            }

            var recurrenceRule = $"RRULE:FREQ=WEEKLY;UNTIL={fechaTermino:yyyyMMddTHHMMssZ};BYDAY={diasRrule}";
            
            var newEvent = CrearEventoBase(detallesSala, correosParticipantes, timeZone);

            var eventStartDateTime = fechaInicio.Date.Add(horaInicio.TimeOfDay);
            var eventEndDateTime = fechaInicio.Date.Add(horaTermino.TimeOfDay);

            newEvent.Start = new EventDateTime { DateTimeDateTimeOffset = eventStartDateTime, TimeZone = timeZone };
            newEvent.End = new EventDateTime { DateTimeDateTimeOffset = eventEndDateTime, TimeZone = timeZone };
            newEvent.Recurrence = new List<string> { recurrenceRule };

            _logger.LogDebug("Creando evento recurrente en calendario: Summary='{summary}', Start='{start}', End='{end}', Recurrence='{recurrence}'", newEvent.Summary, newEvent.Start.DateTimeDateTimeOffset, newEvent.End.DateTimeDateTimeOffset, newEvent.Recurrence.FirstOrDefault());

            var request = service.Events.Insert(newEvent, calendarId);
            request.SendNotifications = sendNotifications;

            var createdEvent = await request.ExecuteAsync();
            _logger.LogInformation("Evento de calendario recurrente creado exitosamente con ID: {eventId}", createdEvent.Id);
            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar la invitación de calendario recurrente.");
            throw;
        }
    }

    /// <summary>
    /// Elimina un evento de calendario existente.
    /// </summary>
    /// <param name="eventoId">El ID del evento de calendario a eliminar.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task EliminarEventoCalendarioAsync(string eventoId, bool sendNotifications = true)
    {
        try
        {
            _logger.LogInformation("Eliminando evento de calendario con ID: {eventoId}", eventoId);
            var service = await GetCalendarServiceAsync();
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";
            var request = service.Events.Delete(calendarId, eventoId);
            request.SendNotifications = sendNotifications;
            await request.ExecuteAsync();
            _logger.LogInformation("Evento de calendario eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el evento de calendario.");
            throw;
        }
    }

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
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el ID del evento de calendario actualizado o null si falla.</returns>
    public async Task<string?> ActualizarEventoCalendarioAsync(string eventoId, CrearSalaResponse detallesSala, List<string> correosParticipantes, DateTime fechaInicio, DateTime fechaTermino, string diasSemana, DateTime horaInicio, DateTime horaTermino,
        bool sendNotifications = true)
    {
        try
        {
            _logger.LogInformation("Actualizando invitación de calendario recurrente para la sala {salaNombre}.", detallesSala.NombreSala);
            var service = await GetCalendarServiceAsync();
            var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";

            var diasRrule = ConvertirDiasParaRRule(diasSemana);
            if (string.IsNullOrEmpty(diasRrule))
            {
                _logger.LogWarning("No se actualizará la invitación porque no se proporcionaron días válidos para la recurrencia ('{diasSemana}').", diasSemana);
                return null;
            }

            var recurrenceRule = $"RRULE:FREQ=WEEKLY;UNTIL={fechaTermino:yyyyMMddTHHMMssZ};BYDAY={diasRrule}";

            var existingEvent = await service.Events.Get(calendarId, eventoId).ExecuteAsync();

            existingEvent.Summary = $"Clase: {detallesSala.NombreSala ?? detallesSala.FriendlyId}";
            existingEvent.Location = detallesSala.UrlSala;
            existingEvent.Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave Espectador: {detallesSala.ClaveEspectador}";
            existingEvent.Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList();
            
            var eventStartDateTime = fechaInicio.Date.Add(horaInicio.TimeOfDay);
            var eventEndDateTime = fechaInicio.Date.Add(horaTermino.TimeOfDay);

            existingEvent.Start = new EventDateTime { DateTimeDateTimeOffset = eventStartDateTime, TimeZone = timeZone };
            existingEvent.End = new EventDateTime { DateTimeDateTimeOffset = eventEndDateTime, TimeZone = timeZone };
            existingEvent.Recurrence = new List<string> { recurrenceRule };

            _logger.LogDebug("Actualizando evento recurrente en calendario: Summary='{summary}', Start='{start}', End='{end}', Recurrence='{recurrence}'", existingEvent.Summary, existingEvent.Start.DateTimeDateTimeOffset, existingEvent.End.DateTimeDateTimeOffset, existingEvent.Recurrence.FirstOrDefault());

            var request = service.Events.Update(existingEvent, calendarId, eventoId);
            request.SendNotifications = sendNotifications;

            var updatedEvent = await request.ExecuteAsync();
            _logger.LogInformation("Evento de calendario recurrente actualizado exitosamente con ID: {eventId}", updatedEvent.Id);
            return updatedEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar la invitación de calendario recurrente.");
            throw;
        }
    }

    /// <summary>
    /// Crea un objeto Event base con los detalles comunes para un evento de calendario.
    /// </summary>
    /// <param name="detallesSala">Detalles de la sala.</param>
    /// <param name="correosParticipantes">Lista de correos electrónicos de los participantes.</param>
    /// <param name="timeZone">La zona horaria del evento.</param>
    /// <returns>Un objeto Event con los detalles base configurados.</returns>
    private Event CrearEventoBase(CrearSalaResponse detallesSala, List<string> correosParticipantes, string timeZone)
    {
        return new Event
        {
            Summary = $"Clase: {detallesSala.NombreSala ?? detallesSala.FriendlyId}",
            Location = detallesSala.UrlSala,
            Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave Espectador: {detallesSala.ClaveEspectador}",
            Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList(),
            Reminders = new Event.RemindersData { UseDefault = true }
        };
    }

    /// <summary>
    /// Convierte una cadena de días de la semana (ej. "LU,MI,VI") a un formato compatible con RRULE de Google Calendar (ej. "MO,WE,FR").
    /// </summary>
    /// <param name="dias">Cadena de días de la semana en español.</param>
    /// <returns>Cadena de días de la semana en formato RRULE.</returns>
    private string ConvertirDiasParaRRule(string dias)
    {
        var mapeoDias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "LU", "MO" }, { "MA", "TU" }, { "MI", "WE" }, { "JU", "TH" }, { "VI", "FR" }, { "SA", "SA" }, { "DO", "SU" }
        };

        var diasSplit = dias.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        return string.Join(",", diasSplit
            .Select(dia => dia.Trim())
            .Where(dia => mapeoDias.ContainsKey(dia))
            .Select(dia => mapeoDias[dia]));
    }

    /// <summary>
    /// Obtiene una instancia de CalendarService autenticada.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una instancia de CalendarService.</returns>
    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        _logger.LogInformation("Obteniendo servicio de Google Calendar.");
        var credential = await GoogleAuthService.GetGoogleCredentialAsync(_configuration, _logger, CalendarService.Scope.Calendar);
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "bbbAPIGL API"
        });
    }

    public async Task<string?> ReprogramarSesionCalendarioAsync(string eventoId, DateTime fechaOriginal, DateTime nuevaFecha)
    {
        try
        {
            _logger.LogInformation("Iniciando reprogramación de sesión en calendario para el evento {eventoId} de la fecha {fechaOriginal} a {nuevaFecha}.", eventoId, fechaOriginal.ToString("yyyy-MM-dd"), nuevaFecha.ToString("yyyy-MM-dd"));
            var service = await GetCalendarServiceAsync();
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";

            // 1. Obtener el evento recurrente maestro para tener los detalles
            var recurringEvent = await service.Events.Get(calendarId, eventoId).ExecuteAsync();
            if (recurringEvent == null)
            {
                _logger.LogError("No se encontró el evento recurrente maestro con ID {eventoId}.", eventoId);
                throw new InvalidOperationException($"No se encontró el evento recurrente maestro con ID {eventoId}.");
            }

            // 2. Encontrar y cancelar la instancia específica del evento recurrente
            var instancesRequest = service.Events.Instances(calendarId, eventoId);
            instancesRequest.ShowDeleted = true; // Incluir instancias canceladas en el resultado
            var instances = await instancesRequest.ExecuteAsync();
            
            var instanceToCancel = instances.Items.FirstOrDefault(i =>
            {
                if (i.Start.DateTimeDateTimeOffset.HasValue)
                {
                    return i.Start.DateTimeDateTimeOffset.Value.Date == fechaOriginal.Date;
                }
                if (!string.IsNullOrEmpty(i.Start.Date))
                {
                    return DateTime.Parse(i.Start.Date).Date == fechaOriginal.Date;
                }
                return false;
            });

            if (instanceToCancel != null && instanceToCancel.Status != "cancelled")
            {
                instanceToCancel.Status = "cancelled";
                await service.Events.Update(instanceToCancel, calendarId, instanceToCancel.Id).ExecuteAsync();
                _logger.LogInformation("Instancia del evento con ID de instancia {instanceId} (del evento maestro {eventoId}) en la fecha {fechaOriginal} fue cancelada.", instanceToCancel.Id, eventoId, fechaOriginal.ToString("yyyy-MM-dd"));
            }
            else if (instanceToCancel != null)
            {
                _logger.LogInformation("La instancia del evento en la fecha {fechaOriginal} ya estaba cancelada.", fechaOriginal.ToString("yyyy-MM-dd"));
            }
            else
            {
                _logger.LogWarning("No se encontró una instancia del evento {eventoId} en la fecha {fechaOriginal} para cancelar. Se continuará con la creación del nuevo evento.", eventoId, fechaOriginal.ToString("yyyy-MM-dd"));
            }

            // 3. Crear un nuevo evento único para la nueva fecha
            if (recurringEvent.Start?.DateTimeDateTimeOffset == null || recurringEvent.End?.DateTimeDateTimeOffset == null)
            {
                _logger.LogError("El evento recurrente maestro con ID {eventoId} no tiene fechas de inicio o fin válidas.", eventoId);
                throw new InvalidOperationException($"El evento recurrente maestro con ID {eventoId} no tiene fechas de inicio o fin válidas.");
            }
            
            var horaInicio = recurringEvent.Start.DateTimeDateTimeOffset.Value.TimeOfDay;
            var horaTermino = recurringEvent.End.DateTimeDateTimeOffset.Value.TimeOfDay;
            var timeZone = recurringEvent.Start.TimeZone;
            var startOffset = recurringEvent.Start.DateTimeDateTimeOffset.Value.Offset;
            var newStart = new DateTimeOffset(nuevaFecha.Year, nuevaFecha.Month, nuevaFecha.Day, horaInicio.Hours, horaInicio.Minutes, horaInicio.Seconds, startOffset);
            var endOffset = recurringEvent.End.DateTimeDateTimeOffset.Value.Offset;
            var newEnd = new DateTimeOffset(nuevaFecha.Year, nuevaFecha.Month, nuevaFecha.Day, horaTermino.Hours, horaTermino.Minutes, horaTermino.Seconds, endOffset);

            var newEvent = new Event
            {
                Summary = recurringEvent.Summary,
                Location = recurringEvent.Location,
                Description = recurringEvent.Description,
                Attendees = recurringEvent.Attendees,
                Reminders = recurringEvent.Reminders,
                Start = new EventDateTime { DateTimeDateTimeOffset = newStart, TimeZone = timeZone },
                End = new EventDateTime { DateTimeDateTimeOffset = newEnd, TimeZone = timeZone }
            };

            var request = service.Events.Insert(newEvent, calendarId);
            request.SendNotifications = true;
            var createdEvent = await request.ExecuteAsync();

            _logger.LogInformation("Nueva sesión única creada con ID {createdEventId} para el evento maestro {eventoId} en la fecha {nuevaFecha}.", createdEvent.Id, eventoId, nuevaFecha.ToString("yyyy-MM-dd"));
            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reprogramar la sesión en el calendario para el evento {eventoId}.", eventoId);
            throw;
        }
    }

    /// <summary>
    /// Convierte un objeto DateTime a la zona horaria especificada.
    /// </summary>
    /// <param name="dateTime">El objeto DateTime a convertir.</param>
    /// <param name="targetTimeZoneId">El ID de la zona horaria de destino (ej. "America/Santiago").</param>
    /// <returns>El objeto DateTime convertido a la zona horaria de destino.</returns>
    private DateTime ConvertToTimeZone(DateTime dateTime, string targetTimeZoneId)
    {
        TimeZoneInfo targetTimeZone = TimeZoneInfo.FindSystemTimeZoneById(targetTimeZoneId);

        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            // Si el tipo es Unspecified, asumimos que está en la zona horaria local del servidor
            // antes de convertirlo a la zona horaria de destino.
            return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, targetTimeZone);
        }
        else if (dateTime.Kind == DateTimeKind.Local)
        {
            return TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, targetTimeZone);
        }
        else // DateTimeKind.Utc
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, targetTimeZone);
        }
    }

    public async Task<IList<Event>> ObtenerInstanciasDeEventoAsync(string eventoId)
    {
        try
        {
            _logger.LogInformation("Obteniendo todas las instancias para el evento {eventoId}.", eventoId);
            var service = await GetCalendarServiceAsync();
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";
            var request = service.Events.Instances(calendarId, eventoId);
            var instances = await request.ExecuteAsync();
            return instances.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener las instancias del evento {eventoId}.", eventoId);
            throw;
        }
    }

    public async Task CancelarInstanciaAsync(Event instancia, bool sendNotifications = true)
    {
        try
        {
            _logger.LogInformation("Cancelando la instancia de evento con ID {instanciaId}.", instancia.Id);
            var service = await GetCalendarServiceAsync();
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";
            
            instancia.Status = "cancelled";
            
            var request = service.Events.Update(instancia, calendarId, instancia.Id);
            request.SendNotifications = sendNotifications;
            await request.ExecuteAsync();
            _logger.LogInformation("Instancia de evento {instanciaId} cancelada exitosamente.", instancia.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cancelar la instancia de evento {instanciaId}.", instancia.Id);
            throw;
        }
    }

    public async Task<string> CrearEventoUnicoAsync(Event evento, bool sendNotifications = true)
    {
        try
        {
            _logger.LogInformation("Creando un nuevo evento único: {summary}", evento.Summary);
            var service = await GetCalendarServiceAsync();
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";
            
            var request = service.Events.Insert(evento, calendarId);
            request.SendNotifications = sendNotifications;
            var createdEvent = await request.ExecuteAsync();
            _logger.LogInformation("Nuevo evento único creado con ID {eventId}.", createdEvent.Id);
            return createdEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el evento único.");
            throw;
        }
    }

    public async Task<Event> ObtenerEventoAsync(string eventoId)
    {
        try
        {
            _logger.LogInformation("Obteniendo el evento con ID {eventoId}.", eventoId);
            var service = await GetCalendarServiceAsync();
            var calendarId = _configuration["GoogleCalendarSettings:CalendarId"] ?? "primary";
            var request = service.Events.Get(calendarId, eventoId);
            var evento = await request.ExecuteAsync();
            return evento;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el evento {eventoId}.", eventoId);
            throw;
        }
    }
}