using bbbAPIGL.DTOs;
using Google.Apis.Auth.OAuth2;
using System;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Text;
using Microsoft.Extensions.Logging;

namespace bbbAPIGL.Services;

public class GoogleCalendarService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(IConfiguration configuration, ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> EnviarInvitacionCalendarioAsync(CrearSalaResponse detallesSala, List<string> correosParticipantes)
    {
        try
        {
            _logger.LogInformation("Enviando invitación de calendario (no recurrente) para la sala {salaNombre}.", detallesSala.NombreSala);
            var service = await GetCalendarServiceAsync();
            var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";

            var newEvent = CrearEventoBase(detallesSala, correosParticipantes, timeZone);
            newEvent.Start = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow, TimeZone = timeZone };
            newEvent.End = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow.AddHours(1), TimeZone = timeZone };

            _logger.LogDebug("Creando evento en calendario: Summary='{summary}', Start='{start}', End='{end}'", newEvent.Summary, newEvent.Start.DateTimeDateTimeOffset, newEvent.End.DateTimeDateTimeOffset);

            var request = service.Events.Insert(newEvent, "primary");
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

    public async Task<string?> EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala, 
        List<string> correosParticipantes,
        DateTime fechaInicio, 
        DateTime fechaTermino, 
        string diasSemana,
        DateTime horaInicio,
        DateTime horaTermino)
    {
        try
        {
            _logger.LogInformation("Enviando invitación de calendario recurrente para la sala {salaNombre}.", detallesSala.NombreSala);
            var service = await GetCalendarServiceAsync();
            var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";

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

            var request = service.Events.Insert(newEvent, "primary");
            request.SendNotifications = true;

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

    public async Task EliminarEventoCalendarioAsync(string eventoId)
    {
        try
        {
            _logger.LogInformation("Eliminando evento de calendario con ID: {eventoId}", eventoId);
            var service = await GetCalendarServiceAsync();
            var request = service.Events.Delete("primary", eventoId);
            request.SendNotifications = true;
            await request.ExecuteAsync();
            _logger.LogInformation("Evento de calendario eliminado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el evento de calendario.");
            throw;
        }
    }

    private Event CrearEventoBase(CrearSalaResponse detallesSala, List<string> correosParticipantes, string timeZone)
    {
        return new Event
        {
            Summary = $"Clase: {detallesSala.NombreSala ?? detallesSala.FriendlyId}",
            Location = detallesSala.UrlSala,
            Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave Moderador: {detallesSala.ClaveModerador}\nClave Espectador: {detallesSala.ClaveEspectador}",
            Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList(),
            Reminders = new Event.RemindersData { UseDefault = true }
        };
    }

    private string ConvertirDiasParaRRule(string dias)
    {
        var mapeoDias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "LU", "MO" }, { "MA", "TU" }, { "MI", "WE" }, { "JU", "TH" }, { "VI", "FR" }, { "SA", "SA" }, { "DO", "SU" }
        };

        var diasSplit = dias.Split(new[] { ',' , ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        return string.Join(",", diasSplit
            .Select(dia => dia.Trim())
            .Where(dia => mapeoDias.ContainsKey(dia))
            .Select(dia => mapeoDias[dia]));
    }

    public async Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml)
    {
        try
        {
            _logger.LogInformation("Enviando correo a {count} destinatarios con asunto: {asunto}", destinatarios.Count, asunto);
            var service = await GetGmailServiceAsync();
            var fromEmail = _configuration["GoogleCalendarSettings:UserToImpersonate"];
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("El correo del remitente (UserToImpersonate) no está configurado.");
                throw new ArgumentException("El correo del remitente (UserToImpersonate) no está configurado correctamente.");
            }

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail),
                Subject = asunto,
                Body = cuerpoHtml,
                IsBodyHtml = true
            };
            destinatarios.ForEach(email => mailMessage.Bcc.Add(email));

            var mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mailMessage);
            
            var rawMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(mimeMessage.ToString()))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
            
            var message = new Message { Raw = rawMessage };
            await service.Users.Messages.Send(message, fromEmail).ExecuteAsync();
            _logger.LogInformation("Correo enviado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo.");
            throw;
        }
    }

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        _logger.LogInformation("Obteniendo servicio de Google Calendar.");
        var settings = _configuration.GetSection("GoogleCalendarSettings");
        var credentialsFilePath = settings["CredentialsFile"];
        var userToImpersonate = settings["UserToImpersonate"];

        _logger.LogDebug("Ruta de archivo de credenciales: {path}", credentialsFilePath);
        _logger.LogDebug("Usuario a suplantar: {user}", userToImpersonate);

        if (string.IsNullOrWhiteSpace(credentialsFilePath))
        {
            _logger.LogError("El archivo de credenciales (CredentialsFile) no está configurado.");
            throw new ArgumentException("El archivo de credenciales (CredentialsFile) no está configurado correctamente.");
        }
        if (string.IsNullOrWhiteSpace(userToImpersonate))
        {
            _logger.LogError("El usuario a suplantar (UserToImpersonate) no está configurado.");
            throw new ArgumentException("El usuario a suplantar (UserToImpersonate) no está configurado correctamente.");
        }

        GoogleCredential credential;
        await using (var stream = new FileStream(credentialsFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(CalendarService.Scope.Calendar)
                .CreateWithUser(userToImpersonate);
        }
        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "bbbAPIGL API"
        });
    }

    private async Task<GmailService> GetGmailServiceAsync()
    {
        _logger.LogInformation("Obteniendo servicio de Gmail.");
        var settings = _configuration.GetSection("GoogleCalendarSettings");
        var credentialsFilePath = settings["CredentialsFile"];
        var userToImpersonate = settings["UserToImpersonate"];

        _logger.LogDebug("Ruta de archivo de credenciales: {path}", credentialsFilePath);
        _logger.LogDebug("Usuario a suplantar: {user}", userToImpersonate);

        if (string.IsNullOrWhiteSpace(credentialsFilePath))
        {
            _logger.LogError("El archivo de credenciales (CredentialsFile) no está configurado.");
            throw new ArgumentException("El archivo de credenciales (CredentialsFile) no está configurado correctamente.");
        }
        if (string.IsNullOrWhiteSpace(userToImpersonate))
        {
            _logger.LogError("El usuario a suplantar (UserToImpersonate) no está configurado.");
            throw new ArgumentException("El usuario a suplantar (UserToImpersonate) no está configurado correctamente.");
        }

        GoogleCredential credential;
        await using (var stream = new FileStream(credentialsFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(GmailService.Scope.GmailSend)
                .CreateWithUser(userToImpersonate);
        }
        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "bbbAPIGL API"
        });
    }

    public async Task<string?> ActualizarEventoCalendarioAsync(string eventoId, CrearSalaResponse detallesSala, List<string> correosParticipantes, DateTime fechaInicio, DateTime fechaTermino, string diasSemana, DateTime horaInicio, DateTime horaTermino)
    {
        try
        {
            _logger.LogInformation("Actualizando invitación de calendario recurrente para la sala {salaNombre}.", detallesSala.NombreSala);
            var service = await GetCalendarServiceAsync();
            var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";

            var diasRrule = ConvertirDiasParaRRule(diasSemana);
            if (string.IsNullOrEmpty(diasRrule))
            {
                _logger.LogWarning("No se actualizará la invitación porque no se proporcionaron días válidos para la recurrencia ('{diasSemana}').", diasSemana);
                return null;
            }

            var recurrenceRule = $"RRULE:FREQ=WEEKLY;UNTIL={fechaTermino:yyyyMMddTHHMMssZ};BYDAY={diasRrule}";

            var existingEvent = await service.Events.Get("primary", eventoId).ExecuteAsync();

            existingEvent.Summary = $"Clase: {detallesSala.NombreSala ?? detallesSala.FriendlyId}";
            existingEvent.Location = detallesSala.UrlSala;
            existingEvent.Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave Moderador: {detallesSala.ClaveModerador}\nClave Espectador: {detallesSala.ClaveEspectador}";
            existingEvent.Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList();
            
            var eventStartDateTime = fechaInicio.Date.Add(horaInicio.TimeOfDay);
            var eventEndDateTime = fechaInicio.Date.Add(horaTermino.TimeOfDay);

            existingEvent.Start = new EventDateTime { DateTimeDateTimeOffset = eventStartDateTime, TimeZone = timeZone };
            existingEvent.End = new EventDateTime { DateTimeDateTimeOffset = eventEndDateTime, TimeZone = timeZone };
            existingEvent.Recurrence = new List<string> { recurrenceRule };

            _logger.LogDebug("Actualizando evento recurrente en calendario: Summary='{summary}', Start='{start}', End='{end}', Recurrence='{recurrence}'", existingEvent.Summary, existingEvent.Start.DateTimeDateTimeOffset, existingEvent.End.DateTimeDateTimeOffset, existingEvent.Recurrence.FirstOrDefault());

            var request = service.Events.Update(existingEvent, "primary", eventoId);
            request.SendNotifications = true;

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

    public async Task EnviarCorreoSimpleAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        await EnviarCorreosAsync(new List<string> { destinatario }, asunto, cuerpoHtml);
    }
}