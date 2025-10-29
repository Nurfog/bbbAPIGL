using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using bbbAPIGL.DTOs;

namespace bbbAPIGL.Services;

public interface IEmailService
{
    Task EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala,
        List<string> correosParticipantes);
    Task EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala, 
        List<string> correosParticipantes, 
        DateTime fechaInicio, 
        DateTime fechaTermino, 
        string diasSemana,
        DateTime horaInicio,
        DateTime horaTermino);
    Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml);
}
