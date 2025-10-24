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
        
        var newEvent = new Event
        {
            Summary = $"Invitación: {detallesSala.FriendlyId}",
            Location = detallesSala.UrlSala,
            Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave de Espectador: {detallesSala.ClaveEspectador}",
            Start = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow, TimeZone = "America/Santiago" }, // Por defecto, ahora
            End = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow.AddHours(1), TimeZone = "America/Santiago" }, // Por defecto, 1 hora después
            Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList(),
            Reminders = new Event.RemindersData { UseDefault = true }
        };

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

        // Convertimos los días de la semana del formato "L,M,W,J,V" al formato RRULE "MO,TU,WE,TH,FR"
        var diasRrule = ConvertirDiasParaRRule(diasSemana);

        // La regla de recurrencia (RRULE)
        var recurrenceRule = $"RRULE:FREQ=WEEKLY;UNTIL={fechaTermino:yyyyMMddTHHMMssZ};BYDAY={diasRrule}";
        
        // Combinamos la fecha de inicio del curso con la hora de inicio y fin de la clase
        var eventStartDateTime = fechaInicio.Date.Add(horaInicio);
        var eventEndDateTime = fechaInicio.Date.Add(horaTermino);

        var newEvent = new Event
        {
            Summary = $"Invitación: {detallesSala.FriendlyId}",
            Location = detallesSala.UrlSala,
            Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave de Espectador: {detallesSala.ClaveEspectador}",
            Start = new EventDateTime { DateTimeDateTimeOffset = eventStartDateTime, TimeZone = "America/Santiago" },
            End = new EventDateTime { DateTimeDateTimeOffset = eventEndDateTime, TimeZone = "America/Santiago" },
            Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList(),
            Recurrence = new List<string> { recurrenceRule },
            // Aseguramos que las notificaciones se envíen a los asistentes
            Reminders = new Event.RemindersData { UseDefault = true }
        };

        var request = service.Events.Insert(newEvent, "primary");
        request.SendNotifications = true;

        await request.ExecuteAsync();
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

        if (string.IsNullOrEmpty(fromEmail))
        {
            throw new InvalidOperationException("El correo para suplantación (UserToImpersonate) no está configurado.");
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
await service.Users.Messages.Send(message, "me").ExecuteAsync();
    }

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        var settings = _configuration.GetSection("GoogleCalendarSettings");
        var credentialsFile = settings["CredentialsFile"];
        var userToImpersonate = settings["UserToImpersonate"];

        if (string.IsNullOrEmpty(credentialsFile))
        {
            throw new InvalidOperationException("El archivo de credenciales de Google (CredentialsFile) no está configurado.");
        }
        if (string.IsNullOrEmpty(userToImpersonate))
        {
            throw new InvalidOperationException("El usuario a suplantar de Google (UserToImpersonate) no está configurado.");
        }

        GoogleCredential credential;
        await using (var stream = new FileStream(credentialsFile, FileMode.Open, FileAccess.Read))
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
        var credentialsFile = settings["CredentialsFile"];
        var userToImpersonate = settings["UserToImpersonate"];

        if (string.IsNullOrEmpty(credentialsFile))
        {
            throw new InvalidOperationException("El archivo de credenciales de Google (CredentialsFile) no está configurado.");
        }
        if (string.IsNullOrEmpty(userToImpersonate))
        {
            throw new InvalidOperationException("El usuario a suplantar de Google (UserToImpersonate) no está configurado.");
        }

        GoogleCredential credential;
        await using (var stream = new FileStream(credentialsFile, FileMode.Open, FileAccess.Read))
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