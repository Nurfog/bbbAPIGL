using bbbAPIGL.DTOs;
using Google.Apis.Auth.OAuth2;
using System;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Text;

namespace bbbAPIGL.Services;

public class GoogleCalendarService : IEmailService
{
    private readonly IConfiguration _configuration;

    public GoogleCalendarService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Sobrecarga para eventos no recurrentes (ej. al crear una sala directamente)
    public async Task EnviarInvitacionCalendarioAsync(CrearSalaResponse detallesSala, List<string> correosParticipantes)
    {
        var service = await GetCalendarServiceAsync();
        var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";

        // Crear el evento base usando el método privado
        var newEvent = CrearEventoBase(detallesSala, correosParticipantes, timeZone);
        newEvent.Start = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow, TimeZone = timeZone };
        newEvent.End = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow.AddHours(1), TimeZone = timeZone };

        var request = service.Events.Insert(newEvent, "primary");
        request.SendNotifications = true;

        await request.ExecuteAsync();
    }

    public async Task EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala, 
        List<string> correosParticipantes,
        DateTime fechaInicio, 
        DateTime fechaTermino, 
        string diasSemana,
        TimeSpan horaInicio,
        TimeSpan horaTermino)
    {
        var service = await GetCalendarServiceAsync();
        var timeZone = _configuration["GoogleCalendarSettings:DefaultTimeZone"] ?? "America/Santiago";

        // Convertimos los días de la semana del formato "L,M,W,J,V" al formato RRULE "MO,TU,WE,TH,FR"
        var diasRrule = ConvertirDiasParaRRule(diasSemana);
        if (string.IsNullOrEmpty(diasRrule))
        {
            // No hacer nada si no hay días válidos para la recurrencia.
            return;
        }

        // La regla de recurrencia (RRULE)
        var recurrenceRule = $"RRULE:FREQ=WEEKLY;UNTIL={fechaTermino:yyyyMMddTHHMMssZ};BYDAY={diasRrule}";
        
        // Crear el evento base y añadirle las propiedades de recurrencia
        var newEvent = CrearEventoBase(detallesSala, correosParticipantes, timeZone);

        var eventStartDateTime = fechaInicio.Date.Add(horaInicio);
        var eventEndDateTime = fechaInicio.Date.Add(horaTermino);

        newEvent.Start = new EventDateTime { DateTimeDateTimeOffset = eventStartDateTime, TimeZone = timeZone };
        newEvent.End = new EventDateTime { DateTimeDateTimeOffset = eventEndDateTime, TimeZone = timeZone };
        newEvent.Recurrence = new List<string> { recurrenceRule };

        var request = service.Events.Insert(newEvent, "primary");
        request.SendNotifications = true;

        await request.ExecuteAsync();
    }

    private Event CrearEventoBase(CrearSalaResponse detallesSala, List<string> correosParticipantes, string timeZone)
    {
        return new Event
        {
            // Usamos NombreSala si está disponible; si no, FriendlyId como respaldo.
            Summary = $"Clase: {detallesSala.FriendlyId ?? detallesSala.FriendlyId}",
            Location = detallesSala.UrlSala,
            // Se añade la clave de moderador para que sea visible en la invitación.
            Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave Moderador: {detallesSala.ClaveModerador}\nClave Espectador: {detallesSala.ClaveEspectador}",
            Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList(),
            // Aseguramos que las notificaciones se envíen a los asistentes
            Reminders = new Event.RemindersData { UseDefault = true }
        };
    }

    private string ConvertirDiasParaRRule(string dias)
    {
        var mapeoDias = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "L", "MO" }, { "M", "TU" }, { "W", "WE" }, { "J", "TH" }, { "V", "FR" }, { "S", "SA" }, { "D", "SU" }
        };

        var diasSplit = dias.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        
        return string.Join(",", diasSplit
            .Select(dia => dia.Trim())
            .Where(dia => mapeoDias.ContainsKey(dia))
            .Select(dia => mapeoDias[dia]));
    }

    public async Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml)
    {
        var service = await GetGmailServiceAsync();
        var fromEmail = _configuration["GoogleCalendarSettings:UserToImpersonate"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
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
    }

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        var settings = _configuration.GetSection("GoogleCalendarSettings");
        var credentialsFilePath = settings["CredentialsFile"];
        if (string.IsNullOrWhiteSpace(credentialsFilePath))
        {
            throw new ArgumentException("El archivo de credenciales (CredentialsFile) no está configurado correctamente.");
        }
        var userToImpersonate = settings["UserToImpersonate"];
        if (string.IsNullOrWhiteSpace(userToImpersonate))
        {
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
        var settings = _configuration.GetSection("GoogleCalendarSettings");
        var credentialsFilePath = settings["CredentialsFile"];
        if (string.IsNullOrWhiteSpace(credentialsFilePath))
        {
            throw new ArgumentException("El archivo de credenciales (CredentialsFile) no está configurado correctamente.");
        }
        var userToImpersonate = settings["UserToImpersonate"];
        if (string.IsNullOrWhiteSpace(userToImpersonate))
        {
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
}
