using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.DTOs;

namespace bbbAPIGL.Services;

public interface IEmailService
{
    Task EnviarInvitacionCalendarioAsync(CrearSalaResponse detallesSala, List<string> correosParticipantes);
    Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml);
}