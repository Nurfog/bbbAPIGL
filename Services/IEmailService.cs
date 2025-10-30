using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using bbbAPIGL.DTOs;

namespace bbbAPIGL.Services;

public interface IEmailService
{
    Task<string?> EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala,
        List<string> correosParticipantes);
    Task<string?> EnviarInvitacionCalendarioAsync(
        CrearSalaResponse detallesSala, 
        List<string> correosParticipantes, 
        DateTime fechaInicio, 
        DateTime fechaTermino, 
        string diasSemana,
        DateTime horaInicio,
        DateTime horaTermino);
    Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml);
    Task EliminarEventoCalendarioAsync(string eventoId);
    Task<string?> ActualizarEventoCalendarioAsync(string eventoId, CrearSalaResponse detallesSala, List<string> correosParticipantes, DateTime fechaInicio, DateTime fechaTermino, string diasSemana, DateTime horaInicio, DateTime horaTermino);
    Task EnviarCorreoSimpleAsync(string destinatario, string asunto, string cuerpoHtml);
}
