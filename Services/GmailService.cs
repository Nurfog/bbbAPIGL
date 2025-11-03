using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Servicio para el envío de correos electrónicos utilizando la API de Gmail.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 18-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public class GmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailService> _logger;

    public GmailService(IConfiguration configuration, ILogger<GmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Envía correos electrónicos a una lista de destinatarios utilizando la API de Gmail.
    /// </summary>
    /// <param name="destinatarios">Lista de direcciones de correo electrónico de los destinatarios.</param>
    /// <param name="asunto">El asunto del correo electrónico.</param>
    /// <param name="cuerpoHtml">El cuerpo del correo electrónico en formato HTML.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
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
                throw new System.ArgumentException("El correo del remitente (UserToImpersonate) no está configurado correctamente.");
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
            
            var rawMessage = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(mimeMessage.ToString()))
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
            
            var message = new Message { Raw = rawMessage };
            await service.Users.Messages.Send(message, fromEmail).ExecuteAsync();
            _logger.LogInformation("Correo enviado exitosamente.");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo.");
            throw;
        }
    }

    /// <summary>
    /// Envía un correo electrónico simple a un único destinatario utilizando la API de Gmail.
    /// </summary>
    /// <param name="destinatario">La dirección de correo electrónico del destinatario.</param>
    /// <param name="asunto">El asunto del correo electrónico.</param>
    /// <param name="cuerpoHtml">El cuerpo del correo electrónico en formato HTML.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task EnviarCorreoSimpleAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        await EnviarCorreosAsync(new List<string> { destinatario }, asunto, cuerpoHtml);
    }

    /// <summary>
    /// Obtiene una instancia de GmailService autenticada.
    /// </summary>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una instancia de GmailService.</returns>
    private async Task<GmailService> GetGmailServiceAsync()
    {
        _logger.LogInformation("Obteniendo servicio de Gmail.");
        var credential = await GoogleAuthService.GetGoogleCredentialAsync(_configuration, _logger, GmailService.Scope.GmailSend);
        return new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "bbbAPIGL API"
        });
    }
}
