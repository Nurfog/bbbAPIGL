/// <summary>
/// Interfaz para el servicio de envío de correos electrónicos.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 01-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public interface IEmailService
{
    /// <summary>
    /// Envía correos electrónicos a una lista de destinatarios.
    /// </summary>
    /// <param name="destinatarios">Lista de direcciones de correo electrónico de los destinatarios.</param>
    /// <param name="asunto">El asunto del correo electrónico.</param>
    /// <param name="cuerpoHtml">El cuerpo del correo electrónico en formato HTML.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml);
    /// <summary>
    /// Envía un correo electrónico simple a un único destinatario.
    /// </summary>
    /// <param name="destinatario">La dirección de correo electrónico del destinatario.</param>
    /// <param name="asunto">El asunto del correo electrónico.</param>
    /// <param name="cuerpoHtml">El cuerpo del correo electrónico en formato HTML.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    Task EnviarCorreoSimpleAsync(string destinatario, string asunto, string cuerpoHtml);
}
