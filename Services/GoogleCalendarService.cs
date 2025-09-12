using bbbAPIGL.DTOs;
using Google.Apis.Auth.OAuth2;
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

    public async Task EnviarInvitacionCalendarioAsync(CrearSalaResponse detallesSala, List<string> correosParticipantes)
    {
        var service = await GetCalendarServiceAsync();
        
        var newEvent = new Event
        {
            Summary = $"Invitación: {detallesSala.FriendlyId}",
            Location = detallesSala.UrlSala,
            Description = $"Únete a la sala virtual.\n\nURL: {detallesSala.UrlSala}\nClave de Espectador: {detallesSala.ClaveEspectador}",
            Start = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow },
            End = new EventDateTime { DateTimeDateTimeOffset = DateTime.UtcNow.AddHours(1) },
            Attendees = correosParticipantes.Select(email => new EventAttendee { Email = email }).ToList()
        };

        var request = service.Events.Insert(newEvent, "primary");
        request.SendNotifications = true;

        await request.ExecuteAsync();
    }

    public async Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml)
    {
        var service = await GetGmailServiceAsync();
        var fromEmail = _configuration["GoogleCalendarSettings:UserToImpersonate"];

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
await service.Users.Messages.Send(message, "jallende@norteamericano.cl").ExecuteAsync();
    }

    private async Task<CalendarService> GetCalendarServiceAsync()
    {
        var settings = _configuration.GetSection("GoogleCalendarSettings");
        GoogleCredential credential;
        await using (var stream = new FileStream(settings["CredentialsFile"], FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(CalendarService.Scope.Calendar)
                .CreateWithUser(settings["UserToImpersonate"]);
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
        GoogleCredential credential;
        await using (var stream = new FileStream(settings["CredentialsFile"], FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(GmailService.Scope.GmailSend)
                .CreateWithUser(settings["UserToImpersonate"]);
        }
        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "bbbAPIGL API"
        });
    }
}