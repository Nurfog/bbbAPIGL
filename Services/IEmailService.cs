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
<<<<<<< HEAD
        string diasSemana,
        TimeSpan horaInicio,
        TimeSpan horaTermino);
=======
        string diasSemana);
>>>>>>> bc0cedfcf1b28861c8327da4f8ee316f08aecbf9
    Task EnviarCorreosAsync(List<string> destinatarios, string asunto, string cuerpoHtml);
}
